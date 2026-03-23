# Event Bus Implementation — Phase 1 (Correlation + Event Emission)

**Date:** 2026-03-09  
**Purpose:** Document the Phase 1 event bus: domain event foundation, correlation, workflow transition events, event store, handler framework, JobRun integration, and failure handling.

---

## 1. Domain Event Architecture

### 1.1 Base Types

- **IDomainEvent** (`CephasOps.Domain.Events`): Marker interface with `EventId`, `EventType`, `OccurredAtUtc`, `CorrelationId`, `CompanyId`, `TriggeredByUserId`, `Source`.
- **DomainEvent** (`CephasOps.Domain.Events`): Abstract base class implementing `IDomainEvent`; all domain events inherit from it.
- **WorkflowTransitionCompletedEvent** (`CephasOps.Application.Events`): Emitted after every successful workflow transition. Adds `WorkflowDefinitionId`, `WorkflowJobId`, `FromStatus`, `ToStatus`, `EntityType`, `EntityId`.

### 1.2 Dispatcher

- **IDomainEventDispatcher** / **DomainEventDispatcher** (`CephasOps.Application.Events`):
  - **PublishAsync&lt;TEvent&gt;(event)**: Appends to event store (if `IEventStore` registered), then dispatches to all `IDomainEventHandler<TEvent>`; marks event processed (or failed) in store.
  - **DispatchToHandlersAsync&lt;TEvent&gt;(event)**: Dispatches only (no persist); used when event was already stored.
- Handlers are resolved via `IServiceProvider.GetServices<IDomainEventHandler<TEvent>>()`. Supports multiple handlers per event type.
- **IJobRunRecorderForEvents**: Optional; each handler run can create a JobRun (JobType = "EventHandling") so handler execution is observable.

### 1.3 Event Store

- **IEventStore** (`CephasOps.Domain.Events`): `AppendAsync(domainEvent)`, `MarkProcessedAsync(eventId, success, errorMessage)`.
- **EventStoreRepository** (`CephasOps.Infrastructure.Persistence`): Persists to **EventStore** table (append-only). On failure, increments `RetryCount` and sets `Status` to "Failed" or "DeadLetter" when `RetryCount >= 5`.

---

## 2. Correlation Propagation

Correlation connects **HTTP request → Workflow transition → JobRun → Event**.

| Component | Where CorrelationId is set/stored |
|-----------|-----------------------------------|
| **HTTP** | `CorrelationIdMiddleware` sets `X-Correlation-Id` (or generates one) and stores in `HttpContext.Items["CorrelationId"]`. |
| **Workflow** | `WorkflowController` sets `dto.CorrelationId ??= _correlationIdProvider.GetCorrelationId()` before calling the engine. `WorkflowEngineService` sets `job.CorrelationId = dto.CorrelationId` on `WorkflowJob`. |
| **JobRun** | `StartJobRunDto.CorrelationId` is set when recording job runs (e.g. from background job or event handler). `JobRun` entity already has `CorrelationId`. |
| **Event** | Every `IDomainEvent` has `CorrelationId`; `WorkflowTransitionCompletedEvent` is built with `CorrelationId` from DTO or job. Event store row includes `CorrelationId`. |

**ICorrelationIdProvider** (`CephasOps.Application.Common.Interfaces`): Returns current correlation ID (e.g. from HTTP). Implemented in Api as **CorrelationIdProvider** (reads from `HttpContext.Items` set by middleware).

---

## 3. Event Lifecycle

1. **Emit**: After a successful workflow transition, `WorkflowEngineService` creates `WorkflowTransitionCompletedEvent` and calls `_domainEventDispatcher.PublishAsync(evt)`. Transition logic is **unchanged**; emission runs after commit. If PublishAsync throws, the exception is caught and logged; the transition is **not** rolled back.
2. **Persist**: `DomainEventDispatcher.PublishAsync` calls `IEventStore.AppendAsync(domainEvent)` (if store registered). Event is written to **EventStore** with `Status = Pending`.
3. **Dispatch**: For each `IDomainEventHandler<WorkflowTransitionCompletedEvent>`, the dispatcher optionally starts a **JobRun** (via `IJobRunRecorderForEvents`), runs `HandleAsync`, then completes or fails the JobRun. Handler failures are logged; other handlers still run. Event is then marked processed (or failed) in the store.

---

## 4. Integration with Workflow Engine

- **No change to transition semantics.** Guards, side effects, and status update behave as before.
- **Additive only:** `WorkflowEngineService` has an optional `IDomainEventDispatcher?`; when null, no event is published.
- **Emission point:** Immediately after `job.State = Succeeded`, `job.CompletedAt = DateTime.UtcNow`, and `SaveChangesAsync`, the engine builds `WorkflowTransitionCompletedEvent` (with `CorrelationId`, `CompanyId`, `WorkflowJobId`, `FromStatus`, `ToStatus`, `EntityType`, `EntityId`) and calls `PublishAsync`. Any exception from PublishAsync is caught and logged.

---

## 5. Integration with JobRun (Job Observability)

- **Event handler runs:** When `IJobRunRecorderForEvents` is registered, the dispatcher calls `StartHandlerRunAsync(domainEvent, handlerName)` before each handler, then `CompleteHandlerRunAsync` or `FailHandlerRunAsync` after. This creates **JobRun** rows with `JobType = "EventHandling"`, `TriggerSource = "EventBus"`, and the event’s `CorrelationId`, so trace is: **Request → WorkflowTransition → Event → JobRun**.
- **WorkflowJob:** `WorkflowJob.CorrelationId` is set from `ExecuteTransitionDto.CorrelationId`, so workflow execution is tied to the same correlation id as the request and the emitted event.

---

## 6. EventStore Table (Append-Only)

| Column | Type | Description |
|--------|------|-------------|
| EventId | uuid | PK |
| EventType | varchar(200) | e.g. WorkflowTransitionCompleted |
| Payload | jsonb | Serialized event |
| OccurredAtUtc | timestamptz | When the event occurred |
| ProcessedAtUtc | timestamptz? | When dispatch completed |
| RetryCount | int | Number of failure/retry cycles |
| Status | varchar(50) | Pending, Processed, Failed, DeadLetter |
| CorrelationId | varchar(100)? | For tracing |
| CompanyId | uuid? | Tenant |

Indexes: `(CompanyId, EventType, OccurredAtUtc)`, `(CorrelationId)`, `(Status)`.

---

## 7. Failure Handling

- **RetryCount:** Incremented in `MarkProcessedAsync` when `success == false`.
- **Poison detection:** When `RetryCount >= 5` (configurable via `EventStoreRepository.MaxRetriesBeforeDeadLetter`), `Status` is set to **DeadLetter**.
- **Failed handlers:** Observable via **JobRun** (Status = Failed, ErrorMessage/ErrorDetails). Event store row is marked Failed (or DeadLetter) so poison events can be queried.

---

## 8. Files Added/Modified

| Area | Files |
|------|--------|
| **Domain** | `Events/IDomainEvent.cs`, `Events/DomainEvent.cs`, `Events/EventStoreEntry.cs`, `Events/IEventStore.cs` |
| **Application** | `Events/WorkflowTransitionCompletedEvent.cs`, `Events/IDomainEventHandler.cs`, `Events/IDomainEventDispatcher.cs`, `Events/DomainEventDispatcher.cs`, `Events/IJobRunRecorderForEvents`, `Events/JobRunRecorderForEvents.cs`, `Events/WorkflowTransitionCompletedEventHandler.cs`, `Common/Interfaces/ICorrelationIdProvider.cs`; `Workflow/DTOs/WorkflowJobDto.cs` (CorrelationId); `Workflow/Services/WorkflowEngineService.cs` (optional dispatcher, emit event, job.CorrelationId) |
| **Infrastructure** | `Persistence/EventStoreRepository.cs`, `Persistence/Configurations/Events/EventStoreEntryConfiguration.cs`; `ApplicationDbContext` (DbSet EventStore, using Domain.Events); `Configurations/Workflow/WorkflowJobConfiguration.cs` (CorrelationId) |
| **Api** | `Services/CorrelationIdProvider.cs`; `Controllers/WorkflowController.cs` (inject ICorrelationIdProvider, set dto.CorrelationId) |
| **Database** | Migration `AddEventBusCorrelationAndEventStore`: add `WorkflowJobs.CorrelationId`, create `EventStore` table |

---

## 9. Registration (Program.cs)

- `ICorrelationIdProvider` → `CorrelationIdProvider`
- `IEventStore` (Domain) → `EventStoreRepository`
- `IJobRunRecorderForEvents` → `JobRunRecorderForEvents`
- `IDomainEventDispatcher` → `DomainEventDispatcher`
- `IDomainEventHandler<WorkflowTransitionCompletedEvent>` → `WorkflowTransitionCompletedEventHandler`

All scoped. Workflow engine receives `IDomainEventDispatcher` by constructor injection (optional).

---

This completes Phase 1: domain event infrastructure, event emission after workflow transitions, system-wide correlation, event persistence, and observable event processing via JobRun, without changing existing workflow engine behaviour.
