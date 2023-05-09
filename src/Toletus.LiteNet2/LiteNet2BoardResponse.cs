﻿using System;
using System.Linq;
using System.Net;
using Toletus.Extensions;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Toletus.LiteNet2.Enums;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    private void LiteNet2_OnResponse(BoardResponseCommand boardResponse)
    {
        Log?.Invoke($"LiteNet < {boardResponse}");

        switch (boardResponse.Command)
        {
            case Commands.GetFirmwareVersion:
                ProcessFirmwareVersionResponse(boardResponse);
                break;
            case Commands.GetMac:
                ProcessMacResponse(boardResponse);
                break;
            case Commands.Gyre:
            case Commands.GyreTimeout:
                ProcessGyreResponse(boardResponse);
                break;
            case Commands.GetIpMode:
                ProcessIpModeResponse(boardResponse);
                break;
            case Commands.GetBuzzerMute:
                ProcessBuzzerMute(boardResponse);
                break;
            case Commands.GetFlowControl:
                ProcessFlowControl(boardResponse);
                break;
            case Commands.GetFlowControlExtended:
                ProcessFlowControlExtended(boardResponse);
                break;
            case Commands.GetEntryClockwise:
                ProcessEntryClockwise(boardResponse);
                break;
            case Commands.GetId:
                ProcessGetId(boardResponse);
                break;
            case Commands.GetMessageLine1:
                ProcessMessageLine1(boardResponse);
                break;
            case Commands.GetMessageLine2:
                ProcessMessageLine2(boardResponse);
                break;
            case Commands.GetSerialNumber:
                ProcessSerialNumber(boardResponse);
                break;
            case Commands.GetFingerprintIdentificationMode:
                ProcessFingerprintIdentificationMode(boardResponse);
                break;
            case Commands.GetShowCounters:
                ProcessShowCounters(boardResponse);
                break;
            case Commands.GetReleaseDuration:
                ProcessReleaseDuration(boardResponse);
                break;
            case Commands.GetMenuPassword:
                ProcessMenuPassword(boardResponse);
                break;
        }

        OnResponse?.Invoke(this, boardResponse);
    }

    private void ProcessMenuPassword(BoardResponseCommand boardResponse)
    {
        MenuPassword = boardResponse.DataString;
    }

    private void ProcessReleaseDuration(BoardResponseCommand boardResponse)
    {
        ReleaseDuration = BitConverter.ToInt32(boardResponse.RawData, 0) / 1000;
    }

    private void ProcessShowCounters(BoardResponseCommand boardResponse)
    {
        ShowCounters = boardResponse.Data == 1;
    }

    private void ProcessFingerprintIdentificationMode(BoardResponseCommand boardResponse)
    {
        FingerprintIdentificationMode = (FingerprintIdentificationMode)boardResponse.RawData[0];
    }

    private void ProcessSerialNumber(BoardResponseCommand boardResponse)
    {
        SerialNumber = BitConverter.ToInt32(boardResponse.RawData, 0).ToString();
    }


    private void ProcessMessageLine2(BoardResponseCommand boardResponse)
    {
        MessageLine2 = boardResponse.DataString;
        return;
    }


    private void ProcessMessageLine1(BoardResponseCommand boardResponse)
    {
        MessageLine1 = boardResponse.DataString;
    }


    private void ProcessGetId(BoardResponseCommand boardResponse)
    {
        Id = boardResponse.RawData[0];
    }

    private void ProcessEntryClockwise(BoardResponseCommand boardResponse)
    {
        EntryClockwise = boardResponse.Data == 1;
    }

    private void ProcessFlowControl(BoardResponseCommand boardResponse)
    {
        ControlledFlow = (ControlledFlow)boardResponse.RawData[0];
    }

    private void ProcessFlowControlExtended(BoardResponseCommand boardResponse)
    {
        ControlledFlowExtended = (ControlledFlowExtended)boardResponse.RawData[0];
    }

    private void ProcessBuzzerMute(BoardResponseCommand boardResponse)
    {
        BuzzerMute = boardResponse.Data == 1;
    }

    private void ProcessFirmwareVersionResponse(BoardResponseCommand boardResponse)
    {
        FirmwareVersion = $"{string.Join(".", boardResponse.RawData.Take(3))}" + $" R{boardResponse.RawData[3]}";
    }

    private void ProcessIpModeResponse(BoardResponseCommand boardResponse)
    {
        if (IpConfig == null) return;
        
        IpConfig.IpMode = (IpMode)boardResponse.RawData[0];
        IpConfig.FixedIp = new IPAddress(boardResponse.RawData.Skip(1).Take(4).Reverse().ToArray());
        IpConfig.SubnetMask = new IPAddress(boardResponse.RawData.Skip(5).Take(4).Reverse().ToArray());
    }

    private void ProcessGyreResponse(BoardResponseCommand boardResponse)
    {
        if (boardResponse.Command == Commands.Gyre)
        {
            if (boardResponse.RawData[0] == 1)
                OnGyre?.Invoke(this, Direction.Entry);
            else if (boardResponse.RawData[0] == 2)
                OnGyre?.Invoke(this, Direction.Exit);
        }
        else if (boardResponse.Command == Commands.GyreTimeout)
            OnGyre?.Invoke(this, Direction.None);
    }

    private void ProcessMacResponse(BoardResponseCommand boardResponse)
    {
        Mac = boardResponse.RawData.Take(6).ToArray().ToHexString(" ");

        if (string.IsNullOrEmpty(Description)) Description = Mac;
    }
}