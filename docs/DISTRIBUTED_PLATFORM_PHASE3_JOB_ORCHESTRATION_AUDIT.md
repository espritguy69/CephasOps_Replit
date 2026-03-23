# Phase 3 Job Orchestration — Audit

**Date:** 2026-03-09

---

## Current system

### BackgroundJob (Domain.Workflow.Entities)

- **Table:** BackgroundJobs. **Fields:** Id, JobType, PayloadJson, State (Queued/Running/Succeeded/Failed), RetryCount, MaxRetries, LastError, Priority, ScheduledAt, CreatedAt, StartedAt, CompletedAt, UpdatedAt, RetriedFromJobRunId, WorkerId, ClaimedAtUtc.
- **Missing for Phase 3:** CompanyId, CorrelationId, CausationId, NextRunAtUtc (retry uses ScheduledAt today), LeaseExpiresAtUtc (no lease expiry; claim is by WorkerId).

### JobPollingCoordinatorService (BackgroundService)

- Polls every PollIntervalSeconds; discovers runnable jobs: State=Queued, (ScheduledAt==null or ScheduledAt<=now), (WorkerId==null or owner inactive). Orders by Priority, CreatedAt; takes MaxJobsPerPoll. Claims via IWorkerCoordinator.TryClaimBackgroundJobAsync(workerId, jobId). Does not process; BackgroundJobProcessorService picks up jobs already claimed by this worker.

### BackgroundJobProcessorService (BackgroundService)

- **Responsibilities (mixed):** (1) Reap stale Running jobs (timeout by JobType). (2) Load jobs: Running + WorkerId=me, and Queued unclaimed (legacy or overflow). (3) For each job: set State=Running, StartedAt, record JobRun (IJobRunRecorder), **dispatch by JobType** (large switch: EmailIngest, PnlRebuild, NotificationSend, NotificationRetention, DocumentGeneration, MyInvoisStatusPoll, InventoryReportExport, ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots, EventHandlingAsync, SlaEvaluation, OperationalReplay, OperationalRebuild). (4) On success: State=Succeeded, CompletedAt, recorder.CompleteAsync. (5) On failure: State=Failed, RetryCount++, ScheduledAt=now+backoff, or leave Failed if MaxRetries exceeded; recorder.FailAsync.
- **Retry:** Exponential backoff (2^retryCount minutes); retry by re-queuing (State=Queued, ScheduledAt set). No NextRunAtUtc column; uses ScheduledAt.
- **No job lifecycle events** emitted to event store (ops.job.started/completed/failed).

### Job creation (direct Add to BackgroundJobs)

- **Schedulers:** EmailIngestionSchedulerService, PnlRebuildSchedulerService, LedgerReconciliationSchedulerService, StockSnapshotSchedulerService, SlaEvaluationSchedulerService create BackgroundJob and context.BackgroundJobs.Add.
- **Enqueuers:** ReplayJobEnqueuer, RebuildJobEnqueuer, AsyncEventEnqueuer create BackgroundJob and Add.
- **Handlers/API:** OrderAssignedOperationsHandler, InventoryController create BackgroundJob and Add.
- All use ApplicationDbContext directly; no abstraction for “enqueue job”.

### IWorkerCoordinator

- TryClaimBackgroundJobAsync: atomic claim (WorkerId, ClaimedAtUtc, State=Running, StartedAt). ReleaseBackgroundJobAsync clears ownership.

---

## Goal (Phase 3)

- **Job Orchestration boundary** owns: job lifecycle, dispatch, retry policy, worker claim logic, SLA timers, job completion events.
- **JobExecution** entity with: JobType, PayloadJson, Status, AttemptCount, MaxAttempts, NextRunAtUtc, lease fields, CorrelationId, CausationId, CompanyId.
- **Worker pipeline** similar to NotificationDispatch: persist job, worker claims, executes, marks result, retry/NextRunAtUtc/dead-letter.
- **Lifecycle events:** ops.job.started.v1, ops.job.completed.v1, ops.job.failed.v1.
- **Business modules** do not run background tasks directly; they enqueue via orchestration.

---

## Approach

- Introduce **JobExecution** entity and **JobExecutions** table (new) for the orchestration boundary. Keep **BackgroundJob** and **BackgroundJobProcessorService** for backward compatibility; new flows use JobExecution. Single **JobExecutionWorkerHostedService** claims JobExecutions and executes via a registry of executors (by JobType). Emit job lifecycle events to event store. Provide **IJobExecutionEnqueuer** so schedulers/callers can enqueue without touching DbContext. Optionally migrate one scheduler (e.g. PnlRebuild) to use JobExecution to prove the path.
