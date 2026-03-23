# Event Bus — Phases 5–7 Summary (Subscribers, Async, Retry, Replay)

**Date:** 2026-03-09  
**Context:** Phases 1–4 (domain events, event store, correlation, workflow emission) are in place. This document summarizes Phase 5–7 additions.

---

## 1. What Was Added

### Phase 5 — Event Subscribers & Async Handling

- **Handler model:** All handlers remain **in-process** by default. Multiple handlers per event type are supported (existing behavior).
- **IAsyncEventSubscriber&lt;TEvent&gt;:** Marker interface for handlers that are intended to run asynchronously (e.g. via background job) when async dispatch is enabled. Current dispatcher does not enqueue; it runs all handlers in-process. **Async enqueue** can be added later by checking for `IAsyncEventSubscriber` and enqueuing a job (JobType e.g. `EventHandlingAsync`) instead of invoking in-process.
- **Execution mode:** In-process = lightweight, immediate; async (future) = durable, via BackgroundJobProcessorService with correlation and JobRun.
- **Idempotency:** Handlers must remain idempotent and retry-safe; failed subscriber execution is observable via JobRun (existing) and EventStore status (Failed/DeadLetter).

### Phase 6 — Retry, Dead-Letter, Replay Safety

- **Retry policy:** Already present: `EventStoreRepository.MaxRetriesBeforeDeadLetter = 5`; after 5 failures status becomes DeadLetter.
- **EventStore read:** `IEventStore.GetByEventIdAsync` for replay and single-event access.
- **Query service:** `IEventStoreQueryService` / `EventStoreQueryService` — list events with filters (company, eventType, status, correlationId, entityType, entityId, date range), get by id, dashboard metrics.
- **Replay service:** `IEventReplayService` / `EventReplayService` — **RetryAsync** (re-dispatch stored event to current handlers), **ReplayAsync** (same but gated by replay policy).
- **API:** `EventStoreController` (`/api/event-store`):
  - `GET /events` — list with filters (companyId, eventType, status, correlationId, entityType, entityId, fromUtc, toUtc, page, pageSize)
  - `GET /events/failed` — list Failed
  - `GET /events/dead-letter` — list DeadLetter
  - `GET /events/{eventId}` — get detail (including payload)
  - `GET /dashboard` — metrics (events today, processed %, failed %, dead-letter count, top failing types/companies)
  - `POST /events/{eventId}/retry` — retry (requires JobsAdmin)
  - `POST /events/{eventId}/replay` — replay if allowed by policy (requires JobsAdmin)
  - `GET /replay-policy/{eventType}` — check if event type is allowed for replay
- **Authorization:** List/detail/dashboard = `JobsView`; retry/replay = `JobsAdmin`. Company scope applied for non–global admins.

### Phase 7 — Event Replay Engine

- **IEventReplayPolicy** / **EventReplayPolicy:** Safe event types (e.g. `WorkflowTransitionCompleted`) are allowed for replay; others are blocked. No full event sourcing; operational replay only.
- **IEventTypeRegistry** / **EventTypeRegistry:** Maps event type name → .NET type for deserializing stored payload when replaying. Register new event types in the static ctor.
- **Replay flow:** Load event from store → check policy (for ReplayAsync) → deserialize payload → MarkAsProcessingAsync → DispatchToHandlersAsync → dispatcher updates MarkProcessedAsync. CorrelationId is preserved (same event payload).
- **Replay audit:** InitiatedByUserId is passed to the replay service and logged; no separate replay audit table in this phase.

---

## 2. Execution Model

- **Publish:** Append to EventStore → MarkAsProcessing → dispatch to all in-process handlers (each optionally wrapped in JobRun) → MarkProcessed.
- **Retry/Replay:** Load event by id → (Replay only: check policy) → deserialize → MarkAsProcessing → DispatchToHandlersAsync (no append) → MarkProcessed (dispatcher).

---

## 3. Retry / Dead-Letter Rules

- **RetryCount** incremented on each handler failure (in MarkProcessedAsync(success: false)).
- **Status:** Pending → Processing → Processed | Failed | DeadLetter.
- **DeadLetter:** When RetryCount ≥ 5. Manual retry via API resets processing (re-dispatch) and may increment RetryCount again if handlers fail.
- **Manual retry:** Authorized via JobsAdmin; InitiatedByUserId logged; original event row is not deleted (append-only); CorrelationId and ParentEventId preserved.

---

## 4. Replay Policy and Safe vs Unsafe Event Types

- **Safe (allowed for replay):** `WorkflowTransitionCompleted` (handlers are log/audit; idempotent).
- **Unsafe/blocked:** Any type not in the allowed set. Add new safe types to `EventReplayPolicy.AllowedForReplay`; add explicitly blocked types to `BlockedForReplay` if needed.
- **Replay** = policy-checked dispatch from store; **Retry** = same dispatch without policy check (for operators who accept responsibility).

---

## 5. Operational Guidance

- Use **list failed** and **list dead-letter** to find events that need attention.
- Use **retry** for one-off re-dispatch; use **replay** when you want policy to block unsafe types.
- Dashboard metrics support date range and company scope.
- Link event to JobRuns via **CorrelationId** (same value on EventStore and JobRun for handler runs).

---

## 6. Migration Notes

- No new migrations in Phase 5–7. EventStore and JobRun schemas unchanged.
- New services: EventStoreQueryService, EventReplayService, EventReplayPolicy, EventTypeRegistry. All registered in Program.cs.

---

## 7. Files Added / Changed

| Area | File | Description |
|------|------|-------------|
| Domain | Events/IEventStore.cs | Added GetByEventIdAsync. |
| Infrastructure | EventStoreRepository.cs | Implemented GetByEventIdAsync. |
| Application | Events/DTOs/EventStoreQueryDto.cs | Filter, list item, detail, dashboard DTOs. |
| Application | Events/IEventStoreQueryService.cs, EventStoreQueryService.cs | Query and dashboard. |
| Application | Events/Replay/IEventReplayPolicy.cs, EventReplayPolicy.cs | Replay allowed/blocked. |
| Application | Events/Replay/IEventTypeRegistry.cs, EventTypeRegistry.cs | Event type → Type, Deserialize. |
| Application | Events/Replay/IEventReplayService.cs, EventReplayService.cs | Retry/Replay. |
| Application | Events/IAsyncEventSubscriber.cs | Marker for async handlers (future). |
| Api | Controllers/EventStoreController.cs | List, failed, dead-letter, detail, dashboard, retry, replay, replay-policy. |
| Api | Program.cs | Registered query, replay policy, type registry, replay service. |
| Tests | Events/EventReplayTests.cs | Replay policy and event type registry tests. |
| Docs | EVENT_BUS_PHASE5_7_SUMMARY.md, EVENT_REPLAY_POLICY.md | New. |

---

## 8. Follow-Up Ideas

- **Async subscribers:** When a handler implements `IAsyncEventSubscriber<T>`, enqueue a background job (e.g. JobType `EventHandlingAsync`) with payload { eventId, handlerName, correlationId }; add processor case to run handler and record JobRun.
- **EventId on JobRun:** Optional column for direct link from JobRun to EventStore (currently linked via CorrelationId).
- **Admin UI:** Frontend Event Bus Monitor (tables, detail drawer, retry/replay buttons, filters) consuming the new API.
- **Replay audit table:** Optional table (ReplayAttemptId, EventId, InitiatedByUserId, AtUtc, Result) for compliance.
