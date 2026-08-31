using System;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Command;

public sealed class ConnectionStateChange
{
    public ConnectionStateChange(BoardConnectionStatus status, ConnectionCause cause)
    {
        Status = status;
        Cause = cause;
        Timestamp = DateTime.Now;
    }

    public BoardConnectionStatus Status { get; }
    public ConnectionCause Cause { get; }
    public DateTime Timestamp { get; }

    public override string ToString() => $"{Status} ({Cause}) @ {Timestamp:HH:mm:ss.fff}";
}
