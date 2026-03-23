using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Schedules ReconcileLedgerBalanceCache jobs (Phase 3 - Inventory hardening).
/// Enqueues one job when none pending; avoids duplicate Queued/Running jobs.
/// </summary>
public class LedgerReconciliationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LedgerReconciliationSchedulerService> _logger;
    /// <summary>Check every 12 hours; enqueue at most one job when none pending.</summary>
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromHours(12);

    public LedgerReconciliationSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<LedgerReconciliationSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ledger reconciliation scheduler service started (interval: {Interval})", SchedulerInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleReconciliationJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ledger reconciliation scheduler: {Message}", ex.Message);
            }

            await Task.Delay(SchedulerInterval, stoppingToken);
        }

        _logger.LogInformation("Ledger reconciliation scheduler service stopped");
    }

    private async Task ScheduleReconciliationJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping ledger reconciliation scheduling");
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
                    JobName = "Ledger Reconciliation Scheduler",
                    JobType = "LedgerReconciliationScheduler",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct);
            }
            try
            {
                var enqueuer = scope.ServiceProvider.GetRequiredService<IJobExecutionEnqueuer>();
                await ScheduleReconciliationJobCoreAsync(context, enqueuer, ct);
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

    private async Task ScheduleReconciliationJobCoreAsync(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, CancellationToken cancellationToken)
    {
        // Phase 4: JobExecution pipeline
        var hasPending = await context.JobExecutions
            .AnyAsync(j => j.JobType == "ReconcileLedgerBalanceCache" && (j.Status == "Pending" || j.Status == "Running"), cancellationToken);
        if (hasPending)
        {
            _logger.LogDebug("Skipping ledger reconciliation schedule: job already queued or running");
            return;
        }

        await enqueuer.EnqueueAsync("ReconcileLedgerBalanceCache", "{}", companyId: null, cancellationToken: cancellationToken);
        _logger.LogInformation("Scheduled reconcile ledger balance cache job via JobExecution pipeline");
    }
}
