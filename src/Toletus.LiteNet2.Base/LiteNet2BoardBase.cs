using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core.Extensions;

namespace Toletus.LiteNet2.Base;

public class LiteNet2BoardBase
{
    public static Action<string>? Log;

    public const int Port = 7878;

    /// <summary>Porta efetiva de conexão. Default = <see cref="Port"/>; sobrescrevível em teste.</summary>
    public int ConnectPort { get; set; } = Port;

    /// <summary>Tamanho fixo do frame LiteNet2 (prefixo+comando+dados+sufixo).</summary>
    public const int FrameSize = 20;

    // Parâmetros de resiliência expostos para calibração em bancada (TDD §4.5 / risco §10).
    // ponytail: defaults iniciais; valores finais saem do soak 24h (T-061) e entram por config no app.
    public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public static TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public static TimeSpan KeepAliveTime { get; set; } = TimeSpan.FromSeconds(10);
    public static TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(5);
    public static int KeepAliveRetryCount { get; set; } = 3;

    public IPAddress Ip { get; set; }
    public IPAddress? NetworkIp { get; set; }
    public int Id { get; set; }
    public string? SerialNumber { get; set; }
    public string ConnectionInfo { get; set; }

    public bool InUse => Connected
                         || ConnectionInfo.Length > 0 && ConnectionInfo != "Disconnected";

    public bool HasFingerprintReader { get; set; }

    public delegate void IdentificationHandler(LiteNet2BoardBase liteNet2Board, Identification identification);

    public delegate void StatusHandler(LiteNet2BoardBase liteNet2Board, string status);

    public event Action<LiteNet2Response>? OnResponse;
    public event IdentificationHandler? OnIdentification;

    public event Action<LiteNet2BoardBase, ConnectionStateChange>? OnConnectionStatusChanged;
    public event StatusHandler? OnStatus;
    public event Action<LiteNet2BoardBase, LiteNet2Send>? OnSend;

    private TcpClient? _tcpClient;
    private SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _receiveCts;
    private Channel<LiteNet2Response>? _dispatchQueue;
    private int _closing;

    public bool Connected => _tcpClient?.Client != null && _tcpClient.Connected;

    public LiteNet2BoardBase(IPAddress ip, string? serialNumber = null, int? id = null, string connectionInfo = "")
    {
        Ip = ip;
        SerialNumber = serialNumber;
        if (id.HasValue) Id = id.Value;
        ConnectionInfo = connectionInfo == "None" ? "Disconnected" : connectionInfo;
    }

    public override string ToString() => $"LiteNet2 #{Id} {Ip}:{Port} {ConnectionInfo}";

    /// <summary>Conexão síncrona (com teto). Mantida para consumidores atuais.</summary>
    public void Connect() => ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Conexão assíncrona com teto próprio, keepalive de transporte e teto de envio (T-010/AC-004.1).
    /// Não inicia recuperação autônoma — o consumidor é o único dono da reconexão (ADR-001).
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient();
        ConfigureSocket(client.Client);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConnectTimeout);

        try
        {
            await client.ConnectAsync(Ip, ConnectPort, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            client.Dispose();
            throw new TimeoutException($"Connect to {Ip}:{Port} exceeded {ConnectTimeout}");
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _tcpClient = client;
        _sendLock = new SemaphoreSlim(1, 1);
        _closing = 0;
        _receiveCts = new CancellationTokenSource();
        StartDispatch(_receiveCts.Token);
        _ = ReceiveLoopAsync(_receiveCts.Token);

        RaiseStateChange(BoardConnectionStatus.Connected, ConnectionCause.None);
    }

    private static void ConfigureSocket(Socket socket)
    {
        socket.SendTimeout = (int)SendTimeout.TotalMilliseconds;

        try
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime,
                (int)KeepAliveTime.TotalSeconds);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval,
                (int)KeepAliveInterval.TotalSeconds);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount,
                KeepAliveRetryCount);
        }
        catch (Exception e)
        {
            // ponytail: keepalive fino é best-effort por plataforma; SO_KEEPALIVE base já ativado acima quando suportado.
            Log?.Invoke($"KeepAlive tuning unavailable: {e.Message}");
        }
    }

    public void CheckConnection()
    {
        Send(LiteNet2Commands.GetId);
    }

    /// <summary>Encerramento local explícito (AC-004.3: publica mudança de estado com causa).</summary>
    public void Close() => CloseInternal(BoardConnectionStatus.Closed, ConnectionCause.LocalClose);

    private void CloseInternal(BoardConnectionStatus status, ConnectionCause cause)
    {
        // Um único caminho de morte publica o evento — reentrância protegida (AC-004.3).
        if (Interlocked.Exchange(ref _closing, 1) == 1) return;

        try
        {
            _receiveCts?.Cancel();
            _dispatchQueue?.Writer.TryComplete();
            _tcpClient?.Close();
        }
        catch (Exception e)
        {
            Log?.Invoke($"Close error: {e.Message}");
        }

        RaiseStateChange(status, cause);
    }

    private void RaiseStateChange(BoardConnectionStatus status, ConnectionCause cause)
    {
        var change = new ConnectionStateChange(status, cause);
        Log?.Invoke($"{this} -> {change}");
        try
        {
            OnConnectionStatusChanged?.Invoke(this, change);
        }
        catch (Exception e)
        {
            Log?.Invoke($"OnConnectionStatusChanged handler error: {e.Message}");
        }
    }

    // ---- Recepção com remontagem + fila de despacho (T-012/T-013) ----

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var stream = _tcpClient!.GetStream();
        var buffer = new byte[1024];
        var assembler = new FrameAssembler(FrameSize);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // Fim de stream = encerramento remoto ordenado.
                    CloseInternal(BoardConnectionStatus.Failed, ConnectionCause.RemoteClose);
                    return;
                }

                foreach (var frame in assembler.Push(buffer, bytesRead))
                {
                    LiteNet2Response? response = null;
                    try
                    {
                        response = ProcessResponse(frame);
                    }
                    catch (Exception e)
                    {
                        // Mensagem malformada: descarta, mantém a conexão viva (AC-006.2).
                        Log?.Invoke($"Malformed message discarded: {e.Message}");
                    }

                    if (response != null)
                        _dispatchQueue?.Writer.TryWrite(response); // despacho fora do laço (AC-006.4)
                }
            }
        }
        catch (OperationCanceledException)
        {
            // cancelamento por Close local — evento já publicado por CloseInternal.
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            // keepalive expirado / reset / socket morto sem FIN (AC-004.1).
            Log?.Invoke($"Receive terminated: {e.Message}");
            CloseInternal(BoardConnectionStatus.Failed, ConnectionCause.ReceiveError);
        }
        catch (Exception e)
        {
            Log?.Invoke($"Receive unexpected error: {e.ToLogString(Environment.StackTrace)}");
            CloseInternal(BoardConnectionStatus.Failed, ConnectionCause.ReceiveError);
        }
    }

    private void StartDispatch(CancellationToken ct)
    {
        _dispatchQueue = Channel.CreateUnbounded<LiteNet2Response>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in _dispatchQueue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        OnResponse?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        // Handler lento/com erro não derruba a recepção (AC-006.3).
                        Log?.Invoke($"OnResponse handler error: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }, ct);
    }

    private LiteNet2Response ProcessResponse(byte[] resp)
    {
        var response = new LiteNet2Response(resp);

        switch (response.Command)
        {
            case LiteNet2Commands.NegativeIdentificationByFingerprintReader:
            case LiteNet2Commands.PositiveIdentificationByFingerprintReader:
            case LiteNet2Commands.IdentificationByBarCode:
            case LiteNet2Commands.IdentificationByRfId:
            case LiteNet2Commands.IdentificationByKeyboard:
                response.Identification = ProcessIdentificationResponse(response);
                break;
        }

        return response;
    }

    private Identification ProcessIdentificationResponse(LiteNet2Response liteNet2Response)
    {
        Identification? identification = null;

        switch (liteNet2Response.Command)
        {
            case LiteNet2Commands.IdentificationByKeyboard:
                identification =
                    new Identification(IdentificationDevice.Keyboard, int.Parse(liteNet2Response.DataString));
                break;
            case LiteNet2Commands.IdentificationByBarCode:
                identification =
                    new Identification(IdentificationDevice.BarCode, int.Parse(liteNet2Response.DataString));
                break;
            case LiteNet2Commands.IdentificationByRfId:
                identification = new Identification(IdentificationDevice.Rfid, int.Parse(liteNet2Response.DataString));
                break;
            case LiteNet2Commands.PositiveIdentificationByFingerprintReader:
            case LiteNet2Commands.NegativeIdentificationByFingerprintReader:
                identification = new Identification(IdentificationDevice.EmbeddedFingerprint,
                    int.Parse(liteNet2Response.Data.ToString()));
                HasFingerprintReader = true;
                break;
        }

        OnIdentification?.Invoke(this, identification!);

        return identification!;
    }

    // ---- Envio serializado com resultado honesto (T-014) ----

    public SendResult Send(LiteNet2Commands liteNet2Command, int parameter)
        => Send(liteNet2Command, BitConverter.GetBytes(parameter));

    public SendResult Send(LiteNet2Commands liteNet2Command, byte parameter)
        => Send(liteNet2Command, new[] { parameter });

    public SendResult Send(LiteNet2Commands liteNet2Command, string parameter)
    {
        parameter = parameter.Truncate(16).PadRight(16, '\0');
        return Send(liteNet2Command, Encoding.ASCII.GetBytes(parameter));
    }

    public SendResult Send(LiteNet2Commands liteNet2Command, byte[]? parameter = null)
        => Send(new LiteNet2Send(liteNet2Command, parameter));

    public SendResult Send(ushort comando, byte[]? parameter = null)
        => Send(new LiteNet2Send(comando, parameter));

    public SendResult Send(LiteNet2Send liteNet2Send)
    {
        OnSend?.Invoke(this, liteNet2Send);

        if (!Connected)
            return SendResult.Fail(SendResult.SendError.Disconnected, $"{this} disconnected"); // AC-005.1

        _sendLock.Wait();
        try
        {
            var stream = _tcpClient!.GetStream();
            stream.Write(liteNet2Send.Payload, 0, liteNet2Send.Payload.Length);
            return SendResult.Ok();
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException)
        {
            CloseInternal(BoardConnectionStatus.Failed, ConnectionCause.SendError);
            return SendResult.Fail(SendResult.SendError.WriteFailure, e.Message);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    protected void EventStatus(string status) => OnStatus?.Invoke(this, status);

    public async Task FetchAndSetSerialNumberAsync()
    {
        try
        {
            using var tcpClient = new TcpClient();
            // Teto curto cobrindo conexão E leitura (AC-003.4): equipamento que atende e cala
            // não pode prender a varredura (~21s presos por tentativa era o defeito).
            using var timeoutCts = new CancellationTokenSource(ConnectTimeout);
            await tcpClient.ConnectAsync(Ip, ConnectPort, timeoutCts.Token).ConfigureAwait(false);

            var stream = tcpClient.GetStream();
            var request = new LiteNet2Send(LiteNet2Commands.GetSerialNumber);

            await stream.WriteAsync(request.Payload.AsMemory(), timeoutCts.Token).ConfigureAwait(false);

            var buffer = new byte[FrameSize];
            var bytesRead = 0;

            while (bytesRead < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(bytesRead, buffer.Length - bytesRead),
                    timeoutCts.Token).ConfigureAwait(false);

                if (read == 0)
                    break;

                bytesRead += read;
            }

            if (bytesRead == buffer.Length)
            {
                var response = new LiteNet2Response(buffer);

                if (response.Command == LiteNet2Commands.GetSerialNumber)
                    SerialNumber = BitConverter.ToInt32(response.RawData, 0).ToString();
            }
        }
        catch (Exception e)
        {
            Log?.Invoke($"FetchAndSetSerialNumberAsync failed {e.Message}");
        }
    }
}
