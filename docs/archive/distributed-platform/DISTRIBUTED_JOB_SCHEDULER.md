# CephasOps Distributed Job Scheduler — Phase 1

This document describes the **job polling coordinator** and safe job claiming introduced in Phase 1. It works with the [Distributed Worker Architecture](DISTRIBUTED_WORKER_ARCHITECTURE.md) to allow multiple workers to safely discover and claim background jobs.

## 1. Scheduling model

- **Job polling coordinator** (`JobPollingCoordinatorService`): A background service that runs on each worker process. On a configurable interval it:
  1. Queries for **runnable** background jobs: `State = Queued`, scheduled (or no schedule), and either unclaimed (`WorkerId` is null) or owned by an **inactive** worker (heartbeat timeout).
  2. Attempts to **claim** up to `MaxJobsPerPoll` jobs atomically (see below).
  3. Records diagnostics (discovered count, claim attempts, success/failure).

- **Execution pipeline**: The existing `BackgroundJobProcessorService` continues to run. It now:
  1. Processes jobs that are **already claimed by this worker** (`State = Running`, `WorkerId = current worker`), i.e. jobs the coordinator just claimed.
  2. As backward compatibility, also processes **unclaimed** Queued jobs (when no worker id or legacy mode): it claims them then executes.

So the flow is: **Coordinator claims jobs → Processor executes jobs claimed by this worker.** No external queue; coordination is via the database.

## 2. Job claiming (safe, concurrent)

- **Where**: `BackgroundJob` has `WorkerId` and `ClaimedAtUtc`. Claiming is implemented in `IWorkerCoordinator.TryClaimBackgroundJobAsync`.
- **Rules**: A job can be claimed only if:
  - `State == Queued`
  - and either `WorkerId` is null, or the current owner is **inactive** (worker not active or `LastHeartbeatUtc` older than `Workers:InactiveTimeoutMinutes`).
- **On claim**: Single SQL update sets `WorkerId = current worker`, `ClaimedAtUtc = now`, `State = Running`, `StartedAt = now`. The update is conditional (WHERE state = Queued AND (unclaimed OR owner inactive)) so only one worker can claim a given job.
- **Concurrency**: Database-level atomic update prevents two workers from both claiming the same job. Rows affected = 1 means this worker won the claim.

## 3. Worker coordination with scheduler

- Each API process runs both **Worker heartbeat** and **Job polling coordinator**. The coordinator uses the same worker id as the heartbeat service.
- Stale worker recovery (in the worker layer) marks workers inactive and releases their **BackgroundJob** ownership (as well as ReplayOperation and RebuildOperation). Released jobs become runnable again for other workers.
- Operational replay/rebuild jobs still use **operation-level** ownership (ReplayOperation/RebuildOperation.WorkerId). The scheduler only claims the **BackgroundJob**; the processor then claims the operation when it runs the job (unchanged Phase 1 worker behaviour).

## 4. Job lifecycle

1. **Enqueue**: A job is created with `State = Queued`, `WorkerId = null`.
2. **Discover**: Coordinator queries runnable jobs (Queued, unclaimed or stale-owned).
3. **Claim**: Coordinator atomically updates job to `State = Running`, `WorkerId = me`, `ClaimedAtUtc`, `StartedAt`.
4. **Execute**: Processor picks jobs where `State = Running` and `WorkerId = me`, runs the handler, then sets `State = Succeeded` or `Failed` and clears ownership on completion.
5. **Stale release**: If the worker dies, heartbeat stops; stale recovery marks the worker inactive and clears `WorkerId`/`ClaimedAtUtc` on jobs it owned. Those jobs go back to runnable (Queued with null owner) and can be claimed by another worker.

## 5. Configuration

```json
{
  "Scheduler": {
    "PollIntervalSeconds": 15,
    "MaxJobsPerPoll": 10
  }
}
```

- **PollIntervalSeconds**: How often the coordinator runs (default 15). Recommended 10–30.
- **MaxJobsPerPoll**: Maximum jobs to claim per cycle (default 10). Keeps a single poll from starving other workers.

## 6. Diagnostics and troubleshooting

- **GET /api/system/scheduler**: Returns polling interval, worker id, last poll time, totals (discovered, claim attempts, success, failure), and recent claim attempts. Admin only (JobsAdmin).
- **Admin → Scheduler** (UI): Same information in a diagnostics page.

### Jobs appear stuck

- **Not being discovered**: Check that jobs are `State = Queued` and either `WorkerId` is null or the owning worker is stale. Use Admin → Workers to see worker heartbeat and stale state.
- **Claim attempts but failures**: Another worker is claiming first (normal under load), or the job is already `Running` (processor may be about to run it). Check Admin → Background Jobs for job state and worker.
- **Coordinator not running**: Ensure the API host is running and the worker is registered (Admin → Workers). Scheduler runs in the same process as the worker heartbeat.
- **Processor not running**: Same process runs both coordinator and processor; if the process is up, both run. Check that jobs claimed by this worker (`State = Running`, `WorkerId = this worker`) are being processed (logs, Background Jobs list).

## 7. Database schema

The scheduler requires `BackgroundJobs.WorkerId` and `BackgroundJobs.ClaimedAtUtc` plus indexes. These are added by the EF Core migration `AddBackgroundJobWorkerOwnership`. If `dotnet ef database update` fails due to other migrations, you can apply only the scheduler schema with the idempotent script:

`backend/scripts/apply-background-job-worker-ownership.sql`

After running it, record the migration as applied (e.g. insert into `__EFMigrationsHistory` for `AddBackgroundJobWorkerOwnership`) so EF does not try to re-apply it.

## 8. Limitations (Phase 1)

- No external queue (Kafka, etc.); polling and claiming are database-driven.
- Diagnostics are in-memory (per process); after restart, totals reset. Recent lists are capped (e.g. 50 items).
- Only one coordinator runs per process; scaling is horizontal (multiple API/worker instances).
- Replay and rebuild execution logic is unchanged; operation-level ownership is still enforced in addition to job-level claiming.
