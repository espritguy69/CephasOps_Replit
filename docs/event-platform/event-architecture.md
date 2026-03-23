# Event Platform Architecture

**Purpose:** Canonical event model and separation of internal vs integration events for the CephasOps event-driven platform.

---

## 1. Canonical Event Model

### 1.1 DomainEvent (internal)

**Type:** `CephasOps.Domain.Events.IDomainEvent` / base class `DomainEvent`.

Used for **in-process and internal event store**: everything that is persisted to EventStore and dispatched to `IDomainEventHandler<T>`.

**Required shape (immutable, tenant-scoped, versionable):**

| Field | Type | Purpose |
|-------|------|---------|
| EventId | Guid | Unique id (set once at creation). |
| EventType | string | Contract name (e.g. `ops.order.created.v1`). |
| Version | string? | Payload/contract version (e.g. `"1"`). |
| CompanyId | Guid? | Tenant scope; must match current tenant for safety. |
| OccurredAtUtc | DateTime | When the fact occurred. |
| CorrelationId | string? | Correlation for tracing. |
| CausationId | Guid? | Causing event/command id. |
| TriggeredByUserId | Guid? | Actor (user) if applicable. |
| Source | string? | Originating module (e.g. WorkflowEngine, Orders). |
| ParentEventId / RootEventId | Guid? | Causality chain (optional). |

**Persistence:** Stored in `EventStore` with JSON payload; `CompanyId` and `PayloadVersion` (Version) are first-class columns. Replay and observability filter by `CompanyId`.

---

### 1.2 IntegrationEvent (outbound boundary)

**Representation:** `CephasOps.Application.Events.PlatformEventEnvelope`.

Used when publishing to **external systems** via `IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)`. One OutboundIntegrationDelivery per connector endpoint is created; payload is sent over HTTP.

**Separation from DomainEvent:**

- **DomainEvent** = internal fact, stored in EventStore, dispatched to internal handlers.
- **IntegrationEvent** = external-facing envelope (EventId, EventName, CompanyId, OccurredAtUtc, Payload, Source, version, trace headers). Built from a DomainEvent using `IDomainEventToPlatformEnvelopeBuilder` (or equivalent) when a handler forwards to the integration bus.

**Rules:**

- Events are **immutable** after creation.
- All events are **tenant-scoped** via `CompanyId`; no cross-tenant leakage.
- Events are **versionable** via `Version` / `PayloadVersion` for schema evolution.

---

## 2. Event Bus

**Interface:** `CephasOps.Application.Events.IEventBus`

| Method | Purpose |
|--------|---------|
| `PublishAsync<TEvent>(TEvent domainEvent, CancellationToken)` | Persist to EventStore (if not already stored), then dispatch to all registered handlers. Tenant context (CompanyId) is on the event. |
| `DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken)` | Dispatch to handlers only; no persist. Use when event was already stored (e.g. dispatcher worker or replay). |

**Subscribe:** Subscription is via **DI**: register `IDomainEventHandler<TEvent>` (or `IEventHandler<TEvent>`) in the service collection. The dispatcher resolves all handlers for the event type and runs them (in-process and/or enqueued for `IAsyncEventSubscriber<T>`).

**Transaction boundary:**

- **Same transaction:** Use `IEventStore.AppendInCurrentTransaction(domainEvent, envelope)` in the same unit of work as the business change (e.g. in WorkflowEngineService), then `SaveChangesAsync`. The background worker will later claim and dispatch.
- **Async after commit:** Use `IEventBus.PublishAsync(domainEvent)` when the event is not part of an existing transaction (e.g. after API create order); this persists and then dispatches.

---

## 3. Event Store Integration

- **Append-only:** No update or delete of event payload; only processing metadata (Status, RetryCount, LastError, etc.) is updated after insert.
- **Schema:** EventStoreEntry includes CompanyId, Payload (JSON), PayloadVersion, Phase 7 lease fields, Phase 8 envelope (RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, TraceId, SpanId, Priority).
- **Replay:** Events can be replayed by EventId; replay respects tenant isolation (scopeCompanyId vs entry.CompanyId) and IEventReplayPolicy.

---

## 4. Observability

- **List/filter events:** `GET /api/observability/events` — filter by CompanyId, EventType, Status, date range, CorrelationId. Tenant-scoped for non–global admins.
- **Event store API:** `GET /api/event-store/events`, failed/dead-letter lists, dashboard, retry, replay, attempt history, related links (JobRuns, WorkflowJobs).
- **Traceability:** EventId, CorrelationId, CausationId, RootEventId; related links API for operational trace.

---

## 5. References

- Runbook: `backend/scripts/EVENT_PLATFORM_RUNBOOK.md`
- Handler guidelines: `docs/event-platform/handler-guidelines.md`
- Replay: `docs/event-platform/replay-strategy.md`
- Tenant safety: `docs/event-platform/tenant-safety.md`
