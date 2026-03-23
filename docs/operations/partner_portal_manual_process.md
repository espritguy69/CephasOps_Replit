# Partner Portal Manual Process (e.g. TIME)

**Related:** [Billing & MyInvois](../business/billing_myinvois_flow.md) | [Integrations overview](../integrations/overview.md) | [Order lifecycle](../business/order_lifecycle_and_statuses.md)

**Source of truth:** Codebase Summary; integrations/overview.md §6.

---

## 1. Purpose

CephasOps has **no API integration** with partner portals (e.g. TIME). Docket upload and invoice submission to the partner are **manual steps** performed by ops at the partner's web portal. CephasOps tracks readiness and (for invoice) MyInvois submission; the partner portal is used for partner-specific docket/invoice workflows.

---

## 2. Docket Upload to Partner Portal

| Step | Action | CephasOps | Partner Portal |
|------|--------|-----------|----------------|
| 1 | SI completes job; admin receives docket | Order: OrderCompleted → DocketsReceived | — |
| 2 | Admin verifies docket (splitter, port, ONU, photos) | DocketsReceived → DocketsVerified | — |
| 3 | Admin uploads docket to partner | DocketsVerified → DocketsUploaded | Log in → Upload docket file |
| 4 | Partner accepts docket | — | Portal shows accepted |

**CephasOps UI:** `/operations/dockets` — filter, checklist, verify, mark uploaded, attach file.

**Partner portal:** Ops logs in to TIME (or other partner) portal; navigates to docket upload section; uploads the docket PDF/image; submits.

---

## 3. Invoice Submission to Partner Portal

| Step | Action | CephasOps | Partner Portal |
|------|--------|-----------|----------------|
| 1 | Order ready for invoice | DocketsUploaded → ReadyForInvoice | — |
| 2 | Admin creates invoice | ReadyForInvoice → Invoiced | — |
| 3 | Admin submits to MyInvois | Invoice submission stored | — |
| 4 | **Manual:** Admin submits invoice to partner | — | Log in → Upload/submit invoice |
| 5 | Partner processes; payment | SubmittedToPortal → Completed (when matched) | Portal shows status |

**MyInvois:** CephasOps submits e-invoice to LHDN MyInvois (automated). See [myinvois_production_runbook.md](myinvois_production_runbook.md).

**Partner portal (TIME):** Ops logs in; navigates to invoice submission; uploads or links the invoice; submits. CephasOps status **SubmittedToPortal** indicates invoice has been submitted to the partner (admin updates when done).

---

## 4. Rejection Handling (Partner Rejects Invoice)

| Scenario | CephasOps | Partner Portal |
|----------|-----------|----------------|
| Partner rejects | Order → Rejected (Invoice Rejected) | Portal shows rejection reason |
| Path 1: Full regenerate | Rejected → ReadyForInvoice → regenerate → Invoiced | — |
| Path 2: Simple correction | Rejected → Reinvoice | Ops corrects in portal |
| After correction | Reinvoice → Invoiced (admin marks) | Ops resubmits in portal |

---

## 5. Evidence and Audit

- **CephasOps:** Order status log, InvoiceSubmissionHistory (MyInvois), audit log.
- **Partner portal:** Screenshots, export reports, or manual notes for evidence of docket/invoice submission when disputes arise.
- **Recommendation:** Document partner portal URL, login method, and key navigation paths in internal ops runbook.

---

## 6. Out of Scope

- Partner API: No automated docket or invoice submission to partner.
- Partner status sync: No automatic sync of partner portal status into CephasOps; admin updates manually.

---

**Last updated:** 2026-02-09
