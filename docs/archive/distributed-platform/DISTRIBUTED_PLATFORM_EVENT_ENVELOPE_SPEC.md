# CephasOps Platform Event Envelope Specification

**Date:** 2026-03-09  
**Status:** Implemented (Phase 1)

---

## 1. Envelope fields (required / standard)

Every domain event persisted to the EventStore and dispatched to handlers must support the following. Implement via `IDomainEvent` and base class `DomainEvent`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| **EventId** | Guid | Yes | Unique id for this event instance. |
| **EventType** | string | Yes | Platform event type name; use `ops.<area>.<action>.v<version>`. |
| **Version** | string | Yes (default "1") | Payload/contract version for version-safe handling. Stored as PayloadVersion. |
| **OccurredAtUtc** | DateTime | Yes | When the event occurred (UTC). |
| **CorrelationId** | string | No | Correlation id for request/trace chain. |
| **CompanyId** | Guid? | No | Tenant/company scope. Must be set for multi-tenant safety. |
| **CausationId** | Guid? | No | Event or command that caused this event (causation chain). |
| **TriggeredByUserId** | Guid? | No | User that triggered (legacy; use ActorId when IHasActor is used). |
| **Source** | string | No | Component that emitted the event (e.g. WorkflowEngine). |
| **EntityType** | string | No | Optional; via IHasEntityContext (e.g. Order, Assurance). |
| **EntityId** | Guid? | No | Optional; via IHasEntityContext. |
| **ParentEventId** | Guid? | No | When this event is a child of another (lineage). |

---

## 2. Event type naming

- **Pattern:** `ops.<area>.<action>.v<version>`
- **Examples:**
  - `ops.workflow.transition_completed.v1`
  - `ops.order.status_changed.v1`
  - `ops.order.assigned.v1`
- **Constants:** Use `PlatformEventTypes` in `CephasOps.Application.Events` for known types.
- **Legacy:** Legacy type names (e.g. `WorkflowTransitionCompleted`, `OrderStatusChanged`) remain registered in `EventTypeRegistry` for replay compatibility.

---

## 3. Persistence (EventStoreEntry)

- EventStore table stores: EventId, EventType, Payload (JSON), OccurredAtUtc, CreatedAtUtc, Status, CorrelationId, CompanyId, TriggeredByUserId, Source, EntityType, EntityId, ParentEventId, **CausationId**, PayloadVersion (from Version), RetryCount, NextRetryAtUtc, ProcessingStartedAtUtc, LastError, LastErrorAtUtc, LastHandler (and Phase 7 lease fields when used).
- Append via `IEventStore.AppendInCurrentTransaction(evt)` in the **same transaction** as business state change (transactional outbox).

---

## 4. Outbox and inbox

- **Outbox:** Events are appended in the same transaction as the business change; a background worker (`EventStoreDispatcherHostedService`) claims Pending/Failed-due events and dispatches them. No separate broker required in Phase 1.
- **Inbox / idempotency:** `EventProcessingLog` records (EventId, HandlerName) ensure each handler runs at most once per event. Handlers must be idempotent by design.

---

## 5. Causation and correlation

- Set **CausationId** to the EventId of the event that caused this one (e.g. OrderStatusChanged caused by WorkflowTransitionCompleted).
- Set **ParentEventId** for child events in a flow. CorrelationId should be consistent across a request or flow.
