# Async Event Bus Architecture

**Date:** 2026-03-13  
**Purpose:** Production-safe async event-driven backbone for CephasOps. Supports scalable workflows, tenant-safe dispatch, and reliable processing without weakening tenant isolation.

---

## 1. Overview

CephasOps uses an **internal domain event bus** backed by an **outbox-style event store**. Events are persisted first, then dispatched to in-process and async handlers. All tenant-scoped events carry `CompanyId`; the dispatcher runs each event under the correct tenant scope. This architecture supports:

- **Orders** — workflow transitions, status changes, completion, assignment
- **Notifications** — order status notification dispatch (SMS/email/WhatsApp)
- **Integrations** — outbound event forwarding to connector endpoints
- **Payouts** — payout snapshot and payroll events
- **Audit timeline** — tenant activity timeline populated from selected domain events
- **Platform analytics** — event store metrics, handler success/failure, dead-letter visibility

**Principles:**

- **Tenant-safe:** Every tenant-scoped event has explicit `CompanyId`; handlers run under `TenantScopeExecutor` with that context.
- **Reliable:** Persist-then-dispatch (outbox); background dispatcher claims pending events and retries with backoff; dead-letter after max retries.
- **Incrementally adoptable:** New handlers and event types can be added without rewriting existing flows.
- **Backward-compatible:** Existing synchronous business flows remain; events are emitted in addition where appropriate.

---

## 2. Current event-like flows (discovery summary)

| Flow | Mechanism | Tenant context | Notes |
|------|------------|----------------|-------|
| **Domain event publish** | `IEventBus.PublishAsync` → `IDomainEventDispatcher` → `IEventStore.AppendAsync` + `DispatchToHandlersAsync` | Event.CompanyId set by caller; dispatcher uses entry.CompanyId | Primary path for new events |
| **Same-transaction emit** | `IEventStore.AppendInCurrentTransaction` (e.g. WorkflowEngineService) | Set on event and envelope | Used when event must be in same DB transaction |
| **Background dispatch** | EventStoreDispatcherHostedService | `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)` | Claims Pending/Failed, deserializes, dispatches with alreadyStored: true |
| **Async handlers** | IAsyncEventSubscriber → IAsyncEventEnqueuer → BackgroundJob | Job.CompanyId from event | EventHandlingAsyncJobExecutor enforces job.CompanyId == entry.CompanyId |
| **Notification dispatch** | OrderStatusNotificationDispatchHandler | Runs in event’s tenant scope (dispatcher sets scope) | Idempotent by sourceEventId in key |
| **Outbound integration** | IntegrationEventForwardingHandler → IOutboundIntegrationBus | Envelope.CompanyId from event | Tenant-prefixed idempotency key |
| **Tenant activity timeline** | TenantActivityTimelineFromEventsHandler → ITenantActivityService | Only records when event.CompanyId is set | Skips platform/unset-tenant events |
| **Replay** | EventReplayService, OperationalReplayExecutionService | scopeCompanyId filter; dispatch under entry.CompanyId; SuppressSideEffects | No async enqueue on replay |
| **Job lifecycle events** | JobExecutionWorkerHostedService → IEventStore.AppendAsync | Job.CompanyId on event | JobStartedEvent, JobCompletedEvent, JobFailedEvent |

---

## 3. Abstractions

| Abstraction | Role |
|-------------|------|
| **IDomainEvent** | Contract for all domain events: EventId, EventType, CompanyId, CorrelationId, CausationId, OccurredAtUtc, Version, Source, TriggeredByUserId. Entity context via IHasEntityContext (EntityType, EntityId). |
| **IEventBus** | Application-facing publish: `PublishAsync` (persist then dispatch), `DispatchAsync` (dispatch only, for already-stored events). |
| **IDomainEventDispatcher** | Internal: persist to store (if not already stored), then run sync handlers and enqueue async handlers. |
| **IEventStore** | Persistence: AppendAsync, AppendInCurrentTransaction, ClaimNextPendingBatchAsync, MarkAsProcessingAsync, MarkProcessedAsync, GetByEventIdAsync. |
| **IDomainEventHandler&lt;TEvent&gt;** | Sync handler for one event type. Runs in-process under the tenant scope set by the dispatcher. |
| **IAsyncEventSubscriber&lt;TEvent&gt;** | Async handler: enqueued via IAsyncEventEnqueuer; executed later by EventHandlingAsyncJobExecutor under job’s CompanyId. |
| **EventStoreEntry** | Persisted envelope: EventId, EventType, Payload (JSON), CompanyId, CorrelationId, CausationId, OccurredAtUtc, Status, RetryCount, EntityType, EntityId, etc. |
| **EventStoreEnvelopeMetadata** | Optional metadata for append: PartitionKey, RootEventId, ReplayId, SourceService, IdempotencyKey, TraceId, etc. |

---

## 4. Dispatch strategy

- **In-process:** After persist (or when alreadyStored), all registered `IDomainEventHandler<TEvent>` (that are not IAsyncEventSubscriber) run synchronously in the same scope. Tenant scope is set by the caller (API) or by EventStoreDispatcherHostedService via `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`.
- **Persisted / background:** Events are stored with Status = Pending. EventStoreDispatcherHostedService claims batches (FOR UPDATE SKIP LOCKED), marks Processing, deserializes, and calls `dispatcher.PublishAsync(domainEvent, alreadyStored: true)`. Handlers run under the event’s CompanyId. On failure, event is marked Failed with retry count; after max retries, marked DeadLetter.
- **Async handlers:** Handlers implementing IAsyncEventSubscriber are not run in-process; instead, IAsyncEventEnqueuer enqueues a BackgroundJob. The job is executed by JobExecutionWorkerHostedService; EventHandlingAsyncJobExecutor loads the event and runs the handler, enforcing job.CompanyId == entry.CompanyId.

---

## 5. Reliable dispatch (outbox-style)

- **Append:** Events are written to EventStoreEntry (same DB as business data; can use same transaction via AppendInCurrentTransaction).
- **Claim:** EventStoreDispatcherHostedService uses ClaimNextPendingBatchAsync (lease + status update) so multiple nodes can run without double-processing the same event.
- **Processing status:** Pending → Processing → Processed | Failed (with RetryCount, NextRetryAtUtc) → DeadLetter after max retries.
- **Idempotency:** IEventProcessingLogStore (TryClaimAsync per handler) ensures each handler runs at most once per (EventId, HandlerName). Outbound integration and notification dispatch use tenant-scoped idempotency keys.
- **Replay:** SuppressSideEffects prevents async handlers from being enqueued on replay; sync handlers are either idempotent or guarded (e.g. OrderAssignedOperationsHandler skips SLA enqueue when IsReplay).

---

## 6. First-use cases (event-driven integration)

| Use case | Trigger | Handler(s) | Tenant safety |
|----------|---------|------------|---------------|
| **Tenant activity timeline** | OrderCreated, OrderCompleted, OrderAssigned, OrderStatusChanged | TenantActivityTimelineFromEventsHandler | Records only when event.CompanyId is set; runs under event scope |
| **Notification request/send** | OrderStatusChanged | OrderStatusNotificationDispatchHandler | Runs in event scope; idempotent by sourceEventId |
| **Outbound integration delivery** | WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned, OrderCreated, OrderCompleted, InvoiceGenerated, MaterialIssued, MaterialReturned, PayrollCalculated | IntegrationEventForwardingHandler | Envelope.CompanyId from event; tenant-prefixed delivery key |

---

## 7. Observability

- **Metrics:** EventBusDispatcherMetrics (events persisted, claimed, succeeded, failed, dead-lettered, retried; per-event-type and per-company). EventBusMetricsCollectorHostedService snapshots to EventBusMetricsSnapshot.
- **Logs:** EventPersisted, Event handler started/completed/failed, ReplaySuppressSideEffects, tenant mismatch (GuardReason=TenantMismatch), duplicate append.
- **Event store dashboard:** IEventStoreQueryService supports 24h window metrics (processed/failed/dead-letter counts and percentages, top event types and companies). Operations overview and observability endpoints can surface event health.
- **Attempt history:** EventStoreAttemptHistoryStore records each dispatch attempt (EventId, HandlerName, Status, DurationMs, ErrorType, WasDeadLettered).

---

## 8. What remains for future phases

- **EventEnvelope** as a formal DTO for API/observability (optional; EventStoreEntry is the persisted envelope).
- **More domain events** for billing, inventory, or platform operations as needed.
- **Event-driven automation** beyond existing OrderCompletedAutomationHandler (e.g. more triggers with idempotency).
- **Request-error aggregation** to TenantMetricsDaily and correlation with event failure rates.
- **Platform observability dashboard** widget for event bus health (lag, failure rate per tenant) if desired.

---

## 9. References

- [DOMAIN_EVENTS_GUIDE.md](DOMAIN_EVENTS_GUIDE.md) — Event types, envelope contract, and how to publish/handle.
- [EVENT_HANDLING_GUARDRAILS.md](EVENT_HANDLING_GUARDRAILS.md) — Tenant safety, idempotency, and guardrails.
- [EVENTSTORE_CONSISTENCY_GUARD.md](EVENTSTORE_CONSISTENCY_GUARD.md) — Append/replay/job consistency and sync handler inventory.
