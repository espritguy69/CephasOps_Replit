namespace CephasOps.Application.Subscription;

/// <summary>Result of subscription/tenant access check (Phase 3).</summary>
public class SubscriptionAccessResult
{
    public bool Allowed { get; set; }
    /// <summary>When not allowed: tenant_suspended, tenant_disabled, subscription_expired, subscription_cancelled, read_only_mode, etc.</summary>
    public string? DenialReason { get; set; }
    /// <summary>When true, only read operations are permitted.</summary>
    public bool ReadOnlyMode { get; set; }

    public static SubscriptionAccessResult Allow() => new() { Allowed = true };
    public static SubscriptionAccessResult Deny(string reason) => new() { Allowed = false, DenialReason = reason };
    public static SubscriptionAccessResult ReadOnly(string? reason = null) => new() { Allowed = false, DenialReason = reason ?? "read_only_mode", ReadOnlyMode = true };
}

/// <summary>Canonical denial reason codes for API/client handling.</summary>
public static class SubscriptionDenialReasons
{
    public const string TenantSuspended = "tenant_suspended";
    public const string TenantDisabled = "tenant_disabled";
    public const string SubscriptionExpired = "subscription_expired";
    public const string SubscriptionCancelled = "subscription_cancelled";
    public const string SubscriptionPastDue = "subscription_past_due";
    public const string ReadOnlyMode = "read_only_mode";
    public const string FeatureNotEnabled = "feature_not_enabled";
    public const string SeatLimitExceeded = "seat_limit_exceeded";
}
