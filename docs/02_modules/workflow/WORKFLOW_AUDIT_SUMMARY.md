# Workflow Implementation & Documentation Audit Summary

**Date:** 2025-03-08  
**Purpose:** Extract and summarize existing workflow logic and documentation for review (e.g. with ChatGPT).  
**Scope:** Whole repo. Updated 2025-03-08: partner workflow resolution fix applied (see §9). Updated 2026-03-08: workflow resolution extension (Partner → Department → OrderType → General, OrderTypeCode, parent code for subtypes); see §9 and docs/WORKFLOW_RESOLUTION_RULES.md.

---

## 1. WORKFLOW OVERVIEW

CephasOps has a **configurable workflow engine** that drives order status transitions. The engine is **settings-driven**: workflow definitions and transitions live in the DB (`WorkflowDefinitions`, `WorkflowTransitions`). At runtime, when a status change is requested (e.g. from Order Detail or parser), the system resolves the **effective workflow** by CompanyId + EntityType + (optionally) PartnerId, finds the allowed transition, validates **guard conditions** (from transition JSON and GuardConditionDefinitions), runs **side effects** (from transition JSON and SideEffectDefinitions), then updates the entity status and creates a **WorkflowJob** for audit. Orders are **not** assigned a workflow at creation (no WorkflowDefinitionId on Order); they start with `Status = "Pending"` and the workflow is **resolved at each transition** by CompanyId + EntityType + PartnerId. Partner-specific workflow resolution is **consistent**: the engine uses the same partner (from DTO or resolved from Order) for both execution and for GetAllowedTransitions/CanTransition. There is **one** seeded Order workflow (GPON full lifecycle) created by SQL scripts; DepartmentId is in the model but **not** used in resolution. Order Type, Order Category, and Installation Method do **not** currently influence which workflow is used; they are order attributes only. Approval workflows, escalation rules, and automation rules exist as separate settings entities and are used in specific flows (e.g. after status change) but are **distinct** from the core status transition engine.

---

## 2. ENTITIES / TABLES

| Entity | File path | Purpose | Key fields |
|--------|-----------|---------|------------|
| **WorkflowDefinition** | `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowDefinition.cs` | Template for a workflow (e.g. Order, Invoice). | Id, CompanyId, Name, EntityType, Description, IsActive, PartnerId?, DepartmentId?, OrderTypeCode?, CreatedAt, UpdatedAt |
| **WorkflowTransition** | `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowTransition.cs` | One allowed status move (FromStatus → ToStatus). | WorkflowDefinitionId, FromStatus?, ToStatus, AllowedRolesJson, GuardConditionsJson, SideEffectsConfigJson, DisplayOrder, IsActive |
| **WorkflowJob** | `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowJob.cs` | One execution record per transition (audit). | Id, CompanyId, WorkflowDefinitionId, EntityType, EntityId, CurrentStatus, TargetStatus, State (Pending/Running/Succeeded/Failed), PayloadJson, InitiatedByUserId, LastError, CreatedAt, CompletedAt |
| **GuardConditionDefinition** | `backend/src/CephasOps.Domain/Settings/Entities/GuardConditionDefinition.cs` | Defines a reusable guard (e.g. noSchedulingConflicts). | Id, CompanyId, Key, Name, Type, ConfigurationJson, IsActive |
| **SideEffectDefinition** | `backend/src/CephasOps.Domain/Settings/Entities/SideEffectDefinition.cs` | Defines a reusable side effect (e.g. Notify, CreateOrderStatusLog). | Id, CompanyId, Key, Name, Type, ConfigurationJson, IsActive |
| **ApprovalWorkflow** | `backend/src/CephasOps.Domain/Settings/Entities/ApprovalWorkflow.cs` | Multi-step approval process (e.g. RMA, Reschedule). | Name, WorkflowType, EntityType, PartnerId?, DepartmentId?, OrderType?, MinValueThreshold?, RequireAllSteps, Steps (ApprovalStep) |
| **EscalationRule** | `backend/src/CephasOps.Domain/Settings/Entities/EscalationRule.cs` | Auto-escalation by time/status/condition. | EntityType, PartnerId?, DepartmentId?, OrderType?, TriggerType, TriggerStatus?, TriggerDelayMinutes?, EscalationType, TargetUserId?, TargetRole?, etc. |
| **AutomationRule** | `backend/src/CephasOps.Domain/Settings/Entities/AutomationRule.cs` | Automated actions (e.g. auto-assign) on triggers. | EntityType, PartnerId?, DepartmentId?, OrderType?, TriggerType, TriggerStatus?, ActionType, ActionConfigJson, etc. |

**Order entity** (`backend/src/CephasOps.Domain/Orders/Entities/Order.cs`): Has **no** WorkflowDefinitionId. It has Status, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerId, DepartmentId, etc. Workflow is resolved at transition time, not stored on the order.

**Relationship to orders:** Orders get their status changed only by going through the workflow engine (when using OrderService.UpdateOrderStatusAsync or direct ExecuteTransitionAsync). The engine resolves workflow by CompanyId + EntityType + **resolution context** (PartnerId, DepartmentId, OrderTypeCode). Resolution priority: **Partner → Department → OrderType → General**. For Orders: PartnerId from Order.PartnerId, DepartmentId from Order.DepartmentId, OrderTypeCode from the order’s OrderType (parent’s Code when the selected type is a subtype, e.g. MODIFICATION_OUTDOOR → "MODIFICATION"). Callers may set `ExecuteTransitionDto.PartnerId`, `DepartmentId`, `OrderTypeCode`; when not set for Order, the engine resolves them from the order (and OrderType) so execution and allowed-transitions use the same workflow. See docs/WORKFLOW_RESOLUTION_RULES.md.

---

## 3. HOW ORDERS ENTER WORKFLOW

- **When an order is created** (manual: `OrderService.CreateOrderAsync`, parser/email: `OrderService.CreateFromParsedDraftAsync`): The order is saved with `Status = "Pending"`. **No** workflow is assigned; **no** call to the workflow engine.
- **When status is changed** (e.g. from Order Detail or API):
  1. **OrderService.UpdateOrderStatusAsync** (`backend/src/CephasOps.Application/Orders/Services/OrderService.cs`): Loads order, resolves order type code for workflow (parent code when subtype), then calls `GetEffectiveWorkflowDefinitionAsync(orderCompanyId, "Order", orderEntity.PartnerId, orderEntity.DepartmentId, orderTypeCode)`. If null, throws. Then builds `ExecuteTransitionDto` with PartnerId, DepartmentId, OrderTypeCode and calls `ExecuteTransitionAsync(orderCompanyId, executeDto, userId)`.
  2. **WorkflowEngineService.ExecuteTransitionAsync**: Resolves partnerId, departmentId, orderTypeCode from DTO or from Order when EntityType is Order. Calls `GetEffectiveWorkflowDefinitionAsync(companyId, entityType, partnerId, departmentId, orderTypeCode)`. The **same** context is used for GetAllowedTransitionsAsync and CanTransitionAsync, so UI and execution stay consistent.
- **Workflow assignment:** There is **no** “assign workflow to order” step; orders do **not** store WorkflowDefinitionId. Workflow is **resolved at each transition** by:
  - CompanyId (from order/context)
  - EntityType = "Order"
  - PartnerId, DepartmentId, OrderTypeCode (from DTO or resolved from Order; OrderTypeCode = parent OrderType.Code when order's type is a subtype)
  - **Resolution order:** 1) Partner-specific, 2) Department-specific, 3) OrderType-specific, 4) General (all null).
- **Seeding:** Workflow definitions/transitions are **not** created by DatabaseSeeder. They are created by:
  - `backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql`
  - `scripts/deploy/workflow/10_seed_order_workflow_if_missing.sql`
  Seeds create one Order WorkflowDefinition (no PartnerId, no DepartmentId) and ~30 transitions (Pending→Assigned, Assigned→OnTheWay, …, invoice rejection loop, etc.).
- **Fallback if no workflow:** If `GetEffectiveWorkflowDefinitionAsync` returns null, OrderService throws: "No active workflow definition found for Order entity. Please configure workflow definitions first."

**Order Type (for workflow):** Workflow resolution uses **OrderTypeCode** (string) on WorkflowDefinition. For an Order, the code is the **parent** order type code when the selected OrderType is a subtype (e.g. MODIFICATION_OUTDOOR → "MODIFICATION", STANDARD → "ASSURANCE"); otherwise the type’s own Code (e.g. ACTIVATION). Order Category and Installation Method are not used for workflow selection.

---

## 4. HOW WORKFLOW MOVES

- **Transitions:** Stored in DB per workflow definition (FromStatus, ToStatus, AllowedRolesJson, GuardConditionsJson, SideEffectsConfigJson). Allowed transitions are those where FromStatus matches current status (or null for initial) and ToStatus matches target; transition must be IsActive.
- **Status change flow:** Request → Resolve context (partnerId, departmentId, orderTypeCode from DTO or from Order when EntityType is Order; for OrderTypeCode use parent type code when subtype) → Get effective workflow (priority: Partner → Department → OrderType → General) → Get current entity status → Find matching transition → Create WorkflowJob (Pending) → Validate guard conditions → Execute side effects → Update entity status → Audit + notifications → Job state Succeeded/Failed.
- **Guard conditions:** Stored per transition as JSON (key → value). Engine uses `GuardConditionValidatorRegistry` and `GuardConditionDefinition` (by key) to validate; e.g. `noSchedulingConflicts` is validated for Order.
- **Side effects:** Stored per transition as JSON. Engine uses `SideEffectExecutorRegistry` and `SideEffectDefinition` to run; e.g. Notify, CreateOrderStatusLog, UpdateOrderFlags, TriggerInvoiceEligibility, CreateStockMovement (see `backend/src/CephasOps.Application/Workflow/Executors/`).
- **Blockers / Assigned / Completed / Rejected / Invoiced:** All are statuses in the same Order workflow. Transitions between them are in the seeded SQL (e.g. Blocker→Assigned, Invoiced→Rejected, Rejected→ReadyForInvoice, Reinvoice→Invoiced). Blocker validation (reason/category) is done in OrderService before calling the engine.
- **Transitions:** Config-driven (DB). No hardcoded transition matrix in the engine; the engine only checks DB transitions.
- **Sign-off / role-based:** AllowedRolesJson on each transition; empty means all roles. Engine filters allowed transitions by user roles in `GetAllowedTransitionsAsync`. No separate “sign-off” entity; approval workflows (ApprovalWorkflow) are a separate feature (e.g. RMA, Reschedule) and are not the same as status transitions.

---

## 5. SETTINGS PAGES

| Route | File path | Purpose / what can be configured |
|-------|-----------|-----------------------------------|
| `/workflow/definitions` | `frontend/src/pages/workflow/WorkflowDefinitionsPage.tsx` | List/create/edit workflow definitions; list/add/edit/delete transitions; attach guard condition keys and side effect keys to transitions. |
| `/workflow/guard-conditions` | `frontend/src/pages/workflow/GuardConditionsPage.tsx` | CRUD for Guard Condition Definitions (key, name, type, config). |
| `/workflow/side-effects` | `frontend/src/pages/workflow/SideEffectsPage.tsx` | CRUD for Side Effect Definitions (key, name, type, config). |
| `/settings/order-statuses` | `frontend/src/pages/settings/OrderStatusesPage.tsx` | **Reference/list only:** shows hardcoded Order/RMA/KPI status lists and transition graph from OrderStatusesController (not the DB workflow). |
| `/settings/guard-condition-definitions` | `frontend/src/pages/settings/GuardConditionDefinitionsPage.tsx` | Alternative page for guard condition definitions (grid + export). |
| `/settings/side-effect-definitions` | `frontend/src/pages/settings/SideEffectDefinitionsPage.tsx` | Side effect definitions (settings). |
| `/settings/approval-workflows` | `frontend/src/pages/settings/ApprovalWorkflowsPage.tsx` | CRUD for Approval Workflows (multi-step approvals; workflow type, entity type, partner, department, order type, steps). |
| `/settings/automation-rules` | Lazy in `frontend/src/routes/settingsRoutes.tsx` → AutomationRulesPage | Automation rules (trigger + action). |
| `/settings/escalation-rules` | Lazy → EscalationRulesPage | Escalation rules (trigger + escalation action). |

Sidebar: `frontend/src/components/layout/Sidebar.tsx` (e.g. `/workflow/definitions`, `/settings/order-statuses`, `/settings/guard-condition-definitions`, `/settings/side-effect-definitions`, `/settings/approval-workflows`, `/settings/automation-rules`, `/settings/escalation-rules`).

---

## 6. DOCUMENTS FOUND

| Path | Short summary | Matches current code? |
|------|----------------|------------------------|
| `docs/01_system/WORKFLOW_ENGINE.md` | Full workflow engine spec: status list, validation rules, activation (partner/general), permissions, overrides, RMA, splitter, invoicing. | Mostly yes. Notes DepartmentId not used in GetEffectiveWorkflowDefinitionAsync; code confirms. |
| `docs/01_system/WORKFLOW_ENGINE_FLOW.md` | Flow diagrams: resolution priority, transition execution, validation, guards, side effects, override, department ownership. | Yes; department ownership in doc may be aspirational (not fully in engine). |
| `docs/02_modules/workflow/WORKFLOW.md` | System diagram, transition execution steps, guard/side effect flow, entities, API endpoints. | Yes. |
| `docs/02_modules/workflow/WORKFLOW_ACTIVATION_RULES.md` | Activation rules, resolution priority (partner → general), code refs to WorkflowDefinitionsService. | Yes; doc mentions DepartmentId in resolution but code only uses PartnerId then null. |
| `docs/05_data_model/entities/workflow_entities.md` | WorkflowJob, BackgroundJob, SystemLog. | Partial; WorkflowDefinition/WorkflowTransition are in settings_entities.md. |
| `docs/05_data_model/entities/settings_entities.md` | WorkflowDefinition, WorkflowTransition (high-level). | Yes. |
| `docs/05_data_model/relationships/workflow_relationships.md` | Order lifecycle, email→order, reschedule, dockets, materials, background jobs. | Conceptual; doesn’t detail workflow engine resolution. |
| `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md` | Single source of truth for status names (PascalCase), Order/RMA/KPI status lists. | Yes; aligns with OrderStatusesController and DB. |
| `docs/business/order_lifecycle_and_statuses.md` | Canonical GPON lifecycle; DB workflow is authoritative; controller graph incomplete. | Yes. |
| `docs/architecture/21_workflow_order_lifecycle.md` | Mermaid lifecycle + sequence; aligned with WORKFLOW_STATUS_REFERENCE and 07_gpon_order_workflow.sql. | Yes; InProgress is not an order status (reference and seed use Assigned→OnTheWay→MetCustomer→OrderCompleted). |
| `docs/02_modules/orders/ORDER_CREATION_LOGIC.md` | Order creation paths, field mapping, department/order type; workflow “status transitions” and “workflow definitions” mentioned. | Yes; no workflow assignment at creation. |
| `docs/workflow-seeding-runbook.md` | When to run, scripts (00 check, 10 seed, 20/30 optional), verification. | Yes. |
| `docs/operations/db_workflow_baseline_spec.md` | Minimum GPON transitions; DB is runtime authority. | Yes. |
| `docs/02_modules/approval-workflows/USAGE.md` | How to configure Approval Workflows (separate from status workflow). | Yes. |

---

## 7. SOURCE OF TRUTH

- **Runtime behaviour:** Database: `WorkflowDefinitions` + `WorkflowTransitions` (and transition GuardConditionsJson / SideEffectsConfigJson). Seeded by SQL scripts (e.g. `07_gpon_order_workflow.sql`, `10_seed_order_workflow_if_missing.sql`).
- **Status names/codes:** Doc: `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md`; backend list: `OrderStatusesController` static lists (OrderWorkflowStatuses, RmaWorkflowStatuses, KpiWorkflowStatuses). DB transitions use the same codes (e.g. Pending, Assigned, Rejected).
- **Business rules (validations, permissions):** Doc: `docs/01_system/WORKFLOW_ENGINE.md`. Code: OrderService (blocker validation), WorkflowEngineService (guards/side effects via registries), transition AllowedRolesJson.
- **Seed data:** SQL scripts under `backend/scripts/postgresql-seeds/` and `scripts/deploy/workflow/`. DatabaseSeeder seeds GuardConditionDefinition and SideEffectDefinition only, not WorkflowDefinition/WorkflowTransitions.
- **Settings UI:** Workflow Definitions, Guard Conditions, Side Effects pages can change definitions and transition config; Order Statuses page is read-only reference (hardcoded list).

---

## 8. GAPS / RISKS

1. ~~**PartnerId not used in execution**~~ **FIXED (2025-03-08):** ExecuteTransitionDto now has optional PartnerId; OrderService and other Order callers pass it; the engine uses it (or resolves from Order when null) so partner-specific workflows are used consistently for execution and for GetAllowedTransitions/CanTransition.
2. ~~**DepartmentId not used in resolution**~~ **FIXED (2026-03-08):** Resolution now supports DepartmentId (priority 2) and OrderTypeCode (priority 3). See docs/WORKFLOW_RESOLUTION_RULES.md.
3. ~~**Order Type not used for workflow selection**~~ **FIXED (2026-03-08):** OrderTypeCode (string) on WorkflowDefinition; for Orders, parent order type code is used when the selected type is a subtype. Order Category and Installation Method remain order attributes only.
4. **Dual status/transition source:** OrderStatusesController returns hardcoded status lists and a transition graph; the real allowed transitions are in the DB. Docs say the controller graph is incomplete and DB is authority; UIs that rely on the controller could be out of sync.
5. ~~**Doc vs code (architecture diagram):** `21_workflow_order_lifecycle.md` includes “InProgress”; it’s not in WORKFLOW_STATUS_REFERENCE or in the seeded transitions.~~ **FIXED:** Lifecycle docs aligned with WORKFLOW_STATUS_REFERENCE and 07_gpon_order_workflow.sql; InProgress clarified as not an order status; diagrams and status definitions updated.
6. **No workflow assignment at order creation:** Orders are not linked to a workflow definition at create time. If later you add multiple workflows (e.g. by order type), you’d need either to assign at creation or keep resolving at each step (and then pass partner/department/order type into resolution).
7. **Approval workflows vs status workflow:** ApprovalWorkflow is a separate concept (multi-step approvals); it’s not the same as status transitions. Ensure stakeholders don’t conflate “approval workflow” with “order status workflow.”

---

## 9. RECOMMENDED NEXT STEP

- ~~**Short term: Fix partner resolution**~~ **DONE:** PartnerId is now passed through ExecuteTransitionDto and used in the engine; when not provided for Order, the engine resolves it from the order so partner-specific workflow works consistently.
- **Next (if needed):** If you need workflow by Order Type or Department, extend `GetEffectiveWorkflowDefinitionAsync` to accept optional DepartmentId and OrderType, and call it with the order’s values; define resolution priority (e.g. partner > department > order type > general) and document it. **Order Type, Order Category, and Installation Method are still NOT part of workflow resolution.**
- ~~**Doc cleanup:** Update `21_workflow_order_lifecycle.md` to remove or clarify “InProgress” so it matches WORKFLOW_STATUS_REFERENCE and the DB seed.~~ **DONE:** [21_workflow_order_lifecycle.md](architecture/21_workflow_order_lifecycle.md) and [order_lifecycle_and_statuses.md](business/order_lifecycle_and_statuses.md) updated; InProgress is not an order status; status codes (e.g. Rejected) and diagrams match reference and 07_gpon_order_workflow.sql.

---

---

## 10. Partner resolution fix (2025-03-08)

Implementation summary for the partner workflow resolution consistency fix.

### 10.1 Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Workflow/DTOs/WorkflowJobDto.cs` | Added optional `PartnerId` (Guid?) to `ExecuteTransitionDto`. |
| `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs` | Use `dto.PartnerId` or resolve from Order when EntityType is Order; pass partnerId to `GetEffectiveWorkflowDefinitionAsync` in ExecuteTransitionAsync, GetAllowedTransitionsAsync, CanTransitionAsync. Added `ResolvePartnerIdForEntityAsync`. |
| `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` | Set `executeDto.PartnerId = orderEntity.PartnerId` when building ExecuteTransitionDto. |
| `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs` | Set `PartnerId = order.PartnerId` on both ExecuteTransitionDto (Cancelled and Blocker). |
| `backend/src/CephasOps.Application/Billing/Services/InvoiceSubmissionService.cs` | Set `PartnerId = order.PartnerId` on ExecuteTransitionDto. |
| `backend/src/CephasOps.Application/Scheduler/Services/SchedulerService.cs` | Set `PartnerId = order.PartnerId` on all five ExecuteTransitionDto usages. |
| `backend/tests/CephasOps.Application.Tests/Workflow/WorkflowEngineServiceTests.cs` | Mock `GetEffectiveWorkflowDefinitionAsync` with `It.IsAny<Guid?>()` for partner parameter. |
| `docs/WORKFLOW_AUDIT_SUMMARY.md` | Updated overview, §3, §4, §8, §9; added §10. |
| `docs/01_system/WORKFLOW_ENGINE.md` | Added §2.5.9 (order workflow resolution at runtime). |
| `docs/01_system/WORKFLOW_ENGINE_FLOW.md` | Added runtime behaviour note before Workflow Activation Resolution. |
| `docs/02_modules/workflow/WORKFLOW_ACTIVATION_RULES.md` | Updated Overview and §2.3 (transition resolution). |

### 10.2 Exact inconsistency fixed

- **Before:** OrderService resolved workflow with `GetEffectiveWorkflowDefinitionAsync(companyId, "Order", orderEntity.PartnerId)` and threw if null, but then called `ExecuteTransitionAsync` with a DTO that had no partnerId. The engine called `GetEffectiveWorkflowDefinitionAsync(companyId, entityType, null)`, so **partnerId was lost** and only the general (PartnerId = null) workflow was ever used for execution and for allowed-transitions.
- **After:** The engine receives partnerId (via `ExecuteTransitionDto.PartnerId` when set by callers, or by resolving from the order when EntityType is Order and PartnerId is null). It passes that partnerId to `GetEffectiveWorkflowDefinitionAsync`. The same resolution is used in GetAllowedTransitionsAsync and CanTransitionAsync (partnerId resolved from order for Order entity). So the workflow that is checked is the workflow that is executed, and the UI (allowed transitions) matches execution.

### 10.3 Final runtime resolution order

1. **Partner-specific workflow** — EntityType + CompanyId + PartnerId + IsActive. Used when partnerId is set (or resolved from order for Order).
2. **General workflow** — EntityType + CompanyId + PartnerId = null + IsActive. Used when no partner-specific workflow exists or partnerId is null.

(Department and Order Type are **not** used in resolution.)

### 10.4 API / service signatures changed

- **ExecuteTransitionDto:** New optional property `PartnerId` (Guid?). Existing API clients that do not send `partnerId` remain valid; for Order, the engine resolves it from the order.
- **IWorkflowEngineService / WorkflowEngineService:** No signature change. `ExecuteTransitionAsync(Guid companyId, ExecuteTransitionDto dto, Guid? initiatedByUserId, CancellationToken cancellationToken)` unchanged; the DTO is extended.
- **WorkflowController:** No change; POST body may include optional `partnerId`.
- **GetEffectiveWorkflowDefinitionAsync:** No change (already had `Guid? partnerId = null`).

### 10.5 Regression risk

- **Low.** Fallback to general workflow when no partner-specific workflow exists is unchanged (WorkflowDefinitionsService logic unchanged). Existing seeded data (one Order workflow with PartnerId = null) continues to work. All 12 WorkflowEngineService tests pass. No UI or CreateOrderPage design changes.

### 10.6 No UI design pages redesigned

- **Confirmed.** No changes to `CreateOrderPage.tsx`, workflow definitions page, or any other frontend workflow/settings page. Only backend and docs were modified.

---

**End of audit.**
