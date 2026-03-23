# Event Platform Architecture

## Overview

CephasOps uses a **single internal event platform** for domain events, persistence, dispatch, replay, and optional integration forwarding.

## Components

1. **Domain events** — `IDomainEvent` / `DomainEvent`; immutable, tenant-scoped, versioned.
2. **Event store** — `IEventStore` + `EventStoreEntry`; transactional outbox (AppendInCurrentTransaction) and async append (AppendAsync).
3. **Event bus** — `IEventBus` (facade over `IDomainEventDispatcher`): `PublishAsync` (persist then dispatch), `DispatchAsync` (dispatch only). Subscribe by registering `IDomainEventHandler<T>` (or `IEventHandler<T>`) in DI.
4. **Dispatcher worker** — `EventStoreDispatcherHostedService`: claims Pending/due-retry events, deserializes, calls dispatcher with `alreadyStored: true`.
5. **Handlers** — In-process and optional async; idempotent via `IEventProcessingLogStore`; tenant-aware via event `CompanyId`.
6. **Integration** — Selected events forwarded to `IOutboundIntegrationBus` via `IntegrationEventForwardingHandler`; outbound deliveries and retry/replay are separate from EventStore.

## Flow

- **Publish:** Caller uses `IEventBus.PublishAsync(evt)` or, for same-transaction emission, `IEventStore.AppendInCurrentTransaction(evt, envelope)` then commits; worker later claims and dispatches.
- **Dispatch:** Dispatcher resolves all `IDomainEventHandler<TEvent>`, runs idempotency claim, executes in-process handlers, optionally enqueues async handlers; marks event Processed/Failed/DeadLetter.
- **Replay:** Single-event replay via `IEventReplayService.ReplayAsync(eventId)`; batch via `IOperationalReplayExecutionService`; tenant-scoped and policy-aware.

## Tenant Safety

- Every event carries `CompanyId`. Query and replay APIs accept `scopeCompanyId` and filter so that non–global-admins only see their tenant’s events.
- When publishing, set `CompanyId` from current tenant context; do not publish cross-tenant.

See **tenant-safety.md** for rules and verification.
