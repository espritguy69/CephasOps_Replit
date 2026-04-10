# Single-Event Retry: Suppress Async Enqueue

## Purpose

When an admin triggers **single-event retry** or **single-event replay** (Event Store “Retry” or “Replay” for one event), the event is re-dispatched to all handlers. Without hardening, that would cause **async handlers to be enqueued again**, creating a second background job for the same event and risking duplicate side effects.

This hardening ensures that during single-event retry/replay, **async handlers are not enqueued**: the dispatcher runs in-process handlers only and skips enqueue when a replay context with **SuppressSideEffects = true** is set.

## How It Works

1. **EventReplayService** (RetryAsync and ReplayAsync) invokes **DispatchStoredEventAsync**, which loads the event from the store and calls **IDomainEventDispatcher.DispatchToHandlersAsync**.

2. **Before dispatch**, the service sets a **replay context** via **IReplayExecutionContextAccessor**:
   - **ReplayExecutionContext.ForSingleEventRetry(replayMode)** creates a context with `SuppressSideEffects = true`, `ReplayOperationId = null`, `ReplayTarget = EventStore`, and `ReplayMode = "Retry"` or `"Replay"`.

3. **DomainEventDispatcher** already respects **SuppressSideEffects**: when `_replayContextAccessor.Current?.SuppressSideEffects` is true, it does **not** call **IAsyncEventEnqueuer.EnqueueAsync** for async handlers. In-process handlers still run; only the enqueue step is skipped.

4. **After dispatch** (in a `finally` block), the service clears the context with `_replayContextAccessor.Set(null)` so subsequent work is not affected.

## Code Changes

- **ReplayExecutionContext**: added **ForSingleEventRetry(string replayMode)** returning a context with `SuppressSideEffects = true`, no operation id, target EventStore.
- **EventReplayService**: inject **IReplayExecutionContextAccessor**; in **DispatchStoredEventAsync**, set the single-event-retry context before calling the dispatcher and clear it in `finally`.

## Relation to Other Hardening

- **Event Bus Idempotency Guard**: Handler-level idempotency (TryClaimAsync) still applies; handlers that already completed for that event are skipped. Suppress-async prevents a **new** async job from being created on retry.
- **Operational replay**: Batch replay (OperationalReplayExecutionService) already sets a replay context with SuppressSideEffects before dispatching. Single-event retry/replay now does the same for consistency and safety.

## Result

- **Retry** (single event): Re-dispatch runs in-process handlers only; no new async job is enqueued.
- **Replay** (single event, policy-allowed): Same behavior.
- API and UI behavior unchanged; only the internal context and dispatcher behavior are updated.
