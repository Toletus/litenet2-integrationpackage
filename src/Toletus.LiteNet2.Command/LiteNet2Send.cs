using System;
using Toletus.LiteNet2.Command.Enums;
using Toletus.Pack.Core;
using Toletus.Pack.Core.Extensions;

namespace Toletus.LiteNet2.Command;

public class LiteNet2Send
{
    private const byte Prefix = 0x53;
    private const byte Suffix = 0xc3;

    public LiteNet2Send(LiteNet2Commands liteNet2Command, byte[]? parameter = null) : this((ushort)liteNet2Command, parameter)
    {
    }

    public LiteNet2Send(ushort comando, byte[]? parameter = null)
    {
        LiteNet2Command = (LiteNet2Commands)comando;
        Payload = GetPayload(comando, parameter);
    }

    public LiteNet2Commands LiteNet2Command { get; set; }
    public byte[] Payload { get; set; }

    private static byte[] GetPayload(ushort command, byte[]? parameter)
    {
        parameter ??= new byte[16];

        var payload = new byte[20];
        payload[0] = Prefix;
        payload[1] = (byte)command;
        payload[2] = (byte)(command >> 8);
        Array.Copy(parameter, 0, payload, 3, parameter.Length);
        payload[19] = Suffix;
        return payload;
    }

    public override string ToString()
    {
        return $"{Payload.ToHexString(" ")} {LiteNet2Command}";
    }
}