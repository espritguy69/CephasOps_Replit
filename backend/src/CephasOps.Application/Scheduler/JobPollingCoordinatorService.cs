using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Scheduler;

/// <summary>
/// Discovers runnable background jobs and claims them for this worker. Claimed jobs are then processed by BackgroundJobProcessorService.
/// </summary>
public sealed class JobPollingCoordinatorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerDiagnostics _diagnostics;
    private readonly SchedulerOptions _options;
    private readonly ILogger<JobPollingCoordinatorService> _logger;

    public JobPollingCoordinatorService(
        IServiceProvider serviceProvider,
        SchedulerDiagnostics diagnostics,
        IOptions<SchedulerOptions> options,
        ILogger<JobPollingCoordinatorService> logger)
    {
        _serviceProvider = serviceProvider;
        _diagnostics = diagnostics;
        _options = options?.Value ?? new SchedulerOptions();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job polling coordinator started. PollIntervalSeconds={Interval}, MaxJobsPerPoll={Max}",
            _options.PollIntervalSeconds, _options.MaxJobsPerPoll);

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
        var maxPerPoll = Math.Max(1, Math.Min(100, _options.MaxJobsPerPoll));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                await PollAndClaimAsync(maxPerPoll, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job polling cycle failed");
            }
        }

        _logger.LogInformation("Job polling coordinator stopped");
    }

    private async Task PollAndClaimAsync(int maxPerPoll, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var coordinator = scope.ServiceProvider.GetRequiredService<Workers.IWorkerCoordinator>();
        var identity = scope.ServiceProvider.GetRequiredService<Workers.IWorkerIdentity>();

        var workerId = identity.WorkerId;
        if (!workerId.HasValue)
        {
            _logger.LogDebug("Scheduler skip: worker not registered yet");
            return;
        }

        _diagnostics.SetWorkerId(workerId.Value);

        if (!await context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug("Scheduler skip: database not available");
            return;
        }

        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-(scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Workers.WorkerOptions>>().Value?.InactiveTimeoutMinutes ?? 3));

        // Discover runnable jobs: Queued, scheduled (or no schedule), and (unclaimed or owner inactive)
        var runnable = await context.BackgroundJobs
            .Where(j => j.State == BackgroundJobState.Queued
                && (j.ScheduledAt == null || j.ScheduledAt <= now)
                && (j.WorkerId == null
                    || context.WorkerInstances.Any(w => w.Id == j.WorkerId && (!w.IsActive || w.LastHeartbeatUtc < cutoff))))
            .OrderBy(j => j.Priority)
            .ThenBy(j => j.CreatedAt)
            .Take(maxPerPoll)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (runnable.Count == 0)
            return;

        var claimResults = new List<(Guid JobId, bool Claimed)>();
        foreach (var jobId in runnable)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var claimed = await coordinator.TryClaimBackgroundJobAsync(workerId.Value, jobId, cancellationToken).ConfigureAwait(false);
            claimResults.Add((JobId: jobId, Claimed: claimed));
        }

        _diagnostics.RecordPoll(now, runnable.Count, claimResults);
        var successCount = claimResults.Count(r => r.Claimed);
        if (successCount > 0)
            _logger.LogDebug("Scheduler claimed {Count} of {Total} runnable jobs", successCount, runnable.Count);
    }
}
