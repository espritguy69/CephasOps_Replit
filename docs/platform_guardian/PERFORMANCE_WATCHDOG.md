# Performance Watchdog

**Purpose:** Platform Guardian service that surfaces performance degradation on multi-tenant hot paths: list/report/dashboard usage, job execution delays, queue lag, and tenants with high latency impact.

---

## Service

**PerformanceWatchdogService** (IPerformanceWatchdogService)

- **GetPerformanceHealthAsync:** Runs under platform bypass. Aggregates:
  - **TenantsWithHighLatencyImpact:** Tenant IDs in Warning or Critical health (from tenant-health).
  - **PendingJobCount:** Count of JobExecutions in Pending status with NextRunAtUtc due (queue lag indicator).
  - **Summary:** Short message (e.g. "N tenant(s) in Warning/Critical; M pending job(s)").
  - **SlowQueryCountLastWindow / SlowJobExecutionCountLastWindow / DegradedEndpoints:** Reserved for future use (e.g. when request timing or job duration tracking is added).

---

## Focus areas (current and planned)

| Area | Current | Planned |
|------|---------|---------|
| List/report/dashboard queries | Reflected in tenant health (job failures, API load) | Optional: slow query log or middleware |
| Background job execution delays | Pending job count as queue lag | Job execution duration tracking |
| Queue lag | PendingJobCount | Same |
| Storage lifecycle duration | Not tracked here | Optional metrics |
| Job watchdog resets | Not tracked here | Optional counter in watchdog |
| Tenant health endpoint cost | Not tracked | Optional |
| Signup/provisioning latency | Not tracked | Optional |

---

## Endpoint

**GET /api/platform/analytics/performance-health**

- **Auth:** SuperAdmin, AdminTenantsView.
- **Returns:** PerformanceHealthDto (GeneratedAtUtc, SlowQueryCountLastWindow, SlowJobExecutionCountLastWindow, DegradedEndpoints, TenantsWithHighLatencyImpact, PendingJobCount, Summary).

---

## Alignment with observability

- Uses existing **GetTenantHealthAsync** (tenant health) and **JobExecutions** for pending count. No new persistence required for the basic view.
- For deeper observability (slow query thresholds, repeated degraded endpoints), extend with request-duration middleware or application metrics and feed into the same DTO.
