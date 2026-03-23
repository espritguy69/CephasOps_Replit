# Event-Driven Operations Plan

**Purpose:** Audit of existing event infrastructure and design of the minimal event-driven operations layer for CephasOps. Implementation details are in **EVENT_DRIVEN_OPERATIONS_IMPLEMENTATION.md**.

---

## 1. Existing event infrastructure (audit)

### 1.1 Domain event model

- **Base:** `CephasOps.Domain.Events.DomainEvent` (EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, TriggeredByUserId, Source, ParentEventId).
- **Application events:** Live in `CephasOps.Application.Events` (e.g. OrderStatusChangedEvent, OrderAssignedEvent, WorkflowTransitionCompletedEvent). They extend DomainEvent and set EventType/Source.

### 1.2 Event publishing path

- **In-transaction append:** `IEventStore.AppendInCurrentTransaction(IDomainEvent)` — used by WorkflowEngineService so events are written in the same DB transaction as the workflow status update. No separate “publish” call in the request.
- **Out-of-transaction:** `IEventStore.AppendAsync` — used when publishing from code that does not share a transaction (e.g. direct PublishAsync from API).
- **Storage:** EventStoreRepository (Infrastructure) persists to `EventStore` table; events are stored with Status = Pending until claimed.

### 1.3 Event store / bus / handlers

- **Store:** EventStoreRepository implements IEventStore (AppendInCurrentTransaction, AppendAsync, ClaimNextPendingBatchAsync, MarkProcessedAsync, MarkAsProcessingAsync).
- **Dispatcher:** EventStoreDispatcherHostedService (BackgroundService) polls for Pending (and retry-eligible Failed) events, claims a batch, deserializes using EventTypeRegistry, then calls **IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true)**.
- **Handler resolution:** DomainEventDispatcher.DispatchToHandlersAsync resolves all `IDomainEventHandler<TEvent>` from DI and runs them (in-process and/or enqueued per IAsyncEventSubscriber). OrderAssignedEvent handlers are thus invoked when the claimed event is dispatched.

### 1.4 Idempotency protections

- **Event processing:** IEventProcessingLogStore (when present) can enforce at-most-once per (EventId, Handler). Replay targets (e.g. Projection) restrict to IProjectionEventHandler.
- **Handler-level:** OrderAssignedOperationsHandler relies on TaskService.CreateTaskAsync idempotency by OrderId (GetTaskByOrderIdAsync). SLA enqueue: handler checks for existing Queued/Running slaevaluation job before adding one.

### 1.5 Side-effect executors

- Workflow side effects run **before** status update and event append (ExecuteSideEffectsAsync). After Part 2, installer task creation was moved to OrderAssignedEvent handler; createInstallerTask was removed from Pending→Assigned SideEffectsConfig so the event path is the single trigger.

### 1.6 Where order status changes emit events

- **Only place that appends OrderStatusChangedEvent and OrderAssignedEvent:** WorkflowEngineService.ExecuteTransitionAsync, after UpdateEntityStatusAsync (Order.Status = newStatus) and within the same transaction. Events appended: WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, and (when TargetStatus == "Assigned") OrderAssignedEvent.

### 1.7 Parent/child correlation

- DomainEvent.ParentEventId and IHasParentEvent; WorkflowTransitionCompletedEvent is the logical parent of OrderStatusChangedEvent and OrderAssignedEvent (same transition). EventTypeRegistry and replay tooling can use this for correlation.

---

## 2. Safest place to emit order lifecycle events

- **Emit only in WorkflowEngineService** after the transition has been applied (status updated, SaveChanges part of the same transaction with AppendInCurrentTransaction). No other code path should append OrderAssignedEvent to avoid duplicate or inconsistent lifecycle events.

---

## 3. Safest way to attach handlers without duplicate side effects

- **Single canonical trigger:** Installer task creation is only in OrderAssignedOperationsHandler; createInstallerTask was removed from Pending→Assigned SideEffectsConfig (seed and migration script).
- **Idempotent handlers:** Task creation by OrderId; SLA enqueue only when no pending slaevaluation job; material pack is a read-only call (GetMaterialPackAsync).
- **No duplicate automation paths:** Do not re-add createInstallerTask side effect; do not add a second handler that also creates tasks for the same event.

---

## 4. Events in scope (minimal)

| Event | When emitted | Handlers |
|-------|----------------|----------|
| OrderAssignedEvent | Order transitions to Assigned (WorkflowEngineService, same transaction as status update) | OrderAssignedOperationsHandler (installer task, material pack, SLA enqueue) |

No additional order lifecycle events (e.g. OrderMaterialsResolvedEvent) were added; one high-value event is sufficient for the first iteration.

---

## 5. What remains scheduler-based

- **SLA evaluation:** SlaEvaluationSchedulerService still runs every 15 minutes and enqueues one slaevaluation job when none pending. OrderAssignedEvent handler *also* enqueues one when there is no pending job (faster kickoff after assign).
- **Exception detection (payout anomaly):** PayoutAnomalyAlertSchedulerService remains fully scheduler-based; no event-driven path.

---

## 6. Migration notes

- **Existing DBs:** Run `remove-installer-task-side-effect-for-event-driven.sql` to remove createInstallerTask from Pending→Assigned so only the event handler creates the task.
- **New seeds:** 07_gpon_order_workflow.sql no longer includes createInstallerTask in SideEffectsConfig for Pending→Assigned.
- **Deploy order:** Deploy code (handler + registration) and run the migration script; no frontend or parser changes required.
