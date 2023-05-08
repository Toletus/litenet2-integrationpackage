namespace Toletus.LiteNet2.Command.Enums
{
    public enum Commands
    {
        ReleaseEntry = 0x0001,
        ReleaseExit = 0x0002,
        Reset = 0x0003,
        novoTempMessage = 0x0004,
        novoNotify = 0x0005,
        novoReleaseEntryAndExit = 0x0006,
        ResetPeripherals = 0x0007,
        ResetCounters = 0x0210,

        GetEntryClockwise = 0x0101,
        GetFlowControl = 0x0102,
        GetFlowControlExtended = 0x010f,
        GetId = 0x0103,
        GetIpMode = 0x0104,
        GetMac = 0x0105,
        GetMessageLine1 = 0x0106,
        GetMessageLine2 = 0x0107,
        GetBuzzerMute = 0x0109,
        GetFingerprintIdentificationMode = 0x010e,
        GetShowCounters = 0x0108,
        GetCounters,
        GetReleaseDuration = 0x010a,
        GetMenuPassword = 0x010b,
        GetFirmwareVersion = 0x010c,
        GetSerialNumber = 0x010d,

        SetEntryClockwise = 0x0201,
        SetFlowControl = 0x0202,
        SetFlowControlExtended = 0x020f,
        SetId = 0x0203,                    
        SetIp = 0x0204,                    
        SetMac = 0x0205,                   
        SetMessageLine1 = 0x0206,                  
        SetMessageLine2 = 0x0207,                  
        SetShowCounters = 0x0208,           
        SetBuzzerMute = 0x0209,
        SetFingerprintIdentificationMode = 0x020e,
        SetReleaseDuration = 0x020a,
        SetMenuPassword = 0x020b,

        IdentificationByRfId = 0x0301,
        IdentificationByBarCode = 0x0302,
        IdentificationByKeyboard = 0x0303,
        PositiveIdentificationByFingerprintReader = 0x0306,
        NegativeIdentificationByFingerprintReader = 0x0307,

        Gyre = 0x0304,
        GyreTimeout = 0x0305,
    }
}