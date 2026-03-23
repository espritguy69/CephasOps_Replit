# Event Platform — Final Decision

**Date:** Event Platform Layer phase.  
**Purpose:** Final summary of what was implemented, what was chosen, what was deferred, and how this positions CephasOps for the future.  
**Depends on:** All prior Event Platform docs (audit, architecture decision, design docs, first wave, data model, runbook).

---

## 1. What was implemented

### 1.1 Documentation (no schema change)

- **EVENT_PLATFORM_CURRENT_STATE_AUDIT.md** — Audit of existing event store, outbound integration bus, inbound webhook runtime, background jobs, notifications; gaps and reuse recommendations.
- **EVENT_PLATFORM_ARCHITECTURE_DECISION.md** — Domain vs integration events, outbox vs jobs, inbox, idempotency, replay, versioning, correlation, observability, in-scope vs deferred.
- **DOMAIN_EVENT_MODEL.md** — Where and how domain events are raised, persisted, and dispatched; handler model.
- **TRANSACTIONAL_OUTBOX_DESIGN.md** — EventStore as internal outbox; write path, dispatch path, retry and dead-letter.
- **INBOUND_EVENT_INBOX_DESIGN.md** — Inbound receipt lifecycle, idempotency, handler result recording.
- **EVENT_DELIVERY_PIPELINE.md** — Outbound delivery pipeline, retries, dead-letter, connector abstraction.
- **EVENT_REPLAY_MODEL.md** — Replay types (domain, outbound, optional inbound), safety, audit.
- **EVENT_OBSERVABILITY_MODEL.md** — Event/delivery/receipt lifecycle, correlation, operational visibility.
- **INTEGRATION_EVENT_CONTRACTS.md** — Naming, envelope, versioning, backward compatibility.
- **EVENT_PLATFORM_FIRST_WAVE_ADOPTION.md** — Selected flows (orders/workflow, webhooks, jobs, notifications); deferred (invoicing, inventory, payout anomaly).
- **EVENT_PLATFORM_DATA_MODEL.md** — Existing tables used by the event platform; no new migrations.
- **EVENT_PLATFORM_RUNBOOK.md** (backend/scripts) — How events flow, how to inspect failed deliveries, how to replay, how to troubleshoot idempotency, safe vs unsafe.

### 1.2 Code (first-wave wiring)

- **IDomainEventToPlatformEnvelopeBuilder** and **DomainEventToPlatformEnvelopeBuilder** — Build PlatformEventEnvelope from IDomainEvent (EventName = EventType, Payload = JSON, envelope fields).
- **IntegrationEventForwardingHandler** — IDomainEventHandler for WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent; builds envelope and calls IOutboundIntegrationBus.PublishAsync. Registered in DI for all three event types.
- **Program.cs** — Registration of builder and forwarding handler. No other API or workflow behavior changed.

---

## 2. Architecture chosen

- **Internal outbox:** EventStore + EventStoreDispatcherHostedService. Domain events persisted in same transaction when possible; worker claims and dispatches to handlers. No second broker.
- **Integration outbound:** IOutboundIntegrationBus + OutboundIntegrationDelivery. Create delivery then dispatch; idempotent per (EventId, EndpointId). Replay on-demand; no background retry worker in this phase.
- **Inbound:** InboundWebhookReceipt + ExternalIdempotencyRecord. Verify → persist → idempotency → handler. Optional receipt replay documented; not implemented in this phase.
- **Domain → integration bridge:** Optional forwarding handler sends selected domain events to the integration bus so connectors receive workflow/order events without application code calling PublishAsync directly. If no endpoints are configured, PublishAsync is a no-op.

---

## 3. What remained deferred

- **Same-transaction integration outbox:** No IntegrationOutbox table written in the same transaction as business state; current “create delivery then dispatch” model retained.
- **Background worker for outbound retries:** No worker that re-dispatches Failed deliveries on NextRetryAtUtc; replay is via API only.
- **Inbound receipt replay API:** Re-run handler for HandlerFailed; documented as optional.
- **Kafka/RabbitMQ or external broker:** Not introduced; database-backed only.
- **Full event platform dashboard:** APIs and queries suffice; no single UI dashboard.
- **Invoicing, inventory, payout anomaly events:** Emit and wire when those modules are ready.

---

## 4. Why this approach fits CephasOps today

- Builds on **existing** EventStore, OutboundIntegrationBus, and InboundWebhookRuntime without rewriting.
- **No new tables**; no migration risk. Clear documentation and one small, optional forwarding path.
- **Stripe-style** reliability: durable event store, at-least-once dispatch, idempotency and dead-letter.
- **Shopify-style** integration events: stable event names (ops.*), envelope, connector abstraction.
- **Uber-style** discipline: single emission point per event type, handler idempotency, replay with scope. Fits single-company ops today and leaves room for multi-company/tenant later.

---

## 5. Positioning for future growth

- **Multi-company/tenant:** CompanyId is already on events and deliveries; connector endpoints can be company-scoped. No change needed for first multi-tenant steps.
- **More event types:** Add domain events and handlers (and optionally register forwarding for new types). Contract versioning (INTEGRATION_EVENT_CONTRACTS) supports new versions.
- **External infra later:** If we add Kafka or RabbitMQ, a relay can read from EventStore (or a dedicated outbox) and publish; internal model stays the same.
- **Next phase:** Add outbound retry worker; add same-transaction integration outbox if required; add inbound receipt replay API; extend first wave to invoicing/inventory/payout; add observability dashboard if needed.

---

## 6. Validation note

- **Build:** One pre-existing build error exists in DocumentGenerationJobEnqueuer.cs (argument type mismatch); it is unrelated to the Event Platform changes. New code (IntegrationEventForwardingHandler, DomainEventToPlatformEnvelopeBuilder, Program.cs registrations) compiles; linter reports no issues on new files.
- **Migrations:** No new migrations; governance unchanged.
- **Docs and code:** Aligned; runbook and architecture docs reference existing APIs and behavior.

---

## 7. Recommended next phase

1. Fix the pre-existing DocumentGenerationJobEnqueuer build error.
2. Add an optional **background worker** that re-dispatches OutboundIntegrationDelivery rows in Failed status with NextRetryAtUtc ≤ now (with rate limit and MaxAttempts).
3. Implement **inbound receipt replay** (re-run handler for HandlerFailed) and document in runbook.
4. Emit **domain events** for invoicing and inventory when those flows are ready; add handlers and optional integration forwarding.
5. Consider **archival/retention** jobs for EventStore and OutboundIntegrationDeliveries (Processed/Delivered by date) and document in runbook.

This final decision closes the Event Platform Layer autonomous architecture phase with a clear, documented, and minimal-implementation outcome.
