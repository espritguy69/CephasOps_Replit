using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Insights;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Platform admin only: aggregated operational intelligence summary across tenants. Read-only; no tenant business data.</summary>
[ApiController]
[Route("api/insights")]
[Authorize(Roles = "SuperAdmin,Admin")]
[RequirePermission(PermissionCatalog.AdminTenantsView)]
public class PlatformOperationalIntelligenceController : ControllerBase
{
    private readonly IOperationalIntelligenceService _intelligenceService;
    private readonly ISlaBreachService _slaBreachService;

    public PlatformOperationalIntelligenceController(IOperationalIntelligenceService intelligenceService, ISlaBreachService slaBreachService)
    {
        _intelligenceService = intelligenceService;
        _slaBreachService = slaBreachService;
    }

    /// <summary>Platform-wide aggregated summary (counts of at-risk orders/installers/buildings across tenants). Safe aggregates only.</summary>
    [HttpGet("platform-operational-intelligence")]
    [ProducesResponseType(typeof(ApiResponse<OperationalIntelligenceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<OperationalIntelligenceSummaryDto>>> GetPlatformOperationalIntelligence(CancellationToken cancellationToken = default)
    {
        var result = await _intelligenceService.GetPlatformSummaryAsync(cancellationToken);
        return this.Success(result);
    }

    /// <summary>Platform-wide SLA summary (aggregate OnTrack, NearingBreach, Breached, NoSla counts across tenants). Admin only.</summary>
    [HttpGet("platform-sla-summary")]
    [ProducesResponseType(typeof(ApiResponse<SlaBreachSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SlaBreachSummaryDto>>> GetPlatformSlaSummary(CancellationToken cancellationToken = default)
    {
        var result = await _slaBreachService.GetPlatformSummaryAsync(cancellationToken);
        return this.Success(result);
    }
}
