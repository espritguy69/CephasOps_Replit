# CephasOps — Workflow Evolution: UI Plan and Test Strategy

**Date:** 2026-03-09  
**Purpose:** Phase 8 — future admin UI for events and workflow; Phase 9 — testing strategy for event/workflow implementation.

---

## 1. Phase 8 — UI Plan (Future Admin Additions)

### 1.1 Event Bus Monitor

- **Purpose:** View recent domain events (from EventStore or bus buffer), filter by EventType, CompanyId, CorrelationId, time range.
- **Key info:** EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, Status (Pending/Processed/Failed), RetryCount. Drill-down to payload (OrderId, FromStatus, ToStatus, etc.).
- **Actions:** Optional “Re-publish” or “Retry” for failed/pending events (when event store and handlers support it).

### 1.2 Workflow Monitor

- **Purpose:** List workflow definitions and their usage (e.g. transition counts, last used). Optional: list “recent workflow executions” (WorkflowJobs) with filters (CompanyId, EntityType, EntityId, State, date range).
- **Key info:** Workflow definition name, EntityType, scope (Partner/Department/OrderType), transition count; per job: EntityId, FromStatus, ToStatus, State, InitiatedByUserId, CreatedAt, CorrelationId, JobRunId (if present).

### 1.3 Workflow Execution Detail

- **Purpose:** Single WorkflowJob detail: CurrentStatus, TargetStatus, State, timestamps, Payload, LastError, InitiatedByUserId. Show **CorrelationId** and **JobRunId** when available so the user can jump to the related request or Job Run in Job Observability.

### 1.4 Failed Workflow Step View

- **Purpose:** List WorkflowJobs with State = Failed. Filter by CompanyId, EntityType, date. Show LastError, EntityId, transition (From → To), CreatedAt. Link to Workflow Execution Detail and, if present, to JobRun (for background-job-triggered transitions).

### 1.5 Correlation with JobRuns

- **Purpose:** From Job Observability (Job Run detail), show “Workflow jobs triggered by this run” (query WorkflowJobs by JobRunId). From Workflow Execution Detail, link “View Job Run” when JobRunId is set. Ensures workflow execution is traceable from both sides (workflow → job, job → workflow).

---

## 2. Phase 9 — Test Strategy (Future Implementation)

### 2.1 Event Dispatch Tests

- Publish a domain event (e.g. OrderStatusChanged) and assert it is received by a test subscriber or written to the event store (if applicable).
- Assert payload contains required fields (EventId, EventType, CorrelationId, CompanyId, EntityId, FromStatus, ToStatus).
- Assert CorrelationId and JobRunId (when passed) appear in the event.

### 2.2 Workflow Step Progression Tests

- Execute a transition (e.g. Pending → Assigned) and assert: WorkflowJob created with State = Succeeded, entity status updated, guards and side effects invoked as expected.
- Assert that when a guard fails, WorkflowJob state = Failed and entity status unchanged.
- Assert that when a side effect throws, WorkflowJob state = Failed and entity status unchanged.

### 2.3 Retry Logic Tests

- If engine-level or handler-level retry is implemented: assert retry count and backoff, and that after max retries the job/event is marked Failed or DeadLetter.
- For event handlers: assert that retrying a processed event is idempotent (e.g. no duplicate transition).

### 2.4 Failure Handling Tests

- Transition fails (guard or side effect): assert WorkflowJob.LastError set, State = Failed, exception propagated to caller.
- Event handler fails: assert event status/RetryCount updated as designed; no duplicate WorkflowJob when handler retries.

### 2.5 Correlation Propagation Tests

- Call ExecuteTransitionAsync with CorrelationId and optional JobRunId; assert they are stored on WorkflowJob (or in emitted event).
- When transition is triggered from a background job (test double), assert JobRunId is passed and stored (or in event).
- Assert that the emitted event contains the same CorrelationId (and JobRunId if applicable) for observability.

---

## 3. Summary

- **UI:** Event Bus Monitor, Workflow Monitor, Workflow Execution Detail, Failed Workflow Step view, and correlation links with JobRuns provide operations and support with full visibility once events and correlation are implemented.
- **Testing:** Focus on event dispatch, workflow progression, retry (if any), failure handling, and correlation propagation so that event-driven workflow extensions are reliable and traceable.
