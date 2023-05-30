using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core;

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
    public event Action<LiteNetResponse>? OnResponse;
    public event IdentificationHandler? OnIdentification;
    public event Action<LiteNet2BoardBase, BoardConnectionStatus>? OnConnectionStatusChanged;
    public event Action<string>? OnStatus;
    public event Action<LiteNet2BoardBase, LiteNetSend>? OnSend;

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

            OnConnectionStatusChanged?.Invoke(this, BoardConnectionStatus.Connected);
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
        Send(LiteNet2Commands.GetId);
    }

    public void Close()
    {
        _tcpClient?.Close();
        OnConnectionStatusChanged?.Invoke(this, BoardConnectionStatus.Closed);
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

    private LiteNetResponse ProcessResponse(byte[] resp)
    {
        var response = new LiteNetResponse(resp);

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

    private Identification ProcessIdentificationResponse(LiteNetResponse liteNetResponse)
    {
        Identification? identification = null;

        switch (liteNetResponse.Command)
        {
            case LiteNet2Commands.IdentificationByKeyboard:
                identification = new Identification(IdentificationDevice.Keyboard, int.Parse(liteNetResponse.DataString));
                break;
            case LiteNet2Commands.IdentificationByBarCode:
                identification = new Identification(IdentificationDevice.BarCode, int.Parse(liteNetResponse.DataString));
                break;
            case LiteNet2Commands.IdentificationByRfId:
                identification = new Identification(IdentificationDevice.Rfid, int.Parse(liteNetResponse.DataString));
                break;
            case LiteNet2Commands.PositiveIdentificationByFingerprintReader:
            case LiteNet2Commands.NegativeIdentificationByFingerprintReader:
                identification = new Identification(IdentificationDevice.EmbeddedFingerprint, int.Parse(liteNetResponse.Data.ToString()));
                HasFingerprintReader = true;
                break;
        }

        OnIdentification?.Invoke(this, identification!);

        return identification!;
    }

    public void Send(LiteNet2Commands liteNet2Command, int parameter)
    {
        Send(liteNet2Command, BitConverter.GetBytes(parameter));
    }

    public void Send(LiteNet2Commands liteNet2Command, byte parameter)
    {
        Send(liteNet2Command, new[] { parameter });
    }

    public void Send(LiteNet2Commands liteNet2Command, string parameter)
    {
        parameter = parameter.Truncate(16).PadRight(16, '\0');

        Send(liteNet2Command, Encoding.ASCII.GetBytes(parameter));
    }

    public void Send(LiteNet2Commands liteNet2Command, byte[]? parameter = null)
    {
        var send = new LiteNetSend(liteNet2Command, parameter);

        Send(send);
    }

    public void Send(ushort comando, byte[]? parameter = null)
    {
        var send = new LiteNetSend(comando, parameter);

        Send(send);
    }

    public void Send(LiteNetSend liteNetSend)
    {
        OnSend?.Invoke(this, liteNetSend);

        if (!Connected) 
        {
            TryReconnect();
            return;
        }

        var stream = _tcpClient!.GetStream();

        try
        {
            stream.Write(liteNetSend.Payload, 0, liteNetSend.Payload.Length);
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