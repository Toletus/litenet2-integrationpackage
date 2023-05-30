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
    private void LiteNet2_OnResponse(LiteNet2Response liteNet2Response)
    {
        Log?.Invoke($"LiteNet < {liteNet2Response}");

        switch (liteNet2Response.Command)
        {
            case LiteNet2Commands.GetFirmwareVersion:
                ProcessFirmwareVersionResponse(liteNet2Response);
                break;
            case LiteNet2Commands.GetMac:
                ProcessMacResponse(liteNet2Response);
                break;
            case LiteNet2Commands.Gyre:
            case LiteNet2Commands.GyreTimeout:
                ProcessGyreResponse(liteNet2Response);
                break;
            case LiteNet2Commands.GetIpMode:
                ProcessIpModeResponse(liteNet2Response);
                break;
            case LiteNet2Commands.GetBuzzerMute:
                ProcessBuzzerMute(liteNet2Response);
                break;
            case LiteNet2Commands.GetFlowControl:
                ProcessFlowControl(liteNet2Response);
                break;
            case LiteNet2Commands.GetFlowControlExtended:
                ProcessFlowControlExtended(liteNet2Response);
                break;
            case LiteNet2Commands.GetEntryClockwise:
                ProcessEntryClockwise(liteNet2Response);
                break;
            case LiteNet2Commands.GetId:
                ProcessGetId(liteNet2Response);
                break;
            case LiteNet2Commands.GetMessageLine1:
                ProcessMessageLine1(liteNet2Response);
                break;
            case LiteNet2Commands.GetMessageLine2:
                ProcessMessageLine2(liteNet2Response);
                break;
            case LiteNet2Commands.GetSerialNumber:
                ProcessSerialNumber(liteNet2Response);
                break;
            case LiteNet2Commands.GetFingerprintIdentificationMode:
                ProcessFingerprintIdentificationMode(liteNet2Response);
                break;
            case LiteNet2Commands.GetShowCounters:
                ProcessShowCounters(liteNet2Response);
                break;
            case LiteNet2Commands.GetReleaseDuration:
                ProcessReleaseDuration(liteNet2Response);
                break;
            case LiteNet2Commands.GetMenuPassword:
                ProcessMenuPassword(liteNet2Response);
                break;
        }

        OnResponse?.Invoke(this, liteNet2Response);
    }

    private void ProcessMenuPassword(LiteNet2Response liteNet2Response)
    {
        MenuPassword = liteNet2Response.DataString;
    }

    private void ProcessReleaseDuration(LiteNet2Response liteNet2Response)
    {
        ReleaseDuration = BitConverter.ToInt32(liteNet2Response.RawData, 0) / 1000;
    }

    private void ProcessShowCounters(LiteNet2Response liteNet2Response)
    {
        ShowCounters = liteNet2Response.Data == 1;
    }

    private void ProcessFingerprintIdentificationMode(LiteNet2Response liteNet2Response)
    {
        FingerprintIdentificationMode = (FingerprintIdentificationMode)liteNet2Response.RawData[0];
    }

    private void ProcessSerialNumber(LiteNet2Response liteNet2Response)
    {
        SerialNumber = BitConverter.ToInt32(liteNet2Response.RawData, 0).ToString();
    }


    private void ProcessMessageLine2(LiteNet2Response liteNet2Response)
    {
        MessageLine2 = liteNet2Response.DataString;
        return;
    }


    private void ProcessMessageLine1(LiteNet2Response liteNet2Response)
    {
        MessageLine1 = liteNet2Response.DataString;
    }


    private void ProcessGetId(LiteNet2Response liteNet2Response)
    {
        Id = liteNet2Response.RawData[0];
    }

    private void ProcessEntryClockwise(LiteNet2Response liteNet2Response)
    {
        EntryClockwise = liteNet2Response.Data == 1;
    }

    private void ProcessFlowControl(LiteNet2Response liteNet2Response)
    {
        ControlledFlow = (ControlledFlow)liteNet2Response.RawData[0];
    }

    private void ProcessFlowControlExtended(LiteNet2Response liteNet2Response)
    {
        ControlledFlowExtended = (ControlledFlowExtended)liteNet2Response.RawData[0];
    }

    private void ProcessBuzzerMute(LiteNet2Response liteNet2Response)
    {
        BuzzerMute = liteNet2Response.Data == 1;
    }

    private void ProcessFirmwareVersionResponse(LiteNet2Response liteNet2Response)
    {
        FirmwareVersion = $"{string.Join(".", liteNet2Response.RawData.Take(3))}" + $" R{liteNet2Response.RawData[3]}";
    }

    private void ProcessIpModeResponse(LiteNet2Response liteNet2Response)
    {
        if (IpConfig == null) return;
        
        IpConfig.IpMode = (IpMode)liteNet2Response.RawData[0];
        IpConfig.FixedIp = new IPAddress(liteNet2Response.RawData.Skip(1).Take(4).Reverse().ToArray());
        IpConfig.SubnetMask = new IPAddress(liteNet2Response.RawData.Skip(5).Take(4).Reverse().ToArray());
    }

    private void ProcessGyreResponse(LiteNet2Response liteNet2Response)
    {
        if (liteNet2Response.Command == LiteNet2Commands.Gyre)
        {
            if (liteNet2Response.RawData[0] == 1)
                OnGyre?.Invoke(this, Direction.Entry);
            else if (liteNet2Response.RawData[0] == 2)
                OnGyre?.Invoke(this, Direction.Exit);
        }
        else if (liteNet2Response.Command == LiteNet2Commands.GyreTimeout)
            OnGyre?.Invoke(this, Direction.None);
    }

    private void ProcessMacResponse(LiteNet2Response liteNet2Response)
    {
        Mac = liteNet2Response.RawData.Take(6).ToArray().ToHexString(" ");

        if (string.IsNullOrEmpty(Description)) Description = Mac;
    }
}