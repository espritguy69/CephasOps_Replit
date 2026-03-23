# Order Lifecycle & Status – Summary

**Related:** [Order lifecycle and statuses (canonical)](order_lifecycle_and_statuses.md) | [Status semantics map](status_semantics_map.md) | [Process flows](process_flows.md) | [Docket process](docket_process.md) | [Billing & MyInvois](billing_myinvois_flow.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

---

## 1. Authoritative document

**Full status definitions, transition rules, blocker reasons, KPI responsibility matrix, checklist gating, docket/invoice loops, and override rules:**  
**[docs/business/order_lifecycle_and_statuses.md](order_lifecycle_and_statuses.md)**

This document is the single canonical order lifecycle spec for the GPON department. CWO/NWO will use department-specific workflows when activated.

---

## 2. Status flow (short)

Pending → Assigned → OnTheWay → MetCustomer → (Blocker | ReschedulePendingApproval) → OrderCompleted → DocketsReceived → DocketsVerified → DocketsUploaded → ReadyForInvoice → Invoiced → SubmittedToPortal → (Rejected → ReadyForInvoice | Reinvoice → Invoiced)* → Completed.  
Side: Cancelled. *(Code status: `Rejected`, display "Invoice Rejected".)*

---

## 3. Key principles (from lifecycle spec)

- Every order is fully traceable; changes stored in status history.
- Service ID (TBBN or partner format) is the universal key.
- TIME approval required for reschedules except same-day; same-day needs customer evidence.
- Splitter details must be complete before docket upload; no splitter = no docket = no invoice.
- SI app is source of truth for fieldwork (GPS, ONU scan, port, photos, signature).
- Only HOD/SuperAdmin/Director can override protections; overrides require reason, remark, evidence.

---

## 4. Legacy references

The following legacy docs are **reference only**; the canonical spec is [order_lifecycle_and_statuses.md](order_lifecycle_and_statuses.md).

- [01_system/ORDER_LIFECYCLE.md](../01_system/ORDER_LIFECYCLE.md) – Legacy full lifecycle spec (content migrated into canonical doc).
- [05_data_model/WORKFLOW_STATUS_REFERENCE.md](../05_data_model/WORKFLOW_STATUS_REFERENCE.md) – Legacy workflow status reference (naming, RMA/KPI statuses).
