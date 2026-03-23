# Domain Event Model

**Date:** Event Platform Layer phase.  
**Purpose:** Formalize how domain events are raised, persisted, and dispatched in CephasOps (Clean Architecture).  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Definition

A **domain event** is an immutable record that something business-significant has happened inside the CephasOps bounded context. It is expressed as `IDomainEvent` (implemented via the base class `DomainEvent`) and is used for:

- Audit and replay
- Triggering side effects (e.g. create installer task on OrderAssigned)
- Process managers and sagas
- Projections and ledger
- Optional forwarding to the integration bus as an integration event

Domain events do **not** contain transport or infrastructure concerns; they live in the Domain and Application layers.

---

## 2. Where events are raised

### 2.1 Application services (recommended)

- **Primary pattern:** Application services (e.g. WorkflowEngineService) perform a business operation, then raise one or more domain events **in the same transaction** as the state change.
- Events are **not** raised from domain entities in the current codebase; they are raised from application services that orchestrate use cases. This keeps entities free of dispatch logic and ensures events are raised at a clear boundary (after validation and persistence).

### 2.2 Single emission point per event type

- For workflow-driven events (OrderStatusChanged, OrderAssigned, WorkflowTransitionCompleted), the **only** place that appends these events is **WorkflowEngineService** after a successful transition, using `IEventStore.AppendInCurrentTransaction`. This avoids duplicate or inconsistent lifecycle events.
- For other event types (e.g. JobStarted, JobCompleted, JobFailed), emission is from the service that performs the action (e.g. job processor). New event types should have a single, documented emission point.

### 2.3 Entities

- Domain entities do **not** currently raise or collect domain events (no entity-level event collection). If we later add entity-collected events, they would be gathered by the application service and appended when the unit of work is committed. For this phase we keep the application-service emission pattern.

---

## 3. Persistence vs dispatch

| Step | When | Who |
|------|------|-----|
| **Persist** | In the same transaction as the business state change when possible | Caller (e.g. WorkflowEngineService) via `IEventStore.AppendInCurrentTransaction(evt, envelope)`. |
| **Commit** | Same DbContext.SaveChangesAsync as the business write | Caller. |
| **Claim** | After commit, background worker polls EventStore | EventStoreDispatcherHostedService via `IEventStore.ClaimNextPendingBatchAsync`. |
| **Dispatch** | After claim, for each event in the batch | IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true). |
| **Handlers** | In-process (and optionally async enqueue) | IDomainEventHandler&lt;T&gt; resolved from DI; IEventProcessingLogStore ensures at-most-once per (EventId, Handler). |

**Transaction safety:** Events that must be consistent with a business state change are appended with `AppendInCurrentTransaction` so that if the transaction rolls back, the event is not persisted. Events published outside a transaction use `AppendAsync` (e.g. from an API that does not share the workflow DbContext).

---

## 4. Base shape (IDomainEvent / DomainEvent)

- **EventId** (Guid): Unique per occurrence.
- **EventType** (string): Stable name, e.g. from PlatformEventTypes.
- **Version** (string): Payload/contract version; default "1".
- **OccurredAtUtc** (DateTime): When the thing happened.
- **CorrelationId** (string?): Request or flow correlation.
- **CompanyId** (Guid?): Tenant scope.
- **CausationId** (Guid?): Event or command that caused this event.
- **TriggeredByUserId** (Guid?): User that triggered the action (if any).
- **Source** (string?): Component that emitted (e.g. WorkflowEngine).
- **ParentEventId** (Guid?): Parent event when this is a child in a flow.
- **RootEventId** (Guid?): Origin of the causality chain.

Optional interfaces: **IHasEntityContext** (EntityType, EntityId) for indexing and filtering.

---

## 5. Concrete event types (current)

| Event type | Emission point | Purpose |
|------------|----------------|---------|
| WorkflowTransitionCompletedEvent | WorkflowEngineService (same transaction as transition) | Workflow transition completed; parent for status/assign events. |
| OrderStatusChangedEvent | WorkflowEngineService (same transaction) | Order status changed; drives notifications. |
| OrderAssignedEvent | WorkflowEngineService (same transaction, when target status = Assigned) | Order assigned; drives installer task, material pack, SLA. |
| JobStartedEvent / JobCompletedEvent / JobFailedEvent | Job execution path | Background job lifecycle. |

New domain events should follow the same pattern: define a class extending `DomainEvent` (and optionally `IHasEntityContext`), set EventType and Version, and append from a single application service in the appropriate transaction boundary.

---

## 6. Handler model

- **IDomainEventHandler&lt;TEvent&gt;:** In-process handler. All registered handlers for the event type are invoked (order may be non-deterministic unless we add ordering).
- **IAsyncEventSubscriber&lt;TEvent&gt;:** Extends IDomainEventHandler; may enqueue work for a background job instead of running inline. During replay with SuppressSideEffects, async enqueue can be skipped.
- **IProjectionEventHandler&lt;TEvent&gt;:** For projection-only replay; other handlers can be excluded when replay target is Projection.
- **Process managers:** IProcessManager.HandleEventAsync(domainEvent) can send commands via ICommandBus in response to events; they are invoked as handlers.

Handlers must be **idempotent** by business key (e.g. create task by OrderId only if not exists). EventProcessingLog gives at-most-once per (EventId, Handler); duplicate delivery due to retry should still be safe.

---

## 7. Envelope (platform metadata)

When persisting, the application can attach **EventStoreEnvelopeMetadata** (PartitionKey, RootEventId, SourceService, SourceModule, CapturedAtUtc, Priority, TraceId, SpanId) via **IPlatformEventEnvelopeBuilder**. This is stored in EventStoreEntry for observability and partitioning. The same envelope concept is used when building integration event payloads.

---

## 8. Summary

- Domain events are raised from **application services**, not entities, at a single emission point per event type.
- They are persisted with **AppendInCurrentTransaction** when part of a business transaction, or **AppendAsync** otherwise.
- A **background worker** claims and dispatches to **IDomainEventHandler&lt;T&gt;** with at-most-once per (EventId, Handler).
- No transport or integration concerns in the domain event type; integration forwarding is done in a handler or application layer that maps domain events to integration events.

This document describes the current CephasOps domain event foundation; no code change is required for Phase 3 beyond what already exists.
