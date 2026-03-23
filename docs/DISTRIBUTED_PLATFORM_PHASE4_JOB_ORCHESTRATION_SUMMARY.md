# CephasOps Distributed Ops Platform — Phase 4 Job Orchestration Hardening Summary

**Date:** 2026-03-10

---

## A. Execution landscape audited

- **Audit doc:** `docs/DISTRIBUTED_PLATFORM_PHASE4_JOB_ORCHESTRATION_AUDIT.md`
- **BackgroundJob producers:** LedgerReconciliation, SlaEvaluation, StockSnapshot schedulers; EmailIngestion; InventoryController; ReplayJobEnqueuer; RebuildJobEnqueuer; OrderAssignedOperationsHandler; AsyncEventEnqueuer.
- **BackgroundJobProcessorService** handled 14 job types; 4 migrated to JobExecution (pnlrebuild in Phase 3; reconcileledgerbalancecache, slaevaluation, populatestockbylocationsnapshots in Phase 4).
- **Risks identified:** Stale lease on JobExecution (no recovery), no operational queryability, lifecycle not formally defined.

---

## B. JobExecution lifecycle hardened

- **Domain:** `JobExecutionStatus` constants: Pending, Running, Succeeded, Failed, DeadLetter.
- **Transitions:** Pending → (claim) → Running → Succeeded | Failed | DeadLetter. Failed + NextRunAtUtc set = retry scheduled; when NextRunAtUtc ≤ now, job is eligible for claim again (semantically Pending). DeadLetter = terminal.
- **Store/worker** use `JobExecutionStatus` for Status values; retry and dead-letter logic in `MarkFailedAsync` (AttemptCount, MaxAttempts, NextRunAtUtc, LastError) unchanged and explicit.

---

## C. Stale lease recovery implemented

- **IJobExecutionStore.ResetStuckRunningAsync(TimeSpan leaseExpiry):** Resets Running jobs to Pending when (1) ProcessingLeaseExpiresAtUtc &lt; now, or (2) ProcessingLeaseExpiresAtUtc is null and StartedAtUtc is older than leaseExpiry. Does not increment AttemptCount. Returns count reset.
- **JobExecutionStore** (Infrastructure): Implementation using EF; safe for InMemory in tests.
- **JobExecutionWorkerHostedService:** At start of each poll loop, calls `ResetStuckRunningAsync(TimeSpan.FromSeconds(leaseSeconds * 2))` and logs when count &gt; 0.

---

## D. Retry / dead-letter behavior implemented

- **MarkFailedAsync:** Increments AttemptCount, sets LastError (truncated 2000), LastErrorAtUtc, clears lease. If isNonRetryable or AttemptCount ≥ MaxAttempts → Status = DeadLetter, CompletedAtUtc set, NextRunAtUtc cleared. Otherwise Status = Failed, NextRunAtUtc = now + backoff (60s, 300s, 900s, 3600s by attempt).
- **Visibility:** AttemptCount, LastError, LastErrorAtUtc, NextRunAtUtc on JobExecution; no separate attempt-history table (minimal approach). Query service exposes these for operational views.

---

## E. Operational queryability added

- **IJobExecutionQueryService** (Application): GetSummaryAsync (counts by status), GetPendingAsync, GetRunningAsync, GetFailedRetryScheduledAsync, GetDeadLetterAsync (list with limit).
- **JobExecutionSummaryDto,** **JobExecutionListItemDto:** Id, JobType, Status, AttemptCount, MaxAttempts, NextRunAtUtc, timestamps, LastError, CompanyId, ProcessingNodeId, ProcessingLeaseExpiresAtUtc.
- **JobOrchestrationController** (Api): GET api/job-orchestration/summary, pending, running, failed-retry-scheduled, dead-letter (JobsAdmin permission).

---

## F. Additional job types migrated

1. **ReconcileLedgerBalanceCache** — Executor: `ReconcileLedgerBalanceCacheJobExecutor` (calls IStockLedgerService.ReconcileBalanceCacheAsync). Scheduler: LedgerReconciliationSchedulerService now enqueues via IJobExecutionEnqueuer and checks JobExecutions for pending.
2. **SlaEvaluation** — Executor: `SlaEvaluationJobExecutor` (calls ISlaEvaluationService.EvaluateAsync, optional companyId from payload). Scheduler: SlaEvaluationSchedulerService now enqueues via IJobExecutionEnqueuer.
3. **PopulateStockByLocationSnapshots** — Executor: `PopulateStockByLocationSnapshotsJobExecutor` (payload: periodEndDate, snapshotType, companyId; calls IStockLedgerService.EnsureSnapshotsForPeriodAsync). Scheduler: StockSnapshotSchedulerService now enqueues via IJobExecutionEnqueuer.

---

## G. BackgroundJob responsibility reduced

- **BackgroundJobProcessorService:** Switch now handles only legacy types: emailingest, notificationsend, notificationretention, documentgeneration, myinvoisstatuspoll, inventoryreportexport, eventhandlingasync, operationalreplay, operationalrebuild. For pnlrebuild, reconcileledgerbalancecache, slaevaluation, populatestockbylocationsnapshots the processor throws NotSupportedException with message "migrated to JobExecution; use IJobExecutionEnqueuer."
- **Class-level comment** documents migrated types and legacy-only list.

---

## H. Tests added

- **JobExecutionStoreResetStuckTests:** ResetStuckRunningAsync resets expired-lease job to Pending; leaves active-lease job unchanged.
- **JobExecutionQueryServiceTests:** GetSummaryAsync counts by status; GetDeadLetterAsync returns only DeadLetter with LastError.
- **JobExecutionEnqueuerTests** (Phase 3): EnqueueAsync adds Pending job with correct JobType/payload/CompanyId; null payload becomes "{}".
- All run with InMemory DbContext; test project references Infrastructure for ApplicationDbContext and JobExecutionStore.

---

## I. Migrations added

- No new migration in Phase 4. JobExecutions schema unchanged; lifecycle and behavior are code/doc only.

---

## J. Remaining orchestration debt

- **BackgroundJob** still used for: emailingest, notificationsend, notificationretention, documentgeneration, myinvoisstatuspoll, inventoryreportexport, eventhandlingasync, operationalreplay, operationalrebuild. Migrate incrementally when retry-safe and low-coupling.
- **Job lifecycle events** (ops.job.started.v1, etc.) append to IEventStore with Source = "JobOrchestration"; optional Phase 8 envelope (PartitionKey, SourceService) can be added later.
- **Dead-letter reset:** No API yet to reset a DeadLetter job to Pending for manual retry; can be added when needed.

---

## K. Recommended Phase 5 extraction candidate

- **Notification retention** or **notification send** (email) — already event/notification-boundary; moving to JobExecution would align with NotificationDispatch and reduce BackgroundJob surface. Alternatively: **documentgeneration** (high value, heavier payload) once idempotency and company scoping are clear.

---

## L. Files created or updated

**Created**
- `docs/DISTRIBUTED_PLATFORM_PHASE4_JOB_ORCHESTRATION_AUDIT.md`
- `docs/DISTRIBUTED_PLATFORM_PHASE4_JOB_ORCHESTRATION_SUMMARY.md`
- `Domain/Workflow/JobExecutionStatus.cs`
- `Application/Workflow/JobOrchestration/JobExecutionQueryDto.cs`
- `Application/Workflow/JobOrchestration/IJobExecutionQueryService.cs`
- `Application/Workflow/JobOrchestration/JobExecutionQueryService.cs`
- `Application/Workflow/JobOrchestration/Executors/ReconcileLedgerBalanceCacheJobExecutor.cs`
- `Application/Workflow/JobOrchestration/Executors/SlaEvaluationJobExecutor.cs`
- `Application/Workflow/JobOrchestration/Executors/PopulateStockByLocationSnapshotsJobExecutor.cs`
- `Api/Controllers/JobOrchestrationController.cs`
- `tests/.../JobOrchestration/JobExecutionStoreResetStuckTests.cs`
- `tests/.../JobOrchestration/JobExecutionQueryServiceTests.cs`

**Updated**
- `Domain/Workflow/IJobExecutionStore.cs` (ResetStuckRunningAsync)
- `Infrastructure/Persistence/JobExecutionStore.cs` (MarkFailedAsync status constants, ResetStuckRunningAsync implementation)
- `Application/Workflow/JobOrchestration/JobExecutionEnqueuer.cs` (JobExecutionStatus.Pending)
- `Application/Workflow/JobOrchestration/JobExecutionWorkerHostedService.cs` (stale-lease reset in loop)
- `Application/Workflow/Services/LedgerReconciliationSchedulerService.cs` (JobExecution pipeline)
- `Application/Workflow/Services/SlaEvaluationSchedulerService.cs` (JobExecution pipeline)
- `Application/Workflow/Services/StockSnapshotSchedulerService.cs` (JobExecution pipeline)
- `Application/Workflow/Services/BackgroundJobProcessorService.cs` (legacy-only switch, migrated-type NotSupportedException)
- `Api/Program.cs` (Phase 4 executors and IJobExecutionQueryService)
- `tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj` (explicit Application + Infrastructure references)
