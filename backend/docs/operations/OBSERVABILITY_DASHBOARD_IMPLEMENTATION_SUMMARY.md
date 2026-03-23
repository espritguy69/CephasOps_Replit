# Platform Observability Dashboard — Implementation Summary

**Date:** 2026-03-13

---

## 1. Files changed

### Backend

| File | Change |
|------|--------|
| `Application/Platform/PlatformAnalyticsDto.cs` | Added `TenantOperationsOverviewItemDto`, `TenantOperationsDetailDto`, `TenantOperationsDailyBucketDto`, `PlatformOperationsSummaryDto`; import Guardian for `TenantAnomalyDto`. |
| `Application/Platform/IPlatformAnalyticsService.cs` | Added `GetTenantOperationsOverviewAsync`, `GetTenantOperationsDetailAsync`, `GetPlatformOperationsSummaryAsync`. |
| `Application/Platform/PlatformAnalyticsService.cs` | Implemented the three new methods using platform bypass; read from Tenants, TenantMetricsDaily, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, TenantAnomalyEvents. |
| `Api/Controllers/PlatformAnalyticsController.cs` | Added `GET operations-summary`, `GET tenant-operations-overview`, `GET tenant-operations-detail/{tenantId}`. |

### Frontend

| File | Change |
|------|--------|
| `api/platformObservability.ts` | **New.** API client and DTOs for operations-summary, tenant-operations-overview, tenant-operations-detail; query key roots for platform-observability. |
| `pages/admin/PlatformObservabilityPage.tsx` | **New.** Summary cards, tenant operations table, tenant detail drawer (7-day trend + recent anomalies). SuperAdmin-only visibility guard. |
| `App.tsx` | Added route `/admin/platform-observability` with `SettingsProtectedRoute` and `PlatformObservabilityPage`. |
| `components/layout/Sidebar.tsx` | Added “Platform Observability” nav item (permission `admin.tenants.view`); fallback for Admin role. |

### Documentation

| File | Change |
|------|--------|
| `docs/operations/OBSERVABILITY_DASHBOARD_DISCOVERY.md` | **New.** Phase 1–2 discovery map and target model. |
| `docs/operations/TENANT_OPERATIONAL_OBSERVABILITY.md` | Added section 7: Platform observability dashboard (auth, endpoints, metrics, docs refs). |
| `docs/remediation/SAAS_REMEDIATION_CHANGELOG.md` | Added “Platform observability dashboard (2026-03-13)” entry. |

### Tests

| File | Change |
|------|--------|
| `Api.Tests/Integration/PlatformObservabilityApiTests.cs` | **New.** 4 tests: SuperAdmin gets 200 for operations-summary and tenant-operations-overview; Member gets 403 for operations-summary; non-existent tenant detail returns 404. |

---

## 2. Backend endpoints added

- `GET /api/platform/analytics/operations-summary` → `PlatformOperationsSummaryDto`
- `GET /api/platform/analytics/tenant-operations-overview` → `IReadOnlyList<TenantOperationsOverviewItemDto>`
- `GET /api/platform/analytics/tenant-operations-detail/{tenantId}` → `TenantOperationsDetailDto` or 404

All require `Authorize(Roles = "SuperAdmin")` and `RequirePermission(PermissionCatalog.AdminTenantsView)`.

---

## 3. Frontend pages/components added

- **PlatformObservabilityPage:** Summary cards (active tenants, failed jobs/notifications/integrations today, tenants with warnings), tenant operations table (tenant name, status, requests, errors, jobs, notifications, integrations, last activity, warning state), tenant detail drawer (7-day daily buckets, recent anomalies). Accessible at `/admin/platform-observability`; only rendered for platform admins (SuperAdmin or `admin.tenants.view`).

---

## 4. Metrics/logs reused or extended

- **Reused (no code change):** `TenantOperationalMetrics` (requests, jobs, notifications, integrations); `TenantOperationsGuard` (log-only warnings); `RequestLogContextMiddleware`; existing OpenTelemetry meter.
- **New aggregation only:** Dashboard reads from existing persisted data: TenantMetricsDaily, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, TenantAnomalyEvents. No new metrics emitted; no schema change.

---

## 5. Tests added

- **PlatformObservabilityApiTests:** Platform-only access (SuperAdmin 200, Member 403), tenant-operations-detail 404 for unknown tenant. All 4 tests passing.

---

## 6. Docs updated

- `TENANT_OPERATIONAL_OBSERVABILITY.md` — section 7 (dashboard).
- `SAAS_REMEDIATION_CHANGELOG.md` — platform observability dashboard entry.
- `OBSERVABILITY_DASHBOARD_DISCOVERY.md` — new (discovery + model).

---

## 7. What remains (optional future work)

- **Request error count per tenant:** Not in DB; only job failures and HealthStatus drive the overview. Could add request-error aggregation to TenantMetricsDaily later if needed.
- **TenantOperationsGuard “recent warnings” API:** Guard only logs; no in-memory buffer exposed. Dashboard uses HealthStatus and TenantAnomalyEvent for anomaly visibility; optional future: expose recent guard warnings via a small buffer if desired.
