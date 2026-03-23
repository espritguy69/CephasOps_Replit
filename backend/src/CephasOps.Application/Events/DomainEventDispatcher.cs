using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events;

/// <summary>
/// Dispatches domain events: persist to store (if available), then dispatch in-process handlers and/or enqueue async handlers.
/// During replay (IReplayExecutionContext.SuppressSideEffects), async handlers are not enqueued to prevent duplicate side effects.
/// When IEventProcessingLogStore is available, each handler is executed at most once per event (idempotency guard).
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventStore? _eventStore;
    private readonly IPlatformEventEnvelopeBuilder? _envelopeBuilder;
    private readonly IJobRunRecorderForEvents? _jobRunRecorder;
    private readonly IAsyncEventEnqueuer? _asyncEnqueuer;
    private readonly IReplayExecutionContextAccessor? _replayContextAccessor;
    private readonly IEventProcessingLogStore? _processingLogStore;
    private readonly EventBusDispatcherMetrics? _metrics;
    private readonly IFailureClassifier? _failureClassifier;
    private readonly IEventStoreAttemptHistoryStore? _attemptHistoryStore;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger,
        IEventStore? eventStore = null,
        IPlatformEventEnvelopeBuilder? envelopeBuilder = null,
        IJobRunRecorderForEvents? jobRunRecorder = null,
        IAsyncEventEnqueuer? asyncEnqueuer = null,
        IReplayExecutionContextAccessor? replayContextAccessor = null,
        IEventProcessingLogStore? processingLogStore = null,
        EventBusDispatcherMetrics? metrics = null,
        IFailureClassifier? failureClassifier = null,
        IEventStoreAttemptHistoryStore? attemptHistoryStore = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _eventStore = eventStore;
        _envelopeBuilder = envelopeBuilder;
        _jobRunRecorder = jobRunRecorder;
        _asyncEnqueuer = asyncEnqueuer;
        _replayContextAccessor = replayContextAccessor;
        _processingLogStore = processingLogStore;
        _metrics = metrics;
        _failureClassifier = failureClassifier;
        _attemptHistoryStore = attemptHistoryStore;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent domainEvent, bool alreadyStored = false, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        if (_eventStore != null && !alreadyStored)
        {
            var envelope = _envelopeBuilder?.Build(domainEvent);
            await _eventStore.AppendAsync(domainEvent, envelope, cancellationToken);
            _metrics?.RecordEventPersisted(domainEvent.EventType, domainEvent.CompanyId);
            _logger.LogInformation(
                "EventPersisted EventId={EventId} EventType={EventType} CorrelationId={CorrelationId} CompanyId={CompanyId} ParentEventId={ParentEventId}",
                domainEvent.EventId, domainEvent.EventType, domainEvent.CorrelationId, domainEvent.CompanyId,
                domainEvent is IHasParentEvent pe ? pe.ParentEventId : null);
            await _eventStore.MarkAsProcessingAsync(domainEvent.EventId, cancellationToken);
        }
        else if (_eventStore != null && alreadyStored)
        {
            // Event was already claimed (Status = Processing); no append or MarkAsProcessing needed.
        }

        await DispatchToHandlersAsync(domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchToHandlersAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        var attemptStartedAt = DateTime.UtcNow;
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>().ToList();
        var replayTarget = _replayContextAccessor?.Current?.ReplayTarget;
        if (string.Equals(replayTarget, ReplayTargets.Projection, StringComparison.OrdinalIgnoreCase))
        {
            handlers = handlers.OfType<IProjectionEventHandler<TEvent>>().Cast<IDomainEventHandler<TEvent>>().ToList();
            if (handlers.Count == 0)
            {
                _logger.LogDebug("Projection replay: no IProjectionEventHandler for event type {EventType}; marking processed.", domainEvent.EventType);
                if (_eventStore != null)
                {
                    var res = await _eventStore.MarkProcessedAsync(domainEvent.EventId, true, null, null, null, false, cancellationToken);
                    if (res != null)
                    {
                        _metrics?.RecordEventSucceeded(res.EventType, res.CompanyId, res.LastHandler, (res.ProcessedAtUtc - res.CreatedAtUtc).TotalSeconds);
                        await RecordAttemptAsync(domainEvent, attemptStartedAt, res, null, cancellationToken);
                    }
                }
                return;
            }
        }
        else if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers registered for event type {EventType}", domainEvent.EventType);
            if (_eventStore != null)
            {
                var res = await _eventStore.MarkProcessedAsync(domainEvent.EventId, true, null, null, null, false, cancellationToken);
                if (res != null)
                {
                    _metrics?.RecordEventSucceeded(res.EventType, res.CompanyId, res.LastHandler, (res.ProcessedAtUtc - res.CreatedAtUtc).TotalSeconds);
                    await RecordAttemptAsync(domainEvent, attemptStartedAt, res, null, cancellationToken);
                }
            }
            return;
        }

        var inProcess = new List<IDomainEventHandler<TEvent>>();
        var asyncHandlers = new List<IDomainEventHandler<TEvent>>();
        foreach (var h in handlers)
        {
            if (h is IAsyncEventSubscriber<TEvent>)
                asyncHandlers.Add(h);
            else
                inProcess.Add(h);
        }

        var replayOperationId = _replayContextAccessor?.Current?.ReplayOperationId;
        var correlationId = domainEvent.CorrelationId;

        Exception? lastException = null;
        string? lastHandlerName = null;
        foreach (var handler in inProcess)
        {
            var handlerName = handler.GetType().Name;

            if (_processingLogStore != null)
            {
                var claimed = await _processingLogStore.TryClaimAsync(
                    domainEvent.EventId,
                    handlerName,
                    replayOperationId,
                    correlationId,
                    cancellationToken).ConfigureAwait(false);
                if (!claimed)
                {
                    _logger.LogDebug(
                        "Event handler skipped (already processed). EventId={EventId}, HandlerName={HandlerName}",
                        domainEvent.EventId, handlerName);
                    continue;
                }
            }

            Guid? jobRunId = null;
            try
            {
                if (_jobRunRecorder != null)
                    jobRunId = await _jobRunRecorder.StartHandlerRunAsync(domainEvent, handlerName, cancellationToken);

                _logger.LogDebug("Event handler started. EventId={EventId}, HandlerName={HandlerName}", domainEvent.EventId, handlerName);
                await handler.HandleAsync(domainEvent, cancellationToken);

                if (jobRunId.HasValue && _jobRunRecorder != null)
                    await _jobRunRecorder.CompleteHandlerRunAsync(jobRunId.Value, cancellationToken);
                if (_processingLogStore != null)
                    await _processingLogStore.MarkCompletedAsync(domainEvent.EventId, handlerName, cancellationToken).ConfigureAwait(false);
                lastHandlerName = handlerName;
                _logger.LogDebug("Event handler completed. EventId={EventId}, HandlerName={HandlerName}", domainEvent.EventId, handlerName);
            }
            catch (Exception ex)
            {
                lastException = ex;
                lastHandlerName = handlerName;
                _logger.LogError(ex, "Event handler {Handler} failed for event {EventId}", handlerName, domainEvent.EventId);
                if (_processingLogStore != null)
                    await _processingLogStore.MarkFailedAsync(domainEvent.EventId, handlerName, ex.Message, cancellationToken).ConfigureAwait(false);
                if (jobRunId.HasValue && _jobRunRecorder != null)
                    await _jobRunRecorder.FailHandlerRunAsync(jobRunId.Value, ex, cancellationToken);
            }
        }

        // During replay with side-effect suppression, do not enqueue async handlers (they would run live and duplicate side effects)
        var suppressSideEffects = _replayContextAccessor?.Current?.SuppressSideEffects ?? false;
        if (asyncHandlers.Count > 0 && _asyncEnqueuer != null && !suppressSideEffects)
        {
            await _asyncEnqueuer.EnqueueAsync(domainEvent.EventId, domainEvent, cancellationToken);
            _logger.LogDebug("Enqueued {Count} async handler(s) for event {EventId}; event will be marked processed when async job completes", asyncHandlers.Count, domainEvent.EventId);
            return;
        }
        if (asyncHandlers.Count > 0 && suppressSideEffects)
        {
            _logger.LogDebug(
                "EventStore consistency: replay side-effects suppressed. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, Operation=ReplaySuppressSideEffects, GuardReason=SuppressSideEffects, AsyncHandlerCount={Count}",
                domainEvent.EventId, domainEvent.EventType, domainEvent.CompanyId, asyncHandlers.Count);
        }

        if (_eventStore != null)
        {
            var errorType = (lastException != null && _failureClassifier != null) ? _failureClassifier.GetErrorType(lastException) : null;
            var isNonRetryable = lastException != null && _failureClassifier != null && _failureClassifier.IsNonRetryable(lastException);
            var res = await _eventStore.MarkProcessedAsync(domainEvent.EventId, lastException == null, lastException?.Message, lastHandlerName, errorType, isNonRetryable, cancellationToken);
            if (res != null)
            {
                if (_metrics != null)
                {
                    var latencySeconds = (res.ProcessedAtUtc - res.CreatedAtUtc).TotalSeconds;
                    if (res.Success)
                        _metrics.RecordEventSucceeded(res.EventType, res.CompanyId, res.LastHandler, latencySeconds);
                    else
                    {
                        _metrics.RecordEventFailed(res.EventType, res.CompanyId, res.LastHandler);
                        if (res.NewStatus == "DeadLetter")
                            _metrics.RecordEventDeadLettered(res.EventType, res.CompanyId);
                        else
                            _metrics.RecordEventRetried(res.EventType, res.CompanyId);
                    }
                }
                await RecordAttemptAsync(domainEvent, attemptStartedAt, res, lastException, cancellationToken);
            }
        }
    }

    private async Task RecordAttemptAsync<TEvent>(TEvent domainEvent, DateTime attemptStartedAt, EventStoreMarkProcessedResult res, Exception? lastException, CancellationToken cancellationToken) where TEvent : IDomainEvent
    {
        if (_attemptHistoryStore == null) return;
        var status = res.Success ? "Success" : (res.NewStatus == "DeadLetter" ? "DeadLetter" : "Retry");
        var finishedAt = res.ProcessedAtUtc;
        var durationMs = (int)(finishedAt - attemptStartedAt).TotalMilliseconds;
        var record = new EventStoreAttemptRecord
        {
            EventId = domainEvent.EventId,
            EventType = res.EventType,
            CompanyId = res.CompanyId,
            HandlerName = res.LastHandler ?? "Unknown",
            AttemptNumber = res.RetryCount > 0 ? res.RetryCount : 1,
            Status = status,
            StartedAtUtc = attemptStartedAt,
            FinishedAtUtc = finishedAt,
            DurationMs = durationMs,
            ErrorType = res.Success ? null : (_failureClassifier?.GetErrorType(lastException!) ?? "Unknown"),
            ErrorMessage = lastException?.Message,
            StackTraceSummary = lastException != null ? TruncateStackTrace(lastException.StackTrace, 2000) : null,
            WasRetried = !res.Success && res.NewStatus == "Failed",
            WasDeadLettered = res.NewStatus == "DeadLetter"
        };
        await _attemptHistoryStore.RecordAttemptAsync(record, cancellationToken);
    }

    private static string? TruncateStackTrace(string? stackTrace, int maxLength)
    {
        if (string.IsNullOrEmpty(stackTrace)) return null;
        return stackTrace.Length <= maxLength ? stackTrace : stackTrace[..maxLength];
    }
}

/// <summary>
/// Records JobRun entries for event handler execution so handler runs are observable.
/// </summary>
public interface IJobRunRecorderForEvents
{
    Task<Guid> StartHandlerRunAsync(IDomainEvent domainEvent, string handlerName, CancellationToken cancellationToken = default);
    Task CompleteHandlerRunAsync(Guid jobRunId, CancellationToken cancellationToken = default);
    Task FailHandlerRunAsync(Guid jobRunId, Exception ex, CancellationToken cancellationToken = default);
}
