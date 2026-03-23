# Operational State Rebuilder

## What it is

The **Operational State Rebuilder** is a bounded platform capability that lets CephasOps rebuild selected **derived operational state** from canonical history sources (Event Store, Event Ledger) in a deterministic, replay-safe way. It is not “replay again”: it is a **targeted rebuild framework** for restoring or regenerating read models when needed (e.g. after projection corruption, tenant bootstrap, or environment rebuild).

## Supported targets

| Target ID | Display name | Source of truth | Rebuild strategy | Resumable | Description |
|-----------|--------------|-----------------|------------------|-----------|-------------|
| `WorkflowTransitionHistory.EventStore` | Workflow transition history (from Event Store) | Event Store | FullReplace | Yes | Rebuilds the `WorkflowTransitionHistory` table from `WorkflowTransitionCompleted` events. Batched; checkpoint/resume supported. Scope: optional CompanyId, FromOccurredAtUtc, ToOccurredAtUtc. |
| `WorkflowTransitionHistory.Ledger` | Workflow transition history (from Event Ledger) | Event Ledger | FullReplace | No | Rebuilds the same table from Event Ledger entries (family `WorkflowTransition`). Full replace in one pass; no resume. Same scope options. |

There is no third target (e.g. order-history projection): no persisted order-history table exists; order timeline is query-time from the Ledger. Batching/checkpoint is implemented for the Event Store target only.

## Source of truth per target

- **WorkflowTransitionHistory.EventStore**: Event Store (`EventStore` table), event type `WorkflowTransitionCompleted`. Events are loaded in order `OccurredAtUtc ASC, EventId ASC`.
- **WorkflowTransitionHistory.Ledger**: Event Ledger (`LedgerEntries` table), `LedgerFamily = 'WorkflowTransition'`, `SourceEventId IS NOT NULL`. Entries are loaded in order `OccurredAtUtc ASC, Id ASC`.

## Rebuild strategy per target

Both Phase 1 targets use **FullReplace**:

1. **Scope** is determined by optional `CompanyId`, `FromOccurredAtUtc`, `ToOccurredAtUtc`. If none are set, the scope is the full table.
2. **Delete**: All rows in `WorkflowTransitionHistory` that fall within the scope are deleted.
3. **Insert**: Source records (events or ledger entries) in the same scope are read in order and inserted into `WorkflowTransitionHistory`. No updates; idempotency is achieved by replace semantics.

No **IdempotentUpsert** or **BoundedAppend** targets are implemented in Phase 1.

## Safety guarantees

- **No side effects**: Rebuild runners do not trigger outbound notifications, external integrations, or async job dispatch. Replay safety and side-effect suppression are preserved.
- **Deterministic**: For a given source and scope, the resulting target state is deterministic (same order, same logic as the projection handlers).
- **Bounded**: Execution is capped (e.g. 50,000 source records per run) to avoid unbounded runs.
- **Audit**: Each run is recorded in `RebuildOperations` (target, scope, requested-by, result counts, state, error).

## Phase 2: Async execution, lock, checkpoint/resume

### Async execution

- **Execute** can run synchronously (default) or in the background. Use `POST /api/event-store/rebuild/execute?async=true` to enqueue a rebuild; the API returns `202` with `rebuildOperationId` and a message. The operation is created with state **Pending** and a background job (type `OperationalRebuild`) runs it. Preview and dry-run remain synchronous.
- **Resume** can also be sync or async: `POST /api/event-store/rebuild/operations/{id}/resume?async=true` (optional `rerunReason` query). Operation state moves through: Pending → Running → Completed (or Failed / PartiallyCompleted).

### Rebuild execution lock

- A **durable lock** prevents conflicting rebuilds for the same target and scope. Scope is represented by a **scope key**: company Guid (when `companyId` is set) or `"global"` (when no company). Only one active rebuild per `(RebuildTargetId, ScopeKey)` is allowed.
- Lock is stored in `RebuildExecutionLocks`; it is acquired when a run starts and released when it finishes (success or failure). Stale locks (e.g. not released for 2 hours) can be reclaimed by a new run. If lock acquisition fails, the operation is set to **Failed** with an error message (e.g. "Another rebuild is running for this target/scope"). The UI shows lock/conflict errors when the API returns them.

### Checkpoint / resume

- **Event Store target** supports checkpoint and resume. Progress is persisted on `RebuildOperation`: `LastProcessedEventId`, `LastProcessedOccurredAtUtc`, `ProcessedCountAtLastCheckpoint`, `CheckpointCount`, `LastCheckpointAtUtc`. After each batch (e.g. 1000 events), a checkpoint is saved. If the run is interrupted or fails after at least one checkpoint, `ResumeRequired` is set and the operation can be **resumed** (same operation id); resume continues from the cursor without re-deleting.
- **Ledger target** does not support resume: full replace in one pass; no cursor is stored.
- Resume is only offered in the API/UI when the operation is in a resumable state and the target supports resume (see target support matrix).

### Target support matrix (Phase 2)

| Target | Strategy | Resumable | Lock scope | Ordering |
|--------|----------|-----------|------------|----------|
| WorkflowTransitionHistory.EventStore | FullReplace | Yes | (TargetId, company or global) | OccurredAtUtc ASC, EventId ASC; cursor persisted |
| WorkflowTransitionHistory.Ledger | FullReplace | No | (TargetId, company or global) | OccurredAtUtc ASC, Id ASC |

## Limitations

- **Two targets only**: Only the two WorkflowTransitionHistory targets above are implemented. No persisted order-history table exists; order timeline is query-time from the Ledger.
- **No financial or parser rebuilds**: Financial and parser rebuilds are out of scope; use dedicated jobs/APIs.
- **One active rebuild per target+scope**: Enforced by rebuild execution lock; overlapping runs for the same target and scope are blocked.
- **Scope**: Company and time range are the only scope dimensions. Entity-type or entity-id scoping is not exposed.

## API

- `GET /api/event-store/rebuild/targets` — List rebuild targets (descriptors; includes `supportsResume`).
- `POST /api/event-store/rebuild/preview` — Preview scope and impact (synchronous; no state changes). Body: `RebuildRequestDto`.
- `POST /api/event-store/rebuild/execute` — Execute rebuild. Body: `RebuildRequestDto`. Query: `async=true` to run in background (returns 202 with `rebuildOperationId`).
- `GET /api/event-store/rebuild/operations/{id}` — Get rebuild operation summary.
- `GET /api/event-store/rebuild/operations/{id}/progress` — Get progress (state, checkpoint count, processed count, row counts).
- `POST /api/event-store/rebuild/operations/{id}/resume` — Resume a partially completed or failed run. Query: `async=true`, optional `rerunReason`.
- `GET /api/event-store/rebuild/operations` — List rebuild operations (paged). Query: page, pageSize, state, rebuildTargetId.

All endpoints require Jobs Admin permission and respect company scope for non–global admins.

## Admin UI

Under **Admin → State Rebuilder**:

- **Rebuild** tab: Target selector, optional scope (company, from/to), Preview, Execute (dry-run), Execute, **Execute (background)**. Sync runs show result summary; background runs return 202 and switch to History with progress.
- **History** tab: Filter by state (All / Queued / Running / Partially done / Completed / Failed). Table: target, requested time, state (with dry-run and “Resume” when applicable), counts, duration, **Resume** button for resumable operations. When a background run is in progress, a progress banner shows state and checkpoint/processed counts (polled). Refresh to update list.

## Operational runbook (Phase 2)

- **Run a rebuild in the background**: In Rebuild tab, choose target and scope, then click **Execute (background)**. Confirm; you get a success message and are taken to History. The new operation appears as Queued then Running; progress (checkpoints, processed count) is shown in the banner and updates every few seconds until Completed or Failed.
- **Resume an interrupted run**: In History, find an operation with state Partially done or Failed and a “Resume” indicator. Click **Resume** to run synchronously, or use the API with `POST .../operations/{id}/resume?async=true` for background resume. Only the Event Store target supports resume.
- **Interpret lock errors**: If execute or resume returns Failed with a message like “Another rebuild is running for this target/scope”, wait for the running operation to finish or cancel it before retrying. One active rebuild per (target, company or global) is allowed.
- **Dry-run vs real**: Use **Execute (dry-run)** to see impact without writing; **Execute** and **Execute (background)** apply changes. Dry-run remains synchronous.

## Future expansion

- Additional targets (e.g. other projection tables) when their source of truth and rebuild semantics are defined.
- Optional **IdempotentUpsert** strategy for targets where full replace is not desired.
- Bounded **order timeline** or **unified order history** cache tables, if we introduce materialized read models for those.
- Tenant bootstrap / environment rebuild flows that orchestrate multiple targets in sequence.

## Relationship to replay

- **Operational Replay** re-dispatches stored events through handlers (including projections and ledger writers). It can repopulate projections and ledger as a side effect of replay.
- **State Rebuilder** is target-centric: it takes a **named target** and a **source of truth**, then clears and repopulates that target’s state directly. It does not run the full event pipeline. Use the Rebuilder when you want to restore a specific read model from Event Store or Ledger without re-running all handlers.

Both mechanisms are replay-safe and do not trigger notifications or external side effects when used as designed.
