namespace CephasOps.Application.Commands;

/// <summary>
/// Idempotency store for command execution: one successful completion per idempotency key.
/// </summary>
public interface ICommandProcessingLogStore
{
    /// <summary>
    /// Try to claim execution for this command. Returns true if claimed (caller must run handler then MarkCompletedAsync or MarkFailedAsync).
    /// Returns false if already completed or in progress (caller may call TryGetCompletedResultAsync for reuse).
    /// </summary>
    Task<bool> TryClaimAsync(
        Guid commandId,
        string idempotencyKey,
        string commandType,
        string? correlationId,
        Guid? workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the command as completed with result. Call after TryClaimAsync returned true and handler succeeded.
    /// </summary>
    Task MarkCompletedAsync(Guid commandId, string resultJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the command as failed. Call after TryClaimAsync returned true and handler threw.
    /// </summary>
    Task MarkFailedAsync(Guid commandId, string? errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get completed result for an idempotency key (for returning cached result when TryClaimAsync returned false).
    /// </summary>
    Task<CommandProcessingLogResult?> TryGetCompletedResultAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cached result for idempotency reuse.
/// </summary>
public class CommandProcessingLogResult
{
    public Guid ExecutionId { get; set; }
    public string? ResultJson { get; set; }
}
