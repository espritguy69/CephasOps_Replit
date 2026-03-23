namespace CephasOps.Application.Rebuild.DTOs;

/// <summary>Result of a rebuild execution (or error).</summary>
public class RebuildExecutionResultDto
{
    public Guid RebuildOperationId { get; set; }
    public string RebuildTargetId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public int RowsDeleted { get; set; }
    public int RowsInserted { get; set; }
    public int RowsUpdated { get; set; }
    public int? SourceRecordCount { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
}
