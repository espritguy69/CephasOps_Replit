using CephasOps.Domain.Billing.Enums;

namespace CephasOps.Application.Platform;

/// <summary>Platform admin: optional fields for PATCH tenant subscription. Only provided properties are updated.</summary>
public class PlatformTenantSubscriptionUpdateRequest
{
    /// <summary>Billing plan slug (e.g. trial, starter). Plan must exist and be active.</summary>
    public string? PlanSlug { get; set; }

    /// <summary>Subscription status: Active, Trialing, Cancelled, PastDue.</summary>
    public string? Status { get; set; }

    /// <summary>When trial ends (UTC). Null to clear.</summary>
    public DateTime? TrialEndsAtUtc { get; set; }

    /// <summary>Next billing date (UTC).</summary>
    public DateTime? NextBillingDateUtc { get; set; }

    /// <summary>Max seats. Null = keep current; use 0 or positive to set.</summary>
    public int? SeatLimit { get; set; }

    /// <summary>Max storage in bytes. Null = keep current; use 0 or positive to set.</summary>
    public long? StorageLimitBytes { get; set; }
}
