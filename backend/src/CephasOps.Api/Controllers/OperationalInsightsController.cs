using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Insights;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Operational dashboards: platform health (admin only) and tenant-scoped performance, operations, financial, risk.</summary>
[ApiController]
[Route("api/insights")]
[Authorize]
public class OperationalInsightsController : ControllerBase
{
    private readonly IOperationalInsightsService _insightsService;
    private readonly ITenantProvider _tenantProvider;

    public OperationalInsightsController(IOperationalInsightsService insightsService, ITenantProvider tenantProvider)
    {
        _insightsService = insightsService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>Platform Health Dashboard — platform admin only. Aggregated health across tenants.</summary>
    [HttpGet("platform-health")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequirePermission(PermissionCatalog.AdminTenantsView)]
    [ProducesResponseType(typeof(ApiResponse<PlatformHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PlatformHealthDto>>> GetPlatformHealth(CancellationToken cancellationToken = default)
    {
        var result = await _insightsService.GetPlatformHealthAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Tenant Performance Dashboard — tenant scoped.</summary>
    [HttpGet("tenant-performance")]
    [ProducesResponseType(typeof(ApiResponse<TenantPerformanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantPerformanceDto>>> GetTenantPerformance(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _insightsService.GetTenantPerformanceAsync(companyId, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Operations Control Dashboard — tenant scoped.</summary>
    [HttpGet("operations-control")]
    [ProducesResponseType(typeof(ApiResponse<OperationsControlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OperationsControlDto>>> GetOperationsControl(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _insightsService.GetOperationsControlAsync(companyId, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Financial Overview Dashboard — tenant scoped.</summary>
    [HttpGet("financial-overview")]
    [ProducesResponseType(typeof(ApiResponse<FinancialOverviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<FinancialOverviewDto>>> GetFinancialOverview(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _insightsService.GetFinancialOverviewAsync(companyId, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Risk and Quality Dashboard — tenant scoped.</summary>
    [HttpGet("risk-quality")]
    [ProducesResponseType(typeof(ApiResponse<RiskQualityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RiskQualityDto>>> GetRiskQuality(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _insightsService.GetRiskQualityAsync(companyId, cancellationToken);
        return this.Success(result);
    }
}
