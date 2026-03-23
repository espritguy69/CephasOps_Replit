using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Billing.Subscription.DTOs;

public class TenantSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BillingPlanId { get; set; }
    public string? PlanSlug { get; set; }
    public TenantSubscriptionStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime? TrialEndsAtUtc { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public int? SeatLimit { get; set; }
    public long? StorageLimitBytes { get; set; }
    public DateTime? NextBillingDateUtc { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
