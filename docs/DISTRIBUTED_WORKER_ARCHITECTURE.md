# CephasOps Distributed Worker Architecture — Phase 1

This document describes the **Worker Coordination Layer** introduced in Phase 1: worker identity, heartbeat, job ownership, stale recovery, and how replay/rebuild jobs run in a distributed-safe way.

## 1. Worker identity model

Each running process that can execute operational jobs (API host, dedicated worker, or scheduler) is represented by a **worker instance** stored in the `WorkerInstances` table.

- **WorkerId** (GUID): Unique id for this process instance.
- **HostName**: From `Dns.GetHostName()`.
- **ProcessId**: OS process id.
- **Role**: `API`, `Worker`, or `Scheduler`. The API process registers as `API` and runs the background job processor in-process.
- **StartedAtUtc**: When the worker registered.
- **LastHeartbeatUtc**: Last time the worker called the heartbeat endpoint.
- **IsActive**: `true` until the worker is marked stale (see below).

Workers register on startup via `WorkerHeartbeatHostedService`. No manual registration is required.

## 2. Heartbeat behavior

- **Interval**: Configurable via `Workers:HeartbeatIntervalSeconds` (default 15). Recommended 10–30 seconds.
- **Inactive timeout**: Configurable via `Workers:InactiveTimeoutMinutes` (default 3). Recommended 2–5 minutes. Workers that have not heartbeaten within this window are treated as **stale**.
- Heartbeat updates `LastHeartbeatUtc` for the current worker. It runs automatically in the background; no API call is required from operators.

### Configuration (appsettings)

```json
{
  "Workers": {
    "HeartbeatIntervalSeconds": 15,
    "InactiveTimeoutMinutes": 3
  }
}
```

## 3. Job ownership model

Operational jobs that must run on only one node use **job ownership** on the operation record (not on the background job row).

### Affected operations

- **ReplayOperation**: `WorkerId`, `ClaimedAtUtc` added. When a worker runs an OperationalReplay background job, it must **claim** the corresponding `ReplayOperation` before executing. Claim fails if another **active** worker already owns it.
- **RebuildOperation**: Same pattern; `WorkerId`, `ClaimedAtUtc` on the rebuild operation.

### Execution rules

1. Before running an OperationalReplay or OperationalRebuild job, the worker calls `TryClaimReplayOperationAsync` or `TryClaimRebuildOperationAsync`. If the operation is already owned by an active worker (heartbeat within timeout), the claim fails and the job is **skipped** this cycle (remains Queued).
2. If the operation is unclaimed or owned by a **stale** worker, the claim succeeds and the worker sets `WorkerId` and `ClaimedAtUtc` on the operation.
3. The worker then executes the replay or rebuild logic (existing code paths unchanged).
4. On completion or failure, the worker **releases** ownership: `WorkerId` and `ClaimedAtUtc` are cleared so another worker can pick the job up on retry or resume.

Single-node behaviour is unchanged: one API process registers one worker, claims the job, runs it, and releases.

## 4. Stale worker recovery

If a worker process dies while holding a job (e.g. crash, OOM, kill), it will stop heartbeating. The **stale recovery** logic (run periodically by the same heartbeat service, about every minute):

1. Finds workers where `LastHeartbeatUtc` is older than `InactiveTimeoutMinutes`.
2. Sets those workers’ `IsActive` to `false`.
3. **Releases** all ReplayOperation and RebuildOperation rows owned by those workers (`WorkerId` and `ClaimedAtUtc` set to null).

After that, any healthy worker can claim and run those operations again (e.g. on the next job poll). The logic is conservative: only workers past the inactive timeout are marked stale; we do not steal jobs from workers that are still heartbeating.

## 5. How rebuild/replay jobs run in distributed mode

- **Replay**: User or API enqueues an OperationalReplay job (creates a Pending `ReplayOperation` and a `BackgroundJob`). Any worker that runs the job processor will try to claim that `ReplayOperation`. Only the one that successfully claims it runs the replay; others skip. On completion or failure, the worker releases; if the operation is resumable, a new job can be enqueued and another worker can claim and resume.
- **Rebuild**: Same pattern for OperationalRebuild and `RebuildOperation`. Locking (e.g. `RebuildExecutionLock`) is unchanged; job ownership is an additional layer so that only one worker executes the rebuild for a given operation.

## 6. Operational troubleshooting

- **Workers list**: Admin → **Workers** in the UI, or `GET /api/system/workers`. Shows all worker instances, last heartbeat age, role, and active/stale state.
- **Worker detail**: `GET /api/system/workers/{id}` or click **Details** in the UI. Shows owned replay/rebuild operations.
- **Stale workers**: If a worker is marked stale, it will no longer be used for new claims. Its jobs have been released; check Background Jobs and Operational Replay/Rebuild lists to see if operations were re-queued or need to be resumed.
- **Single node**: One API = one worker. Heartbeat and stale recovery still run; if the process is healthy, the worker stays active and continues to claim and run jobs as before.

For **distributed job scheduling** (discovering and claiming background jobs across workers), see [Distributed Job Scheduler](DISTRIBUTED_JOB_SCHEDULER.md).

## 7. Limitations (Phase 1)

- Only **Replay** and **Rebuild** operations use job ownership. Other background job types (e.g. EmailIngest, PnlRebuild) are not yet worker-scoped; they remain single-node by convention (one processor) or can be extended in a later phase.
- There is no dedicated “Worker” or “Scheduler” process type yet; all current workers register as `API`. The same primitives (identity, heartbeat, claim/release) can be used when adding separate worker or scheduler hosts.
- No Kafka or external queue: job distribution is via the existing `BackgroundJobs` table and polling. Phase 1 only adds coordination so that when multiple nodes poll, they do not double-execute replay/rebuild.

## 8. Validation checklist

- Backend build succeeds.
- Workers register automatically on API startup.
- Heartbeat updates `LastHeartbeatUtc` (visible in Workers UI or DB).
- Job ownership: only one worker can run a given replay/rebuild operation at a time; claim fails for an already-owned operation when the owner is active.
- Stale recovery: after a worker stops heartbeating for longer than `InactiveTimeoutMinutes`, it is marked inactive and its operations are released; another worker can then claim and run them.
- UI: Admin → Workers shows worker instances, heartbeat age, role, and stale detection; detail view shows owned jobs.
- Single-node operation is unchanged: one API process runs one worker and processes jobs as before.
