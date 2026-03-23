# SLA Intelligence — Data Audit

**Date:** 2026-03-09  
**Purpose:** Audit existing observability data available for SLA breach detection. No code changes; additive SLA features will consume this data.

---

## 1. Summary

CephasOps already persists **WorkflowJob**, **EventStore**, **JobRun**, and **BackgroundJob** with timestamps suitable for computing durations. The **Trace** system assembles correlation chains from these sources. SLA evaluation can be implemented by querying these tables and computing delays from existing timestamps.

---

## 2. WorkflowJob — Workflow Transition Timestamps

**Source:** `CephasOps.Domain.Workflow.Entities.WorkflowJob` (table `WorkflowJobs`).  
**Company scoping:** `CompanyId` (nullable).  
**Correlation:** `CorrelationId` links to events and job runs.

| Field | Type | Description |
|-------|------|-------------|
| **CreatedAt** | DateTime | When the transition was requested (inherited from CompanyScopedEntity). |
| **StartedAt** | DateTime? | When processing of the transition started. |
| **CompletedAt** | DateTime? | When the transition completed (success or failure). |
| **State** | WorkflowJobState | Pending, Running, Succeeded, Failed. |

**Identifiers:** `Id`, `WorkflowDefinitionId`, `EntityType`, `EntityId`, `CurrentStatus`, `TargetStatus`.

**Delays that can be computed:**

- **Request → completion:** `CompletedAt - CreatedAt` (for Succeeded/Failed jobs).
- **Request → start:** `StartedAt - CreatedAt` (time in queue / time to first action).
- **Start → completion:** `CompletedAt - StartedAt` (actual execution time).
- **Stuck detection:** Jobs in `Running` or `Pending` with `CreatedAt` (or `StartedAt`) older than threshold.

**Missing for SLA (optional enhancements):** None critical. All transition lifecycle points are recorded.

---

## 3. EventStore — Event Processing Timestamps

**Source:** `CephasOps.Domain.Events.EventStoreEntry` (table `EventStore`).  
**Company scoping:** `CompanyId` (nullable).  
**Correlation:** `CorrelationId`; `ParentEventId` for child events.

| Field | Type | Description |
|-------|------|-------------|
| **OccurredAtUtc** | DateTime | When the event logically occurred (business time). |
| **CreatedAtUtc** | DateTime | When the event was appended to the store (persistence time). |
| **ProcessedAtUtc** | DateTime? | When the event was fully processed (all handlers done or failed). |
| **Status** | string | Pending, Processing, Processed, Failed, DeadLetter. |

**Identifiers:** `EventId`, `EventType`, `EntityType`, `EntityId`, `LastHandler`, `LastError`, `LastErrorAtUtc`.

**Delays that can be computed:**

- **Emit → processed:** `ProcessedAtUtc - CreatedAtUtc` (or `OccurredAtUtc`) for Processed/Failed/DeadLetter events — total time from persistence to processing complete.
- **Time in pending:** For `Pending` or `Processing` events, `UtcNow - CreatedAtUtc` (no “processing started” timestamp exists).
- **Stalled events:** Events in `Pending` or `Processing` older than threshold (stuck event chains).

**Missing for SLA:**

- **Processing started:** There is no dedicated `ProcessingStartedAtUtc`. Handler execution start is only visible indirectly via **JobRun** (when EventBus creates a JobRun per handler). So “event processing duration” for SLA is best taken as **CreatedAtUtc → ProcessedAtUtc** (or time-in-pending for unprocessed events). Per-handler duration is in JobRun (see below).

---

## 4. JobRun — Job / Event Handler Execution Timestamps

**Source:** `CephasOps.Domain.Workflow.Entities.JobRun` (table `JobRuns`).  
**Company scoping:** `CompanyId` (nullable).  
**Correlation:** `CorrelationId`; `EventId` links to EventStore when the run is for an event handler; `ParentJobRunId` for retries.

| Field | Type | Description |
|-------|------|-------------|
| **StartedAtUtc** | DateTime | When the job/handler started. |
| **CompletedAtUtc** | DateTime? | When the job/handler completed. |
| **DurationMs** | long? | Stored duration in milliseconds (when completed). |
| **CreatedAtUtc** | DateTime | Record creation (when the run was recorded). |
| **Status** | string | Pending, Running, Succeeded, Failed, Cancelled, Retrying, DeadLetter. |

**Identifiers:** `Id`, `JobName`, `JobType`, `TriggerSource`, `EventId`, `BackgroundJobId`, `RelatedEntityType`, `RelatedEntityId`.

**Delays that can be computed:**

- **Execution duration:** `CompletedAtUtc - StartedAtUtc` or `DurationMs` for completed runs.
- **Stuck runs:** Runs in `Running` (or Pending) with `StartedAtUtc` (or CreatedAtUtc) older than threshold.
- **Event handler duration:** Filter by `EventId != null` and use same duration logic — this is the per-handler processing time for events.

**Missing for SLA:** None critical. Start and completion are both present.

---

## 5. BackgroundJob — Queue and Execution Timestamps

**Source:** `CephasOps.Domain.Workflow.Entities.BackgroundJob` (table `BackgroundJobs`).  
**Company scoping:** None on entity; tenant context can come from JobRun (BackgroundJobId) or payload.  
**Correlation:** JobRun has `BackgroundJobId`; CorrelationId is on JobRun.

| Field | Type | Description |
|-------|------|-------------|
| **CreatedAt** | DateTime | When the job was enqueued. |
| **ScheduledAt** | DateTime? | When the job was scheduled to run (null = immediate). |
| **StartedAt** | DateTime? | When processing started. |
| **CompletedAt** | DateTime? | When processing finished. |
| **UpdatedAt** | DateTime | Last update. |
| **State** | BackgroundJobState | Queued, Running, Succeeded, Failed. |

**Delays that can be computed:**

- **Queue time:** `StartedAt - CreatedAt` (or for scheduled: time from ScheduledAt to StartedAt).
- **Execution time:** `CompletedAt - StartedAt`.
- **End-to-end:** `CompletedAt - CreatedAt`.
- **Stuck jobs:** Queued or Running with CreatedAt/StartedAt older than threshold.

**Note:** For SLA by company, join through JobRun (BackgroundJobId + CompanyId) when job runs are recorded.

---

## 6. Trace Timeline — Correlation Chain Timing

**Source:** `TraceQueryService` builds a unified timeline from WorkflowJobs, EventStore, and JobRuns by `CorrelationId` (or by EventId, JobRunId, WorkflowJobId, or Entity).

**Output:** `TraceTimelineDto` with `Items` of type `TraceTimelineItemDto`, each with:
- **TimestampUtc**
- **ItemType** (e.g. WorkflowTransitionRequested, WorkflowTransitionStarted, WorkflowTransitionCompleted, EventEmitted, EventProcessed, EventHandlerStarted, EventHandlerSucceeded/Failed, BackgroundJobQueued, BackgroundJobStarted, BackgroundJobCompleted/Failed)
- **CorrelationId**, **CompanyId**, **EntityType**, **EntityId**
- **RelatedId**, **RelatedIdKind** (Event, JobRun, WorkflowJob)
- **Status**, **Source**, **Title**, **Summary**

**Delays that can be computed from timeline:**

- **Request → workflow complete:** First `WorkflowTransitionRequested` to last `WorkflowTransitionCompleted` in the same correlation.
- **Event emit → processed:** First `EventEmitted` to last `EventProcessed` for that event (or EventId).
- **Handler duration:** `EventHandlerStarted` to `EventHandlerSucceeded`/`EventHandlerFailed` (from JobRun timestamps).
- **Job duration:** `BackgroundJobStarted` to `BackgroundJobCompleted`/`BackgroundJobFailed`.
- **Chain stall:** Correlation chain where the last item is old and there is no completion item (e.g. last is EventEmitted or WorkflowTransitionRequested with no matching Completed).

**Missing for SLA:** Timeline is derived; no extra persistence needed. Evaluation can use either raw tables or timeline API; raw tables are better for bulk evaluation (e.g. background job over many rows).

---

## 7. Delays Summary — What Can Be Computed

| Delay type | From | To | Source tables | Notes |
|------------|------|----|----------------|-------|
| Workflow transition duration | CreatedAt | CompletedAt | WorkflowJob | Full request-to-completion. |
| Workflow time to start | CreatedAt | StartedAt | WorkflowJob | Queue / delay before execution. |
| Workflow execution time | StartedAt | CompletedAt | WorkflowJob | Execution only. |
| Event processing duration | CreatedAtUtc (or OccurredAtUtc) | ProcessedAtUtc | EventStore | For Processed/Failed/DeadLetter. |
| Event time in pending | CreatedAtUtc | now | EventStore | For Pending/Processing (stall). |
| Event handler duration | StartedAtUtc | CompletedAtUtc | JobRun (EventId != null) | Per-handler. |
| Background job duration | StartedAtUtc | CompletedAtUtc | JobRun | Or BackgroundJob StartedAt→CompletedAt. |
| Background job queue time | CreatedAt | StartedAt | BackgroundJob / JobRun | From enqueue to start. |
| Correlation chain stall | Timeline items | — | WorkflowJob + EventStore + JobRun | Last activity old, no completion. |

---

## 8. Metrics / Data Gaps (Missing or Partial)

- **Processing started for events:** EventStore has no `ProcessingStartedAtUtc`. “Time to first handler start” would require using the minimum JobRun.StartedAtUtc for that EventId; that is available via JobRun.
- **Company on BackgroundJob:** BackgroundJob has no CompanyId. Company scope for “background job SLA” should be derived via JobRun (CompanyId) when JobRun.BackgroundJobId is set.
- **Workflow definition / transition name in WorkflowJob:** WorkflowJob has WorkflowDefinitionId and EntityType/CurrentStatus/TargetStatus but not a human-readable “workflow name” or “transition name”. SLA rules can target by WorkflowDefinitionId + FromStatus→ToStatus (from definition/transitions) or by entity type + status pair.
- **Dedicated “SLA target” on definitions:** WorkflowDefinition, EventType, and JobType have no built-in SLA fields. SLA rules will live in a separate rule store (e.g. SlaRule table) keyed by CompanyId, TargetType (workflow/event/job), and TargetName (or TargetId).

None of these gaps block implementing SLA evaluation using existing timestamps; they only affect how rules are configured and how company scope is applied to background jobs.

---

## 9. Recommended Data Usage for SLA Engine

1. **Workflow transition SLA:** Query `WorkflowJobs` where `State` in (Succeeded, Failed) and `CompletedAt` is not null; compute duration from `CreatedAt` to `CompletedAt`; compare to rule’s MaxDurationSeconds. For “stuck” workflow jobs, query `State` in (Pending, Running) and `CreatedAt` (or `StartedAt`) &lt; threshold.
2. **Event processing SLA:** Query `EventStore` where `ProcessedAtUtc` is not null; compute duration from `CreatedAtUtc` to `ProcessedAtUtc`; compare to rule. For stalled events, query Status in (Pending, Processing) and `CreatedAtUtc` &lt; threshold.
3. **Background job / handler SLA:** Query `JobRuns` where `CompletedAtUtc` is not null; use `StartedAtUtc` → `CompletedAtUtc` (or `DurationMs`); filter by `JobType` or `JobName` and CompanyId. For stuck runs, Status = Running and `StartedAtUtc` &lt; threshold.
4. **Event chain stall:** Use correlation chains (e.g. by CorrelationId) and detect chains where the latest activity is older than threshold and no completion item exists for the chain (e.g. last item is EventEmitted or WorkflowTransitionRequested without corresponding Completed).
5. **Company scoping:** Apply CompanyId filter on WorkflowJob, EventStore, and JobRun for all queries; for BackgroundJob-only views, join to JobRun and use JobRun.CompanyId.

This audit confirms that **SLA Intelligence can be implemented additively using existing observability data** without changing workflow engine behaviour or adding heavy new instrumentation.
