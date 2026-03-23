using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Application.Billing.Subscription.Services;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Phase 12: SaaS billing plans (read-only for non-admin).</summary>
[ApiController]
[Route("api/billing/plans")]
[Authorize]
public class BillingPlansController : ControllerBase
{
    private readonly IBillingPlanService _planService;

    public BillingPlansController(IBillingPlanService planService) => _planService = planService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BillingPlanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<BillingPlanDto>>>> List(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var list = await _planService.ListAsync(isActive, cancellationToken);
        return this.Success(list);
    }

    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<BillingPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BillingPlanDto>>> GetBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var plan = await _planService.GetBySlugAsync(slug, cancellationToken);
        if (plan == null) return this.NotFound<BillingPlanDto>($"Plan '{slug}' not found");
        return this.Success(plan);
    }
}
