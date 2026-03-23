using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Scheduler;
using CephasOps.Application.Scheduler.DTOs;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Scheduler diagnostics: polling interval, worker, recent jobs discovered and claim attempts.
/// </summary>
[ApiController]
[Route("api/system/scheduler")]
[Authorize(Policy = "Jobs")]
public class SystemSchedulerController : ControllerBase
{
    private readonly SchedulerDiagnostics _diagnostics;
    private readonly Microsoft.Extensions.Options.IOptions<SchedulerOptions> _options;
    private readonly ILogger<SystemSchedulerController> _logger;

    public SystemSchedulerController(
        SchedulerDiagnostics diagnostics,
        Microsoft.Extensions.Options.IOptions<SchedulerOptions> options,
        ILogger<SystemSchedulerController> logger)
    {
        _diagnostics = diagnostics;
        _options = options;
        _logger = logger;
    }

    /// <summary>Get scheduler status and recent activity.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<SchedulerDiagnosticsDto>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SchedulerDiagnosticsDto>> Get()
    {
        var opts = _options.Value;
        var (discovered, attempts, success, failure) = _diagnostics.GetTotals();
        var dto = new SchedulerDiagnosticsDto
        {
            PollIntervalSeconds = opts.PollIntervalSeconds,
            MaxJobsPerPoll = opts.MaxJobsPerPoll,
            WorkerId = _diagnostics.WorkerId,
            LastPollUtc = _diagnostics.LastPollUtc,
            TotalDiscovered = discovered,
            TotalClaimAttempts = attempts,
            TotalClaimSuccess = success,
            TotalClaimFailure = failure,
            RecentDiscovered = _diagnostics.GetRecentDiscovered(),
            RecentClaimAttempts = _diagnostics.GetRecentClaimAttempts()
                .Select(x => new ClaimAttemptDto { JobId = x.JobId, Success = x.Success })
                .ToList()
        };
        return this.Success(dto);
    }
}
