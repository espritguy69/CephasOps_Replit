# CephasOps SaaS Architecture Map

**Date:** 2026-03-13  
**Purpose:** High-level view of tenant resolution, tenant safety, frontend boundaries, observability, and financial isolation.

---

## Overview

CephasOps operates as a multi-tenant SaaS platform where all tenant-scoped activity must resolve to the correct company context before reads, writes, financial calculations, background jobs, or UI rendering occur.

The architecture uses **defense-in-depth**:

- request-level tenant resolution
- service-level tenant enforcement
- database-level tenant filtering
- write-time tenant validation
- frontend tenant-boundary protection
- platform-only observability
- tenant-scoped financial isolation
- **enterprise:** tenant rate limiting, feature flags, health scoring, activity timeline
- **async event bus:** domain events, outbox-style store, tenant-safe dispatch, timeline/notification/integration handlers

*Diagram is not supported.*

**Enterprise additions (2026-03-13):** Tenant rate limiting (429 + metrics), tenant feature flags (IFeatureFlagService), automated tenant health scoring (TenantHealthScoringService → TenantMetricsDaily.HealthScore/HealthStatus), and tenant activity audit timeline (TenantActivityEvents, GET /api/platform/tenants/{id}/activity-timeline). See [SAAS_ENTERPRISE_UPGRADES.md](SAAS_ENTERPRISE_UPGRADES.md).

**Async event bus (2026-03-13):** Production-safe domain event backbone: IEventBus, EventStore (outbox), EventStoreDispatcherHostedService (tenant-scoped dispatch), TenantActivityTimelineFromEventsHandler, notification and integration handlers. See [ASYNC_EVENT_BUS_ARCHITECTURE.md](ASYNC_EVENT_BUS_ARCHITECTURE.md), [EVENT_HANDLING_GUARDRAILS.md](EVENT_HANDLING_GUARDRAILS.md).

---

## 1. Frontend Tenant Boundary

The frontend is responsible for ensuring that tenant context changes do not leave stale data visible.

**Safeguards**

- **DepartmentContext** controls the active tenant/company context
- **React Query cache** is invalidated on department/company switch
- **Parser upload/export** uses the same active department key as the rest of the UI
- **Platform pages** are route-guarded and permission-guarded
- Tenant users cannot access platform observability pages

**Result**

The frontend cannot continue showing another tenant's cached data after a tenant switch.

---

## 2. Tenant Resolution Flow

All tenant-scoped backend operations depend on a valid tenant context.

**Core components**

- **TenantGuardMiddleware**
- **ITenantProvider**
- **TenantScope**
- **RequireCompanyId**

**Flow**

1. Request enters the API
2. Middleware resolves tenant/company context
3. Tenant context is stored in **TenantScope**
4. Controllers/services enforce tenant presence where required
5. Services resolve **effectiveCompanyId**

**Typical pattern:**

```
effectiveCompanyId = companyId ?? TenantScope.CurrentTenantId
```

**If tenant context is missing:**

- reads fail closed
- writes throw

---

## 3. Service-Level Tenant Enforcement

Application services are responsible for ensuring that tenant-scoped logic never broadens into all-tenant access.

**Rules**

- reads without valid context return empty/null/fail-closed responses
- writes require tenant context
- cross-tenant entity access is blocked
- rebuild/recalculation operations require valid tenant scope

This pattern was applied to:

- templates and messaging
- PnL services
- skill services
- payout snapshot services
- billing/ratecard flows

---

## 4. Database-Level Protection

Tenant enforcement is not trusted to only one layer.

**Database protections**

- **EF Core global query filters** restrict tenant-scoped reads
- **TenantSafetyGuard** prevents unsafe tenant-scoped writes
- **SaveChanges** validation blocks mismatched or missing tenant ownership

**Result**

Even if a service path is written incorrectly, the write layer still protects tenant data.

---

## 5. Background Jobs and Platform Bypass

Not all work comes from HTTP requests.

**Tenant jobs**

Tenant-scoped jobs must run under **TenantScopeExecutor** so they inherit the correct tenant context.

**Platform operations**

Some actions are explicitly platform-wide and may run using **platform bypass**, such as:

- platform analytics
- seeding
- provisioning
- retention or maintenance tasks

Platform bypass must remain:

- explicit
- narrow
- authorized
- read-only where possible

---

## 6. Financial Isolation

CephasOps financial logic is tenant-scoped and fail-closed.

**Protected services**

- BillingRatecardService
- PnlService
- OrderPayoutSnapshotService

**Guarantees**

- no financial calculation runs without tenant context
- no `Guid.Empty` or null tenant context broadens scope
- cross-tenant payout snapshots are not returned
- payout calculations are reproducible through snapshots

**Snapshot protection**

Snapshots preserve immutable calculation inputs such as:

- ratecard version
- payout rules
- calculation timestamp

---

## 7. Platform Observability

CephasOps includes a platform-only observability layer for operators.

**Sources used**

- TenantMetricsDaily
- JobExecutions
- NotificationDispatches
- OutboundIntegrationDeliveries
- TenantAnomalyEvents

**Access**

Only platform administrators with the correct permission can view:

- platform summary
- tenant operations overview
- tenant operations detail

**Safety**

Observability uses platform bypass only for read-only cross-tenant aggregation. It does not expose tenant business records to tenant users.

**Automated Operational Intelligence**

A rule-based intelligence layer (see [AUTOMATED_OPERATIONAL_INTELLIGENCE.md](AUTOMATED_OPERATIONAL_INTELLIGENCE.md)) adds tenant-scoped risk signals (orders, installers, buildings, tenant-level) with explainable reasons. Tenant endpoints use RequireCompanyId and company-scoped queries; platform summary endpoint is admin-only and returns aggregate counts only. Read-only; no change to financial or tenant isolation.

**SLA Breach Engine**

Order-level SLA breach detection (see [SLA_BREACH_ENGINE.md](SLA_BREACH_ENGINE.md)) using Order.KpiDueAt: states NoSla, OnTrack, NearingBreach, Breached with explainable reasons. Tenant endpoints RequireCompanyId; platform-sla-summary is admin-only. Read-only.

---

## 8. Async Event Bus (Domain Events)

CephasOps uses an internal event bus for scalable, decoupled handling of order, notification, integration, and audit flows.

**Components**

- **IEventBus** — Publish domain events (persist then dispatch).
- **IEventStore** — Outbox-style persistence; ClaimNextPendingBatchAsync for background dispatch.
- **EventStoreDispatcherHostedService** — Claims events, runs each under `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`.
- **Handlers** — Sync (in-process) and async (enqueued); tenant scope from event’s CompanyId. EventHandlingAsyncJobExecutor enforces job.CompanyId == entry.CompanyId.

**Tenant safety**

- Every tenant-scoped event carries **CompanyId**. Dispatcher sets tenant scope from entry.CompanyId before running handlers.
- No cross-tenant handler execution; timeline and notification handlers record only for the event’s tenant.
- See [EVENT_HANDLING_GUARDRAILS.md](EVENT_HANDLING_GUARDRAILS.md) and [ASYNC_EVENT_BUS_ARCHITECTURE.md](ASYNC_EVENT_BUS_ARCHITECTURE.md).

---

## 9. Security Model Summary

CephasOps tenant safety depends on multiple layers working together:

| Layer | Protection |
|-------|------------|
| Frontend | cache invalidation, route guards, tenant-scoped UI state |
| API boundary | middleware, tenant provider, permission checks |
| Services | effective company resolution, fail-closed behavior |
| Data layer | EF query filters |
| Write layer | TenantSafetyGuard, SaveChanges validation |
| Jobs | tenant execution wrappers |
| Event bus | CompanyId on events; dispatcher sets scope; job executor enforces company match |
| Finance | tenant-scoped calculations and snapshots |
| Observability | platform-only operational views |

---

## 10. Final State

The platform now provides:

- tenant-safe reads
- tenant-safe writes
- protected tenant switching in the frontend
- platform-only operational monitoring
- tenant-scoped financial calculations
- explicit and controlled platform bypasses

**CephasOps is now structured as a production-ready multi-tenant SaaS platform with an async event-driven backbone for scalable, tenant-safe side effects.**
