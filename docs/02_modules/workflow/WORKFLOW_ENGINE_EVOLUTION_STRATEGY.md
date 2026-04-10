# CephasOps — Workflow Engine Evolution Strategy

**Date:** 2026-03-09  
**Purpose:** Choose one strategy for evolving the workflow engine with event-driven capabilities; document rationale, scope, risk, and rollout.  
**Depends on:** docs/WORKFLOW_ENGINE_AUDIT.md, docs/WORKFLOW_ENGINE_EXECUTION_MODEL.md, docs/WORKFLOW_ENGINE_GAP_ANALYSIS.md.

---

## 1. Options Considered

| Option | Description |
|--------|-------------|
| **A** | Extend the existing workflow engine with event support (emit events from engine; optional event-driven triggers). |
| **B** | Keep the workflow engine for business state transitions; add an Event Bus + optional Process Orchestration layer beside it. |
| **C** | Refactor the workflow engine into a generalized orchestration runtime (long-running, async steps, sagas). |

---

## 2. Chosen Strategy: **Option B**

**Keep the workflow engine for status transitions; add an Event Bus (and optional process orchestration) beside it.**

---

## 3. Why Option B Is Correct

- **Least disruptive:** The current engine is production-proven, settings-driven, and company-scoped. Refactoring it into a full orchestration runtime (Option C) would be a large rewrite and risk. Extending it with events only (Option A) is possible but keeps all “event-driven” logic inside the same service; Option B clearly separates “state transition” (engine) from “event distribution and optional orchestration” (bus + handlers).
- **Clear boundaries:** The engine remains the **single authority** for “can this entity move from status A to B?” and “execute the transition (guards, side effects, status update).” The Event Bus carries **notifications** (e.g. OrderStatusChanged) and optional **commands** (e.g. “trigger transition for order X to SubmittedToPortal” from an event handler). New behaviour (escalation timers, approval completion, integrations) can be implemented as **subscribers** without changing the engine’s core.
- **SaaS-ready and company-scoped:** Events and handlers can carry CompanyId; the engine already enforces company scope. No need to change resolution or multi-tenancy in the engine.
- **Aligns with Job Observability:** CorrelationId (and optionally JobRunId) can be added to events and to WorkflowJob (or event payload) so that workflow execution is traceable from request or background job. The Event Bus does not replace Job Observability; it complements it (events can carry CorrelationId for tracing).
- **Future-proof:** If later you need process orchestration (long-running, wait states), that layer can sit **on top of** the Event Bus and call the existing engine for status transitions. Option B does not preclude Option A-style “engine emits events”; it explicitly adds a bus so that emission and subscription are standard.

---

## 4. What Remains Unchanged

- **Workflow definitions and transitions:** Stored and resolved as today (Partner → Department → OrderType → General). No change to WorkflowDefinition, WorkflowTransition, or GetEffectiveWorkflowDefinitionAsync.
- **Guard conditions and side effects:** Same registries, same DB-driven definitions, same execution order (guards → side effects → status update). No change to GuardConditionValidatorRegistry or SideEffectExecutorRegistry.
- **ExecuteTransitionAsync contract:** Same signature and behaviour: resolve workflow, validate guards, run side effects, update entity, create WorkflowJob, return. Callers (OrderService, SchedulerService, InvoiceSubmissionService, API) continue to call it as today.
- **Approval workflows, automation rules, escalation rules:** Remain separate settings and services; they are not merged into the engine. They may later **publish** or **subscribe** to events.
- **Company scoping:** All workflow and event behaviour remains company-scoped where applicable.

---

## 5. What Will Be Extended

- **Event emission:** After a successful transition, the engine (or a thin wrapper/handler) will **publish** a domain event (e.g. OrderStatusChanged) containing EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, TriggeredByUserId, EntityType, EntityId, FromStatus, ToStatus, WorkflowJobId, optional Payload. CorrelationId (and optionally JobRunId) will be passed in via ExecuteTransitionDto or from ambient context (e.g. HTTP) and included in the event.
- **WorkflowJob:** Optional extension: add **CorrelationId** (and optionally **JobRunId**) to WorkflowJob so each execution is traceable. If not stored on the entity, at least include in the emitted event.
- **Event Bus:** A simple in-process or out-of-process bus (e.g. in-memory mediator, or message queue) so that:
  - Publishers (workflow engine or adapter) publish domain events.
  - Subscribers (handlers) can react (e.g. trigger escalation check, update read model, call external system). One subscriber may call ExecuteTransitionAsync (event-driven transition).
- **Optional Event Store:** Persist events (EventId, EventType, Payload, OccurredAtUtc, ProcessedAtUtc, RetryCount, Status) for audit, replay, or async processing. Can be added after the bus is in place.
- **Process orchestration (later):** If needed, a separate orchestration layer can subscribe to events, manage “wait for approval / timer,” and call the workflow engine when the next transition should run. This is **not** part of the initial extension.

---

## 6. Migration Risk

- **Low for “emit event after transition”:** Add a single publish call (or delegate) after the job is set to Succeeded, with no change to guards/side effects or resolution. If the bus is in-process and synchronous, behaviour is similar to today; if async, ensure at-least-once and failure handling (e.g. event store + retry).
- **Low for CorrelationId:** Add optional CorrelationId (and JobRunId) to ExecuteTransitionDto and to WorkflowJob (or event payload). Existing callers can leave them null; middleware or job processor can set them when available.
- **Medium for new subscribers:** New handlers that call back into the engine (e.g. “on InvoiceSubmitted, transition order”) must be designed to avoid infinite loops and to respect company/entity scope. Good testing and idempotency (e.g. “only transition if current status is Invoiced”) keep risk manageable.
- **No breaking API changes:** ExecuteTransitionAsync can remain backward-compatible; new fields (CorrelationId, JobRunId) optional.

---

## 7. Rollout Plan

1. **Phase 1 — Design and docs:** Finalise domain event schema and Event Store design (docs/DOMAIN_EVENT_ARCHITECTURE.md, docs/WORKFLOW_EVENT_INTEGRATION.md). Define correlation model with Job Observability (docs in Phase 7).
2. **Phase 2 — Correlation and observability:** Add CorrelationId (and optional JobRunId) to ExecuteTransitionDto and WorkflowJob (or event payload only). Ensure request CorrelationId (middleware) and JobRunId (when run inside a BackgroundJob) are passed through. No event bus yet; just plumbing for tracing.
3. **Phase 3 — Event emission:** Introduce a minimal event abstraction (e.g. IDomainEventPublisher). After successful transition, publish OrderStatusChanged (or generic EntityStatusChanged) with the agreed payload. Start with in-process, synchronous publish (e.g. delegate or in-memory bus); no persistence required initially.
4. **Phase 4 — Subscribers (optional):** Register one or more handlers (e.g. “on OrderStatusChanged, check escalation rules” or “on InvoiceSubmitted, transition order”). Prefer handlers that do not call back into the engine first; then add event-driven transition where needed (e.g. InvoiceSubmissionService publishes event, handler calls ExecuteTransitionAsync).
5. **Phase 5 — Event store (optional):** If replay or audit is required, add EventStore table and persist events on publish; process handlers from store with retry/ProcessedAtUtc.
6. **Phase 6 — UI and operations:** Add Event Bus Monitor and Workflow execution detail (with CorrelationId/JobRun) as per Phase 8 UI plan.

---

## 8. What We Explicitly Do Not Do (Initial Rollout)

- We do **not** refactor the engine into a generic orchestration runtime (no Option C).
- We do **not** add long-running or “wait” steps inside the engine (no async workflow steps in the first iteration).
- We do **not** merge ApprovalWorkflow / AutomationRule / EscalationRule into the engine; they remain separate and may integrate via events later.
- We do **not** change transition resolution, guards, or side effects logic beyond adding event publish and correlation.

This keeps the evolution incremental and low-risk while enabling event-driven orchestration and full traceability with Job Observability.
