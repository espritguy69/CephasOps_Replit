# CephasOps — Workflow Engine Gap Analysis

**Date:** 2026-03-09  
**Purpose:** Compare current workflow engine to a modern operations platform; identify missing capabilities.  
**Depends on:** docs/WORKFLOW_ENGINE_AUDIT.md, docs/WORKFLOW_ENGINE_EXECUTION_MODEL.md.

---

## 1. Domain Events

| Gap | Current | Target (modern platform) |
|-----|---------|---------------------------|
| **Domain events** | None. Status change is applied and optionally logged; no published event. | Emit a domain event (e.g. OrderStatusChanged) after a successful transition so other bounded contexts or processes can react. |
| **Event payload** | N/A | EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, TriggeredByUserId, EntityType, EntityId, FromStatus, ToStatus, Payload. |
| **Event store** | N/A | Optional event store for replay, audit, or async handlers. |

**Impact:** Without domain events, any “event-driven” behaviour (e.g. escalation timer, approval completion, integration) must be triggered by polling or by the same process that performed the transition. Adding events allows decoupled subscribers and future orchestration.

---

## 2. Event-Driven Transitions

| Gap | Current | Target |
|-----|---------|--------|
| **Trigger by event** | Transitions are only triggered by direct API or in-process calls. | Allow a transition to be triggered when a domain event is received (e.g. “InvoiceSubmitted” → transition order to SubmittedToPortal). |
| **Event handler → engine** | InvoiceSubmissionService calls ExecuteTransitionAsync directly after submission. | Same outcome could be achieved by publishing InvoiceSubmitted and a handler calling ExecuteTransitionAsync; or by an orchestration layer that reacts to the event and invokes the engine. |

**Impact:** Today the coupling is explicit (InvoiceSubmissionService knows to call workflow). Event-driven triggers would allow multiple subscribers and keep the engine as the single place that changes status.

---

## 3. Async Workflow Steps

| Gap | Current | Target |
|-----|---------|--------|
| **Async steps** | All steps (guards, side effects, status update) run in one synchronous call. | Support steps that “wait” (e.g. wait for approval, wait for timer) and resume when an event or callback occurs. |
| **Resume token / instance** | No workflow instance. | Would require a persisted workflow instance (or saga) and a way to resume by instance id + event. |

**Impact:** Long-running flows (e.g. “wait 24h then escalate”) are implemented outside the engine (e.g. scheduler + escalation rules). Full async steps would require a more complex runtime (orchestration/saga) and are a larger change.

---

## 4. Workflow Execution Instances

| Gap | Current | Target |
|-----|---------|--------|
| **Instance** | No concept of “this order’s workflow instance.” Only current status + history of WorkflowJobs. | Optional: persist a workflow instance (e.g. definition id, entity id, current “step” or status, created/updated). |
| **Instance lifecycle** | N/A | Create on first transition or on order creation; update on each transition; optional “completed” or “cancelled” state. |

**Impact:** For a status-transition-only engine, “instance” can be derived (entity + current status). For future orchestration (multi-step, wait states), an explicit instance table would help.

---

## 5. Workflow Step Retries

| Gap | Current | Target |
|-----|---------|--------|
| **Retry** | No retry inside the engine. Caller may retry the request; each attempt creates a new WorkflowJob. | Optional: configurable retry for transient failures (e.g. side effect timeout), with backoff and max attempts. |
| **Idempotency** | Repeating the same transition (same entity, same target status) would create a second WorkflowJob; transition would fail if current status no longer matches. | Idempotency key (e.g. request id) could avoid duplicate WorkflowJobs when the same request is retried. |

**Impact:** Low for current use; important if transitions are triggered by messages (at-least-once delivery) or unreliable callers.

---

## 6. Event Correlation

| Gap | Current | Target |
|-----|---------|--------|
| **CorrelationId on workflow** | WorkflowJob has no CorrelationId. Request CorrelationId (middleware) is not passed to the engine or stored. | Store CorrelationId on WorkflowJob (or on emitted event) so workflow execution can be traced with the same id as the request or parent job. |
| **JobRun link** | When transition runs inside a BackgroundJob, there is no link between WorkflowJob and JobRun. | Optional: pass CorrelationId/JobRunId into ExecuteTransitionDto; store on WorkflowJob or event for observability. |

**Impact:** Job Observability cannot currently show “this JobRun led to this WorkflowJob” or “this API request led to this transition.” Correlation fixes that.

---

## 7. Failure Recovery

| Gap | Current | Target |
|-----|---------|--------|
| **Recovery** | Failed WorkflowJob stays Failed; no automatic retry. Manual or caller retry only. | Optional: mark job “Retryable,” allow background or manual “retry this WorkflowJob” (re-run guards/side effects/status update). |
| **Dead letter** | No. | Optional: after N failures, move to “dead letter” and alert; no automatic retry. |
| **Compensation** | No. Status is already updated only on success; no compensating action on failure. | For true sagas, compensation (undo) would be needed; out of scope for current engine. |

**Impact:** Moderate; most failures are validation or business rules; retry is often “fix data and call again.” For event-driven flows, retry and dead-letter become more important.

---

## 8. Replay Capability

| Gap | Current | Target |
|-----|---------|--------|
| **Replay** | No event log to replay. WorkflowJob history is audit only. | If domain events are stored (event store), replay could re-publish events for reprocessing or rebuild read models. |
| **Re-execute transition** | Not supported. Engine does not “re-run” a past WorkflowJob. | Could add “retry failed job” (same transition again) as a form of replay. |

**Impact:** Replay is enabled by adding an event store and event emission; the current engine does not provide it.

---

## 9. Summary: What Is Missing

| Capability | Missing? | Priority for evolution |
|------------|----------|-------------------------|
| Domain events | Yes | High — foundation for event-driven and observability. |
| Event-driven transitions | Yes (only direct call) | Medium — can be added via handlers that call engine. |
| Async workflow steps | Yes | Low in short term — large change; use scheduler + rules for now. |
| Workflow execution instances | Yes (implicit only) | Low — optional for future orchestration. |
| Step retries (engine-level) | Yes | Medium — useful with event/message triggers. |
| Event correlation (CorrelationId / JobRun) | Yes | High — needed for Job Observability. |
| Failure recovery / retry job | Yes | Medium — improves operability. |
| Replay | Yes | Low — depends on event store. |

**Recommendation:** Prioritise **domain events** (emit on transition) and **correlation** (CorrelationId, optional JobRunId) so that the existing engine remains the single place for status transitions while enabling observability and future event-driven orchestration without a full rewrite.
