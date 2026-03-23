namespace CephasOps.Application.Events.DTOs;

/// <summary>
/// Minimal event data required for replay eligibility check (avoids loading full payload).
/// </summary>
public class ReplayEligibilityInputDto
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

/// <summary>
/// Request model for filtered operational replay (preview or execute).
/// Aligns with EventStore filter dimensions; adds operational options.
/// </summary>
public class ReplayRequestDto
{
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    /// <summary>Cap on number of events to replay. Null = use service default.</summary>
    public int? MaxEvents { get; set; }
    /// <summary>If true, only preview (counts, sample, blocked reasons); no handler execution.</summary>
    public bool DryRun { get; set; }
    /// <summary>Optional reason for audit (e.g. "Fix notification gap for 2026-03-01").</summary>
    public string? ReplayReason { get; set; }
    /// <summary>Replay target: EventStore, Workflow, Financial, Parser, Projection. Default EventStore.</summary>
    public string? ReplayTarget { get; set; }
    /// <summary>Replay mode: DryRun (preview only), Apply (execute and persist). Default Apply when executing.</summary>
    public string? ReplayMode { get; set; }
}

/// <summary>Replay target constants.</summary>
public static class ReplayTargets
{
    public const string EventStore = "EventStore";
    public const string Workflow = "Workflow";
    public const string Financial = "Financial";
    public const string Parser = "Parser";
    public const string Projection = "Projection";
}

/// <summary>Replay mode constants.</summary>
public static class ReplayModes
{
    public const string DryRun = "DryRun";
    public const string Apply = "Apply";
}

/// <summary>Replay operation state constants (Phase 2).</summary>
public static class ReplayOperationStates
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string PartiallyCompleted = "PartiallyCompleted";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Ordering strategy identifiers for deterministic replay (Phase 2).</summary>
public static class OrderingStrategies
{
    /// <summary>EventStore default: OccurredAtUtc ASC, then EventId ASC. No sequence column; tie-breaker is EventId.</summary>
    public const string OccurredAtUtcAscendingEventIdAscending = "OccurredAtUtcAscendingEventIdAscending";
}

/// <summary>Ordering guarantee level for replay (hardening).</summary>
public static class OrderingGuaranteeLevels
{
    public const string StrongDeterministic = "StrongDeterministic";
    public const string BestEffortDeterministic = "BestEffortDeterministic";
    public const string LimitedDegraded = "LimitedDegraded";
}

/// <summary>
/// Result of a dry-run replay preview. No handlers are executed. Phase 2: ordering, impact, limitations.
/// </summary>
public class ReplayPreviewResultDto
{
    public int TotalMatched { get; set; }
    public int EvaluatedCount { get; set; }
    public int EligibleCount { get; set; }
    public int BlockedCount { get; set; }
    public List<string> BlockedReasons { get; set; } = new();
    public List<EventStoreListItemDto> SampleEvents { get; set; } = new();
    public List<Guid?> CompaniesAffected { get; set; } = new();
    public List<string> EventTypesAffected { get; set; } = new();
    /// <summary>Ordering strategy used for replay (Phase 2).</summary>
    public string? OrderingStrategyId { get; set; }
    /// <summary>Human-readable ordering description.</summary>
    public string? OrderingStrategyDescription { get; set; }
    /// <summary>Entity types estimated to be affected (from sample).</summary>
    public List<string> EstimatedAffectedEntityTypes { get; set; } = new();
    /// <summary>Known limitations for this target/filters (Phase 2).</summary>
    public List<string> Limitations { get; set; } = new();
    /// <summary>Replay target id used for preview.</summary>
    public string? ReplayTargetId { get; set; }
    /// <summary>Ordering guarantee: StrongDeterministic, BestEffortDeterministic, LimitedDegraded.</summary>
    public string? OrderingGuaranteeLevel { get; set; }
    /// <summary>Reason when ordering is degraded.</summary>
    public string? OrderingDegradedReason { get; set; }

    // --- Projection-capable diff preview (bounded) ---
    /// <summary>When target is Projection: projection categories that would be updated (e.g. WorkflowTransitionHistory). Empty when not projection or no matching handlers.</summary>
    public List<string> AffectedProjectionCategories { get; set; } = new();
    /// <summary>When target is Projection and preview is available: estimated number of projection rows affected (upserts). Null when unavailable.</summary>
    public int? EstimatedChangedEntityCount { get; set; }
    /// <summary>Exact = full diff known; Estimated = count/categories only; Unavailable = not computable or not projection target.</summary>
    public string? ProjectionPreviewQuality { get; set; }
    /// <summary>When ProjectionPreviewQuality is Unavailable, reason (e.g. "Not a projection target", "No projection handlers for matched event types").</summary>
    public string? ProjectionPreviewUnavailableReason { get; set; }

    // --- Ledger awareness (Event Ledger expansion) ---
    /// <summary>Ledger families that may receive new entries when replay is executed (derived from event types). Empty when unknown or not applicable.</summary>
    public List<string> LedgerFamiliesAffected { get; set; } = new();
    /// <summary>True when at least one matched event type writes to the ledger (idempotent writes).</summary>
    public bool LedgerWritesExpected { get; set; }
    /// <summary>Ledger-derived projection views that may be updated when replay runs (e.g. WorkflowTransitionTimeline, OrderTimeline). Empty when none.</summary>
    public List<string> LedgerDerivedProjectionsImpacted { get; set; } = new();
    /// <summary>When ledger impact cannot be determined, reason (e.g. "Event types not mapped to ledger").</summary>
    public string? LedgerPreviewUnavailableReason { get; set; }

    // --- Replay safety window ---
    /// <summary>True when replay safety window was applied (events newer than cutoff excluded from preview).</summary>
    public bool SafetyWindowApplied { get; set; }
    /// <summary>Effective cutoff: events with OccurredAtUtc after this are excluded from replay.</summary>
    public DateTime? SafetyCutoffOccurredAtUtc { get; set; }
    /// <summary>Safety window in minutes (e.g. 5).</summary>
    public int? SafetyWindowMinutes { get; set; }
}

/// <summary>Projection preview quality constants.</summary>
public static class ProjectionPreviewQualities
{
    public const string Exact = "Exact";
    public const string Estimated = "Estimated";
    public const string Unavailable = "Unavailable";
}

/// <summary>
/// Replay operation list item.
/// </summary>
public class ReplayOperationListItemDto
{
    public Guid Id { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public bool DryRun { get; set; }
    public string? ReplayReason { get; set; }
    public Guid? CompanyId { get; set; }
    public string? EventType { get; set; }
    public string? ReplayTarget { get; set; }
    public string? ReplayMode { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public int? TotalMatched { get; set; }
    public int? TotalEligible { get; set; }
    public int? TotalExecuted { get; set; }
    public int? TotalSucceeded { get; set; }
    public int? TotalFailed { get; set; }
    public int? SkippedCount { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorSummary { get; set; }
    public string? State { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    /// <summary>Phase 2: true when run can be resumed.</summary>
    public bool ResumeRequired { get; set; }
    /// <summary>Phase 2: ordering strategy used.</summary>
    public string? OrderingStrategyId { get; set; }
    /// <summary>Phase 2: original operation id when this is a rerun.</summary>
    public Guid? RetriedFromOperationId { get; set; }
    /// <summary>Phase 2: rerun reason.</summary>
    public string? RerunReason { get; set; }
    /// <summary>Ordering guarantee level (from target).</summary>
    public string? OrderingGuaranteeLevel { get; set; }
    /// <summary>Reason when ordering is degraded.</summary>
    public string? OrderingDegradedReason { get; set; }
    /// <summary>Replay safety window: effective cutoff used (events with OccurredAtUtc after this were excluded).</summary>
    public DateTime? SafetyCutoffOccurredAtUtc { get; set; }
    /// <summary>Replay safety window in minutes (e.g. 5).</summary>
    public int? SafetyWindowMinutes { get; set; }
}

/// <summary>
/// Replay operation detail including optional per-event results.
/// </summary>
public class ReplayOperationDetailDto : ReplayOperationListItemDto
{
    public string? Status { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public int? MaxEvents { get; set; }
    public string? ReplayCorrelationId { get; set; }
    public string? Notes { get; set; }
    public List<ReplayOperationEventItemDto>? EventResults { get; set; }
    /// <summary>Phase 2: last checkpoint time (for progress).</summary>
    public DateTime? LastCheckpointAtUtc { get; set; }
    /// <summary>Phase 2: last processed event id (resume cursor).</summary>
    public Guid? LastProcessedEventId { get; set; }
    /// <summary>Phase 2: processed count at last checkpoint.</summary>
    public int? ProcessedCountAtLastCheckpoint { get; set; }
}

/// <summary>Phase 2: request body for rerun-failed.</summary>
public class RerunFailedRequestDto
{
    public string? RerunReason { get; set; }
}

/// <summary>Phase 2: progress for an active or resumable replay operation.</summary>
public class ReplayOperationProgressDto
{
    public Guid OperationId { get; set; }
    public string? State { get; set; }
    public bool ResumeRequired { get; set; }
    public int? TotalEligible { get; set; }
    public int? TotalExecuted { get; set; }
    public int? TotalSucceeded { get; set; }
    public int? TotalFailed { get; set; }
    public int? ProcessedCountAtLastCheckpoint { get; set; }
    public DateTime? LastCheckpointAtUtc { get; set; }
    public Guid? LastProcessedEventId { get; set; }
    /// <summary>Approximate progress 0-100 when TotalEligible known.</summary>
    public int? ProgressPercent { get; set; }
    /// <summary>Ordering strategy id.</summary>
    public string? OrderingStrategyId { get; set; }
    /// <summary>Ordering guarantee level.</summary>
    public string? OrderingGuaranteeLevel { get; set; }
    /// <summary>Reason when ordering is degraded.</summary>
    public string? OrderingDegradedReason { get; set; }
}

/// <summary>
/// Single event result within a replay operation.
/// </summary>
public class ReplayOperationEventItemDto
{
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

/// <summary>Replay target descriptor for registry (Phase 2).</summary>
public class ReplayTargetDescriptorDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Supported { get; set; }
    public string? OrderingStrategyId { get; set; }
    public string? OrderingStrategyDescription { get; set; }
    /// <summary>StrongDeterministic, BestEffortDeterministic, or LimitedDegraded.</summary>
    public string? OrderingGuaranteeLevel { get; set; }
    /// <summary>Reason when ordering is degraded.</summary>
    public string? OrderingDegradedReason { get; set; }
    public bool SupportsPreview { get; set; }
    public bool SupportsApply { get; set; }
    public bool SupportsCheckpoint { get; set; }
    public bool IsReplaySafe { get; set; }
    public List<string> SupportedFilterNames { get; set; } = new();
    public List<string> Limitations { get; set; } = new();
}
