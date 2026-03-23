# Operational Replay — Concurrency and Safety

**Purpose:** Operational guidance for running Operational Replay: one active replay per company, safety window, resume/rerun-failed, and how background jobs process replay.

---

## 1. One active replay per company

- **Enforcement:** The application enforces at most one running replay per company via `ReplayExecutionLock` (table `ReplayExecutionLock`, unique on `CompanyId` where `ReleasedAtUtc IS NULL`).
- **Behavior:** Before starting or resuming a replay, the execution service acquires a lock for the operation’s company. If another replay for that company is already running (or has a lock that has not yet expired), the new request fails with a message like: *"Another replay is already running for this company. Wait for it to complete or cancel it before starting a new one."*
- **Resume:** Resuming a `PartiallyCompleted` or `Pending` operation also requires acquiring the company lock. Only one replay (new or resumed) can run per company at a time.
- **Release:** The lock is released in a `finally` block when the run completes, fails, or is cancelled. Stale locks (e.g. after a worker crash) expire after 2 hours and can be reclaimed by the next acquire.

**Recommendation:** Do not start a second replay for the same company while one is running or in `PartiallyCompleted` (resumable). Use the UI or API to cancel or wait for completion before starting another.

---

## 2. Safety window (recent events excluded)

- **Purpose:** Reduce replay/live overlap and races on the same entities.
- **Behavior:** Events with `OccurredAtUtc` newer than **(now − N minutes)** are excluded from replay. Default N = 5 (`ReplaySafetyWindow.DefaultWindowMinutes`). Preview and execution both apply this cutoff.
- **Operational guidance:** If you need to include very recent events, wait until they are at least N minutes old, or adjust the safety window in code/config if your deployment supports it. Replay requests that would include only recent events may process zero events.

---

## 3. Resume and rerun-failed

- **Resume:** For operations in `Pending` or `PartiallyCompleted`, use the resume API (e.g. `ExecuteByOperationIdAsync`). The run continues from the last checkpoint (`LastProcessedEventId` / `LastProcessedOccurredAtUtc`). The same company lock is required; no second replay for that company can run until the resumed run finishes.
- **Rerun-failed:** Creates a **new** replay operation that only processes events that were recorded as failed in the original operation. The new operation also requires the company lock when it runs. Safety window applies to rerun-failed as well.

---

## 4. Background job processing

- **Enqueue:** Starting a replay (or resume) creates a `ReplayOperation` and a `BackgroundJob` with `JobType = "OperationalReplay"`. The worker picks up the job and calls `ExecuteByOperationIdAsync` (for new runs, the operation is first created then executed by job).
- **Execution:** Only one replay per company runs at a time due to the lock. If multiple replay jobs for the same company are queued, they run one after another; the second will fail to acquire the lock and the operation will be marked failed with the “another replay is already running” message.
- **Cancellation:** Requesting cancel sets `CancelRequestedAtUtc`. The execution loop checks this each batch and exits cleanly, then releases the lock.

---

## 5. Summary

| Topic | Guidance |
|-------|----------|
| Concurrency | One active replay per company; enforced by `ReplayExecutionLock`. |
| Safety window | Events in the last N minutes (default 5) are excluded from replay. |
| Resume | Use resume for `Pending`/`PartiallyCompleted`; same lock rules apply. |
| Rerun-failed | New operation; lock required when it runs; safety window applies. |
| Background jobs | Replay jobs are processed by the worker; lock prevents parallel replays per company. |

See also: **docs/SYSTEM_HARDENING_AUDIT.md** (§2 Replay Engine Safety), **ReplayExecutionLockStore**, **OperationalReplayExecutionService**.
