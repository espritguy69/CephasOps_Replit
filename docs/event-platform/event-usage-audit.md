# Event Usage Audit — CephasOps Internal Event Platform

**Date:** 2025-03-10  
**Purpose:** Structured audit of current event usage before formalizing the internal event platform.  
**Scope:** EventStore, OutboundIntegrationDeliveries, InboundWebhookReceipts, retry workers, replay, handlers, webhook processors.

---

## 1. EventStore

| Aspect | Location | Notes |
|--------|----------|------|
| **Interface** | `CephasOps.Domain.Events.IEventStore` | AppendAsync, AppendInCurrentTransaction, ClaimNextPendingBatchAsync, MarkProcessedAsync, GetByEventIdAsync, bulk ops, ResetStuckProcessingAsync. |
| **Entity** | `CephasOps.Domain.Events.EventStoreEntry` | EventId, EventType, Payload (JSON), OccurredAtUtc, CompanyId, Status (Pending/Processing/Processed/Failed/DeadLetter), RetryCount, Phase 7 lease fields, Phase 8 envelope (RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority). |
| **Repository** | `CephasOps.Infrastructure.Persistence.EventStoreRepository` | Implements IEventStore; writes CompanyId from IDomainEvent; supports outbox (AppendInCurrentTransaction) and lease-based claiming. |
| **Query** | `CephasOps.Application.Events.EventStoreQueryService` | GetEventsAsync, GetEventsForReplayAsync, GetByEventIdAsync, GetDashboardAsync, GetRelatedLinksAsync, GetEventStoreCountsAsync; all accept `scopeCompanyId` and filter by CompanyId when set. |

**Where events are appended:**

- **WorkflowEngineService** — `AppendInCurrentTransaction(WorkflowTransitionCompletedEvent)`, `OrderStatusChangedEvent`, `OrderAssignedEvent` in the same transaction as workflow state change.
- **JobExecutionWorkerHostedService** — `AppendAsync(JobStartedEvent)`, `JobCompletedEvent`, `JobFailedEvent` after job state updates.
- **DomainEventDispatcher** — `AppendAsync` when `PublishAsync` is called and event is not already stored (then MarkAsProcessingAsync and dispatch).

---

## 2. OutboundIntegrationDeliveries

| Aspect | Location | Notes |
|--------|----------|------|
| **Entity** | `CephasOps.Domain.Integration.Entities.OutboundIntegrationDelivery` | SourceEventId, IdempotencyKey, Status, PayloadJson, SignatureHeaderValue, IsReplay, CompanyId, ConnectorEndpointId, etc. |
| **Bus** | `CephasOps.Application.Integration.IOutboundIntegrationBus` | PublishAsync(PlatformEventEnvelope) creates one delivery per endpoint; DispatchDeliveryAsync(deliveryId) for on-demand/retry. |
| **Retry worker** | `CephasOps.Application.Integration.OutboundIntegrationRetryWorkerHostedService` | Polls for Pending/Failed (due for retry), dispatches via IOutboundIntegrationBus.DispatchDeliveryAsync. |
| **Store** | `CephasOps.Application.Integration.OutboundDeliveryStore` | GetPendingOrRetryAsync, ListAsync, CreateDeliveryAsync, UpdateDeliveryAsync. |

**Flow:** Handlers (e.g. IntegrationEventForwardingHandler) call `IOutboundIntegrationBus.PublishAsync(envelope)`; bus creates deliveries and performs HTTP POST. Success → Delivered; failure → Failed with NextRetryAtUtc; after max attempts → DeadLetter.

---

## 3. InboundWebhookReceipts

| Aspect | Location | Notes |
|--------|----------|------|
| **Entity** | `CephasOps.Domain.Integration.Entities.InboundWebhookReceipt` | CompanyId, ConnectorKey, Status (Received/Verified/Processing/Processed/HandlerFailed/VerificationFailed), PayloadJson, ExternalIdempotencyKey. |
| **Runtime** | `CephasOps.Application.Integration.InboundWebhookRuntime` | Receives POST, creates receipt, verifies signature, runs handler, marks Processed or HandlerFailed. |
| **Replay** | `CephasOps.Application.Integration.InboundReceiptReplayService` | Replay HandlerFailed receipts by id (re-runs handler with stored payload). |
| **Store** | `CephasOps.Application.Integration.InboundWebhookReceiptStore` | CreateAsync, UpdateAsync, GetByIdAsync, ListAsync. |

---

## 4. Replay

| Aspect | Location | Notes |
|--------|----------|------|
| **Single-event** | `CephasOps.Application.Events.Replay.EventReplayService` | RetryAsync(eventId) / ReplayAsync(eventId); loads from IEventStore, checks scopeCompanyId vs entry.CompanyId (tenant isolation), IEventReplayPolicy.IsReplayAllowed(EventType), deserializes via IEventTypeRegistry, sets ReplayExecutionContext (SuppressSideEffects), dispatches via IDomainEventDispatcher. |
| **Policy** | `CephasOps.Application.Events.Replay.IEventReplayPolicy` | IsReplayAllowed(EventType), IsReplayBlocked(EventType). EventReplayPolicy allows e.g. WorkflowTransitionCompleted; blocks unknown/empty. |
| **Operational** | `CephasOps.Application.Events.Replay.IOperationalReplayPolicy` | Stricter: replay window (MaxReplayWindowDays), max count (MaxReplayCountPerRequest), company blocklist, destructive types. |
| **Bulk** | `CephasOps.Application.Events.Replay.EventBulkReplayService` | ReplayDeadLetterByFilterAsync, BulkResetDeadLetterToPendingAsync, etc., with EventStoreBulkFilter (CompanyId, EventType, FromUtc, ToUtc, MaxCount). |
| **Replay operations** | `CephasOps.Domain.Events.ReplayOperation`, ReplayOperationEvent | Persisted for audit; ReplayTarget (EventStore, Workflow, Financial, Parser, Projection). |

---

## 5. Integration Handlers & Webhook Processors

| Component | Role |
|-----------|------|
| **IntegrationEventForwardingHandler** | IDomainEventHandler for WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent; builds PlatformEventEnvelope via IDomainEventToPlatformEnvelopeBuilder, calls IOutboundIntegrationBus.PublishAsync. |
| **InboundWebhookRuntime** | Receives webhook POST, verifies, stores receipt, invokes connector-specific handler (IInboundWebhookHandler), marks receipt Processed/HandlerFailed. |
| **DomainEventDispatcher** | Resolves IDomainEventHandler&lt;T&gt; from DI; runs in-process and/or IAsyncEventSubscriber (enqueued); uses IEventProcessingLogStore for at-most-once per (EventId, Handler); during replay (SuppressSideEffects) skips enqueue. |

---

## 6. Event Handling Services (summary)

- **EventStoreDispatcherHostedService** — Polls IEventStore.ClaimNextPendingBatchAsync, deserializes with IEventTypeRegistry, calls IDomainEventDispatcher.DispatchToHandlersAsync (alreadyStored: true).
- **EventBus** — Implements IEventBus; PublishAsync/DispatchAsync delegate to IDomainEventDispatcher.
- **EventProcessingLogStore** — Records handler runs (EventId, HandlerName, State, StartedAtUtc, CompletedAtUtc); used for idempotency (at-most-once per event+handler).
- **EventStoreAttemptHistoryStore** — Phase 7: records each dispatch attempt (EventId, HandlerName, AttemptNumber, Status, ErrorType, ErrorMessage, DurationMs).

---

## 7. Where Events Are Emitted

| Source | Event types | Mechanism |
|--------|-------------|-----------|
| WorkflowEngineService | WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent | AppendInCurrentTransaction in same transaction as workflow transition |
| JobExecutionWorkerHostedService | JobStartedEvent, JobCompletedEvent, JobFailedEvent | AppendAsync after job state change |
| DomainEventDispatcher | Any IDomainEvent | AppendAsync when PublishAsync called and not already stored |
| OrdersController | OrderCreatedEvent | IEventBus.PublishAsync after CreateOrderAsync (manual create). |
| ParserService | OrderCreatedEvent | IEventBus.PublishAsync after CreateFromParsedDraftAsync success (new order only; optional IEventBus). |

---

## 8. Tight Coupling & Gaps

- **Coupling:** WorkflowEngineService directly uses IEventStore + IPlatformEventEnvelopeBuilder (appropriate for same-transaction outbox). No central “event bus” call at order creation before formalization (OrderService has no event emission).
- **Gaps identified:** (1) ~~OrderCreatedEvent at order creation~~ Done (OrdersController + ParserService). (2) No single “Subscribe” API — subscription is via DI registration of IDomainEventHandler&lt;T&gt;. (3) Integration event shape documented as PlatformEventEnvelope; Domain has no separate IIntegrationEvent type (see event-architecture.md for canonical model). (4) Observability: GET api/observability/events and EventStoreController already provide event listing/filtering by CompanyId, EventType, Status, date; traceability via EventId, CorrelationId, related links.

---

## 9. Modules That Should Emit Domain Events (recommended)

| Module | Suggested events | Status |
|--------|-------------------|--------|
| Order creation | OrderCreatedEvent | Emitted from OrdersController (manual) and ParserService (parser path). |
| Workflow | WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent | Already emitted |
| Job orchestration | JobStartedEvent, JobCompletedEvent, JobFailedEvent | Already emitted |
| Material / inventory | MaterialIssuedEvent, MaterialReturnedEvent | Documented; implement when inventory service is extended |
| Billing / invoicing | InvoiceGeneratedEvent | Documented; implement when billing flow is instrumented |
| Payroll | PayrollCalculatedEvent | Documented; implement when payroll flow is instrumented |

---

This audit is the basis for the formal internal event platform (see event-architecture.md, handler-guidelines.md, replay-strategy.md, tenant-safety.md).
