namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: aggregated platform health view for GET /api/platform/analytics/platform-health.</summary>
public class PlatformHealthDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalActiveTenants { get; set; }
    public int TenantsInWarningState { get; set; }
    public int TenantsInCriticalAnomalyState { get; set; }
    /// <summary>Rate-limit breach count last 24h (from logs/metrics if available; else 0).</summary>
    public int RateLimitBreachCountLast24h { get; set; }
    /// <summary>Stuck job resets (watchdog + worker) last 24h (if tracked; else null).</summary>
    public int? StuckJobResetsLast24h { get; set; }
    public int FailedJobsLast24h { get; set; }
    /// <summary>Tenants with storage warning (e.g. near quota or growth anomaly).</summary>
    public int StorageWarningTenants { get; set; }
    /// <summary>Suspicious auth or impersonation count if available.</summary>
    public int? SuspiciousAuthOrImpersonationCount { get; set; }
    /// <summary>True if performance degradation flags (e.g. high pending queue, degraded tenants) are set.</summary>
    public bool PerformanceDegradationFlag { get; set; }
    /// <summary>Summary message.</summary>
    public string? Summary { get; set; }
}
