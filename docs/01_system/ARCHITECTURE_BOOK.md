ARCHITECTURE\_BOOK.md – CephasOps v2.0 (Condensed Enterprise Edition)

Production Architecture Handbook
Status: FINAL
Audience: System Architects, Backend/Frontend Engineers, HOD, Cursor AI
Version: 2.0
Confidential – Internal Use Only

1. Executive Overview
   1.1 Purpose

This Architecture Book defines the complete, authoritative technical and operational specification for CephasOps. It governs all system behaviour, lifecycle transitions, workflow enforcement, splitter governance, inventory rules, billing flows, invoicing logic, and audit requirements.
It is the primary reference document for Cursor, all engineering teams, operations, and internal audit.

1.2 System Objectives

CephasOps must:

Enforce a strict order lifecycle

Ensure splitter and material governance

Maintain billing and invoicing compliance

Support audit-safe operations

Guarantee data integrity

Provide predictable, repeatable behaviour

Prevent invalid transitions and financial risk

1.3 Compliance Requirements

CephasOps must comply with:

TIME billing rules

Operational workflow standards

Data integrity \& auditability

Clean Architecture constraints

Mandatory evidence on overrides

Strict SI/Admin/HOD role separation

1.4 Definitions
Service ID (TBBN…)

The primary unique identifier for all installation, assurance, and billing workflows.

Partner Order ID

Fallback identifier for partners without a Service ID system.

TTKT (Trouble Ticket) — FORMAL ENTERPRISE DEFINITION

A Trouble Ticket (TTKT) is an official incident record created when a customer reports a service issue.
In CephasOps:

A TTKT is always tied to a specific Service ID.
The Service ID is used to retrieve customer details for the assurance workflow.

A single Service ID may have multiple TTKTs.
Each TTKT represents a separate reported problem, fault, or complaint.

Each TTKT is an independent assurance case.
Every TTKT must be handled, tracked, and billed individually.

Each TTKT is individually claimable.
Two TTKTs under the same Service ID are treated as two separate assurance jobs.

TTKT is mandatory for assurance and claim submission.
Both Service ID and TTKT number must be provided when:

Creating an assurance order

Submitting assurance claims

Uploading supporting documents

Closing the assurance case in CephasOps

Failure to supply the correct TTKT number results in:

Claim rejection

Invalid assurance mapping

Revenue loss

Incorrect operational reporting

Therefore, TTKT is a critical mandatory field in CephasOps for assurance workflows.

Blocker

A formally declared interruption caused by customer, building, network, or technical factors.

Docket

The installation or assurance completion record.

Invoice Rejection

A rejection issued by TIME or partner due to incorrect billing data.

Override

A HOD/SuperAdmin-authorised forced bypass that must include mandatory evidence.

2. Platform Architecture
   2.1 System Context Diagram
   flowchart TD
   UserAdmin --> AdminUI
   Installer --> SIApp

AdminUI --> API
SIApp --> API

API --> Domain
Domain --> Database\[(PostgreSQL)]

API --> EmailGateway
API --> TimeXPortalManual

2.2 High-Level Components

Admin Portal

SI Mobile App

Backend API

Domain Engine

Workflow Engine

Material/Inventory Engine

Billing \& Invoice Engine

Email Parser

Audit \& KPI Engine

2.3 Technology Stack

.NET 10 (API, Domain, App, Infra)

React + Vite + ShadCN (Admin)

React/Vite (SI App)

PostgreSQL

Email parser (POP3/IMAP)

3. Clean Architecture Specification
   3.1 Domain Layer

Contains all business rules, invariants, and policies.
MUST NOT reference Application or Infrastructure.

3.2 Application Layer

Contains use cases, validators, workflow transitions, and domain event handlers.

3.3 Infrastructure Layer

Responsible for persistence, email parsing, file storage, and external integrations.

3.4 API Layer

Defines request/response models, routing, and access control.

3.5 UI Layer

Admin Portal for operations

SI App for field activities

4. Order Lifecycle (Strict State Machine)
   4.1 Canonical Lifecycle
   Pending →
   Assigned →
   OnTheWay →
   MetCustomer →
   Blocker →
   ReschedulePendingApproval →
   OrderCompleted →
   DocketsReceived →
   DocketsRejected →
   DocketsUploaded →
   ReadyForInvoice →
   Invoiced →
   InvoiceRejected →
   Reinvoice →
   Completed



Cancelled is terminal before billing.

4.2 Lifecycle Diagram
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

5. Workflow Engine Specification
   5.1 Allowed Transitions

Strictly defined; no skipping allowed.

5.2 Validation Enforcement

Every transition requires specific fields (splitter, docket number, invoice submission ID, etc.).

5.3 Rejection Loops
DocketsRejected

Triggered for:

Wrong splitter

Wrong port

Missing mandatory photos

Wrong ONU

Incorrect category

Responsible KPI: SI

InvoiceRejected

Triggered for:

Wrong job category

Wrong rate

Wrong submission ID

TIME billing rule failure

Responsible KPI: Admin

5.4 Reschedule Rules

Normal: Requires TIME email approval

Same-Day: Customer evidence required

5.5 Blocker Enforcement

Pre-Customer Blockers only in Assigned/OnTheWay

Post-Customer Blockers only in MetCustomer

Timing + reason matrix must be respected.

5.6 Reinvoice Path
InvoiceRejected → Reinvoice → Invoiced



Used when correction can be done at portal level.

6. Responsibilities \& KPIs
   SI KPI

Docket rejected

Splitter/port errors

Incorrect ONU

Missing photos

Incorrect completion flow

Admin KPI

Invoice rejected

Mapping errors

Wrong submission ID

Wrong claim data

Finance KPI

Incorrect payment posting

Ops KPI

Incorrect reschedule flow

HOD/SuperAdmin KPI

Excessive overrides (policy breaches)

7. Splitter Governance
   7.1 Validation

Port is invalid if:

Already used

Faulty

Reserved as standby

Wrong splitter

Not linked to building

7.2 Override

Only HOD/SuperAdmin/Director.
Mandatory:

Reason

Remark

Evidence

Timestamp

Override creates a special audit event.

8. Materials \& Inventory

Serialized vs non-serialized handling

Installer allocations

Actual usage vs planned usage

Return/reclaim protocol

Full traceability

9. Billing \& Invoicing Architecture
   Docket Flow
   OrderCompleted → DocketsReceived → DocketsRejected (optional) → DocketsUploaded

Invoice Flow
ReadyForInvoice → Invoiced → InvoiceRejected → Reinvoice → Invoiced → Completed

Ageing Rule
DueDate = portalUploadDate + 45 days

10. Security \& Audit

Full audit trail for all status changes

Evidence mandatory for overrides

Role-based access control

No anonymous actions

11. Error Handling

Error categories:

WorkflowViolation

SplitterValidationError

MissingFieldError

PartnerRejectionError

AssignmentError

Errors must return structured, human-readable feedback.

12. Appendices

Blocker Reason Matrix

Rejection Reason List

Lifecycle Transition Matrix

Domain Glossary

Future Enhancements



13\. Parser Engine Architecture (Production)



The Parser Engine is a core, first-class subsystem in CephasOps, responsible for converting all external partner emails, attachments, and structured files into clean, validated CephasOps entities.



This engine is global, not GPON-only.

It supports every department (GPON, CWO, NWO, future divisions), every partner, and every partner group using a unified configuration layer.



It must be treated as a foundational platform module, similar to Workflow, Billing, and Inventory.



13.1 Purpose of the Parser Engine



The Parser Engine transforms raw partner data into structured entities that follow CephasOps business rules.



It ensures:



Zero hardcoding



Zero template logic inside backend code



Fully configurable formats



Strong validation



Strict normalisation rules



Clean mapping into the Order lifecycle



Compliance with TIME/Telco billing flows



All parsing behaviour is managed under:



Settings → Parser Templates





This allows operational teams (not developers) to add/update partners safely.



13.2 Why the Parser Engine Exists



Different partners provide data in:



Excel (XLS/XLSX)



PDF



HTML email body



MSG files



Hybrid formats



Free-text assurance emails



TTKT (Trouble Ticket) emails



Reschedule approval emails



Every partner has its own layout, header names and field identifiers.



The Parser Engine standardises all these into a single CephasOps JSON structure, allowing the domain and workflow engines to operate with completely uniform data.



13.3 Parser Templates (Configuration Layer)



Every parser is defined using a Parser Template, which includes:



Component       Description

Template Name   Human-friendly name (e.g., “TIME FTTH v3”)

Parser Type     Activation / Assurance / Modification / Reschedule

Partner DIGI / Celcom / TIME FTTH / TIME Assurance / U-Mobile

Partner Group   TIME Group (example)

Department      GPON / CWO / NWO

Supported Format        Excel / PDF / Email Body / Hybrid

Header Mappings Column-to-field rules

Extraction Rules        Regex, keyword sets, NLP rules

Mandatory Fields        Service ID, TTKT, Address, Contact, Appointment

Error Behaviour Reject, fallback, manual review

Normalisation Rules     Contact, date/time, address standards

Field Types     String, number, enum, datetime

Test Parser     Upload a sample → Preview JSON



Every Parser Template is bound to exactly one Partner record. Partner Group and Department are additional routing dimensions, but the primary owner of a template is the Partner (e.g. CELCOM, DIGI, TIME FTTH, TIME ASSURANCE, U-MOBILE). This matches the Settings → Company → Partners screen, where each Partner has its own Department and Partner Group.



The Parser Template is the single source of truth for how partner data is interpreted.



### 13.4 Department & Partner Integration Model

Each Parser Template is explicitly tied to:

- **Partner** (required) - The template owner (e.g., DIGI, CELCOM, TIME FTTH, TIME FTTO, TIME ASSURANCE, U-MOBILE)
- **Partner Group** (optional) - Logical grouping for inheritance (e.g., TIME Group)
- **Department** (required) - Determines which workflow applies (GPON, CWO, NWO)

**Ownership Model**: Parser Templates are **owned by Partners**, optionally grouped under Partner Groups for template inheritance and code reuse.

**Routing vs Workflow**:

- **Partner Group** is used for email routing and parser selection
- **Department** controls which workflow lifecycle is applied

Example:

| Department | Parser Templates |
|-----------|------------------|
| GPON | TIME FTTH, TIME FTTO, Digi HSBB, Celcom HSBB, U-Mobile |
| CWO | (future) CWO fault/repair formats |
| NWO | (future) Network-wide field ops formats |

This model enables:

- Multi-partner ingestion
- Multi-department routing
- Multi-tenant SaaS operation with multiple departments per company
- Full future scalability without code changes



13.5 Parser Flow (End-to-End)

1\. Email Setup



Email Setup defines which inboxes belong to which:



Partner Group



Partner



Department



Parser Template



Example:



TIME FTTH Inbox → TIME FTTH Parser → GPON Department

TIME Assurance Inbox → Assurance Parser → GPON Department

CWO Inbox (future) → CWO Parser → CWO Department



2\. Ingestion



Email fetcher saves:



Subject



Sender



Body HTML



Attachments



Message ID



Received timestamp



Into email\_raw.



3\. Classification



Determines:



Intent (activate/assurance/reschedule)



Partner



Partner Group



Confidence score



If confidence < 0.75 → moved to "Parser Review Queue".



4\. Parser Execution



The template defines extraction logic:



Excel header scanning



PDF text extraction



Regex rules



TTKT detection



Reschedule approval detection



NLP for date/time phrases



Contact number fixup



Address cleanup



5\. Normalisation



Enforces CephasOps formatting rules:



Contact number → always localised (012xxxxxxx)



Date/time → YYYY-MM-DD HH:mm



Building name extraction



Lowercase/uppercase standardisation



6\. Mapping to Domain Entities



Parser output is mapped into:



Order

Customer

Appointment

Installation Type

Partner

Department

TTKT (if Assurance)



7\. Order Resolver



Decides:



Create new order



Update existing order



Apply reschedule



Update contact/address



Merge duplicate partner emails



Prevent duplicate order creation



8\. Handover to Workflow Engine



Once validated:



Lifecycle transitions executed



Splitter rules enforced



Inventory allocation planned



Audit logs created



13.6 Error \& Rejection Governance

Errors sent to:



email\_error\_queue



Parser Review Queue



Admin notification channel



Examples of automatic rejection:



Missing Service ID



Missing TTKT for assurance



Invalid appointment format



Excel corrupted



Wrong partner template used



This ensures bad partner data never pollutes the domain.



13.7 TTKT Handling (Integrated)



TTKT is a mandatory core field for all Assurance orders.



Rules:



Each TTKT belongs to one Service ID



One Service ID may have multiple TTKT records



Each TTKT is a separate assurance order



TTKT is required for claim submission



Missing TTKT → parser rejects and sends to Admin Review



The parser automatically extracts TTKT from:



Excel



PDF



Email body



Partner free-text templates



This aligns with Section 1.4 Definitions



ARCHITECTURE\_BOOK



.



13.8 Future-Proofing



The Parser Engine is built to support:



1\. Additional Departments



You can add:



CWO



NWO



Fibre Migration Team



Enterprise Install Team



…with no code changes.



2\. Additional Partners



Simply add:



New Partner Group



New Partner



New Parser Template



New Inbox



No engineering work needed.



3\. New File Layouts



If TIME changes Excel format → create:



TIME FTTH v4 Parser Template





Assign to inbox and done.



13.9 Architecture Diagram (Parser Perspective)

Email Inbox

   ↓

Ingestion Worker

   ↓

Classifier

   ↓

Parser Template Engine

   ↓

Normalizer

   ↓

Order Resolver

   ↓

Workflow Engine

   ↓

Database + Audit + KPIs

