# CephasOps – ORDER LIFECYCLE SPEC v2.0

**Full Production Version**  
**Updated for 2025**

---

## Department Scope

**This lifecycle applies specifically to the GPON department.**

Other departments (CWO, NWO) will use department-specific workflows when activated. Each department operates its own lifecycle definition configured in **Settings → Workflow Definitions**.

- **GPON Department**: Uses the full lifecycle defined in this document (active in v1)
- **CWO Department**: Will define custom lifecycle when activated (future)
- **NWO Department**: Will define custom lifecycle when activated (future)

Departments may inherit the GPON structure as a starting point or create entirely different workflows based on operational requirements.

---

## Lifecycle Purpose

This lifecycle defines how every GPON job moves through CephasOps from creation → scheduling → fieldwork → verification → billing → payment.

It reflects real-world processes from TIME, CelcomDigi, U Mobile, SI field operations, admin audit workflow, and internal CephasOps controls.



1. CORE PRINCIPLES

Every order is fully traceable
All changes (status, SI action, splitter, docket, invoice, audit, photos) are stored in:
order.statusHistory\[].

Service ID (TBBN or Partner Service ID) is the universal unique key
- TBBN format: TBBN[A-Z]?\d+[A-Z]? (e.g., TBBN1234567, TBBNA12345)
- Partner Service ID: Partner-specific formats (e.g., CELCOM0016996, DIGI0012345)
- System auto-detects Service ID type and can auto-select partner
- If unavailable (e.g. non-TIME partners), system uses partner-specific Service ID formats

TIME approval is required for reschedules EXCEPT same-day
TIME approvals come via email, never API.

Same-day reschedule requires customer evidence
WhatsApp screenshot, SMS, call log, or voice note.

Splitter details must be complete before docket upload
No splitter = no docket upload = no invoice.

Billing relies on data accuracy
Wrong ONU/splitter/photos → risk of non-payment → must be controlled.

TIME X Portal is reference only
No API integration; Admin mirrors statuses manually with evidence.

SI App is source of truth for fieldwork
GPS, ONU scan, port selection, photos, signature.

Only HOD / SuperAdmin can override protections
Overrides require reason + remark + evidence.

2. MASTER STATUS FLOW
   Pending  
   → Assigned  
   → OnTheWay  
   → MetCustomer  
   → (Blocker | ReschedulePendingApproval)  
   → OrderCompleted  
   → DocketsReceived  
   → (DocketsRejected | DocketsUploaded)  
   → ReadyForInvoice  
   → Invoiced  
   → (InvoiceRejected → Reinvoice → Invoiced)\*  
   → Completed

Side States:  
→ Cancelled



* InvoiceRejected optionally loops back to ReadyForInvoice if full regeneration is needed.

3. STATUS DEFINITIONS \& RULES
   3.1 Pending

Order created via:

Email parser

Manual entry

API

Admin can edit EVERYTHING.

KPI: Admin KPI (accuracy of data entry)

3.2 Assigned

Admin assigns SI, sets appointment.

Mandatory before Assigned:

Service ID / Partner Order ID

Customer details

Address

Appointment datetime

SI assigned

Material list generated

Building selected

System actions:

Create SI job card

Add to SI calendar

Log assignment

KPI: Admin KPI (assignment accuracy)

3.3 OnTheWay

Set by SI App or Admin (manual TIME Portal mirror).

KPI: SI KPI (punctuality)

3.4 MetCustomer

SI meets the customer.

**For Assurance Orders:**
- RMA fields (material replacements) become editable
- SI can record old/new material swaps if devices were replaced
- Fields remain locked until this status

KPI: SI KPI (arrival quality)

3.5 Blocker (Full Controlled Production)

Blocker = job cannot proceed.
Two categories:

Pre-Customer Blocker (before MetCustomer)

Post-Customer Blocker (after MetCustomer)

KPI Rules:

Pre-Customer: SI KPI or Admin KPI depending on reason

Post-Customer: SI KPI

3.5.1 Blocker Timing Rules

Allowed only at:
Assigned, OnTheWay → Pre-Customer Blocker
MetCustomer → Post-Customer Blocker

3.5.2 Pre-Customer Blocker Allowed Reasons

Building denies access

MDF/IDF locked

Riser locked

FWS room locked

No access card

Lift unavailable

Splitter room inaccessible

Incorrect building / wrong block

Unsafe environment

Customer wants to postpone (dual)

Customer gave wrong location (dual)

KPI:

Building/network issues → SI KPI

Admin scheduling mistakes (wrong address) → Admin KPI

3.5.3 Post-Customer Blocker Allowed Reasons

Customer rejects cabling fee

Customer disagrees routing

Customer declines installation

Customer postpones

Technical issue inside unit (ONU/router faulty)

LOSi / LOBi

Port mismatch

KPI: SI KPI

3.5.4 Blocker Mandatory Fields
blocker.category
blocker.reason
blocker.remark
blocker.evidence\[] (SI requires ≥ 1 photo)
gps (SI)
reportedBy
timestamp

3.5.5 Blocker Transitions

Valid:

Blocker → Assigned

Blocker → ReschedulePendingApproval

Blocker → Cancelled

Invalid (requires override):

Blocker → OrderCompleted

Blocker → DocketsReceived

3.6 ReschedulePendingApproval

Admin requests TIME approval.

Locked until TIME email.

Exit:
ReschedulePendingApproval → Assigned

KPI: Admin KPI (reschedule quality)

3.7 OrderCompleted

SI submits completion package:

Splitter ID

Port

ONU Serial

Photos

Signature

**For Assurance Orders:**
- If materials were replaced, SI/Admin records RMA data:
  - Serialised replacements: Old device + New device (TIME approval required later)
  - Non-serialised replacements: Material type + quantity (no approval needed)

KPI: SI KPI (accuracy \& completeness)

3.8 DocketsReceived

Admin has received SI docket (paper/WhatsApp/email).

✔ NEW RULE: Docket can be rejected before upload

If Admin detects errors BEFORE DocketsUploaded:

DocketsReceived → DocketsRejected

Common issues:

Wrong splitter

Wrong ONU

Missing photos

Wrong job category

Wrong customer details

Incorrect SI data

Docket from different job

KPI:
DocketsRejected → SI KPI (SI provided incorrect job data)
Dockets verification accuracy → Admin KPI

3.9 DocketsRejected

Admin rejects docket. SI must correct or resend.

Re-entry:
DocketsRejected → DocketsReceived

KPI:

SI KPI for incorrect job submission

Admin KPI for verification quality

3.10 DocketsUploaded

Admin uploads the docket to TIME Portal.

Mandatory Fields:

Docket number

Splitter ID + Port

ONU Serial

Completion Photos

KPI: Admin KPI

3.11 ReadyForInvoice

Admin prepares invoice:

BOQ/BOW

Customer details

Rate card

Materials

**For Assurance Orders - RMA Validation:**
- If serialised materials were replaced:
  - All RMA entries must have TIME approval (`approvedBy` + `approvalNotes`)
  - Missing approval → **BLOCK** transition to ReadyForInvoice
- If non-serialised materials were replaced:
  - At least one replacement row must exist with material type and quantity
- RMA fields become read-only after this status

KPI: Admin KPI

3.12 Invoiced

Admin uploads invoice to TIME portal.

Mandatory:
invoice.submissionId

System sets 45-day due date.

KPI: Admin KPI (billing accuracy)

3.13 InvoiceRejected (NEW Full Flow Added)

TIME rejects the invoice.

Allowed transitions:
Path 1 (Correct full invoice regeneration)
Invoiced → InvoiceRejected → ReadyForInvoice → Invoiced

Path 2 (Simple correction via Reinvoice state)
Invoiced → InvoiceRejected → Reinvoice → Invoiced

Rejection reasons:

Wrong BOQ

Wrong rate

Wrong job category

Missing docket details

Missing documents

Duplicate submission

Incorrect splitter/ONU shown in invoice

Incorrect submissionId

KPI:
📌 InvoiceRejected → Admin KPI (billing accuracy failure)

3.14 Reinvoice

Admin corrects invoice details inside TIME Portal.

Then:

Reinvoice → Invoiced



KPI: Admin KPI

3.15 Completed

Payment received \& matched.

KPI: Finance KPI (optional)

Order becomes locked.

3.16 Cancelled

Terminal state.

Reasons:

Customer withdraws

TIME cancels

Building denies permanently

Duplicate order

KPI:
Based on cause:

SI KPI (SI at fault)

Admin KPI (admin input error)

4. KPI RESPONSIBILITY MATRIX (FULL)
   Status	Primary Actor	KPI Impact
   Pending	Admin	Admin KPI
   Assigned	Admin	Admin KPI
   OnTheWay	SI	SI KPI
   MetCustomer	SI	SI KPI
   Blocker – Pre	SI/Admin	Mixed
   Blocker – Post	SI	SI KPI
   ReschedulePendingApproval	Admin	Admin KPI
   OrderCompleted	SI	SI KPI
   DocketsReceived	Admin	Admin KPI
   DocketsRejected	SI	SI KPI
   DocketsUploaded	Admin	Admin KPI
   ReadyForInvoice	Admin	Admin KPI
   Invoiced	Admin	Admin KPI
   InvoiceRejected	Admin/Clerk	Admin KPI
   Reinvoice	Admin	Admin KPI
   Completed	Finance	Finance KPI
   Cancelled	Admin/SI	Depends on cause
5. AUDIT TRAIL (MANDATORY)

Every transition logs:

performedBy.userId
performedBy.role
timestamp
source
fromStatus
toStatus
beforeValues
afterValues
remark
evidence\[]
gps (if SI)

6. OVERRIDE RULES

Only:

HOD

SuperAdmin

Director

can override statuses or validations.

Mandatory override fields:

override.enabled
override.role
override.reason
override.remark
override.evidence\[]
timestamp



Overrides appear in:

statusHistory\[]

auditTrail\[]

splitterAudit

7. MERMAID – FULL PRODUCTION DIAGRAM
   flowchart TD

P\[Pending]
A\[Assigned]
OTW\[OnTheWay]
MC\[MetCustomer]

BL\[Blocker]
RPA\[Reschedule Pending Approval]

OC\[OrderCompleted]
DR\[DocketsReceived]
DRJ\[DocketsRejected]

DU\[DocketsUploaded]
RI\[ReadyForInvoice]
IV\[Invoiced]

IRJ\[InvoiceRejected]
RE\[Reinvoice]

C\[Completed]
X\[Cancelled]

P --> A
A --> OTW
OTW --> MC

A --> BL
OTW --> BL
MC --> BL

BL --> RPA
RPA --> A

MC --> OC
OC --> DR

DR --> DRJ
DRJ --> DR

DR --> DU
DU --> RI
RI --> IV

IV --> IRJ
IRJ --> RI
IRJ --> RE
RE --> IV

IV --> C

P --> X
A --> X
BL --> X
RPA --> X

## 8. GOVERNANCE

This document is the single source of truth for CephasOps lifecycle.

All backend, frontend, SI app, and operations must follow this spec.

Changes must be:

1. Updated here
2. Implemented in code
3. Communicated to all departments

---

**Related Documentation:**

- [WORKFLOW_ENGINE.md](./WORKFLOW_ENGINE.md) - Workflow transition rules and validation logic
- [SYSTEM_OVERVIEW.md](./SYSTEM_OVERVIEW.md) - Overall system architecture
- [EMAIL_PIPELINE.md](./EMAIL_PIPELINE.md) - Parser events triggering lifecycle changes

---

✅ **END OF CephasOps ORDER LIFECYCLE SPEC v2.0 (Production)**

