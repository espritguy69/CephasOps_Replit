namespace CephasOps.Application.Billing.Usage;

/// <summary>Reads tenant usage for dashboards and enforcement (Phase 4).</summary>
public interface ITenantUsageQueryService
{
    /// <summary>Get usage for a tenant in a period. Returns one entry per metric with Quantity.</summary>
    Task<IReadOnlyList<TenantUsageEntryDto>> GetUsageAsync(Guid tenantId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default);

    /// <summary>Get current month usage for a tenant.</summary>
    Task<IReadOnlyList<TenantUsageEntryDto>> GetCurrentMonthUsageAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Get usage for a specific metric and period.</summary>
    Task<TenantUsageEntryDto?> GetMetricUsageAsync(Guid tenantId, string metricKey, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default);
}

/// <summary>Single usage entry (tenant + metric + period + value).</summary>
public class TenantUsageEntryDto
{
    public Guid TenantId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
