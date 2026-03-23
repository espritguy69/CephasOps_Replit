using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operational event endpoints: dead-letter/failed/pending listing and safe replay (requeue dead-letter).
/// Minimal metadata by default; full payload via event-store when needed.
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize(Policy = "Jobs")]
public class EventsController : ControllerBase
{
    public const int MaxPageSize = 100;

    private readonly IEventStoreQueryService _queryService;
    private readonly IEventReplayService _replayService;
    private readonly IEventBulkReplayService _bulkReplayService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventStoreQueryService queryService,
        IEventReplayService replayService,
        IEventBulkReplayService bulkReplayService,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<EventsController> logger)
    {
        _queryService = queryService;
        _replayService = replayService;
        _bulkReplayService = bulkReplayService;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    private static EventStoreFilterDto BuildFilter(
        string status,
        Guid? companyId,
        string? eventType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? retryCountMin,
        int? retryCountMax,
        int page,
        int pageSize)
    {
        return new EventStoreFilterDto
        {
            Status = status,
            CompanyId = companyId,
            EventType = eventType,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            RetryCountMin = retryCountMin,
            RetryCountMax = retryCountMax,
            Page = page,
            PageSize = Math.Clamp(pageSize, 1, MaxPageSize)
        };
    }

    /// <summary>List dead-letter events. Filters: eventType, companyId, fromUtc, toUtc, retryCountMin, retryCountMax.</summary>
    [HttpGet("dead-letter")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListDeadLetter(
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? retryCountMin,
        [FromQuery] int? retryCountMax,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var filter = BuildFilter("DeadLetter", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, page, pageSize);
        var (items, total) = await _queryService.GetEventsAsync(filter, scopeCompanyId, cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>List failed events (Status = Failed). Same filters as dead-letter.</summary>
    [HttpGet("failed")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListFailed(
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? retryCountMin,
        [FromQuery] int? retryCountMax,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var filter = BuildFilter("Failed", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, page, pageSize);
        var (items, total) = await _queryService.GetEventsAsync(filter, scopeCompanyId, cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>List pending events (Status = Pending). Same filters (retry count typically 0).</summary>
    [HttpGet("pending")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListPending(
        [FromQuery] Guid? companyId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? retryCountMin,
        [FromQuery] int? retryCountMax,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<object>("Company scope not allowed.");
        page = Math.Max(1, page);
        var filter = BuildFilter("Pending", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, page, pageSize);
        var (items, total) = await _queryService.GetEventsAsync(filter, scopeCompanyId, cancellationToken);
        return this.Success<object>(new { items, total, page = filter.Page, pageSize = filter.PageSize });
    }

    /// <summary>Requeue a dead-letter event to Pending so the dispatcher can retry it. Only allowed for DeadLetter. RetryCount unchanged. Requires JobsAdmin.</summary>
    [HttpPost("{eventId:guid}/replay")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<EventReplayResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventReplayResult>>> Replay(Guid eventId, CancellationToken cancellationToken = default)
    {
        var result = await _replayService.RequeueDeadLetterToPendingAsync(eventId, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
        if (!result.Success && result.ErrorMessage == "Event not found.")
            return this.NotFound<EventReplayResult>(result.ErrorMessage!);
        if (!result.Success)
            return this.BadRequest<EventReplayResult>(result.ErrorMessage ?? "Replay failed.");
        return this.Success(result);
    }

    // ---------- Bulk actions (Phase 7). Require JobsAdmin. Support dryRun and filters. ----------

    /// <summary>Bulk replay dead-letter events by filter. Dry-run returns count that would be affected.</summary>
    [HttpPost("bulk/replay-dead-letter")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BulkActionResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkActionResult>>> BulkReplayDeadLetter(
        [FromQuery] bool dryRun = false,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int? retryCountMin = null,
        [FromQuery] int? retryCountMax = null,
        [FromQuery] int maxCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<BulkActionResult>("Company scope not allowed.");
        var filter = BuildFilter("DeadLetter", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, 1, Math.Clamp(maxCount, 1, 1000));
        var result = await _bulkReplayService.ReplayDeadLetterByFilterAsync(filter, scopeCompanyId, _currentUser.UserId, dryRun, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Bulk replay failed events (due for retry) by filter.</summary>
    [HttpPost("bulk/replay-failed")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BulkActionResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkActionResult>>> BulkReplayFailed(
        [FromQuery] bool dryRun = false,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int? retryCountMin = null,
        [FromQuery] int? retryCountMax = null,
        [FromQuery] int maxCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<BulkActionResult>("Company scope not allowed.");
        var filter = BuildFilter("Failed", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, 1, Math.Clamp(maxCount, 1, 1000));
        var result = await _bulkReplayService.ReplayFailedByFilterAsync(filter, scopeCompanyId, _currentUser.UserId, dryRun, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Bulk reset stuck Processing events by filter.</summary>
    [HttpPost("bulk/reset-stuck")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BulkActionResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkActionResult>>> BulkResetStuck(
        [FromQuery] bool dryRun = false,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int? retryCountMin = null,
        [FromQuery] int? retryCountMax = null,
        [FromQuery] int maxCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<BulkActionResult>("Company scope not allowed.");
        var filter = BuildFilter("Processing", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, 1, Math.Clamp(maxCount, 1, 1000));
        var result = await _bulkReplayService.ResetStuckByFilterAsync(filter, scopeCompanyId, dryRun, cancellationToken);
        return this.Success(result);
    }

    /// <summary>Bulk cancel pending events by filter (incident control).</summary>
    [HttpPost("bulk/cancel-pending")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BulkActionResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkActionResult>>> BulkCancelPending(
        [FromQuery] bool dryRun = false,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int? retryCountMin = null,
        [FromQuery] int? retryCountMax = null,
        [FromQuery] int maxCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        if (scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId)
            return this.Forbidden<BulkActionResult>("Company scope not allowed.");
        var filter = BuildFilter("Pending", companyId, eventType, fromUtc, toUtc, retryCountMin, retryCountMax, 1, Math.Clamp(maxCount, 1, 1000));
        var result = await _bulkReplayService.CancelPendingByFilterAsync(filter, scopeCompanyId, _currentUser.UserId, dryRun, cancellationToken);
        return this.Success(result);
    }
}
