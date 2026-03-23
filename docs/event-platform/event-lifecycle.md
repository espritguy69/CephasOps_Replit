# Event Lifecycle

**Purpose:** How an event moves from emission to persistence, dispatch, optional integration, and replay.

---

## 1. Emission

1. **Same-transaction (outbox):** Application code (e.g. WorkflowEngineService) performs a business change and calls `IEventStore.AppendInCurrentTransaction(domainEvent, envelope)` in the same DbContext transaction, then `SaveChangesAsync`. The event row is inserted with Status = Pending.
2. **Post-commit (bus):** Application or API calls `IEventBus.PublishAsync(domainEvent)`. The dispatcher appends to EventStore, marks as Processing, then dispatches to handlers.

In both cases the event must carry **CompanyId** (tenant); the store persists it.

---

## 2. Persistence

- Event is stored in **EventStore** (append-only): EventId, EventType, Payload (JSON), OccurredAtUtc, CompanyId, Status, PayloadVersion, and Phase 7/8 metadata.
- No payload update after insert; only processing metadata (Status, RetryCount, LastError, LastHandler, lease fields) is updated.

---

## 3. Dispatch (background worker)

1. **EventStoreDispatcherHostedService** polls and calls `IEventStore.ClaimNextPendingBatchAsync(...)` (lease-based, FOR UPDATE SKIP LOCKED).
2. For each claimed event: deserialize payload via **IEventTypeRegistry** to `IDomainEvent`, then **IDomainEventDispatcher.DispatchToHandlersAsync(domainEvent)** (alreadyStored: true).
3. For each handler: idempotency is enforced by **IEventProcessingLogStore** (at-most-once per EventId + HandlerName).
4. On success: **IEventStore.MarkProcessedAsync(eventId, success: true, ...)**; on failure: MarkProcessedAsync(success: false, errorMessage, ...) with retry or dead-letter semantics.
5. **IEventStoreAttemptHistoryStore** records each attempt (Phase 7).

---

## 4. Handlers

- **In-process:** All registered `IDomainEventHandler<TEvent>` for that event type run (except when replay target is Projection — then only `IProjectionEventHandler<T>` run).
- **Async:** Handlers implementing `IAsyncEventSubscriber<TEvent>` are enqueued (e.g. to job queue) unless **IReplayExecutionContext.SuppressSideEffects** is true (replay).
- Handlers may: update read models, send notifications, write to ledger, or call **IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)** to create outbound deliveries.

---

## 5. Outbound Integration

- When a handler (or app) calls **IOutboundIntegrationBus.PublishAsync(envelope)**:
  - One **OutboundIntegrationDelivery** per connector endpoint is created.
  - HTTP POST is performed; success → Delivered, failure → Failed with NextRetryAtUtc; after max attempts → DeadLetter.
- **OutboundIntegrationRetryWorkerHostedService** periodically retries Pending/Failed deliveries.

---

## 6. Replay

- **Single-event:** API or service calls **IEventReplayService.ReplayAsync(eventId, scopeCompanyId, ...)**. Loads event, checks tenant (entry.CompanyId == scopeCompanyId), checks **IEventReplayPolicy**, deserializes, sets replay context (SuppressSideEffects), dispatches. No duplicate persistence.
- **Bulk:** Bulk reset (e.g. DeadLetter → Pending) with **EventStoreBulkFilter** (CompanyId, EventType, date range, MaxCount); dispatcher picks up on next poll.
- Replay does not duplicate side effects when handlers are idempotent and/or SuppressSideEffects is used for async enqueue.

---

## 7. Observability and Retention

- **Observability:** List/filter via `GET /api/observability/events` and EventStore APIs; trace via EventId, CorrelationId, related links.
- **Retention:** EventPlatformRetentionWorkerHostedService deletes Processed/DeadLetter events and related rows per configured retention (e.g. EventStore Processed: 90 days).
