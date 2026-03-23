namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Phase 10: Per-profile drift summary for the drift-report.
/// </summary>
public class ProfileDriftSummary
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int TotalDrafts { get; set; }
    public int NeedsReviewCount { get; set; }
    public Dictionary<string, int> CountByCategory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int DriftDetectedCount { get; set; }
    public double DriftDetectedRate => TotalDrafts > 0 ? (double)DriftDetectedCount / TotalDrafts : 0;
    public List<SignatureCount> TopDriftSignatures { get; set; } = new();
    public List<FieldCount> TopMissingFields { get; set; } = new();
    public double? AvgHeaderScore { get; set; }
    public double? AvgBestSheetScore { get; set; }
    public bool RecentProfileChange { get; set; }
    public string? ProfileVersion { get; set; }
    public string? EffectiveFrom { get; set; }
    public string? Owner { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public class SignatureCount
{
    public string Signature { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class FieldCount
{
    public string FieldName { get; set; } = string.Empty;
    public int Count { get; set; }
}
