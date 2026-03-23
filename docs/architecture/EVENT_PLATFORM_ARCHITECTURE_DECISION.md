# Event Platform — Architecture Decision

**Date:** Event Platform Layer phase.  
**Status:** Decision record.  
**Depends on:** EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Domain events vs integration events

### 1.1 Domain events

- **Definition:** Business-significant occurrences inside the CephasOps bounded context, expressed as `IDomainEvent` and persisted in the **EventStore** (internal outbox).
- **Characteristics:**
  - Raised by application/domain code (e.g. WorkflowEngineService after a transition).
  - Stored in the same transaction as the business state change when possible (`AppendInCurrentTransaction`).
  - Consumed in-process by `IDomainEventHandler<T>` (and optionally enqueued via `IAsyncEventSubscriber<T>`).
  - Used for: side effects (installer task, SLA enqueue, notifications), process managers, ledger, projections, and optionally forwarding to the integration bus.
- **Examples:** OrderStatusChanged, OrderAssigned, WorkflowTransitionCompleted, (future) DocketReceived, InvoiceCreated, PayrollRunCompleted, BackgroundJobFailed.
- **Ownership:** Domain/Application own the event type and payload shape; no third-party contract in the domain.

### 1.2 Integration events

- **Definition:** External-facing event contracts emitted to connectors (webhooks, HTTP push). Represent a **stable, versioned** contract for consumers outside CephasOps.
- **Characteristics:**
  - Built from domain events (or from application actions) via a mapping layer.
  - Delivered via **IOutboundIntegrationBus**; one `OutboundIntegrationDelivery` per connector endpoint per event.
  - Payload shape is defined by integration contract (naming, version, envelope); backward compatibility is required.
- **Examples:** `order.status_changed.v1`, `order.assigned.v1`, `workflow.transition_completed.v1` (names and versions defined in INTEGRATION_EVENT_CONTRACTS).
- **Ownership:** Application/Integration layer; domain does not reference integration payloads.

### 1.3 Relationship

- Domain events are the source of truth inside the platform.
- Integration events are a **projection** of domain (or application) facts for external delivery.
- One domain event can result in zero or more integration events (one per subscribed connector/endpoint). Mapping and filtering are in the application/integration layer.

---

## 2. Event persistence vs event delivery

| Concept | Meaning in CephasOps |
|---------|----------------------|
| **Event persistence** | Writing the event to durable storage so it is not lost and can be replayed or audited. For domain events: EventStore. For integration: OutboundIntegrationDelivery row (and optionally a future IntegrationOutbox if we add same-transaction integration outbox). |
| **Event delivery** | Actually sending the event to a consumer. For domain events: dispatching to in-process (and async) handlers. For integration events: HTTP POST to connector endpoint. |

**Decision:**

- **Domain events:** Persist first (EventStore, same transaction as business when possible), then a worker claims and dispatches to handlers. Persistence and delivery are decoupled; delivery is at-least-once with handler-level idempotency.
- **Integration events:** Today we persist the **delivery record** (OutboundIntegrationDelivery) when we decide to send; then we perform HTTP delivery. So “persistence” here is the delivery row; we do not persist a separate integration event in the same transaction as the business write. If we need true transactional integration outbox later, we will add an IntegrationOutbox table and a worker that creates deliveries from it.

---

## 3. Outbox vs background job responsibilities

| Concern | Owner | Responsibility |
|---------|--------|----------------|
| **Internal event outbox** | EventStore + EventStoreDispatcherHostedService | Persist domain events in same transaction as business; claim and dispatch to domain handlers; retry and dead-letter. |
| **Integration delivery** | OutboundIntegrationBus + OutboundIntegrationDelivery | Create delivery record per endpoint; attempt HTTP send; record attempts; retry and dead-letter; replay API. No background retry worker in current scope (replay on-demand). |
| **Background jobs** | JobDefinition, JobRun, JobExecutionWorkerHostedService | One-off or scheduled work (e.g. PnL rebuild, SLA evaluation, payroll). Not event persistence. Jobs may *consume* events or *emit* events but are not the event bus. |

**Decision:** Do not overload the job system with event dispatch. EventStore is the outbox for domain events. OutboundIntegrationDelivery is the record of outbound integration delivery. Background jobs remain for orchestrated/scheduled work.

---

## 4. Inbox / receipt responsibilities

| Concern | Owner | Responsibility |
|--------|--------|----------------|
| **Inbound webhook receipt** | InboundWebhookReceipt + InboundWebhookRuntime | Receive HTTP request → verify → persist receipt → claim idempotency → normalize → run handler → mark Processed/HandlerFailed. |
| **Idempotency** | ExternalIdempotencyRecord + IExternalIdempotencyStore | One completed record per (connectorKey, externalIdempotencyKey). Prevents duplicate handler execution on sender retries. |
| **Receipt processing history** | InboundWebhookReceipt (Status, HandlerErrorMessage, HandlerAttemptCount, ProcessedAtUtc) | Enough for observability and manual replay; no separate “attempt” table for inbound in current design. |

**Decision:** Inbound receipt is the inbox. Idempotency is mandatory for handler execution. Replay of HandlerFailed receipts (re-run handler) is in scope as an operational capability; document and implement if not already present.

---

## 5. Idempotency model

| Layer | Mechanism | Guarantee |
|-------|------------|-----------|
| **Domain event handlers** | EventProcessingLog (EventId + HandlerName) | At-most-once per (event, handler). Handlers must be idempotent by business key (e.g. OrderId for task creation). |
| **Outbound integration** | IdempotencyKey = f(SourceEventId, ConnectorEndpointId) | One delivery row per (event, endpoint). Duplicate PublishAsync with same key does not create a second delivery. |
| **Inbound webhook** | ExternalIdempotencyKey (e.g. connectorKey + externalEventId or body hash) | One completed processing per key. Duplicate request returns 200 with idempotencyReused. |
| **Commands** | CommandProcessingLog (IdempotencyKey) | At-most-once per command key. |

**Decision:** We do not change these. Document them in the observability and runbook docs. New handlers and connectors must respect idempotency.

---

## 6. Replay boundaries

| Replay type | What is replayed | Safety | Owner |
|-------------|------------------|--------|--------|
| **EventStore (domain)** | Stored domain events by ID or filter (e.g. status=Failed, date range). Dispatcher re-dispatches to handlers. | IReplayExecutionContext.SuppressSideEffects for projection-only; handler idempotency required for full replay. | EventStoreDispatcherHostedService + replay API |
| **Outbound integration** | Failed/DeadLetter OutboundIntegrationDelivery rows. Re-dispatches HTTP to endpoint. | Idempotent endpoints; same delivery row re-attempted. | ReplayAsync(ReplayOutboundRequest) |
| **Inbound receipt** | HandlerFailed receipts: re-run handler for same receipt. | Idempotency key already used; handler must be idempotent. | Optional “replay receipt” API |

**Decision:**

- Replay is **explicit** (by ID, status, or filter). No “re-run everything” without a clear scope.
- Document what each replay type does and what it does *not* do (e.g. outbound replay does not re-create domain events).
- EventStore replay can target “projection only” (IProjectionEventHandler) to avoid side effects.

---

## 7. Event contract versioning (integration)

- **Naming:** Stable, dotted names; e.g. `order.status_changed.v1`. Version in the name or in envelope.
- **Envelope:** Timestamp, source (CephasOps), companyId, correlationId, eventId, eventType, payloadVersion. Payload is JSON; additive changes only for backward compatibility.
- **Versioning strategy:** New breaking change → new version (e.g. v2). Old versions supported for a documented period. Document in INTEGRATION_EVENT_CONTRACTS.md.

---

## 8. Correlation and causation model

- **CorrelationId:** Ties events and deliveries to a request or flow. Propagate through domain events and integration envelope.
- **CausationId / ParentEventId:** EventStore and domain events support causation/parent for lineage. Use for “this event was caused by that event.”
- **RootEventId:** Origin of the causality chain; stored in EventStoreEntry and OutboundIntegrationDelivery for traceability.
- **TraceId / SpanId:** Optional; from Activity when available. Stored in EventStore envelope.

**Decision:** Keep current fields. Observability model doc will define how to query “all records for this correlation id” across EventStore, OutboundDelivery, InboundReceipt.

---

## 9. Observability expectations

- **Event lifecycle:** For each domain event we can see: persisted (EventStore), processed/failed (Status, LastError, LastHandler), and if forwarded, the resulting OutboundIntegrationDelivery(ies) and their attempt history.
- **Delivery lifecycle:** For each OutboundIntegrationDelivery: status, attempt count, last error, last HTTP status, next retry.
- **Receipt lifecycle:** For each InboundWebhookReceipt: status, verification result, handler error, processed at.
- **Operational visibility:** Admin/support can list and filter by status, date, company, correlation id, event type. No requirement for a single “event platform dashboard” in this phase; APIs and queries are enough.

---

## 10. In scope now vs later

**In scope in this phase:**

- Document and formalize domain event model (DOMAIN_EVENT_MODEL.md).
- Document transactional outbox (EventStore) and its design (TRANSACTIONAL_OUTBOX_DESIGN.md).
- Document inbound inbox and receipt model (INBOUND_EVENT_INBOX_DESIGN.md).
- Document delivery pipeline and retries (EVENT_DELIVERY_PIPELINE.md).
- Document replay model (EVENT_REPLAY_MODEL.md).
- Document observability model (EVENT_OBSERVABILITY_MODEL.md).
- Document integration event contracts (INTEGRATION_EVENT_CONTRACTS.md).
- First-wave adoption: hook event platform to key flows (orders, workflow, invoicing, webhooks, inventory, payout, background jobs) and document (EVENT_PLATFORM_FIRST_WAVE_ADOPTION.md).
- Data model and migration impact (EVENT_PLATFORM_DATA_MODEL.md).
- Operational runbook (EVENT_PLATFORM_RUNBOOK.md).
- Optional: one domain-event handler that forwards selected events to IOutboundIntegrationBus (or document the manual pattern).
- Optional: inbound receipt replay (re-run handler for HandlerFailed).
- Validation: build, migrations, doc/code alignment.

**Deferred (document only):**

- Kafka/RabbitMQ or external message broker.
- Same-transaction IntegrationOutbox table (business write + integration outbox row in one transaction, worker creates OutboundIntegrationDelivery from it).
- Background worker for automatic outbound retry (NextRetryAtUtc); keep replay on-demand.
- Full “event platform dashboard” UI; APIs and queries suffice for now.
- Multi-tenant/multi-company event isolation beyond current CompanyId scoping.

---

## 11. Summary

| Concept | Decision |
|---------|----------|
| Domain event | IDomainEvent, EventStore, same-transaction when possible, handlers in-process/async. |
| Integration event | Stable contract, IOutboundIntegrationBus, one delivery per endpoint, connector abstraction. |
| Outbox | EventStore is the internal outbox; no second broker. Integration deliveries created on publish (no same-transaction integration outbox in this phase). |
| Inbox | InboundWebhookReceipt + idempotency; optional receipt replay. |
| Idempotency | EventProcessingLog (handlers), IdempotencyKey (outbound), ExternalIdempotencyKey (inbound), CommandProcessingLog. |
| Replay | EventStore replay (domain), Outbound ReplayAsync (failed/DeadLetter), optional inbound receipt replay. |
| Observability | Lifecycle visibility per event/delivery/receipt; correlation/causation; admin APIs. |
| Contracts | Versioned integration event naming and envelope; doc in INTEGRATION_EVENT_CONTRACTS.md. |

This architecture decision is the reference for all Event Platform design docs and implementation in this phase.
