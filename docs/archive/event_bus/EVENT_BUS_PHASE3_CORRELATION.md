# Event Bus — Phase 3: Correlation Model

**Date:** 2026-03-09  
**Context:** Phase 1 already introduced correlation along HTTP → Workflow → Event → JobRun. This document records the audit and any Phase 3 completions.

---

## 1. Audit: Where CorrelationId Already Existed

| Component | Location | Notes |
|-----------|----------|--------|
| **HTTP** | `CorrelationIdMiddleware` (Api) | Reads `X-Correlation-Id` or `Request-Id`; generates if missing; sets `HttpContext.Items["CorrelationId"]`; adds header to response; pushes to Serilog `LogContext`. |
| **Request context** | `ICorrelationIdProvider` / `CorrelationIdProvider` (Application / Api) | Returns current correlation ID from `HttpContext.Items` (set by middleware). |
| **Workflow** | `WorkflowController` | Sets `dto.CorrelationId ??= _correlationIdProvider.GetCorrelationId()` before calling engine. |
| **Workflow** | `WorkflowEngineService` | Sets `job.CorrelationId = dto.CorrelationId` on `WorkflowJob`; builds `WorkflowTransitionCompletedEvent` with `CorrelationId = dto.CorrelationId ?? job.CorrelationId`. |
| **WorkflowJob** | Domain entity + EF config | `CorrelationId` (varchar 100, nullable). Migration `AddEventBusCorrelationAndEventStore`. |
| **JobRun** | Domain entity + `StartJobRunDto` | `CorrelationId` on entity and DTO; set by `JobRunRecorder`, `JobRunRecorderForEvents` (from domain event), and background job processors. |
| **EventStore** | `EventStoreEntry` | `CorrelationId` (indexed). Set in `EventStoreRepository.AppendAsync` from `domainEvent.CorrelationId`. |
| **Event handlers** | `JobRunRecorderForEvents` | Passes `domainEvent.CorrelationId` into `StartJobRunDto` when creating JobRuns for handler execution. |
| **Logging** | `GlobalExceptionHandler` | Includes correlation ID from `HttpContext.Items` in error logs. |
| **API responses** | Tests (ApiSmokeTests) | Response header and problem details extensions include correlation ID. |

No separate “correlation context accessor” existed; `ICorrelationIdProvider` is the request-scoped service used at the HTTP boundary.

---

## 2. What Was Added or Confirmed (Phase 3)

### 2.1 End-to-End Flow (Already in Place)

- **HTTP request** → Middleware sets/generates CorrelationId → `HttpContext.Items` and response header.
- **Workflow transition** → Controller sets `dto.CorrelationId` from provider → Engine sets `job.CorrelationId` → Event built with same CorrelationId.
- **Event emission** → `WorkflowTransitionCompletedEvent.CorrelationId` set from DTO/job → **EventStore** row stores it (indexed).
- **Event handling** → Each handler run recorded via `JobRunRecorderForEvents` with `CorrelationId = domainEvent.CorrelationId` → **JobRun** rows have same CorrelationId.
- **Background jobs** → Processors set `StartJobRunDto.CorrelationId` (e.g. from job id or scheduler); no change in Phase 3.

So: **HTTP → Workflow transition → Event → Event handling → JobRun** share the same CorrelationId when the flow starts from an HTTP request.

### 2.2 Parent Event / Parent Job Run

- **ParentEventId** was added to **EventStoreEntry** (Phase 2 migration). Use when an event is spawned from another (e.g. handler publishes a child event); not set by current code.
- **ParentJobRunId** already exists on **JobRun** and **StartJobRunDto**; used for retries and linking runs. No schema change in Phase 3.

### 2.3 No New Correlation Service

- **ICorrelationIdProvider** remains the single service for “current correlation ID” at the request boundary. Event handlers receive the event (with CorrelationId); background jobs set CorrelationId when starting runs. An ambient AsyncLocal-based context was not added; can be added later if needed for non-HTTP flows.

---

## 3. Propagation Rules

1. **Request boundary:** Always set CorrelationId from middleware (or provider) before workflow/API logic.
2. **Workflow:** `ExecuteTransitionDto.CorrelationId` and `WorkflowJob.CorrelationId` must be set from the same source (controller sets dto; engine copies to job).
3. **Events:** Every published event should carry the same CorrelationId as the triggering request/workflow/job when applicable.
4. **Event handlers:** When creating JobRuns for handler execution, use `domainEvent.CorrelationId` so handler runs are part of the same trace.
5. **EventStore:** CorrelationId is stored and indexed; no change to propagation.
6. **Child flows:** When introducing “child” events (e.g. from a handler), set the child event’s CorrelationId to the parent’s (or a new value and set ParentEventId to the parent’s EventId). Prefer reusing CorrelationId unless intentionally starting a new trace.

---

## 4. Schema and Code (Phase 3)

- **EventStore:** CorrelationId and indexes unchanged. ParentEventId added in Phase 2 (see EVENT_BUS_PHASE2_EVENT_STORE.md).
- **WorkflowJobs:** CorrelationId already present (Phase 1).
- **JobRuns:** CorrelationId and ParentJobRunId already present; no migration in Phase 3.

---

## 5. Risks and Assumptions

- **Background jobs:** Schedulers and processors set their own CorrelationId (e.g. `Guid.NewGuid()` or job id). They are not automatically tied to an HTTP request. To link a job to a request, the job must be created with that request’s CorrelationId (e.g. passed in payload or queue metadata).
- **Out-of-process handlers:** If events are later dispatched by a separate worker, that worker must receive or restore CorrelationId (e.g. from the stored event) and set it when creating JobRuns.

---

## 6. Files Touched (Phase 3)

- No new code files; Phase 3 was an audit and documentation pass. Correlation and ParentJobRunId were already implemented; ParentEventId was added in Phase 2 (EventStore).

---

## 7. Implementation Summary (Phase 2 + 3)

### Files added
- `backend/src/CephasOps.Domain/Events/IHasEntityContext.cs`
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260308194528_ExtendEventStorePhase2.cs` (and Designer)
- `docs/EVENT_BUS_PHASE2_EVENT_STORE.md`
- `docs/EVENT_BUS_PHASE3_CORRELATION.md`

### Files changed
- `backend/src/CephasOps.Domain/Events/EventStoreEntry.cs` — added CreatedAtUtc, TriggeredByUserId, Source, EntityType, EntityId, LastError, LastErrorAtUtc, LastHandler, ParentEventId.
- `backend/src/CephasOps.Domain/Events/IEventStore.cs` — added MarkAsProcessingAsync; MarkProcessedAsync(lastHandler).
- `backend/src/CephasOps.Application/Events/WorkflowTransitionCompletedEvent.cs` — implemented IHasEntityContext.
- `backend/src/CephasOps.Application/Events/DomainEventDispatcher.cs` — call MarkAsProcessingAsync after Append; pass lastHandler to MarkProcessedAsync.
- `backend/src/CephasOps.Infrastructure/Persistence/EventStoreRepository.cs` — populate new fields; MarkAsProcessingAsync; LastError/LastHandler in MarkProcessedAsync; IHasEntityContext handling.
- `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Events/EventStoreEntryConfiguration.cs` — new property lengths and IX_EventStore_OccurredAtUtc.
- `backend/tests/CephasOps.Application.Tests/Events/EventBusPhase1Tests.cs` — verify MarkAsProcessingAsync; MarkProcessedAsync 5-arg signature.

### Migrations added
- **ExtendEventStorePhase2** — adds to EventStore: CreatedAtUtc, TriggeredByUserId, Source, EntityType, EntityId, LastError, LastErrorAtUtc, LastHandler, ParentEventId; index on OccurredAtUtc.

### Follow-up for Phase 4 (completed)
- Apply migration `ExtendEventStorePhase2` to databases (`dotnet ef database update`).
- Phase 4 (event emission from workflow transitions) was already implemented in Phase 1; no further code changes required for “emit after transition.” Optional: add query/API for EventStore (by CompanyId, Status, CorrelationId, date range) if operational visibility is needed.
- **Child events:** Implemented. `DomainEvent` has optional `ParentEventId` and implements `IHasParentEvent`. When a handler publishes a follow-up event, set the child’s `CorrelationId = parent.CorrelationId` and `ParentEventId = parent.EventId`; `EventStoreRepository.AppendAsync` persists `ParentEventId` for lineage. Replay and ordering are unchanged (append-only; child events are stored and processed like any other event).
