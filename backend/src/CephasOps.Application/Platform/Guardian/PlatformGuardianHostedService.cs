using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: runs anomaly, drift, and performance checks on a schedule; does not fail runtime.</summary>
public class PlatformGuardianHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlatformGuardianHostedService> _logger;
    private readonly PlatformGuardianOptions _options;

    public PlatformGuardianHostedService(
        IServiceProvider serviceProvider,
        ILogger<PlatformGuardianHostedService> logger,
        IOptions<PlatformGuardianOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new PlatformGuardianOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Platform Guardian is disabled");
            return;
        }

        var intervalMinutes = Math.Max(5, _options.RunIntervalMinutes);
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _logger.LogInformation("Platform Guardian started. RunIntervalMinutes={Interval}", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunGuardianCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Platform Guardian cycle failed");
            }
        }

        _logger.LogInformation("Platform Guardian stopped");
    }

    private async Task RunGuardianCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        if (_options.RunAnomalyDetection)
        {
            try
            {
                var anomaly = scope.ServiceProvider.GetService<ITenantAnomalyDetectionService>();
                if (anomaly != null)
                {
                    await anomaly.RunDetectionAsync(cancellationToken);
                    _logger.LogDebug("Platform Guardian: anomaly detection completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Platform Guardian: anomaly detection failed");
            }
        }

        if (_options.RunDriftDetection)
        {
            try
            {
                var drift = scope.ServiceProvider.GetService<IPlatformDriftDetectionService>();
                if (drift != null)
                {
                    var report = await drift.DetectAsync(cancellationToken);
                    if (report.CriticalCount > 0)
                        _logger.LogWarning("Platform Guardian: drift report has {Count} critical item(s)", report.CriticalCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Platform Guardian: drift detection failed");
            }
        }

        if (_options.RunPerformanceWatchdog)
        {
            try
            {
                var perf = scope.ServiceProvider.GetService<IPerformanceWatchdogService>();
                if (perf != null)
                {
                    var health = await perf.GetPerformanceHealthAsync(cancellationToken);
                    if (health.TenantsWithHighLatencyImpact.Count > 0 || (health.PendingJobCount ?? 0) > 100)
                        _logger.LogWarning("Platform Guardian: performance issues - {Tenants} tenant(s) impacted, {Pending} pending jobs",
                            health.TenantsWithHighLatencyImpact.Count, health.PendingJobCount ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Platform Guardian: performance watchdog failed");
            }
        }
    }
}
