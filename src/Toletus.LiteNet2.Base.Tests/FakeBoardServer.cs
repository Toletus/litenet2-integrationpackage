using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Toletus.LiteNet2.Base.Tests;

/// <summary>
/// Arnês de equipamento simulado (T-002): servidor TCP controlável em loopback para os
/// ACs [e2e] de REQ-001/002/004/005/006/007. Modos: aceita-e-silencia (meia-aberta),
/// fragmentação em fronteiras arbitrárias, resposta malformada, encerramento com reset,
/// sumiço sem encerramento. Expõe contagem de conexões aceitas.
/// </summary>
public sealed class FakeBoardServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<Socket> _accepted = new();
    private volatile Socket? _last;
    private int _connectionCount;

    public FakeBoardServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = AcceptLoopAsync(_cts.Token);
    }

    public int Port { get; }

    /// <summary>Total de conexões TCP aceitas neste endereço (AC-002.3: deve ser 0 para excluídos).</summary>
    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket socket;
            try
            {
                socket = await _listener.AcceptSocketAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            Interlocked.Increment(ref _connectionCount);
            _accepted.Enqueue(socket);
            _last = socket;
        }
    }

    /// <summary>Espera até uma conexão ser aceita (ou estoura o timeout).</summary>
    public bool WaitForConnection(TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (_last != null) return true;
            Thread.Sleep(10);
        }
        return _last != null;
    }

    /// <summary>Envia bytes ao cliente, fragmentando em pedaços de <paramref name="chunkSize"/> com pausa entre eles.</summary>
    public void PushBytes(byte[] data, int chunkSize, int delayMsBetweenChunks = 5)
    {
        var socket = RequireLast();
        var offset = 0;
        while (offset < data.Length)
        {
            var take = Math.Min(chunkSize, data.Length - offset);
            socket.Send(data, offset, take, SocketFlags.None);
            offset += take;
            if (delayMsBetweenChunks > 0 && offset < data.Length)
                Thread.Sleep(delayMsBetweenChunks);
        }
    }

    /// <summary>Encerramento ordenado (FIN): a leitura do cliente retorna 0 → RemoteClose.
    /// Só Shutdown(Send) — envia FIN limpo sem risco de RST (fechar já em seguida podia gerar reset).</summary>
    public void CloseGraceful()
    {
        var socket = RequireLast();
        socket.Shutdown(SocketShutdown.Send);
    }

    /// <summary>Encerramento abrupto (RST): a leitura do cliente lança → ReceiveError.</summary>
    public void CloseWithReset()
    {
        var socket = RequireLast();
        socket.LingerState = new LingerOption(true, 0);
        socket.Close();
    }

    private Socket RequireLast() => _last ?? throw new InvalidOperationException("No client connected yet");

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        while (_accepted.TryDequeue(out var s))
        {
            try { s.Close(); } catch { /* ignore */ }
        }
        _cts.Dispose();
    }
}
