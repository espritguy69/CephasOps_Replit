# Event Platform — Structured Usage Audit

**Date:** Event Platform formalization.  
**Purpose:** Audit current event usage for the internal event platform; identify emission points, coupling, and modules that should emit domain events.

---

## 1. EventStore

| Item | Location | Description |
|------|----------|-------------|
| **IEventStore** | Domain/Events | AppendAsync, AppendInCurrentTransaction, ClaimNextPendingBatchAsync, MarkProcessedAsync, MarkAsProcessingAsync. |
| **EventStoreEntry** | Domain/Events | EventId, EventType, Payload (jsonb), Status (Pending/Processing/Processed/Failed/DeadLetter), RetryCount, NextRetryAtUtc, CompanyId, CorrelationId, CausationId, ParentEventId, RootEventId, lease fields, Phase 8 envelope (PartitionKey, SourceService, etc.). |
| **EventStoreRepository** | Infrastructure/Persistence | Implements IEventStore. AppendInCurrentTransaction adds to DbContext; caller commits. ClaimNextPendingBatchAsync uses FOR UPDATE SKIP LOCKED; retry backoff; dead-letter after max retries. |
| **EventStoreDispatcherHostedService** | Application/Events | Polls EventStore for Pending/due-retry Failed, claims batch, deserializes via EventTypeRegistry, calls IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true). Partition-aware, backpressure, multi-node lease. |

**Emission into store:** WorkflowEngineService (AppendInCurrentTransaction), JobExecutionWorkerHostedService (AppendAsync for JobStarted/Completed/Failed), DomainEventDispatcher (AppendAsync when PublishAsync and not alreadyStored).

---

## 2. OutboundIntegrationDeliveries

| Item | Location | Description |
|------|----------|-------------|
| **IOutboundIntegrationBus** | Application/Integration | PublishAsync(PlatformEventEnvelope), DispatchDeliveryAsync(deliveryId), ReplayAsync(ReplayOutboundRequest). |
| **OutboundIntegrationDelivery** | Domain/Integration/Entities | Id, ConnectorEndpointId, CompanyId, SourceEventId, EventType, Status (Pending/Delivered/Failed/DeadLetter/Replaying), PayloadJson, AttemptCount, NextRetryAtUtc, etc. |
| **IntegrationEventForwardingHandler** | Application/Integration | IDomainEventHandler for WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent; builds PlatformEventEnvelope and calls IOutboundIntegrationBus.PublishAsync. |

**Creation:** Deliveries are created by the forwarding handler (or direct PublishAsync); not in the same transaction as EventStore append unless caller explicitly wraps both.

---

## 3. InboundWebhookReceipts

| Item | Location | Description |
|------|----------|-------------|
| **IInboundWebhookRuntime** | Application/Integration | ProcessAsync(InboundWebhookRequest) → verify → persist receipt → idempotency claim → handler. |
| **InboundWebhookReceipt** | Domain/Integration/Entities | Id, CompanyId, ExternalIdempotencyKey, Status (Received/Verified/Processing/Processed/HandlerFailed/DeadLetter), PayloadJson, etc. |
| **WebhooksController** | Api/Controllers | POST /api/integration/webhooks/{connectorKey}; calls IInboundWebhookRuntime.ProcessAsync. |

---

## 4. Retry Workers

| Item | Location | Description |
|------|----------|-------------|
| **EventStoreDispatcherHostedService** | Application/Events | Retries Failed events when NextRetryAtUtc ≤ now; backoff 1/5/15/60 min; dead-letter after max retries. |
| **OutboundIntegrationRetryWorkerHostedService** | Application/Integration | If present: retries Pending/Failed outbound deliveries. (Replay API also used for on-demand retry.) |

---

## 5. Replay Services

| Item | Location | Description |
|------|----------|-------------|
| **IEventReplayService** | Application/Events/Replay | RetryAsync(eventId), ReplayAsync(eventId) — re-dispatch stored event to handlers; ReplayAsync respects IEventReplayPolicy. |
| **IOperationalReplayExecutionService** | Application/Events/Replay | RunAsync(ReplayRequestDto), ResumeAsync, RerunFailedAsync; runs batch replay with company lock, scopeCompanyId. |
| **ReplayOperation, ReplayOperationEvent** | Domain/Events | Replay run metadata and event list. |
| **EventStoreController** | Api/Controllers | POST api/event-store/events/{id}/retry, POST api/event-store/events/{id}/replay. |
| **Outbound replay** | Application/Integration | ReplayAsync(ReplayOutboundRequest) for Failed/DeadLetter deliveries. |

---

## 6. Integration Handlers & Webhook Processors

| Item | Location | Description |
|------|----------|-------------|
| **IntegrationEventForwardingHandler** | Application/Integration | Forwards WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned to IOutboundIntegrationBus. |
| **IInboundWebhookHandler** | Application/Integration | CanHandle(connectorKey, messageType), HandleAsync(IntegrationMessage, receiptId). Handlers registered per connector/messageType. |

---

## 7. Event Handling Services (Domain Event Handlers)

| Handler | Event(s) | Responsibility |
|---------|----------|----------------|
| **OrderAssignedOperationsHandler** | OrderAssignedEvent | Installer task, material pack refresh, SLA evaluation enqueue. |
| **OrderStatusNotificationDispatchHandler** | OrderStatusChangedEvent | Creates NotificationDispatch for delivery. |
| **WorkflowTransitionCompletedEventHandler** | WorkflowTransitionCompletedEvent | Logging/diagnostics. |
| **WorkflowTransitionLedgerHandler** | WorkflowTransitionCompletedEvent | Writes to WorkflowTransitionLedger. |
| **OrderLifecycleLedgerHandler** | OrderStatusChangedEvent | Ledger entry for order lifecycle. |
| **WorkflowTransitionHistoryProjectionHandler** | WorkflowTransitionCompletedEvent | WorkflowTransitionHistory projection. |
| **IntegrationEventForwardingHandler** | WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned | Forwards to outbound integration bus. |
| **JobRunRecorderForEvents** | (multiple) | Records JobRun for handler execution. |

---

## 8. Where Events Are Currently Emitted

| Source | Event(s) | Method |
|--------|----------|--------|
| **WorkflowEngineService** | WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent | AppendInCurrentTransaction (same transaction as workflow transition commit). |
| **JobExecutionWorkerHostedService** | JobStartedEvent, JobCompletedEvent, JobFailedEvent | AppendAsync after job state update. |
| **DomainEventDispatcher** | (any IDomainEvent) | AppendAsync when PublishAsync(evt, alreadyStored: false). |

---

## 9. Tight Coupling & Single Source of Truth

- **WorkflowEngineService** is the single place that appends workflow/order events in the same transaction as status change. No other code path should append these to avoid duplicates.
- **Order status → notification:** Decoupled; OrderStatusNotificationDispatchHandler reacts to OrderStatusChangedEvent.
- **Order assigned → installer task / material / SLA:** Decoupled; OrderAssignedOperationsHandler reacts to OrderAssignedEvent.
- **Domain → integration:** Decoupled; IntegrationEventForwardingHandler forwards selected events to IOutboundIntegrationBus.

---

## 10. Modules That Should Emit Domain Events (Recommendations)

| Module | Suggested event(s) | Notes |
|--------|--------------------|-------|
| Order creation (parser / API) | OrderCreated | When order is first persisted. Emit alongside existing logic; do not replace. |
| Order lifecycle | (existing) OrderStatusChanged, OrderAssigned | Already emitted from WorkflowEngineService. |
| Scheduler / assignment | (existing) OrderAssigned | Already emitted when status → Assigned. |
| Materials | MaterialIssued, MaterialReturned | When inventory is issued/returned; emit alongside existing logic. |
| Billing / invoicing | InvoiceGenerated | When invoice is generated; emit alongside existing logic. |
| Payroll | PayrollCalculated | When payroll run is calculated; emit alongside existing logic. |

Implementation: add event types and emission points in follow-up work; event bus and handler pattern are in place to consume them.

---

## 11. Summary

- **EventStore:** Internal outbox; AppendInCurrentTransaction (WorkflowEngineService, same transaction), AppendAsync (dispatcher, job lifecycle).
- **OutboundIntegrationDeliveries:** Created by IntegrationEventForwardingHandler or direct PublishAsync; retry/replay via API or retry worker.
- **InboundWebhookReceipts:** Created by InboundWebhookRuntime on webhook POST; idempotency prevents duplicate handler execution.
- **Replay:** EventStore replay by event id (tenant-scoped); operational batch replay with company lock; outbound replay for Failed/DeadLetter.
- **Handlers:** Idempotent via IEventProcessingLogStore; tenant-aware via CompanyId on event; replay supports projection-only and SuppressSideEffects for async handlers.
