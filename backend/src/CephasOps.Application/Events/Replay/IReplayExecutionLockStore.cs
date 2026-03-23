namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Durable replay execution lock: at most one active replay per company.
/// Prevents concurrent replay jobs for the same company; supports stale lock reclaim via expiry.
/// </summary>
public interface IReplayExecutionLockStore
{
    /// <summary>
    /// Try to acquire the company replay lock for the given operation.
    /// If another replay is active for this company, returns false (operator should see explicit failure).
    /// If the existing lock is stale (expired), it is reclaimed and true is returned.
    /// </summary>
    /// <param name="companyId">Company scope. Caller must not pass null for company-scoped replay.</param>
    /// <param name="replayOperationId">Operation that will hold the lock.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>True if lock was acquired (or reclaimed); false if another replay is active.</returns>
    Task<bool> TryAcquireAsync(Guid companyId, Guid replayOperationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release the lock for this company and operation. Idempotent; safe to call multiple times.
    /// Must be called when replay completes, fails, or is cancelled (e.g. in finally).
    /// </summary>
    Task ReleaseAsync(Guid companyId, Guid replayOperationId, CancellationToken cancellationToken = default);
}
