using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Integration;

/// <summary>
/// Background worker that runs event platform retention cleanup on a configurable interval.
/// Idempotent and restart-safe; each run deletes up to MaxDeletesPerTablePerRun per table.
/// </summary>
public sealed class EventPlatformRetentionWorkerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPlatformRetentionWorkerHostedService> _logger;
    private readonly EventPlatformRetentionOptions _options;

    public EventPlatformRetentionWorkerHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventPlatformRetentionWorkerHostedService> logger,
        IOptions<EventPlatformRetentionOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new EventPlatformRetentionOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Event platform retention worker is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(3600, _options.RunIntervalSeconds)); // at least 1 hour
        _logger.LogInformation(
            "Event platform retention worker started. RunIntervalSeconds={Interval}, EventStoreDays={Es}, EventProcessingLogDays={Epl}, OutboundDays={Ob}, InboundDays={Ib}, ExternalIdempotencyDays={Ext}, MaxPerTable={Max}",
            interval.TotalSeconds, _options.EventStoreProcessedAndDeadLetterDays, _options.EventProcessingLogCompletedDays,
            _options.OutboundDeliveredDays, _options.InboundProcessedDays, _options.ExternalIdempotencyCompletedDays,
            _options.MaxDeletesPerTablePerRun);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IEventPlatformRetentionService>();
                var result = await service.RunRetentionAsync(stoppingToken);
                if (result.Errors.Count > 0)
                    _logger.LogWarning("Event platform retention run had {Count} error(s): {Errors}", result.Errors.Count, string.Join("; ", result.Errors));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event platform retention worker loop error");
            }

            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("Event platform retention worker stopped");
    }
}
