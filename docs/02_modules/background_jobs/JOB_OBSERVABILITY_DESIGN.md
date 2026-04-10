# Job Monitoring & Background Task Observability — Design Document

## 1. Current-State Audit

### 1.1 Scheduler / Background Job Architecture

CephasOps uses **no third-party job engine** (no Hangfire, Quartz, or message bus). It uses:

- **ASP.NET Core `BackgroundService`** for both scheduling and processing.
- **Single database table `BackgroundJobs`** as the queue and state store.
- **One poll-based processor**: `BackgroundJobProcessorService` runs every 30 seconds, picks up to 10 queued jobs, executes them in-process, and updates the same row (Running → Succeeded/Failed).
- **Stale job reaper**: Running jobs exceeding a threshold (e.g. 10 min for EmailIngest, 120 min for others) are marked Failed with "Recovered: stale Running timeout".

### 1.2 Job Entry Points

| Source | How jobs are created | Job types |
|--------|----------------------|-----------|
| **EmailIngestionSchedulerService** | Adds `BackgroundJob` (JobType `EmailIngest`) per active email account on poll interval | EmailIngest |
| **PnlRebuildSchedulerService** | Adds `BackgroundJob` (JobType `pnlrebuild`) when none pending | pnlrebuild |
| **LedgerReconciliationSchedulerService** | Adds `BackgroundJob` (JobType `reconcileledgerbalancecache`) | reconcileledgerbalancecache |
| **StockSnapshotSchedulerService** | Adds `BackgroundJob` (JobType `populatestockbylocationsnapshots`) | populatestockbylocationsnapshots |
| **InventoryController** | Adds `BackgroundJob` (JobType `InventoryReportExport`) on API request | InventoryReportExport |
| **Other API / code** | Can add jobs directly to `BackgroundJobs` (e.g. document generation, notifications) | DocumentGeneration, NotificationSend, NotificationRetention, MyInvoisStatusPoll |

**In-process “jobs” (not in BackgroundJobs):**

- **PayoutAnomalyAlertSchedulerService**: Runs on a timer, records to `PayoutAnomalyAlertRuns` via `IAlertRunHistoryService`.
- **MissingPayoutSnapshotSchedulerService**: Runs on a timer, records to `PayoutSnapshotRepairRuns`.
- **EmailCleanupService**: BackgroundService; no separate run history table.

**Parser “jobs”**: Execution is **inside** the EmailIngest background job via `EmailIngestionService.IngestEmailsAsync`. Parse outcomes are stored in `ParseSession` (Status, ErrorMessage), not as separate job records.

### 1.3 Current BackgroundJob Entity and Table

- **Entity**: `CephasOps.Domain.Workflow.Entities.BackgroundJob`
- **Fields**: Id, JobType, PayloadJson, State (Queued, Running, Succeeded, Failed), RetryCount, MaxRetries, LastError, Priority, ScheduledAt, CreatedAt, StartedAt, CompletedAt, UpdatedAt.
- **Missing for observability**: CompanyId, TriggerSource, CorrelationId, Queue/Channel, PayloadSummary (safe), Worker/Node, ErrorCode, ErrorDetails (truncated), InitiatedByUserId, ParentJobRunId, RelatedEntityType/Id, and explicit Retrying/DeadLetter states.

### 1.4 Current Logging and Error Handling

- **Processor**: Logs at Info (job start/success) and Error (failure); sets `LastError = ex.Message` on failure.
- **Retries**: On failure, if `RetryCount < MaxRetries`, job is re-queued with `ScheduledAt = now + 2^RetryCount` minutes; otherwise it stays Failed.
- **No structured correlation ID**; no central error grouping or sanitization before persistence.

### 1.5 Existing Run History Tables (Non-Unified)

- **PayoutAnomalyAlertRuns**: StartedAt, CompletedAt, counts, TriggerSource (Scheduler/Manual).
- **PayoutSnapshotRepairRuns**: StartedAt, CompletedAt, counts, ErrorOrderIdsJson, TriggerSource, Notes.
- **ParseSession**: Status, ErrorMessage, etc. (parser outcome, not a “job run” record).

### 1.6 Where to Intercept Job Lifecycle

- **BackgroundJobProcessorService**: Single place where BackgroundJob rows are transitioned (Queued → Running → Succeeded/Failed). Ideal for **recording start, success, failure, retry** for all queue-based jobs.
- **Job creation sites**: When `BackgroundJobs.Add(job)` is called (schedulers, InventoryController, etc.), we can either add optional columns to BackgroundJob (CompanyId, TriggerSource, CorrelationId, InitiatedByUserId) or create an observability record there; the design below uses a **central JobRun** record created/updated from the processor and optionally from creation.
- **In-process schedulers**: Payout anomaly and Missing payout snapshot runs can be recorded via a shared **IJobRunRecorder** (or equivalent) so they appear in the same observability model.

### 1.7 Existing Abstractions

- **No common “job” abstraction** across BackgroundJob, PayoutAnomalyAlertRun, and PayoutSnapshotRepairRun.
- **BackgroundJobDto / CreateBackgroundJobDto** exist for API but there is no list/detail/retry API today.
- **BackgroundJobsController**: Only `GET .../health` and `GET .../summary`; no list, detail, or retry.

---

## 2. Gap Analysis

| Need | Current state | Gap |
|------|----------------|-----|
| Job run history | BackgroundJob is the run; no dedicated history table; no CompanyId, TriggerSource, CorrelationId | Add JobRun (or extend BackgroundJob) with full observability fields; optionally retain history separately |
| Job status tracking | State only on BackgroundJob; in-process jobs in other tables | Unified status in one place (JobRun) |
| Failure diagnostics | LastError only; no ErrorCode, truncated ErrorDetails, no grouping | Store sanitized error summary + optional grouping/signature |
| Retry failed jobs | No API or UI; manual DB/script only | Retry endpoint + UI with eligibility and audit |
| Metrics (counts, success rate, duration, stuck) | Summary counts in controller; no time-series or P95 | Dashboard metrics API (by day, job type, company); stuck detection |
| Admin UI | Summary cards + “recent jobs” (last 5 min); no list, filters, detail, retry | Full list, filters, detail drawer/page, retry button |
| Multi-company | BackgroundJob has no CompanyId | JobRun (or BackgroundJob) has CompanyId; filter/scope by company |
| Payout/Parser visibility | Separate tables; not in “jobs” UX | Optionally map those runs into JobRun or a unified “job runs” query that unions them |

---

## 3. Proposed Architecture

### 3.1 Principles

- **Single observability model**: Introduce a **JobRun** (or equivalent) entity that represents one execution of any background workload. BackgroundJob remains the **queue** for processor-driven jobs; JobRun is the **observability record** (created when we start a run, updated on completion).
- **Minimal coupling**: Recording is done via an **IJobRunRecorder** (or IJobObservabilityService). The processor and in-process schedulers call this interface; no binding to a specific scheduler implementation.
- **Gradual adoption**: Existing jobs get observability by:
  - **Phase 1**: Processor creates/updates a JobRun for each BackgroundJob it picks up (and we optionally backfill TriggerSource/CompanyId from payload when possible).
  - **Phase 2**: Job creation sites set optional fields on BackgroundJob (e.g. CorrelationId, TriggerSource, CompanyId) or we create a Pending JobRun when enqueueing.
  - **Phase 3**: In-process schedulers (payout anomaly, missing payout snapshot) call the same recorder so their runs appear in the same list/metrics.
- **Multi-company**: JobRun has nullable CompanyId; list and metrics APIs filter by company when the tenant context is set; SuperAdmin can see all.

### 3.2 High-Level Flow

1. **Queue-based jobs**  
   When a job is enqueued (schedulers or API), we can set optional fields on BackgroundJob (e.g. CorrelationId, TriggerSource, CompanyId from context). When the processor picks the job, it **creates a JobRun** (Pending → Running) and links it to the BackgroundJob (e.g. BackgroundJobId). On completion it updates JobRun (Succeeded/Failed), sets DurationMs, ErrorMessage, etc.

2. **In-process jobs**  
   When a scheduler (e.g. PayoutAnomalyAlert) starts, it calls **IJobRunRecorder.StartAsync**; when it finishes, **CompleteAsync** or **FailAsync**. No BackgroundJob row.

3. **Queries**  
   List, detail, failed list, dashboard metrics, and stuck jobs all read from **JobRun** (and optionally join BackgroundJob for retry).

4. **Retry**  
   For failed **queue-based** jobs, “Retry” means: ensure the corresponding BackgroundJob is back in Queued state (reset state, clear error, optionally bump RetryCount or create a new job and link via ParentJobRunId). For in-process-only runs, retry might mean “trigger a new run” via existing API; policy can mark some job types as non-retryable.

### 3.3 Entity/Model Design

#### 3.3.1 JobRun (new)

Central observability record for one execution.

| Field | Type | Purpose |
|-------|------|---------|
| Id | Guid | PK |
| CompanyId | Guid? | Tenant; null for global jobs |
| JobName | string | Display name (e.g. "Email Ingest", "P&L Rebuild") |
| JobType | string | Category/type (e.g. EmailIngest, pnlrebuild) |
| TriggerSource | string | Scheduler, Manual, System, Retry, Repair |
| CorrelationId | string? | For tracing and copy-to-clipboard |
| QueueOrChannel | string? | e.g. "BackgroundJobs" |
| PayloadSummary | string? | Safe summary (no secrets); truncated |
| Status | string | Pending, Running, Succeeded, Failed, Cancelled, Retrying, DeadLetter |
| StartedAtUtc | DateTime | When execution started |
| CompletedAtUtc | DateTime? | When finished |
| DurationMs | long? | CompletedAt - StartedAt |
| RetryCount | int | Attempt number for this logical run |
| WorkerNode | string? | Host/node name if available |
| ErrorCode | string? | Optional code for grouping |
| ErrorMessage | string? | Short message |
| ErrorDetails | string? | Sanitized/truncated (e.g. 2K chars) |
| InitiatedByUserId | Guid? | If manual/API |
| ParentJobRunId | Guid? | If this run was triggered by another (e.g. retry) |
| RelatedEntityType | string? | e.g. ParseSession, Order |
| RelatedEntityId | string? | ID of related entity |
| BackgroundJobId | Guid? | FK to BackgroundJob when this run is backed by a queue job |
| CreatedAtUtc | DateTime | Record creation |
| UpdatedAtUtc | DateTime | Last update |

Indexes: (StartedAtUtc DESC), (Status, StartedAtUtc), (JobType, StartedAtUtc), (CompanyId, StartedAtUtc), (BackgroundJobId) unique where not null, (CorrelationId) where not null.

#### 3.3.2 JobRunEvent (optional)

For fine-grained timeline (e.g. “Queued”, “Started”, “Completed”); can be added later. Not required for MVP.

#### 3.3.3 JobDefinition / JobRegistration (optional)

Metadata table (JobType, DisplayName, IsRetryable, ExpectedDurationMinutes, Category) for UI and policy. Can be code-driven initially and moved to DB later.

#### 3.3.4 BackgroundJob (existing, optional extensions)

Optional columns to support observability without duplicating data: CorrelationId, TriggerSource, CompanyId, InitiatedByUserId. If we always create a JobRun when the processor starts a job, we can derive these from payload/context and store only in JobRun.

### 3.4 Lifecycle Flow (Queue-Based)

1. **Enqueue** (scheduler or API): Insert BackgroundJob (and optionally set CorrelationId, TriggerSource, CompanyId if we add columns).
2. **Processor picks job**: Create JobRun (Status = Running, StartedAtUtc = now, BackgroundJobId = job.Id, JobType/JobName from job, CompanyId from job or payload). Optionally update BackgroundJob with CorrelationId if we generate one.
3. **Success**: Update JobRun (Status = Succeeded, CompletedAtUtc, DurationMs); BackgroundJob already updated by processor.
4. **Failure**: Update JobRun (Status = Failed, ErrorMessage, ErrorDetails sanitized, CompletedAtUtc, DurationMs). If job will retry: Status = Retrying or leave Failed and next attempt creates new JobRun with ParentJobRunId.
5. **Stale reaper**: When processor marks job Failed for timeout, update corresponding JobRun to Failed with appropriate message.
6. **Retry (manual)**: User clicks Retry; API loads JobRun and BackgroundJob; if retryable, reset BackgroundJob to Queued (or create new BackgroundJob) and optionally create a new JobRun (ParentJobRunId = original) in Pending.

### 3.5 Retry Strategy

- **Eligibility**: Only job types in an allow-list (e.g. EmailIngest, pnlrebuild, reconcileledgerbalancecache, populatestockbylocationsnapshots, InventoryReportExport) are retryable. DocumentGeneration, NotificationSend, etc. can be marked non-retryable to avoid duplicate side effects.
- **Idempotency**: Document in design that retry re-queues the same or a new job; handlers must be idempotent where possible.
- **Audit**: Retry action recorded (e.g. audit log or JobRun with TriggerSource = Retry, InitiatedByUserId).
- **Duplicate prevention**: If “Retry” creates a new BackgroundJob, link it to the previous JobRun via ParentJobRunId; optionally prevent multiple retries within a short window for the same JobRun.

### 3.6 Security and Privacy

- **Payload**: Do not store raw PayloadJson in JobRun; store only a **PayloadSummary** (e.g. keys and non-sensitive values, truncated).
- **ErrorDetails**: Sanitize (remove connection strings, tokens, PII) and truncate (e.g. 2000 chars) before persisting.
- **Authorization**: List/detail/metrics/retry APIs require JobsView/JobsRun as today; list filtered by CompanyId when user is scoped to a company; SuperAdmin sees all.
- **Secrets**: Never log or store full payload or stack traces with secrets; use a shared sanitizer.

### 3.7 Multi-Company

- JobRun.CompanyId nullable; set from BackgroundJob payload (e.g. companyId) or from current user context when job is enqueued.
- List and dashboard APIs: when user has a company scope, filter by CompanyId; SuperAdmin gets no company filter.
- “Top failing companies” metric: group by CompanyId.

### 3.8 UI Proposal

- **Dashboard**: Cards for total runs (e.g. last 24h), success rate, failed count, running count, stuck count (running > expected duration); optional small chart (e.g. success/fail by day).
- **Tables**: “Recent runs”, “Failed runs”, “Running” with columns: Job name/type, status, started, duration, company, trigger, correlation ID (with copy), error summary. Filters: date range, company, job type, status, trigger source, correlation ID.
- **Detail**: Drawer or page for one JobRun: all fields, link to BackgroundJob if any, link to related entity (e.g. ParseSession, Order) when RelatedEntityType/Id set; “Retry” button when allowed.
- **Retry**: Button on detail and on failed row; confirm dialog; call retry API; refresh list.

---

## 4. Rollout Plan

| Phase | Scope | Deliverables |
|-------|--------|--------------|
| **1** | Audit & design | This document |
| **2** | Domain & persistence | JobRun entity, EF config, migration, indexes; optional JobRunEvent/JobDefinition later |
| **3** | Lifecycle instrumentation | IJobRunRecorder + implementation; call from BackgroundJobProcessorService (create/update JobRun); optional TriggerSource/CompanyId on enqueue |
| **4** | Query/API layer | List jobs, list failed, get detail, retry, dashboard metrics, stuck jobs; all with auth and company filter |
| **5** | Admin UI | Dashboard, recent/failed/running tables, filters, detail drawer, retry button |
| **6** | Hardening | Sanitization, duplicate retry protection, retention proposal, tests |

---

## 5. Retention Strategy (Phase 6)

- **JobRuns**: Recommend retaining full detail for 90 days; optionally archive or aggregate older rows (e.g. daily rollup by JobType/Status) for long-term metrics. No automatic purge in MVP; add a scheduled job or admin action later to delete or archive rows older than N days.
- **BackgroundJobs**: Existing table; consider archiving or deleting Succeeded/Failed rows older than 90 days to keep the queue table small, or leave as-is if volume is low.
- **Indexes**: Existing indexes on `StartedAtUtc`, `Status`, `JobType`, `CompanyId` support both recent queries and date-range cleanup.

---

## 6. Summary

- **Current state**: Single queue table (BackgroundJobs), one poll-based processor, several schedulers (some enqueue, two run in-process with their own run tables), no unified observability or retry.
- **Proposed state**: Add **JobRun** as the central observability record; **IJobRunRecorder** used by the processor (and optionally by in-process schedulers); extend APIs and UI for list, detail, metrics, and safe retry; keep BackgroundJob as the queue for processor-driven jobs.
- **Outcome**: Operations get one place to see what ran, what failed, what’s stuck, and to safely retry failed jobs, with multi-company support and without tying the solution to a specific scheduler implementation.

---

## 7. Rollout Summary (Implementation Complete)

### What was implemented

- **Phase 1**: Audit and design document at `docs/JOB_OBSERVABILITY_DESIGN.md`.
- **Phase 2**: `JobRun` entity, `JobRunConfiguration`, `DbSet<JobRun>`, migration `20260309100000_AddJobRunsTable.cs`, and snapshot updated.
- **Phase 3**: `IJobRunRecorder` and `JobRunRecorder` in `Application/Workflow/JobObservability`; payload summary and error sanitization; `BackgroundJobProcessorService` records start/success/failure/cancel for each processed job; DI registration in `Program.cs`.
- **Phase 4**: APIs on `BackgroundJobsController`: `GET job-runs` (list with filters), `GET job-runs/failed`, `GET job-runs/running`, `GET job-runs/{id}`, `GET job-runs/dashboard`, `GET job-runs/stuck`, `POST job-runs/{id}/retry`. Company filter and `JobsView`/`JobsRun` permissions applied.
- **Phase 5**: Admin UI on `BackgroundJobsPage`: Overview tab with dashboard cards and recent failures; Failed / Running / Recent runs tabs with tables; detail drawer with correlation ID copy and retry button; permission-aware retry.
- **Phase 6**: Retention strategy noted in design doc; error/payload sanitization in recorder; duplicate retry prevented (only Failed/DeadLetter with associated BackgroundJob); unit tests in `JobRunRecorderTests.cs` (4 tests).

### Assumptions

- Only queue-based jobs (processed by `BackgroundJobProcessorService`) are recorded to `JobRuns` in this phase. In-process schedulers (PayoutAnomalyAlert, MissingPayoutSnapshot) are not yet recorded to `JobRuns`; they can be added later via the same `IJobRunRecorder`.
- Trigger source for all processor jobs is set to `"Scheduler"`; manual/API-triggered jobs could set TriggerSource at enqueue time in a follow-up (e.g. when adding optional columns to `BackgroundJob` or when creating a pending JobRun at enqueue).
- Retry re-queues the same `BackgroundJob` row; the next processor cycle creates a new `JobRun` row (no `ParentJobRunId` set in this phase).

### How to deploy

1. Apply migration: `dotnet ef database update` (or run the generated SQL from `AddJobRunsTable`) against the target database.
2. Deploy API and frontend; ensure `IJobRunRecorder` is registered (already in `Program.cs`).
3. New job executions will create `JobRuns` automatically; no backfill of historical runs.

### Risks and follow-up

- **Volume**: If job volume is very high, consider indexing and retention (e.g. purge/archive after 90 days) and monitor `JobRuns` table size.
- **In-process jobs**: To include Payout Anomaly and Missing Payout Snapshot runs in the same UI, call `IJobRunRecorder.StartAsync`/`CompleteAsync`/`FailAsync` from those schedulers.
- **ParentJobRunId**: To link retries to the original run, set `ParentJobRunId` when the processor starts a job that was retried (e.g. via a flag or column on `BackgroundJob`).
