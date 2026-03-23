using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Insights;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Automated Operational Intelligence: tenant-scoped risk signals (orders, installers, buildings) and optional platform summary. Read-only.</summary>
[ApiController]
[Route("api/insights/operational-intelligence")]
[Authorize]
public class OperationalIntelligenceController : ControllerBase
{
    private readonly IOperationalIntelligenceService _intelligenceService;
    private readonly ITenantProvider _tenantProvider;

    public OperationalIntelligenceController(IOperationalIntelligenceService intelligenceService, ITenantProvider tenantProvider)
    {
        _intelligenceService = intelligenceService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>Summary counts for current tenant (orders/installers/buildings at risk, severity bands).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<OperationalIntelligenceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OperationalIntelligenceSummaryDto>>> GetSummary(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _intelligenceService.GetSummaryAsync(companyId, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Orders at risk (stuck, likely stuck, reschedule-heavy, blocker accumulation, replacement-heavy, silent). Optional severity filter.</summary>
    [HttpGet("orders-at-risk")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderRiskSignalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderRiskSignalDto>>>> GetOrdersAtRisk([FromQuery] string? severity = null, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _intelligenceService.GetOrdersAtRiskAsync(companyId, severity, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Installers at risk (repeated blockers, high replacements, stuck orders, high issue ratio). Optional severity filter.</summary>
    [HttpGet("installers-at-risk")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstallerRiskSignalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InstallerRiskSignalDto>>>> GetInstallersAtRisk([FromQuery] string? severity = null, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _intelligenceService.GetInstallersAtRiskAsync(companyId, severity, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Buildings/sites at risk (repeated blockers, replacements). Optional severity filter.</summary>
    [HttpGet("buildings-at-risk")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BuildingRiskSignalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BuildingRiskSignalDto>>>> GetBuildingsAtRisk([FromQuery] string? severity = null, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _intelligenceService.GetBuildingsAtRiskAsync(companyId, severity, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Tenant-level risk signals (spike in stuck orders, abnormal replacement ratio).</summary>
    [HttpGet("tenant-risk-signals")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantRiskSignalDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantRiskSignalDto>>>> GetTenantRiskSignals(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _intelligenceService.GetTenantRiskSignalsAsync(companyId, cancellationToken);
        return this.Success(result);
    }
}
