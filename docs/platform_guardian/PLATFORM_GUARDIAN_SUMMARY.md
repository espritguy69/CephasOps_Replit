# Platform Guardian Summary

**Date:** 2026-03-13

Summary of the Platform Guardian layer: what was implemented, what risks are monitored, and how to use it operationally.

---

## 1. What was implemented

| Component | Description |
|-----------|-------------|
| **Query Safety Report** | Repository-level audit of IgnoreQueryFilters(), ExecuteSqlRaw, controller companyId/tenantId overrides. Findings classified Safe / Medium-risk / Critical. No critical findings; one medium (AssetService). See [QUERY_SAFETY_REPORT.md](QUERY_SAFETY_REPORT.md). |
| **Tenant Anomaly Detection** | TenantAnomalyDetectionService evaluates per-tenant metrics (API spike, storage growth, job failure spike), persists TenantAnomalyEvent (Kind, Severity, Details). Severity: Info, Warning, Critical. GET /api/platform/analytics/anomalies. See [TENANT_ANOMALY_DETECTION.md](TENANT_ANOMALY_DETECTION.md). |
| **Configuration Drift Detection** | PlatformDriftDetectionService compares current config to baseline (JobOrchestration:Worker, SaaS:TenantRateLimit, SaaS:StorageLifecycle, PlatformGuardian). Classifies drift Informational / Warning / Critical. GET /api/platform/analytics/drift. See [PLATFORM_DRIFT_REPORT.md](PLATFORM_DRIFT_REPORT.md). |
| **Performance Watchdog** | PerformanceWatchdogService aggregates tenant health and pending job count; surfaces TenantsWithHighLatencyImpact and PendingJobCount. GET /api/platform/analytics/performance-health. See [PERFORMANCE_WATCHDOG.md](PERFORMANCE_WATCHDOG.md). |
| **Security Surface Audit** | Documented [AllowAnonymous], SuperAdmin, impersonation, support, retry/replay, upload/download, signup, companyId/tenantId acceptance, rate limits. See [SECURITY_SURFACE_AUDIT.md](SECURITY_SURFACE_AUDIT.md). |
| **Platform Health Dashboard** | PlatformHealthDto and GET /api/platform/analytics/platform-health aggregate total active tenants, warning/critical counts, failed jobs, performance flag. See [PLATFORM_HEALTH_DASHBOARD.md](PLATFORM_HEALTH_DASHBOARD.md). |
| **Scheduled Guardian** | PlatformGuardianHostedService runs anomaly, drift, and performance checks on configurable interval (default 60 min). PlatformGuardian:Enabled, RunIntervalMinutes. See [GUARDIAN_SCHEDULING.md](GUARDIAN_SCHEDULING.md). |

---

## 2. Risks now automatically monitored

- **Query safety:** Inventoried and reported; no automated code guard (optional: analyzer or tests for new IgnoreQueryFilters).
- **Tenant anomalies:** API spike, storage spike, job failure spike detected and persisted; visible via anomalies endpoint and platform-health.
- **Config drift:** Worker batch size, tenant fairness, lease, rate limit, storage lifecycle, guardian interval checked; critical drift logged.
- **Performance:** Tenants in Warning/Critical health and pending job queue surfaced; performance degradation flag on platform-health.
- **Security surface:** Documented; no automated change to auth—recommendations in SECURITY_SURFACE_AUDIT.md.

---

## 3. Critical findings

- **None.** Query safety report found no critical items. One medium-risk (AssetService IgnoreQueryFilters) and several ExecuteSqlRaw call sites recommended for review (parameterization and tenant scope).

---

## 4. Safe fixes applied

- No code changes to existing tenant guards or query patterns. Additive only: new entities (TenantAnomalyEvent), new services, new endpoints, new docs.

---

## 5. Remaining medium risks

- **AssetService** single IgnoreQueryFilters use: verify call site always constrains by company (see QUERY_SAFETY_REPORT).
- **ExecuteSqlRaw** in EmailTemplateService, TaskService, ParserTemplateService, etc.: ensure parameterized and run in correct tenant scope (review per call site).
- **Rate-limit breach / stuck-job reset counts** on platform-health are placeholders (0 or null) until metrics are collected from logs or counters.

---

## 6. Recommended operational usage

- **Dashboard:** Use GET /api/platform/analytics/platform-health for a single operational view; alert on TenantsInCriticalAnomalyState > 0 or PerformanceDegradationFlag.
- **Anomalies:** Use GET /api/platform/analytics/anomalies?severity=Critical (or Warning) to list recent events; filter by tenantId for support.
- **Drift:** Run GET /api/platform/analytics/drift after config changes or periodically; act on Critical classification.
- **Performance:** Use GET /api/platform/analytics/performance-health for queue lag and impacted tenants.
- **Scheduling:** Keep PlatformGuardian:Enabled true and RunIntervalMinutes at 15–60; increase interval if DB load is a concern with many tenants.

---

## 7. Cross-references

- [docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md](../tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md) – Automatic tenant boundary regression suite; run before release; release-blocking failures for tenant-sensitive changes.
- [docs/platform_guardian/README.md](README.md) – Index of all Guardian docs.
- [docs/saas_scaling/SAAS_SCALE_READINESS_REPORT.md](../saas_scaling/SAAS_SCALE_READINESS_REPORT.md) – Scale & reliability (indexes, jobs, rate limit, storage, observability).
- [docs/saas_operations/OPERATIONAL_RUNBOOKS.md](../saas_operations/OPERATIONAL_RUNBOOKS.md) – Runbooks; reference platform analytics and support endpoints.
- [docs/saas_scaling/SAAS_SCALING_ARCHITECTURE.md](../saas_scaling/SAAS_SCALING_ARCHITECTURE.md) – Scaling architecture and operational safeguards.
- [docs/production_architecture/PRODUCTION_INFRASTRUCTURE_SUMMARY.md](../production_architecture/PRODUCTION_INFRASTRUCTURE_SUMMARY.md) – Production deployment, workers, observability, runbooks.
