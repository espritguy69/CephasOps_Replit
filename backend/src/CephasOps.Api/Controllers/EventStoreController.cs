using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Lineage;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Event store: list events, failed/dead-letter, dashboard, retry, replay. Company-scoped for non–global admins.
/// </summary>
[ApiController]
[Route("api/event-store")]
[Authorize(Policy = "Jobs")]
public class EventStoreController : ControllerBase
{
    public const int MaxPageSize = 100;

    private readonly IEventStoreQueryService _queryService;
    private readonly IEventBusObservabilityService _observability;
    private readonly IEventReplayService _replayService;
    private readonly IEventReplayPolicy _replayPolicy;
    private readonly IEventLineageService? _lineageService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EventStoreController> _logger;

    public EventStoreController(
        IEventStoreQueryService queryService,
        IEventBusObservabilityService observability,
        IEventReplayService replayService,
        IEventReplayPolicy replayPolicy,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<EventStoreController> logger,
        IEventLineageService? lineageService = null)
    {
        _queryService = queryService;
        _observability = observability;
        _replayService = replayService;
        _replayPolicy = replayPolicy;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _lineageService = lineageService;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>List events with optional filters.</summary>
    [HttpGet("events")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListEvents(
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] string? status,
        [FromQuery] string? correlationId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var filter = new EventStoreFilterDto
        {
            CompanyId = companyId,
            EventType = eventType,
            Status = status,
            CorrelationId = correlationId,
            EntityType = entityType,
            EntityId = entityId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize)
        };
        var (items, total) = await _queryService.GetEventsAsync(filter, scopeCompanyId, cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>List failed events (Status = Failed).</summary>
    [HttpGet("events/failed")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListFailed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var filter = new EventStoreFilterDto { Status = "Failed", Page = page, PageSize = Math.Clamp(pageSize, 1, MaxPageSize) };
        var (items, total) = await _queryService.GetEventsAsync(filter, ScopeCompanyId(), cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize = filter.PageSize });
    }

    /// <summary>List dead-letter events (Status = DeadLetter).</summary>
    [HttpGet("events/dead-letter")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListDeadLetter(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var filter = new EventStoreFilterDto { Status = "DeadLetter", Page = page, PageSize = Math.Clamp(pageSize, 1, MaxPageSize) };
        var (items, total) = await _queryService.GetEventsAsync(filter, ScopeCompanyId(), cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize = filter.PageSize });
    }

    /// <summary>Get event by id.</summary>
    [HttpGet("events/{eventId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventStoreDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventStoreDetailDto>>> GetEvent(Guid eventId, CancellationToken cancellationToken = default)
    {
        var detail = await _queryService.GetByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken);
        if (detail == null) return NotFound();
        return this.Success(detail);
    }

    /// <summary>Get execution attempt history for an event (Phase 7).</summary>
    [HttpGet("events/{eventId:guid}/attempt-history")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EventStoreAttemptHistoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EventStoreAttemptHistoryItemDto>>>> GetAttemptHistory(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventExists = await _queryService.GetByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken) != null;
        if (!eventExists) return NotFound();
        var list = await _queryService.GetAttemptHistoryByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken);
        return this.Success<IReadOnlyList<EventStoreAttemptHistoryItemDto>>(list);
    }

    /// <summary>Get related JobRuns (same EventId or CorrelationId) and WorkflowJobs (same CorrelationId) for traceability.</summary>
    [HttpGet("events/{eventId:guid}/related-links")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventStoreRelatedLinksDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventStoreRelatedLinksDto>>> GetRelatedLinks(Guid eventId, CancellationToken cancellationToken = default)
    {
        var links = await _queryService.GetRelatedLinksAsync(eventId, ScopeCompanyId(), cancellationToken);
        if (links == null) return NotFound();
        return this.Success(links);
    }

    /// <summary>Dashboard metrics for event store.</summary>
    [HttpGet("dashboard")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventStoreDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<EventStoreDashboardDto>>> GetDashboard(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var dashboard = await _queryService.GetDashboardAsync(fromUtc, toUtc, ScopeCompanyId(), cancellationToken);
        return this.Success(dashboard);
    }

    /// <summary>Retry a failed or dead-letter event (re-dispatch to current handlers). Requires JobsAdmin.</summary>
    [HttpPost("events/{eventId:guid}/retry")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<EventReplayResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventReplayResult>>> Retry(Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = await _replayService.RetryAsync(eventId, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
        if (!result.Success && result.ErrorMessage == "Event not found.")
            return this.NotFound<EventReplayResult>(result.ErrorMessage!);
        if (!result.Success)
            return this.BadRequest<EventReplayResult>(result.ErrorMessage ?? "Retry failed.");
        return this.Success(result);
    }

    /// <summary>Replay an event only if its type is allowed by replay policy. Requires JobsAdmin.</summary>
    [HttpPost("events/{eventId:guid}/replay")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<EventReplayResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventReplayResult>>> Replay(Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = await _replayService.ReplayAsync(eventId, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
        if (!result.Success && result.ErrorMessage == "Event not found.")
            return this.NotFound<EventReplayResult>(result.ErrorMessage!);
        if (!result.Success && !string.IsNullOrEmpty(result.BlockedReason))
            return this.BadRequest<EventReplayResult>(result.BlockedReason);
        if (!result.Success)
            return this.BadRequest<EventReplayResult>(result.ErrorMessage ?? "Replay failed.");
        return this.Success(result);
    }

    /// <summary>Check whether an event type is allowed for replay.</summary>
    [HttpGet("replay-policy/{eventType}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<object>> GetReplayPolicy(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return this.BadRequest<object>("eventType is required.");
        var allowed = _replayPolicy.IsReplayAllowed(eventType);
        var blocked = _replayPolicy.IsReplayBlocked(eventType);
        return this.Success<object>(new { eventType, allowed, blocked });
    }

    // ---------- Event Bus Observability (admin diagnostics) ----------

    /// <summary>Recent handler processing with optional filters. Bounded, paginated.</summary>
    [HttpGet("observability/processing")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListProcessingLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool failedOnly = false,
        [FromQuery] Guid? eventId = null,
        [FromQuery] Guid? replayOperationId = null,
        [FromQuery] string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var filter = new EventProcessingLogFilterDto
        {
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize),
            FailedOnly = failedOnly,
            EventId = eventId,
            ReplayOperationId = replayOperationId,
            CorrelationId = correlationId
        };
        var (items, total) = await _observability.GetRecentProcessingLogAsync(filter, ScopeCompanyId(), cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>Handler processing rows for a single event.</summary>
    [HttpGet("events/{eventId:guid}/observability/processing")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetEventProcessing(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventExists = await _queryService.GetByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken) != null;
        if (!eventExists) return NotFound();
        var list = await _observability.GetProcessingLogByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken);
        return this.Success<object>(list);
    }

    /// <summary>Event detail with related handler processing (observability).</summary>
    [HttpGet("observability/events/{eventId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventDetailWithProcessingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventDetailWithProcessingDto>>> GetEventDetailWithProcessing(Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = await _observability.GetEventDetailWithProcessingAsync(eventId, ScopeCompanyId(), cancellationToken);
        if (result == null) return NotFound();
        return this.Success(result);
    }

    /// <summary>Get event lineage (correlation tree) by event id. Phase 8.</summary>
    [HttpGet("events/{eventId:guid}/lineage")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventLineageTreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventLineageTreeDto>>> GetEventLineage(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (_lineageService == null) return NotFound();
        var tree = await _lineageService.GetTreeByEventIdAsync(eventId, ScopeCompanyId(), cancellationToken);
        if (tree == null) return NotFound();
        return this.Success(tree);
    }

    /// <summary>Get event lineage by root event id. Phase 8.</summary>
    [HttpGet("lineage/by-root/{rootEventId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventLineageTreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventLineageTreeDto>>> GetLineageByRoot(Guid rootEventId, [FromQuery] int maxNodes = 500, CancellationToken cancellationToken = default)
    {
        if (_lineageService == null) return NotFound();
        var tree = await _lineageService.GetTreeByRootEventIdAsync(rootEventId, ScopeCompanyId(), Math.Clamp(maxNodes, 1, 1000), cancellationToken);
        if (tree == null) return NotFound();
        return this.Success(tree);
    }

    /// <summary>Get event lineage by correlation id. Phase 8.</summary>
    [HttpGet("lineage/by-correlation/{correlationId}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<EventLineageTreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventLineageTreeDto>>> GetLineageByCorrelation(string correlationId, [FromQuery] int maxNodes = 500, CancellationToken cancellationToken = default)
    {
        if (_lineageService == null) return NotFound();
        var tree = await _lineageService.GetTreeByCorrelationIdAsync(correlationId, ScopeCompanyId(), Math.Clamp(maxNodes, 1, 1000), cancellationToken);
        if (tree == null) return NotFound();
        return this.Success(tree);
    }
}
