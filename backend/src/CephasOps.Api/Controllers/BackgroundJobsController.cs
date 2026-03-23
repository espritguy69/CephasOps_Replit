using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Authorization;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Background jobs monitoring and management. Restricted to SuperAdmin and Admin.
/// </summary>
[ApiController]
[Route("api/background-jobs")]
[Authorize(Policy = "Jobs")]
public class BackgroundJobsController : ControllerBase
{
    /// <summary>Global fallback stuck threshold in seconds when job type has no definition.</summary>
    public const int DefaultStuckThresholdSecondsFallback = 7200; // 2 hours
    /// <summary>Max page size for list endpoints.</summary>
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IJobDefinitionProvider _jobDefinitionProvider;
    private readonly IJobRunRetentionService _retentionService;
    private readonly ILogger<BackgroundJobsController> _logger;

    public BackgroundJobsController(
        ApplicationDbContext context,
        IHostApplicationLifetime lifetime,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IJobDefinitionProvider jobDefinitionProvider,
        IJobRunRetentionService retentionService,
        ILogger<BackgroundJobsController> logger)
    {
        _context = context;
        _lifetime = lifetime;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _jobDefinitionProvider = jobDefinitionProvider;
        _retentionService = retentionService;
        _logger = logger;
    }

    private async Task<HashSet<string>> GetRetryAllowedJobTypesAsync(CancellationToken cancellationToken)
    {
        var all = await _jobDefinitionProvider.GetAllAsync(cancellationToken);
        return all.Where(d => d.RetryAllowed).Select(d => d.JobType).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private IQueryable<JobRun> ApplyCompanyFilter(IQueryable<JobRun> query)
    {
        if (_currentUserService.IsSuperAdmin)
            return query;
        var companyId = _tenantProvider.CurrentTenantId;
        if (companyId.HasValue)
            return query.Where(r => r.CompanyId == companyId.Value);
        return query;
    }

    /// <summary>
    /// Get background worker health status
    /// </summary>
    /// <returns>Health status of background workers</returns>
    [HttpGet("health")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> GetHealthStatus()
    {
        try
        {
            // Check if application is running
            var isRunning = !_lifetime.ApplicationStopping.IsCancellationRequested;

            // Get recent job executions (last 5 minutes)
            var recentJobs = await _context.BackgroundJobs
                .Where(j => j.UpdatedAt >= DateTime.UtcNow.AddMinutes(-5))
                .OrderByDescending(j => j.UpdatedAt)
                .Take(10)
                .Select(j => new
                {
                    j.Id,
                    j.JobType,
                    j.State,
                    j.StartedAt,
                    j.CompletedAt,
                    j.UpdatedAt,
                    j.LastError
                })
                .ToListAsync();

            // Get email polling status
            var emailAccounts = await _context.EmailAccounts
                .Where(ea => ea.IsActive)
                .Select(ea => new
                {
                    ea.Id,
                    ea.Name,
                    Email = ea.Username, // EmailAccount uses Username field for email address
                    ea.LastPolledAt,
                    ea.IsActive,
                    MinutesSinceLastPoll = ea.LastPolledAt != null 
                        ? (int?)(DateTime.UtcNow - ea.LastPolledAt.Value).TotalMinutes
                        : (int?)null
                })
                .ToListAsync();

            // Determine overall health
            var allEmailAccountsPolledRecently = emailAccounts.All(ea => 
                ea.LastPolledAt == null || // Never polled yet (acceptable)
                (ea.MinutesSinceLastPoll != null && ea.MinutesSinceLastPoll < 10)); // Polled within last 10 minutes

            var hasRecentActivity = recentJobs.Any() || emailAccounts.Any(ea => ea.LastPolledAt != null);

            var overallHealth = isRunning && (allEmailAccountsPolledRecently || !emailAccounts.Any()) 
                ? "Healthy" 
                : "Degraded";

            var result = new
            {
                status = overallHealth,
                timestamp = DateTime.UtcNow,
                application = new
                {
                    isRunning,
                    uptime = DateTime.UtcNow // Simplified - would need app start time for accurate uptime
                },
                backgroundWorker = new
                {
                    isRunning,
                    recentJobsCount = recentJobs.Count,
                    recentJobs = recentJobs.Take(5)
                },
                emailPolling = new
                {
                    activeAccountsCount = emailAccounts.Count,
                    accounts = emailAccounts.Select(ea => new
                    {
                        ea.Id,
                        ea.Name,
                        ea.Email,
                        ea.LastPolledAt,
                        minutesSinceLastPoll = ea.MinutesSinceLastPoll,
                        status = ea.LastPolledAt == null 
                            ? "Never Polled" 
                            : ea.MinutesSinceLastPoll < 5 
                                ? "Healthy" 
                                : ea.MinutesSinceLastPoll < 10 
                                    ? "Warning" 
                                    : "Stale"
                    })
                }
            };

            return this.Success<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting background jobs health status");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to get health status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get background jobs summary
    /// </summary>
    /// <returns>Summary of background jobs</returns>
    [HttpGet("summary")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> GetJobsSummary()
    {
        try
        {
            var summary = await _context.BackgroundJobs
                .GroupBy(j => j.State)
                .Select(g => new
                {
                    state = g.Key.ToString(),
                    count = g.Count()
                })
                .ToListAsync();

            var totalJobs = await _context.BackgroundJobs.CountAsync();
            var runningJobs = await _context.BackgroundJobs.CountAsync(j => j.State == Domain.Workflow.Entities.BackgroundJobState.Running);
            var queuedJobs = await _context.BackgroundJobs.CountAsync(j => j.State == Domain.Workflow.Entities.BackgroundJobState.Queued);
            var failedJobs = await _context.BackgroundJobs.CountAsync(j => j.State == Domain.Workflow.Entities.BackgroundJobState.Failed);

            var result = new
            {
                totalJobs,
                runningJobs,
                queuedJobs,
                failedJobs,
                summary
            };

            return this.Success<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting background jobs summary");
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to get jobs summary: {ex.Message}"));
        }
    }

    /// <summary>
    /// List job runs with optional filters (date range, company, job type, status, trigger source, correlation id).
    /// </summary>
    [HttpGet("job-runs")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<JobRunDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListJobRuns(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? jobType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? triggerSource = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var query = ApplyCompanyFilter(_context.JobRuns.AsNoTracking());
            if (fromUtc.HasValue)
                query = query.Where(r => r.StartedAtUtc >= fromUtc.Value);
            if (toUtc.HasValue)
                query = query.Where(r => r.StartedAtUtc <= toUtc.Value);
            if (companyId.HasValue)
                query = query.Where(r => r.CompanyId == companyId.Value);
            if (!string.IsNullOrWhiteSpace(jobType))
                query = query.Where(r => r.JobType == jobType);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(triggerSource))
                query = query.Where(r => r.TriggerSource == triggerSource);
            if (!string.IsNullOrWhiteSpace(correlationId))
                query = query.Where(r => r.CorrelationId != null && r.CorrelationId.Contains(correlationId));

            var total = await query.CountAsync(cancellationToken);
            var runs = await query
                .OrderByDescending(r => r.StartedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var items = runs.Select(r => MapToDto(r, retryAllowed)).ToList();

            var result = new { items, total, page, pageSize };
            return this.Success<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing job runs");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// List failed job runs (status Failed or DeadLetter).
    /// </summary>
    [HttpGet("job-runs/failed")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListFailedJobRuns(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = ApplyCompanyFilter(_context.JobRuns.AsNoTracking())
                .Where(r => r.Status == "Failed" || r.Status == "DeadLetter")
                .OrderByDescending(r => r.CompletedAtUtc ?? r.StartedAtUtc);

            var cappedLimit = Math.Clamp(limit, 1, 500);
            var runs = await query.Take(cappedLimit).ToListAsync(cancellationToken);
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var items = runs.Select(r => MapToDto(r, retryAllowed)).ToList();
            return this.Success<object>(new { items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing failed job runs");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get running job runs (status Running).
    /// </summary>
    [HttpGet("job-runs/running")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListRunningJobRuns(CancellationToken cancellationToken = default)
    {
        try
        {
            var runs = await ApplyCompanyFilter(_context.JobRuns.AsNoTracking())
                .Where(r => r.Status == "Running")
                .OrderBy(r => r.StartedAtUtc)
                .Take(500)
                .ToListAsync(cancellationToken);
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var items = runs.Select(r => MapToDto(r, retryAllowed)).ToList();
            return this.Success<object>(new { items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing running job runs");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get job run by id.
    /// </summary>
    [HttpGet("job-runs/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<JobRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetJobRun(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var runEntity = await ApplyCompanyFilter(_context.JobRuns.AsNoTracking()).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (runEntity == null)
                return NotFound(ApiResponse.ErrorResponse("Job run not found"));
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var run = MapToDto(runEntity, retryAllowed);
            return this.Success<object>(run);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job run {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get dashboard metrics (counts, success rate, by job type, recent failures).
    /// </summary>
    [HttpGet("job-runs/dashboard")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<JobRunDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetJobRunsDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var baseQuery = ApplyCompanyFilter(_context.JobRuns.AsNoTracking());
            var now = DateTime.UtcNow;
            var last24 = now.AddHours(-24);

            var last24Query = baseQuery.Where(r => r.StartedAtUtc >= last24);
            var total24 = await last24Query.CountAsync(cancellationToken);
            var succeeded24 = await last24Query.CountAsync(r => r.Status == "Succeeded", cancellationToken);
            var failed24 = await last24Query.CountAsync(r => r.Status == "Failed" || r.Status == "DeadLetter", cancellationToken);
            var successRate = total24 > 0 ? (double)succeeded24 / total24 * 100 : 0;

            var runningNow = await baseQuery.CountAsync(r => r.Status == "Running", cancellationToken);
            var queuedNow = await _context.BackgroundJobs.CountAsync(j => j.State == BackgroundJobState.Queued, cancellationToken);

            // Stuck: running longer than JobDefinition.DefaultStuckThresholdSeconds (per type) or global fallback
            var runningRuns = await baseQuery.Where(r => r.Status == "Running").Select(r => new { r.StartedAtUtc, r.JobType }).ToListAsync(cancellationToken);
            var definitions = await _jobDefinitionProvider.GetAllAsync(cancellationToken);
            var thresholdByJobType = definitions.ToDictionary(d => d.JobType, d => d.DefaultStuckThresholdSeconds ?? DefaultStuckThresholdSecondsFallback, StringComparer.OrdinalIgnoreCase);
            var stuckCount = runningRuns.Count(r => (now - r.StartedAtUtc).TotalSeconds > (thresholdByJobType.TryGetValue(r.JobType, out var sec) ? sec : DefaultStuckThresholdSecondsFallback));

            var byJobType = await last24Query
                .GroupBy(r => r.JobType)
                .Select(g => new JobTypeMetricDto
                {
                    JobType = g.Key,
                    Total = g.Count(),
                    Succeeded = g.Count(r => r.Status == "Succeeded"),
                    Failed = g.Count(r => r.Status == "Failed" || r.Status == "DeadLetter"),
                    AvgDurationMs = g.Where(r => r.DurationMs != null).Average(r => (double?)r.DurationMs!)
                })
                .ToListAsync(cancellationToken);

            // P95 duration (last 24h completed runs; cap sample for efficiency)
            var durationMsList = await last24Query
                .Where(r => r.DurationMs != null)
                .OrderBy(r => r.Id)
                .Take(50_000)
                .Select(r => r.DurationMs!.Value)
                .ToListAsync(cancellationToken);
            long? p95DurationMs = null;
            if (durationMsList.Count > 0)
            {
                durationMsList.Sort();
                var p95Index = (int)Math.Min((durationMsList.Count - 1) * 0.95, durationMsList.Count - 1);
                p95DurationMs = durationMsList[p95Index];
            }

            var retryCountLast24 = await last24Query.CountAsync(r => r.RetryCount > 0, cancellationToken);
            var jobsPerHour = total24 / 24.0;
            var retryRate = total24 > 0 ? (double)retryCountLast24 / total24 * 100 : 0;

            var topFailingCompanies = await last24Query
                .Where(r => (r.Status == "Failed" || r.Status == "DeadLetter") && r.CompanyId != null)
                .GroupBy(r => r.CompanyId!.Value)
                .Select(g => new { CompanyId = g.Key, FailedCount = g.Count() })
                .OrderByDescending(x => x.FailedCount)
                .Take(10)
                .ToListAsync(cancellationToken);
            var companyIds = topFailingCompanies.Select(x => x.CompanyId).ToList();
            var companyNames = await _context.Companies
                .AsNoTracking()
                .Where(c => companyIds.Contains(c.Id))
                .Select(c => new { c.Id, Name = c.ShortName ?? c.LegalName })
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
            var topFailingCompaniesDto = topFailingCompanies
                .Select(x => new TopFailingCompanyDto { CompanyId = x.CompanyId, CompanyName = companyNames.GetValueOrDefault(x.CompanyId), FailedCount = x.FailedCount })
                .ToList();

            var topFailingJobTypes = byJobType
                .Where(x => x.Failed > 0)
                .OrderByDescending(x => x.Failed)
                .Take(10)
                .Select(x => new TopFailingJobTypeDto { JobType = x.JobType, FailedCount = x.Failed })
                .ToList();

            var failureRuns = await baseQuery
                .Where(r => r.Status == "Failed" || r.Status == "DeadLetter")
                .OrderByDescending(r => r.CompletedAtUtc ?? r.StartedAtUtc)
                .Take(10)
                .ToListAsync(cancellationToken);
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var recentFailures = failureRuns.Select(r => MapToDto(r, retryAllowed)).ToList();

            var dashboard = new JobRunDashboardDto
            {
                TotalRunsLast24h = total24,
                SucceededLast24h = succeeded24,
                FailedLast24h = failed24,
                SuccessRateLast24h = Math.Round(successRate, 2),
                RunningNow = runningNow,
                StuckCount = stuckCount,
                QueuedNow = queuedNow,
                P95DurationMsLast24h = p95DurationMs,
                JobsPerHourLast24h = Math.Round(jobsPerHour, 2),
                RetryRateLast24h = Math.Round(retryRate, 2),
                ByJobType = byJobType,
                TopFailingCompanies = topFailingCompaniesDto,
                TopFailingJobTypes = topFailingJobTypes,
                RecentFailures = recentFailures
            };
            return this.Success<object>(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job runs dashboard");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get job runs that are running longer than expected (stuck). Uses JobDefinition.DefaultStuckThresholdSeconds per job type, with optional fallback hours.
    /// </summary>
    [HttpGet("job-runs/stuck")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListStuckJobRuns(
        [FromQuery] double olderThanHours = 2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var fallbackSeconds = (int)(olderThanHours * 3600);
            var runningRuns = await ApplyCompanyFilter(_context.JobRuns.AsNoTracking())
                .Where(r => r.Status == "Running")
                .OrderBy(r => r.StartedAtUtc)
                .ToListAsync(cancellationToken);
            var definitions = await _jobDefinitionProvider.GetAllAsync(cancellationToken);
            var thresholdByJobType = definitions.ToDictionary(d => d.JobType, d => d.DefaultStuckThresholdSeconds ?? fallbackSeconds, StringComparer.OrdinalIgnoreCase);
            var stuck = runningRuns.Where(r => (now - r.StartedAtUtc).TotalSeconds > (thresholdByJobType.TryGetValue(r.JobType, out var sec) ? sec : fallbackSeconds)).ToList();
            var retryAllowed = await GetRetryAllowedJobTypesAsync(cancellationToken);
            var items = stuck.Select(r =>
            {
                var dto = MapToDto(r, retryAllowed);
                dto.EffectiveStuckThresholdSeconds = thresholdByJobType.TryGetValue(r.JobType, out var s) ? s : fallbackSeconds;
                return dto;
            }).ToList();
            return this.Success<object>(new { items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing stuck job runs");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get effective stuck detection thresholds: global fallback and per job type (from JobDefinition).
    /// </summary>
    [HttpGet("job-runs/stuck-thresholds")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetStuckThresholds(CancellationToken cancellationToken = default)
    {
        try
        {
            var definitions = await _jobDefinitionProvider.GetAllAsync(cancellationToken);
            var byJobType = definitions.ToDictionary(d => d.JobType, d => d.DefaultStuckThresholdSeconds ?? DefaultStuckThresholdSecondsFallback, StringComparer.OrdinalIgnoreCase);
            return this.Success<object>(new { globalFallbackSeconds = DefaultStuckThresholdSecondsFallback, byJobType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stuck thresholds");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Retry a failed job run (re-queues the underlying BackgroundJob when eligible).
    /// </summary>
    [HttpPost("job-runs/{id:guid}/retry")]
    [RequirePermission(PermissionCatalog.JobsRun)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RetryJobRun(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var run = await ApplyCompanyFilter(_context.JobRuns.AsNoTracking())
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (run == null)
                return NotFound(ApiResponse.ErrorResponse("Job run not found"));

            if (run.Status != "Failed" && run.Status != "DeadLetter")
                return BadRequest(ApiResponse.ErrorResponse("Only failed or dead-letter runs can be retried"));

            if (!run.BackgroundJobId.HasValue)
                return BadRequest(ApiResponse.ErrorResponse("This run has no associated queue job to retry"));

            var definition = await _jobDefinitionProvider.GetByJobTypeAsync(run.JobType, cancellationToken);
            if (definition == null || !definition.RetryAllowed)
                return BadRequest(ApiResponse.ErrorResponse($"Job type '{run.JobType}' is not retryable from the UI"));

            // Tenant-safe: scope job by same company as the run so FindAsync bypass is avoided
            var job = run.CompanyId.HasValue && run.CompanyId.Value != Guid.Empty
                ? await _context.BackgroundJobs.FirstOrDefaultAsync(j => j.Id == run.BackgroundJobId.Value && j.CompanyId == run.CompanyId.Value, cancellationToken)
                : await _context.BackgroundJobs.FirstOrDefaultAsync(j => j.Id == run.BackgroundJobId.Value, cancellationToken);
            if (job == null)
                return NotFound(ApiResponse.ErrorResponse("Associated background job no longer exists"));

            if (job.State != BackgroundJobState.Failed)
                return BadRequest(ApiResponse.ErrorResponse("Associated job is not in Failed state"));

            job.State = BackgroundJobState.Queued;
            job.LastError = null;
            job.ScheduledAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            job.RetriedFromJobRunId = id; // Link retry run to original for ParentJobRunId when processor starts
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job run {JobRunId} retried by user; BackgroundJob {BackgroundJobId} re-queued", id, job.Id);
            return this.Success<object>(new { message = "Job re-queued for retry", backgroundJobId = job.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying job run {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Purge completed job runs older than the specified retention. Requires Jobs.Admin.
    /// </summary>
    [HttpPost("job-runs/purge")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> PurgeJobRuns(
        [FromBody] PurgeJobRunsRequest? request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var olderThanDays = request?.OlderThanDays ?? 90;
            if (olderThanDays < 1 || olderThanDays > 3650)
                return BadRequest(ApiResponse.ErrorResponse("OlderThanDays must be between 1 and 3650."));
            var batchSize = request?.BatchSize ?? JobRunRetentionService.DefaultBatchSize;
            var olderThanUtc = DateTime.UtcNow.AddDays(-olderThanDays);
            var deleted = await _retentionService.PurgeAsync(olderThanUtc, batchSize, cancellationToken);
            _logger.LogInformation("Purged {Count} job runs older than {Days} days", deleted, olderThanDays);
            return this.Success<object>(new { deletedCount = deleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging job runs");
            return StatusCode(500, ApiResponse.ErrorResponse(ex.Message));
        }
    }

    private static JobRunDto MapToDto(JobRun r, HashSet<string> retryAllowedJobTypes)
    {
        return new JobRunDto
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            JobName = r.JobName,
            JobType = r.JobType,
            TriggerSource = r.TriggerSource,
            CorrelationId = r.CorrelationId,
            QueueOrChannel = r.QueueOrChannel,
            PayloadSummary = r.PayloadSummary,
            Status = r.Status,
            StartedAtUtc = r.StartedAtUtc,
            CompletedAtUtc = r.CompletedAtUtc,
            DurationMs = r.DurationMs,
            RetryCount = r.RetryCount,
            WorkerNode = r.WorkerNode,
            ErrorCode = r.ErrorCode,
            ErrorMessage = r.ErrorMessage,
            ErrorDetails = r.ErrorDetails,
            InitiatedByUserId = r.InitiatedByUserId,
            ParentJobRunId = r.ParentJobRunId,
            RelatedEntityType = r.RelatedEntityType,
            RelatedEntityId = r.RelatedEntityId,
            BackgroundJobId = r.BackgroundJobId,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc,
            CanRetry = (r.Status == "Failed" || r.Status == "DeadLetter") && r.BackgroundJobId.HasValue && retryAllowedJobTypes.Contains(r.JobType)
        };
    }
}

public class PurgeJobRunsRequest
{
    public int? OlderThanDays { get; set; }
    public int? BatchSize { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
