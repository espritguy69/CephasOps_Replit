namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Retry or replay a stored event through current handlers. Retry = re-dispatch without policy check. Replay = allowed only for safe event types.
/// </summary>
public interface IEventReplayService
{
    /// <summary>Re-dispatch the event by id. Used for retry (failed/dead-letter) and replay. Returns true if dispatched.</summary>
    Task<EventReplayResult> RetryAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Replay only if event type is allowed by policy. Returns result with reason if blocked.</summary>
    Task<EventReplayResult> ReplayAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Reset a DeadLetter event to Pending so the dispatcher can pick it up. Only allowed for DeadLetter. RetryCount unchanged.</summary>
    Task<EventReplayResult> RequeueDeadLetterToPendingAsync(Guid eventId, Guid? scopeCompanyId, Guid? initiatedByUserId, CancellationToken cancellationToken = default);
}

public class EventReplayResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BlockedReason { get; set; }
}
