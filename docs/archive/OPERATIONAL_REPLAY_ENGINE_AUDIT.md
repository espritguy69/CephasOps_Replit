# Operational Replay Engine — Phase 1 Audit

**Purpose:** Document current replay capabilities and identify what is reusable for the Operational Replay Engine (batch/filtered replay). No code changes in this phase.

---

## 1. Current Single-Event Replay

### 1.1 IEventReplayService / EventReplayService

- **Location:** `CephasOps.Application/Events/Replay/IEventReplayService.cs`, `EventReplayService.cs`
- **Responsibilities:**
  - **RetryAsync(eventId, scopeCompanyId, initiatedByUserId):** Re-dispatch a stored event by ID **without** policy check. Used for failed/dead-letter retry.
  - **ReplayAsync(eventId, scopeCompanyId, initiatedByUserId):** Re-dispatch only if the event type is allowed by `IEventReplayPolicy`; otherwise returns `BlockedReason`.
- **Flow:**
  1. Load event via `IEventStore.GetByEventIdAsync(eventId)`.
  2. Enforce company scope: if `scopeCompanyId` is set, event must belong to that company.
  3. For replay: call `_replayPolicy.IsReplayAllowed(entry.EventType)`; if false, return blocked.
  4. Deserialize payload via `IEventTypeRegistry.Deserialize(eventType, payload)`.
  5. `MarkAsProcessingAsync(eventId)` then `IDomainEventDispatcher.DispatchToHandlersAsync(domainEvent)` (in-process, no background job).
  6. No explicit update to EventStore status after dispatch in the service (handlers/processor may call `MarkProcessedAsync` elsewhere; single-event replay path does not).
- **Result:** `EventReplayResult` with `Success`, `ErrorMessage`, `BlockedReason`.

**Reusable for batch:** Same dispatch pipeline (load → scope → policy → deserialize → dispatch) can be invoked per event in a batch. Policy and type registry are shared.

### 1.2 IEventReplayPolicy / EventReplayPolicy

- **Location:** `CephasOps.Application/Events/Replay/IEventReplayPolicy.cs`, `EventReplayPolicy.cs`
- **Contract:**
  - `IsReplayAllowed(string eventType):` true only for explicitly allowed types.
  - `IsReplayBlocked(string eventType):` true if type is in blocked set **or** not in allowed set (default deny).
- **Current configuration:**
  - **Allowed:** `WorkflowTransitionCompleted` only (idempotent handlers: log/audit).
  - **Blocked:** explicit set is empty; any type not in Allowed is considered blocked via `IsReplayBlocked`.
- **Lifetime:** Singleton.

**Reusable for batch:** Policy is per-event-type; batch replay can call it for each event. **Missing for operational replay:** no notion of max replay window, max replay count per request, blocked companies, or destructive vs non-destructive classification at policy level.

### 1.3 EventStoreController Replay Endpoints

- **Location:** `CephasOps.Api/Controllers/EventStoreController.cs`
- **Endpoints:**
  - `POST api/event-store/events/{eventId}/retry` — retry (no policy). Permission: `JobsAdmin`.
  - `POST api/event-store/events/{eventId}/replay` — replay (policy enforced). Permission: `JobsAdmin`.
  - `GET api/event-store/replay-policy/{eventType}` — returns `{ eventType, allowed, blocked }`. Permission: `JobsView`.
- **Scoping:** `ScopeCompanyId()`: null for super-admin, else `_currentUser.CompanyId`. All replay/retry and list operations respect this.

**Reusable for batch:** Same auth (Jobs/JobsAdmin) and company scoping pattern. New endpoints for “preview” and “execute replay request” will follow the same pattern.

### 1.4 EventTypeRegistry

- **Location:** `CephasOps.Application/Events/Replay/IEventTypeRegistry.cs`, `EventTypeRegistry.cs`
- **Role:** Maps event type name → .NET type; deserializes payload JSON to `IDomainEvent` for replay.
- **Registered type:** `WorkflowTransitionCompleted` → `WorkflowTransitionCompletedEvent`.
- **Lifetime:** Singleton.

**Reusable for batch:** Same registry used when replaying each event in a batch; no change needed for operational replay.

### 1.5 EventStore / EventStoreEntry

- **Location:** `CephasOps.Domain/Events/IEventStore.cs`, `EventStoreEntry.cs`; persistence in `EventStoreRepository`, `ApplicationDbContext.EventStore`.
- **EventStoreEntry:** EventId, EventType, Payload, OccurredAtUtc, CreatedAtUtc, Status, CorrelationId, CompanyId, EntityType, EntityId, etc. Append-only; only processing metadata is updated.
- **IEventStore:** Append, GetByEventIdAsync, MarkAsProcessingAsync, MarkProcessedAsync. No bulk get by filter; listing is via `IEventStoreQueryService` and `ApplicationDbContext.EventStore`.

**Reusable for batch:** Filtered listing already exists via `IEventStoreQueryService.GetEventsAsync(EventStoreFilterDto, scopeCompanyId)`. For batch replay we need either a method that returns event IDs (or entries) matching a filter with a cap (e.g. MaxEvents), or we use the existing query with a large page size and iterate. EventStore rows are **not** mutated for “replay” semantics (replay re-dispatches; it does not overwrite history).

### 1.6 JobRun / EventStore Observability Linkage

- **Location:** `IEventStoreQueryService.GetRelatedLinksAsync(eventId, scopeCompanyId)` in `EventStoreQueryService.cs`.
- **Behavior:** For a given event, returns:
  - **JobRuns** where `EventId == eventId` or `CorrelationId == entry.CorrelationId`, company-scoped.
  - **WorkflowJobs** where `CorrelationId == entry.CorrelationId`, company-scoped.
- **Usage:** Event Store UI shows “related” JobRuns and WorkflowJobs for traceability. Background job processor creates JobRuns when processing EventHandlingAsync jobs; single-event replay goes through `DispatchToHandlersAsync` in-process and may not create the same JobRun trail unless explicitly integrated.

**Reusable for batch:** Same linkage can be used for “replay operation detail” (e.g. show JobRuns created during a replay run). **Missing:** Replay operations are not yet a first-class entity (ReplayOperationId) with their own audit record and optional child JobRuns/correlation.

---

## 2. Existing Query and Filter Model

- **EventStoreFilterDto** (Application/Events/DTOs/EventStoreQueryDto.cs): CompanyId, EventType, Status, CorrelationId, EntityType, EntityId, FromUtc, ToUtc, Page, PageSize.
- **EventStoreQueryService.GetEventsAsync:** Applies filter + company scope, returns paged list and total count. Uses `OccurredAtUtc` for FromUtc/ToUtc.

**Reusable for batch:** Same filters (CompanyId, EventType, Status, FromUtc, ToUtc, EntityType, EntityId, CorrelationId) are natural for a “replay request.” We need to add MaxEvents (and possibly DryRun, RequestedBy, RequestedAt, ReplayReason) on the request model; the existing filter DTO can be extended or a new replay-specific request DTO can mirror it.

---

## 3. Safeguards Already in Place

| Safeguard | Where | Notes |
|-----------|--------|------|
| Default-deny replay policy | EventReplayPolicy | Only explicitly allowed event types can be replayed; blocked set or “not allowed” → blocked. |
| Company scoping | EventStoreController, EventStoreQueryService | Non–global admins are restricted to their company for list, get, retry, replay. |
| Permission gating | EventStoreController | JobsView for read; JobsAdmin for retry/replay. |
| Single-event scope check | EventReplayService.DispatchStoredEventAsync | Event’s CompanyId must match scopeCompanyId when provided. |
| No overwrite of EventStore | Design | Replay only re-dispatches; it does not modify or delete existing EventStore rows. |

---

## 4. Gaps for Operational Replay

- **No batch/filtered replay:** Only single-event by ID. Need: request model with filters (e.g. CompanyId, EventType, Status, FromOccurredAtUtc, ToOccurredAtUtc, EntityType, EntityId, CorrelationId, MaxEvents), and an execution engine that selects and replays eligible events in batches.
- **No dry-run preview:** No way to see “what would be replayed” (counts, sample events, blocked reasons) without executing handlers.
- **No replay-specific policy extensions:** No max replay window (e.g. “only last 7 days”), max replay count per request, blocked companies, or explicit “destructive” vs “non-destructive” event classification in policy.
- **No replay audit trail:** No persisted ReplayOperation (requested by, filters, dry-run vs execute, total matched/eligible/executed/succeeded/failed, correlation). No per-event replay attempt records if needed.
- **No operational replay API:** Only single-event retry/replay and replay-policy check. Need: preview, execute replay request, list replay operations, get replay operation details/results.
- **No child execution context:** Single-event replay does not set a “replay” correlation or parent reference; for observability we want replay runs to be tied to a ReplayOperationId and optionally to a parent correlation.
- **No batch size / stop-on-policy-violation:** No configurable batch size or “stop on first policy violation” for safe operational runs.
- **Unsafe types not explicitly blocked in list:** Policy blocks by “not allowed”; an explicit block list for known destructive types (and default deny) would make operational replay safer and clearer.

---

## 5. Summary

| Area | Single-Event Replay Today | Reusable for Batch | Missing for Operational Replay |
|------|---------------------------|--------------------|---------------------------------|
| Replay service | Retry + Replay by event ID, policy on replay | Same dispatch + policy + registry per event | Batch iteration, dry-run, audit context |
| Policy | Allowed/blocked by event type, default deny | Per-event-type check | Max window, max count, blocked companies, destructive flag |
| API | Retry/Replay by ID, replay-policy by type | Auth and company scoping | Preview, execute request, list/get replay operations |
| Query/filter | EventStoreFilterDto + GetEventsAsync | Same filters + scope | MaxEvents, dry-run, requested-by, reason; dedicated replay request model |
| EventStore | GetByEventIdAsync, no overwrite | Filtered read path for batch | Bulk get by filter (or use existing paged query with cap) |
| Observability | Related links (JobRuns, WorkflowJobs) by event | Link replay runs to operation | ReplayOperation entity, replay correlation, per-event attempt audit |
| Safeguards | Company scope, JobsAdmin, policy default deny | Keep all | Stricter policy for batch, dry-run, audit trail |

This audit provides the basis for Phase 2 (replay request model) through Phase 10 (documentation) without modifying existing single-event replay behavior.
