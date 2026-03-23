namespace CephasOps.Domain.Events;

/// <summary>
/// Audit record for an operational replay request. Stores request filters and (when executed) result counts.
/// Phase 2: request fields. Phase 6: result fields (TotalMatched, TotalEligible, etc.).
/// </summary>
public class ReplayOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // --- Request (audit) ---
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public bool DryRun { get; set; }
    public string? ReplayReason { get; set; }

    // --- Filters ---
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public int? MaxEvents { get; set; }

    // --- Result (Phase 6: populated after execute) ---
    public int? TotalMatched { get; set; }
    public int? TotalEligible { get; set; }
    public int? TotalExecuted { get; set; }
    public int? TotalSucceeded { get; set; }
    public int? TotalFailed { get; set; }
    public string? ReplayCorrelationId { get; set; }
    public string? Notes { get; set; }
    public string? State { get; set; } // e.g. Pending, Running, Completed, Failed, Cancelled
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>Replay target: EventStore, Workflow, Financial, Parser, Projection.</summary>
    public string? ReplayTarget { get; set; }
    /// <summary>Replay mode: DryRun, Apply.</summary>
    public string? ReplayMode { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public int? SkippedCount { get; set; }
    /// <summary>Summary of errors for failed run (e.g. first N messages).</summary>
    public string? ErrorSummary { get; set; }
    /// <summary>Background job id when replay was run asynchronously.</summary>
    public Guid? BackgroundJobId { get; set; }

    // --- Phase 2: Checkpoint / Resume ---
    /// <summary>True when run was interrupted and can be resumed.</summary>
    public bool ResumeRequired { get; set; }
    /// <summary>Last time a checkpoint was persisted.</summary>
    public DateTime? LastCheckpointAtUtc { get; set; }
    /// <summary>Last event id processed (resume cursor).</summary>
    public Guid? LastProcessedEventId { get; set; }
    /// <summary>OccurredAtUtc of last processed event (resume cursor).</summary>
    public DateTime? LastProcessedOccurredAtUtc { get; set; }
    /// <summary>Number of checkpoints written this run.</summary>
    public int CheckpointCount { get; set; }
    /// <summary>Total processed count at last checkpoint.</summary>
    public int? ProcessedCountAtLastCheckpoint { get; set; }

    // --- Phase 2: Ordering & Rerun lineage ---
    /// <summary>Ordering strategy used, e.g. OccurredAtUtcAscendingEventIdAscending.</summary>
    public string? OrderingStrategyId { get; set; }
    /// <summary>Original operation id when this run is a rerun.</summary>
    public Guid? RetriedFromOperationId { get; set; }
    /// <summary>Reason for rerun (audit).</summary>
    public string? RerunReason { get; set; }

    // --- Cancel (hardening) ---
    /// <summary>When set, the running job will stop at the next checkpoint and set State = Cancelled.</summary>
    public DateTime? CancelRequestedAtUtc { get; set; }

    // --- Replay safety window ---
    /// <summary>Effective safety cutoff: events with OccurredAtUtc after this were excluded from this run.</summary>
    public DateTime? SafetyCutoffOccurredAtUtc { get; set; }
    /// <summary>Safety window in minutes used for this run (e.g. 5).</summary>
    public int? SafetyWindowMinutes { get; set; }

    // --- Distributed worker ownership (Phase 1) ---
    /// <summary>Worker that claimed this operation for execution. Null when unclaimed or after release.</summary>
    public Guid? WorkerId { get; set; }
    /// <summary>When the current worker claimed this operation.</summary>
    public DateTime? ClaimedAtUtc { get; set; }
}

/// <summary>
/// Per-event replay attempt within an operational replay (audit trail).
/// </summary>
public class ReplayOperationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReplayOperationId { get; set; }
    public Guid EventId { get; set; }
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SkippedReason { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public long? DurationMs { get; set; }
}
