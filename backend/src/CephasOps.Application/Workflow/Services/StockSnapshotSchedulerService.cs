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
/// Schedules daily stock-by-location snapshot jobs (Phase 2.2.2).
/// Enqueues one job per day for yesterday's period; avoids duplicate Queued/Running jobs.
/// </summary>
public class StockSnapshotSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockSnapshotSchedulerService> _logger;
    /// <summary>Check every 6 hours; enqueue at most one job when none pending.</summary>
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromHours(6);

    public StockSnapshotSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<StockSnapshotSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock snapshot scheduler service started (interval: {Interval})", SchedulerInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleDailySnapshotJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stock snapshot scheduler: {Message}", ex.Message);
            }

            await Task.Delay(SchedulerInterval, stoppingToken);
        }

        _logger.LogInformation("Stock snapshot scheduler service stopped");
    }

    private async Task ScheduleDailySnapshotJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping stock snapshot scheduling");
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
                    JobName = "Stock Snapshot Scheduler",
                    JobType = "StockSnapshotScheduler",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct);
            }
            try
            {
                var enqueuer = scope.ServiceProvider.GetRequiredService<IJobExecutionEnqueuer>();
                await ScheduleDailySnapshotJobCoreAsync(context, enqueuer, ct);
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

    private async Task ScheduleDailySnapshotJobCoreAsync(ApplicationDbContext context, IJobExecutionEnqueuer enqueuer, CancellationToken cancellationToken)
    {
        // Phase 4: JobExecution pipeline
        var hasPending = await context.JobExecutions
            .AnyAsync(j => j.JobType == "PopulateStockByLocationSnapshots" && (j.Status == "Pending" || j.Status == "Running"), cancellationToken);
        if (hasPending)
        {
            _logger.LogDebug("Skipping stock snapshot schedule: job already queued or running");
            return;
        }

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var payload = new Dictionary<string, object>
        {
            { "periodEndDate", yesterday.ToString("O") },
            { "snapshotType", "Daily" }
        };
        var payloadJson = JsonSerializer.Serialize(payload);
        await enqueuer.EnqueueAsync("PopulateStockByLocationSnapshots", payloadJson, companyId: null, cancellationToken: cancellationToken);
        _logger.LogInformation("Scheduled daily stock-by-location snapshot job for period {Period:yyyy-MM-dd} via JobExecution pipeline", yesterday);
    }
}
