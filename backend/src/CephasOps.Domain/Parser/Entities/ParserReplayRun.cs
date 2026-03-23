namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Records one parser replay run for regression/improvement tracking.
/// Read-only audit; does not modify Orders or Drafts.
/// </summary>
public class ParserReplayRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Who triggered: CLI or Admin.</summary>
    public string TriggeredBy { get; set; } = string.Empty;

    /// <summary>Original parse session (if replayed by session).</summary>
    public Guid? OriginalParseSessionId { get; set; }

    /// <summary>Attachment that was replayed.</summary>
    public Guid AttachmentId { get; set; }

    public string OldParseStatus { get; set; } = string.Empty;
    public decimal OldConfidence { get; set; }
    public string NewParseStatus { get; set; } = string.Empty;
    public decimal NewConfidence { get; set; }

    /// <summary>JSON array of missing field names (old).</summary>
    public string? OldMissingFields { get; set; }

    /// <summary>JSON array of missing field names (new).</summary>
    public string? NewMissingFields { get; set; }

    public string? OldSheetName { get; set; }
    public string? NewSheetName { get; set; }
    public int? OldHeaderRow { get; set; }
    public int? NewHeaderRow { get; set; }

    public bool RegressionDetected { get; set; }
    public bool ImprovementDetected { get; set; }

    /// <summary>Full diff payload (JSON).</summary>
    public string? ResultSummary { get; set; }
}
