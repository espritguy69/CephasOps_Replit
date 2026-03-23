using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Admin.DTOs;
using CephasOps.Application.Admin.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Internal operational overview: job executions, event store, payout health, system health, SI operational intelligence.
/// For operators only; not customer-facing. Uses existing data sources only.
/// </summary>
[ApiController]
[Route("api/admin/operations")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class OperationsOverviewController : ControllerBase
{
    private readonly IOperationsOverviewService _overviewService;
    private readonly ISiOperationalInsightsService _siInsightsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OperationsOverviewController> _logger;

    public OperationsOverviewController(
        IOperationsOverviewService overviewService,
        ISiOperationalInsightsService siInsightsService,
        ITenantProvider tenantProvider,
        ILogger<OperationsOverviewController> logger)
    {
        _overviewService = overviewService;
        _siInsightsService = siInsightsService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get compact operational overview: job execution counts, event store (last 24h), payout/snapshot health, system health.
    /// No sensitive payloads; counts and status only.
    /// </summary>
    [HttpGet("overview")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<OperationalOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OperationalOverviewDto>>> GetOverview(CancellationToken cancellationToken = default)
    {
        var overview = await _overviewService.GetOverviewAsync(cancellationToken);
        return this.Success(overview);
    }

    /// <summary>
    /// Get Service Installer (SI) operational intelligence: completion performance, reschedule/blocker patterns,
    /// material replacement trends, assurance/rework visibility, operational hotspots.
    /// Scoped to a company (tenant). Optional query: companyId (SuperAdmin only), windowDays (default 90).
    /// </summary>
    [HttpGet("si-insights")]
    [RequirePermission(PermissionCatalog.OrdersView)]
    [ProducesResponseType(typeof(ApiResponse<SiOperationalInsightsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SiOperationalInsightsDto>>> GetSiInsights(
        [FromQuery] Guid? companyId,
        [FromQuery] int windowDays = 90,
        CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? _tenantProvider.CurrentTenantId;
        if (effectiveCompanyId == null || effectiveCompanyId == Guid.Empty)
        {
            return this.BadRequest("Company context is required. Set tenant or pass companyId (SuperAdmin only).");
        }
        if (companyId.HasValue && _tenantProvider.CurrentTenantId != companyId.Value)
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            if (!isSuperAdmin)
            {
                return this.BadRequest("Only SuperAdmin can request si-insights for another company.");
            }
        }
        if (windowDays < 1 || windowDays > 365)
        {
            windowDays = 90;
        }
        var insights = await _siInsightsService.GetInsightsAsync(effectiveCompanyId.Value, windowDays, cancellationToken);
        return this.Success(insights);
    }
}
