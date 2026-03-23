using CephasOps.Application.Common;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Services;

/// <summary>
/// Canonical effective company resolution for the request.
/// <para><b>IMPORTANT:</b> This service provides the canonical effective company resolution. All tenant-aware
/// services MUST use ITenantProvider and MUST NOT read CurrentUser.CompanyId directly.</para>
/// <para>Resolution precedence: 1) X-Company-Id (SuperAdmin override), 2) JWT CompanyId,
/// 3) Department→Company fallback, 4) Unresolved.</para>
/// DefaultCompanyId is not used in this path; guard, subscription, and tenant context use this resolution only.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private const string ResolutionSourceDepartmentFallback = "DepartmentFallback";

    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserCompanyFromDepartmentResolver _departmentResolver;
    private readonly TenantOptions _options;
    private readonly ILogger<TenantProvider> _logger;

    private Guid? _cachedEffectiveCompanyId;
    private bool _resolutionRun;

    public TenantProvider(
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor,
        IUserCompanyFromDepartmentResolver departmentResolver,
        IOptions<TenantOptions> options,
        ILogger<TenantProvider> logger)
    {
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _departmentResolver = departmentResolver;
        _options = options?.Value ?? new TenantOptions();
        _logger = logger;
    }

    /// <inheritdoc />
    public Guid? CurrentTenantId => _cachedEffectiveCompanyId;

    /// <inheritdoc />
    public async Task<Guid?> GetEffectiveCompanyIdAsync(CancellationToken cancellationToken = default)
    {
        if (_resolutionRun)
            return _cachedEffectiveCompanyId;

        _resolutionRun = true;

        // 1) X-Company-Id if user is SuperAdmin and header is valid
        if (_currentUser.IsSuperAdmin)
        {
            var header = _httpContextAccessor.HttpContext?.Request?.Headers["X-Company-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out var headerCompanyId) && headerCompanyId != Guid.Empty)
            {
                _cachedEffectiveCompanyId = headerCompanyId;
                return _cachedEffectiveCompanyId;
            }
        }

        // 2) JWT company claim if present and non-empty (read from claims so we can fall through to department fallback when missing)
        var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("company_id")?.Value;
        if (Guid.TryParse(companyIdClaim, out var jwtCompanyId) && jwtCompanyId != Guid.Empty)
        {
            _cachedEffectiveCompanyId = jwtCompanyId;
            return _cachedEffectiveCompanyId;
        }

        // 3) Department → Company fallback only when JWT company is missing
        var userId = _currentUser.UserId;
        if (userId.HasValue)
        {
            var deptResult = await _departmentResolver.TryGetSingleCompanyFromDepartmentsAsync(userId.Value, cancellationToken);
            if (deptResult.Ambiguous)
            {
                _logger.LogWarning(
                    "Tenant resolution: ambiguous department fallback (user belongs to multiple companies). UserId: {UserId}, Path: {Path}",
                    userId.Value,
                    _httpContextAccessor.HttpContext?.Request?.Path.Value ?? "(unknown)");
                _cachedEffectiveCompanyId = null;
                return null;
            }
            if (deptResult.CompanyId.HasValue)
            {
                _logger.LogInformation(
                    "Tenant resolution: department fallback used. UserId: {UserId}, CompanyId: {CompanyId}, Path: {Path}, ResolutionSource: {ResolutionSource}",
                    userId.Value,
                    deptResult.CompanyId.Value,
                    _httpContextAccessor.HttpContext?.Request?.Path.Value ?? "(unknown)",
                    ResolutionSourceDepartmentFallback);
                _cachedEffectiveCompanyId = deptResult.CompanyId.Value;
                return _cachedEffectiveCompanyId;
            }
        }

        // 4) Unresolved
        _cachedEffectiveCompanyId = null;
        return null;
    }
}
