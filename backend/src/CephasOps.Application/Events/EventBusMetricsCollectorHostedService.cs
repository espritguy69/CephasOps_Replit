using CephasOps.Application.Events.Backpressure;
using CephasOps.Application.Events.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Events;

/// <summary>
/// Periodically updates Event Bus gauge metrics (pending/failed/dead-letter counts, oldest pending age)
/// and logs a warning when oldest pending event age exceeds the configured threshold.
/// </summary>
public sealed class EventBusMetricsCollectorHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusMetricsCollectorHostedService> _logger;
    private readonly EventBusDispatcherOptions _options;
    private readonly EventBusDispatcherMetrics _metrics;
    private static readonly TimeSpan CollectInterval = TimeSpan.FromSeconds(30);

    public EventBusMetricsCollectorHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventBusMetricsCollectorHostedService> logger,
        IOptions<EventBusDispatcherOptions> options,
        EventBusDispatcherMetrics metrics)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new EventBusDispatcherOptions();
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Event Bus metrics collector started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event Bus metrics collection failed");
            }

            await Task.Delay(CollectInterval, stoppingToken);
        }
        _logger.LogDebug("Event Bus metrics collector stopped");
    }

    private async Task CollectAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IEventStoreQueryService>();
        EventStoreCountsSnapshot snapshot;
        try
        {
            snapshot = await queryService.GetEventStoreCountsAsync(scopeCompanyId: null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get event store counts for metrics");
            return;
        }

        var ageSeconds = snapshot.OldestPendingEventAgeSeconds;
        _metrics.UpdateSnapshot(
            snapshot.PendingCount,
            snapshot.FailedCount,
            snapshot.DeadLetterCount,
            ageSeconds);

        var backpressure = scope.ServiceProvider.GetService<IEventBusBackpressureService>();
        if (backpressure != null)
            _metrics.UpdateBackpressureLevel(backpressure.GetState().Level);

        var warningMinutes = Math.Max(0, _options.OldestPendingEventAgeWarningMinutes);
        if (warningMinutes > 0 && snapshot.PendingCount > 0 && ageSeconds > TimeSpan.FromMinutes(warningMinutes).TotalSeconds)
        {
            _logger.LogWarning(
                "Event lag: oldest pending event age {AgeSeconds:F0}s exceeds threshold {ThresholdMinutes} min. PendingCount={PendingCount}, EventId would be in next batch",
                ageSeconds, warningMinutes, snapshot.PendingCount);
        }
    }
}
