using System;
using System.Linq;
using System.Net;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.LiteNet2.Enums;
using Toletus.Pack.Core;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    private void LiteNet2_OnResponse(LiteNetResponse liteNetResponse)
    {
        Log?.Invoke($"LiteNet < {liteNetResponse}");

        switch (liteNetResponse.Command)
        {
            case LiteNet2Commands.GetFirmwareVersion:
                ProcessFirmwareVersionResponse(liteNetResponse);
                break;
            case LiteNet2Commands.GetMac:
                ProcessMacResponse(liteNetResponse);
                break;
            case LiteNet2Commands.Gyre:
            case LiteNet2Commands.GyreTimeout:
                ProcessGyreResponse(liteNetResponse);
                break;
            case LiteNet2Commands.GetIpMode:
                ProcessIpModeResponse(liteNetResponse);
                break;
            case LiteNet2Commands.GetBuzzerMute:
                ProcessBuzzerMute(liteNetResponse);
                break;
            case LiteNet2Commands.GetFlowControl:
                ProcessFlowControl(liteNetResponse);
                break;
            case LiteNet2Commands.GetFlowControlExtended:
                ProcessFlowControlExtended(liteNetResponse);
                break;
            case LiteNet2Commands.GetEntryClockwise:
                ProcessEntryClockwise(liteNetResponse);
                break;
            case LiteNet2Commands.GetId:
                ProcessGetId(liteNetResponse);
                break;
            case LiteNet2Commands.GetMessageLine1:
                ProcessMessageLine1(liteNetResponse);
                break;
            case LiteNet2Commands.GetMessageLine2:
                ProcessMessageLine2(liteNetResponse);
                break;
            case LiteNet2Commands.GetSerialNumber:
                ProcessSerialNumber(liteNetResponse);
                break;
            case LiteNet2Commands.GetFingerprintIdentificationMode:
                ProcessFingerprintIdentificationMode(liteNetResponse);
                break;
            case LiteNet2Commands.GetShowCounters:
                ProcessShowCounters(liteNetResponse);
                break;
            case LiteNet2Commands.GetReleaseDuration:
                ProcessReleaseDuration(liteNetResponse);
                break;
            case LiteNet2Commands.GetMenuPassword:
                ProcessMenuPassword(liteNetResponse);
                break;
        }

        OnResponse?.Invoke(this, liteNetResponse);
    }

    private void ProcessMenuPassword(LiteNetResponse liteNetResponse)
    {
        MenuPassword = liteNetResponse.DataString;
    }

    private void ProcessReleaseDuration(LiteNetResponse liteNetResponse)
    {
        ReleaseDuration = BitConverter.ToInt32(liteNetResponse.RawData, 0) / 1000;
    }

    private void ProcessShowCounters(LiteNetResponse liteNetResponse)
    {
        ShowCounters = liteNetResponse.Data == 1;
    }

    private void ProcessFingerprintIdentificationMode(LiteNetResponse liteNetResponse)
    {
        FingerprintIdentificationMode = (FingerprintIdentificationMode)liteNetResponse.RawData[0];
    }

    private void ProcessSerialNumber(LiteNetResponse liteNetResponse)
    {
        SerialNumber = BitConverter.ToInt32(liteNetResponse.RawData, 0).ToString();
    }


    private void ProcessMessageLine2(LiteNetResponse liteNetResponse)
    {
        MessageLine2 = liteNetResponse.DataString;
        return;
    }


    private void ProcessMessageLine1(LiteNetResponse liteNetResponse)
    {
        MessageLine1 = liteNetResponse.DataString;
    }


    private void ProcessGetId(LiteNetResponse liteNetResponse)
    {
        Id = liteNetResponse.RawData[0];
    }

    private void ProcessEntryClockwise(LiteNetResponse liteNetResponse)
    {
        EntryClockwise = liteNetResponse.Data == 1;
    }

    private void ProcessFlowControl(LiteNetResponse liteNetResponse)
    {
        ControlledFlow = (ControlledFlow)liteNetResponse.RawData[0];
    }

    private void ProcessFlowControlExtended(LiteNetResponse liteNetResponse)
    {
        ControlledFlowExtended = (ControlledFlowExtended)liteNetResponse.RawData[0];
    }

    private void ProcessBuzzerMute(LiteNetResponse liteNetResponse)
    {
        BuzzerMute = liteNetResponse.Data == 1;
    }

    private void ProcessFirmwareVersionResponse(LiteNetResponse liteNetResponse)
    {
        FirmwareVersion = $"{string.Join(".", liteNetResponse.RawData.Take(3))}" + $" R{liteNetResponse.RawData[3]}";
    }

    private void ProcessIpModeResponse(LiteNetResponse liteNetResponse)
    {
        if (IpConfig == null) return;
        
        IpConfig.IpMode = (IpMode)liteNetResponse.RawData[0];
        IpConfig.FixedIp = new IPAddress(liteNetResponse.RawData.Skip(1).Take(4).Reverse().ToArray());
        IpConfig.SubnetMask = new IPAddress(liteNetResponse.RawData.Skip(5).Take(4).Reverse().ToArray());
    }

    private void ProcessGyreResponse(LiteNetResponse liteNetResponse)
    {
        if (liteNetResponse.Command == LiteNet2Commands.Gyre)
        {
            if (liteNetResponse.RawData[0] == 1)
                OnGyre?.Invoke(this, Direction.Entry);
            else if (liteNetResponse.RawData[0] == 2)
                OnGyre?.Invoke(this, Direction.Exit);
        }
        else if (liteNetResponse.Command == LiteNet2Commands.GyreTimeout)
            OnGyre?.Invoke(this, Direction.None);
    }

    private void ProcessMacResponse(LiteNetResponse liteNetResponse)
    {
        Mac = liteNetResponse.RawData.Take(6).ToArray().ToHexString(" ");

        if (string.IsNullOrEmpty(Description)) Description = Mac;
    }
}