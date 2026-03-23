using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Rebuild;
using CephasOps.Application.Rebuild.DTOs;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operational State Rebuilder: list targets, preview scope, execute rebuild, get result summary.
/// Admin-only; company-scoped for non-global admins. No side effects (notifications, external integrations).
/// </summary>
[ApiController]
[Route("api/event-store/rebuild")]
[Authorize(Policy = "Jobs")]
public class OperationalRebuildController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IRebuildTargetRegistry _registry;
    private readonly IOperationalRebuildService _rebuildService;
    private readonly IRebuildJobEnqueuer _jobEnqueuer;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OperationalRebuildController> _logger;

    public OperationalRebuildController(
        IRebuildTargetRegistry registry,
        IOperationalRebuildService rebuildService,
        IRebuildJobEnqueuer jobEnqueuer,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<OperationalRebuildController> logger)
    {
        _registry = registry;
        _rebuildService = rebuildService;
        _jobEnqueuer = jobEnqueuer;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    /// <summary>List registered rebuild targets (discoverable).</summary>
    [HttpGet("targets")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RebuildTargetDescriptorDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyList<RebuildTargetDescriptorDto>>> ListTargets()
    {
        var list = _registry.GetAll().Select(t => new RebuildTargetDescriptorDto
        {
            Id = t.Id,
            DisplayName = t.DisplayName,
            Description = t.Description,
            SourceOfTruth = t.SourceOfTruth,
            RebuildStrategy = t.RebuildStrategy,
            ScopeRuleNames = t.ScopeRuleNames,
            OrderingGuarantee = t.OrderingGuarantee,
            IsFullRebuild = t.IsFullRebuild,
            SupportsPreview = t.SupportsPreview,
            Limitations = t.Limitations,
            SupportsResume = t.SupportsResume
        }).ToList();
        return this.Success<IReadOnlyList<RebuildTargetDescriptorDto>>(list);
    }

    /// <summary>Preview rebuild scope and impact (dry-run). No state changes.</summary>
    [HttpPost("preview")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RebuildPreviewResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RebuildPreviewResultDto>>> Preview(
        [FromBody] RebuildRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return this.BadRequest<RebuildPreviewResultDto>("Request body required.");
        request.DryRun = true;

        var result = await _rebuildService.PreviewAsync(request, ScopeCompanyId(), cancellationToken);
        if (result == null)
            return this.BadRequest<RebuildPreviewResultDto>("Unknown target or no runner for target.");
        return this.Success(result);
    }

    /// <summary>Execute rebuild. Use DryRun=true in body for dry-run; false to apply. Use ?async=true to run in background (returns 202 with operation id).</summary>
    [HttpPost("execute")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RebuildExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Execute(
        [FromBody] RebuildRequestDto request,
        [FromQuery] bool async = false,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return this.BadRequest<object>("Request body required.");
        if (request.DryRun && async)
            return this.BadRequest<object>("Cannot enqueue dry-run. Use sync execute with DryRun=true.");

        if (async)
        {
            var operationId = await _rebuildService.EnqueueRebuildAsync(request, ScopeCompanyId(), _currentUser.UserId, cancellationToken);
            _logger.LogInformation("Rebuild enqueued for background execution. RebuildOperationId={OpId}", operationId);
            return new ObjectResult(this.Success<object>(new { rebuildOperationId = operationId, message = "Rebuild queued for background execution." }))
                { StatusCode = StatusCodes.Status202Accepted };
        }

        var result = await _rebuildService.ExecuteAsync(
            request,
            ScopeCompanyId(),
            _currentUser.UserId,
            cancellationToken);

        if (result.RebuildOperationId == Guid.Empty && !string.IsNullOrEmpty(result.ErrorMessage))
            return this.BadRequest<object>(result.ErrorMessage);
        return this.Success<object>(result);
    }

    /// <summary>Resume a PartiallyCompleted or Pending rebuild. Use ?async=true to enqueue and return 202.</summary>
    [HttpPost("operations/{id:guid}/resume")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RebuildExecutionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Resume(
        Guid id,
        [FromQuery] bool async = false,
        [FromQuery] string? rerunReason = null,
        CancellationToken cancellationToken = default)
    {
        var scopeCompanyId = ScopeCompanyId();
        var op = await _rebuildService.GetOperationAsync(id, scopeCompanyId, cancellationToken);
        if (op == null)
            return this.NotFound<object>("Rebuild operation not found.");
        if (op.State != RebuildOperationStates.PartiallyCompleted && op.State != RebuildOperationStates.Pending && !(op.State == RebuildOperationStates.Failed && op.ResumeRequired))
            return this.BadRequest<object>($"Operation cannot be resumed (State={op.State}). Only PartiallyCompleted, Pending, or Failed with ResumeRequired can be resumed.");

        if (async)
        {
            await _rebuildService.EnqueueResumeAsync(id, scopeCompanyId, _currentUser.UserId, rerunReason, cancellationToken);
            _logger.LogInformation("Rebuild resume enqueued. RebuildOperationId={OpId}", id);
            return new ObjectResult(this.Success<object>(new { rebuildOperationId = id, message = "Resume queued for background execution." }))
                { StatusCode = StatusCodes.Status202Accepted };
        }

        var result = await _rebuildService.ExecuteResumeAsync(id, scopeCompanyId, _currentUser.UserId, rerunReason, cancellationToken);
        if (!string.IsNullOrEmpty(result.ErrorMessage) && result.State == RebuildOperationStates.Failed)
            return this.BadRequest<object>(result.ErrorMessage);
        return this.Success<object>(result);
    }

    /// <summary>Get rebuild operation progress (checkpoint, state, counts).</summary>
    [HttpGet("operations/{id:guid}/progress")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RebuildProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RebuildProgressDto>>> GetProgress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var progress = await _rebuildService.GetProgressAsync(id, ScopeCompanyId(), cancellationToken);
        if (progress == null)
            return this.NotFound<RebuildProgressDto>("Rebuild operation not found.");
        return this.Success(progress);
    }

    /// <summary>Get rebuild operation result summary by id.</summary>
    [HttpGet("operations/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RebuildOperationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RebuildOperationSummaryDto>>> GetOperation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var op = await _rebuildService.GetOperationAsync(id, ScopeCompanyId(), cancellationToken);
        if (op == null)
            return this.NotFound<RebuildOperationSummaryDto>("Rebuild operation not found.");
        return this.Success(op);
    }

    /// <summary>List rebuild operations (audit history). Optional filter by state and target.</summary>
    [HttpGet("operations")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListOperations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? state = null,
        [FromQuery] string? rebuildTargetId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var (items, total) = await _rebuildService.ListOperationsAsync(page, size, ScopeCompanyId(), state, rebuildTargetId, cancellationToken);
        return this.Success<object>(new { items, total, page, pageSize = size });
    }
}
