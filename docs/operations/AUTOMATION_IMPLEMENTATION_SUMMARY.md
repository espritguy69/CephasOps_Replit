# Operational Automation Implementation Summary

This document summarizes the completion of the five-phase operational automation roadmap for CephasOps. The plan is maintained in **docs/operations/AUTOMATION_IMPLEMENTATION_PLAN.md**.

---

## Phase-by-phase completion

### Phase 1: Installer Task Generation — **Completed**

- **TaskItem.OrderId** (nullable) added; migration `20260309060310_AddOrderIdToTaskItem`; index `(CompanyId, OrderId)` with filter for idempotency lookups.
- **CreateTaskDto** / **TaskDto** include OrderId. **TaskService**: `GetTaskByOrderIdAsync(companyId, orderId)`; `CreateTaskAsync` is idempotent when `OrderId` is set (returns existing task for that order).
- **CreateInstallerTaskSideEffectExecutor**: Key `createInstallerTask`, EntityType Order. On transition **Pending → Assigned**, creates one task for the assigned SI (title "Complete job: Order {ServiceId}"), idempotent per order.
- Executor registered in **Program.cs**; **SideEffectDefinition** in DatabaseSeeder; **07_gpon_order_workflow.sql** and **activate-installer-task-side-effect.sql** add `createInstallerTask: true` to Pending→Assigned.
- **Tests**: CreateInstallerTaskSideEffectExecutorTests (6 tests).

---

### Phase 2: Material Pack Generation — **Completed**

- **MaterialPackDto**: OrderId, ServiceInstallerId, RequiresCollection, Message, RequiredMaterials, MissingMaterials.
- **MaterialCollectionService.GetMaterialPackAsync**: Uses existing `CheckMaterialCollectionAsync` and `GetRequiredMaterialsForOrderAsync`; no parser or material flow changes.
- **GET api/orders/{id}/material-pack**: Returns material pack for an order (required + missing). Permission: OrdersView.
- **Tests**: MaterialPackTests (3 tests).

---

### Phase 3: SLA Monitoring — **Completed**

- **SlaEvaluationSchedulerService**: HostedService that every 15 minutes enqueues one `slaevaluation` job when none queued or running. Registered in Program.cs.
- **JobDefinitionProvider**: `slaevaluation` added to Defaults (DisplayName "SLA Evaluation", RetryAllowed true, MaxRetries 2).
- **Tests**: JobDefinitionProviderTests (GetByJobTypeAsync_ReturnsDefault_ForSlaEvaluation), SlaEvaluationSchedulerServiceTests (enqueue when none pending; no duplicate when one queued).
- **Docs**: docs/operations/background_jobs.md updated with SlaEvaluationSchedulerService and slaevaluation job type.

---

### Phase 4: Exception Detection — **Completed**

- **Verified**: PayoutAnomalyAlertSchedulerService is registered; runs in-process when `PayoutAnomalyAlert:SchedulerEnabled` is true. Interval from `SchedulerIntervalHours` (1–168h). Does not enqueue a job; calls `IPayoutAnomalyAlertService.RunAlertsAsync` directly.
- **Documented**: docs/operations/background_jobs.md updated to describe the scheduler.

---

### Phase 5: Documentation Consolidation — **Completed**

- **AUTOMATION_IMPLEMENTATION_PLAN.md** is the single source of truth for the five-phase roadmap.
- **Cross-references**: background_jobs.md, PROJECT_TASKS.md (§5–§9), and operational references (installer tasks, material pack, SLA, exception detection) documented in the plan.
- **Key doc references** table added to the plan.

---

## Verification status (Part 1D)

| Automation | Status | Notes |
|------------|--------|--------|
| Workflow guardrails | **Verified working** | Only path via WorkflowController/SchedulerService → WorkflowEngineService; guards and side effects exercised; tests cover valid/invalid transition and event emission. |
| Installer task generation | **Verified working** | Event-driven path (OrderAssignedOperationsHandler); idempotent by OrderId; tests cover create, no duplicate, no SI skip, order not found. |
| Material pack | **Verified working** | GET api/orders/{id}/material-pack and handler call on assign; API and handler tests in place. |
| SLA monitoring | **Verified working** | Scheduler enqueues one when none pending; handler enqueues one when none pending; tests cover no duplicate. |
| Exception detection (payout anomaly) | **Working but limited** | Implemented and runs when SchedulerEnabled=true; no event-driven path; scheduler-based only. |

Nothing is "documented only / not yet real" for the automations above; all have real code paths and tests where applicable.

---

## Backend build and tests

- **Backend build**: Succeeds (0 errors; pre-existing warnings only).
- **Relevant tests**: CreateInstallerTaskSideEffectExecutorTests, MaterialPackTests, SlaEvaluationSchedulerServiceTests, SlaEvaluationServiceTests, JobDefinitionProviderTests, WorkflowEngineServiceTests (event emission), OrderAssignedOperationsHandlerTests — all run and pass as part of the automation/event-driven scope.

---

## Files touched (summary)

| Area | Files |
|------|--------|
| Phase 1 | TaskItem.cs, TaskDto.cs, CreateTaskDto, TaskItemConfiguration, TaskService, ITaskService, CreateInstallerTaskSideEffectExecutor.cs, Program.cs, DatabaseSeeder.cs, 07_gpon_order_workflow.sql, activate-installer-task-side-effect.sql, migration AddOrderIdToTaskItem, ApplicationDbContextModelSnapshot |
| Phase 2 | MaterialCollectionDto.cs (MaterialPackDto), MaterialCollectionService.cs, OrdersController.cs |
| Phase 3 | JobDefinitionProvider.cs, SlaEvaluationSchedulerService.cs, Program.cs |
| Phase 4 | (Verification and docs only) |
| Phase 5 | AUTOMATION_IMPLEMENTATION_PLAN.md, PROJECT_TASKS.md, background_jobs.md |
| Event-driven | OrderAssignedEvent.cs, OrderAssignedOperationsHandler.cs, IMaterialPackProvider.cs, MaterialCollectionService (implements interface), WorkflowEngineService (emit), EventTypeRegistry, Program.cs, 07_gpon_order_workflow.sql, remove-installer-task-side-effect-for-event-driven.sql |
| Tests | CreateInstallerTaskSideEffectExecutorTests.cs, MaterialPackTests.cs, JobDefinitionProviderTests.cs, SlaEvaluationSchedulerServiceTests.cs, WorkflowEngineServiceTests.cs, OrderAssignedOperationsHandlerTests.cs |

---

## Event-driven operations layer (post–Phase 5)

- **OrderAssignedEvent:** Emitted when an order transitions to Assigned (WorkflowEngineService, same transaction as status update). **OrderAssignedOperationsHandler** runs from EventStoreDispatcherHostedService and: creates installer task (idempotent by OrderId), calls IMaterialPackProvider.GetMaterialPackAsync, enqueues one slaevaluation job when none pending.
- **Single canonical path:** createInstallerTask was removed from Pending→Assigned SideEffectsConfig (07_gpon_order_workflow.sql; **remove-installer-task-side-effect-for-event-driven.sql** for existing DBs). Installer task is created only by the event handler.
- **Docs:** EVENT_DRIVEN_OPERATIONS_PLAN.md (audit), EVENT_DRIVEN_OPERATIONS_IMPLEMENTATION.md (implementation). AUTOMATION_VERIFICATION_REPORT.md updated with event-driven trigger points.

---

## Post-deployment

- **Existing DBs**: Run migration `AddOrderIdToTaskItem` (or idempotent script). For event-driven path, run **remove-installer-task-side-effect-for-event-driven.sql** so installer task is created only by OrderAssignedEvent handler (do not run activate-installer-task-side-effect.sql for new deployments). Ensure **SideEffectDefinition** for `createInstallerTask` still exists if any other transition uses it.
- **SLA**: SlaEvaluationSchedulerService starts with the API; no extra config required. OrderAssignedEvent handler also enqueues one slaevaluation job when none pending.
- **Exception detection**: Set `PayoutAnomalyAlert:SchedulerEnabled = true` and configure recipients/interval if automatic payout anomaly alerting is desired.
