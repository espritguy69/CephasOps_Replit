# Event Delivery Pipeline

**Date:** Event Platform Layer phase.  
**Purpose:** Document the outbound event delivery pipeline, retries, dead-letter, and connector abstraction.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Scope

This document covers **outbound** delivery of **integration events** to external systems (webhooks, HTTP push). It does **not** cover internal domain event dispatch (that is the EventStore + IDomainEventDispatcher).

---

## 2. Pipeline overview

1. **Publish:** Application or an event handler calls `IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)`. The envelope contains eventId, eventType, correlationId, companyId, payload, and optional metadata (rootEventId, source, etc.).
2. **Resolve endpoints:** `IConnectorRegistry.GetOutboundEndpointsForEventAsync(eventType, companyId)` returns active connector endpoints that are subscribed to this event type (and optionally scoped to company).
3. **Create delivery records:** For each endpoint, the bus creates one **OutboundIntegrationDelivery** row (or reuses by IdempotencyKey if already present). IdempotencyKey = f(SourceEventId, ConnectorEndpointId). Status = Pending.
4. **Prepare payload:** `IIntegrationPayloadMapper` maps the envelope to JSON. `IOutboundSigner` (if configured) adds signature headers.
5. **Dispatch:** For each Pending delivery, HTTP send is performed via `IOutboundHttpDispatcher`. Result is recorded in **OutboundIntegrationAttempt** (timestamp, HTTP status, error message).
6. **Status update:** Success → Status = Delivered, DeliveredAtUtc set. Failure → Status = Failed, NextRetryAtUtc set (exponential backoff), AttemptCount incremented. After MaxAttempts → Status = DeadLetter.
7. **Replay:** Operator can call Replay API to re-dispatch Failed/DeadLetter deliveries (see EVENT_REPLAY_MODEL.md).

---

## 3. Delivery record (OutboundIntegrationDelivery)

| Field | Purpose |
|-------|---------|
| Id, ConnectorEndpointId, CompanyId | Identity and scope. |
| SourceEventId, EventType, CorrelationId, RootEventId | Traceability. |
| IdempotencyKey | Deduplication (event + endpoint). |
| Status | Pending | Delivered | Failed | DeadLetter | Replaying. |
| PayloadJson | Body sent or to send (may be truncated). |
| AttemptCount, MaxAttempts, NextRetryAtUtc | Retry and backoff. |
| DeliveredAtUtc, LastErrorMessage, LastHttpStatusCode | Outcome. |
| IsReplay, ReplayOperationId | Replay audit. |
| CreatedAtUtc, UpdatedAtUtc | Timestamps. |

---

## 4. Retry and dead-letter

- **Retry:** On HTTP failure, Status = Failed, NextRetryAtUtc = now + backoff (e.g. exponential). Per-endpoint RetryCount/MaxAttempts from ConnectorEndpoint.
- **DeadLetter:** After MaxAttempts, Status = DeadLetter. No automatic further attempts; only replay (on-demand) can re-dispatch.
- **No background retry worker in this phase:** We do **not** run a worker that periodically re-dispatches Pending/Failed with NextRetryAtUtc ≤ now. Replay is explicit via API. If we add a worker later, it would only attempt deliveries that are Pending or Failed and due for retry.

---

## 5. Connector abstraction

- **ConnectorDefinition:** ConnectorKey, DisplayName, ConnectorType, Direction (Outbound/Inbound/Bidirectional). Stored in ConnectorDefinitions.
- **ConnectorEndpoint:** Per definition and optional CompanyId. EndpointUrl, HttpMethod, AllowedEventTypes (outbound filter), SigningConfigJson, AuthConfigJson, RetryCount, TimeoutSeconds, IsPaused, IsActive. Stored in ConnectorEndpoints.
- **IConnectorRegistry:** Resolves endpoints by event type (outbound) or connector key (inbound). New connectors are added by inserting definitions and endpoints; optional custom `IIntegrationPayloadMapper`, `IOutboundSigner`, and handlers can be registered in DI.

---

## 6. Separation of concerns

| Concern | Component | Responsibility |
|---------|-----------|----------------|
| Event preparation | PlatformEventEnvelope, IIntegrationPayloadMapper | Build stable JSON payload and envelope. |
| Signing | IOutboundSigner | Add signature header using endpoint SigningConfigJson. |
| Transport | IOutboundHttpDispatcher | HTTP POST; no business logic. |
| Persistence | IOutboundDeliveryStore | Create/update delivery and attempt records. |
| Retry policy | OutboundIntegrationBus / ConnectorEndpoint | MaxAttempts, NextRetryAtUtc. |

Event preparation and transport are clearly separated; connector-specific logic (mapping, signing) is pluggable.

---

## 7. Security

- **Signing:** IOutboundSigner can add HMAC or other signature; endpoint holds signing config (secret not in code). No built-in HMAC in this phase; add per provider.
- **Auth:** ConnectorEndpoint can store AuthConfigJson (e.g. bearer token); dispatcher adds headers. Secrets in config/store, not logs.
- **Payload:** No secrets in PayloadJson stored for logging; truncate if needed.

---

## 8. Observability

- Each delivery has AttemptCount, LastErrorMessage, LastHttpStatusCode, DeliveredAtUtc.
- OutboundIntegrationAttempt records per-attempt history.
- Operator APIs: list/detail deliveries, filter by endpoint, company, event type, status, date. See EVENT_OBSERVABILITY_MODEL.md.

This document describes the current outbound delivery pipeline; implementation is in OutboundIntegrationBus, IOutboundDeliveryStore, IOutboundHttpDispatcher, and IntegrationController (replay API).
