using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core;
using Toletus.SM25;

namespace Toletus.LiteNet2;

public partial class LiteNet2Board
{
    public object? Tag { get; set; }
    public string? Mac { get; set; }
    public string? Description { get; set; }
    public string? FirmwareVersion { get; set; }
    public IpConfig? IpConfig { get; set; }
    public SM25Reader? FingerprintReader { get; set; }
    public string? MenuPassword { get; set; }
    public int? ReleaseDuration { get; set; }
    public bool? ShowCounters { get; set; }
    public string? SerialNumber { get; set; }
    public string? MessageLine2 { get; set; }
    public string? MessageLine1 { get; set; }
    public bool? EntryClockwise { get; set; }
    public bool? BuzzerMute { get; set; }
    public FingerprintIdentificationMode FingerprintIdentificationMode { get; set; }
    public ControlledFlow ControlledFlow { get; set; }        
    public ControlledFlowExtended? ControlledFlowExtended { get; set; }
    public bool FirmwareWithControlledFlowExtended => ControlledFlowExtended.HasValue;
}