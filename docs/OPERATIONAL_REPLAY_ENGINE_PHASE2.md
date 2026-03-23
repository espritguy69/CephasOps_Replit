# Operational Replay Engine — Phase 2

This document describes the Phase 2 upgrades to the Operational Replay Engine: replay target registry, checkpoint/resume, impact preview, ordering strategy, rerun-failed semantics, progress API, and runbook. It extends [OPERATIONAL_REPLAY_ENGINE_PHASE1.md](OPERATIONAL_REPLAY_ENGINE_PHASE1.md). Only behaviour that exists in the current codebase is documented.

---

## 1. Replay Target Registry

Replay targets are explicit, discoverable modules registered in **IReplayTargetRegistry** (singleton). Each target exposes:

- **Metadata:** Id, DisplayName, Description
- **Supported:** Whether the target is executable (true for EventStore, Workflow, Projection; false for Financial, Parser)
- **Ordering strategy:** OrderingStrategyId and OrderingStrategyDescription
- **Capabilities:** SupportsPreview, SupportsApply, SupportsCheckpoint, IsReplaySafe
- **SupportedFilterNames:** Which request filters apply (e.g. CompanyId, EventType, FromOccurredAtUtc)
- **Limitations:** List of strings describing constraints or caveats

**Registered targets (current behaviour):**

| Id | DisplayName | Supported | Notes |
|----|-------------|-----------|--------|
| EventStore | Event Store | Yes | Default. Full event load, policy, dispatch with side-effect suppression. |
| Workflow | Workflow | Yes | Same as EventStore; use EventType filter (e.g. WorkflowTransitionCompleted). |
| Projection | Projection | Yes | Same event stream and ordering; **only handlers implementing IProjectionEventHandler run**; other handlers are skipped. Distinct execution path in the dispatcher. See §1b for projection handler coverage. |
| Financial | Financial | **No** | Not implemented as a replay target. Use PnlRebuild or dedicated financial flows. |
| Parser | Parser | **No** | Not implemented; use ParserReplayService / attachment-based replay. |

**API:** `GET /api/event-store/replay/targets` returns all descriptors. Only targets with `Supported === true` should be used for execute. The UI should not offer execute for unsupported targets.

**1b. Projection handler model and coverage**

- For replay target **Projection**, the dispatcher invokes only handlers that implement **IProjectionEventHandler&lt;TEvent&gt;** (replay-safe, idempotent read-model updaters). All other handlers are skipped.
- **Supported projection coverage (current):**
  - **WorkflowTransitionHistory** — Handled by `WorkflowTransitionHistoryProjectionHandler` for event type `WorkflowTransitionCompleted`. Writes/upserts into the `WorkflowTransitionHistory` table keyed by EventId (idempotent). Used for workflow history read model and replay-safe rebuilds.
- Event types that have no registered IProjectionEventHandler are still replayed from the store but produce no projection changes; the event is marked processed. Preview indicates when there are no matching projection handlers (see §3).
- Do not claim broad financial or other projection replay unless implemented and replay-safe.

---

## 2. Checkpoint / Resume

**State model:** ReplayOperation.State can be:

- **Pending** — Created, not yet run (or job queued).
- **Running** — Execution in progress.
- **PartiallyCompleted** — Run interrupted (cancel or exception); **ResumeRequired** is true; can be resumed from checkpoint.
- **Completed** — Run finished successfully.
- **Failed** — Run finished with failure (no resume; use rerun-failed if there were failed events).
- **Cancelled** — Operator-requested cancel; run stopped at next checkpoint or immediately if Pending/PartiallyCompleted (no job running).

**Checkpoint fields on ReplayOperation:**

- **ResumeRequired** — True when the run was interrupted and can be resumed.
- **LastCheckpointAtUtc** — Last time a checkpoint was persisted.
- **LastProcessedEventId** / **LastProcessedOccurredAtUtc** — Resume cursor (events after this pair are not yet processed).
- **CheckpointCount** — Number of checkpoints written this run.
- **ProcessedCountAtLastCheckpoint** — Total processed at last checkpoint.

Execution persists a checkpoint every batch (e.g. every 50 events). On cancellation or exception, state is set to PartiallyCompleted, ResumeRequired = true, and the last checkpoint is left in place.

**Resume behaviour:**

- **Same operation.** Resume does not create a new ReplayOperation; it continues the same one.
- **Eligible states:** PartiallyCompleted or Pending.
- **API:** `POST /api/event-store/replay/operations/{id}/resume` (optional query `?async=true` to enqueue a job).
- **Execution:** ExecuteByOperationIdAsync loads the operation, passes LastProcessedEventId and LastProcessedOccurredAtUtc into the event query so only events **after** that cursor are returned. Already-recorded ReplayOperationEvent rows are not reprocessed.
- **Background resume:** When `async=true`, the same job type (OperationalReplay) is enqueued with payload `replayOperationId`; the job processor calls ExecuteByOperationIdAsync, which performs resume when state is PartiallyCompleted or Pending.

Resume is deterministic: ordering is OccurredAtUtc ASC, EventId ASC; resume cursor is exclusive (strictly after last processed event).

---

## 2b. Cancel

**Eligible states for cancel:** Pending, Running, PartiallyCompleted. Completed, Failed, and already Cancelled cannot be cancelled.

**Behaviour:**

- **Pending or PartiallyCompleted:** No job is running. Cancel sets State = Cancelled, CompletedAtUtc = now, ErrorSummary = "Cancelled by user.", ResumeRequired = false immediately.
- **Running:** Cancel sets **CancelRequestedAtUtc** = now on the operation. The execution loop checks this at each batch boundary (after checkpoint); when set, it stops, persists counts, sets State = Cancelled, CompletedAtUtc = now, ErrorSummary = "Cancelled by user.", ResumeRequired = false. Cancellation is cooperative and safe (no corruption).

**API:** `POST /api/event-store/replay/operations/{id}/cancel`. Returns 200 with operation state; 400 if state does not allow cancel.

**UI:** Cancel action is shown only for Pending, Running, and PartiallyCompleted.

---

## 3. Replay Diff / Impact Preview

**Preview API** (`POST /api/event-store/replay/preview`) returns, in addition to Phase 1 fields:

- **OrderingStrategyId** / **OrderingStrategyDescription** — From the target descriptor (e.g. OccurredAtUtcAscendingEventIdAscending).
- **OrderingGuaranteeLevel** — StrongDeterministic, BestEffortDeterministic, or LimitedDegraded (from target).
- **OrderingDegradedReason** — Reason when ordering is degraded (if any).
- **EstimatedAffectedEntityTypes** — Entity types estimated from the sample (from target/preview logic).
- **Limitations** — Known limitations for the selected target/filters (from target descriptor).
- **ReplayTargetId** — Target id used for the preview.

Preview does **not** execute handlers. There is no projection diff or “before/after” snapshot in the current implementation; We do not promise exact before/after diffs. For Projection target, preview includes AffectedProjectionCategories, EstimatedChangedEntityCount, ProjectionPreviewQuality (Estimated or Unavailable), and ProjectionPreviewUnavailableReason when applicable. Limitations and ordering are honest and reflect what the backend uses.

---

## 4. Deterministic Ordering and Visibility

**Ordering strategy:** All supported targets (EventStore, Workflow, Projection) use the same strategy:

- **Id:** `OccurredAtUtcAscendingEventIdAscending`
- **Behaviour:** Events are loaded and processed in order: OccurredAtUtc ASC, then EventId ASC. EventStore has no dedicated sequence number; EventId is the tie-breaker.

**Ordering guarantee level** (per target, exposed in preview and operation detail/progress):

- **StrongDeterministic** — Same order every time for the same filter; EventStore, Workflow, and Projection full runs use this.
- **BestEffortDeterministic** — Order is intended to be stable but not guaranteed. Used for **rerun-failed** operations: events are replayed in **processed order** (OrderBy ProcessedAtUtc of the original run), not event-time order.
- **LimitedDegraded** — Order may be limited or degraded; **OrderingDegradedReason** explains why (e.g. missing sequence column).

ReplayOperation stores **OrderingStrategyId** when the operation is created. Preview, operation detail, and progress API expose **OrderingStrategyId**, **OrderingGuaranteeLevel**, and **OrderingDegradedReason**. For operations created via **rerun-failed** (RetriedFromOperationId set), detail and progress return **OrderingGuaranteeLevel** = BestEffortDeterministic and **OrderingDegradedReason** = "Events replayed in processed order, not event-time order." Do not overstate guarantees; degraded reason is shown when applicable.

---

## 5. Rerun / Retry Semantics

**Rerun failed only (selective failed-event retry):**

- **New operation.** Creates a **new** ReplayOperation; the original is never mutated.
- **Linkage:** New operation has **RetriedFromOperationId** = original operation id, and **RerunReason** (optional, from request body).
- **API:** `POST /api/event-store/replay/operations/{id}/rerun-failed` with optional body `{ "rerunReason": "..." }`.
- **Behaviour:** Loads original operation; loads ReplayOperationEvent rows where Succeeded = false; (skipped events are not retried). The original operation ReplayTarget must be supported (EventStore, Workflow, or Projection); if not, the API returns 400. Creates a new operation and replays only those failed event IDs in processed order. No change to the original operation’s state or event rows. Ordering for the new run is BestEffortDeterministic; see section 4.

**Resume (same operation):** See §2. Resume continues the same operation from checkpoint; it does not create a new one.

**Full rerun:** To “rerun full,” start a new replay via the normal execute endpoint with the same filters; do not mutate the historical operation.

---

## 6. Progress and Status

**Progress API:** `GET /api/event-store/replay/operations/{id}/progress` returns:

- OperationId, State, ResumeRequired
- TotalEligible, TotalExecuted, TotalSucceeded, TotalFailed
- ProcessedCountAtLastCheckpoint, LastCheckpointAtUtc, LastProcessedEventId
- **ProgressPercent** — 0–100 when TotalEligible is known (derived from processed count).

Use this for active or resumable runs to show progress in the UI. For completed or failed runs, the operation detail is the source of truth.

---

## 7. Admin API Additions (Phase 2)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | /api/event-store/replay/targets | List replay target descriptors from registry. |
| GET | /api/event-store/replay/operations/{id}/progress | Progress for active/resumable run. |
| POST | /api/event-store/replay/operations/{id}/resume | Resume operation (PartiallyCompleted or Pending). Query `?async=true` to enqueue. |
| POST | /api/event-store/replay/operations/{id}/rerun-failed | Create new operation that replays only failed events of this operation; body optional `{ rerunReason }`. |
| POST | /api/event-store/replay/operations/{id}/cancel | Request cancel. Pending/PartiallyCompleted: cancelled immediately. Running: stop at next checkpoint. |

Preview and execute requests should send **ReplayTarget** (target id from registry). Execute validates the target via registry **IsSupported**; unsupported targets are rejected.

---

## 8. Side-Effect Suppression

Unchanged from Phase 1. Replay runs with **IReplayExecutionContext** and **SuppressSideEffects = true**. The domain event dispatcher does not enqueue EventHandlingAsync jobs during replay, so duplicate async handlers, notifications, and outbound side effects are avoided. Handlers that must not run during replay should check **IReplayExecutionContextAccessor.Current?.SuppressSideEffects** and skip outbound actions.

---

## 9. Runbook

### Failed replay

1. Check ReplayOperation.State and ErrorSummary; inspect ReplayOperationEvent rows where Succeeded = false.
2. Fix underlying cause (e.g. handler bug, data issue).
3. Use **Rerun failed** (`POST .../operations/{id}/rerun-failed`) to create a new operation that replays only the failed events, or run a new full replay with the same filters if appropriate.

### Resume-required (PartiallyCompleted)

1. Confirm State = PartiallyCompleted and ResumeRequired = true.
2. Optionally call `GET .../operations/{id}/progress` to see ProcessedCountAtLastCheckpoint and TotalEligible.
3. Call `POST .../operations/{id}/resume` (sync) or `POST .../operations/{id}/resume?async=true` to continue. The same operation is resumed from the last checkpoint; no new operation is created.

### Rerun failed

1. From an operation that has TotalFailed > 0 and State = Completed or Failed, call `POST .../operations/{id}/rerun-failed` with optional `{ "rerunReason": "..." }`.
2. A **new** operation is created with RetriedFromOperationId set; only events that had Succeeded = false in the original run are replayed.
3. Track the new operation via list or detail; do not mutate the original.

### Cancel

1. For Pending or PartiallyCompleted: `POST .../operations/{id}/cancel` sets State = Cancelled immediately (no job running).
2. For Running: same endpoint sets CancelRequestedAtUtc; the job stops at the next checkpoint and then sets State = Cancelled. Poll progress or refresh detail to see the transition.

### Active replay monitoring

1. For operations in Running state, poll `GET .../operations/{id}/progress` or refresh operation detail to see TotalExecuted, ProgressPercent, LastCheckpointAtUtc. UI may show checkpoint age (e.g. “2 min ago”).
2. If the run is interrupted (e.g. process exit), state becomes PartiallyCompleted and ResumeRequired = true; use the resume flow above.
3. To stop a running replay: call `POST .../operations/{id}/cancel`. The job will stop at the next checkpoint and state will become Cancelled.

---

## 10. Honest Limitations and Unsupported Targets

- **Financial / Parser:** Listed in the target registry with **Supported = false**. Do not use them for execute; use dedicated PnlRebuild or ParserReplay flows.
- **Projection:** Supported. Only handlers implementing IProjectionEventHandler run for the Projection target; other handlers are skipped.
- **Resume:** Only PartiallyCompleted and Pending can be resumed. Failed runs are not resumed; use rerun-failed to retry failed events in a new operation.
- **Cancelled:** Implemented. Running jobs stop at next checkpoint when cancel is requested; Pending/PartiallyCompleted are cancelled immediately.
- **Preview:** Projection target gets bounded diff preview (affected categories, estimated changed count, quality Estimated/Unavailable and reason). No exact before/after snapshot.
- **Ordering:** Full replay uses OccurredAtUtcAscendingEventIdAscending (StrongDeterministic). Rerun-failed uses processed order (BestEffortDeterministic); progress and detail expose OrderingDegradedReason for rerun-failed.

---

## 11. Data Model (Phase 2)

**ReplayOperation** (additional columns): ResumeRequired, LastCheckpointAtUtc, LastProcessedEventId, LastProcessedOccurredAtUtc, CheckpointCount, ProcessedCountAtLastCheckpoint, OrderingStrategyId, RetriedFromOperationId, RerunReason, CancelRequestedAtUtc. Index on RetriedFromOperationId.

**ReplayOperationEvent:** No schema change in Phase 2 (already had EventType, EntityType, EntityId, SkippedReason, DurationMs from Phase 1 extensions).

Migrations: `AddReplayOperations`, `ExtendReplayOperationsAndEvents`, `ReplayOperationPhase2CheckpointResumeRerun`, `AddWorkflowTransitionHistory` (projection read model for workflow transitions). The EF Core model snapshot is aligned with the entity and these migrations.

---

## 12. Observability (Phase 2)

- **Metrics:** replay.runs.resumed, replay.checkpoints.written, replay.preview.requests, replay.runs.rerun (tags: replay_target, rerun_type e.g. failed_only). Existing replay run/event counters and histogram remain.
- **Logging:** Structured fields for resume, checkpoint, rerun, and ordering where applicable.

---

Phase 2 makes the replay engine production-hardened with a clear target registry, resumable long runs, honest preview and ordering, and safe rerun-failed behaviour without mutating historical operations.
