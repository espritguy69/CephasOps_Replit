# Tenant Observability Metrics

**Purpose:** Improve monitoring with per-tenant health metrics and a dedicated endpoint for platform operators.

---

## Endpoint

**GET /api/platform/analytics/tenant-health**

- **Auth:** SuperAdmin (or permission AdminTenantsView).
- **Returns:** Array of **TenantHealthDto** (one per active tenant).

---

## TenantHealthDto

| Field | Type | Description |
|-------|------|-------------|
| **TenantId** | Guid | Tenant identifier. |
| **ApiRequestsLast24h** | int | API request count in the last 24 hours (from TenantMetricsDaily). |
| **JobFailuresLast24h** | int | Count of failed/DeadLetter job executions in the last 24 hours for that tenant’s companies. |
| **StorageBytes** | long | Current storage usage (from latest daily metrics). |
| **ActiveUsers** | int | Active users in the period (from daily metrics). |
| **LastActivityUtc** | DateTime? | Latest activity date (from daily metrics). |
| **HealthStatus** | string | **Healthy** \| **Warning** \| **Critical**. |

---

## HealthStatus rules

- **Critical:** JobFailuresLast24h ≥ 50.
- **Warning:** JobFailuresLast24h ≥ 10, or no recent daily metrics and no LastActivityUtc.
- **Healthy:** Otherwise.

Thresholds are implementation details and can be tuned in **PlatformAnalyticsService**.

---

## Data sources

- **TenantMetricsDaily:** ApiCalls, ActiveUsers, StorageBytes, DateUtc (last 24h).
- **JobExecutions:** Failed and DeadLetter jobs in last 24h, grouped by tenant via Company.TenantId.
- **Tenants:** Active tenant list.

The service runs under **TenantScopeExecutor.RunWithPlatformBypassAsync** so it can read across all tenants.

---

## Usage

- Dashboards: plot HealthStatus, ApiRequestsLast24h, JobFailuresLast24h, StorageBytes per tenant.
- Alerts: trigger on Critical or Warning.
- Capacity: use StorageBytes and ApiRequestsLast24h for capacity and throttling decisions.
