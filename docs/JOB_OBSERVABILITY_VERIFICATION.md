# Job Observability — Phase 1 Verification Report

**Date:** 2026-03-09  
**Scope:** Audit of implemented Job Observability system; no code changes.

---

## 1. What Is Correctly Implemented

### 1.1 Domain & Persistence

| Item | Status | Location |
|------|--------|----------|
| **JobRun entity** | ✅ Present | `CephasOps.Domain/Workflow/Entities/JobRun.cs` |
| **JobRun fields** | ✅ All required fields exist: Id, CompanyId, JobName, JobType, TriggerSource, CorrelationId, QueueOrChannel, PayloadSummary, Status, StartedAtUtc, CompletedAtUtc, DurationMs, RetryCount, WorkerNode, ErrorCode, ErrorMessage, ErrorDetails, InitiatedByUserId, **ParentJobRunId**, RelatedEntityType, RelatedEntityId, BackgroundJobId, CreatedAtUtc, UpdatedAtUtc |
| **JobRunConfiguration** | ✅ Present | `CephasOps.Infrastructure/Persistence/Configurations/Workflow/JobRunConfiguration.cs` |
| **Table name** | ✅ `JobRuns` | Configuration |
| **Indexes** | ✅ StartedAtUtc; Status+StartedAtUtc; JobType+StartedAtUtc; CompanyId+StartedAtUtc; BackgroundJobId (filtered); CorrelationId (filtered) |
| **AddJobRunsTable migration** | ✅ Present | `20260309100000_AddJobRunsTable.cs` — creates table and all columns including ParentJobRunId |
| **ApplicationDbContext** | ✅ `DbSet<JobRun> JobRuns` | Registered |
| **Model snapshot** | ✅ JobRun entity in snapshot | ApplicationDbContextModelSnapshot.cs |

### 1.2 Lifecycle Instrumentation

| Item | Status | Notes |
|------|--------|-------|
| **IJobRunRecorder** | ✅ Present | StartAsync, CompleteAsync, FailAsync, CancelAsync |
| **StartJobRunDto** | ✅ Has ParentJobRunId | Used when starting a run |
| **JobRunRecorder** | ✅ Implements interface | Payload summary + error sanitization |
| **BackgroundJobProcessorService** | ✅ Instrumented | Resolves recorder from scope; calls StartAsync (no ParentJobRunId set); CompleteAsync on success; FailAsync on failure; CancelAsync on cancellation |
| **DI** | ✅ Registered | `IJobRunRecorder` / `JobRunRecorder` in Program.cs (Scoped) |

### 1.3 API Endpoints

| Endpoint | Method | Status | Permission |
|----------|--------|--------|------------|
| List job runs | GET `/api/background-jobs/job-runs` | ✅ | JobsView |
| Failed runs | GET `/api/background-jobs/job-runs/failed` | ✅ | JobsView |
| Running runs | GET `/api/background-jobs/job-runs/running` | ✅ | JobsView |
| Job run by id | GET `/api/background-jobs/job-runs/{id}` | ✅ | JobsView |
| Dashboard | GET `/api/background-jobs/job-runs/dashboard` | ✅ | JobsView |
| Stuck runs | GET `/api/background-jobs/job-runs/stuck` | ✅ | JobsView |
| Retry | POST `/api/background-jobs/job-runs/{id}/retry` | ✅ | JobsRun |

- List supports: fromUtc, toUtc, companyId, jobType, status, triggerSource, correlationId, page, pageSize.
- Company filter applied for non–SuperAdmin users.
- Retry validates: run exists, status Failed/DeadLetter, BackgroundJobId present, job type in allow-list, BackgroundJob in Failed state.

### 1.4 Frontend

| Item | Status | Notes |
|------|--------|-------|
| **Background Jobs admin page** | ✅ | `frontend/src/pages/admin/BackgroundJobsPage.tsx` |
| **Dashboard cards** | ✅ | Overview tab: Runs (24h), Success rate, Running/Queued, Failed/Stuck |
| **Tabs** | ✅ | Overview, Failed, Running, Recent runs |
| **Failed / Running / Recent tables** | ✅ | Columns: Job, Status, Started, Duration, Error; Failed has Retry button |
| **Job detail drawer** | ✅ | Opens on row click; shows full details, correlation ID with copy, Retry when canRetry and jobs.run |
| **Retry button** | ✅ | On failed rows and in detail drawer; calls retry API, refreshes data |
| **API client** | ✅ | `getJobRunsDashboard`, `listJobRuns`, `listFailedJobRuns`, `listRunningJobRuns`, `getJobRun`, `listStuckJobRuns`, `retryJobRun` in `api/backgroundJobs.ts` |
| **JobRunDto** | ✅ | Includes parentJobRunId, canRetry |

---

## 2. Deviations From Design

1. **TriggerSource**  
   Design allows Scheduler / Manual / System / Retry / Repair. Implementation always sets `TriggerSource = "Scheduler"` when the processor starts a run; manual/API-triggered jobs are not distinguished (no field on BackgroundJob).

2. **Retry chain not linked**  
   Design: “Record retried job as a new run linked to original” via ParentJobRunId.  
   - **JobRun** and **StartJobRunDto** have ParentJobRunId.  
   - **Processor** does **not** set ParentJobRunId when starting a run (no way to know “this BackgroundJob was retried from JobRun X”).  
   - **Retry endpoint** re-queues the same BackgroundJob but does not store the original JobRun id on the job or in payload.  
   So retry creates a new execution and a new JobRun, but the new JobRun has **ParentJobRunId = null**. Retry chains are not recorded.

3. **Index on ParentJobRunId**  
   Design implies querying by parent for “retry chain” views. There is **no index on JobRuns.ParentJobRunId** in configuration or migration.

4. **Stuck threshold**  
   Stuck endpoint uses a single query parameter `olderThanHours` (default 2). Design mentions per–job-type thresholds (e.g. EmailIngest 10 min, others 120 min); no JobDefinition or per-type threshold is used yet.

5. **Jobs.Admin**  
   Design doc does not define Jobs.Admin; purge/retention is not implemented. Permission catalog has JobsView and JobsRun only.

---

## 3. Missing Lifecycle Coverage

1. **In-process schedulers (no JobRun recording)**  
   - **EmailIngestionSchedulerService** — only enqueues BackgroundJobs; no JobRun for “scheduler cycle” itself.  
   - **StockSnapshotSchedulerService**, **LedgerReconciliationSchedulerService**, **PnlRebuildSchedulerService** — same (enqueue only).  
   - **PayoutAnomalyAlertSchedulerService** — runs in-process, writes to `PayoutAnomalyAlertRuns`; does **not** call IJobRunRecorder.  
   - **MissingPayoutSnapshotSchedulerService** — runs in-process, writes to `PayoutSnapshotRepairRuns`; does **not** call IJobRunRecorder.  
   - **EmailCleanupService** — BackgroundService; no JobRun or other run history.  
   So: only runs that go through **BackgroundJobProcessorService** (queue-based jobs) get JobRuns. In-process scheduled work is not in the observability model.

2. **Queued / Pending**  
   JobRun is created when the processor **starts** a job (Status = Running). There is no “Pending” or “Queued” JobRun when a job is first enqueued; history begins at start.

3. **Retry flow**  
   When a job fails and is re-queued by the processor (automatic retry), the **next** run gets a new JobRun with no ParentJobRunId, so automatic retries are also not linked into chains.

---

## 4. Architecture Weaknesses

1. **No JobDefinition layer**  
   Display names and retry allow-list are hardcoded (e.g. `GetJobDisplayName`, `RetryableJobTypes` in controller). Stuck threshold is global. No central metadata for JobType (DisplayName, RetryAllowed, MaxRetries, DefaultStuckThresholdSeconds).

2. **Retry chain requires BackgroundJob extension**  
   To link retry → original run, either:  
   - Add something like `RetriedFromJobRunId` (or equivalent) to **BackgroundJob** and set it when the user clicks Retry; processor then passes it as ParentJobRunId when starting the run, or  
   - Encode the original JobRun id in PayloadJson and have the processor read it and set ParentJobRunId.  
   Current design does neither.

3. **Dashboard metrics**  
   Dashboard returns total/succeeded/failed last 24h, success rate, running/stuck/queued counts, by-job-type counts and avg duration, and recent failures. No P95 duration, jobs-per-hour, retry rate, or “top failing companies / job types” aggregates. Queries are straightforward but may need indexing for large JobRuns tables.

4. **No retention/purge**  
   No retention policy or purge endpoint; JobRuns can grow unbounded.

5. **Pagination**  
   List endpoint has page/pageSize (default 50); no explicit maximum pageSize (e.g. cap at 100) is enforced in the controller.

6. **Error sanitization**  
   JobRunRecorder sanitizes error details (e.g. password/token redaction) and truncates; implementation is present and reasonable.

7. **Company scoping**  
   ApplyCompanyFilter is used on all job-run queries when user is not SuperAdmin; consistent with multi-tenant design.

---

## 5. Summary

| Area | Verdict |
|------|---------|
| **JobRun entity & persistence** | Correctly implemented; ParentJobRunId exists but is never set on new runs. |
| **Recorder & processor instrumentation** | Correct for queue-based jobs; ParentJobRunId not set; in-process schedulers not instrumented. |
| **APIs** | All seven endpoints exist and behave as specified; list/dashboard/stuck use company filter. |
| **Frontend** | Dashboard, tabs, tables, detail drawer, retry button and API calls are in place. |
| **Gaps** | Retry chain linking, index on ParentJobRunId, JobDefinition layer, in-process scheduler coverage, per-type stuck thresholds, retention/purge, extended metrics, and UI enhancements (retry chain, quick filters, timeline) are missing or partial. |

No code was modified during this verification.
