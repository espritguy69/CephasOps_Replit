# Tenant Operational Observability

**Date:** 2026-03-13  
**Purpose:** Operational safety for multi-tenant SaaS: per-tenant visibility, metrics, fairness, and alerting signals. No change to tenant isolation or API behavior.

---

## Overview

The platform instruments key operational paths so operations can:

- **Observe** system health per tenant (logs and metrics).
- **Limit** a single tenant from overloading shared resources (job fairness).
- **Detect** problematic tenants via threshold-based log warnings (no blocking).

---

## 1. Tenant log fields

Structured log properties are consistent across operational paths:

| Field        | Description |
|-------------|-------------|
| `tenantId`  | Company ID (tenant); `null` or omitted when platform-wide. |
| `operation` | One of: `Request`, `BackgroundJob`, `NotificationDispatch`, `IntegrationDelivery`. |
| `durationMs`| Elapsed milliseconds. |
| `success`   | `true` or `false`. |
| `errorType` | Exception type name (when applicable, e.g. job failure). |

**Serilog / request context:** `RequestLogContextMiddleware` already pushes `CompanyId` and `TenantId` into `LogContext`; after the request it logs one line with `tenantId`, `operation=Request`, `durationMs`, `success`.

**Paths instrumented:**

- **API requests** – `RequestLogContextMiddleware` (after `_next`).
- **Background jobs** – `BackgroundJobProcessorService.ProcessJobAsync` (success and failure).
- **Notification dispatch** – `NotificationDispatchWorkerHostedService` (per dispatch).
- **Integration delivery** – `OutboundIntegrationBus.DispatchDeliveryInternalAsync` (per delivery).

---

## 2. Tenant metrics

Lightweight OpenTelemetry counters (meter: `CephasOps.TenantOperations`), exported with existing pipeline (e.g. Prometheus):

| Metric name | Description | Tags |
|------------|-------------|------|
| `cephasops.tenant_operations.requests_total` | API requests per tenant | `tenant_id`, `success` |
| `cephasops.tenant_operations.jobs_executed_total` | Background jobs executed per tenant | `tenant_id` |
| `cephasops.tenant_operations.job_failures_total` | Background job failures per tenant | `tenant_id` |
| `cephasops.tenant_operations.notifications_sent_total` | Notification dispatches per tenant | `tenant_id`, `success` |
| `cephasops.tenant_operations.integration_deliveries_total` | Outbound integration deliveries per tenant | `tenant_id`, `success` |

**Implementation:** `Infrastructure/Metrics/TenantOperationalMetrics.cs`. When `tenant_id` is null or empty, the tag value is `"platform"`.

**Registration:** `Program.cs` calls `AddMeter(TenantOperationalMetrics.MeterName)` with the existing OpenTelemetry metrics setup.

---

## 3. Tenant fairness (noisy-tenant protection)

**Background job processor**

- **Option:** `BackgroundJobs:Fairness:MaxJobsPerTenantPerCycle` (default: 5). Max jobs processed per tenant per polling cycle.
- **Behavior:** Queued jobs are ordered in a round-robin style by tenant; each tenant gets at most `MaxJobsPerTenantPerCycle` in one cycle. Prevents one tenant from monopolizing workers.
- **Implementation:** `BackgroundJobProcessorService.OrderForFairness` groups by `CompanyId`, then builds an ordered list (one from each tenant in turn, up to the cap per tenant).

**Other workers**

- Notification dispatch and integration retry workers process batches as before; per-tenant caps can be added later if needed. Current instrumentation (metrics + logs) allows identifying noisy tenants.

---

## 4. Alerting signals (detection only)

**TenantOperationsGuard** records events in an in-memory sliding window and **logs a warning** when a threshold is exceeded. It does **not** block or throttle.

**Options:** `TenantOperations:Guard` (see below).

| Signal | Option | Default | Log message (example) |
|--------|--------|---------|------------------------|
| Job failure spike | `JobFailureThreshold` | 10 | "TenantOperationsGuard: Tenant job failure spike detected. TenantId=..., FailureCount=... in last N minutes" |
| Job retry storm | `JobRetryThreshold` | 20 | "TenantOperationsGuard: Tenant job retry threshold exceeded. TenantId=..., RetryCount=... in last N minutes" |
| Notification failures | `NotificationFailureThreshold` | 5 | "TenantOperationsGuard: Tenant notification failures detected. TenantId=..., FailureCount=... in last N minutes" |
| Request error rate | `RequestErrorThreshold` | 50 | "TenantOperationsGuard: Tenant request error rate spike. TenantId=..., ErrorCount=... in last N minutes" |

**Configuration:** `appsettings.json` (optional):

```json
{
  "TenantOperations": {
    "Guard": {
      "WindowMinutes": 5,
      "JobFailureThreshold": 10,
      "JobRetryThreshold": 20,
      "NotificationFailureThreshold": 5,
      "RequestErrorThreshold": 50
    }
  },
  "BackgroundJobs": {
    "Fairness": {
      "MaxJobsPerTenantPerCycle": 5
    }
  }
}
```

**Implementation:** `Infrastructure/Operational/TenantOperationsGuard.cs`. Registered as singleton; used by request middleware (on error response), job processor (on failure/retry), and notification worker (on failure).

---

## 5. Files touched

| Area | File(s) |
|------|--------|
| Metrics | `Infrastructure/Metrics/TenantOperationalMetrics.cs` (new) |
| Guard | `Infrastructure/Operational/TenantOperationsGuard.cs` (new), `TenantOperationsGuardOptions` |
| Fairness | `Application/Workflow/BackgroundJobFairnessOptions.cs` (new), `BackgroundJobProcessorService` (OrderForFairness, options) |
| Request logs | `Api/Middleware/RequestLogContextMiddleware.cs` (duration, success, metrics, guard on error) |
| Job logs/metrics | `Application/Workflow/Services/BackgroundJobProcessorService.cs` (structured log, metrics, guard) |
| Notification logs/metrics | `Application/Notifications/NotificationDispatchWorkerHostedService.cs` |
| Integration logs/metrics | `Application/Integration/OutboundIntegrationBus.cs` |
| Registration | `Api/Program.cs` (meter, options, guard singleton) |
| Tests | `Application.Tests/TenantIsolation/TenantOperationalObservabilityTests.cs` (metrics no-throw, fairness ordering) |

---

## 6. Tenant isolation

- **No change** to tenant isolation: no new global queries, no bypass of tenant safeguards. All instrumentation uses existing tenant context (`TenantScope.CurrentTenantId`, `ITenantProvider`, or entity `CompanyId`).
- **API responses** unchanged; only server-side logging and metrics are added.

---

## 7. Platform observability dashboard (2026-03-13)

A **platform-admin-only** operational dashboard gives operators tenant-aware visibility without exposing cross-tenant data to tenant users.

### Authorization

- **Endpoints:** All under `GET /api/platform/analytics/*`. Require `[Authorize(Roles = "SuperAdmin")]` and `[RequirePermission(PermissionCatalog.AdminTenantsView)]`.
- **Who can see:** Platform admins (SuperAdmin or users with `admin.tenants.view`). Tenant users and normal Admins without that permission receive 403.
- **Frontend:** Route `/admin/platform-observability`; nav item visible only when user has `admin.tenants.view` (or SuperAdmin). Page content is hidden for non–platform-admins with a clear message.

### Endpoints and data

| Endpoint | Purpose |
|----------|--------|
| `GET /api/platform/analytics/operations-summary` | Summary: active tenants, failed jobs/notifications/integrations today, tenants with warnings count. |
| `GET /api/platform/analytics/tenant-operations-overview` | Per-tenant table: name, status, requests, job ok/fail, notifications ok/fail, integrations ok/fail, last activity, HealthStatus (Healthy \| Warning \| Critical). |
| `GET /api/platform/analytics/tenant-operations-detail/{tenantId}` | Single tenant: last 7 days daily buckets (requests, jobs, notifications, integrations), recent TenantAnomalyEvent list. Returns 404 if tenant not found. |

All aggregation runs under `TenantScopeExecutor.RunWithPlatformBypassAsync`; reads only from Tenants, TenantMetricsDaily, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, TenantAnomalyEvents. No schema change; no raw business data returned.

### Metrics and signals used

- **Requests:** TenantMetricsDaily.ApiCalls (last 24h).
- **Job failures / ok:** JobExecutions (Failed/DeadLetter) and TenantMetricsDaily.BackgroundJobsExecuted.
- **Notifications:** NotificationDispatches (Sent vs Failed/DeadLetter) by company in tenant, last 24h.
- **Integrations:** OutboundIntegrationDeliveries (Delivered vs Failed/DeadLetter) by company in tenant, last 24h.
- **Warning state:** Derived from job failure thresholds (e.g. ≥50 = Critical, ≥10 or no activity = Warning). Optional: TenantAnomalyEvent for “recent anomalies” in tenant detail.

### Documentation

- **Discovery and model:** `backend/docs/operations/OBSERVABILITY_DASHBOARD_DISCOVERY.md`.
- **Changelog:** `backend/docs/remediation/SAAS_REMEDIATION_CHANGELOG.md` (Platform observability dashboard entry).
