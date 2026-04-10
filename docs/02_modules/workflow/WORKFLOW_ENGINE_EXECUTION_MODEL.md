# CephasOps — Workflow Engine Execution Model

**Date:** 2026-03-09  
**Purpose:** Document how workflow transitions actually execute: sync/async, persistence, history, failure and retry.  
**Depends on:** docs/WORKFLOW_ENGINE_AUDIT.md.

---

## 1. Synchronous Transitions

- **ExecuteTransitionAsync** runs entirely in-process and is **awaited** by the caller. From the caller’s perspective it is a single synchronous operation: request in, WorkflowJob and entity status updated, response out.
- Steps inside the call: resolve workflow → get current status → find transition → create WorkflowJob (Pending) → persist → set Running → validate guards → execute side effects → update entity status → audit/notify → set Succeeded/Failed → persist → return.
- **No** queue is used for the transition itself. The HTTP request (or in-process call) holds the entire execution. Timeouts are the normal request/timeout (e.g. HTTP, command) only.

---

## 2. Background Job Triggers

- The workflow engine **does not** enqueue a **BackgroundJob** for a transition. When a transition is triggered from:
  - **API (WorkflowController):** Runs in the request pipeline; no background job.
  - **OrderService (UI):** Same; no background job.
  - **SchedulerService / EmailIngestionService:** These may run inside a **BackgroundJob** (e.g. EmailIngest). In that case the transition runs **inside** the same job execution, synchronously. The WorkflowJob is **not** linked to the BackgroundJob or JobRun (no FK or CorrelationId).
- So: workflow execution is **not** triggered by a background job; it may run **inside** a background job when the caller is a job processor.

---

## 3. Workflow Execution Persistence

- **WorkflowJob** is the only persistent record of a single transition execution. Stored in `WorkflowJobs` (CompanyId, WorkflowDefinitionId, EntityType, EntityId, CurrentStatus, TargetStatus, State, PayloadJson, InitiatedByUserId, LastError, StartedAt, CompletedAt, CreatedAt, UpdatedAt).
- **Entity status** is updated in the same request (Order.Status, Invoice.Status, etc.) in the same transaction as the job state update (Succeeded/Failed).
- **OrderStatusLog** (or equivalent) is created by a side effect (CreateOrderStatusLogSideEffectExecutor), not by the engine core; audit log (AuditLogService) is called from the engine for order status changes.
- There is **no** separate “workflow instance” table (no row like “Order X is at step 3 of workflow W”). Only the entity’s current status and the history of WorkflowJobs represent progress.

---

## 4. Workflow Instance Tracking

- There is **no** workflow instance. The system does not track “this order is in workflow definition Y, step N.” It only has:
  - Entity’s **current status** (e.g. Order.Status).
  - **WorkflowJob** rows per past transition (EntityType, EntityId, CurrentStatus, TargetStatus, State, timestamps).
- “Allowed next steps” are computed by **GetAllowedTransitionsAsync** from the effective workflow definition and current status (and user roles). There is no persisted “current step” or “instance id.”

---

## 5. Workflow History

- **WorkflowJob** rows for a given entity (EntityType + EntityId) form the history of transition attempts. Query: GetWorkflowJobsAsync(companyId, entityType, entityId). Ordered by CreatedAt descending.
- Each row gives: From (CurrentStatus), To (TargetStatus), State (Succeeded/Failed), InitiatedByUserId, StartedAt, CompletedAt, LastError, Payload.
- **OrderStatusLog** (created by side effect) may provide a separate audit of status changes; it is not the same as WorkflowJob (which records the workflow execution, including failed attempts).
- **No** step-level history (e.g. “guard X passed at T1, side effect Y ran at T2”). Only the single job record per transition.

---

## 6. Failure Handling

- If **guard validation** fails: exception thrown → job set to Failed, LastError set, CompletedAt set, SaveChanges → exception rethrown to caller.
- If **side effect** throws: same; whole transition fails, job Failed.
- If **UpdateEntityStatusAsync** throws: same; entity status is **not** updated (transaction or save order ensures consistency).
- **Notifications** (Task.Run) are fire-and-forget; their failure is logged but does **not** change the WorkflowJob state (job is already Succeeded).
- **No** dead-letter queue, no automatic retry at engine level. Caller (e.g. API client or background job) may retry the request.

---

## 7. Retry Handling

- The workflow engine **does not** retry. One attempt per ExecuteTransitionAsync call. If the call fails, the WorkflowJob remains Failed; the caller may call ExecuteTransitionAsync again (e.g. same or new request), which will create a **new** WorkflowJob.
- **BackgroundJob** (EmailIngest, etc.) has its own retry (RetryCount, MaxRetries, exponential backoff). If that job runs a transition and the transition fails, the whole BackgroundJob fails and may be retried; on retry, the job processor might call ExecuteTransitionAsync again, resulting in a new WorkflowJob. There is no “retry of the same WorkflowJob.”

---

## 8. Long-Running Processes

- **Not supported.** A transition completes in one request. There is no “pause and wait for external event” or “resume when approval received.”
- Long-running behaviour (e.g. “wait for TIME approval”) is implemented **outside** the engine: e.g. order in ReschedulePendingApproval; when approval is received, a separate call triggers the next transition (e.g. to Assigned). The engine does not manage the wait.

---

## 9. Waiting for Async Results

- The engine does **not** wait for async results (e.g. external API, human approval). It runs to completion in one call. Any “wait” is implemented by not calling the engine until the condition is met, then calling it again.

---

## 10. Retryable Steps

- Steps (guards, side effects, status update) are **not** individually retryable by the engine. The whole transition is atomic from the caller’s perspective: either all succeed and the job is Succeeded, or one fails and the job is Failed. No per-step retry or skip.

---

## 11. Step Execution History

- **Not recorded.** Only the WorkflowJob aggregate (and entity status change) is persisted. Which guard failed or which side effect threw is only in logs and in LastError (high-level). No table of “step X ran at T with result Y.”

---

## 12. Correlation Across Jobs

- **WorkflowJob** has no **CorrelationId** or **JobRunId**. It cannot be correlated with:
  - The HTTP request (CorrelationIdMiddleware sets X-Correlation-Id in context; it is not passed to the engine or stored on WorkflowJob).
  - A **BackgroundJob** or **JobRun** when the transition is triggered from a background job.
- So: **no** built-in correlation between workflow execution and Job Observability (JobRun) or request tracing. Adding CorrelationId (and optionally JobRunId) to WorkflowJob (or to a new event) would enable this.

---

## 13. Summary Table

| Capability | Supported? | Notes |
|------------|------------|--------|
| Synchronous transitions | Yes | Single await; in-process. |
| Background job triggers | No | Engine does not enqueue jobs for transitions. |
| Workflow execution persistence | Yes | WorkflowJob per transition. |
| Workflow instance tracking | No | No instance table; only current status + job history. |
| Workflow history | Yes | WorkflowJobs by entity. |
| Failure handling | Yes | Job → Failed, LastError; exception to caller. |
| Retry at engine level | No | Caller may retry request. |
| Long-running processes | No | One-shot transition only. |
| Wait for async results | No | Implemented outside engine. |
| Retryable steps | No | All-or-nothing transition. |
| Step execution history | No | Only job-level record. |
| Correlation (JobRun / request) | No | Not stored on WorkflowJob. |
