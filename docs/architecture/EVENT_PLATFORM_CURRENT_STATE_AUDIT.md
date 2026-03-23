# Event Platform — Current State Architecture Audit

**Date:** Event Platform Layer architecture phase.  
**Purpose:** Document existing event-related and integration-related architecture so the Event Platform Layer can build on it without duplication or conflict.

**Scope:** Domain events, event store, outbox, inbound webhooks, outbound integration bus, background jobs, notifications, and related persistence and workers.

---

## 1. What already exists

### 1.1 Domain events and event store (internal outbox)

| Component | Location | Description |
|-----------|----------|-------------|
| **IDomainEvent** | Domain/Events | Base contract: EventId, EventType, OccurredAtUtc, CorrelationId, CompanyId, TriggeredByUserId, Source, ParentEventId. Extended by application events (OrderStatusChangedEvent, OrderAssignedEvent, WorkflowTransitionCompletedEvent, etc.). |
| **IEventStore** | Domain/Events | AppendAsync, AppendInCurrentTransaction (same-transaction outbox), ClaimNextPendingBatchAsync, MarkProcessedAsync, MarkAsProcessingAsync. Phase 7: lease (ProcessingNodeId, ProcessingLeaseExpiresAtUtc). Phase 8: envelope (RootEventId, PartitionKey, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority). |
| **EventStoreEntry** | Domain/Events | Persisted row: EventId, EventType, Payload (jsonb), Status (Pending/Processing/Processed/Failed/DeadLetter), RetryCount, NextRetryAtUtc, CorrelationId, CompanyId, EntityType/EntityId, CausationId, ParentEventId, Phase 7 lease fields, Phase 8 envelope fields. |
| **EventStoreRepository** | Infrastructure/Persistence | Implements IEventStore. AppendInCurrentTransaction adds to DbContext; caller commits. ClaimNextPendingBatchAsync uses FOR UPDATE SKIP LOCKED; retry backoff (1/5/15/60 min), dead-letter after max retries. |
| **EventStoreDispatcherHostedService** | Application/Events | Background worker: polls EventStore for Pending/due-retry Failed, claims batch, deserializes via EventTypeRegistry, calls IDomainEventDispatcher.PublishAsync(evt, alreadyStored: true). Partition-aware claim, backpressure, multi-node lease. |
| **IDomainEventDispatcher** | Application/Events | PublishAsync (persist then dispatch or dispatch-only when alreadyStored), DispatchToHandlersAsync. Resolves IDomainEventHandler&lt;T&gt;, IEventProcessingLogStore for idempotency, IAsyncEventSubscriber for async enqueue. |
| **IPlatformEventEnvelopeBuilder** | Application/Events | Builds EventStoreEnvelopeMetadata (PartitionKey, RootEventId, SourceService, SourceModule, etc.) from domain event. |
| **Event processing idempotency** | Application/Events | IEventProcessingLogStore (EventProcessingLog table): at-most-once per (EventId, Handler). EventStoreAttemptHistory for attempt audit. |

**Emission points:** WorkflowEngineService appends WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent in the same transaction as status update (AppendInCurrentTransaction). No other code path should append these to avoid duplicates.

**Handlers (examples):** OrderAssignedOperationsHandler (installer task, material pack, SLA enqueue), OrderStatusNotificationDispatchHandler (NotificationDispatch), WorkflowTransitionCompletedEventHandler, WorkflowTransitionLedgerHandler (ledger). Process managers (IProcessManager) react to events and send commands via ICommandBus.

---

### 1.2 External integration bus (outbound)

| Component | Location | Description |
|-----------|----------|-------------|
| **IOutboundIntegrationBus** | Application/Integration | PublishAsync(PlatformEventEnvelope), DispatchDeliveryAsync(deliveryId), ReplayAsync(ReplayOutboundRequest). |
| **OutboundIntegrationBus** | Application/Integration | Resolves endpoints via IConnectorRegistry, creates OutboundIntegrationDelivery per endpoint (idempotent by eventId+endpointId), IIntegrationPayloadMapper → JSON, IOutboundSigner, IOutboundHttpDispatcher. Records attempts, retry, DeadLetter. |
| **OutboundIntegrationDelivery** | Domain/Integration/Entities | Id, ConnectorEndpointId, CompanyId, SourceEventId, EventType, CorrelationId, RootEventId, IdempotencyKey, Status (Pending/Delivered/Failed/DeadLetter/Replaying), PayloadJson, AttemptCount, MaxAttempts, NextRetryAtUtc, DeliveredAtUtc, LastErrorMessage, LastHttpStatusCode, IsReplay, ReplayOperationId. |
| **OutboundIntegrationAttempt** | Domain/Integration/Entities | Per-attempt record (delivery id, timestamp, HTTP status, error). |
| **IConnectorRegistry** | Application/Integration | GetOutboundEndpointsForEventAsync(eventName, companyId), GetInboundEndpointAsync(connectorKey, companyId). ConnectorDefinition + ConnectorEndpoint. |
| **IOutboundDeliveryStore** | Application/Integration | CreateDeliveryAsync, GetByIdempotencyKeyAsync, update status/attempts. Persisted in OutboundIntegrationDeliveries / OutboundIntegrationAttempts. |

**Gap:** Outbound bus is **not** automatically wired to the domain event bus. Application code (or an event handler) must call `IOutboundIntegrationBus.PublishAsync(PlatformEventEnvelope)`. There is no registered IDomainEventHandler that forwards selected domain events to the integration bus. Phase 10 doc states: "Wiring to the internal event bus is optional."

**Replay:** ReplayAsync(ReplayOutboundRequest) re-dispatches Failed/DeadLetter deliveries by filter (endpoint, company, event type, status, date range). No background worker for retrying Pending outbound deliveries; replay is on-demand via API.

---

### 1.3 Inbound webhook runtime

| Component | Location | Description |
|-----------|----------|-------------|
| **IInboundWebhookRuntime** | Application/Integration | ProcessAsync(InboundWebhookRequest) → verify → persist receipt → idempotency claim → normalize → handler. |
| **InboundWebhookReceipt** | Domain/Integration/Entities | Id, ConnectorEndpointId, CompanyId, ExternalIdempotencyKey, ExternalEventId, ConnectorKey, MessageType, Status (Received/Verified/Processing/Processed/VerificationFailed/HandlerFailed/DeadLetter), PayloadJson, CorrelationId, VerificationPassed, ReceivedAtUtc, ProcessedAtUtc, HandlerErrorMessage, HandlerAttemptCount. |
| **IInboundWebhookReceiptStore** | Application/Integration | CreateAsync, UpdateAsync. Table InboundWebhookReceipts with unique (ConnectorKey, ExternalIdempotencyKey). |
| **IExternalIdempotencyStore** | Application/Integration | TryClaimAsync(key, connectorKey, companyId, receiptId), MarkCompletedAsync. ExternalIdempotencyRecord table. |
| **IInboundWebhookVerifier** | Application/Integration | CanVerify(connectorKey), VerifyAsync → (IsValid, FailureReason). NoOp if none registered. |
| **IInboundWebhookHandler** | Application/Integration | CanHandle(connectorKey, messageType), HandleAsync(IntegrationMessage, receiptId). |
| **WebhooksController** | Api/Controllers | POST /api/integration/webhooks/{connectorKey}; builds InboundWebhookRequest; calls IInboundWebhookRuntime.ProcessAsync. |

Receipt is created **before** idempotency claim so every call is logged; idempotency prevents duplicate handler execution. Handler failure → HandlerFailed; no automatic retry of handler (idempotency protects on sender retry).

---

### 1.4 Connector abstraction

| Component | Description |
|-----------|-------------|
| **ConnectorDefinition** | ConnectorKey, DisplayName, ConnectorType, Direction. Table ConnectorDefinitions. |
| **ConnectorEndpoint** | Per definition and optional CompanyId. EndpointUrl, AllowedEventTypes, SigningConfigJson, AuthConfigJson, RetryCount, TimeoutSeconds, IsPaused, IsActive. Table ConnectorEndpoints. |
| **IConnectorRegistry** | Resolves by event type (outbound) or connector key (inbound). |

---

### 1.5 Background jobs and job execution

| Component | Location | Description |
|-----------|----------|-------------|
| **JobExecution, JobDefinition, JobRun** | Domain/Workflow | Job definitions and runs. JobExecutionWorkerHostedService processes jobs; JobOrchestrationController for orchestrated jobs (e.g. PnlRebuild, OperationalReplay). |
| **Command processing** | Application/Commands | ICommandBus, CommandProcessingLog (idempotency by IdempotencyKey). Command handlers execute in-process. |

---

### 1.6 Notifications (SMS, WhatsApp, etc.)

| Component | Location | Description |
|-----------|----------|-------------|
| **NotificationDispatch** | Domain/Notifications | Outbound notification records. NotificationDispatchWorkerHostedService sends via INotificationDeliverySender. Event-driven: OrderStatusNotificationDispatchHandler creates dispatch from OrderStatusChangedEvent. |
| **IUnifiedMessagingService** | Application | Single facade for job update / SI on-the-way / TTKT (SMS/WhatsApp). |

---

### 1.7 Replay operations

| Component | Location | Description |
|-----------|----------|-------------|
| **ReplayOperation, ReplayOperationEvent** | Domain/Events | Replay run metadata and event list. Operational replay (re-run events from EventStore) with targets (e.g. Projection). |
| **EventStore replay** | Application/Events | Replay by event IDs or filters; IReplayExecutionContext.SuppressSideEffects; IProjectionEventHandler for projection-only replay. |
| **Outbound replay** | Application/Integration | ReplayAsync(ReplayOutboundRequest) for Failed/DeadLetter outbound deliveries. |

---

## 2. What is partially implemented

| Area | Partial state | Gap |
|------|----------------|-----|
| **Domain event → integration event** | PlatformEventEnvelope and IPlatformEventEnvelopeBuilder exist for EventStore envelope. Outbound bus accepts PlatformEventEnvelope. | No handler forwards domain events to IOutboundIntegrationBus. Manual call or one forwarding handler needed. |
| **Outbound retry worker** | Delivery has NextRetryAtUtc and status Failed; Replay API exists. | No background worker that periodically re-dispatches Pending/Failed with NextRetryAtUtc ≤ now. Replay is on-demand only. |
| **Inbound handler retry** | Receipt has HandlerFailed; idempotency prevents duplicate. | No automatic retry of HandlerFailed receipts; no replay-of-inbound story. |
| **Event observability** | EventStore has Status, RetryCount, LastError, LastHandler; Outbound has attempts; Inbound has receipt status. | No single “event lifecycle” view or admin surface that spans internal events, outbound deliveries, and inbound receipts. Correlation is present but not unified. |
| **Integration event contracts** | Payload is JSON; IIntegrationPayloadMapper per event type. | No formal versioned contract doc or envelope standard (e.g. event name, version, source, timestamp) documented as API. |

---

## 3. What is missing

- **Unified event platform narrative:** Single doc that ties domain events → event store (outbox) → dispatcher → handlers → optional integration bus. Clarification of “domain event” vs “integration event” and when each is persisted/delivered.
- **Transactional outbox as a named pattern for integration:** EventStore is the internal outbox. OutboundIntegrationDelivery is created in a separate call (not in the same transaction as business state unless the caller explicitly opens a transaction that includes both). So: internal domain events are outbox-style (same transaction); outbound integration deliveries are “create then dispatch” (durable but not same-transaction with business write unless caller does so).
- **First-class observability model:** Correlation/causation exist; no single observability doc or admin query surface that ties EventId → OutboundIntegrationDelivery → attempts and InboundWebhookReceipt.
- **Formal integration event contract:** Versioned event naming, envelope (timestamp, source, company, correlationId), backward-compatibility policy.
- **Inbound replay:** No “replay this receipt” (re-run handler for HandlerFailed) story.
- **Background worker for outbound retries:** Only on-demand replay; no scheduled retry of Failed deliveries.

---

## 4. What can be reused

- **EventStore + EventStoreDispatcherHostedService:** Keep as the internal event outbox and dispatcher. Do not replace with a second outbox.
- **IEventStore.AppendInCurrentTransaction:** Keep for workflow and any future in-transaction event emission.
- **IDomainEventDispatcher, IDomainEventHandler&lt;T&gt;, IEventProcessingLogStore:** Keep. Extend with new handlers (e.g. forward selected events to integration bus).
- **IOutboundIntegrationBus, OutboundIntegrationDelivery, IConnectorRegistry:** Keep. Add wiring from domain events and/or document the single “publish integration event” path.
- **InboundWebhookRuntime, InboundWebhookReceipt, IExternalIdempotencyStore:** Keep. Optionally add “replay receipt” (re-run handler for HandlerFailed) and document.
- **PlatformEventEnvelope, IPlatformEventEnvelopeBuilder:** Reuse for building integration envelopes from domain events when wiring.
- **Replay (EventStore and Outbound):** Keep. Document replay boundaries and safety; add inbound replay if needed.

---

## 5. What should remain as-is

- **WorkflowEngineService** event emission (AppendInCurrentTransaction for WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent). Single source of truth for these.
- **NotificationDispatch pipeline:** Separate from integration bus; event-driven via OrderStatusNotificationDispatchHandler.
- **Command bus and CommandProcessingLog:** Idempotent command handling; do not merge with event store.
- **Job execution (JobRun, JobDefinition):** Orchestration and workers; do not conflate with event dispatch.
- **Connector definitions and endpoints:** Already support multi-connector, per-company; keep.

---

## 6. Key architectural gaps

1. **No automatic domain → integration bridge:** Internal domain events are not automatically published to the external integration bus. Either document “call IOutboundIntegrationBus.PublishAsync from application code” as the pattern, or add a single forwarding handler (e.g. for WorkflowTransitionCompletedEvent / OrderStatusChangedEvent / OrderAssignedEvent) that builds PlatformEventEnvelope and calls PublishAsync.
2. **Outbound delivery not in same transaction as business write:** OutboundIntegrationBus.PublishAsync creates deliveries and then dispatches; it does not participate in the caller’s DbContext transaction. So “transactional outbox” for integration means: either (a) caller opens a transaction, writes business state, calls an integration-outbox append in the same transaction, and a worker later reads the outbox and creates OutboundIntegrationDelivery and dispatches, or (b) current model: create delivery after business commit, with at-least-once dispatch and idempotent endpoint. Current design is (b). For true same-transaction outbox for integration, a separate IntegrationOutbox table (event row in same transaction as business) + worker that creates deliveries from it would be needed; not in place today.
3. **Observability:** Correlation and causation exist in events and outbound; no unified “event platform” observability doc or API that spans internal events, outbound deliveries, and inbound receipts.
4. **Contract versioning:** No formal integration event contract versioning or compatibility policy.

---

## 7. Recommendations for layering

- **Layer 1 — Domain events:** Keep. Entities/application raise IDomainEvent; no transport in domain.
- **Layer 2 — Event store (internal outbox):** Keep. AppendInCurrentTransaction + EventStoreDispatcherHostedService. Single internal event bus.
- **Layer 3 — Domain event handlers:** Keep and extend. Add optional “integration forwarding” handler(s) that map selected domain events to PlatformEventEnvelope and call IOutboundIntegrationBus.PublishAsync (after handler idempotency so we don’t double-publish on replay).
- **Layer 4 — Integration bus (outbound):** Keep. Clear boundary: PlatformEventEnvelope in, delivery records and HTTP out. Connector abstraction stays.
- **Layer 5 — Inbound webhook:** Keep. Receipt + idempotency + handlers. Optionally add receipt replay and document.
- **Layer 6 — Observability:** Add a single observability model doc and, if useful, minimal admin/query surface (e.g. “event by correlation id” spanning EventStore + OutboundDelivery + InboundReceipt) without bloating UI.
- **Do not introduce:** Kafka/RabbitMQ or second broker in this phase. Do not add a second internal outbox table unless we explicitly design an IntegrationOutbox for same-transaction integration publish; current “create delivery then dispatch” is acceptable if documented.

---

## 8. References

- Phase 10: `docs/PHASE_10_EXTERNAL_INTEGRATION_BUS.md`, `docs/PHASE_10_DELIVERABLE_SUMMARY.md`
- Event-driven plan: `docs/operations/EVENT_DRIVEN_OPERATIONS_PLAN.md`
- Event store / envelope: `docs/DISTRIBUTED_PLATFORM_EVENT_ENVELOPE_SPEC.md`
- Integration map: `docs/architecture/integration_map.md`
- Domain event architecture: `docs/DOMAIN_EVENT_ARCHITECTURE.md`
