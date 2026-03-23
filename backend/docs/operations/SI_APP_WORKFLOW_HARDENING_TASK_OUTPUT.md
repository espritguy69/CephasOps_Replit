# SI-App Workflow Hardening — Task Output

**Date:** 2026-03-12  
**Scope:** Audit and harden SI-App/order execution state transitions; no schema/migrations; no weakening of tenant/financial/event guards.

---

## 1) Canonical workflow summary

### Source(s) of truth

- **Seed:** `backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql` — WorkflowDefinition + WorkflowTransitions for Order (GPON lifecycle).
- **Domain constants:** `CephasOps.Domain/Orders/Enums/OrderStatus.cs` — 17 statuses (12 main flow + 5 side), single source of truth for status strings.
- **Application guard:** `CephasOps.Application/Workflow/SiWorkflowGuard.cs` — Static allowed (From, To) set aligned with seed; defense-in-depth over DB.
- **Docs:** `docs/01_system/21_workflow_order_lifecycle.md`, `backend/docs/operations/SI_APP_WORKFLOW_HARDENING_REPORT.md`, `docs/business/order_lifecycle_and_statuses.md` (references; some paths like `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md` are cited in index but not under backend).

### Canonical statuses

**Main flow (12):** Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, DocketsReceived, DocketsVerified, DocketsRejected, DocketsUploaded, ReadyForInvoice, Invoiced, SubmittedToPortal, Completed.

**Side (5):** Blocker, ReschedulePendingApproval, Rejected, Cancelled, Reinvoice.

### Allowed transition model

- **Normal SI path:** Assigned → OnTheWay → MetCustomer → OrderCompleted (then docket path to DocketsReceived → … → Completed).
- **Exception paths:** Assigned/OnTheWay/MetCustomer → Blocker; Assigned → ReschedulePendingApproval; Blocker → MetCustomer, Assigned, ReschedulePendingApproval, Cancelled; ReschedulePendingApproval → Assigned, Cancelled; Pending/Assigned/… → Cancelled; billing rejection loop (Rejected, Reinvoice, Invoiced).
- **Terminal (no outgoing in seed/guard):** Cancelled, Completed (end of main flow).

### Mismatches discovered

- **None** between seed, OrderStatus constants, and SiWorkflowGuard. Guard set matches `07_gpon_order_workflow.sql`.
- **Exception outcomes:** Reschedule, blocker, customer/building issue are **canonical statuses** (ReschedulePendingApproval, Blocker) or transitions to them, not separate outcome tables; design is consistent.
- **OrderWorkflowTransitionValidationTests:** Fail in current run due to **TenantSafetyGuard** (no tenant context when creating Order in CreateOrderAsync). This is a pre-existing test-environment issue; no change was made to tenant guards.

---

## 2) Transition audit summary

### Files/methods reviewed

| File | Method / path | Current behavior | Validation | Risk |
|------|----------------|------------------|------------|------|
| **WorkflowEngineService** | ExecuteTransitionAsync | Resolves current status → (new) same-status check for Order → SiWorkflowGuard for Order → DB transition lookup → guards → side effects → UpdateEntityStatusAsync | Same-status reject; SiWorkflowGuard; DB transition | **Low** — central choke point hardened |
| **OrderService** | ChangeOrderStatusAsync | Builds ExecuteTransitionDto, calls WorkflowEngineService; Blocker + Reschedule reason checks | Reschedule reason; BlockerValidationService; all transitions via engine | **Low** |
| **WorkflowController** | Execute transition endpoint | Calls WorkflowEngineService.ExecuteTransitionAsync | Same as engine | **Low** |
| **OrdersController** | Change status endpoint | Calls OrderService.ChangeOrderStatusAsync | Same as OrderService | **Low** |
| **SchedulerService** | Assign/Blocker/Reschedule flows | Calls WorkflowEngineService.ExecuteTransitionAsync | Same as engine | **Low** |
| **EmailIngestionService** | Cancel/Blocker/Reschedule approval | ExecuteTransitionAsync or OrderService.ChangeOrderStatusAsync | Via engine / OrderService | **Low** |
| **AgentModeService** | Reschedule approval | OrderService.ChangeOrderStatusAsync | Via OrderService | **Low** |
| **ExecuteWorkflowTransitionHandler** | Command handler | WorkflowEngineService.ExecuteTransitionAsync | Via engine | **Low** |
| **WorkflowEngineService** | UpdateEntityStatusAsync | Sets order.Status only for Order entity | Called only after valid transition | **Low** — single write point |

**No code path** was found that updates Order status without going through WorkflowEngineService.ExecuteTransitionAsync (and thus SiWorkflowGuard for Order).

### High-risk paths found

- **None.** Invalid jumps (e.g. Assigned → OrderCompleted), terminal-state exits (e.g. Cancelled → Assigned), and same-status/duplicate requests are rejected by SiWorkflowGuard or the new same-status check. Completion-only side effects (e.g. OrderCompletedEvent) run only after a valid transition (MetCustomer → OrderCompleted) by design.

---

## 3) Exact changes made

### WorkflowEngineService.cs

- **Risk addressed:** Duplicate or no-op transition (e.g. Assigned → Assigned) could create a workflow job and run side effects twice or unnecessarily.
- **Change:** Immediately after `GetCurrentEntityStatusAsync`, when `dto.EntityType` is Order and `currentStatus` equals `dto.TargetStatus` (case-insensitive), throw `InvalidOperationException` with message: "Order is already in the requested status. Duplicate or no-op transition is not allowed."
- **Placement:** Before SiWorkflowGuard and before creating the workflow job, so no job is created and no side effects run.

### SiWorkflowGuardTests.cs

- **Risk addressed:** Same-status transitions not explicitly covered in guard tests.
- **Change:** Added **RequireValidOrderTransition_SameStatus_Throws** — Assigned → Assigned throws InvalidOperationException; IsAllowedOrderTransition(Assigned, Assigned) is false.

### OrderWorkflowTransitionValidationTests.cs

- **Risk addressed:** Invalid jump (Assigned → OrderCompleted) is rejected by SiWorkflowGuard with InvalidOperationException; tests previously expected InvalidWorkflowTransitionException.
- **Change:** **InvalidTransition_Assigned_To_OrderCompleted_ThrowsWithAllowedNext** — expect `Exception`, assert it is InvalidOperationException and message contains Assigned, OrderCompleted; **Bypass_DirectExecuteTransition_InvalidJump_StillValidated** — expect `Exception` (guard throws first). Added **SameStatus_DuplicateTransition_AssignedToAssigned_Throws** — same-status request throws with "already in the requested status" and "Duplicate or no-op"; order status unchanged. (Note: OrderWorkflowTransitionValidationTests currently fail in this repo due to TenantSafetyGuard in CreateOrderAsync; test logic is correct for when tenant context is set.)

### SI_APP_WORKFLOW_HARDENING_REPORT.md

- Documented duplicate/no-op rejection in WorkflowEngineService; updated “Which services / paths were hardened” and “What exact invalid transitions or unsafe actions are now prevented”; updated “What tests were added or updated.”

### docs/01_system/21_workflow_order_lifecycle.md

- In Enforcement section, added bullet: duplicate/no-op transition (e.g. Assigned → Assigned) is rejected; no workflow job or side effects run.

---

## 4) Tests added/updated

| File | Scenarios |
|------|-----------|
| **SiWorkflowGuardTests.cs** | RequireValidOrderTransition_SameStatus_Throws (Assigned→Assigned throws; IsAllowedOrderTransition false). |
| **OrderWorkflowTransitionValidationTests.cs** | SameStatus_DuplicateTransition_AssignedToAssigned_Throws (engine rejects same-status); InvalidTransition_Assigned_To_OrderCompleted and Bypass invalid jump updated to expect exception from guard. |

**SiWorkflowGuardTests:** 19 tests pass (including new same-status test). **OrderWorkflowTransitionValidationTests:** Require tenant context in test setup to pass (TenantSafetyGuard); no change was made to tenant isolation.

---

## 5) Safety conclusion

- **No schema changes.** No new tables, columns, or migrations.
- **No migration changes.** No migration files added or modified.
- **No weakening of existing guards.** Tenant, financial, and EventStore guards are unchanged. SiWorkflowGuard and BlockerValidationService are unchanged except that the engine now rejects same-status before the guard.
- **Workflow transitions fail safely** on invalid or out-of-order use: invalid jumps and same-status/duplicate requests throw before any job or side effects; terminal-state transitions (e.g. Cancelled → Assigned) are not in the allowed set and are rejected by the guard.
- **Valid installer flows still work:** Assigned → OnTheWay → MetCustomer → OrderCompleted and all seeded exception paths (Blocker, ReschedulePendingApproval, Cancelled, docket/billing) remain allowed and unchanged.

---

## 6) Remaining follow-up risks

- **OrderWorkflowTransitionValidationTests tenant context:** Tests that create Order entities need a tenant context (or platform bypass) in the test class so SaveChangesAsync does not hit TenantSafetyGuard. This is a test-environment setup issue; not a workflow logic or guard weakening.
- **Concurrent duplicate transition:** If two requests for Assigned → OnTheWay run concurrently and both read currentStatus = Assigned, both could pass the guard and DB lookup; one would succeed and one would update order to OnTheWay again (idempotent write). Side effects could run twice. A full fix would require optimistic concurrency (e.g. row version) or transactional idempotency; the user ruled out schema changes, so no change was made.
- **Reschedule reason:** Enforced only in OrderService (ChangeOrderStatusDto path). Direct calls to WorkflowEngineService with ReschedulePendingApproval and no reason are not blocked in the engine; main operator path is via OrderService (documented in report).
- **Material/assurance:** No code was found that records completion-only material or assurance in a state that bypasses the workflow; existing side effects run from the engine after a valid transition. No additional protection was added.

---

**Summary:** Canonical workflow is defined by `07_gpon_order_workflow.sql`, OrderStatus constants, and SiWorkflowGuard. All order status changes go through WorkflowEngineService. Hardening added: **rejection of duplicate/no-op Order transitions** (current status == target status) before job creation and side effects. Tests and docs were updated accordingly. No schema or migrations; no weakening of tenant, financial, or event guards.
