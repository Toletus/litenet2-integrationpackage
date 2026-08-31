namespace Toletus.LiteNet2.Command;

/// <summary>
/// Resultado de Envio (TDD §4.4). Envio a equipamento desconectado ou com falha de
/// escrita retorna erro explícito — fim do descarte silencioso (AC-005.1).
/// </summary>
public sealed class SendResult
{
    public enum SendError
    {
        None,
        Disconnected,
        TimeoutExceeded,
        WriteFailure
    }

    private SendResult(bool success, SendError error, string? message)
    {
        Success = success;
        Error = error;
        Message = message;
    }

    public bool Success { get; }
    public SendError Error { get; }
    public string? Message { get; }

    public static SendResult Ok() => new(true, SendError.None, null);
    public static SendResult Fail(SendError error, string? message = null) => new(false, error, message);

    public override string ToString() => Success ? "Ok" : $"Fail({Error}: {Message})";
}
