namespace CephasOps.Domain.Billing.Entities;

/// <summary>Tenant-level feature override (enable/disable regardless of plan).</summary>
public class TenantFeatureFlag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
