# Operational Timeline — Traceability Audit

**Purpose:** Document existing traceability between WorkflowJob, EventStore, JobRun, HTTP correlation, background jobs, event handlers, and replay/retry so we can extend the system safely for a unified operational timeline.

**Date:** 2026-03-09

---

## 1. Existing Traceability Overview

### 1.1 Correlation ID Flow

| Layer | Where set | Where stored | Notes |
|-------|-----------|--------------|--------|
| **HTTP** | `CorrelationIdMiddleware`: from `X-Correlation-Id` or `Request-Id` header, or generated GUID | `HttpContext.Items["CorrelationId"]`, Serilog `LogContext`, response header `X-Correlation-Id` | Not persisted. No "HTTP request started" record. |
| **Workflow** | `WorkflowController.ExecuteTransition`: `dto.CorrelationId ??= _correlationIdProvider.GetCorrelationId()` | `WorkflowJob.CorrelationId` | Direct: request → workflow job. |
| **Event** | Workflow engine / event source sets from domain event | `EventStoreEntry.CorrelationId`, `DomainEvent.CorrelationId` | Propagated when event is appended; source sets it (e.g. from WorkflowJob). |
| **Event handler (in-process)** | From domain event | `JobRun.CorrelationId` via `JobRunRecorderForEvents` (`domainEvent.CorrelationId`) | Direct: same correlation as event. |
| **Event handler (async)** | Payload has `correlationId` in BackgroundJob payload | **JobRun gets `CorrelationId = job.Id`** in `BackgroundJobProcessorService` | **Gap:** Async handler JobRuns are correlated by BackgroundJob.Id, not by event CorrelationId. Timeline by CorrelationId will not include these JobRuns; they are only reachable via EventId. |
| **Background job (non-event)** | Schedulers set `CorrelationId = Guid.NewGuid()` per job | `JobRun.CorrelationId = job.Id` when processor starts run | No link to HTTP or workflow; correlation is job-centric. |

### 1.2 Direct Links (FK or explicit ID)

| From | To | Link type | Field(s) |
|------|-----|-----------|----------|
| **JobRun** | EventStore | Optional FK-style | `JobRun.EventId` → `EventStoreEntry.EventId` |
| **JobRun** | JobRun (retry) | Optional parent | `JobRun.ParentJobRunId` |
| **JobRun** | BackgroundJob | Optional | `JobRun.BackgroundJobId` → `BackgroundJob.Id` |
| **EventStoreEntry** | EventStoreEntry (child) | Optional | `EventStoreEntry.ParentEventId` |
| **BackgroundJob** | JobRun (retried from) | Optional | `BackgroundJob.RetriedFromJobRunId` (used when creating new JobRun as `ParentJobRunId`) |

There is **no** direct FK from WorkflowJob to EventStore or JobRun; linkage is via **CorrelationId** only.

### 1.3 Inferred Links (same CorrelationId / EventId / Entity)

| Lookup | How inferred |
|--------|----------------|
| WorkflowJob ↔ EventStore | Same `CorrelationId` |
| WorkflowJob ↔ JobRun | Same `CorrelationId` (with async-handler gap above) |
| EventStore ↔ JobRun | Same `EventId` (direct) or same `CorrelationId` |
| Entity-centric | `WorkflowJob.EntityType`+`EntityId`, `EventStoreEntry.EntityType`+`EntityId`, `JobRun.RelatedEntityType`+`RelatedEntityId` |

### 1.4 Existing Timeline Implementation

- **TraceQueryService** already builds a unified timeline from WorkflowJob, EventStore, and JobRun.
- **Lookups:** By CorrelationId, EventId, JobRunId, WorkflowJobId, EntityType+EntityId.
- **Company scoping:** Applied on all queries when `scopeCompanyId` is set.
- **TraceController** exposes: `GET /api/trace/correlation/{id}`, `event/{eventId}`, `jobrun/{jobRunId}`, `workflowjob/{id}`, `entity?entityType=&entityId=`.
- **TraceTimelineItemDto** has: TimestampUtc, CorrelationId, CompanyId, ItemType, Status, Source, EntityType, EntityId, Title, Summary, RelatedId, RelatedIdKind, ParentRelatedId, ActorUserId, HandlerName.

---

## 2. Item Types and Sources Today

| ItemType | Source entity | Timestamps used |
|----------|----------------|-----------------|
| WorkflowTransitionRequested | WorkflowJob | `CreatedAt` |
| WorkflowTransitionStarted | WorkflowJob | `StartedAt` |
| WorkflowTransitionCompleted | WorkflowJob | `CompletedAt` |
| EventEmitted | EventStoreEntry | `OccurredAtUtc` |
| EventProcessed | EventStoreEntry | `ProcessedAtUtc` |
| EventHandlerStarted / EventHandlerCompleted | JobRun (EventId set) | `StartedAtUtc`, `CompletedAtUtc` |
| BackgroundJobStarted / BackgroundJobCompleted | JobRun (no EventId) | `StartedAtUtc`, `CompletedAtUtc` |

---

## 3. Missing Timeline Data

| Missing item | Reason |
|--------------|--------|
| **HTTP request started** | No persisted request log; correlation exists only in memory and logs. |
| **Background job queued** | BackgroundJob.CreatedAt exists but is not currently mapped into the trace timeline. JobRun is created when the job *starts*, not when queued. |
| **Replay requested** | EventReplayService.RetryAsync/ReplayAsync do not write any audit record; only log. |
| **Replay executed** | Same; no stored "replay completed" or "replay failed" record. |
| **Manual retry requested (job)** | Retry creates a new BackgroundJob/JobRun with ParentJobRunId; no separate "retry requested" audit row. The new JobRun with TriggerSource "Retry" is the only trace. |

---

## 4. Missing or Inconsistent Timestamps

| Entity | Field | Notes |
|--------|--------|--------|
| WorkflowJob | CreatedAt, StartedAt, CompletedAt | From CompanyScopedEntity/own props; all present. |
| EventStoreEntry | OccurredAtUtc, CreatedAtUtc, ProcessedAtUtc, LastErrorAtUtc | Present. |
| JobRun | StartedAtUtc, CompletedAtUtc, CreatedAtUtc | Present. |
| BackgroundJob | CreatedAt, StartedAt, CompletedAt | Present but **not** used in timeline today (no "job queued" item). |

No critical timestamp is missing for the existing three sources; the main gap is **not using** BackgroundJob.CreatedAt for a "BackgroundJobQueued" timeline item.

---

## 5. Missing Actor / Source Metadata

| Area | Current state | Gap |
|------|----------------|-----|
| WorkflowJob | InitiatedByUserId | Present and mapped to ActorUserId. |
| EventStoreEntry | TriggeredByUserId, Source | Present; Source and TriggeredByUserId mapped. |
| JobRun | InitiatedByUserId, TriggerSource | Present; TriggerSource mapped as Source. |
| Replay/Retry | initiatedByUserId passed to EventReplayService | Not persisted; no timeline row for "replay requested by user X". |
| Event handler name | EventStoreEntry.LastHandler | Present and mapped to HandlerName on EventProcessed item. JobRun does not store handler name; handler is only on EventStoreEntry. |

---

## 6. Duplication Risks

| Risk | Mitigation in place / recommendation |
|------|--------------------------------------|
| Same WorkflowJob yields 1–3 items (Requested, Started, Completed) | By design; distinct lifecycle points. |
| Same EventStoreEntry yields 1–2 items (Emitted, Processed) | By design. |
| Same JobRun yields 2 items (Started, Completed) | By design. |
| Event + in-process handler: event and JobRun share CorrelationId | No duplication; different item types and RelatedIdKind. |
| Event + async handler: JobRun has CorrelationId = job.Id | No duplication; that JobRun is not in the same correlation chain as the event (see gap above). |
| Entity-based lookup returns all WorkflowJobs, Events, JobRuns for entity | Could be many items; pagination not yet implemented on timeline. |

---

## 7. Replay and Retry Linkage

| Operation | Where | Persisted linkage |
|-----------|--------|-------------------|
| Event retry (API) | EventStoreController.Retry | None. EventReplayService re-dispatches; EventStoreEntry status/ProcessedAtUtc/LastError may be updated by handler outcome. |
| Event replay (API) | EventStoreController.Replay | None. Same as retry but gated by IEventReplayPolicy. |
| Job retry (API) | BackgroundJobsController retry | New BackgroundJob with RetriedFromJobRunId; on process, new JobRun with ParentJobRunId. Timeline can show parent/child via ParentRelatedId. |

So: **event replay/retry** has no dedicated audit row; **job retry** is visible via new JobRun + ParentJobRunId.

---

## 8. Summary Table

| Link | Direct? | Notes |
|------|---------|--------|
| HTTP → WorkflowJob | Inferred (same request) | CorrelationId set from request; not persisted for HTTP. |
| WorkflowJob → EventStore | Inferred | Same CorrelationId. |
| WorkflowJob → JobRun | Inferred | Same CorrelationId (in-process handlers only; async handlers use job.Id). |
| EventStore → JobRun | Direct + Inferred | EventId on JobRun; also CorrelationId for in-process. |
| JobRun → JobRun | Direct | ParentJobRunId. |
| EventStore → EventStore | Direct | ParentEventId. |
| Replay/Retry (event) | None | No stored record. |
| Retry (job) | Direct | New JobRun.ParentJobRunId. |

---

## 9. Recommendations for Next Phases

1. **Keep** existing TraceQueryService and TraceController as the backbone; extend with additional item types and sources.
2. **Add** timeline items for "BackgroundJobQueued" (from BackgroundJob.CreatedAt) when building by CorrelationId or by Entity; consider storing CorrelationId on BackgroundJob for EventHandlingAsync so async handler runs join the same correlation chain (optional schema change).
3. **Consider** lightweight audit records for "ReplayRequested" / "ReplayExecuted" (e.g. small table or event-store event) if replay visibility on the timeline is required; otherwise leave as log-only.
4. **Pagination:** Add optional limit/offset or cursor to timeline API when returning by entity or by correlation with many items.
5. **API route:** Current API is `/api/trace/*`. Add alias `/api/operational-trace/*` if desired for consistency with naming.

This audit is the basis for Phase 2 (trace model) and Phase 3 (query layer and API) without changing workflow semantics or rebuilding Event Bus / Job Observability.
