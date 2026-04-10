# Operational Replay Engine — Phase 2 Design

## 1. Replay Target Registry

- **IReplayTargetDescriptor**: metadata for a target (Id, DisplayName, SupportedFilters, OrderingStrategyId, SupportsPreview, SupportsApply, SupportsCheckpoint, IsReplaySafe).
- **IReplayTargetRegistry**: returns all registered targets; get by id; validate request against target.
- **Targets**: EventStoreReplayTarget, WorkflowReplayTarget (EventStore + event type filter), ProjectionReplayTarget (same event stream, projection-only semantics). Financial/Parser as placeholder descriptors with Supported=false and description.
- **Registration**: In DI, register registry that holds EventStore, Workflow, Projection; Financial/Parser as "incomplete" with honest metadata.
- **Execution**: Execution service resolves target from registry; uses target’s ordering and capability flags. No change to core replay loop except it is driven by target (e.g. EventStore vs Projection handler path).

## 2. Checkpoint / Resume

- **ReplayOperation** new fields: `ResumeRequired` (bool), `LastCheckpointAtUtc`, `LastProcessedEventId`, `LastProcessedOccurredAtUtc` (for cursor), `CheckpointCount`, `ProcessedCountAtLastCheckpoint`.
- **State model**: Pending → Running → Completed | Failed | PartiallyCompleted (ResumeRequired=true). Cancelled = explicit cancel (optional).
- **Checkpoint table (optional)**: We can store only on ReplayOperation to avoid extra table: last event id + timestamp + counts. For Phase 2, checkpoint data on ReplayOperation is enough; no separate ReplayCheckpoint table unless we need multiple checkpoints per run.
- **Resume**: ExecuteByOperationIdAsync accepts operations in state PartiallyCompleted (or Pending). Load operation, get LastProcessedEventId, query events after that in same order (OccurredAtUtc, EventId), continue from there; skip already-recorded ReplayOperationEvent rows for that operation. Persist checkpoint every N events (e.g. 50).
- **Determinism**: Ordering is OccurredAtUtc ASC, EventId ASC. Resume = “events after (LastProcessedOccurredAtUtc, LastProcessedEventId)” so no reprocessing.

## 3. Replay Diff / Impact Preview

- **ReplayPreviewResultDto** extensions: `OrderingStrategy`, `OrderingStrategyDescription`, `EstimatedAffectedEntityTypes`, `EstimatedAffectedEntityIds` (count or sample), `Limitations`, `ProjectionDiffAvailable` (bool), optional `ProjectionSummaryBefore` / `ProjectionSummaryAfter` for projection target when cheap.
- **ReplayPreviewService**: Use target from registry; add ordering strategy from target; for event-store targets compute affected entity types/ids from sample or count; limitations from target. No fake diff; if projection diff is expensive, set ProjectionDiffAvailable=false and document.

## 4. Projection Replay

- **ProjectionReplayTarget**: Same as EventStore in terms of event loading/ordering; “apply” path runs only projection handlers (read-model updaters). Requires a way to mark handlers as projection-only or a separate list of projection handlers. Phase 2: implement as target that uses same IEventReplayService but with a filter so only projection handlers run (e.g. handler metadata or a dedicated IProjectionReplayHandler interface). If no projection handlers exist yet, target is “supported” but no-op beyond event replay to projection handlers that opt-in.
- **Replay-safe**: Only handlers that are registered as projection handlers and are idempotent. No side-effect handlers.

## 5. Deterministic Ordering

- **Ordering strategy**: Explicit value per target, e.g. `OccurredAtUtcAscendingEventIdAscending`. Stored in ReplayOperation (OrderingStrategyId). Preview and detail return it. EventStore/Workflow/Projection all use same: OccurredAtUtc ASC, EventId ASC. Document that EventStore has no sequence number; tie-breaker is EventId.

## 6. Rerun / Retry Semantics

- **ReplayOperation**: `RetriedFromOperationId`, `RerunReason`, `RequestedByUserId` (existing). New operation created for “rerun full” or “rerun failed only”; link via RetriedFromOperationId.
- **Rerun failed only**: New operation, same filters but we only consider events that were in the original run and had Succeeded=false. Load original ReplayOperationEvent rows (failed), replay those event IDs only. New operation with RetriedFromOperationId = original.
- **Resume**: Same operation; resume from checkpoint (no new operation).
- **Retry interrupted**: Same operation; call ExecuteByOperationIdAsync again when state = PartiallyCompleted.

## 7. API Extensions

- `GET /api/event-store/replay/targets` — list target descriptors from registry.
- `POST /api/event-store/replay/preview` — existing; response extended with Phase 2 preview fields.
- `POST /api/event-store/replay/execute` — existing; optional RetriedFromOperationId, RerunReason in body for reruns.
- `POST /api/event-store/replay/operations/{id}/resume` — resume operation (state PartiallyCompleted or Pending); enqueue job if async.
- `POST /api/event-store/replay/operations/{id}/rerun-failed` — create new operation that replays only failed events from this operation; optional async.
- `GET /api/event-store/replay/operations/{id}/progress` — return progress (ProcessedCount, TotalEligible, LastCheckpointAtUtc, State) for active/resumable runs.
- No mutation of historical operation meaning; rerun always creates new op when “rerun failed”.

## 8. Data Model (Migrations)

- **ReplayOperation**: ResumeRequired (bool), LastCheckpointAtUtc (DateTime?), LastProcessedEventId (Guid?), LastProcessedOccurredAtUtc (DateTime?), CheckpointCount (int), ProcessedCountAtLastCheckpoint (int?), OrderingStrategyId (string?), RetriedFromOperationId (Guid?), RerunReason (string?). State allowed values: Pending, Running, PartiallyCompleted, Completed, Failed, Cancelled.
- **ReplayOperationEvent**: no schema change for Phase 2 (already has EventId, Succeeded, etc.).

## 9. Observability

- Metrics: replay.runs.resumed, replay.checkpoints.written, replay.run.progress_percent (gauge or last value), replay.preview.requests, replay.runs.rerun (tag: rerun_type = full | failed_only), replay.runs.failed (per target), ordering_strategy tag where useful.
- Logging: structured fields for resume, checkpoint, rerun, ordering.

## 10. Implementation Order

1. Domain + migration (ReplayOperation fields; state constants).
2. Replay target registry (interfaces, descriptors, EventStore/Workflow/Projection, register in DI).
3. Ordering strategy (constants, store on operation, expose in preview/detail).
4. Checkpoint in RunReplayCoreAsync (persist every N events; set PartiallyCompleted on cancel or failure with progress).
5. Resume in ExecuteByOperationIdAsync (load checkpoint, query events after cursor, continue).
6. Richer preview DTO and ReplayPreviewService (ordering, limitations, affected entities).
7. Rerun: create new operation with RetriedFromOperationId; rerun-failed loads failed event IDs and replays only those.
8. API: targets, resume, rerun-failed, progress; preview/execute DTOs extended.
9. Projection target: descriptor + same execution path with projection-only flag (or handler filter); if no handlers, no-op.
10. UI: target selector, preview card, progress, resume/rerun buttons, badges.
11. Metrics + docs.
