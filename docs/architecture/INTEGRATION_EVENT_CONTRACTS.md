# Integration Event Contracts

**Date:** Event Platform Layer phase.  
**Purpose:** Define how CephasOps shapes external event contracts for connectors (naming, envelope, versioning, compatibility).  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md.

---

## 1. Principles

- **Stable naming:** Event type names are stable and versioned; consumers can rely on them.
- **Envelope:** Every integration event carries a standard envelope (timestamp, source, identifiers, correlation).
- **Minimal payload:** Payload contains what subscribers need; no internal-only or PII unless required and documented.
- **Backward compatibility:** Additive changes only for existing versions; breaking changes introduce a new version (e.g. v2).

---

## 2. Event type naming

- **Pattern:** `{area}.{action}.v{version}` or `ops.{area}.{action}.v{version}`.
- **Examples:**
  - `order.status_changed.v1`
  - `order.assigned.v1`
  - `workflow.transition_completed.v1`
- **Constants:** Use a shared constants class (e.g. IntegrationEventTypes or extend PlatformEventTypes) so code and connectors agree. Document in this doc or a generated catalog.

---

## 3. Envelope (standard fields)

Every integration event payload (or HTTP body) should include:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventId | string (Guid) | Yes | Unique id for this event instance. |
| eventType | string | Yes | Event type name (e.g. order.status_changed.v1). |
| occurredAtUtc | string (ISO 8601) | Yes | When the business fact occurred. |
| capturedAtUtc | string (ISO 8601) | No | When CephasOps captured the event. |
| correlationId | string | No | Request or flow correlation. |
| companyId | string (Guid) | No | Tenant; omit or null for global. |
| rootEventId | string (Guid) | No | Origin of causality chain. |
| source | string | No | e.g. "CephasOps". |
| payloadVersion | string | No | Payload schema version; default "1". |
| payload | object | Yes | Event-specific data. |

The **payload** object is event-specific; envelope fields are consistent across event types.

---

## 4. Payload philosophy

- **Additive:** New optional fields can be added; existing fields are not removed or renamed in the same version.
- **No secrets:** No passwords, tokens, or PII in payload unless required and documented; use references (e.g. orderId) instead of full objects where appropriate.
- **Ids over nesting:** Prefer entity IDs and minimal context; subscribers can call back for details if needed.

---

## 5. Versioning strategy

- **v1, v2, …:** Integer version in the event type name or in payloadVersion. New version when:
  - Breaking change (removed/renamed field, type change).
  - Significant semantic change that could break consumers.
- **Support policy:** Document how long old versions are supported (e.g. “v1 supported for 12 months after v2 release”). Deprecation notice in docs and optionally in envelope (e.g. deprecationAt).

---

## 6. Source metadata

- **source:** Always "CephasOps" (or configurable service name) so consumers know the origin.
- **companyId:** Set when event is company-scoped; required for multi-tenant connectors. Omit for system-wide events.

---

## 7. Correlation and tracing

- **correlationId:** Propagated from domain event; same value across a flow so consumers can correlate with their own requests.
- **rootEventId:** Same across a causality chain; useful for “all events from this root”.
- **TraceId/SpanId:** Optional; include in envelope if we have Activity (distributed tracing). Not required for this phase.

---

## 8. Mapping from domain events

- **IIntegrationPayloadMapper** (or equivalent) maps domain event + PlatformEventEnvelope to the integration JSON. Domain event type (e.g. OrderStatusChangedEvent) maps to integration event type (e.g. order.status_changed.v1). Payload is built from domain event properties; envelope fields are filled from envelope metadata.
- **One domain event → one integration event type:** But one integration event can be delivered to many endpoints (one OutboundIntegrationDelivery per endpoint). Payload is the same; endpoint may filter by event type.

---

## 9. Documented event catalog (recommended)

Maintain a list (in this doc or a separate catalog) of:

- Event type name and version
- When it is emitted (which domain event or action)
- Envelope + payload schema (or link to schema)
- Deprecation status

Example:

| Integration event type | Emitted when | Payload summary |
|-------------------------|--------------|------------------|
| order.status_changed.v1 | Order status transition (WorkflowEngineService) | orderId, fromStatus, toStatus, workflowJobId, entityType, entityId. |
| order.assigned.v1 | Order assigned to installer | orderId, workflowJobId, entityType, entityId. |
| workflow.transition_completed.v1 | Any workflow transition | workflowDefinitionId, transitionId, workflowJobId, entityType, entityId, fromStatus, toStatus. |

---

## 10. Backward-compatibility expectations

- **Additive only for same version:** New optional fields, new event types. No removal or renaming.
- **New version for breaking changes:** New event type or new payloadVersion; old version deprecated with notice period.
- **Connector responsibility:** Connectors should ignore unknown fields and handle missing optional fields; CephasOps documents required vs optional.

This document defines the integration event contract approach; implementation uses IIntegrationPayloadMapper and PlatformEventEnvelope; specific event types and payloads are defined in code and can be listed in the catalog above or in a separate file.
