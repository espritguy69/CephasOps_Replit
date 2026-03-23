# Platform Health Dashboard

**Purpose:** Single operational view that aggregates Platform Guardian outputs for platform operators.

---

## Endpoint

**GET /api/platform/analytics/platform-health**

- **Auth:** SuperAdmin, AdminTenantsView.
- **Returns:** PlatformHealthDto.

---

## PlatformHealthDto fields

| Field | Description |
|-------|-------------|
| GeneratedAtUtc | When the snapshot was generated. |
| TotalActiveTenants | Count of active tenants (from tenant-health). |
| TenantsInWarningState | Tenants with HealthStatus = Warning. |
| TenantsInCriticalAnomalyState | Distinct tenants with at least one Critical anomaly in last 24h. |
| RateLimitBreachCountLast24h | Reserved; set to 0 until rate-limit breach metric is available. |
| StuckJobResetsLast24h | Reserved; null until watchdog/worker reset count is tracked. |
| FailedJobsLast24h | Sum of JobFailuresLast24h across all tenants. |
| StorageWarningTenants | Heuristic: tenants in Warning with non-zero storage. |
| SuspiciousAuthOrImpersonationCount | Reserved; null when not available. |
| PerformanceDegradationFlag | True if performance watchdog reports high-latency tenants or high pending job count. |
| Summary | Short human-readable summary. |

---

## Aggregation logic

- **PlatformHealthService** calls:
  - **GetTenantHealthAsync** (tenant health)
  - **GetAnomaliesAsync** (Critical, last 24h)
  - **GetPerformanceHealthAsync** (performance watchdog)
- It aggregates counts and sets PerformanceDegradationFlag from the performance watchdog result. Rate-limit and stuck-job metrics are placeholders until those metrics are collected (e.g. from logs or counters).

---

## Usage

- Dashboard UI can poll **platform-health** for a single card or header.
- Alerts can be based on TenantsInCriticalAnomalyState > 0, PerformanceDegradationFlag, or CriticalCount from drift.
