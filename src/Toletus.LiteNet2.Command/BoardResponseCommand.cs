using System;
using System.Linq;
using Toletus.Extensions;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Command;

public class BoardResponseCommand
{
    public BoardResponseCommand(byte[] response)
    {
        /* Payload (20 bytes)
         *
         * /- Prefix (1 byte) (0x53)
         * |
         * |/- LiteNet2Command (2 bytes)
         * ||
         * || /- Data (16 bytes)
         * || |                         
         * || |               /- Suffix (1 byte) (0xc3)
         * || |               |
         * 01234567890123456789
        */

        Payload = response;
        LiteNet2Command = (LiteNet2Commands)BitConverter.ToUInt16(response, 1);
        RawData = response.Skip(3).Take(16).ToArray();
    }

    public byte[] Payload { get; set; }
    public LiteNet2Commands LiteNet2Command { get; }
    public byte[] RawData { get; }
    public ushort Data => BitConverter.ToUInt16(RawData, 0);
    public string DataString => RawData.SupressEndWithZeroBytes().ConvertToAsciiString().Trim();
    public Identification? Identification { get; set; }

    public override string ToString()
    {
        try
        {
            var ret = $"[{Payload.ToHexString(" ")}] {Data} {DataString} {LiteNet2Command}";

            return ret;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}