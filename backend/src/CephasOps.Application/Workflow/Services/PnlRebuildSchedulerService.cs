using System.Text.Json;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Schedules PnlRebuild jobs (Phase 4 - Reporting/P&amp;L). Enqueues one job per day when none pending.
/// Uses first company from DB (single-company deployment); period = current month.
/// </summary>
public class PnlRebuildSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PnlRebuildSchedulerService> _logger;
    /// <summary>Check every 24 hours; enqueue at most one job when none pending.</summary>
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromHours(24);

    public PnlRebuildSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<PnlRebuildSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("P&L rebuild scheduler service started (interval: {Interval})", SchedulerInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SchedulePnlRebuildJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in P&L rebuild scheduler: {Message}", ex.Message);
            }

            await Task.Delay(SchedulerInterval, stoppingToken);
        }

        _logger.LogInformation("P&L rebuild scheduler service stopped");
    }

    private async Task SchedulePnlRebuildJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping P&L rebuild scheduling");
            return;
        }

        var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            Guid? jobRunId = null;
            if (recorder != null)
            {
                jobRunId = await recorder.StartAsync(new StartJobRunDto
                {
                    JobName = "P&L Rebuild Scheduler",
                    JobType = "PnlRebuildScheduler",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct);
            }
            try
            {
                var enqueuer = scope.ServiceProvider.GetRequiredService<IJobExecutionEnqueuer>();
                await SchedulePnlRebuildJobCoreAsync(context, enqueuer, ct);
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

    private async Task SchedulePnlRebuildJobCoreAsync(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, CancellationToken cancellationToken)
    {
        var companyId = await context.Companies
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (companyId == Guid.Empty)
        {
            _logger.LogWarning("No company found, skipping P&L rebuild scheduling");
            return;
        }

        // Phase 3: use JobExecutions (new pipeline) for PnlRebuild
        var hasPending = await context.JobExecutions
            .AnyAsync(j => j.JobType == "PnlRebuild" && (j.Status == "Pending" || j.Status == "Running"), cancellationToken);
        if (hasPending)
        {
            _logger.LogDebug("Skipping P&L rebuild schedule: job already queued or running");
            return;
        }

        var period = DateTime.UtcNow.ToString("yyyy-MM");
        var payload = new Dictionary<string, object>
        {
            { "companyId", companyId.ToString() },
            { "period", period }
        };
        var payloadJson = JsonSerializer.Serialize(payload);
        await enqueuer.EnqueueAsync("PnlRebuild", payloadJson, companyId, nextRunAtUtc: null, cancellationToken: cancellationToken);
        _logger.LogInformation("Scheduled P&L rebuild job for period {Period} via JobExecution pipeline", period);
    }
}
