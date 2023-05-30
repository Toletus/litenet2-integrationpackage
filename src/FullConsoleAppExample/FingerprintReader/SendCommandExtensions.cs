using System;
using System.Net.Sockets;
using Toletus.SM25.Command;

namespace FullConsoleAppExample.FingerprintReader
{
    internal static class SendCommandExtensions
    {
        public static void Send(this SM25Send sendCommand, TcpClient client)
        {
            var stream = client?.GetStream();
            stream?.Write(sendCommand.Payload, 0, sendCommand.Payload.Length);
            Console.Write($"Fingerprint Reader Sent:{Environment.NewLine}{sendCommand}");
        }
    }
}
