# Billing and Invoicing (GPON – Canonical)

**Related:** [Order lifecycle and statuses](../business/order_lifecycle_and_statuses.md) | [Billing & MyInvois flow (overview)](../business/billing_myinvois_flow.md) | [Docket process](../business/docket_process.md) | [Process flows](../business/process_flows.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

This document is the **single authoritative** billing and invoicing specification for GPON. It covers partner billing and e-invoicing only; it is not the general ledger (GL) or payment gateway specification.

---

## 1. Purpose and scope

- **Purpose:** Define how invoices are created from ready orders, submitted to MyInvois (e-invoice), and tracked through rejection/resubmission and payment matching until the order is completed.
- **Scope:** **Partner billing only** (invoicing telco partners e.g. TIME, CelcomDigi, U Mobile for completed jobs). Single-company, department-scoped where applicable.
- **Out of scope (explicit):** General ledger (GL); double-entry accounting; payment gateway (Stripe/PayPal etc.); partner APIs for invoice submission or status. Payment is recorded and matched **internally**; external accounting/GL remains outside this module.

---

## 2. Billing prerequisites

- **Order status:** Order must have reached **DocketsUploaded** (docket uploaded to partner portal).
- **RMA (Assurance orders):** If serialised materials were replaced, **all RMA entries must have TIME approval** (approvedBy + approvalNotes). Missing approval **blocks** transition to ReadyForInvoice. If only non-serialised replacements, at least one replacement row (material type + quantity) must exist. RMA fields become read-only after ReadyForInvoice.
- **Invoice readiness:** Admin has prepared invoice content (BOQ/BOW, customer details, rate card, materials). No splitter = no docket = no invoice (enforced at order lifecycle).

---

## 3. Invoice lifecycle

```
ReadyForInvoice
  → Invoiced
  → (InvoiceRejected | Completed)
  → If InvoiceRejected:
      Path 1: InvoiceRejected → ReadyForInvoice → (regenerate) → Invoiced
      Path 2: InvoiceRejected → Reinvoice → (correct in TIME Portal) → Invoiced
  → Invoiced → Completed (when payment received and matched)
```

- **ReadyForInvoice:** All validations passed; invoice can be created and submitted.
- **Invoiced:** Invoice generated and submitted to MyInvois; submission stored; 45-day due date set.
- **InvoiceRejected:** Partner/TIME rejected the invoice; admin must correct and resubmit.
- **Reinvoice:** Interim state for simple correction path; admin corrects details in TIME Portal then resubmits (Reinvoice → Invoiced).
- **Completed:** Payment received and matched to order; order becomes locked. Terminal billing state for the order.

---

## 4. MyInvois submission flow and background status polling

- **Submission:** Admin generates invoice (header, line items from BOQ/rate card and materials), selects optional document template; system generates PDF and creates invoice record. Invoice is **submitted to MyInvois** (e-invoice); submission ID and metadata are stored.
- **Status polling:** A **background job** (MyInvoisStatusPoll) polls MyInvois for e-invoice status (e.g. accepted/rejected). Results update submission status in the system.
- **Rejection handling:** If TIME/partner rejects the invoice, admin is notified; admin corrects and resubmits via either full regeneration or simple correction path (see below).

---

## 5. Full regenerate vs simple correction

- **Full regenerate (InvoiceRejected → ReadyForInvoice → Invoiced):** Admin corrects invoice content (e.g. wrong BOQ, rate, job category, docket details, documents, splitter/ONU). Invoice is regenerated from order data and document template; new PDF and submission; transition ReadyForInvoice → Invoiced.
- **Simple correction (InvoiceRejected → Reinvoice → Invoiced):** Admin corrects details **inside TIME Portal** (or partner portal) without full regeneration in CephasOps. After correction, admin marks reinvoice as resubmitted; transition Reinvoice → Invoiced (new submissionId stored as applicable).
- **Rejection reasons (from lifecycle):** Wrong BOQ, wrong rate, wrong job category, missing docket details, missing documents, duplicate submission, incorrect splitter/ONU in invoice, incorrect submissionId.

---

## 6. Who can create, edit, submit, regenerate, and resubmit

- **Create invoice (ReadyForInvoice → Invoiced):** **Ops (Admin/Billing).** User with access to billing and the order’s department.
- **Edit invoice:** **Ops (Admin/Billing)** before or after submission as allowed by workflow (e.g. before Completed).
- **Submit to MyInvois:** **Ops (Admin/Billing)** – triggers submission and stores submission ID.
- **Regenerate (full):** **Ops (Admin/Billing)** – after InvoiceRejected; produces new invoice and submission.
- **Resubmit (simple correction):** **Ops (Admin/Billing)** – after correcting in TIME Portal; updates status to Invoiced with new submissionId as applicable.
- **Payment recording and matching:** **Finance** (or Admin with finance role). When payment is received and matched, order moves to Completed.
- **Override / exception:** Only **HOD, SuperAdmin, Director** can override billing-related validations where applicable; override requires reason, remark, evidence.

---

## 7. Payment recording and matching rules

- **Payment** is recorded against the invoice/order in the system.
- When payment is **received and matched** to the order, the order transitions to **Completed**.
- Order becomes **locked** at Completed. No further invoice or status changes for that order in normal flow.
- Matching is **internal** (no payment gateway); external bank/accounting reconciliation is out of scope of this spec.

---

## 8. Terminal billing states

- **Completed:** Payment received and matched; order locked. **Terminal** for the order’s billing lifecycle.
- **Cancelled (order):** Order cancelled (terminal at order level); no invoice or payment for that order (handled in order lifecycle).

---

## 9. Audit and compliance requirements

- **Audit trail:** Invoice creation, submission, rejection, regeneration, resubmission, and payment matching must be traceable (who, when, from/to status, remark/evidence where applicable). Align with order status history and audit log.
- **MyInvois compliance:** E-invoice submission and status follow LHDN/MyInvois requirements; submission ID and status are stored for audit.
- **Document retention:** Invoice PDF and submission metadata retained per policy; document templates (e.g. Handlebars) used for layout are versioned in Settings.

---

## 10. Explicit out-of-scope items

- **General ledger (GL):** No double-entry bookkeeping in CephasOps; GL remains in external accounting system.
- **Payment gateway:** No Stripe, PayPal, or other card/online payment processing; payment = internal recording and matching.
- **Partner APIs:** No API integration with TIME or other partners for invoice submission or status; submission and status updates are manual (MyInvois) or via background poll of MyInvois.
- **Statutory tax returns:** SST returns, annual filings – handled outside this module.
- **Credit notes / debit notes:** If supported in code, treat as extension; not elaborated in this canonical doc unless present in source-of-truth reports.
