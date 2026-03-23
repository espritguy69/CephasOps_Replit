using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Integration;

/// <summary>
/// Background worker that periodically loads Pending or Failed (due for retry) outbound deliveries
/// and dispatches them via IOutboundIntegrationBus. Complements on-demand replay.
/// </summary>
public sealed class OutboundIntegrationRetryWorkerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboundIntegrationRetryWorkerHostedService> _logger;
    private readonly OutboundIntegrationRetryWorkerOptions _options;

    public OutboundIntegrationRetryWorkerHostedService(
        IServiceProvider serviceProvider,
        ILogger<OutboundIntegrationRetryWorkerHostedService> logger,
        IOptions<OutboundIntegrationRetryWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new OutboundIntegrationRetryWorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbound integration retry worker is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));
        var maxPerPoll = Math.Clamp(_options.MaxDeliveriesPerPoll, 1, 100);
        _logger.LogInformation("Outbound integration retry worker started. Interval={Interval}s, MaxPerPoll={MaxPerPoll}", interval.TotalSeconds, maxPerPoll);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(maxPerPoll, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound retry worker loop error");
            }

            await Task.Delay(interval, stoppingToken);
        }

        _logger.LogInformation("Outbound integration retry worker stopped");
    }

    private async Task ProcessBatchAsync(int maxCount, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboundDeliveryStore>();
        var bus = scope.ServiceProvider.GetRequiredService<IOutboundIntegrationBus>();

        var due = await store.GetPendingOrRetryAsync(maxCount, cancellationToken);
        if (due.Count == 0)
            return;

        _logger.LogDebug("Outbound retry worker processing {Count} deliveries", due.Count);
        var success = 0;
        var failed = 0;
        foreach (var d in due)
        {
            var result = await bus.DispatchDeliveryAsync(d.Id, cancellationToken);
            if (result.Success)
                success++;
            else
                failed++;
        }
        if (success > 0 || failed > 0)
            _logger.LogInformation("Outbound retry worker batch: {Success} delivered, {Failed} failed", success, failed);
    }
}
