# Event Handler Guidelines

## Contract

- Implement `IDomainEventHandler<TEvent>` (or `IEventHandler<TEvent>`) for the event type you handle.
- Register the handler in DI; the dispatcher resolves all handlers for that event type.

## Idempotency

- Handlers may be run more than once (retry, replay). Use `IEventProcessingLogStore` (already used by the dispatcher) so each handler runs at most once per event (per handler name).
- Within the handler, design for idempotency: e.g. use event id or (event id + entity id) to skip or upsert instead of blind insert.

## Retry-safety

- Prefer non-throwing when the outcome is already achieved (e.g. duplicate). Throw only for transient or genuine failure so the dispatcher can retry or dead-letter appropriately.
- Avoid non-idempotent side effects (e.g. sending an email) without deduplication; use async handler + job idempotency if needed.

## Tenant awareness

- The event carries `CompanyId`. Handlers must only act on data scoped to that company. Do not use a different tenant context when processing the event.
- When querying or writing data, filter by `domainEvent.CompanyId` (or equivalent) so there is no cross-tenant leakage.

## Background vs in-process

- In-process handlers run during dispatch. For long-running or I/O-heavy work, implement `IAsyncEventSubscriber<TEvent>` so the dispatcher enqueues the work and marks the event processed when the async job completes.
- During replay with SuppressSideEffects, async handlers are not enqueued to avoid duplicate side effects.

## Replay execution

- Handlers run during replay the same way as during normal dispatch, unless replay is projection-only (only `IProjectionEventHandler<T>` run). Ensure handlers can safely run again (idempotency and no duplicate external side effects when replayed).
