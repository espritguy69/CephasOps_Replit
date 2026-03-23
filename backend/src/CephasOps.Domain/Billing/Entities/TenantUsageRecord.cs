namespace CephasOps.Domain.Billing.Entities;

/// <summary>Phase 12: Metered usage for a tenant in a period.</summary>
public class TenantUsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Last time Quantity was updated (for upsert semantics).</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
