# System Hardening Audit — CephasOps

**Date:** 2026-03-09  
**Scope:** Event Bus, Operational Replay Engine, Event Ledger, Workflow Engine, Admin APIs, Database constraints and performance  
**Goal:** Identify stability, safety, and reliability improvements before the next major architectural step. No code or schema changes were made; this is an audit and recommendations only.

---

## 1. Event Bus Stability

### 1.1 Event publishing and store append

- **Persist-then-dispatch:** `DomainEventDispatcher.PublishAsync` appends to the event store, marks the event as Processing, then calls `DispatchToHandlersAsync`. This ensures events are durable before any handler runs.
- **Duplicate append protection:** `EventStoreEntry` uses `EventId` as the primary key. A second `AppendAsync` with the same `EventId` would cause a unique constraint violation. Callers must ensure each event has a unique `EventId` (e.g. `Guid.NewGuid()` at creation). There is no in-repository “insert if not exists” for `EventId`; the PK enforces idempotency at the DB layer.

### 1.2 Handler execution

- **In-process handlers:** Run sequentially in a loop. Exceptions are caught, logged, and stored (last handler name, error message); processing continues to the next handler. After all in-process handlers, `MarkProcessedAsync` is called once (success/failure).
- **Async handlers:** When present and not in replay, the dispatcher enqueues a single background job (by `eventId`) and returns without marking processed; the background job processor loads the event, runs async handlers, then calls `MarkProcessedAsync`. So each event is marked processed either after in-process-only handling or after the async job completes.
- **Replay:** When `IReplayExecutionContext.SuppressSideEffects` is true, async handlers are not enqueued; only in-process handlers run. This avoids duplicate side effects during operational replay.

### 1.3 Correlation

- **CorrelationId** is carried on the domain event and persisted on `EventStoreEntry`. It is available for logging and tracing. No issues identified.

### 1.4 Replay compatibility

- Replay uses the same dispatcher with replay context set (`SuppressSideEffects = true`). No double append (replay path uses `DispatchToHandlersAsync` with events loaded from the store, not `PublishAsync`). Ledger and other projection-style handlers can safely run during replay.

### 1.5 Error handling and retries

- **Failed in-process handling:** Event is marked with `success: false`, `RetryCount` incremented, `Status` set to `Failed` or `DeadLetter` after `MaxRetriesBeforeDeadLetter` (5). There is no automatic retry loop inside the dispatcher; retries are triggered externally (e.g. admin “retry” or background job). Risk of “retry storms” exists only if an external process repeatedly retries the same failing event without backoff or circuit breaker.
- **Processing state:** `MarkAsProcessingAsync` is used before dispatch (both in `PublishAsync` and in `EventReplayService`). If the process crashes after marking Processing but before MarkProcessed, the event remains in Processing. There is no automated “recovery” of stuck Processing events; they rely on manual or scheduled retry.

### 1.6 Gaps and risks

| Risk | Description | Affected components |
|------|-------------|---------------------|
| **No handler-level idempotency** | If the same event is dispatched twice (e.g. two callers both calling retry for the same event, or retry while an async job for that event is still queued), all in-process handlers run again and async handlers can be enqueued again. No idempotency key or lock prevents duplicate handling. | `DomainEventDispatcher`, `EventReplayService`, async job processor |
| **Single-event retry can re-enqueue** | `EventReplayService.RetryAsync`/`ReplayAsync` do not set replay context. They call `MarkAsProcessingAsync` then `DispatchToHandlersAsync`. If the event has async handlers, the dispatcher will enqueue again. So a “retry” can create a second async job for the same event. | `EventReplayService`, `DomainEventDispatcher`, `IAsyncEventEnqueuer` |
| **Stuck “Processing” events** | Events left in Processing after a crash are only corrected by manual or scheduled retry. No built-in “claim” or timeout to reassign or fail them. | `EventStoreRepository`, background processor |
| **Limited processing history** | Only the latest error and last handler are stored per event. There is no per-event log of all handler runs (e.g. for debugging repeated failures). | `EventStoreEntry`, observability |

---

## 2. Replay Engine Safety

### 2.1 Replay ordering and checkpointing

- **Ordering:** Events are loaded in ascending `OccurredAtUtc` then `EventId` for deterministic replay. Batch size and max events are capped (e.g. default batch 50, take up to 10000 per query).
- **Checkpointing:** After each batch, the replay operation is updated (counts, last event cursor, state). Resume uses `resumeAfterEventId` and `resumeAfterOccurredAtUtc` so the next run continues strictly after the last checkpointed event.

### 2.2 Resume and cancel

- **Resume:** Allowed only for operations in `Pending` or `PartiallyCompleted`. Resume reuses the same operation and continues from the stored cursor. Implementation is consistent.
- **Cancel:** Cooperative. `RequestCancelAsync` sets `CancelRequestedAtUtc`. The execution loop reloads this flag each batch and exits cleanly, then updates the operation state. For Pending/PartiallyCompleted (no job running), cancel is applied immediately.

### 2.3 Rerun-failed

- **Rerun-failed** creates a new `ReplayOperation` and only processes events that were recorded as failed in the original operation. No double-counting of success; the new operation has its own ledger and metrics.

### 2.4 Replay context and side effects

- **Replay context:** `OperationalReplayExecutionService` sets `ReplayExecutionContext.ForReplay(..., suppressSideEffects: true)` for the duration of the run and clears it in a `finally` block. Async handlers are not enqueued during replay; in-process handlers (e.g. ledger writers) run and are expected to be idempotent.

### 2.5 Background job processing

- **Enqueue:** `ReplayJobEnqueuer.EnqueueReplayAsync` creates a new `ReplayOperation` with `State = Pending`, a `BackgroundJob` with `JobType = "OperationalReplay"` and payload `replayOperationId`, `scopeCompanyId`, `requestedByUserId`, then saves both. `EnqueueResumeAsync` creates only a new job for an existing operation. There is no check for an already-running or queued replay for the same company before enqueueing.
- **Execution:** `BackgroundJobProcessorService.ProcessOperationalReplayJobAsync` resolves `IOperationalReplayExecutionService` from a new scope and calls `ExecuteByOperationIdAsync(operationId, ...)`. The execution service loads the operation, builds the request from it, and runs the replay loop (with replay context and checkpointing). No per-company lock is acquired; multiple replay jobs for the same company can run in parallel if the job runner picks them.
- **Job configuration:** OperationalReplay job type has `MaxRetries = 2` and `DefaultStuckThresholdSeconds = 7200` in `JobDefinitionProvider`. Failed jobs can be retried up to 2 times; stuck detection is available for observability but does not prevent concurrent replays.

### 2.6 Gaps and risks

| Risk | Description | Affected components |
|------|-------------|---------------------|
| **No concurrency guard per company** | Multiple replay operations for the same company can run at the same time (e.g. two admins or two jobs). Overlapping event sets will be processed twice by handlers. Ledger is idempotent; other handlers may not be. This can cause duplicate side effects or inconsistent state. | `OperationalReplayExecutionService`, replay job enqueuer, API |
| **No “safety window” for recent events** | Replay filters use `ToOccurredAtUtc` from the request. There is no enforced exclusion of “recent” events (e.g. last N minutes). Replaying very recent events while live traffic is still writing can increase race risk (e.g. same order updated by both replay and live). | Replay request validation, `EventStoreQueryService.GetEventsForReplayAsync` |
| **Resume vs. concurrent run** | If an operation is in PartiallyCompleted and a second replay for the same company is started, two execution paths can process overlapping events. No lock prevents this. | `OperationalReplayExecutionService`, `ExecuteByOperationIdAsync` |

---

## 3. Event Ledger Integrity

### 3.1 Append-only and idempotency

- **Append-only:** All writes go through `LedgerWriter` as inserts. No updates or deletes on ledger entries.
- **Idempotency (event-driven):** `AppendFromEventAsync` checks for an existing row with the same `SourceEventId` and `LedgerFamily`; if found, it returns without inserting. The database enforces uniqueness via a unique partial index on `(SourceEventId, LedgerFamily)` where `SourceEventId IS NOT NULL`.
- **Idempotency (replay-operation-driven):** `AppendFromReplayOperationAsync` does the same for `(ReplayOperationId, LedgerFamily)` with a unique partial index where `ReplayOperationId IS NOT NULL`.

### 3.2 Ordering and payload

- **Ordering:** Each entry stores `OrderingStrategyId` and `OccurredAtUtc`. Queries use `OccurredAtUtc` and `Id` for ordering. Per-family ordering is consistent with the replay strategy.
- **Payload snapshot:** Stored as provided by the handler. Correctness depends on handlers passing the intended snapshot.

### 3.3 Replay compatibility

- During replay, ledger handlers run with the same event payload; they call `AppendFromEventAsync` with the replayed event’s id. The existence check ensures one row per (SourceEventId, LedgerFamily), so replay does not create duplicate entries.

### 3.4 Gaps and risks

| Risk | Description | Affected components |
|------|-------------|---------------------|
| **TOCTOU race on append** | The pattern is “check exists then insert”. Under concurrency, two callers can both see “not exists” and both attempt insert. One will succeed; the other will hit the unique constraint and get a DB exception. The application does not use “insert and ignore conflict” or “on conflict do nothing”, so the failing caller may surface an error to the handler. Handlers that do not catch duplicate-key exceptions could cause the event to be marked failed or retried. | `LedgerWriter`, ledger handlers |
| **No application-level conflict handling** | If the database throws on duplicate key, the caller (e.g. ledger handler) must handle it or the event may be marked as failed. There is no central “handle unique violation as success” in the writer. | `LedgerWriter` |
| **Payload snapshot not validated** | Payload snapshot is stored as provided by each handler; there is no schema validation, size limit, or format check. Incorrect or oversized JSON could be written, causing downstream timeline/projection consumers to fail when parsing, or could bloat the ledger table. | `LedgerWriter`, ledger handlers, timeline/projection readers |

---

## 4. Workflow Engine Correctness

### 4.1 Transition execution and event emission

- **Flow:** Transition validation, job creation/update, and `SaveChangesAsync` are performed first. Only after a successful commit does the engine publish domain events.
- **Events per transition:** One `WorkflowTransitionCompletedEvent` and, when the entity is an order, one `OrderStatusChangedEvent` are published after the transition is committed. Both use new `EventId`s and the same correlation and company context. No evidence of duplicate emission for the same transition.

### 4.2 Side effects and replay

- **Workflow as source:** The workflow engine is the source of transition and order-status events; it is not invoked during event replay. Replay re-runs handlers (e.g. ledger, projections) that consume these events. So workflow side effects (DB updates) are not re-executed during replay.
- **Failure of event publish:** If `PublishAsync` fails after the transition is saved, the failure is logged and the transition is not rolled back. The event may be missing from the store; operational procedures (e.g. replay or repair) would need to address that. No bypass path was found that would skip event emission under normal success path.

### 4.3 Order status and bypass paths

- **Order.Status writes:** In the Application layer, `Order.Status` is set only in `WorkflowEngineService` (single assignment: `order.Status = newStatus` after transition validation and side effects, inside the same commit as the workflow job). `OrderService.UpdateOrderStatusAsync` does not update the entity directly; it calls `ExecuteTransitionAsync` and the workflow engine performs the status update. No other Application code was found that assigns to `Order.Status`; other modules (Scheduler, Parser, Agent, Billing) only read `order.Status` for branching.
- **Bypass conclusion:** Order status is changed only through the workflow transition path. There are no direct bypass paths that set `Order.Status` while skipping the workflow engine or event emission.

### 4.4 Summary

- Order status events are emitted once per successful transition, after commit. Workflow side effects are replay-safe because replay does not re-execute transitions. Order status is updated exclusively via the workflow engine; no bypass paths were identified.

---

## 5. Admin API Safety

### 5.1 Authorization

- **Event Ledger:** Controller is `[Authorize(Policy = "Jobs")]`. All actions use `[RequirePermission(PermissionCatalog.JobsAdmin)]`. Company scope is applied for non–super-admin users (`ScopeCompanyId()`); requests that specify a different company are rejected with 403.
- **Operational Replay:** Same: `Jobs` policy and `JobsAdmin` on all endpoints. Scope enforced so that non–super-admins can only run or list replay for their company.

### 5.2 Data access and pagination

- **Ledger entries:** `ListEntries` uses `page` and `pageSize`; `pageSize` is clamped to a maximum of 100. Total count is returned for UI pagination.
- **Timeline endpoints:** Workflow transition, order timeline, and unified order history all use a `limit` parameter clamped to a maximum of 500.
- **Replay operations:** `ListOperations` uses `page` and `pageSize` (max 100). Detail and progress endpoints are by single operation id.
- **Event store listing:** `EventStoreQueryService.GetEventsAsync` uses `Page` and `PageSize` (clamped); no unbounded list found in the reviewed code.

### 5.3 Query efficiency

- List and timeline queries use indexed columns (e.g. `CompanyId`, `OccurredAtUtc`, `LedgerFamily`, `EntityId`, `OrderId`) and limits. No full table scans without filters were identified in the reviewed admin paths.

### 5.4 Summary

- Admin replay and ledger/timeline endpoints are protected by Jobs policy and JobsAdmin permission, with company scoping. Pagination and limits are in place; no critical missing authorization or unbounded scan was found.

---

## 6. Database Constraints and Performance

### 6.1 Event store

- **EventStoreEntry:** Primary key on `EventId`. Indexes on `(CompanyId, EventType, OccurredAtUtc)`, `CorrelationId`, `Status`, `OccurredAtUtc`. This supports filtering by company, type, status, and time. No additional uniqueness beyond the PK.

### 6.2 Ledger entries

- **LedgerEntry:** Unique partial index on `(SourceEventId, LedgerFamily)` where `SourceEventId IS NOT NULL`. Unique partial index on `(ReplayOperationId, LedgerFamily)` where `ReplayOperationId IS NOT NULL`. Additional indexes on `(CompanyId, LedgerFamily, OccurredAtUtc)`, `(EntityType, EntityId, LedgerFamily)`, `RecordedAtUtc`, and filtered indexes on `SourceEventId` and `ReplayOperationId`. Schema supports idempotent appends and common query patterns.

### 6.3 Replay operations and events

- **ReplayOperation:** PK on `Id`. Indexes on `RequestedAtUtc`, `(CompanyId, RequestedAtUtc)`, `RequestedByUserId`, `RetriedFromOperationId`. There is no index on `State`; listing “running” or “resumable” operations may filter on `State` without a dedicated index (impact depends on volume).
- **ReplayOperationEvent:** PK on `Id`. Indexes on `ReplayOperationId` and `EventId`. Suitable for “events for this operation” and “operations that replayed this event” lookups.

### 6.4 Gaps and risks

| Risk | Description | Affected components |
|------|-------------|---------------------|
| **ReplayOperations.State not indexed** | Queries that filter by `State` (e.g. Running, PartiallyCompleted) may perform less efficiently at scale. | Replay operation list, progress, resume eligibility |
| **No DB-level “one running replay per company”** | Concurrency is not enforced by the schema. Preventing overlapping replays per company would require application-level locking or a dedicated constraint/table. | Replay execution, API |

---

## 7. Recommended Improvements (Prioritized)

| # | Improvement | Risk addressed | Affected components | Recommended mitigation | Difficulty |
|---|-------------|----------------|---------------------|------------------------|------------|
| 1 | **Event Bus idempotency guard** | Duplicate handling when the same event is dispatched or retried more than once (including re-enqueue of async handlers). | `DomainEventDispatcher`, `EventReplayService`, async job processor | Introduce a processing guard (e.g. “claimed until completed” or idempotency key per event/handler). Optionally ensure single-event retry either sets a replay-like context so async handlers are not re-enqueued, or uses a “retry” path that never enqueues. | Medium |
| 2 | **Replay safety locks** | Concurrent replays for the same company processing overlapping events and running handlers twice. | `OperationalReplayExecutionService`, replay job enqueuer, API | Before starting or resuming a replay, acquire a lock (e.g. distributed or DB advisory lock) scoped by company (and optionally replay target). Release when the run completes or is cancelled. Reject or queue new replays for that company while the lock is held. | Medium |
| 3 | **Event Bus observability** | Hard to diagnose retries, stuck Processing events, and handler failure history. | Event store, dispatcher, background processor | Add an observability layer: optional processing log table or structured logs per event (handler name, outcome, timestamp). Consider a small dashboard or admin view for Processing/Failed/DeadLetter counts and last error. Optionally, scheduled job or manual tool to list and optionally retry or dead-letter stuck Processing events. | Medium |
| 4 | **Ledger append conflict handling** | TOCTOU race can surface as handler failure when two writers append for the same (SourceEventId, LedgerFamily). | `LedgerWriter`, ledger handlers | In `LedgerWriter`, catch unique constraint violation on insert and treat as success (no-op). Alternatively use DB “insert on conflict do nothing” (e.g. PostgreSQL `ON CONFLICT`) so the second insert is a no-op and no exception is thrown. | Low |
| 5 | **Replay “safety window”** | Replaying very recent events while live traffic is still writing. | Replay request validation, query layer | Reject or warn when `ToOccurredAtUtc` is within a configured window of “now” (e.g. last 5–15 minutes), or add an optional parameter to exclude recent events. Document as operational guidance. | Low |
| 6 | **Single-event retry and async handlers** | Retry/Replay of a single event can enqueue async handlers again. | `EventReplayService`, dispatcher | When invoking dispatch from `EventReplayService`, set a replay context with `SuppressSideEffects = true` so that async handlers are not enqueued for single-event retries, or introduce a dedicated “retry path” that never enqueues. | Low |
| 7 | **ReplayOperations.State index** | List/filter by State may be slow at scale. | Replay queries | Add an index on `ReplayOperations.State` (or composite e.g. `(CompanyId, State)`) if profiling shows need. | Low |
| 8 | **Stuck Processing events** | Events left in Processing after a crash. | Event store, operations | Document or add a small admin tool to list events in Processing older than N minutes and optionally mark them Failed for retry or move to DeadLetter. Optional: background job that marks as Failed after a timeout so they can be retried. | Medium |
| 9 | **Ledger payload snapshot validation** | Incorrect or oversized payload snapshots can break timeline/projection readers or bloat the ledger. | `LedgerWriter`, ledger handlers | Optional: enforce a max length for `PayloadSnapshot` and/or validate JSON before insert; document handler contract for snapshot format. | Low |

---

## Summary

- **Event Bus:** Persist-then-dispatch and PK on `EventId` prevent duplicate store appends. Main gaps are handler-level idempotency, single-event retry re-enqueueing async handlers, and observability/stuck Processing handling.
- **Replay Engine:** Ordering, checkpoint/resume, cancel, and rerun-failed behave correctly. The main gap is no concurrency control per company and no safety window for recent events.
- **Event Ledger:** Append-only and DB unique indexes enforce idempotency; the only issue is the TOCTOU race that can surface as a handler failure without conflict handling in the writer.
- **Workflow Engine:** Single emission per transition and replay-safe; no issues identified.
- **Admin APIs:** Authorization and pagination are in place; no critical issues found.
- **Database:** Schema and indexes are generally adequate; adding an index on `ReplayOperations.State` (if needed) and considering application-level replay locks would improve safety and performance.

Implementing **Event Bus idempotency guard**, **Replay safety locks**, and **Event Bus observability** (items 1–3) will give the highest impact on production stability and resilience before the next major architectural step.

**Implementation note:** The Event Bus idempotency guard (item 1) has been implemented. See **docs/EVENT_BUS_IDEMPOTENCY_GUARD.md** for purpose, uniqueness rules, replay interaction, and limitations.
