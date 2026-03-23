namespace CephasOps.Domain.Events;

/// <summary>
/// Durable lock ensuring only one operational replay runs per company at a time.
/// One active lock per company (ReleasedAtUtc IS NULL). Stale locks can be reclaimed when ExpiresAtUtc has passed.
/// </summary>
public class ReplayExecutionLock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Company scope for the replay. Null is not used for locking (global replays are not locked).</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Replay operation that holds the lock.</summary>
    public Guid ReplayOperationId { get; set; }

    public DateTime AcquiredAtUtc { get; set; }
    /// <summary>When the lock is considered stale if not released (e.g. worker crash).</summary>
    public DateTime? ExpiresAtUtc { get; set; }
    /// <summary>When the lock was released (Completed/Failed/Cancelled). Null = still active.</summary>
    public DateTime? ReleasedAtUtc { get; set; }
}
