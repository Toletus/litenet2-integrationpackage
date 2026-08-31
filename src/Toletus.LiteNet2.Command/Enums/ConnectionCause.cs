namespace Toletus.LiteNet2.Command.Enums;

/// <summary>
/// Por que a conexão com a catraca mudou de estado. Vai ao log (AC-004.2).
/// </summary>
public enum ConnectionCause
{
    None,
    RemoteClose,
    KeepAliveExpired,
    SendError,
    ReceiveError,
    LocalClose
}
