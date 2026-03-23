# Event Bus Idempotency Guard

## Purpose

The idempotency guard ensures that **each event handler processes a given event at most once**, even across:

- Retries (single-event retry or replay)
- Replay (operational replay)
- Resume (resumed replay)
- Rerun-failed
- Concurrent processing (e.g. two workers or in-process + async job)

This prevents:

- Duplicate ledger writes
- Duplicate projections
- Duplicate workflow side effects
- Replay edge-case duplication

## How It Works

1. **EventProcessingLog table**  
   One row per `(EventId, HandlerName)`. States: `Processing` | `Completed` | `Failed`.  
   A unique constraint on `(EventId, HandlerName)` ensures only one row per event/handler.

2. **Before running a handler**  
   The dispatcher (or async job processor) calls `IEventProcessingLogStore.TryClaimAsync(eventId, handlerName, ...)`.  
   - If a row already exists with `State = Completed` → return false (skip handler).  
   - If no row or row is `Failed` or stale `Processing` → claim (insert or update to `Processing`) and return true.

3. **After running the handler**  
   - On success: `MarkCompletedAsync` sets `State = Completed`, `CompletedAtUtc = now`.  
   - On failure: `MarkFailedAsync` sets `State = Failed`, `Error = message`.  
   Only the row in `Processing` is updated (same transaction as the claim).

4. **Concurrency**  
   - First claim: `INSERT`; a second concurrent insert for the same `(EventId, HandlerName)` hits the unique constraint and is treated as “could not claim” (skip).  
   - Retry/stale claim: `UPDATE ... WHERE State IN ('Failed', 'Processing') AND ...`; only one worker’s update succeeds.

## Uniqueness Rules

- **One row per (EventId, HandlerName).**  
  There is no separate “attempt” table; the same row is reused for retries (AttemptCount is incremented, State goes Processing → Completed or Failed).

- **At most one successful completion.**  
  Once `State = Completed`, no further claim is allowed for that (EventId, HandlerName). Replay, retry, and concurrent runs will skip that handler.

- **Stale Processing.**  
  If a row is in `Processing` for longer than **15 minutes** (configurable via `EventProcessingLogStore.StaleProcessingThreshold`), it is treated as stale (crashed worker). A new claim is allowed and the row is updated to `Processing` again with `AttemptCount` incremented.

## Replay Compatibility

- **Replay / resume / rerun-failed**  
  Events are re-dispatched with the same `EventId`. For each handler, `TryClaimAsync` is called. If the handler already completed for that event (e.g. from a previous run or live), the guard returns false and the handler is skipped. So:
  - Ledger handlers do not write duplicate ledger entries (and the ledger’s own idempotency key is a second line of defense).
  - Projections and other handlers are not run twice for the same event.

- **ReplayOperationId**  
  When the current execution is part of a replay, `ReplayOperationId` is passed to `TryClaimAsync` and stored in `EventProcessingLog` for observability. It does not change the uniqueness rule: idempotency is still per (EventId, HandlerName).

- **Projection replay**  
  The same guard applies: if a projection handler has already completed for that event, it is skipped.

## Where the Guard Is Integrated

- **In-process handlers**  
  `DomainEventDispatcher.DispatchToHandlersAsync`: before each in-process handler, `TryClaimAsync`; if false, skip and log; if true, run handler then `MarkCompletedAsync` or `MarkFailedAsync`.

- **Async handlers**  
  `BackgroundJobProcessorService.ProcessEventHandlingAsyncJobAsync`: same pattern for each async handler in the loop.

The guard is **central**: it is not inside individual handlers but in the single dispatch/job-processing pipeline.

## Logging and Observability

Structured logs are emitted for:

- **Event skipped (already processed):**  
  `"Event handler skipped (already processed). EventId=..., HandlerName=..."`  
  (and in the store: `"Event handler already completed, skipping"`)

- **Handler started:**  
  `"Event handler started. EventId=..., HandlerName=..."`

- **Handler completed:**  
  `"Event handler completed. EventId=..., HandlerName=..."`  
  (and in the store: `"Event handler completed. EventId=..., HandlerName=..."`)

- **Handler failed:**  
  `"Event handler {Handler} failed for event {EventId}"`  
  (and in the store: `"Event handler failed. EventId=..., HandlerName=..., Error=..."`)

- **Duplicate processing attempt:**  
  Covered by “Event handler skipped (already processed)” and store’s “already completed, skipping” / “Could not claim (concurrent insert)”.

## Limitations

- **Optional dependency**  
  If `IEventProcessingLogStore` is not registered, the dispatcher and async job processor do not use the guard (backward compatibility). Register the store (e.g. `EventProcessingLogStore`) to enable idempotency.

- **Stale threshold**  
  Default 15 minutes. If a worker runs a handler for longer than that and another process tries to process the same event/handler, the first run may still complete later and overwrite the row. For very long-running handlers, consider increasing the threshold or designing handlers to be safely re-entrant.

- **No per-attempt history**  
  Only the latest state and error are kept; AttemptCount is incremented but previous errors are not retained. For full attempt history, a separate audit table would be needed.

- **Event store vs processing log**  
  The event store’s `Status` (Pending / Processing / Processed / Failed / DeadLetter) is per **event**. The processing log is per **event + handler**. So one event can be “Processed” at the event store level while some handlers are Completed and others Failed in the log.
