# Replay Company Lock

## Purpose

Only **one operational replay** may run at a time **per company**. The replay company lock:

- Prevents concurrent replay jobs from rebuilding the same company scope
- Avoids nondeterministic replay results and race conditions
- Makes operator behavior in the admin UI predictable (one active replay per company)

The lock is enforced at **execution time**, not only at request submission. It is a durable, storage-level safeguard.

## When the Lock Applies

- **Company-scoped replay:** When `ReplayOperation.CompanyId` has a value, the lock is acquired before execution and released when the run finishes (success, failure, or cancel).
- **Global replay:** When `CompanyId` is null (e.g. cross-company or admin-wide replay), **no lock is used**. Multiple global replays can run concurrently. This is documented as a deliberate limitation.

## Lock Acquisition

- **Execute (new replay):** Before `RunReplayCoreAsync`, the service tries to acquire the lock for `operation.CompanyId`. If acquisition fails, the operation is set to **Failed** with a clear `ErrorSummary` and the API returns an error message. The replay does **not** run and is **not** queued behind the other one.
- **Execute by operation id (start or resume):** Same: acquire before running. If acquisition fails, the API returns an error; the operation remains in Pending or PartiallyCompleted so the operator can retry later.
- **Rerun-failed:** After creating the new operation, the service tries to acquire the lock for that company. If it fails, the new operation is set to **Failed** with a clear `ErrorSummary` and the API returns an error.

Acquisition is **explicit**: the caller always gets a clear success or failure. There is no silent queuing.

## Lock Release

The lock is released in a **finally** block so it is always released even when:

- The replay completes successfully
- The replay fails with an exception
- The replay is cancelled (cooperative cancel at next checkpoint)
- The process crashes (see stale lock below)

Release is implemented as: set `ReleasedAtUtc = now` for the row that matches `(CompanyId, ReplayOperationId)` and `ReleasedAtUtc IS NULL`. It is idempotent and safe to call multiple times.

## Stale Lock Handling

If a worker crashes or the process dies while holding the lock, the row would otherwise stay active forever. To avoid that:

- Each lock row has an **ExpiresAtUtc** (e.g. acquired time + 2 hours).
- When **acquiring**, the store first looks for an existing active lock for the company. If it finds one:
  - If **ExpiresAtUtc** is in the past, the lock is treated as **stale** and **reclaimed**: the row is updated to the new `ReplayOperationId` and a new `ExpiresAtUtc`. The new replay proceeds.
  - If **ExpiresAtUtc** is in the future, the lock is still active and acquisition **fails**.

So after a crash, the lock automatically becomes reclaimable after the expiry window (default 2 hours). No heartbeat is required for this behavior.

## Database Enforcement

- Table: **ReplayExecutionLock** (Id, CompanyId, ReplayOperationId, AcquiredAtUtc, ExpiresAtUtc, ReleasedAtUtc).
- **Unique partial index:** `(CompanyId)` **WHERE ReleasedAtUtc IS NULL**. So at most one row per company can have `ReleasedAtUtc IS NULL` at any time. This prevents two concurrent replays for the same company even under race conditions.

## Where the Lock Is Enforced

- **Background job path:** When the background job runs `ExecuteByOperationIdAsync` (or when the API calls `ExecuteAsync` and then the job runs the same execution), the lock is acquired and released inside `OperationalReplayExecutionService`. So the real execution boundary (run/resume) is protected.
- **Synchronous execution path:** When the API calls `ExecuteAsync` or `ExecuteByOperationIdAsync` without queuing (e.g. `async=false`), the same service methods run and use the same lock logic.
- **Resume:** Resuming an operation goes through `ExecuteByOperationIdAsync`, so it must acquire the lock. If another replay for that company is active, resume fails with a clear message.
- **Rerun-failed:** Creates a new operation and runs it; acquisition and release are done around that run. The company lock still applies.

## Cancel / Failure

- **Cancel:** When the operator requests cancel, the running job sets `CancelRequestedAtUtc`. At the next checkpoint the loop exits and the operation is marked Cancelled. The **finally** in the execution service then runs and **releases the lock**. So cancel always releases the lock.
- **Failure:** On exception, the **finally** still runs and releases the lock. The operation state is updated to Failed or PartiallyCompleted (and in rerun-failed, we set Failed and rethrow after saving).

## API / Operator-Visible Behavior

When a replay cannot start because the company lock is held:

- **Execute:** The new operation is saved with **State = Failed** and **ErrorSummary** set to: *"Another replay is already running for this company. Wait for it to complete or cancel it before starting a new one."* The API response includes this as **ErrorMessage**.
- **Resume / start by operation id:** The API returns an error with message: *"Another replay is already running for this company. Wait for it to complete or cancel it before starting or resuming another."* The operation is not changed (remains Pending or PartiallyCompleted).
- **Rerun-failed:** The new operation is saved with **State = Failed** and **ErrorSummary** set to: *"Another replay is already running for this company. Wait for it to complete or cancel it before rerunning failed events."* The API response includes this as **ErrorMessage**.

Operators see a clear, actionable message and can wait or cancel the active replay.

## Logging and Observability

Structured logs are emitted for:

- **Lock acquired:** `"Replay execution lock acquired. CompanyId=..., ReplayOperationId=..."`
- **Lock acquisition failed:** `"Replay execution lock acquisition failed: another replay is active. CompanyId=..., ActiveReplayOperationId=..."`
- **Lock released:** `"Replay execution lock released. CompanyId=..., ReplayOperationId=..."`
- **Stale lock reclaimed:** `"Replay execution lock reclaimed (stale). CompanyId=..., PreviousOpId=..., NewOpId=..."`

Additional warnings when replay is not started due to lock: `"Replay not started: company lock not acquired"`, `"Replay resume/start not executed: company lock not acquired"`, `"Rerun-failed not started: company lock not acquired"`.

## Limitations

- **Global (null company) replays** are not locked. Multiple replays with no company scope can run at once. Document and use with care.
- **Expiry window** is fixed (e.g. 2 hours). Very long-running replays that exceed this could have their lock reclaimed by another run if the expiry is not extended. A future improvement could add a heartbeat (e.g. update `ExpiresAtUtc` at each checkpoint).
- **Optional dependency:** If `IReplayExecutionLockStore` is not registered, the execution service does not acquire or release locks (backward compatibility). Register the store to enable the safeguard.
