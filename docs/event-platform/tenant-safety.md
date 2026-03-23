# Tenant Safety — Event Platform

**Purpose:** Ensure every event operation enforces tenant isolation: **CompanyId == TenantScope.CurrentTenantId** (or equivalent) and no cross-tenant event leakage.

---

## 1. Rules

1. **Emission:** When creating a domain event, set **CompanyId** from the current tenant (e.g. from request context or business entity). Never publish an event with a CompanyId from another tenant.
2. **Persistence:** EventStore persists **CompanyId** from the event; no overwrite by store. EventStoreRepository does not substitute or clear CompanyId.
3. **Query:** When listing or filtering events, non–global-admins must only see events for their tenant. **EventStoreQueryService** and **ObservabilityController** accept `scopeCompanyId`; when set, filter is applied: `e.CompanyId == scopeCompanyId`.
4. **Replay:** **EventReplayService** and **EventBulkReplayService** enforce scope: replay by event id requires `entry.CompanyId == scopeCompanyId`; bulk filters use **EventStoreBulkFilter.CompanyId**.
5. **API:** Controllers (EventStoreController, ObservabilityController) set `scopeCompanyId` from **ICurrentUserService** (e.g. SuperAdmin => null for global; else CompanyId). If the client passes a different CompanyId and the user is not allowed, return 403.
6. **Handlers:** Handlers receive the event with its **CompanyId**; they must only read/write data for that tenant and must not use request context to switch tenant for that event.

---

## 2. Verification Points

| Component | Verification |
|-----------|--------------|
| EventStoreQueryService.GetEventsAsync | When scopeCompanyId.HasValue, query has `.Where(e => e.CompanyId == scopeCompanyId.Value)`. Same for GetEventsForReplayAsync, GetByEventIdAsync (detail), GetDashboardAsync, GetEventStoreCountsAsync, GetAttemptHistoryByEventIdAsync. |
| EventReplayService.DispatchStoredEventAsync | After loading entry, `if (scopeCompanyId.HasValue && entry.CompanyId != scopeCompanyId.Value) return error`. |
| ObservabilityController.ListEvents | scopeCompanyId from current user; if scopeCompanyId.HasValue && companyId.HasValue && companyId != scopeCompanyId => 403. GetEventsAsync(filter, scopeCompanyId, ...). |
| EventStoreController | ScopeCompanyId() used for list, replay, dashboard, etc.; filter by company when not SuperAdmin. |
| EventStoreRepository.AppendInCurrentTransaction | CompanyId taken from domainEvent.CompanyId; no substitution. |
| IntegrationEventForwardingHandler | Forwards event to outbound bus; envelope carries same CompanyId. |

---

## 3. New Code Checklist

- When adding a new event: set **CompanyId** at emission from the authoritative tenant (order.CompanyId, user’s company, etc.).
- When adding a new query or API that returns events: accept a tenant scope and apply **CompanyId** filter for non-global admins.
- When adding a new replay or bulk operation: require **EventStoreBulkFilter.CompanyId** or equivalent and reject cross-tenant access.

---

## 4. References

- Event architecture: `docs/event-platform/event-architecture.md`
- Replay: `docs/event-platform/replay-strategy.md`
- Audit: `docs/event-platform/event-usage-audit.md`
