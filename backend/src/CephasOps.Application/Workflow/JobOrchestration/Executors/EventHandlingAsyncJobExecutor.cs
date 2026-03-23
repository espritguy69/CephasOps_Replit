using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Runs async domain event handlers for a single event loaded from the store (Phase 11).
/// Payload: eventId (required), correlationId?, companyId?.
/// Replaces legacy EventHandlingAsync BackgroundJob execution.
/// </summary>
public sealed class EventHandlingAsyncJobExecutor : IJobExecutor
{
    public string JobType => "eventhandlingasync";

    private readonly IServiceProvider _serviceProvider;
    private readonly IEventStore _eventStore;
    private readonly IEventTypeRegistry _typeRegistry;
    private readonly IJobRunRecorderForEvents _jobRunRecorder;
    private readonly IEventProcessingLogStore? _processingLogStore;
    private readonly ILogger<EventHandlingAsyncJobExecutor> _logger;

    public EventHandlingAsyncJobExecutor(
        IServiceProvider serviceProvider,
        IEventStore eventStore,
        IEventTypeRegistry typeRegistry,
        IJobRunRecorderForEvents jobRunRecorder,
        ILogger<EventHandlingAsyncJobExecutor> logger,
        IEventProcessingLogStore? processingLogStore = null)
    {
        _serviceProvider = serviceProvider;
        _eventStore = eventStore;
        _typeRegistry = typeRegistry;
        _jobRunRecorder = jobRunRecorder;
        _logger = logger;
        _processingLogStore = processingLogStore;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("eventhandlingasync job requires payload with eventId");

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
        if (payload == null || !payload.TryGetValue("eventId", out var eventIdEl))
            throw new ArgumentException("eventId is required for EventHandlingAsync job");
        var eventIdStr = eventIdEl.ValueKind == JsonValueKind.String ? eventIdEl.GetString() : eventIdEl.ToString();
        if (string.IsNullOrEmpty(eventIdStr))
            throw new ArgumentException("Invalid eventId in EventHandlingAsync payload");
        if (!Guid.TryParse(eventIdStr!.Replace("-", ""), out var eventId) && !Guid.TryParse(eventIdStr, out eventId))
            throw new ArgumentException("Invalid eventId in EventHandlingAsync payload");

        _logger.LogInformation("Executing async event handling job {JobId} for event {EventId}", job.Id, eventId);

        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null)
            throw new InvalidOperationException($"Event {eventId} not found in store");
        if (job.CompanyId.HasValue && entry.CompanyId.HasValue && job.CompanyId.Value != entry.CompanyId.Value)
        {
            _logger.LogWarning(
                "EventStore consistency: event company does not match job company. EventId={EventId}, EventCompanyId={EventCompanyId}, JobCompanyId={JobCompanyId}, Operation=EventHandlingAsync, GuardReason=TenantMismatch",
                eventId, entry.CompanyId, job.CompanyId);
            throw new InvalidOperationException(
                $"EventStore consistency: event {eventId} belongs to company {entry.CompanyId} but job is scoped to company {job.CompanyId}. Refusing to process.");
        }
        var domainEvent = _typeRegistry.Deserialize(entry.EventType, entry.Payload);
        if (domainEvent == null)
            throw new InvalidOperationException($"Could not deserialize event {eventId} (type {entry.EventType})");

        var eventType = domainEvent.GetType();
        var handlerInterfaceType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var asyncSubscriberType = typeof(IAsyncEventSubscriber<>).MakeGenericType(eventType);

        var handlers = _serviceProvider.GetServices(handlerInterfaceType);
        var asyncHandlers = new List<object>();
        foreach (var h in handlers)
        {
            if (h != null && asyncSubscriberType.IsInstanceOfType(h))
                asyncHandlers.Add(h);
        }

        Exception? lastException = null;
        string? lastHandlerName = null;
        var handleMethod = handlerInterfaceType.GetMethod("HandleAsync", new[] { eventType, typeof(CancellationToken) });
        if (handleMethod == null)
            throw new InvalidOperationException("HandleAsync not found on handler interface");

        foreach (var handler in asyncHandlers)
        {
            var handlerName = handler.GetType().Name;

            if (_processingLogStore != null)
            {
                var claimed = await _processingLogStore.TryClaimAsync(
                    eventId,
                    handlerName,
                    replayOperationId: null,
                    domainEvent.CorrelationId,
                    cancellationToken);
                if (!claimed)
                {
                    _logger.LogDebug(
                        "Async event handler skipped (already processed). EventId={EventId}, HandlerName={HandlerName}",
                        eventId, handlerName);
                    continue;
                }
            }

            Guid? jobRunId = null;
            try
            {
                jobRunId = await _jobRunRecorder.StartHandlerRunAsync(domainEvent, handlerName, cancellationToken);
                var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                if (task != null)
                    await task;
                if (jobRunId.HasValue)
                    await _jobRunRecorder.CompleteHandlerRunAsync(jobRunId.Value, cancellationToken);
                if (_processingLogStore != null)
                    await _processingLogStore.MarkCompletedAsync(eventId, handlerName, cancellationToken);
                lastHandlerName = handlerName;
            }
            catch (Exception ex)
            {
                lastException = ex;
                lastHandlerName = handlerName;
                _logger.LogError(ex, "Async event handler {Handler} failed for event {EventId}", handlerName, eventId);
                if (_processingLogStore != null)
                    await _processingLogStore.MarkFailedAsync(eventId, handlerName, ex.Message, cancellationToken);
                if (jobRunId.HasValue)
                    await _jobRunRecorder.FailHandlerRunAsync(jobRunId.Value, ex, cancellationToken);
            }
        }

        await _eventStore.MarkProcessedAsync(eventId, lastException == null, lastException?.Message, lastHandlerName, null, false, cancellationToken);
        return true;
    }
}
