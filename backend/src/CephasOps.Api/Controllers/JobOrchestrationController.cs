using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operational visibility over JobExecution (Phase 4). Summary and list by status.
/// </summary>
[ApiController]
[Route("api/job-orchestration")]
[Authorize(Policy = "Jobs")]
public class JobOrchestrationController : ControllerBase
{
    private readonly IJobExecutionQueryService _queryService;
    private readonly ILogger<JobOrchestrationController> _logger;

    public JobOrchestrationController(IJobExecutionQueryService queryService, ILogger<JobOrchestrationController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>Get counts by status: Pending, Running, FailedRetryScheduled, DeadLetter, Succeeded.</summary>
    [HttpGet("summary")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<JobExecutionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JobExecutionSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _queryService.GetSummaryAsync(cancellationToken);
        return this.Success(summary);
    }

    /// <summary>List pending jobs (due to run).</summary>
    [HttpGet("pending")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JobExecutionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobExecutionListItemDto>>>> GetPending([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _queryService.GetPendingAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return this.Success(list);
    }

    /// <summary>List running jobs (claimed, lease active).</summary>
    [HttpGet("running")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JobExecutionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobExecutionListItemDto>>>> GetRunning([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _queryService.GetRunningAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return this.Success(list);
    }

    /// <summary>List failed jobs with retry scheduled (NextRunAtUtc in future).</summary>
    [HttpGet("failed-retry-scheduled")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JobExecutionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobExecutionListItemDto>>>> GetFailedRetryScheduled([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _queryService.GetFailedRetryScheduledAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return this.Success(list);
    }

    /// <summary>List dead-letter jobs (terminal failure, no further retries).</summary>
    [HttpGet("dead-letter")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JobExecutionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobExecutionListItemDto>>>> GetDeadLetter([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _queryService.GetDeadLetterAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return this.Success(list);
    }
}
