# CephasOps Distributed Ops Platform — Phase 3 Job Orchestration Summary

**Date:** 2026-03-09

---

## A. What was added

- **JobExecution** (Domain): Persisted work item with JobType, PayloadJson, Status (Pending | Running | Succeeded | Failed | DeadLetter), AttemptCount, MaxAttempts, NextRunAtUtc, lease fields (ProcessingNodeId, ProcessingLeaseExpiresAtUtc, ClaimedAtUtc), CompanyId, CorrelationId, CausationId, Priority.
- **IJobExecutionStore** (Domain): AddAsync, ClaimNextPendingBatchAsync (FOR UPDATE SKIP LOCKED), MarkSucceededAsync, MarkFailedAsync. Implementation in Infrastructure (**JobExecutionStore**).
- **JobExecutions** table**: Migration `20260309230000_AddJobExecutions`; indexes on (Status, NextRunAtUtc) and (CompanyId, Status).
- **IJobExecutionEnqueuer** (Application): EnqueueAsync(jobType, payloadJson, companyId?, correlationId?, causationId?, priority, nextRunAtUtc?, ct). Business modules use this instead of adding BackgroundJob directly for the new pipeline.
- **IJobExecutor** / **IJobExecutorRegistry** (Application): Executors keyed by JobType; registry resolves by type (case-insensitive).
- **JobExecutionWorkerHostedService** (Application): Polls store, claims batch, emits **JobStartedEvent**, runs executor, then **MarkSucceededAsync** + **JobCompletedEvent** or **MarkFailedAsync** + **JobFailedEvent**. Options: JobOrchestration:Worker (BatchSize, PollIntervalMs, LeaseSeconds, NodeId, BusyDelayMs).
- **Lifecycle events** (Application.Events): **JobStartedEvent**, **JobCompletedEvent**, **JobFailedEvent** (ops.job.started.v1, ops.job.completed.v1, ops.job.failed.v1); appended via IEventStore when worker runs jobs.

---

## B. Worker pipeline (same pattern as NotificationDispatch)

1. **Enqueue**: Scheduler or other caller uses **IJobExecutionEnqueuer.EnqueueAsync** → **IJobExecutionStore.AddAsync** (Status = Pending).
2. **Claim**: **JobExecutionWorkerHostedService** calls **ClaimNextPendingBatchAsync** (Status = Pending and NextRunAtUtc ≤ now, ORDER BY Priority DESC, CreatedAtUtc, FOR UPDATE SKIP LOCKED); sets Status = Running, lease fields, StartedAtUtc, ClaimedAtUtc.
3. **Execute**: For each claimed job, get **IJobExecutor** from registry; if none, mark failed (non-retryable) and emit JobFailedEvent. Otherwise emit JobStartedEvent, run **ExecuteAsync**, then mark succeeded/failed and emit JobCompletedEvent or JobFailedEvent.
4. **Retry**: **MarkFailedAsync** increments AttemptCount, sets NextRunAtUtc (backoff) and LastError; when AttemptCount ≥ MaxAttempts or isNonRetryable, Status = DeadLetter.

---

## C. PnlRebuild wired to JobExecution

- **PnlRebuildSchedulerService** no longer adds **BackgroundJob** rows for pnlrebuild. It checks **JobExecutions** for Pending/Running **PnlRebuild** jobs; if none, calls **IJobExecutionEnqueuer.EnqueueAsync("PnlRebuild", payloadJson, companyId)**.
- **PnlRebuildJobExecutor** (IJobExecutor, JobType = "PnlRebuild"): Parses payload (companyId, period), calls **IPnlService.RebuildPnlAsync**.
- **BackgroundJobProcessorService** still processes other **BackgroundJob** types (email ingest, notification send, document generation, etc.) and any legacy pnlrebuild rows; no change to that path.

---

## D. DI and configuration

- **Program.cs**: IJobExecutionStore → JobExecutionStore; IJobExecutionEnqueuer → JobExecutionEnqueuer; IJobExecutor → PnlRebuildJobExecutor; IJobExecutorRegistry → JobExecutorRegistry; JobExecutionWorkerOptions (JobOrchestration:Worker); JobExecutionWorkerHostedService.
- Optional appsettings: `JobOrchestration:Worker:NodeId`, `BatchSize`, `PollIntervalMs`, `LeaseSeconds`, `BusyDelayMs`.

---

## E. Migrations

- **20260309230000_AddJobExecutions**: Creates JobExecutions table and indexes. Apply via `dotnet ef database update` or idempotent script as per project practice.

---

## F. Success criteria (met)

- Jobs are persisted (JobExecutions table).
- Worker claims jobs (ClaimNextPendingBatchAsync with lease).
- Retries handled in job layer (MarkFailedAsync, NextRunAtUtc, DeadLetter after MaxAttempts).
- Business modules no longer run PnlRebuild background work directly; scheduler enqueues, worker executes.
- Lifecycle events emitted: ops.job.started.v1, ops.job.completed.v1, ops.job.failed.v1.

---

## G. Remaining debt / next steps

- Other job types (email ingest, notification send, document generation, SLA evaluation, etc.) still use **BackgroundJob** + **BackgroundJobProcessorService**. Migrate them incrementally to **IJobExecutionEnqueuer** + new **IJobExecutor** implementations.
- Consider **stale lease reaper** for JobExecutions (reset Running jobs whose ProcessingLeaseExpiresAtUtc &lt; now to Pending), similar to EventStore and NotificationDispatches.
- Optional: expose JobExecutions in admin/ops UI (list, dead-letter reset, metrics).
