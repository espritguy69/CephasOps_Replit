using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Domain.Billing.Entities;

/// <summary>Phase 12: Tenant's subscription to a billing plan. SaaS scaling: trial, limits, next billing.</summary>
public class TenantSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid BillingPlanId { get; set; }
    public TenantSubscriptionStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    /// <summary>When trial ends (UTC). Null when not on trial.</summary>
    public DateTime? TrialEndsAtUtc { get; set; }
    /// <summary>Billing interval (from plan or overridden).</summary>
    public BillingCycle BillingCycle { get; set; }
    /// <summary>Max allowed users/seats. Null = unlimited.</summary>
    public int? SeatLimit { get; set; }
    /// <summary>Max storage in bytes. Null = unlimited.</summary>
    public long? StorageLimitBytes { get; set; }
    /// <summary>Next billing date (UTC).</summary>
    public DateTime? NextBillingDateUtc { get; set; }
    /// <summary>External payment provider subscription ID (Stripe, etc.).</summary>
    public string? ExternalSubscriptionId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
