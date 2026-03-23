using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operational replay: preview, execute, and list replay operations. Admin-only; company-scoped for non-global admins.
/// </summary>
[ApiController]
[Route("api/event-store/replay")]
[Authorize(Policy = "Jobs")]
public class OperationalReplayController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IReplayPreviewService _previewService;
    private readonly IOperationalReplayExecutionService _executionService;
    private readonly IReplayJobEnqueuer _replayJobEnqueuer;
    private readonly IReplayOperationQueryService _queryService;
    private readonly IReplayTargetRegistry _targetRegistry;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OperationalReplayController> _logger;

    public OperationalReplayController(
        IReplayPreviewService previewService,
        IOperationalReplayExecutionService executionService,
        IReplayJobEnqueuer replayJobEnqueuer,
        IReplayOperationQueryService queryService,
        IReplayTargetRegistry targetRegistry,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<OperationalReplayController> logger)
    {
        _previewService = previewService;
        _executionService = executionService;
        _replayJobEnqueuer = replayJobEnqueuer;
        _queryService = queryService;
        _targetRegistry = targetRegistry;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>List registered replay targets (Phase 2).</summary>
    [HttpGet("targets")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReplayTargetDescriptorDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<ReplayTargetDescriptorDto>>> ListTargets()
    {
        var list = _targetRegistry.GetAll();
        return this.Success(list);
    }

    /// <summary>Dry-run preview: matching count, eligible count, blocked reasons, sample events. No handlers executed. Phase 2: ordering, limitations, affected entities.</summary>
    [HttpPost("preview")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ReplayPreviewResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReplayPreviewResultDto>>> Preview(
        [FromBody] ReplayRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null) return this.BadRequest<ReplayPreviewResultDto>("Request body required.");
        var result = await _previewService.PreviewAsync(request, ScopeCompanyId(), cancellationToken);
        return this.Success(result);
    }

    /// <summary>Execute replay for eligible events. Persists ReplayOperation and runs handlers. Requires JobsAdmin. Use ?async=true to queue and return 202.</summary>
    [HttpPost("execute")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<OperationalReplayExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OperationalReplayExecutionResultDto>>> Execute(
        [FromBody] ReplayRequestDto request,
        [FromQuery] bool async = false,
        CancellationToken cancellationToken = default)
    {
        if (request == null) return this.BadRequest<OperationalReplayExecutionResultDto>("Request body required.");
        if (request.DryRun)
            return this.BadRequest<OperationalReplayExecutionResultDto>("Use preview endpoint for dry-run; execute requires DryRun=false.");

        if (async)
        {
            var operationId = await _replayJobEnqueuer.EnqueueReplayAsync(request, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
            _logger.LogInformation("Replay queued for async execution. ReplayOperationId={OpId}", operationId);
            return new ObjectResult(ApiResponse<object>.SuccessResponse(new { replayOperationId = operationId, message = "Replay queued for background execution." }))
                { StatusCode = StatusCodes.Status202Accepted };
        }

        var result = await _executionService.ExecuteAsync(request, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
        if (result.ReplayOperationId == Guid.Empty && !string.IsNullOrEmpty(result.ErrorMessage))
            return this.BadRequest<OperationalReplayExecutionResultDto>(result.ErrorMessage!);
        return this.Success(result);
    }

    /// <summary>List replay operations (audit history).</summary>
    [HttpGet("operations")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListOperations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var (items, total) = await _queryService.ListAsync(page, size, ScopeCompanyId(), cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize = size });
    }

    /// <summary>Get replay operation detail and per-event results.</summary>
    [HttpGet("operations/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ReplayOperationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReplayOperationDetailDto>>> GetOperation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await _queryService.GetByIdAsync(id, ScopeCompanyId(), cancellationToken);
        if (detail == null) return this.NotFound<ReplayOperationDetailDto>("Replay operation not found.");
        return this.Success(detail);
    }

    /// <summary>Phase 2: Progress for an active or resumable replay operation.</summary>
    [HttpGet("operations/{id:guid}/progress")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ReplayOperationProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReplayOperationProgressDto>>> GetProgress(Guid id, CancellationToken cancellationToken)
    {
        var progress = await _queryService.GetProgressAsync(id, ScopeCompanyId(), cancellationToken);
        if (progress == null) return this.NotFound<ReplayOperationProgressDto>("Replay operation not found.");
        return this.Success(progress);
    }

    /// <summary>Phase 2: Resume an interrupted (PartiallyCompleted) or Pending replay. Queues background job when async=true.</summary>
    [HttpPost("operations/{id:guid}/resume")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<OperationalReplayExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Resume(Guid id, [FromQuery] bool async = false, CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        var detail = await _queryService.GetByIdAsync(id, scopeCompanyId, cancellationToken);
        if (detail == null) return this.NotFound<object>("Replay operation not found.");
        if (detail.State != ReplayOperationStates.PartiallyCompleted && detail.State != ReplayOperationStates.Pending)
            return this.BadRequest<object>($"Operation cannot be resumed (State={detail.State}). Only PartiallyCompleted or Pending can be resumed.");

        if (async)
        {
            await _replayJobEnqueuer.EnqueueResumeAsync(id, scopeCompanyId, _currentUser.UserId, cancellationToken);
            _logger.LogInformation("Replay resume queued. ReplayOperationId={OpId}", id);
            return new ObjectResult(ApiResponse<object>.SuccessResponse(new { replayOperationId = id, message = "Resume queued for background execution." }))
                { StatusCode = StatusCodes.Status202Accepted };
        }
        var result = await _executionService.ExecuteByOperationIdAsync(id, scopeCompanyId, _currentUser.UserId, cancellationToken);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
            return this.BadRequest<object>(result.ErrorMessage);
        return this.Success<object>(result);
    }

    /// <summary>Phase 2: Rerun only the failed events from this operation as a new operation. Optional async.</summary>
    [HttpPost("operations/{id:guid}/rerun-failed")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<OperationalReplayExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OperationalReplayExecutionResultDto>>> RerunFailed(Guid id, [FromBody] RerunFailedRequestDto? body = null, CancellationToken cancellationToken = default)
    {
        var result = await _executionService.ExecuteRerunFailedAsync(id, ScopeCompanyId(), _currentUser.UserId, body?.RerunReason, cancellationToken);
        if (result.ReplayOperationId == Guid.Empty && !string.IsNullOrEmpty(result.ErrorMessage))
            return this.BadRequest<OperationalReplayExecutionResultDto>(result.ErrorMessage);
        return this.Success(result);
    }

    /// <summary>Request cancellation. Pending/PartiallyCompleted: cancelled immediately. Running: stop at next checkpoint.</summary>
    [HttpPost("operations/{id:guid}/cancel")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<OperationalReplayExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OperationalReplayExecutionResultDto>>> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        var detail = await _queryService.GetByIdAsync(id, scopeCompanyId, cancellationToken);
        if (detail == null)
            return this.NotFound<OperationalReplayExecutionResultDto>("Replay operation not found.");
        var result = await _executionService.RequestCancelAsync(id, cancellationToken);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            if (result.ReplayOperationId == Guid.Empty) return this.NotFound<OperationalReplayExecutionResultDto>(result.ErrorMessage);
            return this.BadRequest<OperationalReplayExecutionResultDto>(result.ErrorMessage);
        }
        return this.Success(result);
    }
}
