using System;
using System.Net;
using System.Threading;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Enums;
using Commands = Toletus.LiteNet2.Command.Enums.Commands;
using ConnectionStatus = Toletus.LiteNet2.Command.Enums.ConnectionStatus;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board : LiteNet2BoardBase
{
    public delegate void GyreHandler(LiteNet2Board liteNet2Board, Direction direction);
    public delegate void ResponseHandler(LiteNet2Board liteNet2Board, ResponseCommand responseCommand);
    public delegate void SendHandler(LiteNet2Board liteNet2Board, SendCommand sendCommand);

    public event Action<bool> OnFingerprintReaderConnected;
    public event Action<string> OnReady;
    public event GyreHandler OnGyre;
    public new event SendHandler OnSend;
    public new event ResponseHandler OnResponse;

    public LiteNet2Board(IPAddress ip, int id) : base(ip, id)
    {
        IpConfig = new IpConfig();
        OnConnectionStatusChanged += LiteNetOnConnectionStatusChanged;
        base.OnResponse += LiteNet2_OnResponse;
        base.OnSend += LiteNetOnSend;
    }

    private void LiteNetOnSend(LiteNet2BoardBase liteNet2BoardBase, SendCommand send)
    {
        Log?.Invoke("LiteNet > " + send);
        OnSend?.Invoke(this, send);
    }

    private void LiteNetOnConnectionStatusChanged(LiteNet2BoardBase liteNet2BoardBase, ConnectionStatus connectionStatus)
    {
        if (connectionStatus == ConnectionStatus.Connected)
        {
            OnReady?.Invoke("LiteNet2 Ok");
            Log?.Invoke($"{liteNet2BoardBase} {connectionStatus}");
            Send(Commands.GetFlowControlExtended);

            if (FingerprintReader == null)
                CreateFingerprintReaderAndTest();
        }
        else
        {
            EventStatus(connectionStatus.ToString());
        }
    }

    public new void Close()
    {
        base.Close();
        FingerprintReader?.Close();
    }

    public static LiteNet2Board CopyToLiteNet2(LiteNet2BoardBase liteNet2BoardBase)
    {
        return new LiteNet2Board(liteNet2BoardBase.Ip, liteNet2BoardBase.Id);
    }

    public override string ToString()
    {
        return $"{base.ToString()}" + (HasFingerprintReader ? " Bio" : "") + $" {Description}";
    }

    public void WaitForFingerprintReader()
    {
        var c = 0;
        while (FingerprintReader == null & c++ < 20)
            Thread.Sleep(100);
    }
}