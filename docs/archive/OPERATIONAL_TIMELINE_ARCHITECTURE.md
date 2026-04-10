# Operational Timeline Architecture

**Purpose:** Describe the unified operational trace model, data sources, and how the timeline is assembled for root-cause investigation.

---

## 1. Trace Model

### 1.1 Timeline Item (TraceTimelineItemDto)

A single entry on the operational timeline. Each item has:

| Field | Description |
|-------|-------------|
| **TimestampUtc** | When the activity occurred (UTC). |
| **CorrelationId** | Links workflow, events, and job runs. |
| **CompanyId** | Tenant scope. |
| **ItemType** | Stable type (see below). |
| **Status** | e.g. Pending, Running, Succeeded, Failed, Processed, DeadLetter. |
| **Source** | e.g. WorkflowEngine, EventBus, Scheduler, Retry. |
| **EntityType / EntityId** | Business entity context (Order, Assurance, etc.). |
| **Title** | Short display title. |
| **Summary / DetailSummary** | Optional detail or error message. |
| **RelatedId** | Primary ID of the record (EventId, JobRunId, WorkflowJobId). |
| **RelatedIdKind** | "Event", "JobRun", "WorkflowJob". |
| **ParentRelatedId** | Parent record (ParentJobRunId, ParentEventId). |
| **ActorUserId** | User who initiated (when available). |
| **HandlerName** | Event handler name (when applicable). |

### 1.2 Item Types (TraceTimelineItemTypes)

- **Workflow:** WorkflowTransitionRequested, WorkflowTransitionStarted, WorkflowTransitionCompleted  
- **Events:** EventEmitted, EventProcessed  
- **Event handlers:** EventHandlerStarted, EventHandlerSucceeded, EventHandlerFailed  
- **Background jobs:** BackgroundJobQueued, BackgroundJobStarted, BackgroundJobCompleted, BackgroundJobFailed  
- **Replay/retry (when audit exists):** ReplayRequested, ReplayExecuted, ManualRetryRequested  

### 1.3 Response (TraceTimelineDto)

- **LookupKind / LookupValue** — How the timeline was requested (e.g. CorrelationId, EventId).  
- **Items** — Chronologically ordered timeline items.  
- **TotalCount, Page, PageSize** — Set when `limit` is used for pagination.

---

## 2. Data Sources

The timeline is a **derived read model**. No separate event-sourcing write path is used. Data is read from:

| Source | Tables | What is turned into timeline items |
|--------|--------|-----------------------------------|
| **Workflow** | WorkflowJobs | Requested, Started, Completed (by CreatedAt, StartedAt, CompletedAt). |
| **Events** | EventStore (EventStoreEntry) | EventEmitted (OccurredAtUtc), EventProcessed (ProcessedAtUtc). |
| **Job runs** | JobRuns | EventHandler/BackgroundJob Started and Succeeded/Failed (StartedAtUtc, CompletedAtUtc). |
| **Background job queued** | JobRuns + BackgroundJobs | When a JobRun has BackgroundJobId, a BackgroundJobQueued item is added using BackgroundJob.CreatedAt. |

Replay/retry: Event replay and retry do not write dedicated audit rows today; only job retry is visible via a new JobRun with ParentJobRunId.

---

## 3. Linkage

- **CorrelationId** — Main link. Set from HTTP (X-Correlation-Id), propagated to WorkflowJob and domain events; EventStore and in-process handler JobRuns share it. Async event-handler JobRuns use BackgroundJob.Id as CorrelationId, so they are reached via EventId, not via the same correlation chain.  
- **EventId** — JobRun.EventId links a run to an event.  
- **ParentJobRunId / ParentEventId** — Explicit parent links for retries and child flows.  
- **EntityType + EntityId** — Used for entity-centric lookup across WorkflowJobs, EventStore, and JobRuns.

---

## 4. Lookup Methods

| Lookup | API (TraceController) | API (OperationalTraceController) | Behavior |
|--------|------------------------|-----------------------------------|----------|
| By CorrelationId | GET /api/trace/correlation/{id} | GET /api/operational-trace/by-correlation/{id} | All items with that CorrelationId; company-scoped. |
| By EventId | GET /api/trace/event/{id} | GET /api/operational-trace/by-event/{id} | Event + same CorrelationId chain, or event + JobRuns by EventId. |
| By JobRunId | GET /api/trace/jobrun/{id} | GET /api/operational-trace/by-job-run/{id} | Job run + same CorrelationId chain, or run + event + BackgroundJobQueued. |
| By WorkflowJobId | GET /api/trace/workflowjob/{id} | GET /api/operational-trace/by-workflow-job/{id} | Workflow job + same CorrelationId chain, or single job. |
| By Entity | GET /api/trace/entity?entityType=&entityId= | GET /api/operational-trace/by-entity?entityType=&entityId= | All items for that EntityType+EntityId; company-scoped. |

Optional query: **limit** (max 2000) to paginate; response includes TotalCount, Page, PageSize when limit is applied.

---

## 5. Company Scoping

All queries accept a scope company ID (from the current user). When set:

- WorkflowJobs, EventStore, JobRuns are filtered by CompanyId.  
- SuperAdmin can pass null to see all tenants.

---

## 6. Metrics

**GET /api/trace/metrics** (and **GET /api/operational-trace/metrics**) returns TraceMetricsDto for a time window (default last 24h):

- FailedEventsCount, DeadLetterEventsCount  
- FailedJobRunsCount, DeadLetterJobRunsCount  
- CorrelationChainsWithFailuresCount (distinct correlation IDs with at least one failed/dead-letter event or job run)

Same company scoping applies.

---

## 7. Implementation Notes

- **TraceQueryService** (Application) builds the timeline from WorkflowJobs, EventStore, JobRuns, and BackgroundJobs.  
- **TraceController** and **OperationalTraceController** (Api) expose the same logic under /api/trace and /api/operational-trace.  
- No new persistence is required for the timeline; it is assembled on read.  
- For limitations (e.g. async handler correlation, replay audit), see docs/OPERATIONAL_TIMELINE_AUDIT.md.

---

## 8. Limitations and Follow-up

- **HTTP request started** — Not persisted; no timeline row.  
- **Event replay/retry** — No dedicated audit; only logs. Optional: small ReplayAudit table.  
- **Async handler correlation** — EventHandlingAsync JobRuns use job.Id as CorrelationId; chain is joined via EventId.  
- **End-to-end duration** — Can be computed from first/last item in a correlation chain for future metrics.
