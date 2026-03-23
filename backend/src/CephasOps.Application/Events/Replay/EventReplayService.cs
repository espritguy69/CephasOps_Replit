using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Retry or replay events from the store. Replay is gated by IEventReplayPolicy.
/// Single-event retry/replay sets a replay context with SuppressSideEffects so async handlers are not enqueued (no duplicate async job).
/// </summary>
public class EventReplayService : IEventReplayService
{
    private readonly IEventStore _eventStore;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IEventTypeRegistry _typeRegistry;
    private readonly IEventReplayPolicy _replayPolicy;
    private readonly IReplayExecutionContextAccessor _replayContextAccessor;
    private readonly ILogger<EventReplayService> _logger;

    public EventReplayService(
        IEventStore eventStore,
        IDomainEventDispatcher dispatcher,
        IEventTypeRegistry typeRegistry,
        IEventReplayPolicy replayPolicy,
        IReplayExecutionContextAccessor replayContextAccessor,
        ILogger<EventReplayService> logger)
    {
        _eventStore = eventStore;
        _dispatcher = dispatcher;
        _typeRegistry = typeRegistry;
        _replayPolicy = replayPolicy;
        _replayContextAccessor = replayContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventReplayResult> RetryAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default)
    {
        return await DispatchStoredEventAsync(eventId, scopeCompanyId, checkPolicy: false, initiatedByUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EventReplayResult> ReplayAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default)
    {
        return await DispatchStoredEventAsync(eventId, scopeCompanyId, checkPolicy: true, initiatedByUserId, cancellationToken);
    }

    private async Task<EventReplayResult> DispatchStoredEventAsync(Guid eventId, Guid? scopeCompanyId, bool checkPolicy, Guid? initiatedByUserId, CancellationToken cancellationToken)
    {
        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null)
            return new EventReplayResult { Success = false, ErrorMessage = "Event not found." };
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value)
        {
            _logger.LogWarning(
                "EventStore consistency: replay blocked due to tenant scope mismatch. EventId={EventId}, EventCompanyId={EventCompanyId}, ScopeCompanyId={ScopeCompanyId}, Operation=Replay, GuardReason=TenantMismatch",
                eventId, entry.CompanyId, scopeCompanyId.Value);
            return new EventReplayResult { Success = false, ErrorMessage = "Event not in scope." };
        }

        if (checkPolicy && !_replayPolicy.IsReplayAllowed(entry.EventType))
            return new EventReplayResult { Success = false, BlockedReason = $"Replay not allowed for event type '{entry.EventType}'." };

        var domainEvent = _typeRegistry.Deserialize(entry.EventType, entry.Payload);
        if (domainEvent == null)
            return new EventReplayResult { Success = false, ErrorMessage = $"Could not deserialize event type '{entry.EventType}'." };

        var replayMode = checkPolicy ? "Replay" : "Retry";
        var replayContext = ReplayExecutionContext.ForSingleEventRetry(replayMode);
        try
        {
            await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, async (ct) =>
            {
                _replayContextAccessor.Set(replayContext);
                await _eventStore.MarkAsProcessingAsync(eventId, ct);
                await _dispatcher.DispatchToHandlersAsync(domainEvent, ct);
                _logger.LogInformation("Event {EventId} ({EventType}) {Mode} completed. InitiatedBy={UserId}", eventId, entry.EventType, replayMode, initiatedByUserId);
            }, cancellationToken);
            return new EventReplayResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event {EventId} retry/replay failed", eventId);
            return new EventReplayResult { Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _replayContextAccessor.Set(null);
        }
    }

    /// <inheritdoc />
    public async Task<EventReplayResult> RequeueDeadLetterToPendingAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default)
    {
        var entry = await _eventStore.GetByEventIdAsync(eventId, cancellationToken);
        if (entry == null)
            return new EventReplayResult { Success = false, ErrorMessage = "Event not found." };
        if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value)
        {
            _logger.LogWarning(
                "EventStore consistency: requeue blocked due to tenant scope mismatch. EventId={EventId}, EventCompanyId={EventCompanyId}, ScopeCompanyId={ScopeCompanyId}, Operation=Requeue, GuardReason=TenantMismatch",
                eventId, entry.CompanyId, scopeCompanyId.Value);
            return new EventReplayResult { Success = false, ErrorMessage = "Event not in scope." };
        }
        if (entry.Status != "DeadLetter")
            return new EventReplayResult { Success = false, ErrorMessage = $"Requeue only allowed for DeadLetter events. Current status: {entry.Status}." };

        var reset = await _eventStore.ResetDeadLetterToPendingAsync(eventId, cancellationToken);
        if (!reset)
            return new EventReplayResult { Success = false, ErrorMessage = "Event could not be reset (not found or not DeadLetter)." };

        _logger.LogInformation(
            "Event requeued from DeadLetter to Pending. EventId={EventId}, EventType={EventType}, CompanyId={CompanyId}, CorrelationId={CorrelationId}, RetryCount={RetryCount}, InitiatedBy={UserId}",
            eventId, entry.EventType, entry.CompanyId, entry.CorrelationId, entry.RetryCount, initiatedByUserId);
        return new EventReplayResult { Success = true };
    }
}
