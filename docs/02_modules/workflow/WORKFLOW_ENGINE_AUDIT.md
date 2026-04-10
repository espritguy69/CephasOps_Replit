# CephasOps — Workflow Engine Audit

**Date:** 2026-03-09  
**Purpose:** Full map of the current workflow engine for evolution planning (event architecture, orchestration).  
**Scope:** Codebase inspection only; no code changes.

---

## 1. Executive Summary

The CephasOps workflow engine is a **status-transition engine**: it validates and executes single-step state changes (FromStatus → ToStatus) for entities (Order, Invoice) with configurable guards and side effects. It is **not** a long-running orchestrator: each transition runs synchronously in-process, creates one **WorkflowJob** record for audit, and does not enqueue background work for the transition itself. **Automation rules**, **approval workflows**, and **escalation rules** are separate settings-driven systems that run after or alongside status changes; they do not replace the engine.

**Classification:** **Status-transition engine** with rule-driven guards and side effects. It is a **partial workflow** in the sense that it governs *allowed moves* and *per-step actions*, but it has no workflow *instance* (no “this order is at step 3 of 12”), no native async steps, no event emission, and no integration with the Job Observability (JobRun) system.

---

## 2. Workflow Definitions Structure

### 2.1 Entities

| Entity | Location | Purpose |
|--------|----------|---------|
| **WorkflowDefinition** | `Domain/Workflow/Entities/WorkflowDefinition.cs` | Template per entity type. Extends `CompanyScopedEntity` (Id, CompanyId, CreatedAt, UpdatedAt, IsDeleted, etc.). |
| **WorkflowTransition** | `Domain/Workflow/Entities/WorkflowTransition.cs` | One allowed move: FromStatus (nullable) → ToStatus. Company-scoped. |
| **WorkflowJob** | `Domain/Workflow/Entities/WorkflowJob.cs` | One record per transition execution (audit trail). States: Pending, Running, Succeeded, Failed. |

**WorkflowDefinition** fields: Name, EntityType, Description, IsActive, PartnerId?, DepartmentId?, OrderTypeCode?, CreatedByUserId, UpdatedByUserId. Navigation: Transitions (collection).

**WorkflowTransition** fields: WorkflowDefinitionId, FromStatus?, ToStatus, AllowedRolesJson, GuardConditionsJson, SideEffectsConfigJson, DisplayOrder, IsActive, CreatedByUserId, UpdatedByUserId.

**WorkflowJob** fields: WorkflowDefinitionId, EntityType, EntityId, CurrentStatus, TargetStatus, State, PayloadJson, InitiatedByUserId, LastError, StartedAt, CompletedAt. **No** CorrelationId, **no** JobRunId, **no** link to BackgroundJob.

### 2.2 Resolution (Effective Workflow)

Implemented in `WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync`. Priority:

1. **Partner-specific** — EntityType + CompanyId + PartnerId + IsActive.
2. **Department-specific** — EntityType + CompanyId + DepartmentId, PartnerId = null.
3. **Order-type-specific** — EntityType + CompanyId + OrderTypeCode, PartnerId and DepartmentId = null (for Orders, OrderTypeCode = parent type code when subtype).
4. **General** — EntityType + CompanyId, PartnerId/DepartmentId/OrderTypeCode all null.

At most one active definition per scope; duplicates throw. Orders do **not** store WorkflowDefinitionId; resolution uses Order.PartnerId, Order.DepartmentId, and order type code (from OrderType, parent if subtype).

---

## 3. How Transitions Are Triggered

### 3.1 Entry Points

| Caller | Usage |
|--------|--------|
| **OrderService.UpdateOrderStatusAsync** | User/admin changes order status from UI/API. Resolves workflow, builds ExecuteTransitionDto (PartnerId, DepartmentId, OrderTypeCode from order), calls ExecuteTransitionAsync. |
| **WorkflowController** | POST /api/workflow/execute with ExecuteTransitionDto (EntityType, EntityId, TargetStatus, Payload, optional PartnerId/DepartmentId/OrderTypeCode). Used by frontend/SI app. |
| **EmailIngestionService** | After parser: transitions to Cancelled or Blocker with ExecuteTransitionDto (PartnerId from order). |
| **InvoiceSubmissionService** | After invoice submission: transitions related orders to SubmittedToPortal via ExecuteTransitionAsync. |
| **SchedulerService** | Multiple flows: assign slot (Pending→Assigned), start job (Assigned→OnTheWay), complete (MetCustomer→OrderCompleted), docket received (OrderCompleted→DocketsReceived), cancel slot. Resolves scope and calls ExecuteTransitionAsync. |
| **AgentModeService** | Uses workflow engine for allowed transitions / execution (agent flows). |

All transitions are **request-driven** (API or in-process call). There is **no** event subscriber or message consumer that triggers transitions.

### 3.2 ExecuteTransitionDto

EntityId, EntityType, TargetStatus, Payload (optional). Optional scope overrides: PartnerId, DepartmentId, OrderTypeCode. When null for Order, engine resolves from entity via IOrderPricingContextResolver / IEffectiveScopeResolver.

### 3.3 Flow Inside ExecuteTransitionAsync

1. Resolve PartnerId, DepartmentId, OrderTypeCode (DTO or from entity).
2. Get effective WorkflowDefinition (priority above).
3. Get current entity status (Order/Invoice from DB).
4. Find matching transition (FromStatus, ToStatus, IsActive).
5. Create WorkflowJob (Pending), persist.
6. Set job Running; validate guard conditions; execute side effects; update entity status; audit log (Order); fire-and-forget notification; set job Succeeded or Failed.
7. Return WorkflowJobDto.

Any exception fails the job (State = Failed, LastError set), then rethrown. **No retry** at engine level; callers may retry the request.

---

## 4. Guard Conditions

### 4.1 Evaluation

- Stored per transition in **GuardConditionsJson** (key → value, e.g. `"noSchedulingConflicts": true`).
- **GuardConditionValidatorRegistry** resolves validator by **Key** (from transition). Loads **GuardConditionDefinition** from DB (CompanyId, Key, EntityType, IsActive). Definition holds ValidatorConfigJson; validator receives entityId + config.
- All guards for the transition are evaluated; if a *required* guard (value true) is not met, validation throws and transition is aborted.
- Validators are registered in DI by **Key**; registry is built from `IEnumerable<IGuardConditionValidator>`.

### 4.2 Registered Validators (by Key)

From codebase: **NoSchedulingConflictsValidator**, **SiAssignedValidator**, **AppointmentDateSetValidator**, **BuildingSelectedValidator**, **CustomerContactProvidedValidator**, **DocketUploadedValidator**, **MaterialsSpecifiedValidator**, **NoActiveBlockersValidator**, **PhotosRequiredValidator**, **SerialsValidatedValidator**, **SplitterAssignedValidator**, **AssuranceReplacementValidator**, **ChecklistCompletedValidator**.

GuardConditionDefinition (settings) must have matching Key and EntityType; ValidatorType/ValidatorConfigJson are used by the definition, but the registry matches on **Key** and the validator’s **EntityType** property.

---

## 5. Side Effects

### 5.1 Execution

- Stored per transition in **SideEffectsConfigJson** (key → value or config object).
- **SideEffectExecutorRegistry** resolves executor by **Key**. Loads **SideEffectDefinition** from DB (CompanyId, Key, EntityType, IsActive). Definition holds ExecutorConfigJson.
- Executors run in transition order (as iterated); one failure throws and fails the whole transition (no “best effort” option at engine level).
- **Notify** is run fire-and-forget (Task.Run) *after* the engine (in WorkflowEngineService), not inside the side-effect registry; other side effects are awaited.

### 5.2 Registered Executors (by Key)

**CreateOrderStatusLogSideEffectExecutor**, **CheckMaterialCollectionSideEffectExecutor**, **CreateStockMovementSideEffectExecutor**, **NotifySideEffectExecutor**, **UpdateOrderFlagsSideEffectExecutor**, **TriggerInvoiceEligibilitySideEffectExecutor**.

SideEffectDefinition (settings) must have matching Key and EntityType.

---

## 6. Workflows and Background Jobs

- **WorkflowJob** is created for **every** transition execution. It is an **audit record** (who, what, when, success/failure). It is **not** a queue item.
- **BackgroundJob** (and **JobRun** in Job Observability) are **separate**: used for EmailIngest, PnlRebuild, MyInvoisStatusPoll, etc. The workflow engine **does not** enqueue a BackgroundJob for a transition.
- When **SchedulerService** or **EmailIngestionService** runs inside a background job (e.g. EmailIngest), that background job may call **ExecuteTransitionAsync**; the transition runs **synchronously** in the same process. The WorkflowJob is not linked to the BackgroundJob or JobRun (no foreign key or CorrelationId).
- **Conclusion:** Workflows do **not** trigger background jobs for the transition itself. They run in-process. Only post-transition notification is fire-and-forget (no job record for it).

---

## 7. Async Processing

- Transition execution is **fully synchronous** from the caller’s perspective: await ExecuteTransitionAsync until the job is Succeeded/Failed and entity status updated.
- **Async** only in the sense of **async/await** (I/O to DB, validators, executors). No “wait for external event,” no “resume later,” no step-level async.
- Notifications after order status change are dispatched with **Task.Run** (fire-and-forget); failure is logged but does not affect the WorkflowJob state.

---

## 8. Approval Workflows

- **ApprovalWorkflow** (and **ApprovalStep**) are in **Settings**: multi-step approval (e.g. RescheduleApproval, RmaApproval). Used by **ApprovalWorkflowService** (CRUD, resolution by WorkflowType/EntityType/Partner/Department/OrderType).
- **EmailSendingService** and **RMAService** use **IApprovalWorkflowService** for approval logic. They do **not** call the status-transition engine to “advance” an approval; approvals are a separate flow. When an approval completes, the *caller* may then call the workflow engine (e.g. transition to Assigned after reschedule approval).
- **Interaction:** Approval workflows are **orthogonal** to the workflow engine. The engine does not know about approval steps; it only executes status transitions when invoked.

---

## 9. Automation Rules

- **AutomationRule** (Settings): TriggerType (StatusChange, TimeBased, …), TriggerStatus, ActionType (AssignToUser, ChangeStatus, Notify, …), ActionConfigJson, etc.
- **OrderService** calls **ExecuteAutomationRulesAsync** *after* a successful status change (UpdateOrderStatusAsync). Automation can e.g. auto-assign SI; it does **not** call the workflow engine itself. If an automation action were “ChangeStatus,” that would need to be implemented (e.g. call ExecuteTransitionAsync); current codebase does not show automation triggering transitions.
- **Interaction:** Automation runs **after** the transition; it can change order fields (e.g. AssignedSiId). It is **not** part of the transition validation or side-effect pipeline.

---

## 10. Escalation Rules

- **EscalationRule** (Settings): TriggerType (TimeBased, StatusBased, …), TriggerStatus, TriggerDelayMinutes, EscalationType (NotifyUser, AssignToUser, ChangeStatus, …).
- **OrderService** calls **CheckEscalationRulesAsync** after status change (and automation). EscalationRuleService is CRUD; **evaluation** of “should we escalate now?” and execution of escalation (notify, assign, or change status) would be in a scheduler or in OrderService. If escalation includes ChangeStatus, that would require calling the workflow engine; no such link is evident in the audited callers.
- **Interaction:** Escalation is **separate** from the engine. Engine does not evaluate or fire escalation rules.

---

## 11. SLA / Business Hours

- **OrderService** has **CalculateAndTrackSlaAsync** after status change. SLA is **not** part of the workflow engine (no guard “within business hours,” no side effect “start SLA timer”). It is an integration that runs after the transition.
- No business-hours or calendar logic inside WorkflowEngineService or transition definitions.

---

## 12. Order Lifecycle and Parser / SI App Usage

- **Order lifecycle:** All order status changes (from admin UI, SI app, or API) go through **OrderService.UpdateOrderStatusAsync** → **ExecuteTransitionAsync** (with scope from order). One seeded Order workflow (GPON) defines allowed transitions (e.g. Pending→Assigned, Assigned→OnTheWay, …).
- **Parser pipeline:** **EmailIngestionService** creates orders with Status = Pending; on cancel/blocker it builds ExecuteTransitionDto and calls ExecuteTransitionAsync (PartnerId from order).
- **SI installer app:** Uses API (e.g. POST /api/workflow/execute or order status update endpoint) to move orders (e.g. OnTheWay, MetCustomer, OrderCompleted). Same engine and resolution.

---

## 13. Workflow-Related Controllers

| Controller | Responsibility |
|------------|----------------|
| **WorkflowController** | POST execute, GET allowed-transitions, GET can-transition, GET jobs (by id, by entity, by state). Company from current user (CompanyId ?? Guid.Empty). |
| **WorkflowDefinitionsController** | CRUD workflow definitions and transitions (API not re-audited here; see existing docs). |
| **GuardConditionDefinitionsController** | CRUD guard condition definitions. |
| **SideEffectDefinitionsController** | CRUD side effect definitions. |
| **OrderStatusesController** | Returns hardcoded status lists and a **fallback** transition graph when entityId/entityType not provided; **DB workflow is authoritative** for execution. |

---

## 14. Company Scoping

- **WorkflowDefinition**, **WorkflowTransition**, **WorkflowJob** are company-scoped (CompanyId on entity; queries filter by CompanyId unless single-company mode uses Guid.Empty).
- **GetEffectiveWorkflowDefinitionAsync** and **ExecuteTransitionAsync** take **companyId**; WorkflowController uses current user’s CompanyId.
- **GuardConditionDefinition** and **SideEffectDefinition** are company-scoped (CompanyId); registries load by CompanyId + Key + EntityType.
- **AutomationRule**, **EscalationRule**, **ApprovalWorkflow** are company-scoped; used in flows that already have company context.

---

## 15. Conclusion: Engine Type

| Question | Answer |
|----------|--------|
| Status-transition engine? | **Yes.** Single-step FromStatus → ToStatus, validated and executed in one call. |
| Rule-driven state machine? | **Partially.** Transitions and guards/side effects are DB-driven; resolution is rule-based (partner/department/order type). No explicit state machine DSL. |
| Partial workflow orchestrator? | **Partial.** It orchestrates *one* step (guards → side effects → status update + audit). No multi-step instance, no wait states. |
| Full workflow runtime? | **No.** No long-running instances, no event-driven transitions, no correlation with JobRun, no retry at engine level, no replay. |

**Verdict:** The engine is a **status-transition engine** with **configurable guards and side effects** and **company- and scope-aware resolution**. It is the right place to extend for **event emission** or **event-driven triggers** without replacing it; adding an event bus or process orchestration layer can sit **beside** it (Option B) or **extend** it (Option A) as decided in the evolution strategy.
