using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Workers;

/// <summary>
/// Registers this process as a worker on startup and runs heartbeat + stale recovery on a timer.
/// Starts automatically when the API (or worker process) starts.
/// </summary>
public sealed class WorkerHeartbeatHostedService : BackgroundService
{
    private const string WorkerRoleApi = "API";
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerIdentityHolder _identityHolder;
    private readonly WorkerOptions _options;
    private readonly ILogger<WorkerHeartbeatHostedService> _logger;

    public WorkerHeartbeatHostedService(
        IServiceProvider serviceProvider,
        WorkerIdentityHolder identityHolder,
        IOptions<WorkerOptions> options,
        ILogger<WorkerHeartbeatHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _identityHolder = identityHolder;
        _options = options?.Value ?? new WorkerOptions();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var coordinator = scope.ServiceProvider.GetRequiredService<IWorkerCoordinator>();

        var hostName = Dns.GetHostName();
        var processId = Process.GetCurrentProcess().Id;

        try
        {
            var workerId = await coordinator.RegisterAsync(hostName, processId, WorkerRoleApi, stoppingToken).ConfigureAwait(false);
            _identityHolder.SetWorkerId(workerId);
            _logger.LogInformation("Worker heartbeat service started. WorkerId={WorkerId}, HostName={HostName}, ProcessId={ProcessId}",
                workerId, hostName, processId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register worker. Heartbeat will not run.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.HeartbeatIntervalSeconds));
        var staleRecoveryInterval = TimeSpan.FromMinutes(1);

        var lastStaleRecovery = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);

                using var heartbeatScope = _serviceProvider.CreateScope();
                var coord = heartbeatScope.ServiceProvider.GetRequiredService<IWorkerCoordinator>();
                var wid = _identityHolder.WorkerId;
                if (wid.HasValue)
                    await coord.HeartbeatAsync(wid.Value, stoppingToken).ConfigureAwait(false);

                if ((DateTime.UtcNow - lastStaleRecovery) >= staleRecoveryInterval)
                {
                    lastStaleRecovery = DateTime.UtcNow;
                    var recovered = await coord.RecoverStaleWorkersAsync(stoppingToken).ConfigureAwait(false);
                    if (recovered > 0)
                        _logger.LogInformation("Stale worker recovery released {Count} workers", recovered);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker heartbeat or stale recovery failed");
            }
        }

        _logger.LogInformation("Worker heartbeat service stopped");
    }
}
