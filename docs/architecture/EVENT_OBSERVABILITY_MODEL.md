# Event Observability Model

**Date:** Event Platform Layer phase.  
**Purpose:** Define how to observe event lifecycle, delivery, and receipt across the event platform.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md.

---

## 1. Goals

- **Event lifecycle visibility:** For a domain event, see when it was persisted, when it was processed or failed, which handler failed, and (if forwarded) the resulting outbound delivery status.
- **Delivery lifecycle:** For each outbound delivery, see status, attempt count, last error, last HTTP status, next retry.
- **Receipt lifecycle:** For each inbound receipt, see status, verification result, handler error, processed at.
- **Correlation:** Use CorrelationId, RootEventId, EventId to tie domain events, outbound deliveries, and (where applicable) inbound receipts together for support and debugging.

---

## 2. Key identifiers

| Identifier | Where | Use |
|------------|--------|-----|
| **EventId** | Domain event, EventStoreEntry, OutboundIntegrationDelivery.SourceEventId | Unique event; link outbound delivery to source event. |
| **CorrelationId** | Domain event, EventStoreEntry, OutboundIntegrationDelivery, InboundWebhookReceipt | Request or flow; query “all records for this correlation”. |
| **RootEventId** | EventStoreEntry, OutboundIntegrationDelivery | Origin of causality chain. |
| **ConnectorEndpointId** | OutboundIntegrationDelivery, InboundWebhookReceipt | Which connector/endpoint. |
| **Receipt Id** | InboundWebhookReceipt | Inbound receipt identity. |
| **Delivery Id** | OutboundIntegrationDelivery | Outbound delivery identity. |

---

## 3. Domain event lifecycle (EventStore)

| State | Meaning |
|-------|---------|
| Pending | Persisted, not yet claimed by dispatcher. |
| Processing | Claimed; handlers running. |
| Processed | All handlers completed successfully. |
| Failed | At least one handler failed; will retry (NextRetryAtUtc). |
| DeadLetter | Max retries exceeded or non-retryable. |

**Observable fields:** EventId, EventType, Status, OccurredAtUtc, CreatedAtUtc, ProcessedAtUtc, RetryCount, NextRetryAtUtc, LastError, LastErrorAtUtc, LastHandler, CorrelationId, CompanyId, EntityType, EntityId, RootEventId, PartitionKey, SourceService, SourceModule.

**Queries:** By EventId, CorrelationId, CompanyId, EventType, Status, date range. Optional: EventStoreAttemptHistory for per-attempt audit.

---

## 4. Outbound delivery lifecycle

| State | Meaning |
|-------|---------|
| Pending | Delivery created; not yet sent (or send in progress). |
| Delivered | HTTP success. |
| Failed | HTTP failure; will retry (NextRetryAtUtc) or replay. |
| DeadLetter | Max attempts exceeded. |
| Replaying | Replay in progress. |

**Observable fields:** Id, SourceEventId, EventType, ConnectorEndpointId, CompanyId, Status, AttemptCount, MaxAttempts, NextRetryAtUtc, DeliveredAtUtc, LastErrorMessage, LastHttpStatusCode, CorrelationId, CreatedAtUtc, UpdatedAtUtc. OutboundIntegrationAttempt for per-attempt history.

**Queries:** By delivery Id, SourceEventId, ConnectorEndpointId, CompanyId, EventType, Status, date range.

---

## 5. Inbound receipt lifecycle

| State | Meaning |
|-------|---------|
| Received | HTTP received; not yet verified. |
| Verified | Verification passed (or skipped). |
| Processing | Handler running. |
| Processed | Handler completed successfully. |
| VerificationFailed | Signature/timestamp verification failed. |
| HandlerFailed | Handler threw. |
| DeadLetter | Optional terminal state. |

**Observable fields:** Id, ConnectorKey, ExternalEventId, ExternalIdempotencyKey, Status, VerificationPassed, ReceivedAtUtc, ProcessedAtUtc, HandlerErrorMessage, HandlerAttemptCount, CompanyId, CorrelationId.

**Queries:** By receipt Id, ConnectorKey, CompanyId, Status, date range, ExternalIdempotencyKey.

---

## 6. Correlation and causation

- **CorrelationId:** Propagate from API request or workflow job through domain events and integration envelope. Use to list “all events and deliveries for this request”.
- **CausationId / ParentEventId:** EventStore and domain events; use to build “event A caused event B” chains.
- **RootEventId:** Same across a chain; use to group all events and outbound deliveries that belong to one root cause.

A future “event by correlation id” API could return: EventStore entries + OutboundIntegrationDelivery rows + (optionally) InboundWebhookReceipts that share the same CorrelationId or RootEventId. This phase does not require a single dashboard; APIs and queries are sufficient.

---

## 7. Operational visibility

- **Admin/support:** List and filter events (EventStore), deliveries (OutboundIntegrationDeliveries), receipts (InboundWebhookReceipts) by status, company, date, event type, connector. Detail by ID.
- **Logging:** Structured logs with EventId, CorrelationId, CompanyId, EventType, Status in dispatcher, bus, and runtime. No secrets in logs.
- **Metrics (optional):** Counts by event type, status, handler; delivery success/failure rates; receipt verification/handler failure rates. Can be added via existing metrics hooks.

---

## 8. What we do not do in this phase

- We do **not** mandate a single “event platform dashboard” UI. Operator APIs and query surfaces (e.g. existing IntegrationController, EventLedgerController, or new admin endpoints) are enough.
- We do **not** add distributed tracing beyond existing TraceId/SpanId in envelope where Activity is available.

This document defines the observability model; implementation relies on existing EventStore, OutboundIntegrationDelivery, OutboundIntegrationAttempt, InboundWebhookReceipt, and operator APIs. New endpoints that aggregate by CorrelationId or RootEventId can be added in a follow-up if needed.
