using System;
using System.Linq;
using System.Net;
using Toletus.Extensions;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.LiteNet2.Enums;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    private void LiteNet2_OnResponse(BoardResponse boardResponse)
    {
        Log?.Invoke($"LiteNet < {boardResponse}");

        switch (boardResponse.Command)
        {
            case LiteNet2Commands.GetFirmwareVersion:
                ProcessFirmwareVersionResponse(boardResponse);
                break;
            case LiteNet2Commands.GetMac:
                ProcessMacResponse(boardResponse);
                break;
            case LiteNet2Commands.Gyre:
            case LiteNet2Commands.GyreTimeout:
                ProcessGyreResponse(boardResponse);
                break;
            case LiteNet2Commands.GetIpMode:
                ProcessIpModeResponse(boardResponse);
                break;
            case LiteNet2Commands.GetBuzzerMute:
                ProcessBuzzerMute(boardResponse);
                break;
            case LiteNet2Commands.GetFlowControl:
                ProcessFlowControl(boardResponse);
                break;
            case LiteNet2Commands.GetFlowControlExtended:
                ProcessFlowControlExtended(boardResponse);
                break;
            case LiteNet2Commands.GetEntryClockwise:
                ProcessEntryClockwise(boardResponse);
                break;
            case LiteNet2Commands.GetId:
                ProcessGetId(boardResponse);
                break;
            case LiteNet2Commands.GetMessageLine1:
                ProcessMessageLine1(boardResponse);
                break;
            case LiteNet2Commands.GetMessageLine2:
                ProcessMessageLine2(boardResponse);
                break;
            case LiteNet2Commands.GetSerialNumber:
                ProcessSerialNumber(boardResponse);
                break;
            case LiteNet2Commands.GetFingerprintIdentificationMode:
                ProcessFingerprintIdentificationMode(boardResponse);
                break;
            case LiteNet2Commands.GetShowCounters:
                ProcessShowCounters(boardResponse);
                break;
            case LiteNet2Commands.GetReleaseDuration:
                ProcessReleaseDuration(boardResponse);
                break;
            case LiteNet2Commands.GetMenuPassword:
                ProcessMenuPassword(boardResponse);
                break;
        }

        OnResponse?.Invoke(this, boardResponse);
    }

    private void ProcessMenuPassword(BoardResponse boardResponse)
    {
        MenuPassword = boardResponse.DataString;
    }

    private void ProcessReleaseDuration(BoardResponse boardResponse)
    {
        ReleaseDuration = BitConverter.ToInt32(boardResponse.RawData, 0) / 1000;
    }

    private void ProcessShowCounters(BoardResponse boardResponse)
    {
        ShowCounters = boardResponse.Data == 1;
    }

    private void ProcessFingerprintIdentificationMode(BoardResponse boardResponse)
    {
        FingerprintIdentificationMode = (FingerprintIdentificationMode)boardResponse.RawData[0];
    }

    private void ProcessSerialNumber(BoardResponse boardResponse)
    {
        SerialNumber = BitConverter.ToInt32(boardResponse.RawData, 0).ToString();
    }


    private void ProcessMessageLine2(BoardResponse boardResponse)
    {
        MessageLine2 = boardResponse.DataString;
        return;
    }


    private void ProcessMessageLine1(BoardResponse boardResponse)
    {
        MessageLine1 = boardResponse.DataString;
    }


    private void ProcessGetId(BoardResponse boardResponse)
    {
        Id = boardResponse.RawData[0];
    }

    private void ProcessEntryClockwise(BoardResponse boardResponse)
    {
        EntryClockwise = boardResponse.Data == 1;
    }

    private void ProcessFlowControl(BoardResponse boardResponse)
    {
        ControlledFlow = (ControlledFlow)boardResponse.RawData[0];
    }

    private void ProcessFlowControlExtended(BoardResponse boardResponse)
    {
        ControlledFlowExtended = (ControlledFlowExtended)boardResponse.RawData[0];
    }

    private void ProcessBuzzerMute(BoardResponse boardResponse)
    {
        BuzzerMute = boardResponse.Data == 1;
    }

    private void ProcessFirmwareVersionResponse(BoardResponse boardResponse)
    {
        FirmwareVersion = $"{string.Join(".", boardResponse.RawData.Take(3))}" + $" R{boardResponse.RawData[3]}";
    }

    private void ProcessIpModeResponse(BoardResponse boardResponse)
    {
        if (IpConfig == null) return;
        
        IpConfig.IpMode = (IpMode)boardResponse.RawData[0];
        IpConfig.FixedIp = new IPAddress(boardResponse.RawData.Skip(1).Take(4).Reverse().ToArray());
        IpConfig.SubnetMask = new IPAddress(boardResponse.RawData.Skip(5).Take(4).Reverse().ToArray());
    }

    private void ProcessGyreResponse(BoardResponse boardResponse)
    {
        if (boardResponse.Command == LiteNet2Commands.Gyre)
        {
            if (boardResponse.RawData[0] == 1)
                OnGyre?.Invoke(this, Direction.Entry);
            else if (boardResponse.RawData[0] == 2)
                OnGyre?.Invoke(this, Direction.Exit);
        }
        else if (boardResponse.Command == LiteNet2Commands.GyreTimeout)
            OnGyre?.Invoke(this, Direction.None);
    }

    private void ProcessMacResponse(BoardResponse boardResponse)
    {
        Mac = boardResponse.RawData.Take(6).ToArray().ToHexString(" ");

        if (string.IsNullOrEmpty(Description)) Description = Mac;
    }
}