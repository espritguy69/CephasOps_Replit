using System.Text.Json;
using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.Rates;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Background job that periodically detects completed orders without a payout snapshot
/// and creates them via IOrderPayoutSnapshotService. Ensures no completed order is left without a snapshot
/// even if status was updated outside OrderService.ChangeOrderStatusAsync.
/// </summary>
public class MissingPayoutSnapshotSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MissingPayoutSnapshotSchedulerService> _logger;

    /// <summary>Run once per day.</summary>
    private static readonly TimeSpan SchedulerInterval = TimeSpan.FromHours(24);

    public MissingPayoutSnapshotSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<MissingPayoutSnapshotSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Missing payout snapshot scheduler started (interval: {Interval})",
            SchedulerInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRepairAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Missing payout snapshot scheduler error: {Message}", ex.Message);
            }

            await Task.Delay(SchedulerInterval, stoppingToken);
        }

        _logger.LogInformation("Missing payout snapshot scheduler stopped");
    }

    private async Task RunRepairAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Database not available, skipping missing payout snapshot repair");
            return;
        }

        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            var recorder = scope.ServiceProvider.GetService<IJobRunRecorder>();
            Guid? jobRunId = null;
            if (recorder != null)
            {
                jobRunId = await recorder.StartAsync(new StartJobRunDto
                {
                    JobName = "Missing Payout Snapshot Repair",
                    JobType = "MissingPayoutSnapshotRepair",
                    TriggerSource = "Scheduler",
                    CorrelationId = Guid.NewGuid().ToString()
                }, ct);
            }

            var startedAt = DateTime.UtcNow;
            try
            {
                var repairService = scope.ServiceProvider.GetRequiredService<IMissingPayoutSnapshotRepairService>();
                var result = await repairService.DetectMissingPayoutSnapshotsAsync(ct);
                var completedAt = DateTime.UtcNow;

                if (jobRunId.HasValue && recorder != null)
                    await recorder.CompleteAsync(jobRunId.Value, ct);

                var run = new PayoutSnapshotRepairRun
                {
                    Id = Guid.NewGuid(),
                    StartedAt = startedAt,
                    CompletedAt = completedAt,
                    TotalProcessed = result.TotalProcessed,
                    CreatedCount = result.CreatedCount,
                    SkippedCount = result.SkippedCount,
                    ErrorCount = result.ErrorCount,
                    ErrorOrderIdsJson = result.ErrorOrderIds.Count > 0
                        ? JsonSerializer.Serialize(result.ErrorOrderIds.Select(x => x.ToString()).ToList())
                        : null,
                    TriggerSource = RepairRunTriggerSource.Scheduler,
                    Notes = result.TotalProcessed == 0 ? "No completed orders without snapshot" : null
                };
                dbContext.PayoutSnapshotRepairRuns.Add(run);
                await dbContext.SaveChangesAsync(ct);

                if (result.TotalProcessed > 0)
                {
                    _logger.LogInformation(
                        "Missing payout snapshot repair run: processed={Total}, created={Created}, skipped={Skipped}, errors={Errors}",
                        result.TotalProcessed, result.CreatedCount, result.SkippedCount, result.ErrorCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Missing payout snapshot repair failed: {Message}", ex.Message);
                if (jobRunId.HasValue && recorder != null)
                    await recorder.FailAsync(jobRunId.Value, new FailJobRunDto { ErrorMessage = ex.Message, ErrorDetails = ex.ToString() }, ct);
                throw;
            }
        }, cancellationToken);
    }
}
