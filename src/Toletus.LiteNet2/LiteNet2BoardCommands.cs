using System.Net;
using System.Text;
using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core.Extensions;
using Toletus.Pack.Core.Utils;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    public void ReleaseEntry(string message)
    {
        Send(LiteNet2Commands.ReleaseEntry, message.RemoveDiacritics());
    }

    public void ReleaseExit(string message)
    {
        Send(LiteNet2Commands.ReleaseExit, message.RemoveDiacritics());
    }

    public void SetEntryClockwise(bool entryClockwise)
    {
        Send(LiteNet2Commands.SetEntryClockwise, entryClockwise ? 0x01 : 0x00);
    }

    public void SetBuzzerMute(bool on)
    {
        Send(LiteNet2Commands.SetBuzzerMute, on ? 1 : 0);
    }

    public void SetFlowControl(ControlledFlow controlledFlow)
    {
        Send(LiteNet2Commands.SetFlowControl, (int)controlledFlow);
    }

    public void SetFlowControlExtended(ControlledFlowExtended controlledFlowExtended)
    {
        Send(LiteNet2Commands.SetFlowControlExtended, (int)controlledFlowExtended);
    }

    public void GetMac()
    {
        Send(LiteNet2Commands.GetMac);
    }

    public void SetId(int id)
    {
        Id = id;
        Send(LiteNet2Commands.SetId, id);
    }

    public void SetFingerprintIdentificationMode(FingerprintIdentificationMode fingerprintIdentificationMode)
    {
        Send(LiteNet2Commands.SetFingerprintIdentificationMode, (int)fingerprintIdentificationMode);
    }

    public void GetEntryClockwise()
    {
        Send(LiteNet2Commands.GetEntryClockwise);
    }

    public void GetFlowControl()
    {
        Send(LiteNet2Commands.GetFlowControl);
    }

    public void GetFlowControlExtended()
    {
        Send(LiteNet2Commands.GetFlowControlExtended);
    }

    public void GetId()
    {
        Send(LiteNet2Commands.GetId);
    }

    public void GetMessageLine1()
    {
        Send(LiteNet2Commands.GetMessageLine1);
    }

    public void GetMessageLine2()
    {
        Send(LiteNet2Commands.GetMessageLine2);
    }

    public void GetBuzzerMute()
    {
        Send(LiteNet2Commands.GetBuzzerMute);
    }

    public void GetReleaseDuration()
    {
        Send(LiteNet2Commands.GetReleaseDuration);
    }

    public void GetMenuPassword()
    {
        Send(LiteNet2Commands.GetMenuPassword);
    }

    public void GetFirmwareVersion()
    {
        Send(LiteNet2Commands.GetFirmwareVersion);
    }

    public void GetSerialNumber()
    {
        Send(LiteNet2Commands.GetSerialNumber);
    }

    public void GetFingerprintIdentificationMode()
    {
        Send(LiteNet2Commands.GetFingerprintIdentificationMode);
    }

    public void GetIpMode()
    {
        Send(LiteNet2Commands.GetIpMode);
    }

    public void SetMessageLine1(string msg1)
    {
        Send(LiteNet2Commands.SetMessageLine1, msg1.RemoveDiacritics());
    }

    public void SetMessageLine2(string msg2)
    {
        Send(LiteNet2Commands.SetMessageLine2, msg2.RemoveDiacritics());
    }

    public void SetReleaseDuration(int releaseDuration)
    {
        Send(LiteNet2Commands.SetReleaseDuration, releaseDuration);
    }

    public void ResetCounters()
    {
        Send(LiteNet2Commands.ResetCounters);
    }

    public void Reset()
    {
        Send(LiteNet2Commands.Reset);
    }

    public void SetIp(bool dhcp, IPAddress ip, IPAddress subnetMask)
    {
        if (Connected) Close();

        var confIp = dhcp ? "dhcp" : $"{ip} {subnetMask}";
        var content = $"{Id} ip {confIp}";

        UdpUtils.Send(NetworkIp!, 7878, content);
    }

    public void SetMenuPassword(string password)
    {
        SetMenuPassword(Encoding.ASCII.GetBytes(password));
    }

    public void SetMenuPassword(byte[] password)
    {
        Send(LiteNet2Commands.SetMenuPassword, password);
    }

    public void SetShowCounters(bool showCounters)
    {
        Send(LiteNet2Commands.SetShowCounters, showCounters ? 1 : 0);
    }

    public void GetShowCounters()
    {
        Send(LiteNet2Commands.GetShowCounters);
    }
}