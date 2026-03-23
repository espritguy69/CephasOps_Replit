# Operational Automation Implementation Plan

This document is the single source of truth for the CephasOps operational automation roadmap. Phases are executed in strict order; each phase is audited, implemented, tested, and documented before the next begins.

**Rules:** One phase at a time; audit before implementing; update this plan and `docs/PROJECT_TASKS.md` after each phase; add tests per phase; backend-first, minimal frontend; no broad refactors; no workflow redesign; no manual Create Order regression; preserve parser material flow.

---

## Phase order

1. **Installer Task Generation**
2. **Material Pack Generation**
3. **SLA Monitoring**
4. **Exception Detection**
5. **Documentation Consolidation**

---

## Phase 1: Installer Task Generation

### Current state (audit)

- **TaskItem** (`backend/src/CephasOps.Domain/Tasks/Entities/TaskItem.cs`): Generic task (Title, AssignedToUserId, RequestedByUserId, Status, etc.). No OrderId or order link.
- **TaskService**: `CreateTaskAsync(CreateTaskDto, companyId, userId)` exists. No order-scoped or duplicate-prevention logic.
- **Workflow**: `CheckMaterialCollectionSideEffectExecutor` runs on **Pending → Assigned** (Key `"checkMaterialCollection"`). It only checks materials and sends notifications; it does not create tasks.
- **Side effects**: Executors are registered in `Program.cs` and keyed by `Key`; transitions enable them via `SideEffectsConfigJson` (e.g. `{"checkMaterialCollection":true}`). Pending→Assigned is configured in `07_gpon_order_workflow.sql` and `activate-material-collection.sql`.

### Design (smallest safe)

- Add optional **OrderId** to TaskItem (and DTOs) + migration.
- Add **CreateInstallerTaskSideEffectExecutor** (Key `"createInstallerTask"`) that on transition to **Assigned** creates one task for the assigned SI (Title e.g. "Complete job: Order {ServiceId}", AssignedToUserId = SI.UserId), with **idempotency by OrderId** (no duplicate task per order).
- Register the executor in `Program.cs` and add a **SideEffectDefinition** for `createInstallerTask` / Order in DatabaseSeeder (or equivalent).
- Add `createInstallerTask: true` to Pending→Assigned in workflow seed/script.
- RequestedByUserId for the created task: use the SI’s UserId (automation “requested by” the assigned user).

### Implementation status

- [x] OrderId on TaskItem + migration (`20260309060310_AddOrderIdToTaskItem`)
- [x] CreateTaskDto / TaskDto OrderId; TaskService idempotency by OrderId; GetTaskByOrderIdAsync
- [x] CreateInstallerTaskSideEffectExecutor (Key `createInstallerTask`, Order, Pending→Assigned)
- [x] Program.cs registration; SideEffectDefinition in DatabaseSeeder; `createInstallerTask` in 07_gpon seed and `activate-installer-task-side-effect.sql`
- [x] Tests: CreateInstallerTaskSideEffectExecutorTests (6 tests)
- [x] Docs updated

---

## Phase 2: Material Pack Generation

### Current state (audit)

- **MaterialCollectionService**: `CheckMaterialCollectionAsync(orderId)` returns required vs available/missing materials.
- **CheckMaterialCollectionSideEffectExecutor** only notifies; it does not produce a “pack” list or document.

### Design (smallest safe)

- Add an API or service that returns a “material pack” for an order (e.g. GET order/{id}/material-pack or equivalent) using existing material-check logic, returning required/missing list (and optionally a simple document or structured payload). No parser or material flow changes.

### Implementation status

- [x] **MaterialPackDto** (RequiredMaterials + MissingMaterials + OrderId, ServiceInstallerId, RequiresCollection, Message)
- [x] **MaterialCollectionService.GetMaterialPackAsync** (uses CheckMaterialCollectionAsync + GetRequiredMaterialsForOrderAsync)
- [x] **GET api/orders/{id}/material-pack** (OrdersController.GetMaterialPack)
- [x] Tests: MaterialPackTests (3 tests)
- [x] Docs updated

---

## Phase 3: SLA Monitoring

### Current state (audit)

- **SlaEvaluationService**: `EvaluateAsync(companyId)` evaluates SlaRules (WorkflowTransition, EventProcessing, BackgroundJob, EventChainStall), records breaches.
- **BackgroundJobProcessorService** handles job type **"slaevaluation"** and calls `ProcessSlaEvaluationJobAsync` → `ISlaEvaluationService.EvaluateAsync`.
- **SlaMonitorController**: Exposes breaches list, rules, etc.
- **Gap**: Confirm whether a scheduled/recurring job exists that enqueues "slaevaluation" periodically; if not, add or enable it.

### Design (smallest safe)

- Confirm or add a scheduler/cron that enqueues the SLA evaluation job so monitoring runs automatically. No redesign of SLA rules or evaluation logic.

### Implementation status

- [x] **SlaEvaluationSchedulerService** added: runs every 15 min, enqueues one `slaevaluation` job when none pending.
- [x] **slaevaluation** added to JobDefinitionProvider.Defaults (DisplayName "SLA Evaluation", RetryAllowed true, MaxRetries 2).
- [x] Hosted service registered in Program.cs.
- [x] Tests: JobDefinitionProvider (slaevaluation default), SlaEvaluationSchedulerServiceTests (enqueue when none pending, no duplicate when one queued).
- [x] Docs: background_jobs.md updated.

---

## Phase 4: Exception Detection

### Current state (audit)

- **PayoutAnomalyService**, **PayoutAnomalyAlertService**, **PayoutAnomalyAlertSchedulerService**, **EmailPayoutAnomalyAlertSender** exist under `backend/src/CephasOps.Application/Rates/`.
- **JobDefinitionProvider** includes **"PayoutAnomalyAlert"**.

### Design (smallest safe)

- Verify that exception (payout anomaly) detection is scheduled and running; document it; add or adjust tests if needed. No redesign.

### Implementation status

- [x] **Verified**: PayoutAnomalyAlertSchedulerService is registered in Program.cs; runs in-process (no job enqueue) when `PayoutAnomalyAlert:SchedulerEnabled` is true. Interval from `SchedulerIntervalHours` (1–168h). JobDefinitionProvider includes "PayoutAnomalyAlert" for observability when run via API or future job-based trigger.
- [x] **Docs**: background_jobs.md updated to describe PayoutAnomalyAlertSchedulerService. No code changes; no new tests (existing coverage in Rates area).

---

## Phase 5: Documentation Consolidation

### Design

- Consolidate automation/operational docs (automation rules, SLA, material collection, exception detection) and reference them from this plan and any runbooks.

### Implementation status

- [x] **Single plan**: This file (AUTOMATION_IMPLEMENTATION_PLAN.md) is the single source of truth for the five-phase roadmap.
- [x] **Cross-references**: background_jobs.md documents schedulers and job types (slaevaluation, PayoutAnomalyAlert scheduler). PROJECT_TASKS.md lists each phase with IDs and completion notes.
- [x] **Operational references**: Automation features are documented in:
  - **Installer tasks**: Workflow side effect `createInstallerTask`; seed `07_gpon_order_workflow.sql`, `activate-installer-task-side-effect.sql`; DatabaseSeeder SideEffectDefinition.
  - **Material pack**: GET api/orders/{id}/material-pack; MaterialCollectionService.GetMaterialPackAsync; MaterialPackDto.
  - **SLA**: docs/operations/background_jobs.md (SlaEvaluationSchedulerService, slaevaluation); SlaMonitorController; SlaEvaluationService.
  - **Exception detection**: docs/operations/background_jobs.md (PayoutAnomalyAlertSchedulerService); PayoutHealthController; IPayoutAnomalyAlertService.

### Key doc references

| Topic | Document / location |
|-------|----------------------|
| Background jobs & schedulers | docs/operations/background_jobs.md |
| Workflow side effects | DatabaseSeeder (SeedDefaultSideEffectsAsync); activate-installer-task-side-effect.sql |
| Order workflow (GPON) | backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql |
| Project task tracking | docs/PROJECT_TASKS.md (§5–§8 automation phases) |

---

## End state

- Backend build succeeds.
- Relevant tests run and pass.
- `docs/operations/AUTOMATION_IMPLEMENTATION_SUMMARY.md` written with phase-by-phase completion summary.
