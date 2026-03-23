using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Billing.Usage;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Platform admin: tenant usage for any tenant (Phase 4).</summary>
[ApiController]
[Route("api/platform/usage")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class PlatformUsageController : ControllerBase
{
    private readonly ITenantUsageQueryService _queryService;
    private readonly ILogger<PlatformUsageController> _logger;

    public PlatformUsageController(ITenantUsageQueryService queryService, ILogger<PlatformUsageController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>Get usage for a tenant (current month). Platform admin only.</summary>
    [HttpGet("tenants/{tenantId:guid}")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantUsageEntryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantUsageEntryDto>>>> GetTenantUsage(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var usage = await _queryService.GetCurrentMonthUsageAsync(tenantId, cancellationToken);
        return this.Success(usage);
    }

    /// <summary>Get usage for a tenant for a given month. Query params: year, month (1-12). Platform admin only.</summary>
    [HttpGet("tenants/{tenantId:guid}/by-month")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantUsageEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantUsageEntryDto>>>> GetTenantUsageByMonth(
        Guid tenantId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return this.BadRequest<IReadOnlyList<TenantUsageEntryDto>>("Year and month (1-12) required.");
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var usage = await _queryService.GetUsageAsync(tenantId, start, end, cancellationToken);
        return this.Success(usage);
    }
}
