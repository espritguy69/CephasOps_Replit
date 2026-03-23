# Workflow Orchestration Contract (Phase 9)

## Purpose

This contract describes the workflow orchestration layer introduced in Phase 9: **workflow instances**, **step records**, and the **orchestrator API**. It does not replace the existing **WorkflowEngine** (status transitions and WorkflowJob); it adds a parallel model for multi-step, long-running processes that can be coordinated with the command bus and process managers.

---

## Concepts

### Workflow instance

A **WorkflowInstance** represents one run of a multi-step workflow for a given entity.

| Field | Description |
|-------|-------------|
| Id | Unique identifier. |
| WorkflowType | Logical name (e.g. "OrderFulfillment", "PayrollRun"). |
| EntityType | Entity kind (e.g. "Order", "Invoice"). |
| EntityId | Target entity id. |
| CurrentStep | Name of the current step. |
| Status | Running, Completed, Failed, Compensating. |
| CorrelationId | Optional correlation for tracing. |
| PayloadJson | Optional JSON payload. |
| CompanyId | Optional tenant scope. |
| CreatedAt, UpdatedAt, CompletedAt | Timestamps. |

### Workflow step record

A **WorkflowStepRecord** is one completed step within an instance.

| Field | Description |
|-------|-------------|
| Id | Unique identifier. |
| WorkflowInstanceId | Parent instance. |
| StepName | Name of the step. |
| Status | e.g. Completed. |
| StartedAt, CompletedAt | Timestamps. |
| PayloadJson | Step payload. |
| CompensationDataJson | Optional data for compensation. |

---

## Orchestrator API

### IWorkflowOrchestrator

- **StartWorkflowAsync(workflowType, entityType, entityId, initialPayloadJson, companyId, correlationId, ct)**  
  Creates a new WorkflowInstance and first step "Started". Returns WorkflowInstanceDto.

- **AdvanceStepAsync(instanceId, stepName, payloadJson, ct)**  
  Updates instance CurrentStep and appends a WorkflowStepRecord. Instance must be in status Running.

- **GetInstanceAsync(instanceId, ct)**  
  Returns WorkflowInstanceDto or null.

- **ListInstancesAsync(workflowType, entityType, status, companyId, skip, take, ct)**  
  Returns (Items, TotalCount) for operator diagnostics.

---

## Behavior

- **No automatic progression:** The orchestrator does not subscribe to the event bus. Application code (e.g. process managers or handlers) must call `AdvanceStepAsync` when a step is done.
- **Compensation:** Status `Compensating` and `CompensationDataJson` on steps are reserved for future use; no automatic compensation is implemented.
- **Completion:** To mark an instance completed, call `AdvanceStepAsync` with a final step name and then update the instance status to Completed (or add a dedicated `CompleteWorkflowAsync` in a later phase).

---

## Relation to command bus

- Commands can carry **WorkflowInstanceId**; the command pipeline stores it in CommandProcessingLogs for correlation.
- Process managers can start a workflow (StartWorkflowAsync), then issue commands that reference the instance id; when events are handled, the process manager can advance steps and send further commands.

---

## Relation to existing workflow engine

- **WorkflowEngineService** and **WorkflowJob** remain the authority for **status transitions** (e.g. Order Pending → Assigned → Completed) and event emission (WorkflowTransitionCompletedEvent, OrderStatusChangedEvent).
- **WorkflowOrchestrator** is for **multi-step process coordination** (instance + step tracking, future saga/compensation). Use both where needed: e.g. transition engine for entity status, orchestrator for a long-running flow that spans multiple commands and events.

---

## Operator APIs

- **GET /api/command-orchestration/workflow-instances** – List instances (filter by workflowType, entityType, status, companyId).
- **GET /api/command-orchestration/workflow-instances/{id}** – Get instance by id.

Step history is stored in **WorkflowSteps**; a future endpoint can return instance + steps for a full timeline.

---

## Limitations and future work

- No timeout or automatic step advancement.
- No built-in compensation runner; CompensationDataJson is for design-time use.
- Process managers must be explicitly registered and invoked (e.g. from event handlers); no auto-discovery by event type.
