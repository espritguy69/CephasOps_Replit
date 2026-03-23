# Job Observability — Phase 2 Summary

**Date:** 2026-03-09  
**Scope:** Stabilization and extension of Job Observability to production-grade reliability after Phase 1 verification.

---

## 1. What Was Improved

### 1.1 Retry Chain (Phase 2)

- **BackgroundJob** now has nullable **RetriedFromJobRunId**. When a run is retried from the UI, the re-queued job stores the JobRun id being retried.
- **BackgroundJobProcessorService** sets **ParentJobRunId = job.RetriedFromJobRunId** and **TriggerSource = "Retry"** when starting a run for a retried job.
- **JobRuns** has an index on **ParentJobRunId** for efficient “retry chain” queries.
- Result: Retry flow is **Original Run → Retry Run → Retry Run** with each new run linked to the original via ParentJobRunId.

### 1.2 JobDefinition Metadata (Phase 3)

- **JobDefinition** entity and **JobDefinitions** table: JobType, DisplayName, RetryAllowed, MaxRetries, DefaultStuckThresholdSeconds.
- **IJobDefinitionProvider** / **JobDefinitionProvider**: resolve definition by job type from DB with fallback to built-in defaults for known types (EmailIngest, pnlrebuild, reconcileledgerbalancecache, populatestockbylocationsnapshots, PayoutAnomalyAlert, MissingPayoutSnapshotRepair, scheduler cycles, etc.).
- **Retry validation** uses definition.RetryAllowed instead of a hardcoded allow-list.
- **Display names** for runs come from JobDefinition when available.
- **Stuck detection** (Phase 5) uses **JobDefinition.DefaultStuckThresholdSeconds** per job type with a global fallback.

### 1.3 Scheduler Coverage (Phase 4)

All in-process schedulers now record JobRuns via **IJobRunRecorder** (StartAsync → CompleteAsync / FailAsync):

- **PayoutAnomalyAlertSchedulerService** — JobType `PayoutAnomalyAlert`
- **MissingPayoutSnapshotSchedulerService** — JobType `MissingPayoutSnapshotRepair`
- **EmailIngestionSchedulerService** — JobType `EmailIngestionScheduler` (cycle)
- **StockSnapshotSchedulerService** — JobType `StockSnapshotScheduler` (cycle)
- **LedgerReconciliationSchedulerService** — JobType `LedgerReconciliationScheduler` (cycle)
- **PnlRebuildSchedulerService** — JobType `PnlRebuildScheduler` (cycle)

### 1.4 Stuck Detection (Phase 5)

- Stuck logic uses **per–job-type** threshold: **JobDefinition.DefaultStuckThresholdSeconds** when present, else global fallback (default 2 hours).
- **GET /api/background-jobs/job-runs/stuck** returns runs considered stuck under this rule; optional query **olderThanHours** is used as fallback seconds when no definition exists.
- **GET /api/background-jobs/job-runs/stuck-thresholds** exposes global fallback and per–job-type effective thresholds (JobsView).
- Stuck runs in the response include **effectiveStuckThresholdSeconds** for transparency.

### 1.5 Dashboard Metrics (Phase 6)

- **P95 duration** (last 24h, completed runs, sample capped for performance).
- **Jobs per hour** (last 24h).
- **Retry rate** (last 24h: runs with RetryCount > 0).
- **Top failing companies** (last 24h, company-scoped).
- **Top failing job types** (last 24h).
- Queries use existing indexes (e.g. JobType, CompanyId, StartedAtUtc).

### 1.6 Retention & Purge (Phase 7)

- **IJobRunRetentionService** / **JobRunRetentionService**: purge completed runs older than a given UTC time in configurable batches (max batch size 10,000).
- **POST /api/background-jobs/job-runs/purge**: body `{ "olderThanDays": 90, "batchSize": 1000 }` (optional). Requires **Jobs.Admin**.
- **PermissionCatalog**: new **JobsAdmin** (`jobs.admin`); added to Jobs module and All list.

### 1.7 UI Improvements (Phase 8)

- **Stuck tab**: lists stuck runs with threshold column and “Stuck” badge.
- **Running tab**: “Stuck” badge on runs that appear in the stuck list.
- **Recent tab**: quick filters — 24h, 7 days, Failed only, All.
- **Detail drawer**: retry chain link (“Retry of: &lt;parent run id&gt;” opening parent run); timeline (Started → Completed / Running).
- **Dashboard**: optional cards for P95 duration, jobs/hour, retry rate when API returns them.

### 1.8 Hardening (Phase 9)

- **Pagination**: list job-runs page size clamped to **MaxPageSize = 100**.
- **Failed list**: limit clamped to 1–500.
- **Running list**: capped at 500 runs.
- Authorization (RequirePermission), company scoping (ApplyCompanyFilter), retry safety (definition.RetryAllowed), and error sanitization (JobRunRecorder) were already in place; no regressions.

### 1.9 Tests (Phase 10)

- **JobRunRecorderTests**: StartAsync with ParentJobRunId sets ParentJobRunId and TriggerSource.
- **JobDefinitionProviderTests**: GetByJobTypeAsync returns default when not in DB; returns from DB when exists; GetByJobTypeAsync null for unknown type; GetAllAsync includes DB and defaults.
- **JobRunRetentionServiceTests**: PurgeAsync deletes only completed runs older than cutoff; respects batch size.

---

## 2. New Architecture Elements

| Element | Purpose |
|--------|---------|
| **JobDefinition** | Metadata per job type: display name, retry policy, stuck threshold. |
| **IJobDefinitionProvider** | Resolve definition by job type (DB + built-in defaults). |
| **RetriedFromJobRunId** (BackgroundJob) | Link retried queue job to the JobRun id that was retried. |
| **ParentJobRunId** (JobRun) | Link a run to its parent (e.g. retry chain). |
| **IJobRunRetentionService** | Batch purge of old completed runs. |
| **Jobs.Admin** | Permission for purge and future admin-only job operations. |
| **Stuck thresholds API** | Expose effective stuck thresholds for operators and UI. |

---

## 3. Operational Usage

- **Dashboard**: Use Overview for runs (24h), success rate, running/queued, failed/stuck, and optional P95, jobs/h, retry rate and top failing companies/job types.
- **Stuck jobs**: Open Stuck tab or GET `/api/background-jobs/job-runs/stuck`; adjust per-type thresholds via JobDefinitions or use `olderThanHours` for fallback.
- **Thresholds**: GET `/api/background-jobs/job-runs/stuck-thresholds` to see global and per–job-type effective thresholds.
- **Retry**: Use Retry on failed runs (only when job type has RetryAllowed). New run will have ParentJobRunId set; view chain in detail drawer.
- **Retention**: Call POST `/api/background-jobs/job-runs/purge` with `olderThanDays` (and optional `batchSize`) as needed; requires Jobs.Admin.

---

## 4. Migration Notes

- **Database**: Apply migrations that add **RetriedFromJobRunId** to BackgroundJobs, **ParentJobRunId** index on JobRuns, and **JobDefinitions** table (and any snapshot updates). No breaking changes to existing JobRuns data.
- **Permissions**: Seed or assign **jobs.admin** to roles that should run purge.
- **Frontend**: Ensure API client and types include new dashboard fields, stuck thresholds, and purge if you add a purge button. Existing endpoints remain compatible.

---

## 5. Success Criteria Met

- Complete run history (processor + all in-process schedulers).
- Retry chains (ParentJobRunId + RetriedFromJobRunId).
- Scheduler coverage (all listed schedulers record JobRuns).
- Stuck job detection with per-type thresholds and API exposure.
- Operational metrics (P95, jobs/h, retry rate, top failing companies/job types).
- Retention tooling (purge API and Jobs.Admin).

The job system is now fully observable and operationally manageable within the scope of Phase 2.
