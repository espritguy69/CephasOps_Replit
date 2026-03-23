using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Integration;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Integration.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operator APIs for external integration bus: connectors, outbound deliveries, inbound webhooks, replay.
/// Phase 10.
/// </summary>
[ApiController]
[Route("api/integration")]
[Authorize(Policy = "Jobs")]
public class IntegrationController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IConnectorRegistry _registry;
    private readonly IOutboundDeliveryStore _outboundStore;
    private readonly IInboundWebhookReceiptStore _inboundStore;
    private readonly IOutboundIntegrationBus _outboundBus;
    private readonly IInboundReceiptReplayService _inboundReplayService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<IntegrationController> _logger;

    public IntegrationController(
        IConnectorRegistry registry,
        IOutboundDeliveryStore outboundStore,
        IInboundWebhookReceiptStore inboundStore,
        IOutboundIntegrationBus outboundBus,
        IInboundReceiptReplayService inboundReplayService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<IntegrationController> logger)
    {
        _registry = registry;
        _outboundStore = outboundStore;
        _inboundStore = inboundStore;
        _outboundBus = outboundBus;
        _inboundReplayService = inboundReplayService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>List connector definitions.</summary>
    [HttpGet("connectors")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListConnectors(CancellationToken cancellationToken)
    {
        var list = await _registry.ListDefinitionsAsync(cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items = list }));
    }

    /// <summary>List connector endpoints (optionally by definition or company).</summary>
    [HttpGet("connectors/endpoints")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListEndpoints(
        [FromQuery] Guid? connectorDefinitionId,
        [FromQuery] Guid? companyId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (!_currentUser.IsSuperAdmin && companyId.HasValue && companyId != scope)
            return Forbid();
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var (items, totalCount) = await _registry.ListEndpointsAsync(connectorDefinitionId, companyId ?? scope, skip, take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, totalCount, skip, take }));
    }

    /// <summary>List outbound deliveries with filters.</summary>
    [HttpGet("outbound/deliveries")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListOutboundDeliveries(
        [FromQuery] Guid? connectorEndpointId,
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (!_currentUser.IsSuperAdmin && companyId.HasValue && companyId != scope)
            return Forbid();
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var (items, totalCount) = await _outboundStore.ListAsync(connectorEndpointId, companyId ?? scope, eventType, status, fromUtc, toUtc, skip, take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, totalCount, skip, take }));
    }

    /// <summary>Get single outbound delivery by id.</summary>
    [HttpGet("outbound/deliveries/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<OutboundIntegrationDelivery>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OutboundIntegrationDelivery>>> GetOutboundDelivery(Guid id, CancellationToken cancellationToken)
    {
        var d = await _outboundStore.GetByIdAsync(id, cancellationToken);
        if (d == null)
            return this.NotFound("Delivery not found.");
        if (!_currentUser.IsSuperAdmin && d.CompanyId.HasValue && d.CompanyId != ScopeCompanyId())
            return Forbid();
        return this.Success(d);
    }

    /// <summary>Replay failed or dead-letter outbound deliveries.</summary>
    [HttpPost("outbound/replay")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ReplayOutbound(
        [FromBody] ReplayOutboundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MaxCount <= 0) request.MaxCount = 100;
        if (request.MaxCount > 500) request.MaxCount = 500;
        var scope = ScopeCompanyId();
        if (!_currentUser.IsSuperAdmin && request.CompanyId.HasValue && request.CompanyId != scope)
            return Forbid();
        var result = await _outboundBus.ReplayAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { result.Dispatched, result.Failed, result.Errors }));
    }

    /// <summary>List inbound webhook receipts with filters.</summary>
    [HttpGet("inbound/receipts")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListInboundReceipts(
        [FromQuery] string? connectorKey,
        [FromQuery] Guid? companyId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (!_currentUser.IsSuperAdmin && companyId.HasValue && companyId != scope)
            return Forbid();
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var (items, totalCount) = await _inboundStore.ListAsync(connectorKey, companyId ?? scope, status, fromUtc, toUtc, skip, take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, totalCount, skip, take }));
    }

    /// <summary>Get single inbound webhook receipt by id.</summary>
    [HttpGet("inbound/receipts/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<InboundWebhookReceipt>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InboundWebhookReceipt>>> GetInboundReceipt(Guid id, CancellationToken cancellationToken)
    {
        var r = await _inboundStore.GetByIdAsync(id, cancellationToken);
        if (r == null)
            return this.NotFound("Receipt not found.");
        if (!_currentUser.IsSuperAdmin && r.CompanyId.HasValue && r.CompanyId != ScopeCompanyId())
            return Forbid();
        return this.Success(r);
    }

    /// <summary>Replay (re-run handler) for an inbound receipt in HandlerFailed status.</summary>
    [HttpPost("inbound/receipts/{id:guid}/replay")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ReplayInboundReceipt(Guid id, CancellationToken cancellationToken = default)
    {
        var r = await _inboundStore.GetByIdAsync(id, cancellationToken);
        if (r == null)
            return this.NotFound("Receipt not found.");
        if (!_currentUser.IsSuperAdmin && r.CompanyId.HasValue && r.CompanyId != ScopeCompanyId())
            return Forbid();
        var result = await _inboundReplayService.ReplayAsync(id, cancellationToken);
        if (result.ReceiptNotFound)
            return this.NotFound("Receipt not found.");
        if (result.InvalidStatus || result.NoHandler)
            return BadRequest(result.ErrorMessage ?? "Invalid replay request.");
        if (!result.Success)
            return Ok(ApiResponse<object>.SuccessResponse(new { success = false, errorMessage = result.ErrorMessage }));
        return Ok(ApiResponse<object>.SuccessResponse(new { success = true }));
    }
}
