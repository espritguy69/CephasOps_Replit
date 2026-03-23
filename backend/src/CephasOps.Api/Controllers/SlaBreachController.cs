using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>SLA Breach Engine: tenant-scoped summary and orders at risk (nearing breach / breached). Read-only; uses Order.KpiDueAt.</summary>
[ApiController]
[Route("api/insights/sla")]
[Authorize]
public class SlaBreachController : ControllerBase
{
    private readonly ISlaBreachService _slaBreachService;
    private readonly ITenantProvider _tenantProvider;

    public SlaBreachController(ISlaBreachService slaBreachService, ITenantProvider tenantProvider)
    {
        _slaBreachService = slaBreachService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>Tenant-scoped SLA summary: counts for OnTrack, NearingBreach, Breached, NoSla.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<SlaBreachSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SlaBreachSummaryDto>>> GetSummary(CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _slaBreachService.GetSummaryAsync(companyId, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Tenant-scoped orders at risk (nearing breach or breached). Optional breachState (NearingBreach, Breached) and severity (Warning, Critical) filters.</summary>
    [HttpGet("orders-at-risk")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SlaBreachOrderItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlaBreachOrderItemDto>>>> GetOrdersAtRisk(
        [FromQuery] string? breachState = null,
        [FromQuery] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var result = await _slaBreachService.GetOrdersAtRiskAsync(companyId, breachState, severity, cancellationToken);
        return this.Success(result);
    }
}
