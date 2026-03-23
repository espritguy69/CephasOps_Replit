using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events.Ledger;
using CephasOps.Application.Events.Ledger.DTOs;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Event Ledger: append-only operational ledger. List entries, get by id, list families, ledger-derived timeline.
/// </summary>
[ApiController]
[Route("api/event-store/ledger")]
[Authorize(Policy = "Jobs")]
public class EventLedgerController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly ILedgerQueryService _ledgerQuery;
    private readonly ILedgerFamilyRegistry _familyRegistry;
    private readonly IWorkflowTransitionTimelineFromLedger _timelineFromLedger;
    private readonly IOrderTimelineFromLedger _orderTimelineFromLedger;
    private readonly IUnifiedOrderHistoryFromLedger _unifiedOrderHistory;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public EventLedgerController(
        ILedgerQueryService ledgerQuery,
        ILedgerFamilyRegistry familyRegistry,
        IWorkflowTransitionTimelineFromLedger timelineFromLedger,
        IOrderTimelineFromLedger orderTimelineFromLedger,
        IUnifiedOrderHistoryFromLedger unifiedOrderHistory,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider)
    {
        _ledgerQuery = ledgerQuery;
        _familyRegistry = familyRegistry;
        _timelineFromLedger = timelineFromLedger;
        _orderTimelineFromLedger = orderTimelineFromLedger;
        _unifiedOrderHistory = unifiedOrderHistory;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>List supported ledger families with ordering metadata.</summary>
    [HttpGet("families")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LedgerFamilyDescriptorDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<LedgerFamilyDescriptorDto>>> ListFamilies()
    {
        var list = _familyRegistry.GetAll();
        return this.Success(list);
    }

    /// <summary>List ledger entries with optional filters.</summary>
    [HttpGet("entries")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListEntries(
        [FromQuery] Guid? companyId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] string? ledgerFamily,
        [FromQuery] DateTime? fromOccurredUtc,
        [FromQuery] DateTime? toOccurredUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (scope.HasValue && companyId.HasValue && companyId != scope)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var (items, total) = await _ledgerQuery.ListAsync(
            companyId ?? scope,
            entityType,
            entityId,
            ledgerFamily,
            fromOccurredUtc,
            toOccurredUtc,
            page,
            size,
            cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize = size });
    }

    /// <summary>Get a single ledger entry by id.</summary>
    [HttpGet("entries/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<LedgerEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LedgerEntryDto>>> GetEntry(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _ledgerQuery.GetByIdAsync(id, ScopeCompanyId(), cancellationToken);
        if (entry == null)
            return this.NotFound<LedgerEntryDto>("Ledger entry not found.");
        return this.Success(entry);
    }

    /// <summary>Ledger-derived projection: workflow transition timeline for an entity (from ledger entries).</summary>
    [HttpGet("timeline/workflow-transition")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkflowTransitionTimelineItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowTransitionTimelineItemDto>>>> GetWorkflowTransitionTimeline(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromOccurredUtc,
        [FromQuery] DateTime? toOccurredUtc,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return this.BadRequest<IReadOnlyList<WorkflowTransitionTimelineItemDto>>("entityType is required.");
        var scope = ScopeCompanyId();
        if (scope.HasValue && companyId.HasValue && companyId != scope)
            return this.Forbidden<IReadOnlyList<WorkflowTransitionTimelineItemDto>>("Company scope not allowed.");
        var list = await _timelineFromLedger.GetByEntityAsync(
            entityType,
            entityId,
            companyId ?? scope,
            fromOccurredUtc,
            toOccurredUtc,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
        return this.Success(list);
    }

    /// <summary>Ledger-derived projection: order timeline for an order (from OrderLifecycle ledger entries).</summary>
    [HttpGet("timeline/order")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderTimelineItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderTimelineItemDto>>>> GetOrderTimeline(
        [FromQuery] Guid orderId,
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromOccurredUtc,
        [FromQuery] DateTime? toOccurredUtc,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (scope.HasValue && companyId.HasValue && companyId != scope)
            return this.Forbidden<IReadOnlyList<OrderTimelineItemDto>>("Company scope not allowed.");
        var list = await _orderTimelineFromLedger.GetByOrderIdAsync(
            orderId,
            companyId ?? scope,
            fromOccurredUtc,
            toOccurredUtc,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
        return this.Success(list);
    }

    /// <summary>Unified operational history for an order (WorkflowTransition + OrderLifecycle ledger entries merged by time).</summary>
    [HttpGet("timeline/unified-order")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnifiedOrderHistoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnifiedOrderHistoryItemDto>>>> GetUnifiedOrderHistory(
        [FromQuery] Guid orderId,
        [FromQuery] Guid? companyId,
        [FromQuery] DateTime? fromOccurredUtc,
        [FromQuery] DateTime? toOccurredUtc,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeCompanyId();
        if (scope.HasValue && companyId.HasValue && companyId != scope)
            return this.Forbidden<IReadOnlyList<UnifiedOrderHistoryItemDto>>("Company scope not allowed.");
        var list = await _unifiedOrderHistory.GetByOrderIdAsync(
            orderId,
            companyId ?? scope,
            fromOccurredUtc,
            toOccurredUtc,
            Math.Clamp(limit, 1, 500),
            cancellationToken);
        return this.Success(list);
    }
}
