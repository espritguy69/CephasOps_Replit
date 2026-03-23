namespace CephasOps.Domain.Billing.Entities;

/// <summary>SaaS scaling: daily aggregated metrics per tenant for reporting and analytics.</summary>
public class TenantMetricsDaily
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime DateUtc { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalUsers { get; set; }
    public int OrdersCreated { get; set; }
    public int BackgroundJobsExecuted { get; set; }
    public long StorageBytes { get; set; }
    public int ApiCalls { get; set; }
    /// <summary>Enterprise: 0–100 health score from TenantHealthScoringService.</summary>
    public int? HealthScore { get; set; }
    /// <summary>Enterprise: Healthy | Warning | Critical.</summary>
    public string? HealthStatus { get; set; }
    /// <summary>Enterprise: count of rate limit exceeded events in this period.</summary>
    public int RateLimitExceededCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
