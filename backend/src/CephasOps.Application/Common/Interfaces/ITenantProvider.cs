namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Provides the canonical effective company resolution for the request.
/// <para><b>IMPORTANT:</b> All tenant-aware services MUST use ITenantProvider and MUST NOT read
/// CurrentUser.CompanyId (or ICurrentUserService.CompanyId) directly.</para>
/// <para>Resolution precedence: 1) X-Company-Id (SuperAdmin override), 2) JWT CompanyId,
/// 3) Department→Company fallback, 4) Unresolved.</para>
/// Resolve once per request via GetEffectiveCompanyIdAsync (e.g. in TenantGuardMiddleware) before reading CurrentTenantId.
/// Used by DbContext global query filters and services. See docs/architecture/SAAS_MULTI_TENANT_IMPLEMENTATION_SUMMARY.md.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Current company (tenant) id after resolution. Null until GetEffectiveCompanyIdAsync is called for this request;
    /// then returns the cached effective company (or null if unresolved).
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Resolves the effective company for this request using the canonical precedence and caches the result.
    /// Must be called once per request (e.g. by TenantGuardMiddleware) before any consumer reads CurrentTenantId.
    /// Precedence: 1) X-Company-Id (SuperAdmin + valid header), 2) JWT CompanyId, 3) Department→Company fallback, 4) Unresolved.
    /// </summary>
    Task<Guid?> GetEffectiveCompanyIdAsync(CancellationToken cancellationToken = default);
}
