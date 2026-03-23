namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Result of a single parser replay run.
/// </summary>
public class ParserReplayResult
{
    public Guid ReplayRunId { get; set; }
    public Guid AttachmentId { get; set; }
    public Guid? OriginalParseSessionId { get; set; }
    public string? FileName { get; set; }

    public string OldParseStatus { get; set; } = string.Empty;
    public decimal OldConfidence { get; set; }
    public string NewParseStatus { get; set; } = string.Empty;
    public decimal NewConfidence { get; set; }

    public IReadOnlyList<string>? OldMissingFields { get; set; }
    public IReadOnlyList<string>? NewMissingFields { get; set; }

    public string? OldSheetName { get; set; }
    public string? NewSheetName { get; set; }
    public int? OldHeaderRow { get; set; }
    public int? NewHeaderRow { get; set; }

    public bool RegressionDetected { get; set; }
    public bool ImprovementDetected { get; set; }

    /// <summary>Phase 9: old/new category and reason for CLI/reports.</summary>
    public string? OldParseFailureCategory { get; set; }
    public string? NewParseFailureCategory { get; set; }
    public string? ReasonForChange { get; set; }
    public string? NewDriftSignature { get; set; }

    /// <summary>Error if replay could not be performed (e.g. attachment not found, no file content).</summary>
    public string? Error { get; set; }
}
