using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Trace;
using CephasOps.Application.Trace.DTOs;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operational trace explorer (alias): unified timeline by CorrelationId, EventId, JobRunId, WorkflowJobId, or Entity.
/// Same as /api/trace with path pattern /api/operational-trace/by-* for runbook and cross-linking.
/// </summary>
[ApiController]
[Route("api/operational-trace")]
[Authorize(Policy = "Jobs")]
public class OperationalTraceController : ControllerBase
{
    private readonly ITraceQueryService _traceQuery;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public OperationalTraceController(ITraceQueryService traceQuery, ICurrentUserService currentUser, ITenantProvider tenantProvider)
    {
        _traceQuery = traceQuery;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }

    private Guid? ScopeCompanyId() => _currentUser.IsSuperAdmin ? null : _tenantProvider.CurrentTenantId;

    private static TraceQueryOptions? Options(int? limit) =>
        limit.HasValue ? new TraceQueryOptions { Limit = Math.Min(Math.Max(1, limit.Value), 2000) } : null;

    /// <summary>Get timeline by correlation ID.</summary>
    [HttpGet("by-correlation/{correlationId}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceTimelineDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TraceTimelineDto>>> GetByCorrelationId(string correlationId, [FromQuery] int? limit, CancellationToken cancellationToken = default)
    {
        var timeline = await _traceQuery.GetByCorrelationIdAsync(correlationId, ScopeCompanyId(), Options(limit), cancellationToken);
        return this.Success(timeline);
    }

    /// <summary>Get timeline by event ID.</summary>
    [HttpGet("by-event/{eventId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceTimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TraceTimelineDto>>> GetByEventId(Guid eventId, [FromQuery] int? limit, CancellationToken cancellationToken = default)
    {
        var timeline = await _traceQuery.GetByEventIdAsync(eventId, ScopeCompanyId(), Options(limit), cancellationToken);
        if (timeline == null) return NotFound();
        return this.Success(timeline);
    }

    /// <summary>Get timeline by job run ID.</summary>
    [HttpGet("by-job-run/{jobRunId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceTimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TraceTimelineDto>>> GetByJobRunId(Guid jobRunId, [FromQuery] int? limit, CancellationToken cancellationToken = default)
    {
        var timeline = await _traceQuery.GetByJobRunIdAsync(jobRunId, ScopeCompanyId(), Options(limit), cancellationToken);
        if (timeline == null) return NotFound();
        return this.Success(timeline);
    }

    /// <summary>Get timeline by workflow job ID.</summary>
    [HttpGet("by-workflow-job/{workflowJobId:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceTimelineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TraceTimelineDto>>> GetByWorkflowJobId(Guid workflowJobId, [FromQuery] int? limit, CancellationToken cancellationToken = default)
    {
        var timeline = await _traceQuery.GetByWorkflowJobIdAsync(workflowJobId, ScopeCompanyId(), Options(limit), cancellationToken);
        if (timeline == null) return NotFound();
        return this.Success(timeline);
    }

    /// <summary>Get timeline by entity (EntityType + EntityId).</summary>
    [HttpGet("by-entity")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceTimelineDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TraceTimelineDto>>> GetByEntity(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest("entityType is required.");
        var timeline = await _traceQuery.GetByEntityAsync(entityType.Trim(), entityId, ScopeCompanyId(), Options(limit), cancellationToken);
        return this.Success(timeline);
    }

    /// <summary>Get minimal trace metrics for the last 24h (or custom window). Company-scoped.</summary>
    [HttpGet("metrics")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<TraceMetricsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TraceMetricsDto>>> GetMetrics(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        var end = toUtc ?? DateTime.UtcNow;
        var start = fromUtc ?? end.AddHours(-24);
        if (start >= end) start = end.AddHours(-24);
        var metrics = await _traceQuery.GetMetricsAsync(start, end, ScopeCompanyId(), cancellationToken);
        return this.Success(metrics);
    }
}
