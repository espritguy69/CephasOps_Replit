# BACKGROUND_JOBS_MODULE.md
Background Jobs & Email Ingestion – Full Backend Specification

---

## 1. Purpose

Centralize all **scheduled and background tasks**, including:

- **Email ingestion worker** (TIME/Celcom work orders)
- **Snapshot cleanup** (email parser)
- **P&L rebuild** jobs
- Periodic billing/aging recalculations
- Future maintenance tasks

---

## 2. Data Model

### 2.1 BackgroundJob

Represents a logical job type (template).

Fields:

- `Id`
- `Name` (e.g. "EmailIngestion", "SnapshotCleanup", "PnlRebuild")
- `JobType` (enum)
- `ScheduleCron` (string; cron expression or equivalent)
- `IsEnabled`
- `LastRunAt` (nullable)
- `LastRunStatus` (enum: Success, Failed, Running, Disabled)
- `LastRunMessage` (text)
- `ConfigJson` (job-specific config; e.g. which company/partner/mailbox)

---

### 2.2 BackgroundJobExecution

History of job runs.

Fields:

- `Id`
- `BackgroundJobId`
- `StartedAt`
- `FinishedAt`
- `Status` (Success, Failed)
- `Message` (error summary or success note)
- `RunConfigJson` (resolved effective config)
- `CorrelationId`

Indexes:

- `(BackgroundJobId, StartedAt DESC)`

---

## 3. Email Ingestion Worker

### 3.1 Responsibilities

- Periodically poll EmailAccount mailboxes (via IMAP/POP3)
- Download new emails + attachments
- Store them as EmailMessage + EmailAttachment
- Trigger parse pipeline → ParseSession → ParsedOrderDraft
- Mark emails as processed (flag, move folder, or metadata)

### 3.2 Configuration

In `BackgroundJob.ConfigJson` for EmailIngestion:

Example:

```json
{
  "emailAccountId": "GUID",
  "pollIntervalMinutes": 5,
  "maxEmailsPerRun": 50,
  "folder": "INBOX",
  "archiveFolder": "Processed"
}

---
3.3 Flow

For each enabled EmailIngestion job:

Load EmailAccount credentials from CompanySetting.

Connect via IMAP/POP3.

Fetch unseen messages (up to limit).

Persist EmailMessage + EmailAttachment.

Call Email Parser service to create ParseSession.

Optionally auto-run parsing and create ParsedOrderDraft.

Move/flag email as processed.

Log execution result in BackgroundJobExecution.

4. Snapshot Cleanup Job
4.1 Purpose

Delete old snapshots (Excel/attachments) after X days, as defined by:

CompanySetting.SnapshotRetentionDays or

GlobalSetting.SnapshotRetentionDays

4.2 Flow

Select SnapshotRecord older than today - retentionDays.

Delete from storage.

Mark as deleted in DB.

5. P&L Rebuild Job
5.1 Purpose

Rebuild P&L nightly or on-demand.

Config example:

{
  "companyId": "GUID",
  "daysBack": 90
}


Flow:

Determine fromPeriod and toPeriod.

Call PnlRebuildService.RebuildAsync(companyId, fromPeriod, toPeriod).

Log summary (orders processed, errors, etc.).

6. Framework

Implementation can use:

Hangfire, Quartz.NET, or a custom hosted service.

Backend responsibilities:

Provide BackgroundJob entities and APIs.

A single JobScheduler service reads BackgroundJob records and registers/executes them.

7. API

Base path: /api/background-jobs

GET /api/background-jobs

GET /api/background-jobs/{id}

POST /api/background-jobs

PUT /api/background-jobs/{id}

POST /api/background-jobs/{id}/run (manual trigger)

GET /api/background-jobs/{id}/executions

8. Security

Only Admin/DevOps roles can manage jobs.

Read-only view may be allowed for Ops to see “when did email sync last run?”.

---


---

## 6. `SPLITTERS_MANAGEMENT_MODULE.md`

```md
# SPLITTERS_MANAGEMENT_MODULE.md
Splitters & Ports Management – Full Backend Specification

---

## 1. Purpose

This module tracks **network splitters and their ports**, enforcing TIME’s requirements:

- Each building has registered splitters
- Ports are tracked per order
- Standby ports are respected
- Splitter usage required before completing jobs

---

## 2. Data Model

### 2.1 Splitter

Fields:

- `Id`
- `CompanyId`
- `BuildingId`
- `Name` (e.g. "B-1", "MDF-1")
- `SplitterType` (enum; 1:8, 1:16, 1:32, etc.)
- `TotalPorts` (int)
- `StandbyPortNumber` (nullable; e.g. 32)
- `LocationDescription` (text: riser level, MDF room, etc.)
- `IsActive`
- `CreatedAt`, `CreatedByUserId`
- `UpdatedAt`, `UpdatedByUserId`

Indexes:

- `(CompanyId, BuildingId)`
- `(CompanyId, Name)`

---

### 2.2 SplitterPort

Fields:

- `Id`
- `CompanyId`
- `SplitterId`
- `PortNumber` (1..TotalPorts)
- `IsStandbyPort` (bool)
- `Status` (enum: Available, Reserved, Used, Faulty)
- `CurrentOrderId` (nullable)
- `AssignedAt` (nullable)
- `AssignedByUserId` (nullable)
- `LockedAt` (nullable – when permanently used)
- `MetadataJson` (optional)

Unique constraint:

- `(SplitterId, PortNumber)`

---

### 2.3 SplitterUsageLog

Tracks historical usage.

Fields:

- `Id`
- `CompanyId`
- `OrderId`
- `BuildingId`
- `SplitterId`
- `PortNumber`
- `ServiceId` (from order)
- `Status` (Assigned, Released)
- `Timestamp`
- `ChangedByUserId`
- `Notes`

Indexes:

- `(CompanyId, OrderId)`
- `(CompanyId, SplitterId, PortNumber)`

---

## 3. Integration with Orders

`OrderSplitterAllocation` (already documented under Orders) links Orders to Splitter & Port. This module is the **master data**; Orders stores the relationship.

Workflow:

- When SI completes job in SI App (MetCustomer → OrderCompleted transition):
  - SI supplies `splitterId` + `portNumber`.
  - WorkflowEngine validates using Splitter + SplitterPort data (see rules below).
  - If valid:
    - Mark SplitterPort:
      - `Status = Used`
      - `CurrentOrderId = orderId`
      - `LockedAt = now`
    - Create SplitterUsageLog.

---

## 4. Business Rules

1. **Port must exist**:
   - `PortNumber` between 1 and `TotalPorts`.
2. **Port must belong to building**:
   - Splitter.BuildingId = Order.BuildingId.
3. **Port availability**:
   - `Status` must be `Available` before assignment.
4. **Standby port rule**:
   - If port is marked `IsStandbyPort`:
     - Require approval:
       - `StandbyOverrideApproved = true`
       - `ApprovalAttachmentId` provided (DocumentTemplates/File integration).
5. **Uniqueness**:
   - A port once `Used` cannot be reassigned unless explicitly released (for rare reconfiguration).

---

## 5. Services

### 5.1 SplitterService

- CRUD for Splitter
- Auto-generate SplitterPort records for `TotalPorts` on creation
- Manage standby port settings

### 5.2 SplitterPortService

- Query available ports for building
- Reserve a port for an upcoming job (future use)
- Mark port as used when job completed
- Release port (if rollback or migration)

### 5.3 SplitterUsageService

- Log usage
- Provide history for audits.

---

## 6. API

Base path: `/api/splitters`

- `GET /api/splitters?companyId=&buildingId=`
- `GET /api/splitters/{id}`
- `POST /api/splitters`
- `PUT /api/splitters/{id}`

Ports:

- `GET /api/splitters/{splitterId}/ports`
- `GET /api/splitters/{splitterId}/ports/available`
- `POST /api/splitters/{splitterId}/ports/{portNumber}/assign`
  - Body: `orderId`, optional `notes`
- `POST /api/splitters/{splitterId}/ports/{portNumber}/release`

Logs:

- `GET /api/splitter-usage`
  - Filters: `companyId`, `orderId`, `splitterId`, `portNumber`

---

## 7. Security

- Only Admin / Network roles can edit Splitters and Ports.
- SI App can **view** building splitters+ports and propose a port, but final validation & locking is enforced via backend.

