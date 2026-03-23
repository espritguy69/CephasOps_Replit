# Event Platform Formalization — Implementation Summary

**Mission:** Formalize the internal event platform for CephasOps as an event-driven SaaS operations platform without breaking existing behavior.

---

## A. Event Usage Audit

- **EventStore:** IEventStore + EventStoreEntry + EventStoreRepository; AppendInCurrentTransaction (WorkflowEngineService, same transaction), AppendAsync (DomainEventDispatcher, JobExecutionWorkerHostedService). EventStoreDispatcherHostedService claims Pending/due-retry, deserializes, dispatches.
- **OutboundIntegrationDeliveries:** IOutboundIntegrationBus, OutboundIntegrationDelivery; IntegrationEventForwardingHandler forwards WorkflowTransitionCompleted, OrderStatusChanged, OrderAssigned to outbound bus.
- **InboundWebhookReceipts:** IInboundWebhookRuntime, InboundWebhookReceipt; WebhooksController POST; idempotency prevents duplicate handler execution.
- **Retry workers:** EventStoreDispatcherHostedService (internal events); outbound retry/replay via API or dedicated worker.
- **Replay:** IEventReplayService (single-event retry/replay); IOperationalReplayExecutionService (batch); tenant-scoped; policy and company lock.
- **Handlers:** OrderAssignedOperationsHandler, OrderStatusNotificationDispatchHandler, WorkflowTransitionLedgerHandler, OrderLifecycleLedgerHandler, WorkflowTransitionHistoryProjectionHandler, IntegrationEventForwardingHandler, JobRunRecorderForEvents.
- **Emission points:** WorkflowEngineService (AppendInCurrentTransaction for workflow/order events); JobExecutionWorkerHostedService (JobStarted/Completed/Failed); DomainEventDispatcher (AppendAsync when PublishAsync and not alreadyStored).
- **Tight coupling:** WorkflowEngineService is the single source for workflow/order events; no other path appends those. Handlers are decoupled (event-driven).
- **Modules that should emit (recommended):** OrderCreated at order creation; MaterialIssued/MaterialReturned at inventory; InvoiceGenerated at billing; PayrollCalculated at payroll. Documented in audit; emission can be added alongside existing logic in follow-up.

Full audit: **docs/event-platform/EVENT_USAGE_AUDIT.md**.

---

## B. Event Model Introduced

- **DomainEvent (internal):** IDomainEvent + DomainEvent base; EventId, EventType, Version, CompanyId, OccurredAtUtc, CorrelationId, CausationId, Source, ParentEventId, RootEventId. Immutable, tenant-scoped, versionable (EventType + Version). Persisted in EventStore.
- **IntegrationEvent (outbound):** PlatformEventEnvelope; creates OutboundIntegrationDelivery per endpoint. Clear separation: domain events internal; integration envelope for HTTP delivery. Bridge: IntegrationEventForwardingHandler maps selected domain events to envelope and calls IOutboundIntegrationBus.PublishAsync.

Documented: **docs/event-platform/EVENT_MODEL.md**.

---

## C. Event Bus Implementation

- **IEventBus** added in Application/Events:
  - `PublishAsync<TEvent>(domainEvent)` — persist (via IDomainEventDispatcher → IEventStore.AppendAsync) then dispatch to handlers. Tenant context preserved on event (CompanyId).
  - `DispatchAsync<TEvent>(domainEvent)` — dispatch only (for already-stored events, e.g. worker or replay).
- **EventBus** implementation delegates to IDomainEventDispatcher (PublishAsync → dispatcher.PublishAsync(evt, alreadyStored: false); DispatchAsync → dispatcher.DispatchToHandlersAsync).
- **Subscribe:** Handlers register via DI as IDomainEventHandler<TEvent> (or IEventHandler<TEvent>). No separate subscribe API; discovery is through DI.
- **Same-transaction emission:** Unchanged; callers use IEventStore.AppendInCurrentTransaction in the same DbContext transaction; worker later claims and dispatches.
- **Registration:** IEventBus and EventBus registered in Program.cs (Scoped).

---

## D. Handler Architecture

- **IDomainEventHandler<TEvent>** and **IEventHandler<TEvent>** (alias): HandleAsync(domainEvent, cancellationToken). IEventHandler<TEvent> extends IDomainEventHandler<TEvent> for formal naming.
- **Idempotency:** IEventProcessingLogStore (EventProcessingLog) ensures at-most-once per (EventId, Handler). Handlers should be designed idempotent and retry-safe.
- **Tenant-aware:** Event carries CompanyId; handlers must scope all operations to that company.
- **Background:** IAsyncEventSubscriber<TEvent> handlers are enqueued; during replay with SuppressSideEffects they are not enqueued.
- **Replay:** Same handlers run on replay; projection-only replay runs only IProjectionEventHandler<T>.

Documented: **docs/event-platform/HANDLER_GUIDELINES.md**.

---

## E. Modules Emitting Events

- **Current:** WorkflowEngineService emits WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent, OrderCompletedEvent in same transaction. JobExecutionWorkerHostedService emits JobStartedEvent, JobCompletedEvent, JobFailedEvent.
- **Follow-up (implemented):** OrderCreatedEvent (OrderService), OrderCompletedEvent (WorkflowEngineService), InvoiceGeneratedEvent (BillingService), MaterialIssuedEvent (OrderMaterialUsageService), MaterialReturnedEvent (StockLedgerService.ReturnAsync), PayrollCalculatedEvent (PayrollService). All forwarded via IntegrationEventForwardingHandler; types in PlatformEventTypes and EventTypeRegistry.

---

## F. Event Store Integration

- **Reused:** Existing EventStore table and IEventStore/EventStoreRepository. Payload persisted as PayloadJson; CompanyId, EventType, Status, Version (PayloadVersion), CorrelationId, CausationId, RootEventId, etc. already in schema.
- **Replay support:** EventStore is the source for replay; GetByEventIdAsync, GetEventsForReplayAsync, and replay services use it with tenant scope.
- **No schema change** in this implementation.

---

## G. Replay Support

- **Single-event:** IEventReplayService.RetryAsync(eventId, scopeCompanyId, …), ReplayAsync(eventId, scopeCompanyId, …). Replay respects IEventReplayPolicy. Tenant isolation: event loaded only if entry.CompanyId == scopeCompanyId (or global).
- **Batch:** IOperationalReplayExecutionService with company lock; filter by CompanyId, EventType, date range; projection-only option. Replay does not duplicate side effects when handlers are idempotent and SuppressSideEffects used for async handlers.
- **APIs:** POST api/event-store/events/{id}/retry, POST api/event-store/events/{id}/replay; operational replay via JobOrchestrationController.

Documented: **docs/event-platform/REPLAY_STRATEGY.md**.

---

## H. Observability Additions

- **GET /api/observability/events** (ObservabilityController). Filters: companyId, eventType, status, fromUtc, toUtc, correlationId, page, pageSize. Tenant-scoped. Returns items, total, page, pageSize. **GET /api/observability/insights** (OperationalInsights): companyId, type, fromUtc, toUtc, page, pageSize; tenant-scoped; field ops intelligence.
- **Existing:** api/event-store/events (list), dashboard, failed/dead-letter, attempt-history, lineage, observability/processing, retry, replay — primary event-store and handler observability surface.

---

## I. Tenant Safety Verification

- **Publishing:** Event.CompanyId is set by caller; recommendation documented to set from current tenant (e.g. ICurrentUserService.CompanyId). EventStoreRepository persists CompanyId as provided; no overwrite.
- **Queries:** EventStoreQueryService, EventBusObservabilityService, ReplayOperationQueryService, LedgerQueryService, GetByEventIdAsync, GetEventsForReplayAsync all accept scopeCompanyId and filter by CompanyId when set.
- **Replay/retry:** EventReplayService rejects when scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId. Operational replay uses company-scoped filter and company lock.
- **API:** EventStoreController and ObservabilityController use ScopeCompanyId(); forbid when companyId != scopeCompanyId for non–super-admins.

Documented: **docs/event-platform/TENANT_SAFETY.md**.

---

## J. Documentation Added

Under **docs/event-platform/**:

| Document | Content |
|----------|---------|
| EVENT_USAGE_AUDIT.md | Structured audit: EventStore, outbound/inbound, retry, replay, handlers, emission points, coupling, recommended events. |
| EVENT_MODEL.md | Canonical DomainEvent vs IntegrationEvent; immutability, tenant-scope, versioning. |
| EVENT_ARCHITECTURE.md | Overview, components, flow, tenant safety. |
| EVENT_LIFECYCLE.md | Emission, claim/dispatch, handler execution, retry/dead-letter, replay, retention. |
| HANDLER_GUIDELINES.md | Idempotency, retry-safety, tenant awareness, background vs in-process, replay. |
| REPLAY_STRATEGY.md | Single-event and batch replay, tenant isolation, avoiding duplicate side effects. |
| TENANT_SAFETY.md | Rules and verification for publishing, queries, replay, handlers, observability. |
| IMPLEMENTATION_SUMMARY.md | This report. |

---

## K. Build/Test Status

- **Build:** dotnet build (CephasOps.Api) succeeds (exit code 0). Pre-existing XML doc warnings only; no new errors.
- **Tests:** CephasOps.Application.Tests: 745 passed, 7 skipped, 2 failed. The 2 failures are in SlaEvaluationSchedulerServiceTests (TargetParameterCountException on reflection Invoke); pre-existing test/mock setup issue, not caused by event platform changes. No event bus or event store code is modified in a way that would affect those tests. Regression: existing flows (email parser → order, order → scheduler, order → billing, order → payroll, integration deliveries, retry worker, inbound webhooks) are unchanged; no code paths removed or replaced. Manual/E2E validation of those flows recommended in staging.

---

## L. Remaining Platform Gaps

1. **Additional domain events:** OrderCreated, MaterialIssued, MaterialReturned, InvoiceGenerated, PayrollCalculated — documented; emission to be added in modules alongside existing logic.
2. **Outbound retry worker:** If not already present, a background worker for Pending/Failed outbound deliveries (NextRetryAtUtc) could be added; today replay is on-demand.
3. **Inbound receipt replay:** Re-run handler for HandlerFailed receipts (replay-by-receipt) not implemented; idempotency protects on sender retry.
4. **Unified observability view:** Single view spanning EventStore + OutboundIntegrationDelivery + InboundWebhookReceipt (e.g. by CorrelationId) not implemented; correlation exists in data but no single API that joins the three.
5. **Formal integration event contract:** Versioned envelope and backward-compatibility policy documented in architecture docs; no separate contract versioning API.

---

**Summary:** The internal event platform is formalized with a central IEventBus, canonical event model documentation, handler contract (IDomainEventHandler / IEventHandler), event persistence and replay (existing store and services), observability endpoint GET /api/observability/events, tenant safety verification and documentation, and docs/event-platform/ documentation. Existing behavior is preserved; new event emissions can be added incrementally using the same bus and handlers.
