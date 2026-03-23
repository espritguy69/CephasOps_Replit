# CephasOps Storybook – Full Production Version
This document explains **what CephasOps does**, **why it exists**, and **how the business logic works** from end-to-end.  
It is the *business bible* for developers and AI agents working on the system.

---

# 1. Who We Are and What We Do

## 1.1 Company
**CEPHAS SDN BHD** and **CEPHAS TRADING & SERVICES**  
We operate as subcontractors for multiple telecommunication partners:

### Primary Principal:
- **TIME dotCom (ISP)**

### TIME’s Partners Under Us:
- TIME–Digi HSBB
- TIME–Celcom HSBB
- TIME–U Mobile
- TIME–CelcomDigi (new integrations)

### Other Potential Direct Partners:
- Celcom
- Digi
- U-Mobile
- Future third-party ISPs

---

# 2. Nature of Work

Cephas handles the following job categories:

### 2.1 Activation Jobs (New Installations)
- FTTH (Home)
- FTTO (Office)
- FTTR (Room-to-Room)
- Fiber Modification (Activation-related)

### 2.2 Modification Jobs
- **Indoor Relocation**: Move within same unit  
  (e.g., Bedroom → Living Hall)
- **Outdoor Relocation**: Move to a different unit/premises  
  (old address → new address)

### 2.3 Assurance / Troubleshooting
- TTKT (Trouble Ticket)
- AWO (Assurance Work Order)
- Link Down (LOSi / LOBi)
- Router/ONU fault
- Internal wiring issues

### 2.4 SDU Jobs
- Surface / pole jobs
- RDF pole installs
- Special cabling jobs

---

# 3. How We Receive Orders

Orders always come through **EMAIL**, in one of the following formats:

### (A) TIME FTTH/FTTO Activation Emails
- With Excel or PDF attachments
- Standard layout (“Notification of Installation – FTTH/FTTO”)

### (B) TIME–Digi HSBB Emails
- HSBB-specific subject format
- Always attached with Excel
- Has **Digi partner service ID (DIGI00xxxxxx)**

### (C) TIME–Celcom HSBB Emails
- Same structure as Digi
- Partner service ID: **CELCOM00xxxxxx**

### (D) TIME Modification Emails
- Outdoor relocation = Two addresses (old/new)
- Indoor relocation = Same address, different room/location note

### (E) TIME Assurance Emails (Plain Text)
Contains:
- Service ID (TBBN…)
- TTID / TTKT
- Customer Details Block
- Issue Block
- Appointment
- IPTV URL (work order link)

### (F) TIME Reschedule Approval Emails (Human Replies)
These emails:
- Often have **NO attachment**
- Contain **human-written approval phrases**, e.g.
  - “Approved, please proceed”
  - “New appointment is on…”
  - “Rescheduled to…”

---

# 4. Super Important Business Rules

## 4.1 **TIME must approve any reschedule.**
Admin CANNOT change date/time on their own.

Flow:

1. Admin sends request
2. Order goes into `ReschedulePendingApproval`
3. TIME replies with approval email (Excel or plain text)
4. Parser updates order date/time + restores to `Assigned`

---

## 4.2 **Splitter Port Usage is Mandatory**
At job completion (before docket stage):

- Installer must record:
  - Splitter ID  
  - Port used
- System enforces:
  - Port not previously used
  - Port belongs to the building
  - One port must remain as **standby**
- Port once used → forever locked

---

## 4.3 **Outdoor Relocation = Two Addresses**
- New address = Where installation is done
- Old address = Must be captured (for history + TIME processing)

## 4.4 Indoor Relocation = Same Address, Room Change
Store:
- `oldLocationNote`
- `newLocationNote`

Just room movement.

---

## 4.5 Billing Logic

### Scenario 1 — TIME Principal Billing
For TIME, TIME–Digi, TIME–Celcom, TIME–U Mobile:

billing.billingScenario = TIME_PRINCIPAL
billing.billingPartnerId = TIME

shell
Copy code

### Scenario 2 — Direct Billing
For future partners:

billing.billingScenario = DIRECT
billing.billingPartnerId = <Partner>

yaml
Copy code

All invoices go through:
- Upload portal
- Payment due = portalUploadDate + 45 days

---

# 5. Order Creation Flow

## Step 1: Email Ingestion
- Email + attachments pulled in every 1 minute.

## Step 2: Parser Identifies Order Type
Based on:
- Subject
- Sender
- Attachment
- Content

## Step 3: Extract Required Fields
Depending on order type:
- Activation
- Modification
- Assurance
- SDU

## Step 4: Normalise Data
Includes:
- Address trimming
- Contact number fixing (remove +60, add leading 0)
- Case correction
- Date/time format standardisation

## Step 5: ID Matching
To avoid duplicates:
- Match by Service ID
- Match by Partner Order ID (DIGI/CELCOM format)
- Match by TTKT for Assurance

## Step 6: Create Order
If not matched → new order in `Pending`

---

# 6. Lifecycle (Business Perspective)

Below is the business flow (technical status mapping is in `order_lifecycle.md`).

### 6.1 Admin Confirmation
Admin validates:
- Building type
- Materials auto-suggested
- Installer assignment
- Appointment accuracy

### 6.2 Installer Execution
Installer must:
- Travel to site
- Record “On The Way”
- Record “Met Customer”
- Complete installation/troubleshooting
- Update completion info (photos, splitter)

### 6.3 Post-Completion
Admin must:
- Verify completion
- Collect docket
- Upload docket to TIME portal

### 6.4 Invoicing
System generates:
- BOQ
- Invoice PDF
- Price breakdown

Admin uploads to portal → system starts ageing.

### 6.5 Payment
Once paid:
- Order marked **Completed**

---

# 7. Calendar & Scheduling (Admin View)

Every order appears as a **card** on the calendar showing:

- Service ID (unique ID)
- Partner (TIME, Digi, Celcom…)
- Customer Name
- Time
- Building short name
- SI Assigned
- Status colour

### Click Actions:
- Change status (Assigned → OTW → Met Customer → Completed)
- Request reschedule
- View job details
- Set splitter usage

---

# 8. Material & Inventory Logic

Materials differ by building type.

### PRELAID  
- Patchcord 6m  
- Patchcord 10m

### NON-PRELAID  
- Fibre cable 80m  
- UPC, APC connectors

### SDU  
- RDF cable

### Rules:
- When order created → auto-material suggestion  
- When assigned → issue materials to SI  
- When job ends → update:
  - `Installer → Warehouse` (unused return)
  - `Installer → Customer` (used)

---

# 9. Assurance Workflow

A full Assurance order contains:
- Service ID (TBBN…)
- Ticket ID (TTKT…)
- AWO ID (optional)
- Issue (LOSi, LOBi, router failure)
- Appointment
- IPTV URL

### Assurance completion requires:
- Old ONU/Router collected (if applicable)
- New ONU/Router issued
- Full troubleshooting remark

---

# 10. What the System Must Guarantee

### 10.1 Business Accuracy
- No missing mandatory fields
- No illegal reschedules
- No duplicate orders
- No splitter misuse

### 10.2 Automation
- Email parsing accuracy ≥ 98%
- Status updates with minimal admin input

### 10.3 Accountability
- Every change logged
- Every SI action timestamped
- Every docket tracked
- Invoice ageing visible in dashboard

---

# 11. Future Enhancements

- Direct API integration with TIME (if they provide APIs)
- WhatsApp/SMS reminders to customers
- SI mobile app with live tracking
- Predictive ETA for SI arrival
- Auto reconciliation with bank payments
- Machine-learning based reschedule detection

---

# 12. Final Notes

This storybook must be used as:
- A training guide for developers
- A reference for AI agents in Cursor
- A master blueprint for future modules

CephasOps is not just a job tracker —  
It is a **full operational ecosystem** for managing a multi-partner ISP subcontractor business at scale.

