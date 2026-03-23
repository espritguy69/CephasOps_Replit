namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: performance health view for GET /api/platform/analytics/performance-health.</summary>
public class PerformanceHealthDto
{
    public DateTime GeneratedAtUtc { get; set; }
    /// <summary>Count of slow queries detected in the last window (if tracking enabled).</summary>
    public int SlowQueryCountLastWindow { get; set; }
    /// <summary>Count of job executions exceeding slow threshold (if tracked).</summary>
    public int SlowJobExecutionCountLastWindow { get; set; }
    /// <summary>Endpoint paths that have been flagged as degraded (repeated slow or errors).</summary>
    public IReadOnlyList<string> DegradedEndpoints { get; set; } = Array.Empty<string>();
    /// <summary>Tenant IDs with warning/critical health or high latency impact.</summary>
    public IReadOnlyList<Guid> TenantsWithHighLatencyImpact { get; set; } = Array.Empty<Guid>();
    /// <summary>Queue lag indicator: e.g. pending job count (if available).</summary>
    public int? PendingJobCount { get; set; }
    /// <summary>Summary message.</summary>
    public string? Summary { get; set; }
}
