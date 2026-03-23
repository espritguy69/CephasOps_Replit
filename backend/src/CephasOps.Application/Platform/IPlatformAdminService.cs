using CephasOps.Application.Billing.Subscription.DTOs;

namespace CephasOps.Application.Platform;

/// <summary>Platform admin: tenant list, diagnostics, suspend/resume, subscription management. SuperAdmin only.</summary>
public interface IPlatformAdminService
{
    Task<List<PlatformTenantListDto>> ListTenantsAsync(string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<PlatformTenantDiagnosticsDto?> GetTenantDiagnosticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid?> GetCompanyIdByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Get current subscription for a tenant (active/trialing preferred, else latest).</summary>
    Task<TenantSubscriptionDto?> GetTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Update tenant subscription; only non-null request fields are applied. Returns updated DTO or null if tenant/subscription not found.</summary>
    Task<TenantSubscriptionDto?> UpdateTenantSubscriptionAsync(Guid tenantId, PlatformTenantSubscriptionUpdateRequest request, CancellationToken cancellationToken = default);
}
