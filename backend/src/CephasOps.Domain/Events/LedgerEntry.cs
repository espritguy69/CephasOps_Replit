namespace CephasOps.Domain.Events;

/// <summary>
/// Append-only operational event ledger entry. Immutable once written.
/// Links to source domain event (when event-driven) or to replay operation (when operation-driven).
/// Used for deterministic reconstruction, audit, and ledger-derived projections.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Source domain event id when this entry was created from an event. Null for operation-driven entries.</summary>
    public Guid? SourceEventId { get; set; }
    /// <summary>Replay operation id when this entry was created from a completed replay operation. Null for event-driven entries.</summary>
    public Guid? ReplayOperationId { get; set; }

    /// <summary>Ledger family: WorkflowTransition, ReplayOperationCompleted, etc. Determines idempotency scope and ordering.</summary>
    public string LedgerFamily { get; set; } = string.Empty;
    /// <summary>Optional category within the family (e.g. transition type).</summary>
    public string? Category { get; set; }

    public Guid? CompanyId { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    /// <summary>Domain event type (e.g. WorkflowTransitionCompleted) or operation fact type.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>When the fact occurred (event time or operation completion time).</summary>
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>When this ledger record was written. Never changed.</summary>
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional JSON snapshot of canonical payload for this family. Append-only; no in-place edit.</summary>
    public string? PayloadSnapshot { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    /// <summary>Ordering strategy id used when writing (e.g. OccurredAtUtcAscendingEventIdAscending).</summary>
    public string? OrderingStrategyId { get; set; }
}
