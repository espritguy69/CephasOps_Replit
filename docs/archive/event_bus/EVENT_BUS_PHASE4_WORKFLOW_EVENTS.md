# Event Bus — Phase 4: Workflow Event Emission

**Date:** 2026-03-09  
**Context:** Phase 1–3 (domain events, event store, correlation) are in place. This document describes workflow transition event emission and lifecycle.

**Production hardening (outbox, dispatcher worker, retry, dead-letter):** See **EVENT_BUS_PHASE4_PRODUCTION.md**. As of Phase 4 production, events are staged in the same transaction as the workflow commit and processed by **EventStoreDispatcherHostedService**; this doc’s “PublishAsync after SaveChanges” is superseded by that flow.

---

## 1. Workflow Transition Event Design

### 1.1 When Events Are Emitted

Events are emitted **only after a workflow transition has successfully committed**:

1. Guard conditions are validated.
2. Side effects are executed.
3. Entity status is updated (e.g. Order.Status).
4. Audit and notifications run (fire-and-forget where applicable).
5. `WorkflowJob` is updated: `State = Succeeded`, `CompletedAt = DateTime.UtcNow`.
6. **`SaveChangesAsync`** is called (transition is committed).
7. **Then** the engine builds `WorkflowTransitionCompletedEvent` and calls `IDomainEventDispatcher.PublishAsync(evt)`.

No event is emitted for:

- Failed transitions (guards, side effects, or status update throw).
- When `IDomainEventDispatcher` is not registered (optional dependency).

### 1.2 Failure Safety

- If **PublishAsync** throws, the exception is caught and logged. The transition result is **not** rolled back; the job remains `Succeeded`.
- Event persistence still occurs inside **PublishAsync** (append to EventStore, then dispatch to handlers). If the store is unavailable, the exception is caught in the engine and logged.

---

## 2. Event Type: WorkflowTransitionCompletedEvent

### 2.1 Interfaces

- **IDomainEvent** — standard event contract (EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, TriggeredByUserId, Source).
- **IHasEntityContext** — enables EventStore to index by EntityType/EntityId.

### 2.2 Payload Structure

| Field | Type | Description |
|-------|------|-------------|
| EventId | Guid | Unique event instance id. |
| EventType | string | `"WorkflowTransitionCompleted"`. |
| OccurredAtUtc | DateTime | When the transition completed. |
| CorrelationId | string? | From request/workflow; for end-to-end tracing. |
| CompanyId | Guid? | Tenant. |
| TriggeredByUserId | Guid? | User who initiated the transition. |
| Source | string | `"WorkflowEngine"`. |
| WorkflowDefinitionId | Guid | Workflow definition that contained the transition. |
| WorkflowTransitionId | Guid? | Transition definition id (WorkflowTransitionDto.Id). |
| WorkflowJobId | Guid | Workflow job record for this execution. |
| FromStatus | string | Entity status before transition. |
| ToStatus | string | Entity status after transition. |
| EntityType | string | e.g. `"Order"`. |
| EntityId | Guid | Id of the entity that transitioned. |

---

## 3. Correlation Flow

1. **HTTP:** Middleware sets or generates `X-Correlation-Id`, stores in `HttpContext.Items`.
2. **Controller:** `WorkflowController` sets `dto.CorrelationId ??= _correlationIdProvider.GetCorrelationId()` before calling the engine.
3. **Engine:** Sets `job.CorrelationId = dto.CorrelationId` and builds the event with `CorrelationId = dto.CorrelationId ?? job.CorrelationId`.
4. **EventStore:** Row stores `CorrelationId` (indexed).
5. **Handlers:** When `JobRunRecorderForEvents` is used, each handler run creates a **JobRun** with the event’s `CorrelationId`.

So one correlation id ties: **Request → WorkflowJob → Event → EventStore → JobRun(s)**.

---

## 4. Event Lifecycle (Example)

1. **User** calls API to move Order from Pending → Assigned (with or without `X-Correlation-Id`).
2. **WorkflowController** sets `dto.CorrelationId` from provider (or leaves null).
3. **WorkflowEngineService** executes transition; job succeeds and is saved.
4. **Engine** builds `WorkflowTransitionCompletedEvent` (WorkflowDefinitionId, WorkflowTransitionId, WorkflowJobId, FromStatus, ToStatus, EntityType, EntityId, CorrelationId, CompanyId, TriggeredByUserId, OccurredAtUtc) and calls **PublishAsync**.
5. **DomainEventDispatcher.PublishAsync**:
   - Appends event to **EventStore** (Status = Pending).
   - Marks event as **Processing**.
   - Resolves all `IDomainEventHandler<WorkflowTransitionCompletedEvent>` and runs each:
     - Optionally starts a **JobRun** (EventHandling, CorrelationId from event).
     - Calls **HandleAsync** (e.g. log, optional side effects).
     - Completes or fails the JobRun.
   - Marks event **Processed** or **Failed** (and updates RetryCount, LastError, LastHandler as in Phase 2).
6. **WorkflowTransitionCompletedEventHandler** (default): logs the event; JobRun is created by the dispatcher when `IJobRunRecorderForEvents` is registered.

---

## 5. Initial Event Handler

- **WorkflowTransitionCompletedEventHandler** implements `IDomainEventHandler<WorkflowTransitionCompletedEvent>`.
- **Purpose:** Demonstrate event bus; log transition for observability.
- **Behaviour:** Async, idempotent (logging only), safe for retries.
- **JobRun:** Created by the dispatcher via `IJobRunRecorderForEvents`, not inside the handler. Optional side effects (e.g. enqueue background job) can be added in the same or additional handlers.

---

## 6. Handler Failure Handling

If a handler throws:

- The dispatcher catches the exception, logs it, and (if registered) calls **FailHandlerRunAsync** for that handler’s JobRun.
- Other handlers still run.
- After all handlers, the dispatcher calls **IEventStore.MarkProcessedAsync(eventId, success: false, errorMessage, lastHandler)** so the EventStore entry is updated:
  - **RetryCount** incremented.
  - **LastError** set (truncated).
  - **LastHandler** set.
  - **Status** set to **Failed** or **DeadLetter** (after max retries).

---

## 7. Tests

- **Event emission after transition:** `WorkflowEngineServiceTests.ExecuteTransitionAsync_WhenEventDispatcherRegistered_EmitsEventAfterSuccess` — runs a successful transition with a mock `IDomainEventDispatcher` and asserts `PublishAsync` was called once with a `WorkflowTransitionCompletedEvent` that has the correct WorkflowDefinitionId, WorkflowJobId, FromStatus, ToStatus, EntityType, EntityId, CorrelationId, CompanyId, TriggeredByUserId, OccurredAtUtc, and WorkflowTransitionId.
- **Event persistence, handler execution, handler failure, correlation:** Covered by **EventBusPhase1Tests** (dispatch, store append/mark processed, JobRun with CorrelationId, handler success/failure).

---

## 8. Files Touched (Phase 4)

| Area | File | Change |
|------|------|--------|
| Application | Events/WorkflowTransitionCompletedEvent.cs | Added WorkflowTransitionId. |
| Application | Workflow/Services/WorkflowEngineService.cs | Set evt.WorkflowTransitionId = transition.Id when building event. |
| Tests | Workflow/WorkflowEngineServiceTests.cs | ExecuteTransitionAsync_WhenEventDispatcherRegistered_EmitsEventAfterSuccess; CreateServiceWithEventDispatcher helper. |
| Docs | EVENT_BUS_PHASE4_WORKFLOW_EVENTS.md | New. |

No changes to workflow transition logic; emission is additive and does not alter engine semantics.
