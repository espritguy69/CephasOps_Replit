# Business Process Flows

**Related:** [Product overview](../overview/product_overview.md) | [Order lifecycle summary](order_lifecycle_summary.md) | [Docket process](docket_process.md) | [Billing & MyInvois](billing_myinvois_flow.md) | [01_system/ORDER_LIFECYCLE](../01_system/ORDER_LIFECYCLE.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. End-to-end main flow

**Email received**  
→ **Parsed into order draft** (parser matches building; extracts customer, address, Service ID)  
→ **Order approved and enriched** (admin sets Service ID, partner, customer, address, building, appointment)  
→ **Assigned to installer** (SI and slot set; job appears in SI app)  
→ **Installer attends** (On the way → Met customer)  
→ **Job completed or blocked** (Completed: splitter, port, ONU serial, photos, signature; or Blocker/Reschedule with reason and evidence)  
→ **Docket received by admin** (paper/WhatsApp/email)  
→ **Docket verified** (rejected if wrong → SI corrects; otherwise accepted)  
→ **Docket uploaded** to partner portal (e.g. TIME)  
→ **Invoice prepared** (BOQ, rate card, materials; for assurance with RMA, replacements must be approved)  
→ **Invoice generated** (PDF, document template)  
→ **Invoice submitted** to MyInvois (e-invoice); status polled in background  
→ **Payment received and matched** (order marked Completed)

---

## 2. Side paths

- **Reschedule:** Admin requests TIME approval (by email); same-day reschedule requires customer evidence (WhatsApp/SMS/call log/voice note).
- **Blocker:** Job cannot proceed (e.g. building denies access, wrong address, customer postpones). Blocker → reassign or cancel; override to Completed requires HOD/SuperAdmin with reason and evidence.
- **Docket rejected:** Admin rejects (wrong splitter/ONU, missing photos, etc.) → SI corrects or resends → Dockets received again.
- **Invoice rejected:** Partner/TIME rejects → admin corrects and resubmits (Ready for invoice → Invoiced, or Reinvoice → Invoiced).
- **Cancelled:** Terminal state (customer withdraws, partner cancels, building denies permanently, duplicate order, etc.).

---

## 3. Order status flow (summary)

Pending → Assigned → OnTheWay → MetCustomer → (Blocker | ReschedulePendingApproval) → OrderCompleted → DocketsReceived → (DocketsRejected | DocketsUploaded) → ReadyForInvoice → Invoiced → (InvoiceRejected → Reinvoice → Invoiced)* → Completed.  
Side: Cancelled.

*InvoiceRejected can loop back to ReadyForInvoice or Reinvoice depending on correction path.

---

## 4. What the system does not cover (see scope doc)

Leads/quotation-to-order; statutory payroll (EPF, SOCSO, PCB); offline SI app; partner API; payment gateway; full GL/accounting. See [Scope not handled](../operations/scope_not_handled.md).
