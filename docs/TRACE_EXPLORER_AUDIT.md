# Operational Trace Explorer — Traceability Audit

**Date:** 2026-03-09  
**Purpose:** Document existing observability links between WorkflowJob, EventStore, JobRun, replay operations, and retry actions. Identify gaps for a unified trace explorer.

---

## 1. WorkflowJob

**Entity:** `CephasOps.Domain.Workflow.Entities.WorkflowJob` (extends `CompanyScopedEntity`)

| Field | Type | Use for trace |
|-------|------|----------------|
| Id | Guid | Primary key; lookup by WorkflowJobId. |
| CompanyId | Guid? | Company scoping (from CompanyScopedEntity). |
| CreatedAt | DateTime | When the workflow transition was requested. |
| UpdatedAt | DateTime | Last update. |
| WorkflowDefinitionId | Guid | Which workflow. |
| EntityType | string | e.g. "Order", "Invoice" — entity context. |
| EntityId | Guid | Entity instance — supports entity-based lookup. |
| CurrentStatus | string | Business status before transition. |
| TargetStatus | string | Intended next status. |
| State | enum | Pending, Running, Succeeded, Failed. |
| LastError | string? | If failed. |
| PayloadJson | string? | Transition context (actor, metadata). |
| InitiatedByUserId | Guid? | Actor when available. |
| StartedAt | DateTime? | When processing started. |
| CompletedAt | DateTime? | When processing finished. |
| **CorrelationId** | string? | **Links to EventStore and JobRun.** Set when workflow is created; propagated to emitted events. |

**Linkage**

- **Out:** Emits `WorkflowTransitionCompletedEvent` with same `CorrelationId` → EventStore entry.
- **In:** Can be found from EventStore or JobRun by matching `CorrelationId` (and company).

**Gaps**

- No direct foreign key to EventId or JobRunId. Link is via `CorrelationId` only.

---

## 2. EventStore (EventStoreEntry)

**Entity:** `CephasOps.Domain.Events.EventStoreEntry`

| Field | Type | Use for trace |
|-------|------|----------------|
| EventId | Guid | Primary key; lookup by EventId. |
| EventType | string | e.g. "WorkflowTransitionCompleted". |
| Payload | string (jsonb) | Full event payload. |
| OccurredAtUtc | DateTime | When the event occurred (business time). |
| CreatedAtUtc | DateTime | When the record was written. |
| ProcessedAtUtc | DateTime? | When processing completed. |
| RetryCount | int | Number of retry attempts. |
| Status | string | Pending, Processing, Processed, Failed, DeadLetter. |
| **CorrelationId** | string? | **Links to WorkflowJob and JobRun.** |
| CompanyId | Guid? | Company scoping. |
| TriggeredByUserId | Guid? | Actor when available. |
| Source | string? | e.g. "WorkflowEngine". |
| EntityType | string? | Entity context. |
| EntityId | Guid? | Entity instance — supports entity lookup. |
| LastError | string? | Last failure message. |
| LastErrorAtUtc | DateTime? | When last error occurred. |
| LastHandler | string? | Handler that last ran/failed. |
| ParentEventId | Guid? | Optional parent event (child flows). |

**Linkage**

- **Out:** Same `CorrelationId` as WorkflowJob that emitted it. Handler execution creates JobRuns with `EventId` and `CorrelationId`.
- **In:** Can be found from WorkflowJob or JobRun by `CorrelationId`; from JobRun by `EventId`.

**Gaps**

- Retry/Replay actions are not persisted as separate records; only `RetryCount` and status changes reflect retries. No "replay executed at X by user Y" record.

---

## 3. JobRun

**Entity:** `CephasOps.Domain.Workflow.Entities.JobRun`

| Field | Type | Use for trace |
|-------|------|----------------|
| Id | Guid | Primary key; lookup by JobRunId. |
| CompanyId | Guid? | Company scoping. |
| JobName | string | Display name (e.g. "WorkflowTransitionCompleted - XHandler"). |
| JobType | string | e.g. "EventHandling", "EmailIngest". |
| TriggerSource | string | Scheduler, Manual, System, Retry, EventBus, etc. |
| **CorrelationId** | string? | **Links to WorkflowJob and EventStore.** |
| QueueOrChannel | string? | e.g. "BackgroundJobs". |
| PayloadSummary | string? | Truncated summary. |
| Status | string | Pending, Running, Succeeded, Failed, etc. |
| StartedAtUtc | DateTime | When run started. |
| CompletedAtUtc | DateTime? | When run finished. |
| DurationMs | long? | Duration. |
| RetryCount | int | Retry attempts for this run. |
| ErrorMessage, ErrorDetails | string? | Failure info. |
| InitiatedByUserId | Guid? | Actor when manual/API. |
| ParentJobRunId | Guid? | Links to previous run (e.g. retry). |
| RelatedEntityType | string? | Entity context. |
| RelatedEntityId | string? | Entity instance. |
| BackgroundJobId | Guid? | When backed by a BackgroundJob. |
| **EventId** | Guid? | **When this run is for an event handler — links to EventStore.** |
| CreatedAtUtc, UpdatedAtUtc | DateTime | Audit. |

**Linkage**

- **Out:** `CorrelationId` links to WorkflowJob and EventStore. `EventId` links to EventStore. `ParentJobRunId` links to another JobRun (retry chain). `BackgroundJobId` links to BackgroundJob.
- **In:** Can be found from EventStore by `EventId` or `CorrelationId`; from WorkflowJob by `CorrelationId`.

**Gaps**

- JobRun is created when a job **starts**, not when it is queued. So "background job queued" is not represented as a JobRun.

---

## 4. BackgroundJob

**Entity:** `CephasOps.Domain.Workflow.Entities.BackgroundJob`

| Field | Type | Use for trace |
|-------|------|----------------|
| Id | Guid | Primary key. |
| JobType | string | e.g. "EventHandlingAsync", "EmailIngest". |
| PayloadJson | string | JSON payload (may contain correlationId for some job types). |
| State | enum | Queued, Running, Succeeded, Failed. |
| RetryCount | int | Retry attempts. |
| CreatedAt | DateTime | When job was queued. |
| StartedAt | DateTime? | When processing started. |
| CompletedAt | DateTime? | When completed. |
| RetriedFromJobRunId | Guid? | When retried from UI/API. |

**Linkage**

- **Out:** When the processor picks the job, it creates a **JobRun** with `BackgroundJobId = job.Id`. JobRun carries `CorrelationId` (for event-handling jobs, from payload; for others may be null).
- **In:** No `CorrelationId` column on BackgroundJob. Correlation is only available after a JobRun is created (when job starts). So **by CorrelationId we cannot find a BackgroundJob** unless we parse PayloadJson for known types (e.g. EventHandlingAsync stores correlationId in payload).

**Gaps**

- **No CorrelationId on BackgroundJob.** Timeline by correlation cannot include "background job queued" unless we add CorrelationId to BackgroundJob or derive it from payload for specific job types.
- "Background job queued" timestamp is BackgroundJob.CreatedAt but not linkable by correlation from existing schema.

---

## 5. Replay operations

**Implementation:** `EventReplayService.RetryAsync` / `ReplayAsync` — load event from EventStore, re-dispatch to handlers. No dedicated persistence.

- **Retry:** Re-dispatch without policy check. EventStore row is updated (MarkAsProcessingAsync, then MarkProcessedAsync or failure). No separate "retry requested" row.
- **Replay:** Same, but gated by `IEventReplayPolicy`. Only `WorkflowTransitionCompleted` allowed.

**Linkage**

- Replay/retry acts on an **EventId**. The same EventStore row is updated (status, RetryCount, LastError, etc.). No new EventStore row for "replay executed."
- If handlers run, new **JobRuns** may be created (in-process or via EventHandlingAsync job), with same EventId and CorrelationId.

**Gaps**

- **No explicit "retry requested" or "replay executed" record** with timestamp and actor. We only have EventStore.RetryCount and status. InitiatedByUserId is not stored on the event for the retry/replay action (only in logs). So timeline cannot show "User X requested retry at T" without adding a small audit table or logging to a queryable store.

---

## 6. Retry actions (job run retry)

**Implementation:** User retries a **JobRun** from Background Jobs UI → new BackgroundJob may be created with `RetriedFromJobRunId` (or similar); processor creates a new JobRun with `ParentJobRunId = RetriedFromJobRunId`.

- **BackgroundJob.RetriedFromJobRunId** links the new job to the JobRun that was retried.
- **JobRun.ParentJobRunId** links the new run to the previous run.

**Linkage**

- Retry chain: JobRun A → (user retry) → JobRun B with `ParentJobRunId = A.Id`. So we can walk "retry of" chains via ParentJobRunId.

**Gaps**

- None for job-run retry linkage. Timeline can show "JobRun B (retry of JobRun A)" from ParentJobRunId.

---

## 7. Current API linkage (Event Bus Monitor)

- **GET /api/event-store/events/{eventId}** — event detail.
- **GET /api/event-store/events/{eventId}/related-links** — returns JobRuns (by EventId or CorrelationId) and WorkflowJobs (by CorrelationId), company-scoped.

So from an **EventId** we can get EventStore entry + related JobRuns + related WorkflowJobs. We do **not** have:

- Lookup by **CorrelationId** alone (returning all events, job runs, workflow jobs for that correlation).
- Lookup by **JobRunId** (returning that run + same-correlation events and workflow jobs).
- Lookup by **WorkflowJobId** (returning that workflow job + same-correlation events and job runs).
- Lookup by **EntityType + EntityId** (returning all related events, job runs, workflow jobs for that entity).

---

## 8. Summary: linkage matrix

| From ↓ / To → | WorkflowJob | EventStore | JobRun | BackgroundJob |
|---------------|-------------|------------|--------|----------------|
| **WorkflowJob** | — | Same CorrelationId (event emitted) | Same CorrelationId (if handler creates run) | — |
| **EventStore** | Same CorrelationId | — | EventId, or same CorrelationId | — |
| **JobRun** | Same CorrelationId | EventId | ParentJobRunId (retry) | BackgroundJobId |
| **BackgroundJob** | — | — | JobRun.BackgroundJobId (when run created) | — |

**Identifiers for unified lookup**

- **CorrelationId** — present on WorkflowJob, EventStore, JobRun. Best backbone for trace. Not on BackgroundJob.
- **EventId** — present on EventStore (PK), JobRun (FK). Links event ↔ handler runs.
- **JobRunId** — present on JobRun (PK). Links to EventStore via EventId; to WorkflowJob via CorrelationId; to parent run via ParentJobRunId.
- **WorkflowJobId** — present on WorkflowJob (PK). Links to EventStore and JobRun via CorrelationId.
- **EntityType + EntityId** — present on WorkflowJob, EventStore; on JobRun as RelatedEntityType/RelatedEntityId. Supports entity-centric trace.

---

## 9. Gaps for Trace Explorer

1. **Single-ID lookup:** No API that accepts CorrelationId, EventId, JobRunId, or WorkflowJobId and returns a unified timeline. Related-links is event-centric only.
2. **Entity lookup:** No API that accepts EntityType + EntityId and returns all related events, job runs, workflow jobs.
3. **Chronological timeline:** No read model that merges WorkflowJob, EventStore, JobRun (and optionally BackgroundJob) into one ordered timeline.
4. **BackgroundJob and correlation:** BackgroundJob has no CorrelationId; "job queued" cannot be placed on a correlation-based timeline unless we add CorrelationId to BackgroundJob or parse payload for known types.
5. **Replay/Retry as first-class items:** No stored record for "retry requested" or "replay executed" with timestamp and actor; only EventStore updates and logs.

---

## 10. Recommendations (for later phases)

- **Phase 2–3:** Build a **timeline read model** derived from WorkflowJob, EventStore, JobRun (and where possible BackgroundJob). One endpoint per lookup type: by CorrelationId, EventId, JobRunId, WorkflowJobId, EntityType+EntityId. Chronological order, company-scoped.
- **Phase 4:** Add **CorrelationId** to BackgroundJob only if we need "background job queued" on the same correlation timeline; otherwise omit or derive from payload for EventHandlingAsync only. For replay/retry, consider a small **audit table** (e.g. EventReplayAudit: EventId, AtUtc, InitiatedByUserId, Action = Retry|Replay) only if operations need "who retried when" in the timeline; otherwise keep as-is and infer from RetryCount/status.
- **No change** to workflow engine behaviour or Event Bus publish/dispatch logic; use existing correlation model as the backbone.
