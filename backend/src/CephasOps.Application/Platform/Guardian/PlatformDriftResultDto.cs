namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: single drift finding.</summary>
public class PlatformDriftItemDto
{
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Expected { get; set; }
    public string? Actual { get; set; }
    /// <summary>Informational | Warning | Critical</summary>
    public string Classification { get; set; } = "Informational";
    public string? Message { get; set; }
}

/// <summary>Platform Guardian: full drift report.</summary>
public class PlatformDriftResultDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<PlatformDriftItemDto> Items { get; set; } = Array.Empty<PlatformDriftItemDto>();
    public int InformationalCount { get; set; }
    public int WarningCount { get; set; }
    public int CriticalCount { get; set; }
}
