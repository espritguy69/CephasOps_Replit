namespace CephasOps.Application.Rebuild.DTOs;

/// <summary>Summary of a rebuild operation (for list/detail).</summary>
public class RebuildOperationSummaryDto
{
    public Guid Id { get; set; }
    public string RebuildTargetId { get; set; } = string.Empty;
    public Guid? RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public Guid? ScopeCompanyId { get; set; }
    public DateTime? FromOccurredAtUtc { get; set; }
    public DateTime? ToOccurredAtUtc { get; set; }
    public bool DryRun { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMs { get; set; }
    public Guid? BackgroundJobId { get; set; }
    public int RowsDeleted { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int? SourceRecordCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }
    public bool ResumeRequired { get; set; }
    public DateTime? LastCheckpointAtUtc { get; set; }
    public int ProcessedCountAtLastCheckpoint { get; set; }
    public int CheckpointCount { get; set; }
    public Guid? RetriedFromOperationId { get; set; }
    public string? RerunReason { get; set; }
}

/// <summary>Progress and checkpoint info for a rebuild operation (Phase 2).</summary>
public class RebuildProgressDto
{
    public Guid OperationId { get; set; }
    public string State { get; set; } = string.Empty;
    public bool ResumeRequired { get; set; }
    public DateTime? LastCheckpointAtUtc { get; set; }
    public int ProcessedCountAtLastCheckpoint { get; set; }
    public int CheckpointCount { get; set; }
    public int RowsDeleted { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int? SourceRecordCount { get; set; }
}
