using CephasOps.Domain.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Workflow.JobOrchestration;

/// <summary>SaaS resilience: detects stuck Running job executions (expired lease or missing lease and old), resets them to Pending so they can be re-claimed. Complements the worker's in-cycle reset.</summary>
public class JobExecutionWatchdogService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobExecutionWatchdogService> _logger;
    private readonly JobExecutionWorkerOptions _options;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _leaseExpiry = TimeSpan.FromMinutes(10);

    public JobExecutionWatchdogService(
        IServiceProvider serviceProvider,
        ILogger<JobExecutionWatchdogService> logger,
        IOptions<JobExecutionWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new JobExecutionWorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job execution watchdog started. Interval={Interval}min", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await RunWatchdogAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job execution watchdog cycle failed");
            }
        }

        _logger.LogInformation("Job execution watchdog stopped");
    }

    private async Task RunWatchdogAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IJobExecutionStore>();
        if (store == null) return;

        var leaseExpiry = TimeSpan.FromSeconds(Math.Max(60, _options.LeaseSeconds * 2));
        var resetCount = await store.ResetStuckRunningAsync(leaseExpiry, cancellationToken);
        if (resetCount > 0)
            _logger.LogWarning("JobExecutionWatchdog: reset {Count} stuck Running job(s) to Pending (lease expiry {LeaseExpiry}s)", resetCount, leaseExpiry.TotalSeconds);
    }
}
