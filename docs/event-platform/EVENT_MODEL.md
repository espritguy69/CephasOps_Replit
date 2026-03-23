# Canonical Event Model

## DomainEvent (internal)

Internal domain events implement `IDomainEvent` and extend `DomainEvent` where applicable.

**Canonical structure:**

| Field | Type | Description |
|-------|------|-------------|
| EventId | Guid | Unique event identifier. |
| EventType | string | Versioned type name (e.g. `ops.order.assigned.v1`). |
| Version | string? | Payload/contract version. |
| CompanyId | Guid? | Tenant scope; must match current tenant when publishing. |
| OccurredAtUtc | DateTime | When the fact occurred. |
| CorrelationId | string? | Correlation for tracing. |
| CausationId | Guid? | Event or command that caused this event. |
| TriggeredByUserId | Guid? | User that triggered (if applicable). |
| Source | string? | Source service/module. |
| ParentEventId | Guid? | Parent event when this is a child. |
| RootEventId | Guid? | Root of causality chain. |

**Rules:**

- **Immutable:** Event instances are not modified after creation.
- **Tenant-scoped:** Every event must have `CompanyId` set when the operation is tenant-scoped; publish path should enforce `CompanyId == TenantScope.CurrentTenantId` where applicable.
- **Versionable:** Use `EventType` and `Version` for backward-compatible evolution; `PlatformEventTypes` holds canonical type names.

**Persistence:** Stored in `EventStore` (PayloadJson, CompanyId, EventType, Status, etc.). Schema supports versioning via PayloadVersion and EventType.

---

## IntegrationEvent (outbound)

Outbound integration events are represented as **PlatformEventEnvelope** when publishing to `IOutboundIntegrationBus`.

**Envelope fields (summary):** EventId, EventType, CompanyId, OccurredAtUtc, CorrelationId, RootEventId, PayloadJson, SourceService, SourceModule, etc.

**Separation:**

- **DomainEvent:** Internal fact; persisted in EventStore; dispatched to in-process handlers and optional async jobs. May be forwarded to integration bus by a handler.
- **IntegrationEvent (envelope):** External contract; creates OutboundIntegrationDelivery per connector endpoint; delivered over HTTP with signing/auth. Not stored in EventStore; delivery records in OutboundIntegrationDeliveries.

**Bridge:** `IntegrationEventForwardingHandler` maps selected domain events to `PlatformEventEnvelope` and calls `IOutboundIntegrationBus.PublishAsync`. No automatic forwarding of all domain events; only registered event types are forwarded.
