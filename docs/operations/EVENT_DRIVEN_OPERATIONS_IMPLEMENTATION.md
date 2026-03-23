# Event-Driven Operations Implementation

**Purpose:** Implementation details for the event-driven operations layer. Plan and audit are in **EVENT_DRIVEN_OPERATIONS_PLAN.md**.

---

## 1. Event definitions

| Event | Type string | Location | When emitted |
|-------|-------------|----------|--------------|
| OrderAssignedEvent | OrderAssigned | Application/Events/OrderAssignedEvent.cs | When order transitions to Assigned in WorkflowEngineService (same transaction as OrderStatusChangedEvent) |

**OrderAssignedEvent properties:** OrderId, WorkflowJobId, DomainEvent base (EventId, CompanyId, CorrelationId, TriggeredByUserId, etc.). Implements IHasEntityContext (EntityType=Order, EntityId=OrderId).

---

## 2. Trigger points

- **WorkflowEngineService.ExecuteTransitionAsync:** After appending OrderStatusChangedEvent, if `dto.EntityType == "Order"` and `dto.TargetStatus == "Assigned"`, appends OrderAssignedEvent with same CompanyId, CorrelationId, TriggeredByUserId, OccurredAtUtc, and sets WorkflowJobId = job.Id. No emission on invalid transition or when transition is not to Assigned.

---

## 3. Handlers

### OrderAssignedOperationsHandler

- **Interface:** IDomainEventHandler&lt;OrderAssignedEvent&gt;
- **Registration:** Program.cs — Scoped, same pattern as OrderLifecycleLedgerHandler.
- **Actions:**
  1. **Installer task:** Load order; if AssignedSiId and ServiceInstaller.UserId exist, call ITaskService.CreateTaskAsync (idempotent by OrderId). Otherwise skip (log debug).
  2. **Material pack:** Call IMaterialPackProvider.GetMaterialPackAsync(orderId, companyId). Exceptions caught and logged; handler continues.
  3. **SLA:** If no BackgroundJob with JobType=slaevaluation and State in (Queued, Running), add one with optional companyId in payload and SaveChanges. Otherwise skip (log debug).

**Idempotency:**

- Task: TaskService.CreateTaskAsync returns existing task when one exists for the order.
- Material pack: Read-only; no stored pack entity; repeated calls are safe.
- SLA: Only one slaevaluation job queued at a time (global check); repeated event does not enqueue a second.

**Retry behavior:** Handler runs in EventStoreDispatcherHostedService; on failure the event is marked Failed and retried according to EventBusDispatcherOptions (MaxRetriesBeforeDeadLetter). No at-least-once duplicate task creation because of TaskService idempotency.

---

## 4. EventTypeRegistry

- **OrderAssigned** registered in EventTypeRegistry (Replay namespace) so that claimed events from the store deserialize to OrderAssignedEvent when replayed or dispatched.

---

## 5. Dependency and migration changes

- **IMaterialPackProvider:** New interface; MaterialCollectionService implements it. OrderAssignedOperationsHandler depends on IMaterialPackProvider; DI registers MaterialCollectionService as IMaterialPackProvider.
- **createInstallerTask removed** from Pending→Assigned in 07_gpon_order_workflow.sql. Script **remove-installer-task-side-effect-for-event-driven.sql** removes it for existing DBs.

---

## 6. Tests

- **WorkflowEngineServiceTests:** ExecuteTransitionAsync_WhenEventStoreRegistered_StagesEventsInSameTransaction — asserts 3 events when transitioning to Assigned (WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent) and OrderAssignedEvent content. ExecuteTransitionAsync_WhenTransitionNotToAssigned_EmitsOnlyTwoEvents_NoOrderAssignedEvent — asserts 2 events and no OrderAssignedEvent when transitioning to InProgress.
- **OrderAssignedOperationsHandlerTests:** HandleAsync_OrderWithAssignedSi_CreatesTask_CallsMaterialPack_EnqueuesSlaJob; HandleAsync_RepeatedCall_DoesNotDuplicateTask_EnqueuesSlaOnlyOnce; HandleAsync_OrderWithoutAssignedSi_SkipsTask_StillCallsMaterialPack_EnqueuesSla; HandleAsync_OrderNotFound_DoesNothing.

---

## 7. What remains scheduler-based vs event-driven

| Automation | Scheduler | Event-driven |
|------------|-----------|--------------|
| Installer task on Pending→Assigned | No (side effect removed) | Yes — OrderAssignedOperationsHandler |
| Material pack availability | N/A (on-demand API) | Handler calls GetMaterialPackAsync on assign |
| SLA evaluation kickoff | SlaEvaluationSchedulerService every 15 min | Handler enqueues one job when none pending |
| Payout anomaly alerts | PayoutAnomalyAlertSchedulerService | No |

---

## 8. Files changed (implementation)

| Path | Purpose |
|------|---------|
| Application/Events/OrderAssignedEvent.cs | Event type |
| Application/Events/OrderAssignedOperationsHandler.cs | Handler: task, material pack, SLA |
| Application/Orders/Services/IMaterialPackProvider.cs | Interface for material pack (testability) |
| Application/Orders/Services/MaterialCollectionService.cs | Implement IMaterialPackProvider |
| Application/Workflow/Services/WorkflowEngineService.cs | Emit OrderAssignedEvent when TargetStatus == Assigned |
| Application/Events/Replay/EventTypeRegistry.cs | Register OrderAssigned |
| Api/Program.cs | Register OrderAssignedOperationsHandler, IMaterialPackProvider |
| postgresql-seeds/07_gpon_order_workflow.sql | Remove createInstallerTask from Pending→Assigned |
| scripts/remove-installer-task-side-effect-for-event-driven.sql | Migration script for existing DBs |
| Tests: WorkflowEngineServiceTests.cs, OrderAssignedOperationsHandlerTests.cs | Event emission and handler tests |
