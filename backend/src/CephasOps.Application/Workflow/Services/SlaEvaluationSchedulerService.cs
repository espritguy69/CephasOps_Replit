using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Schedules SLA evaluation jobs per tenant. Enqueues one JobExecution per active company when none pending for that company.
/// All tenant-scoped writes (JobExecution) occur with TenantScope.CurrentTenantId set; JobRunRecorder writes JobRun (not tenant-scoped).
/// </summary>
public class SlaEvaluationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaEvaluationSchedulerService> _logger;
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromMinutes(15);

    public SlaEvaluationSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<SlaEvaluationSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA evaluation scheduler started (interval: {Interval})", SchedulerInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleSlaEvaluationJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SLA evaluation scheduler: {Message}", ex.Message);
            }

            await Task.Delay(SchedulerInterval, stoppingToken);
        }

        _logger.LogInformation("SLA evaluation scheduler stopped");
    }

    private async Task ScheduleSlaEvaluationJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping SLA evaluation scheduling");
            return;
        }

        // Observability: one JobRun per scheduler cycle. Start/Complete/Fail run under platform bypass (JobRun is platform-owned).
        var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            Guid? jobRunId = null;
            if (recorder != null)
            {
                jobRunId = await recorder.StartAsync(new StartJobRunDto
                {
                    JobName = "SLA Evaluation Scheduler",
                    JobType = "SlaEvaluationScheduler",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct);
            }
            try
            {
                var enqueuer = scope.ServiceProvider.GetRequiredService<IJobExecutionEnqueuer>();
                await ScheduleSlaEvaluationJobCoreAsync(context, enqueuer, ct);
                if (jobRunId.HasValue && recorder != null)
                    await recorder.CompleteAsync(jobRunId.Value, ct);
            }
            catch (Exception ex)
            {
                if (jobRunId.HasValue && recorder != null)
                    await recorder.FailAsync(jobRunId.Value, new FailJobRunDto { ErrorMessage = ex.Message, ErrorDetails = ex.ToString() }, ct);
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Per-tenant orchestration: for each active company, set TenantScope, then enqueue one SlaEvaluation JobExecution if none pending for that tenant.
    /// Ensures every JobExecution write has valid tenant context (TenantSafetyGuard).
    /// </summary>
    private async Task ScheduleSlaEvaluationJobCoreAsync(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, CancellationToken cancellationToken)
    {
        // Company is not CompanyScopedEntity; with no tenant scope the filter allows all rows.
        var companyIds = await context.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (companyIds.Count == 0)
        {
            _logger.LogDebug("No active companies, skipping SLA evaluation scheduling");
            return;
        }

        int enqueued = 0;
        foreach (var companyId in companyIds)
        {
            await TenantScopeExecutor.RunWithTenantScopeAsync(companyId, async (ct) =>
            {
                var hasPending = await context.JobExecutions
                    .AnyAsync(j => j.JobType == "SlaEvaluation" && (j.Status == "Pending" || j.Status == "Running"), ct);
                if (hasPending)
                {
                    _logger.LogDebug("Skipping SLA evaluation for company {CompanyId}: job already queued or running", companyId);
                    return;
                }

                await enqueuer.EnqueueAsync("SlaEvaluation", "{}", companyId, cancellationToken: ct);
                enqueued++;
            }, cancellationToken);
        }

        if (enqueued > 0)
            _logger.LogInformation("Scheduled {Count} SLA evaluation job(s) via JobExecution pipeline", enqueued);
    }
}
