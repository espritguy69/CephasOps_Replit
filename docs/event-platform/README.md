# CephasOps Internal Event Platform

This folder documents the **formal internal event platform** for CephasOps: architecture, lifecycle, handlers, replay, and tenant safety.

## Contents

| Document | Purpose |
|----------|---------|
| [event-usage-audit.md](event-usage-audit.md) | Audit of current event usage (EventStore, integrations, replay, handlers). |
| [event-architecture.md](event-architecture.md) | Canonical event model (DomainEvent vs IntegrationEvent), event bus, store, observability. |
| [event-lifecycle.md](event-lifecycle.md) | How events move from emission to persistence, dispatch, integration, and replay. |
| [handler-guidelines.md](handler-guidelines.md) | Idempotent, retry-safe, tenant-aware handler implementation. |
| [replay-strategy.md](replay-strategy.md) | Replay by id, tenant isolation, avoiding duplicate side effects. |
| [tenant-safety.md](tenant-safety.md) | Tenant isolation rules and verification points. |

## Operational Runbook

See **backend/scripts/EVENT_PLATFORM_RUNBOOK.md** for day-to-day operations (retry, replay, retention, troubleshooting).

## Summary

- **DomainEvent** (IDomainEvent / DomainEvent): internal events persisted to EventStore and dispatched to IDomainEventHandler&lt;T&gt;.
- **IntegrationEvent** (PlatformEventEnvelope): outbound envelope for IOutboundIntegrationBus; built from domain events when forwarding.
- **IEventBus**: PublishAsync (persist + dispatch), DispatchAsync (dispatch only). Subscribe via DI (IDomainEventHandler&lt;T&gt;).
- **Tenant safety**: CompanyId on every event; queries and replay scoped by tenant.
- **Observability**: GET /api/observability/events and GET /api/observability/insights (feature-gated); EventStore APIs for listing, filtering, and traceability. Follow-up events (OrderCreated, OrderCompleted, InvoiceGenerated, MaterialIssued, MaterialReturned, PayrollCalculated) and emission points: see [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md).
