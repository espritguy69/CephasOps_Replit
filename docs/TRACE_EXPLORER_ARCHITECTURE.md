# Operational Trace Explorer — Architecture

**Date:** 2026-03-09  
**Purpose:** Technical overview of the Trace Explorer: data sources, timeline model, APIs, scoping, and UI integration.

---

## 1. Overview

The **Operational Trace Explorer** gives operations a single place to enter one identifier (CorrelationId, EventId, JobRunId, WorkflowJobId, or Entity) and see the **entire execution chain** in chronological order. It does not replace the Event Bus, Workflow Engine, or Background Jobs; it **unifies** their visibility using the existing correlation model.

- **Backend:** `TraceQueryService` assembles a read-only timeline from existing tables (no new persistence).
- **API:** `TraceController` exposes GET endpoints under `api/trace/`.
- **Frontend:** Admin page at `/admin/trace-explorer` with search, entity lookup, and timeline visualization.
- **Cross-links:** Event Bus Monitor and Background Jobs link to the Trace Explorer with the relevant ID.

---

## 2. Data Sources (no new tables)

Timeline items are **derived** from:

| Source | Table / entity | Used for |
|--------|----------------|----------|
| Workflow | `WorkflowJobs` | Transition requested, started, completed (by CorrelationId, WorkflowJobId, or Entity). |
| Events | `EventStore` (EventStoreEntry) | Event emitted, event processed (by CorrelationId, EventId, or Entity). |
| Jobs | `JobRuns` | Background/handler job started, completed (by CorrelationId, JobRunId, or Entity). |

- **CorrelationId** is the backbone: it links WorkflowJob → EventStore → JobRun when set consistently.
- **EventId** links JobRun to EventStore for event-handler runs.
- **EntityType + EntityId** are used to find all workflow jobs, events, and job runs that reference that entity.

---

## 3. Timeline Read Model

### 3.1 DTOs

- **TraceTimelineDto**
  - `LookupKind`: e.g. `"CorrelationId"`, `"EventId"`, `"JobRunId"`, `"WorkflowJobId"`, `"Entity"`.
  - `LookupValue`: the value used (e.g. correlation string, or GUID).
  - `Items`: list of `TraceTimelineItemDto`, **chronologically ordered** (oldest first).

- **TraceTimelineItemDto**
  - `TimestampUtc`, `CorrelationId`, `CompanyId`
  - `ItemType`, `Status`, `Source`
  - `EntityType`, `EntityId`
  - `Title`, `Summary`
  - `RelatedId`, `RelatedIdKind` (Event | JobRun | WorkflowJob), `ParentRelatedId`
  - `ActorUserId`, `HandlerName` (when available)

### 3.2 Item types

| ItemType | Source | When |
|----------|--------|------|
| WorkflowTransitionRequested | WorkflowJob.CreatedAt | Workflow transition created |
| WorkflowTransitionStarted | WorkflowJob.StartedAt | When present |
| WorkflowTransitionCompleted | WorkflowJob.CompletedAt | When present |
| EventEmitted | EventStore.OccurredAtUtc | Event stored |
| EventProcessed | EventStore.ProcessedAtUtc | When present |
| EventHandlerStarted | JobRun (EventId set) | Job run started |
| EventHandlerCompleted | JobRun.CompletedAtUtc | When present |
| BackgroundJobStarted | JobRun (no EventId) | Job run started |
| BackgroundJobCompleted | JobRun.CompletedAtUtc | When present |

---

## 4. APIs

All trace endpoints:

- **Policy:** `Jobs`
- **Permission:** `JobsView` (RequirePermission)
- **Scoping:** Non–SuperAdmin users are restricted to their `CompanyId`; SuperAdmin has no company filter.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `api/trace/correlation/{correlationId}` | Timeline for all records with that correlation. Always 200; items may be empty. |
| GET | `api/trace/event/{eventId}` | Timeline for that event and related (same correlation or event-only). 404 if event not found or wrong company. |
| GET | `api/trace/jobrun/{jobRunId}` | Timeline for that job run and related. 404 if not found or wrong company. |
| GET | `api/trace/workflowjob/{workflowJobId}` | Timeline for that workflow job and related. 404 if not found or wrong company. |
| GET | `api/trace/entity?entityType=&entityId=` | Timeline for all records referencing that entity. 200; items may be empty. `entityType` required. |

Responses are wrapped in the standard `ApiResponse<TraceTimelineDto>`.

---

## 5. Company scoping (SaaS-ready)

- **SuperAdmin:** `scopeCompanyId = null` → all companies included.
- **Other users:** `scopeCompanyId = currentUser.CompanyId` → only WorkflowJobs, EventStore, and JobRuns for that company are included.
- All lookups (correlation, event, job run, workflow job, entity) apply the same scope. A user cannot see another company’s trace data.

---

## 6. Frontend

- **Page:** `TraceExplorerPage` at route `/admin/trace-explorer`.
- **Access:** Same as Event Bus Monitor / Background Jobs (e.g. `jobs.view` or Admin/SuperAdmin).
- **Search:**
  - **By ID:** Single search box. If value is a GUID, tries EventId → JobRunId → WorkflowJobId; otherwise treats as CorrelationId.
  - **By entity:** Entity type (e.g. Order) + Entity ID (GUID); calls `getTraceByEntity`.
- **Timeline:** List of items with timestamp, status badge, icon by ItemType, title; expandable row for Summary, HandlerName, RelatedId, and links to Event Bus Monitor or Background Jobs.
- **URL params:** Support for deep-linking: `?eventId=`, `?jobRunId=`, `?workflowJobId=`, `?correlationId=`, `?entityType=&entityId=` to open with a pre-run lookup.

---

## 7. Cross-links

- **Event Bus Monitor** (event detail): “View full trace in Trace Explorer” → `/admin/trace-explorer?eventId={eventId}`.
- **Event Bus Monitor** (related workflow jobs table): “View trace” → `/admin/trace-explorer?workflowJobId={id}`.
- **Background Jobs** (job run detail): “View full trace in Trace Explorer” → `/admin/trace-explorer?jobRunId={id}`.

---

## 8. Design constraints (unchanged)

- **No Event Bus or Workflow Engine behaviour changes:** Trace is read-only and additive.
- **Correlation model:** Existing CorrelationId propagation remains the backbone; no new correlation storage.
- **Additive only:** No new persistence for trace; optional minimal enrichment (e.g. HandlerName) from existing columns only.

---

## 9. Related docs

- **Traceability audit (gaps and linkage):** `docs/TRACE_EXPLORER_AUDIT.md`
- **How to use and debug:** `docs/TRACE_EXPLORER_RUNBOOK.md`
- **Event Bus rollout:** `docs/EVENT_BUS_ROLLOUT_READINESS_SUMMARY.md`
