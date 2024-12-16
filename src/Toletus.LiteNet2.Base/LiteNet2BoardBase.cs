﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core.Extensions;

namespace Toletus.LiteNet2.Base;

public class LiteNet2BoardBase
{
    public static Action<string>? Log;

    public const int Port = 7878;

    public IPAddress Ip { get; set; }
    public IPAddress? NetworkIp { get; set; }
    public int Id { get; set; }
    public string ConnectionInfo { get; set; }

    public bool InUse => Connected
                         || ConnectionInfo.Length > 0 && ConnectionInfo != "Disconnected";

    public bool HasFingerprintReader { get; set; }

    public delegate void IdentificationHandler(LiteNet2BoardBase liteNet2Board, Identification identification);

    public delegate void StatusHandler(LiteNet2BoardBase liteNet2Board, string status);

    public event Action<LiteNet2Response>? OnResponse;
    public event IdentificationHandler? OnIdentification;
    public event Action<LiteNet2BoardBase, BoardConnectionStatus>? OnConnectionStatusChanged;
    public event StatusHandler? OnStatus;
    public event Action<LiteNet2BoardBase, LiteNet2Send>? OnSend;

    private TcpClient? _tcpClient;

    public bool Connected => _tcpClient?.Client != null && _tcpClient.Connected;

    public LiteNet2BoardBase(IPAddress ip, int? id = null, string connectionInfo = "")
    {
        Ip = ip;
        if (id.HasValue) Id = id.Value;
        ConnectionInfo = connectionInfo == "None" ? "Disconnected" : connectionInfo;
    }

    public override string ToString() => $"LiteNet2 #{Id} {Ip}:{Port} {ConnectionInfo}";

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
        var send = new LiteNet2Send(liteNet2Command, parameter);

        Send(send);
    }

    public void Send(ushort comando, byte[]? parameter = null)
    {
        var send = new LiteNet2Send(comando, parameter);

        Send(send);
    }

    public void Send(LiteNet2Send liteNet2Send)
    {
        OnSend?.Invoke(this, liteNet2Send);

        if (!Connected)
        {
            TryReconnect();
            return;
        }

        var stream = _tcpClient!.GetStream();

        try
        {
            stream.Write(liteNet2Send.Payload, 0, liteNet2Send.Payload.Length);
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

                OnStatus?.Invoke(this, "Reconnecting");

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

    protected void EventStatus(string status) => OnStatus?.Invoke(this, status);
}