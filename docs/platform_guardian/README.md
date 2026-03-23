# Platform Guardian

**Purpose:** Continuous safety and reliability layer that helps the platform self-audit, self-detect drift, and self-surface tenant risks before they become production incidents.

---

## Contents

| Document | Description |
|----------|-------------|
| [QUERY_SAFETY_REPORT.md](QUERY_SAFETY_REPORT.md) | Audit of risky query patterns (IgnoreQueryFilters, raw SQL, controller overrides). |
| [TENANT_ANOMALY_DETECTION.md](TENANT_ANOMALY_DETECTION.md) | Tenant anomaly detection service, severity levels, persistence, endpoint. |
| [PLATFORM_DRIFT_REPORT.md](PLATFORM_DRIFT_REPORT.md) | Configuration drift detection (JobOrchestration, SaaS:TenantRateLimit, StorageLifecycle, etc.). |
| [PERFORMANCE_WATCHDOG.md](PERFORMANCE_WATCHDOG.md) | Performance health (degraded tenants, queue lag, slow paths). |
| [SECURITY_SURFACE_AUDIT.md](SECURITY_SURFACE_AUDIT.md) | Security audit of AllowAnonymous, SuperAdmin, impersonation, file access, rate limits. |
| [PLATFORM_HEALTH_DASHBOARD.md](PLATFORM_HEALTH_DASHBOARD.md) | Aggregated platform health endpoint (GET /api/platform/analytics/platform-health). |
| [GUARDIAN_SCHEDULING.md](GUARDIAN_SCHEDULING.md) | Scheduled Guardian hosted service (anomaly, drift, performance on interval). |
| [PLATFORM_GUARDIAN_SUMMARY.md](PLATFORM_GUARDIAN_SUMMARY.md) | Summary of what was implemented, risks monitored, and operational usage. |

---

## Endpoints (SuperAdmin)

| Endpoint | Description |
|----------|-------------|
| GET /api/platform/analytics/anomalies | List tenant anomaly events (since, tenantId, severity, take). |
| GET /api/platform/analytics/drift | Configuration drift report vs baseline. |
| GET /api/platform/analytics/performance-health | Performance health (queue lag, degraded tenants). |
| GET /api/platform/analytics/platform-health | Aggregated platform health (one operational view). |

Existing tenant-health and dashboard remain: GET /api/platform/analytics/tenant-health, GET /api/platform/analytics/dashboard.

---

## Configuration

- **PlatformGuardian:Enabled** – Enable scheduled Guardian (default true).
- **PlatformGuardian:RunIntervalMinutes** – Interval in minutes (default 60; min 5).
- **PlatformGuardian:AnomalyDetection**, **DriftDetection**, **PerformanceWatchdog** – Section names for sub-options.

See [GUARDIAN_SCHEDULING.md](GUARDIAN_SCHEDULING.md) and [TENANT_ANOMALY_DETECTION.md](TENANT_ANOMALY_DETECTION.md).

---

## Safety

- No removal or weakening of tenant safety guardrails (TenantGuardMiddleware, TenantScopeExecutor, global query filters).
- All Guardian reads/writes that touch cross-tenant data use **TenantScopeExecutor.RunWithPlatformBypassAsync** in platform-only code paths.
- Guardian does not fail application runtime when drift or anomalies are found; it reports and logs.
