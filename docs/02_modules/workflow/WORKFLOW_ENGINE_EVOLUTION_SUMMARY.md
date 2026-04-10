# CephasOps — Workflow Engine Evolution Summary

**Date:** 2026-03-09  
**Purpose:** Final report: what exists, what’s missing, recommended direction, and implementation roadmap.  
**Audit scope:** Autonomous workflow engine audit and evolution (no code built).

---

## 1. What Exists Today

- **Status-transition engine:** WorkflowEngineService executes single-step transitions (FromStatus → ToStatus) for entities (Order, Invoice). Resolution is by CompanyId + EntityType + Partner → Department → OrderType → General. Definitions and transitions live in DB (WorkflowDefinitions, WorkflowTransitions).
- **Guards and side effects:** GuardConditionValidatorRegistry and SideEffectExecutorRegistry load definitions from settings (GuardConditionDefinition, SideEffectDefinition) and run validators/executors by Key. Transition JSON configures which guards and side effects run per transition.
- **WorkflowJob:** One record per transition execution (audit). States: Pending, Running, Succeeded, Failed. No CorrelationId or JobRunId.
- **Callers:** OrderService (UI/API), WorkflowController (API), EmailIngestionService (parser), InvoiceSubmissionService (billing), SchedulerService (calendar/slots). All call ExecuteTransitionAsync in-process; no event-driven trigger.
- **Separate systems:** ApprovalWorkflow, AutomationRule, EscalationRule are settings-driven and used after or alongside status changes; they do not replace the engine and do not currently trigger transitions via events.
- **Job Observability:** BackgroundJob + JobRun + IJobRunRecorder exist for background jobs (EmailIngest, PnlRebuild, etc.). Workflow execution is **not** linked to JobRun or CorrelationId.
- **Company scoping:** Workflow definitions, transitions, jobs, and settings are company-scoped. Engine and API use current user’s CompanyId.

---

## 2. What Capabilities Already Exist

- Configurable, DB-driven transitions and resolution (partner/department/order type).
- Role-based allowed transitions (AllowedRolesJson).
- Guard validation and side effect execution in one synchronous flow.
- Audit trail (WorkflowJob) and optional OrderStatusLog / AuditLog for order changes.
- Fire-and-forget notifications after order status change.
- API for execute, allowed-transitions, can-transition, and job history.
- CorrelationId on HTTP requests (middleware); not yet passed into the engine or stored on WorkflowJob.

---

## 3. What Is Missing

- **Domain events:** No event is published when a transition succeeds. No event-driven subscribers.
- **Event-driven transitions:** Transitions are only triggered by direct calls; no “on event X, run transition Y.”
- **Correlation with Job Observability:** WorkflowJob has no CorrelationId or JobRunId; cannot trace from request or JobRun to workflow execution.
- **Async workflow steps / long-running:** No “wait for approval” or “wait for timer” inside the engine; such flows are implemented outside (e.g. scheduler + escalation rules).
- **Workflow instance:** No persisted “workflow instance”; only current entity status and WorkflowJob history.
- **Engine-level retry:** No retry of a failed transition; callers may retry the request.
- **Event store / replay:** No event persistence for replay or audit of events.
- **Step-level history:** Only job-level record; no per-guard or per-side-effect history.

---

## 4. Recommended Architecture Direction

**Option B:** Keep the workflow engine for business state transitions; add an **Event Bus** (and optional process orchestration) **beside** it.

- **Rationale:** Least disruptive; engine stays the single authority for status transitions. Events enable observability, event-driven handlers, and future orchestration without refactoring the engine into a full runtime.
- **Unchanged:** Workflow definitions, resolution, guards, side effects, ExecuteTransitionAsync contract, approval/automation/escalation as separate systems, company scoping.
- **Extended:** Emit a domain event after each successful transition; add CorrelationId (and optional JobRunId) to DTO and WorkflowJob (or event); optional Event Store; optional subscribers (e.g. event-driven transition, escalation check).

---

## 5. Implementation Roadmap

| Phase | Deliverable | Description |
|-------|-------------|-------------|
| **1** | Design and docs | Finalise event schema (DOMAIN_EVENT_ARCHITECTURE.md), integration (WORKFLOW_EVENT_INTEGRATION.md), correlation model. |
| **2** | Correlation and observability | Add CorrelationId and optional JobRunId to ExecuteTransitionDto and WorkflowJob (or event payload). Pass from HTTP/JobRun into engine. |
| **3** | Event emission | After successful transition, publish OrderStatusChanged (or EntityStatusChanged) with base fields + entity/status payload. In-process bus first. |
| **4** | Subscribers (optional) | Register handlers (e.g. escalation check, read model). Add event-driven transition where needed (e.g. InvoiceSubmitted → transition order). |
| **5** | Event store (optional) | Persist events (EventStore table); ProcessedAtUtc, RetryCount, Status for handler processing and replay. |
| **6** | UI and operations | Event Bus Monitor, Workflow Monitor, Workflow Execution Detail, Failed Workflow Step view, correlation links with JobRuns (see WORKFLOW_EVOLUTION_UI_AND_TESTING.md). |

Testing (event dispatch, workflow progression, retry if any, failure handling, correlation propagation) should accompany Phases 2–4 as in the test strategy document.

---

## 6. Document Index

| Document | Purpose |
|----------|---------|
| **WORKFLOW_ENGINE_AUDIT.md** | Full map of the current engine (definitions, transitions, guards, side effects, callers, company scoping). |
| **WORKFLOW_ENGINE_EXECUTION_MODEL.md** | How execution runs: sync, persistence, history, failure, retry, correlation. |
| **WORKFLOW_ENGINE_GAP_ANALYSIS.md** | Gaps vs modern platform (events, event-driven, async, correlation, replay). |
| **WORKFLOW_ENGINE_EVOLUTION_STRATEGY.md** | Option B chosen; what stays, what extends, risk, rollout. |
| **DOMAIN_EVENT_ARCHITECTURE.md** | DomainEvent base, EventStore table design. |
| **WORKFLOW_EVENT_INTEGRATION.md** | How events interact with workflows; observability and correlation. |
| **WORKFLOW_EVOLUTION_UI_AND_TESTING.md** | Future UI (Event Bus Monitor, Workflow Monitor, correlation); test strategy. |
| **WORKFLOW_ENGINE_EVOLUTION_SUMMARY.md** | This document. |

---

## 7. Success Criteria (from Brief)

- The CephasOps workflow engine is **fully understood** — achieved via audit and execution model docs.
- The **correct architectural direction** is chosen — Option B (event bus beside engine) with correlation and optional event store.
- **Future event-driven orchestration** can be added **without unnecessary rewrites** — engine unchanged; events and handlers added incrementally.

No code has been built; all deliverables are documentation for use in the next implementation phase.
