using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Application.Billing.Subscription.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>Phase 12: Tenant subscription management.</summary>
[ApiController]
[Route("api/billing/subscriptions")]
[Authorize]
public class TenantSubscriptionsController : ControllerBase
{
    private readonly ITenantSubscriptionService _subscriptionService;
    private readonly ITenantContext _tenantContext;

    public TenantSubscriptionsController(ITenantSubscriptionService subscriptionService, ITenantContext tenantContext)
    {
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    /// <summary>List subscriptions for a tenant (admin).</summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequirePermission(PermissionCatalog.AdminBillingPlansView)]
    [ProducesResponseType(typeof(ApiResponse<List<TenantSubscriptionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TenantSubscriptionDto>>>> ListByTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _subscriptionService.ListByTenantAsync(tenantId, cancellationToken);
        return this.Success(list);
    }

    /// <summary>Get current tenant's active subscription.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> GetMyActive(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue) return this.NotFound<TenantSubscriptionDto>("No tenant context");
        var sub = await _subscriptionService.GetActiveAsync(tenantId.Value, cancellationToken);
        if (sub == null) return this.NotFound<TenantSubscriptionDto>("No active subscription");
        return this.Success(sub);
    }

    /// <summary>Subscribe current tenant to a plan (or admin for a tenant).</summary>
    [HttpPost("tenant/{tenantId:guid}/subscribe")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequirePermission(PermissionCatalog.AdminBillingPlansEdit)]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> Subscribe(
        Guid tenantId,
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.PlanSlug))
            return BadRequest(ApiResponse<TenantSubscriptionDto>.ErrorResponse("PlanSlug is required."));
        var sub = await _subscriptionService.SubscribeAsync(tenantId, request.PlanSlug!.Trim(), cancellationToken);
        if (sub == null) return this.NotFound<TenantSubscriptionDto>($"Plan '{request.PlanSlug}' not found or subscribe failed.");
        return this.Success(sub);
    }

    /// <summary>Cancel current tenant's subscription.</summary>
    [HttpPost("me/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> CancelMine(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue) return this.NotFound("No tenant context");
        var ok = await _subscriptionService.CancelAsync(tenantId.Value, cancellationToken);
        if (!ok) return this.NotFound("No active subscription to cancel");
        return this.Success(message: "Subscription cancelled.");
    }

    /// <summary>Cancel a tenant's subscription (admin).</summary>
    [HttpPost("tenant/{tenantId:guid}/cancel")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequirePermission(PermissionCatalog.AdminBillingPlansEdit)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> CancelTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var ok = await _subscriptionService.CancelAsync(tenantId, cancellationToken);
        if (!ok) return this.NotFound("No active subscription to cancel");
        return this.Success(message: "Subscription cancelled.");
    }
}

public class SubscribeRequest
{
    public string? PlanSlug { get; set; }
}
