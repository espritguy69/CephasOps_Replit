# DB Workflow Baseline Spec – GPON Order

**Related:** [Order lifecycle and statuses (canonical)](../business/order_lifecycle_and_statuses.md) | [Workflow engine validation (GPON)](workflow_engine_validation_gpon.md) | [Discrepancies](../_discrepancies.md)

**Source of truth:** docs/business/order_lifecycle_and_statuses.md; docs/_source/Business_Processes_SourceOfTruth.md.

This document defines the **minimum GPON transition set** that must exist in the DB (WorkflowDefinition + WorkflowTransitions) to support the canonical order flow. Runtime authority is the DB; the fallback controller graph is incomplete and must not be relied on.

---

## 1. Scope and purpose

- **EntityType:** Order.
- **Purpose:** Ensure the effective Order workflow in the DB contains at least these transitions so that WorkflowEngineService can execute the full GPON lifecycle. AllowedRolesJson (and optional GuardConditionsJson, SideEffectsConfigJson) should be set per transition so that the right roles can trigger each step.
- **Convention:** `AllowedRolesJson` is stored per WorkflowTransition. Empty or null means “all roles”; otherwise the user’s role must be in the list to execute the transition. Role names should align with your RBAC (e.g. Admin, Ops, SI, Finance, HOD, SuperAdmin, Director).

---

## 2. Minimum required transitions (canonical flow)

The following table lists the **minimum** FromStatus → ToStatus pairs that must exist in the DB for the canonical GPON flow. Optional transitions (e.g. invoice rejection loop) are in §3.

| # | FromStatus | ToStatus | Purpose | AllowedRolesJson guidance |
|---|------------|----------|---------|---------------------------|
| 1 | Pending | Assigned | Ops assigns SI and appointment | Ops, Admin (and optionally HOD, SuperAdmin) |
| 2 | Pending | Cancelled | Cancel before assignment | Ops, Admin |
| 3 | Assigned | OnTheWay | SI or Ops marks on the way | SI, Ops, Admin |
| 4 | Assigned | Blocker | Pre-customer blocker (e.g. access denied) | SI, Ops, Admin |
| 5 | Assigned | ReschedulePendingApproval | Ops requests TIME approval | Ops, Admin |
| 6 | Assigned | Cancelled | Cancel after assignment | Ops, Admin |
| 7 | OnTheWay | MetCustomer | SI arrives at customer | SI, Ops, Admin |
| 8 | OnTheWay | Blocker | Blocker en route | SI, Ops, Admin |
| 9 | MetCustomer | OrderCompleted | SI completes job | SI, Ops, Admin |
| 10 | MetCustomer | Blocker | Post-customer blocker | SI, Ops, Admin |
| 11 | Blocker | Assigned | Ops re-assigns (reschedule/reassign) | Ops, Admin, HOD, SuperAdmin, Director |
| 12 | Blocker | MetCustomer | Ops resumes at customer (e.g. same-day fix) | Ops, Admin, HOD, SuperAdmin, Director |
| 13 | Blocker | ReschedulePendingApproval | Ops requests reschedule approval | Ops, Admin |
| 14 | Blocker | Cancelled | Cancel from blocker | Ops, Admin, HOD, SuperAdmin, Director |
| 15 | ReschedulePendingApproval | Assigned | TIME approval received; re-assign | Ops, Admin |
| 16 | ReschedulePendingApproval | Cancelled | Cancel pending reschedule | Ops, Admin |
| 17 | OrderCompleted | DocketsReceived | Ops receives docket | Ops, Admin |
| 18 | DocketsReceived | DocketsVerified | Admin QA; docket validated | Ops, Admin |
| 19 | DocketsVerified | DocketsUploaded | Admin uploads to partner portal | Ops, Admin |
| 20 | DocketsUploaded | ReadyForInvoice | Billing prepares invoice | Ops, Admin (Billing) |
| 21 | ReadyForInvoice | Invoiced | Invoice prepared | Ops, Admin (Billing) |
| 22 | Invoiced | SubmittedToPortal | Invoice submitted to partner portal (system may trigger after MyInvois) | Ops, Admin, System |
| 23 | SubmittedToPortal | Completed | Payment received and matched | Finance, Ops, Admin, System |
| 24 | Invoiced | Completed | Direct complete if SubmittedToPortal not used | Finance, Ops, Admin, System |

**Blocker exits (Ops-only for 11–12):** Blocker → Assigned and Blocker → MetCustomer are Ops-only (or HOD/SuperAdmin/Director) so that only authorised roles can move orders out of Blocker.

**Docket path:** DocketsReceived → DocketsVerified → DocketsUploaded. Optional rejection loop: DocketsReceived → DocketsRejected (admin rejects with reason); DocketsRejected → DocketsReceived (admin accepts corrected docket). Implemented in 07_gpon_order_workflow.sql.

**Billing path:** ReadyForInvoice → Invoiced → SubmittedToPortal → Completed (or Invoiced → Completed where applicable).

---

## 3. Optional transitions (product-dependent)

If production requires the **invoice rejection / reinvoice loop** at order status level, add these transitions to the same WorkflowDefinition:

| # | FromStatus | ToStatus | Purpose | AllowedRolesJson guidance |
|---|------------|----------|---------|---------------------------|
| O1 | Invoiced | InvoiceRejected | System/TIME rejection (or Ops when rejection is recorded) | System, Ops, Admin |
| O2 | SubmittedToPortal | InvoiceRejected | Rejection after submission | System, Ops, Admin |
| O3 | InvoiceRejected | ReadyForInvoice | Full regeneration path | Ops, Admin (Billing) |
| O4 | InvoiceRejected | Reinvoice | Simple correction path | Ops, Admin (Billing) |
| O5 | Reinvoice | Invoiced | After correction in partner portal | Ops, Admin (Billing) |

**Note:** Code OrderStatus enum uses `Rejected` (display name "Invoice Rejected"); invoice rejection loop is implemented. See [_discrepancies.md](../_discrepancies.md) for audit register.

---

## 4. Override-only transitions (optional in DB)

Canonical doc allows override for invalid transitions (e.g. Blocker → OrderCompleted, Blocker → DocketsReceived). If implemented via workflow, define these transitions with AllowedRolesJson restricted to HOD, SuperAdmin, Director and enforce override reason/remark/evidence in guards or application logic.

---

## 5. AllowedRolesJson format and behaviour

- **Storage:** Per WorkflowTransition, typically as JSON array of role names, e.g. `["Ops","Admin"]` or `["SI","Ops","Admin"]`.
- **Semantics:** If AllowedRolesJson is null or empty, all authenticated users may execute the transition (subject to any guard conditions). Otherwise, the current user’s role must be in the list.
- **Suggested roles (align with your RBAC):** Admin, Ops, SI, Finance, HOD, SuperAdmin, Director, System (for system-triggered transitions, e.g. Invoiced → SubmittedToPortal).
- **System-triggered:** For transitions invoked by the application (e.g. InvoiceSubmissionService setting SubmittedToPortal), ensure “System” or the service account role is allowed, or use a dedicated mechanism (e.g. bypass role check for system calls) as per your implementation.

---

## 6. Summary

- **Minimum set (§2):** 24 transitions covering Pending → Assigned → OnTheWay → MetCustomer, Blocker exits (Assigned, MetCustomer, ReschedulePendingApproval, Cancelled), docket path (DocketsReceived → DocketsVerified → DocketsUploaded), and billing path (ReadyForInvoice → Invoiced → SubmittedToPortal → Completed).
- **Optional (§3):** InvoiceRejected/Reinvoice loop (5 transitions) if product requires it at order status level.
- **Authority:** DB workflow is authoritative; fallback controller graph is incomplete. Seed or maintain these transitions via Workflow Definitions UI or migration/seed scripts so that WorkflowEngineService can execute the full canonical flow.
