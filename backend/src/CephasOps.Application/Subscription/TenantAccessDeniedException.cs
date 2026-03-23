namespace CephasOps.Application.Subscription;

/// <summary>Thrown when tenant/subscription state does not allow access (Phase 3).</summary>
public class TenantAccessDeniedException : Exception
{
    public string? DenialReason { get; }

    public TenantAccessDeniedException(string? denialReason)
        : base(denialReason ?? "Tenant access is not allowed.")
    {
        DenialReason = denialReason;
    }

    public TenantAccessDeniedException(string? denialReason, string message)
        : base(message)
    {
        DenialReason = denialReason;
    }
}
