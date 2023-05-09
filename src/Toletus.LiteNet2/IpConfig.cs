using System.Net;
using Toletus.LiteNet2.Enums;

namespace Toletus.LiteNet2;

public class IpConfig
{
    public IpMode? IpMode { get; set; }
    public IPAddress? FixedIp { get; set; }
    public IPAddress? SubnetMask { get; set; }
}