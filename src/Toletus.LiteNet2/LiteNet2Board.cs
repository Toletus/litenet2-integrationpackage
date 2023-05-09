using System;
using System.Net;
using System.Threading;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.LiteNet2.Enums;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board : LiteNet2BoardBase
{
    public delegate void GyreHandler(LiteNet2Board liteNet2Board, Direction direction);
    public delegate void ResponseHandler(LiteNet2Board liteNet2Board, BoardResponseCommand boardResponseCommand);
    public delegate void SendHandler(LiteNet2Board liteNet2Board, BoardSendCommand boardSendCommand);

    public event Action<bool>? OnFingerprintReaderConnected;
    public event Action<string>? OnReady;
    public event GyreHandler? OnGyre;
    public new event SendHandler? OnSend;
    public new event ResponseHandler? OnResponse;

    public LiteNet2Board(IPAddress ip, int id) : base(ip, id)
    {
        IpConfig = new IpConfig();
        OnConnectionStatusChanged += LiteNetOnConnectionStatusChanged;
        base.OnResponse += LiteNet2_OnResponse;
        base.OnSend += LiteNetOnSend;
    }

    private void LiteNetOnSend(LiteNet2BoardBase liteNet2BoardBase, BoardSendCommand boardSend)
    {
        Log?.Invoke("LiteNet > " + boardSend);
        OnSend?.Invoke(this, boardSend);
    }

    private void LiteNetOnConnectionStatusChanged(LiteNet2BoardBase liteNet2BoardBase, BoardConnectionStatus boardConnectionStatus)
    {
        if (boardConnectionStatus == BoardConnectionStatus.Connected)
        {
            OnReady?.Invoke("LiteNet2 Ok");
            Log?.Invoke($"{liteNet2BoardBase} {boardConnectionStatus}");
            Send(LiteNet2Commands.GetFlowControlExtended);

            if (FingerprintReader == null)
                CreateFingerprintReaderAndTest();
        }
        else
            EventStatus(boardConnectionStatus.ToString());
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