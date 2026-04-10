# CephasOps — Workflow / Event Integration

**Date:** 2026-03-09  
**Purpose:** Design how domain events interact with the workflow engine (emit, handlers, correlation).  
**Depends on:** docs/WORKFLOW_ENGINE_EVOLUTION_STRATEGY.md, docs/DOMAIN_EVENT_ARCHITECTURE.md.

---

## 1. Integration Points

| Integration | Direction | Description |
|-------------|-----------|-------------|
| **Workflow transition emits event** | Engine → Bus | After a successful transition, publish e.g. OrderStatusChanged (EntityType, EntityId, FromStatus, ToStatus, WorkflowJobId, CorrelationId, CompanyId, TriggeredByUserId). |
| **Event handler advances workflow** | Bus → Engine | A handler subscribes to an event (e.g. InvoiceSubmitted); it calls ExecuteTransitionAsync to move the related order to SubmittedToPortal. |
| **Background job completion emits event** | Job → Bus | When a BackgroundJob (e.g. MyInvoisStatusPoll) completes, it may publish an event; a handler can then call the workflow engine if a status change is needed. |
| **Workflow side effect publishes event** | Engine → Bus | Optionally, a side effect (e.g. custom executor) could publish an event instead of (or in addition to) the single “after transition” event. Prefer one event per transition to keep semantics clear. |
| **Approval workflow completion emits event** | Approval → Bus | When an approval step or workflow completes, publish e.g. ApprovalCompleted; a handler can call ExecuteTransitionAsync (e.g. ReschedulePendingApproval → Assigned). |
| **Escalation rule emits timed event** | Scheduler / Rule → Bus | A scheduled job or escalation evaluator can publish e.g. EscalationDue; a handler may notify or call the engine (e.g. if escalation action is ChangeStatus). |

---

## 2. Recommended First Steps

1. **Emit one event per successful transition** from the workflow engine (or a thin adapter): OrderStatusChanged (or EntityStatusChanged) with base fields + EntityId, FromStatus, ToStatus, WorkflowJobId. No side effect or approval change in the first iteration.
2. **Pass CorrelationId (and optionally JobRunId)** into ExecuteTransitionDto; store on WorkflowJob or at least in the emitted event for observability.
3. **Add optional in-process subscribers** (e.g. “on OrderStatusChanged, enqueue escalation check” or “on OrderStatusChanged, update read model”). Avoid handlers that call back into the engine until patterns are stable.
4. **Add event-driven transition** where it simplifies coupling: e.g. InvoiceSubmissionService publishes InvoiceSubmitted; handler calls ExecuteTransitionAsync for the order. Ensure idempotency (e.g. only transition if current status is Invoiced).

---

## 3. Observability and Correlation (Phase 7)

Workflow execution must be traceable through **CorrelationId**, **JobRun**, and **EventId**.

### 3.1 Correlation Model

| Id | Source | Stored / Used |
|----|--------|----------------|
| **CorrelationId** | HTTP (X-Correlation-Id), or generated for background jobs, or from parent event | Set on ExecuteTransitionDto when available (e.g. from HttpContext or JobRun). Stored on WorkflowJob (optional column) and included in emitted event. |
| **JobRunId** | IJobRunRecorder when transition runs inside a BackgroundJob | Passed in ExecuteTransitionDto when the caller is the job processor. Stored on WorkflowJob (optional) or only in event payload. Enables “this JobRun caused this WorkflowJob.” |
| **EventId** | Assigned when publishing the domain event | Stored in EventStore; can be linked from WorkflowJob (e.g. WorkflowJobId in event, EventId in event store). Enables “this transition produced this event.” |

### 3.2 Trace Flows

- **Request → Transition:** Request has CorrelationId (middleware) → passed to ExecuteTransitionDto → stored on WorkflowJob and event → logs and UI can show “request X led to WorkflowJob Y and event Z.”
- **BackgroundJob → Transition:** Job processor starts JobRun (recorder) → calls ExecuteTransitionAsync with CorrelationId = job.Id or JobRunId, and JobRunId = current run → WorkflowJob and event carry them → Job Observability UI can show “JobRun R led to WorkflowJob W.”
- **Event → Transition:** Handler receives event (with CorrelationId) → calls ExecuteTransitionAsync and forwards CorrelationId (and optionally sets TriggeredByUserId from event) → new WorkflowJob and new event share or extend the same correlation chain.

### 3.3 Implementation Notes

- Add **CorrelationId** and **JobRunId** (nullable) to ExecuteTransitionDto and to WorkflowJob entity/table when implementing Phase 2.
- Ensure CorrelationId is read from ambient context (e.g. IHttpContextAccessor, or from current JobRun when in a job) and set on the DTO when not provided by the caller.
- Emitted event must include CorrelationId, and optionally JobRunId and WorkflowJobId, so that observability UIs can correlate workflow execution with requests and job runs.

---

## 4. Failure and Retry (Handlers)

- If an **event handler** fails (e.g. one that calls ExecuteTransitionAsync), the event can remain Pending (or be retried) depending on the bus implementation. Handlers should be idempotent (e.g. check current status before transitioning).
- Workflow engine **does not** retry; if the handler’s call to ExecuteTransitionAsync fails, the handler can retry or dead-letter the event. Event store RetryCount and Status support this.

---

## 5. Company Scoping

- Every emitted event must include **CompanyId** (from the entity or workflow context). Subscribers must filter or scope by CompanyId when processing. ExecuteTransitionAsync already receives companyId; the event should carry the same value.
