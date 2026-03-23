namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Current tenant context for multi-tenant isolation (Phase 11).
/// Resolved from current user's company (Company.TenantId). Null when not in tenant scope.
/// </summary>
public interface ITenantContext
{
    /// <summary>Current tenant ID; null if user has no company or company has no tenant.</summary>
    Guid? TenantId { get; }

    /// <summary>Tenant slug when resolved; null otherwise.</summary>
    string? TenantSlug { get; }

    /// <summary>True when the current request is scoped to a tenant.</summary>
    bool IsTenantResolved { get; }
}
