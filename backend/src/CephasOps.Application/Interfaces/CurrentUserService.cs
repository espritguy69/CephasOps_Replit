using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Implementation of ICurrentUserService that reads from HTTP context claims
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("company_id")?.Value;

            if (string.IsNullOrEmpty(companyIdClaim))
                return null;
            if (Guid.TryParse(companyIdClaim, out var companyId) && companyId != Guid.Empty)
                return companyId;
            return null;
        }
    }

    public string? Email
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public List<string> Roles
    {
        get
        {
            var roles = new List<string>();

            // Try to get roles from role claims
            var roleClaims = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value)
                ?? _httpContextAccessor.HttpContext?.User?.FindAll("role")?.Select(c => c.Value)
                ?? _httpContextAccessor.HttpContext?.User?.FindAll("roles")?.Select(c => c.Value)
                ?? Enumerable.Empty<string>();

            roles.AddRange(roleClaims);

            // Also try to get roles from a comma-separated claim
            var rolesClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("roles")?.Value;
            if (!string.IsNullOrEmpty(rolesClaim))
            {
                var rolesFromClaim = rolesClaim.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                roles.AddRange(rolesFromClaim);
            }

            return roles.Distinct().ToList();
        }
    }

    public bool IsSuperAdmin => Roles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

    public Guid? ServiceInstallerId
    {
        get
        {
            var siIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("serviceInstallerId")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("siId")?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("si_id")?.Value;

            return Guid.TryParse(siIdClaim, out var siId) ? siId : null;
        }
    }
}

