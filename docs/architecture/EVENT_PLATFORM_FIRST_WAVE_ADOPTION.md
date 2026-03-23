# Event Platform — First-Wave Adoption

**Date:** Event Platform Layer phase.  
**Purpose:** Document which business flows are event-enabled in the first wave and what was deferred.  
**Depends on:** EVENT_PLATFORM_ARCHITECTURE_DECISION.md, EVENT_PLATFORM_CURRENT_STATE_AUDIT.md.

---

## 1. Selected flows (first wave)

### 1.1 Orders and workflow

| Flow | Event(s) | Trigger point | Why selected |
|------|----------|----------------|--------------|
| Workflow transition completed | WorkflowTransitionCompletedEvent | WorkflowEngineService after transition, same transaction | Parent event for all workflow-driven changes; already emitted. |
| Order status changed | OrderStatusChangedEvent | WorkflowEngineService when entity is Order, same transaction | Drives notifications and reporting; already emitted. |
| Order assigned | OrderAssignedEvent | WorkflowEngineService when target status = Assigned, same transaction | Drives installer task, material pack, SLA; already emitted. |

**Implementation:** Events are appended in WorkflowEngineService via IEventStore.AppendInCurrentTransaction. Handlers: OrderAssignedOperationsHandler, OrderStatusNotificationDispatchHandler, WorkflowTransitionCompletedEventHandler, WorkflowTransitionLedgerHandler. **First-wave addition:** Optional integration forwarding handler that publishes these events to IOutboundIntegrationBus so connectors receive them (see §3).

### 1.2 Webhook / integration receipts

| Flow | Component | Trigger point | Why selected |
|------|------------|----------------|--------------|
| Inbound webhook | InboundWebhookReceipt, IInboundWebhookRuntime | POST /api/integration/webhooks/{connectorKey} | Already implemented; idempotency and receipt lifecycle are first-class. |

No change; documented as part of the event platform.

### 1.3 Background job lifecycle

| Flow | Event(s) | Trigger point | Why selected |
|------|----------|----------------|--------------|
| Job started / completed / failed | JobStartedEvent, JobCompletedEvent, JobFailedEvent | Job execution path (BackgroundJobProcessorService or equivalent) | Already defined; emission can be added or confirmed where jobs run. |

**Status:** Event types exist (JobLifecycleEvents); emission points may be in place. Document as first-wave; ensure events are appended when jobs start/complete/fail so observability and optional integration forwarding are possible.

### 1.4 Notifications (SMS, WhatsApp)

| Flow | Component | Trigger point | Why selected |
|------|------------|----------------|--------------|
| Order status → notification | OrderStatusNotificationDispatchHandler | OrderStatusChangedEvent | Already event-driven; creates NotificationDispatch. |

No change; remains event-driven.

### 1.5 Invoicing, inventory, payout anomaly

| Flow | Status | Reason |
|------|--------|--------|
| Invoice created / submitted | **Deferred** | Not yet event-emitting; add when invoice flows are ready. |
| Inventory allocated / stock movement | **Deferred** | Add when inventory module raises domain events. |
| Payout anomaly raised | **Deferred** | Currently scheduler-based; event can be added when anomaly detection emits. |

Document as second-wave candidates.

---

## 2. Event names (integration)

For outbound integration, event names follow PlatformEventTypes (ops.*):

- ops.workflow.transition_completed.v1
- ops.order.status_changed.v1
- ops.order.assigned.v1
- ops.job.started.v1, ops.job.completed.v1, ops.job.failed.v1

Connector endpoints filter by these names (AllowedEventTypes). See INTEGRATION_EVENT_CONTRACTS.md.

---

## 3. Domain event → integration bus wiring

**Gap (audit):** Outbound bus was not automatically wired to domain events; callers had to call IOutboundIntegrationBus.PublishAsync manually.

**First-wave addition:** An optional handler that forwards selected domain events to the integration bus:

- **Events forwarded:** WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent (and optionally JobStartedEvent, JobCompletedEvent, JobFailedEvent when emitted).
- **Mechanism:** IDomainEventHandler&lt;T&gt; that builds PlatformEventEnvelope from the domain event (via IDomainEventToPlatformEnvelopeBuilder) and calls IOutboundIntegrationBus.PublishAsync(envelope). Handler is idempotent: outbound delivery is keyed by (EventId, EndpointId), so duplicate handler run (e.g. on replay) does not create duplicate deliveries.
- **Registration:** Handler registered in DI; no change to WorkflowEngineService or other emitters. If no connector endpoints are configured for an event type, PublishAsync is a no-op (endpoints.Count == 0).

This gives “event-driven integration” for the first-wave flows without changing existing API or workflow behavior.

---

## 4. What was deferred

- **Invoicing / inventory / payout anomaly events:** Emit domain events when those modules are ready; then add handlers and optional integration forwarding.
- **Automatic outbound retry worker:** Replay remains on-demand; no background worker that re-dispatches Failed deliveries on NextRetryAtUtc.
- **Inbound receipt replay API:** Re-run handler for HandlerFailed receipts; document in runbook if implemented later.
- **Full observability dashboard:** APIs and queries suffice for first wave.

---

## 5. Summary

| Area | First wave | Deferred |
|------|------------|----------|
| Orders / workflow | Events already emitted; add integration forwarding handler | — |
| Webhooks | Inbound receipt + idempotency already in place | Receipt replay API |
| Background jobs | Event types defined; confirm emission | — |
| Notifications | Already event-driven | — |
| Invoicing / inventory / payout | — | Domain events + handlers when ready |
| Outbound retry | Replay API only | Background retry worker |

First-wave adoption is conservative: we hook the existing event store and handlers into the integration bus for key flows and document the rest for later.
