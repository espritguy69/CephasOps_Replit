# Workflow Relationships (Full Production)

## 1. Overview

This document describes **cross-entity workflows** and how they connect:

1. Order lifecycle (Order ↔ WorkOrder ↔ Docket ↔ Materials).
2. Email parsing and order creation.
3. Rescheduling with ISP approval.
4. Docket submission and cut-off rules.
5. Materials issuance and serial tracing.
6. Background jobs (including snapshot cleanup).

The key high-level flows:

- `Email → ParseSession → ParsedOrderDraft → Approve → Order (+ WorkOrder)`
- `Email → ParseSession → RescheduleRequest → Approve → Order/WorkOrder schedule change`
- `Email → ParseSession → Approve → Order → SnapshotCleanupJob`

---

## 2. Order Lifecycle Relationships

### 2.1 Customer & Premises to Order

- `CustomerAccount` (1) → `Premises` (N)
- `CustomerAccount` (1) → `Order` (N)
- `Premises` (1) → `Order` (N)

**Order** links:

- `Order.customer_account_id` (FK)
- `Order.premises_id` (FK)
- Optional: `Order.contact_person_id` (FK)

---

### 2.2 Order ↔ WorkOrder

- 1 `Order` → many `WorkOrder`.

Relationships:

- `WorkOrder.order_id` (FK → Order.id)
- `WorkOrder.sequence_number` indicates visit sequence.

Lifecycle:

1. New order → at least one work order is created.
2. Revisit or assurance → new `WorkOrder` with `sequence_number` incremented.

---

### 2.3 WorkOrder ↔ Installer / Subcon

- `WorkOrder.installer_profile_id` (FK → InstallerProfile)
- `WorkOrder.subcon_company_id` (FK → Company, if subcon)

Supporting entities:

- `Assignment` (records explicit assignment details):
  - `Assignment.work_order_id` (FK)
  - `Assignment.installer_profile_id` (FK)
  - 1 `WorkOrder` → N `Assignment` (assignment history).

---

### 2.4 Status Histories

- `OrderStatusHistory.order_id` (FK → Order)
- `WorkOrderStatusHistory.work_order_id` (FK → WorkOrder)

Every status change for order/work order is stored here, including reschedules, failures, and completions.

---

## 3. Email Parsing Workflow

### 3.1 EmailMessage ↔ ParseSession

- 1 `EmailMessage` → 0..1 `ParseSession`
- `ParseSession.email_message_id` (FK → EmailMessage)

---

### 3.2 ParseSession ↔ ParsedOrderDraft

- 1 `ParseSession` → 0..1 `ParsedOrderDraft`
- `ParsedOrderDraft.parse_session_id` (FK)

`ParsedOrderDraft` contains proposed order data extracted from the email.

---

### 3.3 ParsedOrderDraft → Order / WorkOrder

When `ParsedOrderDraft.status = Approved`:

- System creates:
  - `Order`
  - One initial `WorkOrder`
  - Maybe `CustomerAccount` and `Premises` if new.

Relationships:

- `ParsedOrderDraft.created_order_id` (FK → Order.id)

The flow:

```text
EmailMessage
  ↓ (parsed by job)
ParseSession
  ↓ (if parse successful)
ParsedOrderDraft (PendingApproval)
  ↓ (manual approval)
Order + WorkOrder (live, schedulable)
3.4 Snapshot & Cleanup Workflow
Parser may generate snapshot files:

SnapshotCleanupTask.parse_session_id (FK → ParseSession)

Flow:

text
Copy code
EmailMessage → ParseSession → Snapshot (file)
   ↓
SnapshotCleanupTask (created with expires_at = now + 7 days)
   ↓ (Background job)
Delete snapshot file → mark SnapshotCleanupTask.status = Deleted
4. Rescheduling & ISP Approval
4.1 RescheduleRequest Relationships
When admin requests reschedule:

RescheduleRequest.order_id (FK → Order)

RescheduleRequest.work_order_id (FK → WorkOrder, optional)

May link to an email approval:

RescheduleRequest.isp_email_message_id (FK → EmailMessage)

Flow:

text
Copy code
(Internal trigger / Customer call)
   ↓
RescheduleRequest (PendingApproval)
   ↓ (Email sent to TIME)
   ↓
EmailMessage (reply from TIME)
   ↓ (Parser or manual link)
RescheduleRequest.status = Approved
   ↓
Order / WorkOrder schedule fields updated
OrderStatusHistory / WorkOrderStatusHistory updated
5. Dockets, Cut-Off & Billing
5.1 WorkOrder ↔ Docket
1 WorkOrder → many Docket

For example, one e-docket plus one manual docket.

Relationships:

Docket.order_id (FK → Order)

Docket.work_order_id (FK → WorkOrder)

Docket.installer_profile_id (FK → InstallerProfile)

5.2 Docket ↔ DocketLine
1 Docket → many DocketLine

DocketLine is used for:

Base job fee

Allowances

Penalties

Adjustments

5.3 Docket ↔ Global Settings (Cut-Off)
Daily cut-off (e.g. 17:00) lives in:

GlobalSetting with key DAILY_DOCKET_CUTOFF

Workflow:

Installer submits docket.

System checks current_time vs DAILY_DOCKET_CUTOFF.

If missed, system may:

Mark as late, or

Move to next-day batch, depending on policy.

A scheduled job (via JobDefinition / JobRun) can enforce:

Reminders before cut-off.

Reports of missing dockets.

6. Materials & Templates
6.1 MaterialTemplate ↔ MaterialTemplateItem
1 MaterialTemplate → many MaterialTemplateItem

Maps a job type or install profile to a set of material items.

6.2 MaterialTemplate ↔ MaterialAssignmentPreset
1 MaterialTemplate → many MaterialAssignmentPreset (over time)

MaterialAssignmentPreset ties:

job_type → material_template_id

So job type FTTH_STD may always use a specific template, unless overridden.

6.3 WorkOrder ↔ MaterialTemplate
During planning:

System checks job type of the Order/WorkOrder.

Looks up MaterialAssignmentPreset for that job type.

Expands to a collection of MaterialTemplateItem.

Creates WorkOrderMaterial records.

Relationships:

WorkOrderMaterial.work_order_id (FK)

WorkOrderMaterial.material_item_id (FK)

WorkOrderMaterial.order_id (FK)

6.4 Materials ↔ MaterialMovement ↔ Warehouse
MaterialMovement.work_order_id (FK) connects materials to job.

MaterialMovement.from_warehouse_id / to_warehouse_id (FK → Warehouse)

MaterialSerial is updated for serialised items:

status changes InStock → Installed

order_id / work_order_id set

7. Background Jobs & Logging
7.1 JobDefinition ↔ JobRun
1 JobDefinition → many JobRun

Jobs can be:

Email fetch & parse

SLA check

Docket reminder

Snapshot cleanup

7.2 JobRun ↔ SystemLog
1 JobRun → many SystemLog entries (for debugging)

Each log entry sets:

entity_type = "JobRun"

entity_id = job_run.id

7.3 SystemLog / AuditEvent ↔ Domain Entities
Every critical operation attaches context:

SystemLog.entity_type and entity_id:

Order / WorkOrder / Docket / SnapshotCleanupTask / etc.

AuditEvent.user_id:

Who changed what, when (status, assignments, permissions).

8. Summary Diagrams (Text)
8.1 Email → Order Creation
text
Copy code
EmailMessage
  ↓
ParseSession
  ↓
ParsedOrderDraft (PendingApproval)
  ↓ (Approved)
Order
  ↓
WorkOrder
8.2 Email → Order → SnapshotCleanup
text
Copy code
EmailMessage
  ↓
ParseSession
  ↓
Snapshot (file)
  ↓
SnapshotCleanupTask (expires_at = +7 days)
  ↓ (JobDefinition: SNAPSHOT_CLEANUP)
JobRun
  ↓
Deletes expired snapshots
  ↓
SystemLog entries for each deletion
8.3 Reschedule with ISP Approval
text
Copy code
Dispatcher / Installer / Customer
  ↓
RescheduleRequest (PendingApproval)
  ↓
Email to TIME
  ↓
EmailMessage (Reply)
  ↓
Parser or manual link → RescheduleRequest.isp_email_message_id
  ↓
RescheduleRequest.status = Approved
  ↓
Order / WorkOrder dates updated
  ↓
OrderStatusHistory / WorkOrderStatusHistory entries