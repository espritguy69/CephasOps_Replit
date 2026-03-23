# Docket Process

**Related:** [Process flows](process_flows.md) | [Order lifecycle summary](order_lifecycle_summary.md) | [Billing & MyInvois](billing_myinvois_flow.md) | [01_system/ORDER_LIFECYCLE](../01_system/ORDER_LIFECYCLE.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

**Implementation:** DocketsPage at `/operations/dockets`. DocketsRejected status; transitions DocketsReceived ↔ DocketsRejected; Reject modal with required reason; Accept corrected button. Workflow seeded in 07_gpon_order_workflow.sql.

---

## 1. Flow

**Receive** – Admin receives docket (paper, WhatsApp, or email) from SI or customer.

**Verify** – Admin checks docket for correctness.  
- If errors **before** upload: DocketsReceived → **DocketsRejected**.  
- Common issues: wrong splitter, wrong ONU, missing photos, wrong job category, wrong customer details, incorrect SI data, docket from different job.  
- KPI: DocketsRejected → SI KPI (incorrect job data); verification accuracy → Admin KPI.

**Reject** – Admin rejects docket; SI must correct or resend.  
- Re-entry: DocketsRejected → DocketsReceived when admin accepts corrected docket.

**Upload** – When correct, admin uploads docket to partner portal (e.g. TIME).  
- Mandatory: Docket number; Splitter ID + Port; ONU serial; completion photos.  
- Status: DocketsUploaded.  
- KPI: Admin KPI.

---

## 2. Rules (from order lifecycle)

- Splitter details must be complete before docket upload; no splitter = no docket upload = no invoice.
- Docket can be rejected before upload (DocketsReceived → DocketsRejected).
- TIME portal is reference only (no API); admin mirrors upload and status with evidence.
