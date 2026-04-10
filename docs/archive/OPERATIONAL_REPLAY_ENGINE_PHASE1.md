# Operational Replay Engine — Phase 1

This document describes the first controlled Operational Replay Engine added to CephasOps: architecture, safety model, replay policy, dry-run behavior, audit model, background job integration, API, observability, operational usage, runbook, and limitations.

**Phase 2:** For the replay target registry, checkpoint/resume, rerun-failed, progress API, and runbook for failed/resumable runs, see [OPERATIONAL_REPLAY_ENGINE_PHASE2.md](OPERATIONAL_REPLAY_ENGINE_PHASE2.md).

---

## 1. Architecture

- **Single-event replay (existing):** `IEventReplayService` replays one event by ID; `IEventReplayPolicy` allows or blocks by event type. Used for retry and manual replay from the Event Store UI.
- **Operational replay (Phase 1):** Filtered batch replay with a dedicated request model, eligibility engine, dry-run preview, execution engine, **replay execution context (side-effect suppression)**, **background job integration**, and full audit trail.
  - **Replay request:** `ReplayRequestDto` carries filters (CompanyId, EventType, Status, FromOccurredAtUtc, ToOccurredAtUtc, EntityType, EntityId, CorrelationId, MaxEvents) and options (DryRun, ReplayReason, **ReplayTarget**, **ReplayMode**).
  - **Replay targets:** EventStore (default), Workflow (event-type–filtered), Financial/Parser/Projection (bounded or documented; see § Replay targets).
  - **Replay modes:** DryRun (preview only), Apply (execute and persist). Dry-run is enforced via preview API; execute always uses Apply when running handlers.
  - **Replay execution context:** `IReplayExecutionContext` / `IReplayExecutionContextAccessor` (AsyncLocal) set during replay with `SuppressSideEffects = true`. The domain event dispatcher does **not** enqueue `EventHandlingAsync` jobs when this is set, so replay does not trigger duplicate async handlers, notifications, or outbound side effects. Handlers that perform SMS/email/external calls should check the accessor and skip when `SuppressSideEffects` is true.
  - **Eligibility:** `IOperationalReplayPolicy` extends the single-event policy with max replay window, max replay count per request, blocked companies, and destructive event types. Default deny.
  - **Preview:** `IReplayPreviewService` returns matching count, evaluated count, eligible/blocked counts, blocked reasons, sample events, and affected companies/event types **without** executing handlers.
  - **Execution:** `IOperationalReplayExecutionService` creates a `ReplayOperation` record (or uses an existing Pending one when run by the background job), sets replay context, loads events in **deterministic order** (OccurredAtUtc ascending, then EventId), filters by eligibility, and replays each via `IEventReplayService.ReplayAsync` in batches. Results and per-event attempts (including EventType, EntityType, EntityId, SkippedReason, DurationMs) are stored.
  - **Background job:** `IReplayJobEnqueuer` creates a ReplayOperation with State = Pending and enqueues a job of type `OperationalReplay`. The background job processor runs `ExecuteByOperationIdAsync` and updates the operation on completion or failure.
  - **Audit:** `ReplayOperation` and `ReplayOperationEvent` persist request parameters, ReplayTarget, ReplayMode, StartedAtUtc, DurationMs, SkippedCount, ErrorSummary, BackgroundJobId, and per-event EventType, EntityType, EntityId, SkippedReason, DurationMs.

No change to the event bus or to single-event replay behavior. All new behaviour is additive and gated by Jobs Admin.

---

## 2. Safety Model

- **Default deny:** Only event types explicitly allowed by `IEventReplayPolicy` can be replayed; operational replay applies additional checks (window, company, destructive).
- **Operational replay stricter than single-event:** Same allow-list; plus max replay window (e.g. 30 days), max events per request (e.g. 1000), and optional blocked companies and destructive-type list.
- **No mutation of EventStore:** Replay re-dispatches events to current handlers; it does not update or delete existing EventStore rows.
- **Company scoping:** Non–global admins are restricted to their company for preview, execute, and list operations.
- **Authorization:** All operational replay APIs require `JobsAdmin`; listing and viewing operations are also admin-only.
- **Dry-run first:** Preview API allows checking counts and blocked reasons before running execute.

---

## 3. Replay Policy

- **Single-event policy (`IEventReplayPolicy`):**  
  - Allowed: e.g. `WorkflowTransitionCompleted` (idempotent handlers).  
  - Blocked: anything not in the allowed set (and any explicitly listed blocked types).
- **Operational policy (`IOperationalReplayPolicy`):**  
  - Uses the single-event policy for “replayable type.”  
  - Adds: max replay window (days), max replay count per request, `IsDestructiveEventType`, `IsCompanyBlocked`.  
  - `CheckEligibility(entry, request, utcNow)` returns eligible/blocked and a reason.

Configuration is in code (`EventReplayPolicy`, `OperationalReplayPolicy`). Blocked companies and destructive types can be extended via static sets or future config.

---

## 4. Dry-Run Behavior

- **Preview endpoint:** `POST /api/event-store/replay/preview` with `ReplayRequestDto` (typically with `dryRun: true` or ignored for preview).
- **Behavior:**  
  - Applies the same filters as execute (company scope, request filters, MaxEvents cap).  
  - Loads a batch of events (no payload needed for eligibility).  
  - Runs eligibility for each event in the batch.  
  - Returns: TotalMatched, EvaluatedCount, EligibleCount, BlockedCount, BlockedReasons, SampleEvents, CompaniesAffected, EventTypesAffected.  
- **No handler execution:** Handlers are not invoked; only query and policy checks run.

---

## 5. Audit Model

- **ReplayOperation:** One row per replay request (preview does not create a row; execute does).  
  - Request: RequestedByUserId, RequestedAtUtc, DryRun, ReplayReason, ReplayTarget, ReplayMode, and all filter fields.  
  - Result: TotalMatched, TotalEligible, TotalExecuted, TotalSucceeded, TotalFailed, SkippedCount, ReplayCorrelationId, State, StartedAtUtc, CompletedAtUtc, DurationMs, ErrorSummary, Notes, BackgroundJobId.
- **ReplayOperationEvent:** Per-event rows: ReplayOperationId, EventId, EventType, EntityType, EntityId, Succeeded, ErrorMessage, SkippedReason, ProcessedAtUtc, DurationMs.
- **APIs:** List operations (`GET .../replay/operations`), get operation detail including event results (`GET .../replay/operations/{id}`).

---

## 6. API Summary

- **Preview:** `POST /api/event-store/replay/preview` — body: ReplayRequestDto. Returns ReplayPreviewResultDto (no handlers run).
- **Execute (sync):** `POST /api/event-store/replay/execute` — body: ReplayRequestDto (DryRun must be false). Creates ReplayOperation, runs replay in-process, returns OperationalReplayExecutionResultDto (200).
- **Execute (async):** `POST /api/event-store/replay/execute?async=true` — same body. Creates ReplayOperation with State = Pending, enqueues OperationalReplay job, returns 202 Accepted with `{ replayOperationId, message }`. Poll operation detail or Operations History for completion.
- **List operations:** `GET /api/event-store/replay/operations?page=1&pageSize=20` — returns paginated ReplayOperation list (company-scoped for non–global admins).
- **Operation detail:** `GET /api/event-store/replay/operations/{id}` — returns ReplayOperationDetailDto including EventResults (EventType, EntityType, EntityId, SkippedReason, DurationMs).

All replay endpoints require `JobsAdmin` and company scoping where applicable.

---

## 7. Observability

- **Structured logging:** Replay completion and job completion log ReplayOperationId, ReplayTarget, counts, DurationMs.
- **Metrics (System.Diagnostics.Metrics):** Meter `CephasOps.Replay` — counters: `replay.runs.started`, `replay.runs.completed`, `replay.runs.failed`, `replay.events.processed`, `replay.events.failed`; histogram: `replay.run.duration_seconds`. Tags: `replay_target`, `dry_run`. Export via OpenTelemetry or similar if configured.
- **Background jobs:** OperationalReplay jobs appear in the existing background jobs list with display name “Operational Replay”; JobDefinitionProvider includes default for OperationalReplay (MaxRetries = 2, StuckThreshold = 7200s).

---

## 8. Operational Usage

1. **Preview:** In Admin → Operational Replay, set filters (e.g. event type, date range, company), optionally MaxEvents and ReplayReason. Run “Run preview (dry-run)”. Review total matched, eligible/blocked counts, blocked reasons, and sample events.
2. **Execute (sync):** If the preview is acceptable, set a ReplayReason, then “Execute replay (sync)”. Confirm the dialog. The run creates a ReplayOperation and processes eligible events in the current request; the UI shows the new operation and can open its detail.
3. **Execute (async):** Use “Execute in background” to queue the replay. A ReplayOperation is created in Pending state and an OperationalReplay job is enqueued. Navigate to the operation detail page to monitor; refresh to see State, StartedAtUtc, DurationMs, and counts when the job completes.
4. **History:** Use “Operations History” to list past runs (Target/Mode, Skipped, Duration, State, Error summary) and open “Detail” for per-event results and links to the Event Store.
5. **Event Store links:** From operation detail, use event links to open the Event Bus Monitor for that event (and related JobRuns/WorkflowJobs as already supported).

---

## 9. Replay Targets (Phase 1)

| Target      | Implementation | Notes |
|------------|----------------|--------|
| EventStore | Default. Load events by filters, order by OccurredAtUtc asc + EventId, apply policy, dispatch with replay context. | Full support. |
| Workflow   | Same as EventStore with event type filter (e.g. WorkflowTransitionCompleted). | Use EventType filter. |
| Financial  | Bounded: use PnlRebuild job or RebuildPnlAsync; record as one logical operation. | Not wired as a separate replay target in Phase 1; document and add in a follow-up if needed. |
| Parser     | Bounded: ParserReplayService by attachment/session; no raw event-store replay. | Not wired as replay target in Phase 1; document and add in a follow-up if needed. |
| Projection | Treated as EventStore for Phase 1; no separate projection-only path. | Same as EventStore. |

---

## 10. Runbook (short)

- **Run a safe replay:** Preview first with filters and MaxEvents. Set ReplayReason. Execute (sync for small batches, async for large). Check operation detail for State, Failed count, ErrorSummary.
- **Replay stuck/failed:** Check BackgroundJobs for job type OperationalReplay; inspect LastError, RetryCount. Check ReplayOperation.ErrorSummary and ReplayOperationEvent rows for failed events. Use **Rerun failed** (Phase 2) to create a new operation that replays only failed events, or re-run single-event replay from Event Store UI for specific events if needed.
- **Resume interrupted run (Phase 2):** If State = PartiallyCompleted and ResumeRequired = true, use `POST .../operations/{id}/resume` (or `?async=true`) to continue from the last checkpoint. See [OPERATIONAL_REPLAY_ENGINE_PHASE2.md](OPERATIONAL_REPLAY_ENGINE_PHASE2.md).
- **Side effects during replay:** Replay runs with SuppressSideEffects; async event handlers are not enqueued. If a handler must not run during replay, implement a check on `IReplayExecutionContextAccessor.Current?.SuppressSideEffects` and skip outbound actions.

---

## 11. Rollout Guidance

- **Database:** Apply migrations for `ReplayOperations` and `ReplayOperationEvents` (including `ExtendReplayOperationsAndEvents` for ReplayTarget, ReplayMode, StartedAtUtc, DurationMs, SkippedCount, ErrorSummary, BackgroundJobId, and per-event EventType, EntityType, EntityId, SkippedReason, DurationMs).
- **Permissions:** Only users with `JobsAdmin` can use operational replay; company scoping applies for non–global admins.
- **Safe default:** Only event types already allowed for single-event replay (e.g. `WorkflowTransitionCompleted`) are eligible; unsafe types remain blocked. Replay context ensures no duplicate async jobs or notifications during replay.
- **Monitoring:** Use ReplayOperation and ReplayOperationEvent for audit; ReplayCorrelationId and metrics (CephasOps.Replay) for observability.
- **Limits:** Respect MaxReplayWindowDays and MaxReplayCountPerRequest; tune in `OperationalReplayPolicy` if needed.

---

## 12. Limitations (Phase 1)

- **Financial/Parser/Projection targets:** Phase 2 adds a target registry; EventStore, Workflow, and Projection are supported; Financial and Parser are registered but not supported for execute. See [OPERATIONAL_REPLAY_ENGINE_PHASE2.md](OPERATIONAL_REPLAY_ENGINE_PHASE2.md).
- **Resumability:** Phase 2 adds checkpoint/resume for interrupted runs (PartiallyCompleted). See Phase 2 doc for resume and rerun-failed behaviour.
- **Ordering:** Deterministic by OccurredAtUtc ascending then EventId; Phase 2 exposes OrderingStrategyId in preview and operation detail.
- **Idempotency:** Handlers re-invoked during replay must be idempotent (e.g. projection upserts). Replay does not re-send notifications or enqueue new async jobs thanks to SuppressSideEffects.

---

## 13. Success Criteria (Phase 1)

- Operational replay **preview** (counts, blocked reasons, sample) without executing handlers.  
- **Filtered replay** execution for eligible events in deterministic order with configurable cap.  
- **Replay execution context** and **side-effect suppression** so replay does not trigger duplicate async jobs or outbound notifications.  
- **Replay audit trail** (ReplayOperation + ReplayOperationEvent with full fields).  
- **Background job integration** for async execute (202 + operation id).  
- **API** (preview, execute sync/async, list, detail) and **Admin UI** (operations list, detail, event results, async execute).  
- **Metrics** and **structured logging** for replay runs and events.  
- **Documentation** (safety, targets, API, runbook, limitations).

CephasOps can safely replay selected operational event slices without unsafe system-wide reprocessing or duplicate side effects.
