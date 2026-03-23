namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Determines which event types are safe for manual replay. Unsafe types are blocked unless explicitly approved.
/// </summary>
public interface IEventReplayPolicy
{
    /// <summary>Returns true if the event type is allowed for replay (e.g. notification, analytics, idempotent workflow follow-up).</summary>
    bool IsReplayAllowed(string eventType);

    /// <summary>Returns true if the event type is known to be unsafe (destructive, non-idempotent) and should not be replayed without explicit approval.</summary>
    bool IsReplayBlocked(string eventType);
}
