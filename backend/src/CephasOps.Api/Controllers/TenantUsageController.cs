using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Tenant usage metering (Phase 4). Current tenant usage for tenant admin; platform admin can query any tenant.</summary>
[ApiController]
[Route("api/usage")]
[Authorize]
public class TenantUsageController : ControllerBase
{
    private readonly ITenantUsageQueryService _queryService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TenantUsageController> _logger;

    public TenantUsageController(
        ITenantUsageQueryService queryService,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ILogger<TenantUsageController> logger)
    {
        _queryService = queryService;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Get current tenant's usage for the current month. Tenant admin sees own tenant only.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantUsageEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantUsageEntryDto>>>> GetCurrentTenantUsage(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
            return StatusCode(401, ApiResponse.ErrorResponse("Tenant context required"));
        var usage = await _queryService.GetCurrentMonthUsageAsync(tenantId.Value, cancellationToken);
        return this.Success(usage);
    }

    /// <summary>Get current tenant's usage for a given month. Query params: year, month (1-12).</summary>
    [HttpGet("current/by-month")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantUsageEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantUsageEntryDto>>>> GetCurrentTenantUsageByMonth(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return this.BadRequest<IReadOnlyList<TenantUsageEntryDto>>("Year and month (1-12) required.");
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
            return StatusCode(401, ApiResponse.ErrorResponse("Tenant context required"));
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var usage = await _queryService.GetUsageAsync(tenantId.Value, start, end, cancellationToken);
        return this.Success(usage);
    }
}
