using System.ComponentModel;

namespace Toletus.LiteNet2.Enums;

public enum IpMode
{
    [Description("Dynamic (DHCP)")]
    Dynamic,
    [Description("Fixed")]
    Fixed
}