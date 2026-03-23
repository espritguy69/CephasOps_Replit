# Event Lifecycle

## 1. Emission

- **Same-transaction (outbox):** Service calls `IEventStore.AppendInCurrentTransaction(evt, envelope)` within the same DbContext transaction as business state, then commits. Event appears in EventStore with Status = Pending.
- **Fire-and-forget:** Service calls `IEventBus.PublishAsync(evt)`; implementation appends via `IEventStore.AppendAsync` then marks Processing and dispatches to handlers. When dispatcher is used from worker, `PublishAsync(evt, alreadyStored: true)` is used so only dispatch runs.

## 2. Claim and dispatch (worker)

- `EventStoreDispatcherHostedService` polls for Pending (or Failed with NextRetryAtUtc ≤ now), claims a batch (lease), deserializes payload to `IDomainEvent`, then calls `IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true)`.
- Dispatcher runs all `IDomainEventHandler<TEvent>` (with idempotency and optional async enqueue), then calls `IEventStore.MarkProcessedAsync` (Success / Failed / DeadLetter).

## 3. Handler execution

- Each handler is invoked after idempotency claim (EventProcessingLog). Handlers must be idempotent and retry-safe; use event identity and payload, not external state, for duplicate detection when needed.
- Async handlers are enqueued; event is marked processed when async job completes (or on failure). During replay with SuppressSideEffects, async enqueue is skipped.

## 4. Retry and dead-letter

- Failed events are retried with backoff (1/5/15/60 min). After max retries, status becomes DeadLetter.
- Operators can retry or replay via API: POST api/event-store/events/{id}/retry or /replay.

## 5. Replay

- **Single event:** Replay re-dispatches the stored event to current handlers; policy can block certain event types.
- **Batch:** Operational replay runs a filter (company, type, date range); respects company lock and tenant scope; can run projection-only or full dispatch.

## 6. Retention

- `EventPlatformRetentionWorkerHostedService` (if configured) archives or deletes processed events per `EventPlatformRetention` policy.
