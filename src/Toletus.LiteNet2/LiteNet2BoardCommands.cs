using System.Net;
using System.Text;
using Toletus.Extensions;
using Toletus.LiteNet2.Base.Utils;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2
{
    public partial class LiteNet2Board
    {
        public void ReleaseEntry(string message)
        {
            Send(Commands.ReleaseEntry, message.RemoveDiacritics());
        }

        public void ReleaseExit(string message)
        {
            Send(Commands.ReleaseExit, message.RemoveDiacritics());
        }

        public void SetEntryClockwise(bool entryClockwise)
        {
            Send(Commands.SetEntryClockwise, entryClockwise ? 0x01 : 0x00);
        }

        public void SetBuzzerMute(bool on)
        {
            Send(Commands.SetBuzzerMute, on ? 1 : 0);
        }

        public void SetFlowControl(ControlledFlow controlledFlow)
        {
            Send(Commands.SetFlowControl, (int)controlledFlow);
        }

        public void SetFlowControlExtended(ControlledFlowExtended controlledFlowExtended)
        {
            Send(Commands.SetFlowControlExtended, (int)controlledFlowExtended);
        }

        public void GetMac()
        {
            Send(Commands.GetMac);
        }

        public void SetId(int id)
        {
            Id = id;
            Send(Commands.SetId, id);
        }

        public void SetFingerprintIdentificationMode(FingerprintIdentificationMode fingerprintIdentificationMode)
        {
            Send(Commands.SetFingerprintIdentificationMode, (int)fingerprintIdentificationMode);
        }

        public void GetEntryClockwise()
        {
            Send(Commands.GetEntryClockwise);
        }

        public void GetFlowControl()
        {
            Send(Commands.GetFlowControl);
        }

        public void GetFlowControlExtended()
        {
            Send(Commands.GetFlowControlExtended);
        }

        public void GetId()
        {
            Send(Commands.GetId);
        }

        public void GetMessageLine1()
        {
            Send(Commands.GetMessageLine1);
        }

        public void GetMessageLine2()
        {
            Send(Commands.GetMessageLine2);
        }

        public void GetBuzzerMute()
        {
            Send(Commands.GetBuzzerMute);
        }

        public void GetReleaseDuration()
        {
            Send(Commands.GetReleaseDuration);
        }

        public void GetMenuPassword()
        {
            Send(Commands.GetMenuPassword);
        }

        public void GetFirmwareVersion()
        {
            Send(Commands.GetFirmwareVersion);
        }

        public void GetSerialNumber()
        {
            Send(Commands.GetSerialNumber);
        }

        public void GetFingerprintIdentificationMode()
        {
            Send(Commands.GetFingerprintIdentificationMode);
        }

        public void GetIpMode()
        {
            Send(Commands.GetIpMode);
        }

        public void SetMessageLine1(string msg1)
        {
            Send(Commands.SetMessageLine1, msg1.RemoveDiacritics());
        }

        public void SetMessageLine2(string msg2)
        {
            Send(Commands.SetMessageLine2, msg2.RemoveDiacritics());
        }

        public void SetReleaseDuration(int releaseDuration)
        {
            Send(Commands.SetReleaseDuration, releaseDuration);
        }

        public void ResetCounters()
        {
            Send(Commands.ResetCounters);
        }

        public void Reset()
        {
            Send(Commands.Reset);
        }

        public void SetIp(bool dhcp, IPAddress ip, IPAddress subnetMask)
        {
            if (Connected) Close();

            var confIp = dhcp ? "dhcp" : $"{ip} {subnetMask}";
            var content = $"{Id} ip {confIp}";

            UdpUtil.Send(NetworkIp, 7878, content);
        }

        public void SetMenuPassword(string password)
        {
            SetMenuPassword(Encoding.ASCII.GetBytes(password));
        }

        public void SetMenuPassword(byte[] password)
        {
            Send(Commands.SetMenuPassword, password);
        }

        public void SetShowCounters(bool showCounters)
        {
            Send(Commands.SetShowCounters, showCounters ? 1 : 0);
        }

        public void GetShowCounters()
        {
            Send(Commands.GetShowCounters);
        }
    }
}