# SI-App Workflow Hardening — Implementation Report

**Date:** 2026-03-12  
**Scope:** Harden the Service Installer (SI) order workflow so that status progression, exception handling, and completion are stricter and harder to misuse. No schema changes, no migrations, no SI-App redesign. Defense-in-depth over existing WorkflowTransitions (DB).

---

## 1. What SI-App workflow safeguard was added

**SiWorkflowGuard** (`CephasOps.Application/Workflow/SiWorkflowGuard.cs`)

A small static guard that enforces the **canonical Order status transition set** in code:

- **RequireValidOrderTransition(currentStatus, targetStatus, operationName)**  
  Validates that the (current, target) pair is in the allowed set. If not, throws `InvalidOperationException` with a clear message listing allowed next statuses from the current status. Used for entity type `"Order"` only.

- **IsAllowedOrderTransition(currentStatus, targetStatus)**  
  Returns true/false for tests and diagnostics.

The allowed set matches the seeded transitions in `07_gpon_order_workflow.sql`:

- **Normal SI flow:** Assigned → OnTheWay → MetCustomer → OrderCompleted (then docket/billing path to Completed).
- **Exception flows:** Assigned/OnTheWay → Blocker; Assigned → ReschedulePendingApproval; Blocker → MetCustomer, Assigned, ReschedulePendingApproval, Cancelled; ReschedulePendingApproval → Assigned, Cancelled; etc.
- **No invalid jumps:** e.g. Assigned → Completed, Assigned → OrderCompleted, Assigned → MetCustomer, OnTheWay → Completed, OnTheWay → OrderCompleted are **not** in the set and are rejected.

**Reschedule reason requirement** (in `OrderService.ChangeOrderStatusAsync`):

- When target status is **ReschedulePendingApproval**, the request **must** include a non-empty **Reason** (e.g. customer request, building issue). Otherwise an `InvalidOperationException` is thrown with a message asking for a reason for auditability. This uses the existing `ChangeOrderStatusDto.Reason` field; no schema change.

**Duplicate / no-op transition rejection** (in `WorkflowEngineService.ExecuteTransitionAsync`):

- When `dto.EntityType` is `"Order"` and the order’s current status equals the requested target status, the engine throws `InvalidOperationException` with a message that the order is already in the requested status and duplicate or no-op transition is not allowed. No workflow job is created and no side effects run.

---

## 2. Which services / paths were hardened

| Location | Change |
|----------|--------|
| **WorkflowEngineService.ExecuteTransitionAsync** | After resolving current entity status: (1) when `dto.EntityType` is `"Order"` and `currentStatus == dto.TargetStatus`, throws immediately (no job, no side effects); (2) when `dto.EntityType` is `"Order"`, calls `SiWorkflowGuard.RequireValidOrderTransition(currentStatus, dto.TargetStatus, "Order workflow")` before looking up the transition in the DB. So both **OrdersController → OrderService → WorkflowEngine** and **WorkflowController → WorkflowEngine** paths are protected. |
| **OrderService.ChangeOrderStatusAsync** | After Blocker validation block, when `dto.Status == ReschedulePendingApproval`, requires `!string.IsNullOrWhiteSpace(dto.Reason)`; otherwise throws with a clear message. |

No other services or controllers were modified. Material/device and stock flows were audited: **CreateStockMovementSideEffectExecutor** and **StockLedgerService** already tie movements to orders (OrderId, OrderMaterialUsage, ValidateOrderDepartmentAsync). No additional material/device guard was added; existing flows are order-linked.

---

## 3. What exact invalid transitions or unsafe actions are now prevented

**Invalid transitions (rejected by SiWorkflowGuard):**

- Assigned → Completed  
- Assigned → OrderCompleted  
- Assigned → MetCustomer  
- OnTheWay → Completed  
- OnTheWay → OrderCompleted  
- Any (from, to) pair not in the allowed set (e.g. Completed → OnTheWay, or a custom transition that a company might add in WorkflowTransitions but that is not in the canonical list).

**Completion without prior “Met Customer” equivalent:**

- OrderCompleted is only allowed from **MetCustomer** (and optionally from states that are already past MetCustomer if such transitions were ever added). Assigned or OnTheWay → OrderCompleted is explicitly disallowed by the guard.
- Completed is only allowed from **SubmittedToPortal** in the canonical set. So “complete” cannot be reached from Assigned or OnTheWay in one step.

**Exception flow (reschedule):**

- Transition to **ReschedulePendingApproval** without a **Reason** is rejected in OrderService. Reschedule actions must carry an explicit reason for operational accountability.

**Duplicate / no-op transitions:**

- Requesting a transition to the order’s current status (e.g. Assigned → Assigned) is rejected in the engine before any job is created or side effects run. This avoids double side effects and clarifies that same-status requests are invalid.

**Already enforced elsewhere (unchanged):**

- Blocker still requires reason and category validation via **BlockerValidationService** (existing).
- Workflow engine still requires a matching row in WorkflowTransitions for the transition to execute; the guard adds a second layer so that even if the DB is modified to allow an invalid jump, the application rejects it.

---

## 4. What tests were added or updated

**New: SiWorkflowGuardTests** (`CephasOps.Application.Tests/Workflow/SiWorkflowGuardTests.cs`)

- **RequireValidOrderTransition_AllowedTransitions_DoesNotThrow** (Theory): Assigned→OnTheWay, OnTheWay→MetCustomer, MetCustomer→OrderCompleted, Pending→Assigned, Assigned→Blocker, Assigned→ReschedulePendingApproval, Blocker→Assigned, ReschedulePendingApproval→Assigned, OrderCompleted→DocketsReceived, SubmittedToPortal→Completed. All pass without throw; `IsAllowedOrderTransition` returns true.
- **RequireValidOrderTransition_InvalidJumps_Throws** (Theory): Assigned→Completed, Assigned→OrderCompleted, Assigned→MetCustomer, OnTheWay→Completed, OnTheWay→OrderCompleted. All throw `InvalidOperationException` with message containing current and target status; `IsAllowedOrderTransition` returns false.
- **RequireValidOrderTransition_AssignedToCompleted_ThrowsWithAllowedList**: Asserts exception message contains “Allowed next statuses”.
- **RequireValidOrderTransition_CompleteWithoutMetCustomer_Throws**: Assigned→OrderCompleted and OnTheWay→OrderCompleted throw.
- **RequireValidOrderTransition_EmptyCurrent_Throws** / **RequireValidOrderTransition_EmptyTarget_Throws**: Empty current or target status throws with appropriate message.
- **RequireValidOrderTransition_SameStatus_Throws**: Assigned → Assigned throws; `IsAllowedOrderTransition` returns false for same-status.

**OrderWorkflowTransitionValidationTests** (workflow engine):

- **SameStatus_DuplicateTransition_AssignedToAssigned_Throws**: Engine rejects Order transition when current status equals target status; throws `InvalidOperationException` with "already in the requested status" and "Duplicate or no-op"; order status unchanged.
- **InvalidTransition_Assigned_To_OrderCompleted_ThrowsWithAllowedNext** / **Bypass_DirectExecuteTransition_InvalidJump_StillValidated**: Updated to expect `InvalidOperationException` from SiWorkflowGuard (invalid jump rejected before DB transition lookup).

No integration test was added for the Reschedule reason requirement (the rule is a single check in OrderService; guard tests cover the transition matrix).

---

## 5. Assumptions or unresolved edge cases

- **Entity type:** The guard runs only when `dto.EntityType` is `"Order"` (case-insensitive). Other entity types are unchanged.
- **WorkflowTransitions table:** The allowed set in code is aligned with `07_gpon_order_workflow.sql`. If new transitions are added only in the DB and not in the guard, they will be rejected. To support new transitions, both the seed (or migration) and `SiWorkflowGuard.AllowedOrderTransitions` must be updated.
- **Reschedule reason:** Enforced in both **OrderService** (when using `ChangeOrderStatusDto`) and **WorkflowEngineService** (when transitioning to ReschedulePendingApproval via any path, including direct `POST /api/workflow/execute`). See §7 below.
- **Material/device/replacement:** Existing code already links stock movements and ledger operations to orders. No new validation was added for “material without order” because the current flows are order-scoped. If new endpoints or side effects are added that perform consumption without an order, they should be validated separately.
- **Assurance/rework:** No dedicated assurance or rework status was added; existing statuses and transitions (and the guard’s allowed set) remain the source of truth.

---

## 6. Why this is safe and does not change valid business behavior

- **Allowed set matches seed:** The guard’s allowed (From, To) pairs match the transitions defined in `07_gpon_order_workflow.sql`. Valid flows (Assigned→OnTheWay→MetCustomer→OrderCompleted, Blocker, Reschedule, docket path, billing path, etc.) continue to work as before.
- **Read-only guard:** The guard only validates; it does not change data or call external services. Failure results in an exception and no status update.
- **No schema or migration:** No new tables, columns, or migrations. Uses existing `OrderStatus` constants and `ChangeOrderStatusDto.Reason`.
- **Reschedule reason:** The requirement is additive: previously a reason could be omitted; now it is required when setting ReschedulePendingApproval. Callers that already send a reason are unaffected.
- **Single place for Order transitions:** Both the OrdersController path (OrderService) and the WorkflowController path (WorkflowEngineService) go through the same engine; the guard is applied once in the engine for Order entity type, so behavior is consistent.
- **No weakening of existing safeguards:** Tenant, financial, and EventStore guards are untouched. Blocker validation remains in place. Workflow guard conditions and side effects still run after the transition is allowed.

---

---

## 7. Operational hardening (2026-03-13)

**Reschedule reason in workflow engine**

- **SiWorkflowGuard.RequireRescheduleReason(reason, operationName)**  
  Throws if `reason` is null or whitespace. Used when the transition target is **ReschedulePendingApproval** so that direct calls to the workflow engine (e.g. `POST /api/workflow/execute`) also require a reason in the payload (`payload["reason"]`). OrderService already required reason via `ChangeOrderStatusDto.Reason`; the guard ensures consistency when the transition is invoked without going through OrderService.

- **WorkflowEngineService**  
  After `RequireValidOrderTransition`, when `dto.EntityType` is Order and `dto.TargetStatus` is ReschedulePendingApproval, calls `SiWorkflowGuard.RequireRescheduleReason(reason, "Order workflow")` with `reason` from `dto.Payload?.GetValueOrDefault("reason")?.ToString()`.

**Operational audit trail (OrderStatusLog)**

- **EnrichPayloadForSideEffectsAsync**  
  Before executing side effects, the engine builds an enriched payload so that **CreateOrderStatusLogSideEffectExecutor** always receives initiator and installer when available: `userId` from `initiatedByUserId` (API/user context) and, for Order, `siId` from the order’s `AssignedSiId`. Client payload is preserved; missing keys are filled in. This ensures OrderStatusLog rows have TriggeredByUserId and TriggeredBySiId for audit and dispute resolution without requiring every client to pass them.

**Tests added**

- **SiWorkflowGuardTests:** RequireRescheduleReason_WhenReasonProvided_DoesNotThrow, RequireRescheduleReason_WhenReasonNull_Throws, RequireRescheduleReason_WhenReasonWhitespace_Throws.
- **OrderWorkflowTransitionValidationTests:** ReschedulePendingApproval_WithoutReasonInPayload_Throws, ReschedulePendingApproval_WithReasonInPayload_Succeeds.

---

**Summary:** A static **SiWorkflowGuard** enforces the canonical Order transition set in code (defense-in-depth over WorkflowTransitions). **WorkflowEngineService** calls the guard for Order before executing any transition and requires a reschedule reason when target is ReschedulePendingApproval (payload or OrderService). **OrderService** also requires a non-empty **Reason** when transitioning to ReschedulePendingApproval. Invalid jumps (e.g. Assigned→Completed, completion without MetCustomer) are rejected with clear messages. Payload enrichment ensures OrderStatusLog receives userId and siId for a complete audit trail. Unit and workflow tests cover allowed transitions, invalid jumps, completion prerequisites, reschedule reason, and duplicate transition rejection. Material/device flows remain order-linked by existing design; no schema or product redesign was introduced.

---

## Related

- **Index of safeguards:** [PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md) — discoverable list of all platform guards and reports.
- **When a guard fails:** [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) — operator guidance for workflow and other safeguard failures.
