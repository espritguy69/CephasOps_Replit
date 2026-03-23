# Event Bus Phase 4 — Production hardening

**Date:** 2026-03-09  
**Status:** Implemented  
**Depends on:** EVENT_BUS_PHASE4_WORKFLOW_EVENTS.md, EVENT_BUS_IDEMPOTENCY_GUARD.md, DOMAIN_EVENT_ARCHITECTURE.md.

---

## 1. What was implemented

Phase 4 moves the event bus from “events exist” to **durable, observable, retryable, idempotent, and safe for multi-module workflows**:

1. **Durable event publishing** — Events are written in the **same transaction** as business data (outbox-style). Workflow transition and event rows commit together; no “commit then publish” gap.
2. **Reliable dispatch** — A background **EventStoreDispatcherHostedService** claims pending (and due-retry) events and dispatches them via `IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true)`.
3. **Retry and dead-letter** — Failed handler runs increment `RetryCount`, set `NextRetryAtUtc` with bounded backoff, and after `MaxRetriesBeforeDeadLetter` (default 5) the event is marked **DeadLetter**. Structured logs record retry scheduled and dead-lettered.
4. **Idempotency** — Unchanged: `EventProcessingLogStore` ensures at most one successful completion per (EventId, HandlerName). Used by both in-process dispatch and async job processing.
5. **Observability** — Structured logging for: event persisted (in transaction), event dispatched from store, handler started/completed/failed, retry scheduled, message dead-lettered. CorrelationId, EventId, EventType, CompanyId, Attempt are included. Existing Event Bus observability API and handler processing tabs remain.
6. **Safe background processing** — Dispatcher uses **FOR UPDATE SKIP LOCKED** to claim events; bounded batch size and configurable polling; cancellation-aware; no unbounded loops.
7. **Event contracts** — Optional `PayloadVersion` on `EventStoreEntry` for future version-safe handling. Existing events remain compatible.
8. **Event lineage** — `CorrelationId` and `ParentEventId` are persisted on `EventStoreEntry` and propagated through append and dispatch. Handlers and trace APIs can use them for correlation and parent-child chains.
9. **Stuck Processing recovery** — Events left in Processing (e.g. after crash/termination) are reset automatically: each dispatcher loop runs `ResetStuckProcessingAsync`; entries in Processing longer than `StuckProcessingTimeoutMinutes` are set to Failed with `NextRetryAtUtc = now` so they are re-claimed. Safe against duplicate processing; retry and dead-letter semantics preserved. Structured logs record each reset (EventId, EventType, CompanyId, CorrelationId, RetryCount, age).

---

## 2. Architecture flow

```
[Workflow transition]
       │
       ▼
  Build event(s) ──► IEventStore.AppendInCurrentTransaction(evt)  (one or two events for Order)
       │
       ▼
  Job update + SaveChangesAsync()   ◄── single transaction
       │
       ▼
  Commit (events and job persisted)

  --- Background (EventStoreDispatcherHostedService) ---

  Poll (configurable interval)
       │
       ▼
  IEventStore.ResetStuckProcessingAsync(timeout)   (reset stale Processing → Failed for retry)
       │
       ▼
  IEventStore.ClaimNextPendingBatchAsync()   (Pending or Failed with NextRetryAtUtc <= now, FOR UPDATE SKIP LOCKED)
       │
       ▼
  For each claimed entry:
    Deserialize payload ──► IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true)
       │
       ▼
    Dispatcher: DispatchToHandlersAsync (in-process + optional async enqueue)
       │
       ▼
    On success: MarkProcessedAsync(success: true)
    On failure: MarkProcessedAsync(success: false) → RetryCount++, NextRetryAtUtc set, Status Failed or DeadLetter
```

---

## 3. Event lifecycle

| Status      | Meaning |
|------------|--------|
| **Pending** | Just persisted; not yet claimed by dispatcher. |
| **Processing** | Claimed by worker; dispatch in progress. |
| **Processed** | All in-process handlers run; async handlers enqueued or none; event complete. |
| **Failed** | At least one handler failed; `NextRetryAtUtc` set; will be re-claimed when due. |
| **DeadLetter** | `RetryCount >= MaxRetriesBeforeDeadLetter`; no further automatic retries. |

---

## 4. Retry and dead-letter

- **Backoff:** Fixed delays per attempt (seconds): 60, 120, 300, 600, 900 (configurable via options for future use; repository uses internal array).
- **NextRetryAtUtc:** Set when marking as Failed; dispatcher only re-claims when `NextRetryAtUtc <= now` and `RetryCount < MaxRetriesBeforeDeadLetter`.
- **Dead-letter:** After 5 failed attempts (default), status set to DeadLetter; no further retries. Logged with EventId, EventType, CompanyId, CorrelationId, Attempts, LastError.
- **Poison handling:** Unknown event type or deserialization failure in the worker marks the event as failed (or can be marked processed with error) to avoid infinite retry; see worker log “Could not deserialize event”.

---

## 5. Idempotency

- Unchanged from Phase 1–3: **EventProcessingLog** (EventId, HandlerName) with states Processing | Completed | Failed.
- **TryClaimAsync** before each handler; **MarkCompletedAsync** / **MarkFailedAsync** after.
- Replay, retry, and concurrent workers do not duplicate handler side effects for the same (EventId, HandlerName).

---

## 6. Configuration

**Section:** `EventBus:Dispatcher`

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| PollingIntervalSeconds | int | 15 | Seconds between dispatcher poll cycles. |
| BatchSize | int | 20 | Max events to claim per cycle (capped 1–100). |
| MaxRetriesBeforeDeadLetter | int | 5 | After this many failures, status set to DeadLetter. |
| RetryDelaySeconds | int[] | [60,120,300,600,900] | Delay in seconds per retry attempt (index 0 = first retry). |
| StuckProcessingTimeoutMinutes | int | 15 | Events in Processing longer than this are reset to Failed (NextRetryAtUtc = now) for recovery after crash/termination. |

Example `appsettings.json`:

```json
{
  "EventBus": {
    "Dispatcher": {
      "PollingIntervalSeconds": 15,
      "BatchSize": 20,
      "MaxRetriesBeforeDeadLetter": 5,
      "StuckProcessingTimeoutMinutes": 15
    }
  }
}
```

---

## 7. Operational considerations

- **Single transaction:** Workflow and event(s) commit together. If SaveChanges fails, nothing is committed; no orphan events.
- **Dispatcher dependency:** Events are processed only when the dispatcher hosted service is running. Ensure the process (or at least one instance) runs the dispatcher.
- **Concurrency:** Multiple app instances can run the dispatcher; **FOR UPDATE SKIP LOCKED** prevents double-processing of the same event.
- **Stuck Processing recovery:** At the start of each poll cycle, the dispatcher calls `ResetStuckProcessingAsync(StuckProcessingTimeoutMinutes)`. Events in Processing longer than that (or with no `ProcessingStartedAtUtc` and old `CreatedAtUtc`, for legacy rows) are set to Failed with `NextRetryAtUtc = now` and become eligible for re-claim. Structured logs record each reset. Duplicate processing is prevented by the idempotency guard.
- **Company scoping:** EventStore and handlers remain company-scoped where applicable; filters and permissions unchanged.

---

## 8. Limitations and next steps

- **Event versioning:** `PayloadVersion` is present on the entity but not yet used by serialization or handlers; use for future contract versioning.
- **Phase 5 (Operational Observability):** Health check (`/health`), metrics (counters and gauges), event lag monitoring, dead-letter inspection APIs, safe replay. See **docs/operations/EVENT_BUS_OPERATIONS.md**.
- **Removed legacy note:** No dedicated health check for “dispatcher running” or “pending event count”; can be added to existing health endpoint.
- **Metrics:** Only structured logs today; optional: expose counters for published, processed, failed, dead-lettered.

---

## 9. Files touched (Phase 4)

| Area | File | Change |
|------|------|--------|
| Domain | Events/EventStoreEntry.cs | NextRetryAtUtc, PayloadVersion. |
| Domain | Events/IEventStore.cs | AppendInCurrentTransaction, ClaimNextPendingBatchAsync. |
| Application | Events/EventBusDispatcherOptions.cs | New options class. |
| Application | Events/EventStoreDispatcherHostedService.cs | New background worker. |
| Application | Events/IDomainEventDispatcher.cs | PublishAsync(..., alreadyStored). |
| Application | Events/DomainEventDispatcher.cs | alreadyStored: skip append and MarkAsProcessing. |
| Application | Workflow/Services/WorkflowEngineService.cs | IEventStore; stage events in same transaction; single SaveChanges. |
| Infrastructure | Persistence/EventStoreRepository.cs | AppendInCurrentTransaction, ClaimNextPendingBatchAsync, NextRetryAtUtc in MarkProcessedAsync, retry/dead-letter logging. |
| Infrastructure | Persistence/Configurations/Events/EventStoreEntryConfiguration.cs | PayloadVersion, index Status+NextRetryAtUtc. |
| Infrastructure | Persistence/Migrations | AddEventStorePhase4NextRetryAndVersion. |
| API | Program.cs | EventBusDispatcherOptions, EventStoreDispatcherHostedService. |
| Tests | Workflow/WorkflowEngineServiceTests.cs | ExecuteTransitionAsync_WhenEventStoreRegistered_StagesEventsInSameTransaction; CreateServiceWithEventStore. |
| Tests | Events/EventBusIdempotencyGuardTests.cs, ReplayExecutionLockStoreTests.cs | Use SQLite in-memory (relational provider) so ExecuteUpdateAsync is supported. |
| Docs | EVENT_BUS_PHASE4_PRODUCTION.md | This document. |

Existing workflow behaviour (guards, side effects, status transitions) is unchanged; only the event emission path is durable and processed asynchronously by the dispatcher.
