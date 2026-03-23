\# Background Jobs – Entities \& Relationships (Full Production)



\## 1. Overview

Background jobs handle:

\- Snapshot cleanup (7-day deletion)

\- Email fetch + parsing

\- Order auto-assignment

\- SLA reminders

\- Docket deadline checks

\- Material serial mismatch scan



All jobs run under a global scheduler.



---



\## 2. Entities



\### 2.1 `JobDefinition`

Represents a registered background job type.



Fields:

\- `id` (PK)

\- `job\_code` (unique, e.g. SNAPSHOT\_CLEANUP)

\- `name`

\- `description`

\- `schedule\_cron`

\- `is\_active`

\- `max\_retry`

\- `timeout\_seconds`

\- `created\_at`, `updated\_at`



Relationships:

\- 1 `JobDefinition` → many `JobRun`



---



\### 2.2 `JobRun`

Execution instance of a job.



Fields:

\- `id` (PK)

\- `job\_definition\_id` (FK → JobDefinition)

\- `started\_at`

\- `finished\_at`

\- `status` (Success / Failed / Timeout / Running)

\- `error\_message`

\- `records\_processed`

\- `metadata` (JSON)

\- `created\_at`



Relationships:

\- Each run may create multiple logs:

&nbsp; - 1 `JobRun` → many `SystemLog` (via logging\_entities.md)



---



\### 2.3 `SnapshotCleanupTask`

Represents snapshot files pending cleanup.



Fields:

\- `id` (PK)

\- `parse\_session\_id` (FK → ParseSession)

\- `snapshot\_path`

\- `expires\_at` (7 days after creation)

\- `deleted\_at` (nullable)

\- `status` (Pending / Deleted / Failed)

\- `failure\_reason`

\- `created\_at`, `updated\_at`



Relationships:

\- 1 `SnapshotCleanupTask` → 1 `ParseSession`



---



\## 3. Workflow

\- New snapshot → `SnapshotCleanupTask` created  

\- Background job scans expired records  

\- Deletes files physically  

\- Marks status → Deleted  



