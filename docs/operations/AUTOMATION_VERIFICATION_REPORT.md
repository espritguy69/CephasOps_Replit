# Automation Verification Report

**Purpose:** Document actual trigger points, services, dependencies, and weak spots for the current operational automation stack. Updated after Part 1 verification and Part 2 event-driven upgrade.

---

## 1. Workflow transition guardrails

### Actual flow

| Step | Component | Location |
|------|-----------|----------|
| Entry | **WorkflowController.ExecuteTransition** (POST /api/workflow/execute) | Api/Controllers/WorkflowController.cs |
| Auth | Requires Authorize; companyId/userId from ICurrentUserService | Same |
| Execution | **IWorkflowEngineService.ExecuteTransitionAsync** | Application/Workflow/Services/WorkflowEngineService.cs |
| Resolve scope | ResolveWorkflowScopeAsync (Order → IOrderPricingContextResolver) | Same |
| Definition | GetEffectiveWorkflowDefinitionAsync (Partner/Department/OrderType priority) | WorkflowDefinitionsService |
| Current status | GetCurrentEntityStatusAsync (Order → Orders table) | WorkflowEngineService |
| Match transition | From workflowDefinition.Transitions by FromStatus + ToStatus | Same |
| Guard validation | **ValidateGuardConditionsAsync** → GuardConditionValidatorRegistry; loads GuardConditionDefinition from DB per key; runs IGuardConditionValidator | Same + GuardConditionValidatorRegistry.cs |
| Side effects | **ExecuteSideEffectsAsync** → SideEffectExecutorRegistry; loads SideEffectDefinition from DB; runs ISideEffectExecutor per transition.SideEffectsConfig | Same + SideEffectExecutorRegistry.cs |
| Status update | **UpdateEntityStatusAsync** (Order.Status = newStatus, SaveChanges) | Same |
| Events | WorkflowTransitionCompletedEvent + OrderStatusChangedEvent (and OrderAssignedEvent after Part 2) appended to IEventStore in same transaction | Same |

### Trigger points

- **Only path to execute a transition:** WorkflowController (API) or **SchedulerService** (e.g. ConfirmSlot → ExecuteTransitionAsync with TargetStatus "Assigned"). Both call the same engine; no direct Order.Status set elsewhere for workflow-driven status.
- **Invalid transition:** Transition not in workflow definition → InvalidWorkflowTransitionException (400) with allowed next statuses.
- **Guard failure:** ValidateGuardConditionsAsync throws → job marked Failed, exception rethrown.

### Dependencies

- WorkflowDefinitions + WorkflowTransitions (DB, seeded by 07_gpon_order_workflow.sql).
- GuardConditionDefinition rows (company, key, entity type); validators registered in DI (AssuranceReplacementValidator, ChecklistCompletedValidator, etc.).
- SideEffectDefinition rows (company, key, entity type); executors registered in DI (createInstallerTask, checkMaterialCollection, etc.).

### Weak spots / assumptions

- **Bypass:** Any code that sets Order.Status without going through ExecuteTransitionAsync would bypass guards and side effects. Audit: Order.Status is set only in WorkflowEngineService.UpdateEntityStatusAsync and in parser flows (e.g. Cancel, Blocker) that call ExecuteTransitionAsync. SchedulerService uses ExecuteTransitionAsync. So guardrails hold for normal paths.
- **Guard config:** If a transition has no GuardConditions in DB, no guards run. Pending→Assigned in seed has no guard conditions by default (only SideEffectsConfig).
- **Side effect definition:** If SideEffectDefinition for createInstallerTask is missing for a company, the registry skips (logs warning, does not throw). So installer task would not be created for that company.

---

## 2. Installer task generation

### Actual flow

| Step | Component | Location |
|------|-----------|----------|
| Trigger | Pending→Assigned transition SideEffectsConfig includes "createInstallerTask" | WorkflowTransitions.SideEffectsConfigJson |
| Load definition | SideEffectExecutorRegistry loads SideEffectDefinition (Key=createInstallerTask, EntityType=Order) | SideEffectExecutorRegistry.cs |
| Execute | **CreateInstallerTaskSideEffectExecutor.ExecuteAsync** | Executors/CreateInstallerTaskSideEffectExecutor.cs |
| Condition | transition.ToStatus == "Assigned" | Same |
| Load order | _context.Orders.FirstOrDefaultAsync(entityId) | Same |
| Require SI | order.AssignedSiId required; else skip (log warning) | Same |
| Load SI | ServiceInstaller by AssignedSiId; require si.UserId | Same |
| Create task | **ITaskService.CreateTaskAsync** with OrderId=entityId, AssignedToUserId=si.UserId, Title="Complete job: Order {ServiceId}" | TaskService |
| Idempotency | **TaskService.CreateTaskAsync** when dto.OrderId has value: GetTaskByOrderIdAsync first; if existing, return it | TaskService.cs |

### Trigger points

- **Canonical (after Part 2):** OrderAssignedEvent handler only. ExecuteTransitionAsync (WorkflowController or SchedulerService) when transition is Pending→Assigned emits OrderAssignedEvent; OrderAssignedOperationsHandler creates the installer task. createInstallerTask was removed from Pending→Assigned SideEffectsConfig (seed and script) so there is one canonical path.

### Dependencies

- SideEffectDefinition "createInstallerTask" for Order (seeded in DatabaseSeeder).
- WorkflowTransition Pending→Assigned no longer includes createInstallerTask in SideEffectsConfig (07_gpon_order_workflow.sql has checkMaterialCollection only; remove-installer-task-side-effect-for-event-driven.sql removes it for existing DBs).
- TaskItem.OrderId column; TaskService.GetTaskByOrderIdAsync.

### Weak spots / assumptions

- **Missing SI or UserId:** Executor skips without failing transition (log warning). Safe.
- **Duplicate:** Idempotency by OrderId in TaskService; repeated trigger returns existing task.

---

## 3. Material pack generation

### Actual flow

| Step | Component | Location |
|------|-----------|----------|
| API | GET **api/orders/{id}/material-pack** | OrdersController.GetMaterialPack |
| Service | **MaterialCollectionService.GetMaterialPackAsync** | Orders/Services/MaterialCollectionService.cs |
| Implementation | Calls CheckMaterialCollectionAsync + GetRequiredMaterialsForOrderAsync; returns MaterialPackDto (RequiredMaterials, MissingMaterials, Message, etc.) | Same |

### Trigger points

- **On-demand only:** No automatic generation. Callers (e.g. frontend, SI app) call GET material-pack when needed.
- **After Part 2:** OrderAssignedEvent handler may call GetMaterialPackAsync to “refresh”/warm (no separate stored pack entity).

### Dependencies

- MaterialCollectionService (template service, stock ledger for SI inventory).
- Order must exist; optional AssignedSiId and template for required/missing.

### Weak spots / assumptions

- **Documented only / not real:** No “generation” at assign time unless we add it in event handler (e.g. warm cache or write to a pack table). Current design: API returns pack on demand; no duplication.

---

## 4. SLA monitoring

### Actual flow

| Step | Component | Location |
|------|-----------|----------|
| Scheduler | **SlaEvaluationSchedulerService** (HostedService) every 15 min | Workflow/Services/SlaEvaluationSchedulerService.cs |
| Check | No pending slaevaluation job (Queued or Running) | Same |
| Enqueue | Insert BackgroundJob (JobType=slaevaluation, PayloadJson={}) | Same |
| Processor | **BackgroundJobProcessorService** picks job; switch "slaevaluation" → **ProcessSlaEvaluationJobAsync** | BackgroundJobProcessorService.cs |
| Evaluation | **ISlaEvaluationService.EvaluateAsync(companyId)** | Sla/SlaEvaluationService.cs |
| Result | Evaluates SlaRules (WorkflowTransition, EventProcessing, BackgroundJob, EventChainStall); records breaches | Same |

### Trigger points

- **Scheduled:** SlaEvaluationSchedulerService every 15 min when no slaevaluation job pending.
- **After Part 2:** OrderAssignedEvent handler enqueues one slaevaluation job when none Queued/Running (idempotent); faster kickoff after assign.

### Dependencies

- JobDefinitionProvider default for "slaevaluation".
- SlaRules in DB; SlaEvaluationService; breach storage.

### Weak spots / assumptions

- **Scheduler only:** No per-order “start tracking” today; evaluation is company-wide. Safe.
- **No spam:** Scheduler only adds job when none pending. Handler-enqueued job is one per assign; processor processes sequentially.

---

## 5. Exception detection (payout anomaly)

### Actual flow

| Step | Component | Location |
|------|-----------|----------|
| Scheduler | **PayoutAnomalyAlertSchedulerService** (HostedService) when PayoutAnomalyAlert:SchedulerEnabled=true | Rates/Services/PayoutAnomalyAlertSchedulerService.cs |
| Interval | SchedulerIntervalHours (1–168) | Same |
| Execution | In-process: **IPayoutAnomalyAlertService.RunAlertsAsync** (no job enqueue) | Same |
| Result | Evaluates anomalies, sends alerts, records run history | PayoutAnomalyAlertService |

### Trigger points

- **Scheduled only:** Scheduler runs RunAlertsAsync on interval. No event-driven path; remains scheduler-based.

### Dependencies

- PayoutAnomalyAlertOptions (SchedulerEnabled, SchedulerIntervalHours, DefaultRecipientEmails, etc.).

### Weak spots / assumptions

- **Documented only / not real:** Fully implemented and running when SchedulerEnabled=true. No false positive from “scheduler assumptions” identified.

---

## 6. Summary table

| Automation | Trigger | Services | Verified (Part 1) | Event-driven (Part 2) |
|------------|---------|----------|-------------------|------------------------|
| Workflow guardrails | POST /api/workflow/execute, SchedulerService | WorkflowEngineService, GuardConditionValidatorRegistry | Yes | N/A |
| Installer task | OrderAssignedEvent (Pending→Assigned) | OrderAssignedOperationsHandler, TaskService | Yes | Event-driven (single path) |
| Material pack | GET /api/orders/{id}/material-pack + handler on assign | MaterialCollectionService/IMaterialPackProvider.GetMaterialPackAsync | Yes | Handler calls GetMaterialPackAsync on assign |
| SLA monitoring | SlaEvaluationSchedulerService every 15 min + handler on assign | SlaEvaluationSchedulerService, BackgroundJobProcessorService, SlaEvaluationService | Yes | Handler enqueues one when none pending |
| Exception detection | PayoutAnomalyAlertSchedulerService (interval) | PayoutAnomalyAlertSchedulerService, IPayoutAnomalyAlertService | Yes | Remains scheduler-based |

---

## 7. Post–Part 2: Event-driven layer

- **OrderAssignedEvent** emitted when order transitions to Assigned (WorkflowEngineService, same transaction as status update).
- **Handlers:** OrderAssignedOperationsHandler — installer task (idempotent by OrderId), material pack (IMaterialPackProvider.GetMaterialPackAsync), SLA kickoff (enqueue one slaevaluation job only when none Queued/Running).
- **Migration:** createInstallerTask removed from Pending→Assigned SideEffectsConfig (07_gpon_order_workflow.sql and remove-installer-task-side-effect-for-event-driven.sql); one canonical trigger.

See **docs/operations/EVENT_DRIVEN_OPERATIONS_PLAN.md** and **EVENT_DRIVEN_OPERATIONS_IMPLEMENTATION.md** for audit and implementation details.
