namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Phase 10: Full drift report result (executive summary + per-profile summaries).
/// </summary>
public class DriftReportResult
{
    public int Days { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalDrafts { get; set; }
    public int ProfilesWithDrafts { get; set; }
    public string? ProfileIdFilter { get; set; }
    public List<ProfileDriftSummary> ProfileSummaries { get; set; } = new();
    /// <summary>Optional: replay regression counts by profile when ParserReplayRuns data is included.</summary>
    public Dictionary<Guid, int>? ReplayRegressionsByProfile { get; set; }
}
