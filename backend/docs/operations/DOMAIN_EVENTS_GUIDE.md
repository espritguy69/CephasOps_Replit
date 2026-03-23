# Domain Events Guide

**Date:** 2026-03-13  
**Purpose:** How to define, publish, and handle domain events in CephasOps in a tenant-safe way.

---

## 1. Domain event contract (IDomainEvent)

Every domain event must implement `IDomainEvent` (or extend `DomainEvent`). Required:

- **EventId** (Guid) — Unique per event; set to `Guid.NewGuid()` if new.
- **EventType** (string) — Stable name for the event (e.g. `PlatformEventTypes.OrderCreated`).
- **Version** (string) — Payload contract version (e.g. `"1"`).
- **OccurredAtUtc** (DateTime) — When the thing happened.
- **CompanyId** (Guid?) — **Tenant context.** Set for all tenant-scoped events; null only for platform-wide events.
- **CorrelationId** (string) — Optional; for request/trace correlation.
- **CausationId** (Guid?) — Optional; EventId of the causing command/event.
- **TriggeredByUserId** (Guid?) — Optional; user who triggered the action.
- **Source** (string) — Optional; module or service name (e.g. `"Orders"`, `"WorkflowEngine"`).

For entity-scoped events, implement **IHasEntityContext** (EntityType, EntityId) so the event store and handlers can use aggregate type and id.

---

## 2. Event envelope (persisted)

When an event is appended to the store, it is persisted as **EventStoreEntry** with:

- EventId, EventType, Payload (JSON), OccurredAtUtc, CompanyId, CorrelationId, CausationId, TriggeredByUserId, Source
- EntityType, EntityId (from IHasEntityContext)
- Status (Pending | Processing | Processed | Failed | DeadLetter), RetryCount, NextRetryAtUtc, ProcessingStartedAtUtc
- ParentEventId, RootEventId, PartitionKey, IdempotencyKey, etc.

Handlers never see the raw entry; they receive the deserialized **IDomainEvent** when the dispatcher runs them.

---

## 3. Publishing events

**Preferred:** Use **IEventBus.PublishAsync** from application services or API. The bus will:

1. Append the event to the event store (if not already stored).
2. Mark it as Processing.
3. Dispatch to all registered sync handlers.
4. Enqueue async handlers (unless replay with SuppressSideEffects).

Ensure **CompanyId** is set on the event for tenant-scoped operations (e.g. from `TenantScope.CurrentTenantId` or the aggregate’s CompanyId).

**Same-transaction emit:** When the event must be committed in the same DB transaction as other changes (e.g. workflow transition), use **IEventStore.AppendInCurrentTransaction** and then call **IDomainEventDispatcher.DispatchToHandlersAsync** after the transaction commits, or rely on the background dispatcher to pick up the new row. WorkflowEngineService uses AppendInCurrentTransaction for workflow/order events.

---

## 4. Handlers

- **IDomainEventHandler&lt;TEvent&gt;** — Implement `HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)`. Registered in DI per event type. Runs in-process; tenant scope is set by the dispatcher (from entry.CompanyId when events come from the store).
- **IAsyncEventSubscriber&lt;TEvent&gt;** — Same interface, but the dispatcher will not run it in-process; it will enqueue a job. The job runs later under the job’s CompanyId; EventHandlingAsyncJobExecutor enforces that job.CompanyId == event.CompanyId.

**Tenant safety in handlers:**

- Handlers run under the scope set by EventStoreDispatcherHostedService (`TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`). Do not switch tenant or use platform bypass unless the operation is explicitly platform-wide.
- If your handler writes tenant-scoped data, use the event’s CompanyId (and optionally TenantScope.CurrentTenantId for consistency). Do not use a different company id.
- If the event has no CompanyId (platform event), do not write tenant-scoped rows unless you have another explicit tenant context.

---

## 5. Example event types (existing)

| Event type | When | CompanyId | Typical handlers |
|------------|------|-----------|------------------|
| OrderCreatedEvent | Order created (API or parser) | Set from order/context | Integration forward, TenantActivityTimeline |
| OrderCompletedEvent | Order reaches completed status | Set from workflow | Automation (invoice), Insights, Integration, TenantActivityTimeline |
| OrderAssignedEvent | Order assigned to installer | Set from workflow | OrderAssignedOperationsHandler, Integration, TenantActivityTimeline |
| OrderStatusChangedEvent | Order status transition | Set from workflow | Ledger, Notification dispatch, Integration, TenantActivityTimeline |
| WorkflowTransitionCompletedEvent | Any workflow transition | Set from entity | Ledger, projection, Integration |
| InvoiceGeneratedEvent | Invoice created | Set from billing | Integration |
| MaterialIssuedEvent / MaterialReturnedEvent | Stock movement | Set from inventory | Integration |
| PayrollCalculatedEvent | Payroll run | Set from payroll | Integration |
| JobStartedEvent / JobCompletedEvent / JobFailedEvent | Background job lifecycle | Set from job | Observability |

---

## 6. Adding a new event type

1. Add a class extending `DomainEvent` and implementing `IHasEntityContext` if entity-scoped. Set EventType, Version, Source in the constructor.
2. Register the type in **EventTypeRegistry** (Replay/EventTypeRegistry.cs) so the dispatcher can deserialize it from the store.
3. Publish from your service with `_eventBus.PublishAsync(evt, cancellationToken)` and ensure CompanyId is set.
4. Register one or more **IDomainEventHandler&lt;TEvent&gt;** in Program.cs.
5. Document the event and handler in this guide or ASYNC_EVENT_BUS_ARCHITECTURE.md.

---

## 7. References

- [ASYNC_EVENT_BUS_ARCHITECTURE.md](ASYNC_EVENT_BUS_ARCHITECTURE.md) — Overall architecture and dispatch strategy.
- [EVENT_HANDLING_GUARDRAILS.md](EVENT_HANDLING_GUARDRAILS.md) — Tenant safety and the five guardrails.
