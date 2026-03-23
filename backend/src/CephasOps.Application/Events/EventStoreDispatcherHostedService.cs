using CephasOps.Application.Events.Backpressure;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Events;

/// <summary>
/// Background worker that claims Pending (and due-retry Failed) events from the EventStore and dispatches them via IDomainEventDispatcher.
/// Uses FOR UPDATE SKIP LOCKED for safe horizontal scaling; processes each batch in parallel up to MaxConcurrentDispatchers (Phase 6).
/// Phase 8: partition-aware claim ordering, adaptive backpressure (batch size and delay).
/// </summary>
public class EventStoreDispatcherHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventStoreDispatcherHostedService> _logger;
    private readonly EventBusDispatcherOptions _options;
    private readonly EventBusDispatcherMetrics? _metrics;
    private readonly IEventBusBackpressureService? _backpressure;
    private readonly SemaphoreSlim _concurrency;

    public EventStoreDispatcherHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventStoreDispatcherHostedService> logger,
        IOptions<EventBusDispatcherOptions> options,
        EventBusDispatcherMetrics? metrics = null,
        IEventBusBackpressureService? backpressure = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options?.Value ?? new EventBusDispatcherOptions();
        _metrics = metrics;
        _backpressure = backpressure;
        var maxConcurrent = Math.Clamp(_options.MaxConcurrentDispatchers, 1, 64);
        _concurrency = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            SetDispatcherRunning(true);
        }
        catch
        {
            // Optional state not registered
        }

        var configuredBatch = Math.Clamp(_options.MaxEventsPerPoll > 0 ? _options.MaxEventsPerPoll : _options.BatchSize, 1, 100);
        _logger.LogInformation(
            "Event Store dispatcher started. NodeId={NodeId}, LeaseSeconds={LeaseSeconds}, PollingIntervalSeconds={PollingIntervalSeconds}, MaxEventsPerPoll={BatchSize}, MaxConcurrentDispatchers={MaxConcurrent}, MaxRetriesBeforeDeadLetter={MaxRetries}",
            _options.NodeId ?? "(single-node)", _options.ProcessingLeaseSeconds, _options.PollingIntervalSeconds, configuredBatch, _concurrency.CurrentCount, _options.MaxRetriesBeforeDeadLetter);

        var baseInterval = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));
        var maxRetries = Math.Max(1, _options.MaxRetriesBeforeDeadLetter);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ResetStuckProcessingIfConfiguredAsync(stoppingToken);
                var batchSize = GetEffectiveBatchSize(configuredBatch);
                if (batchSize == 0)
                {
                    var backpressureDelay = _backpressure?.GetSuggestedDelayMs() ?? 10000;
                    _logger.LogWarning("Event Store dispatcher backpressure Paused; waiting {DelayMs}ms", backpressureDelay);
                    await Task.Delay(TimeSpan.FromMilliseconds(backpressureDelay), stoppingToken);
                    continue;
                }
                var hadWork = await ProcessNextBatchAsync(batchSize, maxRetries, stoppingToken);
                var delayMs = hadWork && _options.DispatcherBusyDelayMs > 0 ? _options.DispatcherBusyDelayMs
                    : _options.DispatcherIdleDelayMs > 0 ? _options.DispatcherIdleDelayMs : (int)baseInterval.TotalMilliseconds;
                var extraDelay = _backpressure?.GetSuggestedDelayMs() ?? 0;
                if (delayMs + extraDelay > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs + extraDelay), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Event Store dispatcher is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event Store dispatcher loop error");
                await Task.Delay(baseInterval, stoppingToken);
            }
        }

        try
        {
            SetDispatcherRunning(false);
        }
        catch
        {
            // Optional state not registered
        }
        _logger.LogInformation("Event Store dispatcher stopped");
    }

    private int GetEffectiveBatchSize(int configuredBatch)
    {
        var suggested = _backpressure?.GetSuggestedBatchSize(configuredBatch);
        if (suggested.HasValue) return suggested.Value;
        return configuredBatch;
    }

    private void SetDispatcherRunning(bool running)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var state = scope.ServiceProvider.GetService<IEventStoreDispatcherState>() as EventStoreDispatcherState;
            if (state != null)
                state.IsRunning = running;
        }
        catch
        {
            // Ignore
        }
    }

    private void RecordFailedMetric(EventStoreMarkProcessedResult res)
    {
        if (_metrics == null) return;
        _metrics.RecordEventFailed(res.EventType, res.CompanyId, res.LastHandler);
        if (res.NewStatus == "DeadLetter")
            _metrics.RecordEventDeadLettered(res.EventType, res.CompanyId);
        else
            _metrics.RecordEventRetried(res.EventType, res.CompanyId);
    }

    private async Task ResetStuckProcessingIfConfiguredAsync(CancellationToken cancellationToken)
    {
        var timeoutMinutes = Math.Max(1, _options.StuckProcessingTimeoutMinutes);
        var timeout = TimeSpan.FromMinutes(timeoutMinutes);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            var count = await eventStore.ResetStuckProcessingAsync(timeout, cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("Event Store dispatcher reset {Count} stuck Processing event(s) (timeout {TimeoutMinutes} min)", count, timeoutMinutes);
                _metrics?.RecordEventsRecoveredFromStuck(count);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event Store dispatcher failed to reset stuck Processing events");
        }
    }

    private async Task<bool> ProcessNextBatchAsync(int batchSize, int maxRetries, CancellationToken cancellationToken)
    {
        string? nodeId = _options.NodeId;
        DateTime? leaseExpiresAtUtc = null;
        if (_options.ProcessingLeaseSeconds > 0)
            leaseExpiresAtUtc = DateTime.UtcNow.AddSeconds(_options.ProcessingLeaseSeconds);

        IReadOnlyList<EventStoreEntry> batch;
        using (var scope = _serviceProvider.CreateScope())
        {
            var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
            try
            {
                batch = await eventStore.ClaimNextPendingBatchAsync(batchSize, maxRetries, cancellationToken, nodeId, leaseExpiresAtUtc);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Remediation 1.1: if EventStore is missing Phase 8 columns (e.g. RootEventId), claim fails every cycle.
                var msg = ex.Message ?? "";
                if (msg.Contains("RootEventId", StringComparison.OrdinalIgnoreCase) || (msg.Contains("column", StringComparison.OrdinalIgnoreCase) && msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogCritical(ex, "EventStore schema is missing Phase 8 columns (e.g. RootEventId). Apply migrations or run backend/scripts/apply-remediation-1.1-schema-repair.sql. Dispatcher will skip this cycle.");
                }
                else
                {
                    _logger.LogError(ex, "Failed to claim pending event batch");
                }
                return false;
            }
        }

        if (batch.Count == 0)
            return false;

        _logger.LogInformation("Event Store dispatcher claimed {Count} event(s) for processing. NodeId={NodeId}", batch.Count, nodeId ?? "(none)");

        try
        {
            _metrics?.SetParallelWorkerCount(batch.Count);
            _metrics?.RecordClaimed(batch.Count);
        }
        catch { /* optional metrics */ }

        var tasks = batch.Select(entry => ProcessOneEventAsync(entry, cancellationToken)).ToList();
        await Task.WhenAll(tasks);

        try
        {
            _metrics?.SetParallelWorkerCount(0);
        }
        catch { /* optional metrics */ }
        return true;
    }

    /// <summary>Dispatches one event under tenant scope or platform bypass via TenantScopeExecutor; releases concurrency in finally.</summary>
    private async Task ProcessOneEventAsync(EventStoreEntry entry, CancellationToken cancellationToken)
    {
        await _concurrency.WaitAsync(cancellationToken);
        try
        {
            await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, async (ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
                var typeRegistry = scope.ServiceProvider.GetRequiredService<IEventTypeRegistry>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

                try
                {
                    var domainEvent = typeRegistry.Deserialize(entry.EventType, entry.Payload);
                    if (domainEvent == null)
                    {
                        _logger.LogWarning("Could not deserialize event {EventId} (type {EventType}); marking as poison (non-retryable)", entry.EventId, entry.EventType);
                        var markRes = await eventStore.MarkProcessedAsync(entry.EventId, success: false, "Deserialization failed: unknown or invalid type", null, "Deserialization", isNonRetryable: true, ct);
                        if (markRes != null)
                        {
                            RecordFailedMetric(markRes);
                            _metrics?.RecordNonRetryableFailed(entry.EventType, entry.CompanyId);
                        }
                        var attemptStore = scope.ServiceProvider.GetService<IEventStoreAttemptHistoryStore>();
                        if (attemptStore != null)
                        {
                            var startedAt = entry.LastClaimedAtUtc ?? entry.ProcessingStartedAtUtc ?? entry.CreatedAtUtc;
                            await attemptStore.RecordAttemptAsync(new EventStoreAttemptRecord
                            {
                                EventId = entry.EventId,
                                EventType = entry.EventType,
                                CompanyId = entry.CompanyId,
                                HandlerName = "Deserializer",
                                AttemptNumber = entry.RetryCount + 1,
                                Status = "DeadLetter",
                                StartedAtUtc = startedAt,
                                FinishedAtUtc = DateTime.UtcNow,
                                ErrorType = "Deserialization",
                                ErrorMessage = "Deserialization failed: unknown or invalid type",
                                WasRetried = false,
                                WasDeadLettered = true
                            }, ct);
                        }
                        return;
                    }

                    _logger.LogDebug("Dispatching event from store. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, CorrelationId={CorrelationId}, Attempt={Attempt}",
                        entry.EventId, entry.EventType, entry.CompanyId, entry.CorrelationId, entry.RetryCount + 1);
                    _metrics?.RecordEventDispatched(entry.EventType, entry.CompanyId, null);

                    await dispatcher.PublishAsync(domainEvent, alreadyStored: true, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event Store dispatcher failed to process event {EventId} ({EventType})", entry.EventId, entry.EventType);
                    try
                    {
                        var classifier = scope.ServiceProvider.GetService<IFailureClassifier>();
                        var errorType = classifier?.GetErrorType(ex) ?? "Unknown";
                        var isNonRetryable = classifier != null && classifier.IsNonRetryable(ex);
                        var markRes = await eventStore.MarkProcessedAsync(entry.EventId, success: false, ex.Message, null, errorType, isNonRetryable, ct);
                        if (markRes != null)
                        {
                            RecordFailedMetric(markRes);
                            if (isNonRetryable) _metrics?.RecordNonRetryableFailed(entry.EventType, entry.CompanyId);
                        }
                    }
                    catch (Exception markEx)
                    {
                        _logger.LogError(markEx, "Failed to mark event {EventId} as failed after dispatch error", entry.EventId);
                    }
                }
            }, cancellationToken);
        }
        finally
        {
            _concurrency.Release();
        }
    }
}
