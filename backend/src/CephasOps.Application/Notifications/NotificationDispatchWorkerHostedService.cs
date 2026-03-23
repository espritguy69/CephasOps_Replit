using System.Diagnostics;
using CephasOps.Domain.Notifications;
using CephasOps.Infrastructure.Metrics;
using CephasOps.Infrastructure.Operational;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Notifications;

/// <summary>
/// Background worker that claims pending NotificationDispatches and sends via INotificationDeliverySender (Phase 2).
/// Idempotent, lease-based claim; retry and dead-letter via store.
/// </summary>
public class NotificationDispatchWorkerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDispatchWorkerHostedService> _logger;
    private readonly NotificationDispatchWorkerOptions _options;

    public NotificationDispatchWorkerHostedService(
        IServiceProvider serviceProvider,
        ILogger<NotificationDispatchWorkerHostedService> logger,
        IOptions<NotificationDispatchWorkerOptions>? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new NotificationDispatchWorkerOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize = Math.Clamp(_options.BatchSize, 1, 50);
        var intervalMs = Math.Max(1000, _options.PollIntervalMs);
        var nodeId = _options.NodeId ?? Environment.MachineName;
        var leaseSeconds = Math.Max(30, _options.LeaseSeconds);

        _logger.LogInformation("Notification dispatch worker started. NodeId={NodeId}, BatchSize={BatchSize}, PollIntervalMs={IntervalMs}",
            nodeId, batchSize, intervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetService<INotificationDispatchStore>();
                var sender = scope.ServiceProvider.GetService<INotificationDeliverySender>();
                if (store == null || sender == null)
                {
                    await Task.Delay(intervalMs, stoppingToken);
                    continue;
                }

                var leaseExpiresAtUtc = DateTime.UtcNow.AddSeconds(leaseSeconds);
                var batch = await store.ClaimNextPendingBatchAsync(batchSize, nodeId, leaseExpiresAtUtc, stoppingToken);
                if (batch.Count == 0)
                {
                    await Task.Delay(intervalMs, stoppingToken);
                    continue;
                }

                var guard = scope.ServiceProvider.GetService<ITenantOperationsGuard>();
                _logger.LogDebug("Claimed {Count} notification dispatch(es)", batch.Count);
                foreach (var dispatch in batch)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        var result = await sender.SendAsync(dispatch, stoppingToken).ConfigureAwait(false);
                        sw.Stop();
                        await store.MarkProcessedAsync(dispatch.Id, result.Success, result.ErrorMessage, isNonRetryable: false, stoppingToken);
                        var durationMs = (int)sw.ElapsedMilliseconds;
                        _logger.LogInformation("Notification dispatch {DispatchId}. tenantId={TenantId}, operation=NotificationDispatch, durationMs={DurationMs}, success={Success}", dispatch.Id, dispatch.CompanyId, durationMs, result.Success);
                        TenantOperationalMetrics.RecordNotificationSent(dispatch.CompanyId, result.Success);
                        if (!result.Success)
                            guard?.RecordNotificationFailure(dispatch.CompanyId);
                        if (result.Success)
                            _logger.LogInformation("Notification dispatch {DispatchId} sent via {Channel} to {Target}", dispatch.Id, dispatch.Channel, dispatch.Target);
                        else
                            _logger.LogWarning("Notification dispatch {DispatchId} failed: {Error}", dispatch.Id, result.ErrorMessage);
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        var durationMs = (int)sw.ElapsedMilliseconds;
                        _logger.LogError(ex, "Notification dispatch {DispatchId} error. tenantId={TenantId}, operation=NotificationDispatch, durationMs={DurationMs}, success=false", dispatch.Id, dispatch.CompanyId, durationMs);
                        await store.MarkProcessedAsync(dispatch.Id, false, ex.Message, isNonRetryable: false, stoppingToken);
                        TenantOperationalMetrics.RecordNotificationSent(dispatch.CompanyId, false);
                        guard?.RecordNotificationFailure(dispatch.CompanyId);
                    }
                }

                if (batch.Count > 0 && _options.BusyDelayMs > 0)
                    await Task.Delay(_options.BusyDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification dispatch worker loop error");
                await Task.Delay(intervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("Notification dispatch worker stopped");
    }
}

/// <summary>Options for NotificationDispatchWorkerHostedService.</summary>
public class NotificationDispatchWorkerOptions
{
    public const string SectionName = "Notifications:DispatchWorker";

    public int BatchSize { get; set; } = 10;
    public int PollIntervalMs { get; set; } = 5000;
    public int BusyDelayMs { get; set; } = 500;
    public string? NodeId { get; set; }
    public int LeaseSeconds { get; set; } = 120;
}
