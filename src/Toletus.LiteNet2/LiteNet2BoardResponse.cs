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
    private void LiteNet2_OnResponse(ResponseCommand response)
    {
        Log?.Invoke($"LiteNet < {response}");

        switch (response.Command)
        {
            case Commands.GetFirmwareVersion:
                ProcessFirmwareVersionResponse(response);
                break;
            case Commands.GetMac:
                ProcessMacResponse(response);
                break;
            case Commands.Gyre:
            case Commands.GyreTimeout:
                ProcessGyreResponse(response);
                break;
            case Commands.GetIpMode:
                ProcessIpModeResponse(response);
                break;
            case Commands.GetBuzzerMute:
                ProcessBuzzerMute(response);
                break;
            case Commands.GetFlowControl:
                ProcessFlowControl(response);
                break;
            case Commands.GetFlowControlExtended:
                ProcessFlowControlExtended(response);
                break;
            case Commands.GetEntryClockwise:
                ProcessEntryClockwise(response);
                break;
            case Commands.GetId:
                ProcessGetId(response);
                break;
            case Commands.GetMessageLine1:
                ProcessMessageLine1(response);
                break;
            case Commands.GetMessageLine2:
                ProcessMessageLine2(response);
                break;
            case Commands.GetSerialNumber:
                ProcessSerialNumber(response);
                break;
            case Commands.GetFingerprintIdentificationMode:
                ProcessFingerprintIdentificationMode(response);
                break;
            case Commands.GetShowCounters:
                ProcessShowCounters(response);
                break;
            case Commands.GetReleaseDuration:
                ProcessReleaseDuration(response);
                break;
            case Commands.GetMenuPassword:
                ProcessMenuPassword(response);
                break;
        }

        OnResponse?.Invoke(this, response);
    }

    private void ProcessMenuPassword(ResponseCommand response)
    {
        MenuPassword = response.DataString;
    }

    private void ProcessReleaseDuration(ResponseCommand response)
    {
        ReleaseDuration = BitConverter.ToInt32(response.RawData, 0) / 1000;
    }

    private void ProcessShowCounters(ResponseCommand response)
    {
        ShowCounters = response.Data == 1;
    }

    private void ProcessFingerprintIdentificationMode(ResponseCommand response)
    {
        FingerprintIdentificationMode = (FingerprintIdentificationMode)response.RawData[0];
    }

    private void ProcessSerialNumber(ResponseCommand response)
    {
        SerialNumber = BitConverter.ToInt32(response.RawData, 0).ToString();
    }


    private void ProcessMessageLine2(ResponseCommand response)
    {
        MessageLine2 = response.DataString;
        return;
    }


    private void ProcessMessageLine1(ResponseCommand response)
    {
        MessageLine1 = response.DataString;
    }


    private void ProcessGetId(ResponseCommand response)
    {
        Id = response.RawData[0];
    }

    private void ProcessEntryClockwise(ResponseCommand response)
    {
        EntryClockwise = response.Data == 1;
    }

    private void ProcessFlowControl(ResponseCommand response)
    {
        ControlledFlow = (ControlledFlow)response.RawData[0];
    }

    private void ProcessFlowControlExtended(ResponseCommand response)
    {
        ControlledFlowExtended = (ControlledFlowExtended)response.RawData[0];
    }

    private void ProcessBuzzerMute(ResponseCommand response)
    {
        BuzzerMute = response.Data == 1;
    }

    private void ProcessFirmwareVersionResponse(ResponseCommand response)
    {
        FirmwareVersion = $"{string.Join(".", response.RawData.Take(3))}" + $" R{response.RawData[3]}";
    }

    private void ProcessIpModeResponse(ResponseCommand response)
    {
        IpConfig.IpMode = (IpMode)response.RawData[0];
        IpConfig.FixedIp = new IPAddress(response.RawData.Skip(1).Take(4).Reverse().ToArray());
        IpConfig.SubnetMask = new IPAddress(response.RawData.Skip(5).Take(4).Reverse().ToArray());
    }

    private void ProcessGyreResponse(ResponseCommand response)
    {
        if (response.Command == Commands.Gyre)
        {
            if (response.RawData[0] == 1)
                OnGyre?.Invoke(this, Direction.Entry);
            else if (response.RawData[0] == 2)
                OnGyre?.Invoke(this, Direction.Exit);
        }
        else if (response.Command == Commands.GyreTimeout)
            OnGyre?.Invoke(this, Direction.None);
    }

    private void ProcessMacResponse(ResponseCommand response)
    {
        Mac = response.RawData.Take(6).ToArray().ToHexString(" ");

        if (string.IsNullOrEmpty(Description)) Description = Mac;
    }
}