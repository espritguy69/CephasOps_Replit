# Event Platform — Tenant Safety Rules

## Rule 1: Event publishing

- When publishing a domain event from a tenant-scoped operation, set `CompanyId` on the event to the current tenant (e.g. from request/user context). Do not set another tenant’s CompanyId.
- Recommendation: Where a “current tenant” is available (e.g. `ICurrentUserService.CompanyId`), assert or set `evt.CompanyId = currentTenantId` before calling `IEventBus.PublishAsync` or `IEventStore.AppendInCurrentTransaction`.

## Rule 2: Event store queries

- All event store query APIs accept `scopeCompanyId`. When the caller is not a global admin, pass the user’s CompanyId so results are filtered to that tenant. Implementations (e.g. `EventStoreQueryService`, `EventBusObservabilityService`) filter with `Where(e => e.CompanyId == scopeCompanyId)` when `scopeCompanyId` is set.

## Rule 3: Replay and retry

- `IEventReplayService.RetryAsync` and `ReplayAsync` take `scopeCompanyId`. The event is loaded only if it belongs to that company (or scope is global). Re-dispatch uses the event’s existing CompanyId; no override to another tenant.
- Operational replay (`IOperationalReplayExecutionService`) uses request and lock scope by company; events replayed are those matching the filter and belonging to that company.

## Rule 4: Handlers

- Handlers receive the event with its `CompanyId`. They must only read/write data for that company. Do not switch tenant context inside the handler; use `domainEvent.CompanyId` for any tenant-scoped query or write.

## Rule 5: Observability and API

- Event list, dashboard, attempt history, lineage, and processing log APIs all apply `scopeCompanyId` when the user is not a global admin. Controllers derive scope from current user and forbid cross-tenant access (e.g. rejecting a requested companyId different from the user’s).

## Verification

- EventStoreRepository does not overwrite CompanyId; it persists the value from the event.
- EventReplayService: `if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value)` → treat as not found.
- EventStoreQueryService, EventBusObservabilityService, ReplayOperationQueryService, LedgerQueryService: filter by `scopeCompanyId` when provided.
- EventStoreController: `ScopeCompanyId()`; rejects when `companyId != scopeCompanyId` for non–super-admins.
