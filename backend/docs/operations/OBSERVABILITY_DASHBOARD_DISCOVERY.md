# Observability Dashboard — Discovery and Model (Phase 1–2)

**Date:** 2026-03-13  
**Purpose:** Internal map of existing operational telemetry and target dashboard model for platform operators. No schema changes; minimal additive backend/frontend.

---

## 1. Existing observability foundations

### 1.1 Metrics (OpenTelemetry / System.Diagnostics.Metrics)

| Location | Metric / Counter | Dimensions | Notes |
|----------|------------------|------------|--------|
| `TenantOperationalMetrics` | `cephasops.tenant_operations.requests_total` | tenant_id, success | Recorded in RequestLogContextMiddleware |
| | `cephasops.tenant_operations.jobs_executed_total` | tenant_id | BackgroundJobProcessorService (success) |
| | `cephasops.tenant_operations.job_failures_total` | tenant_id | BackgroundJobProcessorService (failure) |
| | `cephasops.tenant_operations.notifications_sent_total` | tenant_id, success | NotificationDispatchWorkerHostedService |
| | `cephasops.tenant_operations.integration_deliveries_total` | tenant_id, success | OutboundIntegrationBus |

- Exported via `AddMeter(TenantOperationalMetrics.MeterName)` in Program.cs; Prometheus scrape at `/metrics`.
- **In-memory counters only** — no dashboard API reads these directly; we use DB aggregates for the dashboard.

### 1.2 Structured logs

- **Request:** `RequestLogContextMiddleware` — one line per request: tenantId, operation=Request, durationMs, success; LogContext has CompanyId, TenantId, UserId, DepartmentId.
- **Jobs:** `BackgroundJobProcessorService` — metrics + optional guard; logs on failure.
- **Notifications:** `NotificationDispatchWorkerHostedService` — RecordNotificationSent(companyId, success).
- **Integrations:** `OutboundIntegrationBus` — RecordIntegrationDelivery(companyId, success).

### 1.3 TenantOperationsGuard

- **Location:** `Infrastructure/Operational/TenantOperationsGuard.cs`
- **Role:** Detection-only; in-memory sliding window; **logs warning** when thresholds exceeded (job failure spike, retry storm, notification failures, request error spike). Does not block.
- **State:** Not exposed via API; no buffer of “recent warnings” for dashboard. Suspicious-tenant signal for dashboard derived from **HealthStatus** (Warning/Critical) and **TenantAnomalyEvent** (DB) instead.

### 1.4 Persisted data usable for dashboard

| Source | Scope | Use for dashboard |
|--------|--------|-------------------|
| **TenantMetricsDaily** | Per tenant, per day | ApiCalls, BackgroundJobsExecuted, StorageBytes, ActiveUsers, LastActivity (max DateUtc) |
| **TenantMetricsMonthly** | Per tenant, per month | Aggregates for platform summary |
| **JobExecutions** | CompanyId, Status, LastErrorAtUtc | Job failures (Failed/DeadLetter) in last 24h per tenant |
| **NotificationDispatches** | CompanyId, Status | Sent vs Failed/DeadLetter in last 24h per tenant |
| **OutboundIntegrationDeliveries** | CompanyId, Status | Delivered vs Failed/DeadLetter in last 24h per tenant |
| **Tenants** | Id, Name, IsActive | Tenant name and status |
| **TenantAnomalyEvent** | TenantId, Kind, Severity, OccurredAtUtc | Recent anomaly/warning list for tenant detail |

### 1.5 Existing API endpoints (platform admin)

| Endpoint | Auth | Purpose |
|----------|------|--------|
| `GET /api/platform/analytics/dashboard` | SuperAdmin, AdminTenantsView | PlatformDashboardAnalyticsDto (active tenants, monthly usage, job volume) |
| `GET /api/platform/analytics/tenant-health` | SuperAdmin, AdminTenantsView | List TenantHealthDto (no tenant name; no notifications/integrations) |
| `GET /api/platform/analytics/anomalies` | SuperAdmin, AdminTenantsView | TenantAnomalyDto list |
| `GET /api/platform/analytics/drift` | SuperAdmin, AdminTenantsView | Platform drift |
| `GET /api/platform/analytics/performance-health` | SuperAdmin, AdminTenantsView | Performance health |
| `GET /api/platform/analytics/platform-health` | SuperAdmin, AdminTenantsView | Aggregated platform health |
| `GET /api/admin/operations/overview` | SuperAdmin, Admin, JobsView | OperationalOverviewDto (jobs, event store, payout, guard violations) |

### 1.6 Missing for dashboard (resolved without schema change)

- **Tenant name** on tenant-health: available from `Tenants` table; add to DTO and query.
- **Request “error” count:** Not stored per tenant in DB; only in metrics. Dashboard uses **job failures** and **HealthStatus**; request errors can be added to TenantMetricsDaily in a future change if needed.
- **Notifications sent/failed, Integrations delivered/failed:** Available by querying `NotificationDispatches` and `OutboundIntegrationDeliveries` by CompanyId (in tenant’s companies) and status, time-bound (e.g. last 24h).
- **Suspicious-tenant flag:** Use existing `HealthStatus` (Warning/Critical) and optional `TenantAnomalyEvent` for tenant detail “recent warnings”.

---

## 2. Target observability model (dashboard views)

### 2.1 Tenant overview (table)

| Field | Source |
|-------|--------|
| Tenant name | Tenants.Name |
| Status | Tenants.IsActive |
| Request count | TenantMetricsDaily (last 24h) ApiCalls |
| Error count | (Request errors not in DB; use job failures or leave 0 and document) |
| Background jobs ok/fail | TenantMetricsDaily.BackgroundJobsExecuted; JobExecutions Failed+DeadLetter last 24h |
| Notifications ok/fail | Count NotificationDispatches (Sent) vs (Failed + DeadLetter) last 24h by tenant companies |
| Integrations ok/fail | Count OutboundIntegrationDeliveries (Delivered) vs (Failed + DeadLetter) last 24h by tenant companies |
| Last activity | Max of TenantMetricsDaily.DateUtc, or latest JobExecution/Notification/Integration activity |
| Warning state | HealthStatus (Healthy | Warning | Critical) or “suspicious” flag when not Healthy |

### 2.2 Tenant detail view

- Request trend: TenantMetricsDaily.ApiCalls for last 7 days.
- Error trend: JobExecutions failed count per day (or bucket) for last 7 days.
- Job activity trend: TenantMetricsDaily.BackgroundJobsExecuted for last 7 days.
- Notification trend: Count NotificationDispatches by day for last 7 days (optional).
- Integration trend: Count OutboundIntegrationDeliveries by day for last 7 days (optional).
- Recent warnings/errors: TenantAnomalyEvent for tenant, last N.

### 2.3 Platform summary

- Total active tenants: existing dashboard.
- Noisy tenants: e.g. top N by request count or job volume (from existing or extended DTO).
- Failed jobs today: sum of JobExecutions (Failed+DeadLetter) where LastErrorAtUtc today.
- Failed notifications today: count NotificationDispatches (Failed+DeadLetter) last 24h.
- Failed integrations today: count OutboundIntegrationDeliveries (Failed+DeadLetter) last 24h.
- Tenants with warnings: count of tenants with HealthStatus != Healthy.

---

## 3. Authorization

- **Platform observability dashboard:** Only **platform admins** (SuperAdmin + AdminTenantsView). Existing `api/platform/analytics` already enforces this.
- **Tenant users:** Must not see platform observability endpoints or cross-tenant data. Enforced by route (api/platform/*) and role/permission; tenant-scoped APIs remain unchanged.

---

## 4. Implementation approach

1. **Backend:** Extend `IPlatformAnalyticsService` / `PlatformAnalyticsService` with:
   - **GetTenantOperationsOverviewAsync:** Returns list of tenant overview rows (name, status, requests, job ok/fail, notification ok/fail, integration ok/fail, last activity, HealthStatus). Use platform bypass; read from Tenants, TenantMetricsDaily, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries.
   - **GetTenantOperationsDetailAsync(tenantId):** Returns trend data (daily buckets) + recent TenantAnomalyEvent list.
   - **GetPlatformOperationsSummaryAsync:** Optional; or extend existing dashboard DTO with failed jobs/notifications/integrations today and tenant-warning count.
2. **API:** New or extended endpoints under `api/platform/analytics` (e.g. `GET tenant-operations-overview`, `GET tenant-operations-detail/{tenantId}`, and extend dashboard response if needed).
3. **Frontend:** New platform-admin-only page (e.g. “Platform Observability” or “Tenant Operations”) that calls these endpoints; summary cards, tenant table, tenant detail drawer. Hidden from non–platform-admin users; query keys scoped to platform (e.g. `platform-observability`).
4. **Metrics:** No new metrics; reuse existing. Optional: document that request-error aggregation could be added to TenantMetricsDaily later.
5. **Guard signals:** Surface via HealthStatus and TenantAnomalyEvent; no change to TenantOperationsGuard itself for v1.
