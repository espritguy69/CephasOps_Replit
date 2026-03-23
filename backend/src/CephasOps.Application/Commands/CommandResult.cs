namespace CephasOps.Application.Commands;

/// <summary>
/// Result of sending a command through the bus.
/// </summary>
public class CommandResult<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>True when the same result was returned due to idempotency (same IdempotencyKey).</summary>
    public bool IdempotencyReused { get; set; }
    /// <summary>Unique execution id for this send (or the original execution when IdempotencyReused).</summary>
    public Guid ExecutionId { get; set; }

    public static CommandResult<T> Ok(T result, Guid executionId, bool idempotencyReused = false) => new()
    {
        Success = true,
        Result = result,
        ExecutionId = executionId,
        IdempotencyReused = idempotencyReused
    };

    public static CommandResult<T> Fail(string errorMessage, Guid executionId) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ExecutionId = executionId
    };
}
