# Billing & MyInvois E-Invoice Flow (Overview)

**Related:** [Order lifecycle and statuses](order_lifecycle_and_statuses.md) | [Process flows](process_flows.md) | [Docket process](docket_process.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

---

## Authoritative document

**Full billing and invoicing specification (prerequisites, lifecycle, MyInvois, regenerate vs reinvoice, who can perform actions, payment matching, audit, out-of-scope):**  
**[docs/modules/billing_and_invoicing.md](../modules/billing_and_invoicing.md)**

---

## Process overview

- **Prerequisites:** Order at DocketsUploaded; for Assurance with serialised replacements, RMA must have TIME approval before ReadyForInvoice.
- **Invoice creation:** Admin creates invoice from order(s) (header, line items from BOQ/rate card and materials); document templates (e.g. Handlebars) for layout; PDF generated.
- **MyInvois:** Invoice submitted to MyInvois (e-invoice); submission stored; **background job** polls for status (accepted/rejected).
- **Rejection:** If partner/TIME rejects – **full regenerate** (InvoiceRejected → ReadyForInvoice → Invoiced) or **simple correction** (InvoiceRejected → Reinvoice → Invoiced after correcting in TIME Portal).
- **Payment:** Payment recorded and matched to order; order moves to **Completed** and is locked.
- **Assurance / RMA:** Serialised replacements require TIME approval before ReadyForInvoice; non-serialised require at least one replacement row. RMA fields read-only after ReadyForInvoice.

---

## Legacy references (reference only)

- [02_modules/billing/OVERVIEW.md](../02_modules/billing/OVERVIEW.md) – Legacy billing/tax/e-invoice module spec.
- [02_modules/billing/WORKFLOW.md](../02_modules/billing/WORKFLOW.md) – Legacy billing workflow.
- [02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md](../02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md) – Legacy MyInvois submission notes.
