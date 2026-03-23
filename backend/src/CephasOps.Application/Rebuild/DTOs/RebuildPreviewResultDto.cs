namespace CephasOps.Application.Rebuild.DTOs;

/// <summary>Result of a rebuild preview (dry-run): scope and estimated impact.</summary>
public class RebuildPreviewResultDto
{
    public string RebuildTargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SourceRecordCount { get; set; }
    public int? CurrentTargetRowCount { get; set; }
    public string RebuildStrategy { get; set; } = string.Empty;
    public string? ScopeDescription { get; set; }
    public bool DryRun { get; set; } = true;
}
