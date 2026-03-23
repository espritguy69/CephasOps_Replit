namespace CephasOps.Domain.Billing.Entities;

/// <summary>SaaS scaling: monthly aggregated metrics per tenant for billing and reporting.</summary>
public class TenantMetricsMonthly
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalUsers { get; set; }
    public int OrdersCreated { get; set; }
    public int BackgroundJobsExecuted { get; set; }
    public long StorageBytes { get; set; }
    public int ApiCalls { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
