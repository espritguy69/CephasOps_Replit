namespace CephasOps.Domain.Billing.Enums;

/// <summary>Phase 12: Tenant subscription lifecycle.</summary>
public enum TenantSubscriptionStatus
{
    Active = 0,
    Cancelled = 1,
    PastDue = 2,
    Trialing = 3
}
