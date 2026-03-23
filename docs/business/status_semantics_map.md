# Order Status Semantics Map (GPON)

**Related:** [Order lifecycle and statuses (canonical)](order_lifecycle_and_statuses.md) | [Order lifecycle summary](order_lifecycle_summary.md)

**Source of truth:** docs/business/order_lifecycle_and_statuses.md

One-page reference for status owner, required fields, meaning, and evidence.

---

| Status | Owner | Required fields | Meaning | Evidence |
|--------|-------|-----------------|---------|----------|
| **Pending** | Admin | — | Order created; not yet assigned | Parser/manual/API creation |
| **Assigned** | Admin | ServiceId, customer details, address, appointment, SI assigned, material list, building | SI assigned; appointment confirmed | SI job card, calendar entry |
| **OnTheWay** | SI / Ops | — | SI travelling to site | SI app or manual TIME mirror |
| **MetCustomer** | SI | — | SI met customer on-site | SI app |
| **Blocker** | SI / Admin | category, reason, remark, evidence (≥1 photo), gps, reportedBy, timestamp | Job cannot proceed | Photo(s), GPS, reason |
| **ReschedulePendingApproval** | Admin | — | Waiting for TIME approval | TIME email |
| **OrderCompleted** | SI | Splitter ID, Port, ONU Serial, Photos, Signature | Physical job done; completion package submitted | SI app; RMA data if Assurance |
| **DocketsReceived** | Admin | — | Admin received docket (paper/WhatsApp/email) | Docket file |
| **DocketsVerified** | Admin | Docket number, Splitter ID + Port, ONU Serial, Completion Photos | Docket validated (QA passed) | QA checklist |
| **DocketsUploaded** | Admin | Docket number, Splitter ID + Port, ONU Serial, Completion Photos | Docket uploaded to partner portal | Portal confirmation |
| **ReadyForInvoice** | Admin | BOQ, customer details, rate card, materials; RMA approval if Assurance | Ready for billing | Invoice draft |
| **Invoiced** | Admin | BOQ, customer details, rate card, materials | Invoice prepared; ready for submission | Invoice record |
| **InvoiceRejected** | System | — | TIME rejected invoice | Rejection reason |
| **Reinvoice** | Admin | — | Admin correcting in TIME Portal | Portal correction |
| **SubmittedToPortal** | Ops (Billing) / System | invoice.submissionId (when MyInvois used) | Invoice submitted to partner portal | Portal/MyInvois confirmation |
| **Completed** | Finance | — | Payment received and matched | Payment allocation |
| **Cancelled** | Admin / SI | — | Order cancelled; terminal | Cancellation reason |
