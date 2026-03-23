# Transactional Outbox Design

**Date:** Event Platform Layer phase.  
**Purpose:** Document the transactional outbox used for reliable domain event persistence and dispatch in CephasOps.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Role of the outbox

The **EventStore** table (and `EventStoreEntry` entity) acts as the **internal event outbox**:

- Domain events are written to the same database (and, when possible, the **same transaction**) as the business state change.
- A background worker claims rows and dispatches them to in-process handlers. This decouples “persistence” from “delivery” and guarantees that once the transaction commits, the event is durable and will eventually be processed (with retries and dead-letter).

We do **not** use a separate message broker (Kafka, RabbitMQ) in this phase; the database is the outbox store.

---

## 2. Write path

### 2.1 In-transaction append (preferred)

- **API:** `IEventStore.AppendInCurrentTransaction(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope)`.
- **Behavior:** Adds an `EventStoreEntry` to the current DbContext; **no** SaveChanges. The **caller** must call `SaveChangesAsync()` in the same transaction as the business write.
- **Use:** WorkflowEngineService after updating workflow state and entity status: it appends WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, and (when applicable) OrderAssignedEvent, then the same transaction commits.
- **Guarantee:** If the transaction rolls back, the event is not persisted. If it commits, the event is durable.

### 2.2 Out-of-transaction append

- **API:** `IEventStore.AppendAsync(IDomainEvent domainEvent, EventStoreEnvelopeMetadata? envelope, CancellationToken)`.
- **Behavior:** Opens a persistence path (may use its own scope/transaction) and inserts one row. Used when the caller does not share a DbContext with the workflow (e.g. publishing from an API that only triggers a command).
- **Guarantee:** At-least-once persistence; no atomicity with an external business transaction.

---

## 3. Stored shape (EventStoreEntry)

| Field | Purpose |
|-------|---------|
| EventId | Primary key; unique per event. |
| EventType | Handler routing and filtering. |
| Payload | JSON serialized event (no secrets). |
| OccurredAtUtc, CreatedAtUtc | Timestamps. |
| Status | Pending → Processing → Processed | Failed | DeadLetter. |
| ProcessedAtUtc | Set when Status = Processed. |
| RetryCount, NextRetryAtUtc | Retry and backoff. |
| CorrelationId, CompanyId, TriggeredByUserId, Source | Tracing and scoping. |
| EntityType, EntityId | Optional entity context for indexing. |
| ParentEventId, CausationId | Lineage and causation. |
| LastError, LastErrorAtUtc, LastHandler | Failure observability. |
| ProcessingNodeId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc | Dispatcher ownership (multi-node). |
| RootEventId, PartitionKey, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority | Platform envelope (Phase 8). |
| PayloadVersion | Contract version. |

---

## 4. Dispatch path

1. **EventStoreDispatcherHostedService** (background worker) runs on a configurable interval.
2. **Claim:** Calls `IEventStore.ClaimNextPendingBatchAsync(maxCount, maxRetriesBeforeDeadLetter, ..., nodeId, leaseExpiresAtUtc)`. Uses `FOR UPDATE SKIP LOCKED` (or equivalent) so multiple nodes can run without double-processing. Claimed rows move to Status = Processing and get lease fields set.
3. **Deserialize:** Each claimed entry is deserialized using EventTypeRegistry to the correct `IDomainEvent` type.
4. **Dispatch:** For each event, `IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true)` is called so the dispatcher does **not** persist again; it only runs handlers.
5. **Completion:** After handlers run, `IEventStore.MarkProcessedAsync(eventId, success, errorMessage, lastHandler, errorType, isNonRetryable)` is called. Success → Status = Processed, lease cleared. Failure → Status = Failed (or DeadLetter if isNonRetryable or retries exhausted), NextRetryAtUtc set with backoff.
6. **Stuck recovery:** Optional job resets events stuck in Processing (e.g. lease expired) to Failed with NextRetryAtUtc = now so they can be re-claimed.

---

## 5. Retry and dead-letter

- **Retry:** Failed events get NextRetryAtUtc with backoff (e.g. 1, 5, 15, 60 minutes). The dispatcher only claims Failed events when NextRetryAtUtc ≤ now.
- **DeadLetter:** After max retries (configurable), or when isNonRetryable is true, Status = DeadLetter. These are not auto-claimed; they can be manually reset to Pending via `ResetDeadLetterToPendingAsync` or bulk APIs for replay.
- **Poison:** Handlers can signal non-retryable (e.g. validation error) so the event goes to DeadLetter immediately.

---

## 6. Separation from transport

The outbox and dispatcher are **internal**: they persist and deliver to **in-process handlers** only. They do **not** perform HTTP or external delivery. External delivery is the responsibility of **IOutboundIntegrationBus** and **OutboundIntegrationDelivery**. If a handler wants to publish to the integration bus, it calls `IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)`; that path has its own delivery records and retries. The outbox design is thus cleanly separated from transport.

---

## 7. Future scalability

- **Multi-node:** Lease (ProcessingNodeId, ProcessingLeaseExpiresAtUtc) and FOR UPDATE SKIP LOCKED allow multiple dispatcher instances. Stuck-processing reset handles crashed nodes.
- **Partitioning:** PartitionKey can be used for ordering or backpressure within a partition; current claim logic can be extended to be partition-aware.
- **External broker (deferred):** If we later add Kafka or RabbitMQ, a **relay** could read from the EventStore (or a dedicated outbox table) and publish to the broker; the internal outbox would remain the source of truth for durability.

---

## 8. What we do not do

- We do **not** add a second outbox table for “integration events” in this phase. Integration deliveries are created when the application (or a handler) calls the outbound bus; they are not written in the same transaction as business state. That design is documented in the architecture decision.
- We do **not** store secrets in Payload; only non-sensitive event data.

This document describes the current transactional outbox design; implementation is already in place (EventStoreRepository, EventStoreDispatcherHostedService, IEventStore).
