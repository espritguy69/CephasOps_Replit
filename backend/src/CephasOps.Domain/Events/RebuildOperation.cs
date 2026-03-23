namespace CephasOps.Domain.Events;

/// <summary>
/// Audit record for an operational state rebuild run. Stores target, scope, result summary, and checkpoint/resume state.
/// Phase 2: Pending (queued), Running, PartiallyCompleted (resumable), Completed, Failed; background job and lock support.
/// </summary>
public class RebuildOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RebuildTargetId { get; set; } = string.Empty;
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }

    public Guid? ScopeCompanyId { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public bool DryRun { get; set; }

    public string State { get; set; } = "Running";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMs { get; set; }

    /// <summary>Background job id when run was enqueued (async).</summary>
    public Guid? BackgroundJobId { get; set; }

    public int RowsDeleted { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int? SourceRecordCount { get; set; }

    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }

    // --- Phase 2: Checkpoint / resume ---
    /// <summary>True when run was interrupted and can be resumed.</summary>
    public bool ResumeRequired { get; set; }
    public DateTime? LastCheckpointAtUtc { get; set; }
    /// <summary>Last processed source cursor (e.g. EventId for Event Store).</summary>
    public Guid? LastProcessedEventId { get; set; }
    /// <summary>OccurredAtUtc of last processed item (ordering key).</summary>
    public DateTime? LastProcessedOccurredAtUtc { get; set; }
    /// <summary>Total processed count at last checkpoint.</summary>
    public int ProcessedCountAtLastCheckpoint { get; set; }
    public int CheckpointCount { get; set; }
    /// <summary>Original operation id when this run is a resume.</summary>
    public Guid? RetriedFromOperationId { get; set; }
    public string? RerunReason { get; set; }

    // --- Distributed worker ownership (Phase 1) ---
    /// <summary>Worker that claimed this operation for execution. Null when unclaimed or after release.</summary>
    public Guid? WorkerId { get; set; }
    /// <summary>When the current worker claimed this operation.</summary>
    public DateTime? ClaimedAtUtc { get; set; }
}

/// <summary>
/// Durable lock: one active rebuild per (TargetId, ScopeKey). Prevents overlapping rebuilds for same target/scope.
/// ScopeKey = ScopeCompanyId.ToString("N") or "global" for full rebuild.
/// </summary>
public class RebuildExecutionLock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RebuildTargetId { get; set; } = string.Empty;
    /// <summary>Company scope key or "global" for full rebuild. Unique with TargetId when active.</summary>
    public string ScopeKey { get; set; } = string.Empty;

    public Guid RebuildOperationId { get; set; }
    public DateTime AcquiredAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
}
