using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Toletus.Extensions;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Base;

public class LiteNet2BoardBase
{
    public static Action<string>? Log;

    public const int Port = 7878;

    public IPAddress Ip { get; set; }
    public IPAddress? NetworkIp { get; set; }
    public int Id { get; set; }
    public bool HasFingerprintReader { get; set; }

    public delegate void IdentificationHandler(LiteNet2BoardBase liteNet2Board, Identification identification);
    public event Action<BoardResponseCommand>? OnResponse;
    public event IdentificationHandler? OnIdentification;
    public event Action<LiteNet2BoardBase, ConnectionStatus>? OnConnectionStatusChanged;
    public event Action<string>? OnStatus;
    public event Action<LiteNet2BoardBase, BoardSendCommand>? OnSend;

    private TcpClient? _tcpClient;

    public bool Connected => _tcpClient?.Client != null &&  _tcpClient.Connected;

    public LiteNet2BoardBase(IPAddress ip, int? id = null)
    {
        Ip = ip;
        if (id.HasValue) Id = id.Value;
    }

    public override string ToString() => $"LiteNet2 #{Id} {Ip}:{Port}";

    public void Connect()
    {
        try
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(Ip, Port);

            _ = Response();

            OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connected);
        }
        catch (SocketException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private void CheckConnection()
    {
        Send(Commands.GetId);
    }

    public void Close()
    {
        _tcpClient?.Close();
        OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Closed);
    }

    private async Task Response()
    {
        Log?.Invoke("Response start");

        var buffer = new byte[1024];
        try
        {
            var bytesRead = 1;

            while (bytesRead != 0)
            {
                bytesRead = await _tcpClient!.GetStream().ReadAsync(buffer, 0, buffer.Length);

                var respFull = buffer.Take(bytesRead).ToArray();

                var skip = 0;
                while (respFull.Length > skip)
                {
                    var resp = respFull.Skip(skip).Take(20).ToArray();

                    var responseCommand = ProcessResponse(resp);

                    OnResponse?.Invoke(responseCommand);

                    skip += 20;
                }
            }
        }
        catch (ObjectDisposedException e)
        {
            Log?.Invoke($"Response ObjectDisposedException {e.ToLogString(Environment.StackTrace)}");
        }
        catch (IOException e)
        {
            Log?.Invoke($"Connection closed. Receive boardResponse finised. (IOException)");
            _tcpClient?.Close();
            throw;
        }
        catch (Exception e)
        {
            Log?.Invoke($"Response Exception {e.ToLogString(Environment.StackTrace)}");
            Close();
            throw;
        }
        finally
        {
            Log?.Invoke("Response finally");
        }
    }

    private BoardResponseCommand ProcessResponse(byte[] resp)
    {
        var response = new BoardResponseCommand(resp);

        switch (response.Command)
        {
            case Commands.NegativeIdentificationByFingerprintReader:
            case Commands.PositiveIdentificationByFingerprintReader:
            case Commands.IdentificationByBarCode:
            case Commands.IdentificationByRfId:
            case Commands.IdentificationByKeyboard:
                response.Identification = ProcessIdentificationResponse(response);
                break;
        }

        return response;
    }

    private Identification ProcessIdentificationResponse(BoardResponseCommand boardResponse)
    {
        Identification? identification = null;

        switch (boardResponse.Command)
        {
            case Commands.IdentificationByKeyboard:
                identification = new Identification(IdentificationDevice.Keyboard, int.Parse(boardResponse.DataString));
                break;
            case Commands.IdentificationByBarCode:
                identification = new Identification(IdentificationDevice.BarCode, int.Parse(boardResponse.DataString));
                break;
            case Commands.IdentificationByRfId:
                identification = new Identification(IdentificationDevice.Rfid, int.Parse(boardResponse.DataString));
                break;
            case Commands.PositiveIdentificationByFingerprintReader:
            case Commands.NegativeIdentificationByFingerprintReader:
                identification = new Identification(IdentificationDevice.EmbeddedFingerprint, int.Parse(boardResponse.Data.ToString()));
                HasFingerprintReader = true;
                break;
        }

        OnIdentification?.Invoke(this, identification!);

        return identification!;
    }

    public void Send(Commands command, int parameter)
    {
        Send(command, BitConverter.GetBytes(parameter));
    }

    public void Send(Commands command, byte parameter)
    {
        Send(command, new[] { parameter });
    }

    public void Send(Commands command, string parameter)
    {
        parameter = parameter.Truncate(16).PadRight(16, '\0');

        Send(command, Encoding.ASCII.GetBytes(parameter));
    }

    public void Send(Commands command, byte[]? parameter = null)
    {
        var send = new BoardSendCommand(command, parameter);

        Send(send);
    }

    public void Send(ushort comando, byte[]? parameter = null)
    {
        var send = new BoardSendCommand(comando, parameter);

        Send(send);
    }

    public void Send(BoardSendCommand boardSend)
    {
        OnSend?.Invoke(this, boardSend);

        if (!Connected) 
        {
            TryReconnect();
            return;
        }

        var stream = _tcpClient!.GetStream();

        try
        {
            stream.Write(boardSend.Payload, 0, boardSend.Payload.Length);
        }
        catch (SocketException sex)
        {
            _tcpClient?.Close();
            throw;
        }
        catch (IOException iox)
        {
            _tcpClient?.Close();
            throw;
        }
        catch (Exception e)
        {
            _tcpClient?.Close();
            throw;
        }
    }

    private bool _reconnecting;

    private void TryReconnect()
    { 
        Task.Run(async () =>
        {
            try
            {
                if (_reconnecting) return;

                OnStatus?.Invoke("Reconnecting");

                while (!Connected)
                {
                    _reconnecting = true;
                    await Task.Delay(200);
                    Connect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                _reconnecting = false;
            }
        });
    }

    protected void EventStatus(string status) => OnStatus?.Invoke(status);
}