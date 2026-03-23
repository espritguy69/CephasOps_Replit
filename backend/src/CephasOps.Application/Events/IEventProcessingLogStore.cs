namespace CephasOps.Application.Events;

/// <summary>
/// Idempotency guard for event handlers: ensures each handler processes a given event at most once (one successful completion per EventId + HandlerName).
/// Used by the event bus before/after handler execution to skip already-completed handlers and to record completion or failure.
/// </summary>
public interface IEventProcessingLogStore
{
    /// <summary>
    /// Try to claim processing for this event/handler. Returns true if we claimed (caller must run handler and then MarkCompletedAsync or MarkFailedAsync).
    /// Returns false if this handler has already completed for this event (caller must skip handler).
    /// Thread-safe and safe for concurrent workers: only one claim succeeds per (EventId, HandlerName).
    /// </summary>
    /// <param name="eventId">Domain event id.</param>
    /// <param name="handlerName">Handler type name.</param>
    /// <param name="replayOperationId">Optional replay operation id (for observability).</param>
    /// <param name="correlationId">Optional correlation id from the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if claimed (proceed to run handler); false if already completed (skip handler).</returns>
    Task<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        Guid? replayOperationId,
        string? correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the current claim as completed successfully. Must be called after TryClaimAsync returned true and handler succeeded.
    /// </summary>
    Task MarkCompletedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the current claim as failed. Must be called after TryClaimAsync returned true and handler threw.
    /// </summary>
    Task MarkFailedAsync(Guid eventId, string handlerName, string? errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check whether this handler has already completed for this event (without claiming). Used for observability or pre-checks.
    /// </summary>
    Task<bool> IsCompletedAsync(Guid eventId, string handlerName, CancellationToken cancellationToken = default);
}
