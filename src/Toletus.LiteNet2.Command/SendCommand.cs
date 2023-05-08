using System;
using Toletus.Extensions;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Command
{
    public class SendCommand
    {
        private const byte Prefix = 0x53;
        private const byte Suffix = 0xc3;

        public SendCommand(Commands command, byte[] parameter = null) : this((ushort)command, parameter)
        { }

        public SendCommand(ushort comando, byte[] parameter = null)
        {
            Command = (Commands)comando;
            Payload = GetPayload(comando, parameter);
        }

        public Commands Command { get; set; }
        public byte[] Payload { get; set; }

        private static byte[] GetPayload(ushort command, byte[] parameter)
        {
            /* Payload (20 bytes)
             *
             * /- Prefix (1 byte) (0x53)
             * |
             * |/- Command (2 bytes)
             * ||
             * || /- Parameters (16 bytes)
             * || |                         
             * || |               /- Suffix (1 byte) (0xc3)
             * || |               |
             * 01234567890123456789
             * 
            */

            if (parameter == null) parameter = new byte[16];

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
            return $"{Payload.ToHexString(" ")} {Command}";
        }
    }
}