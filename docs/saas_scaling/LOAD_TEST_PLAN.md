# Load Test Plan

**Purpose:** Prepare the platform for load testing with many tenants and high concurrency. Use **tools/load_testing/seed_test_tenants.ps1** to seed tenant data; then run scenarios below.

---

## 1. Seed data targets

| Data | Target | How |
|------|--------|-----|
| **Tenants** | 50 | Run `seed_test_tenants.ps1` (provisions via POST /api/platform/tenants/provision). |
| **Users** | 200 | ~4 users per tenant: create via POST /api/users (or invite) per tenant after login as tenant admin. |
| **Orders** | 1000 | ~20 orders per tenant: create via Orders API or import per tenant. |
| **Background jobs** | Many | Trigger report exports, sync jobs, or enqueue via app so JobExecution queue has work. |
| **Files** | Many | Upload files via POST /api/files per tenant to exercise storage and lifecycle. |

---

## 2. Prerequisites

- Backend API running (e.g. http://localhost:5000).
- SuperAdmin JWT: obtain via POST /api/auth/login with SuperAdmin credentials; set `$env:LOAD_TEST_JWT` or pass `-Token` to the script.
- PostgreSQL and app configured for the environment.

---

## 3. Seed script usage

```powershell
cd tools/load_testing
$env:LOAD_TEST_JWT = "<your-superadmin-jwt>"
.\seed_test_tenants.ps1 -BaseUrl "http://localhost:5000" -TenantCount 50
# Optional: -WhatIf to dry-run
```

---

## 4. Load test scenarios

### 4.1 Concurrent tenants

- **Goal:** Verify tenant isolation and stability under many active tenants.
- **Method:** Simulate requests from multiple tenants (different CompanyId/tenant context) in parallel. Use distinct JWTs per tenant (login as each tenant admin) or a test harness that sets tenant context.
- **Metrics:** Response times, error rate, DB connection usage, rate limit 429s per tenant.

### 4.2 Background job spikes

- **Goal:** Verify job fairness and no single tenant starving others.
- **Method:** Enqueue many jobs for a subset of tenants (e.g. 5 tenants with 100 jobs each); run worker; confirm other tenants still get jobs (tenant fairness) and no deadlocks.
- **Metrics:** Jobs completed per tenant per cycle, ExecutionTimeMs in logs, ResetStuckRunning count.

### 4.3 File uploads

- **Goal:** Storage quota and lifecycle under load.
- **Method:** Upload files from many tenants concurrently; check storage usage and TenantMetricsDaily/Monthly; run StorageLifecycleService and confirm tier transitions.
- **Metrics:** Upload latency, storage bytes per tenant, tier distribution.

### 4.4 Heavy reporting queries

- **Goal:** Index usage and query performance for tenant-scoped lists.
- **Method:** Run report/dashboard endpoints for many tenants (Orders list by date, Job list, File list) with filters (CompanyId, CreatedAt, Status). Use indexes from TENANT_INDEX_AUDIT.md.
- **Metrics:** Query duration, DB CPU, index usage.

---

## 5. Observability during tests

- **GET /api/platform/analytics/tenant-health:** Check HealthStatus (Healthy/Warning/Critical) and JobFailuresLast24h, ApiRequestsLast24h per tenant.
- **Logs:** Filter by TenantId/CompanyId; watch for TenantRateLimitExceeded, job failures, and watchdog resets.
- **Database:** Monitor connection pool, long-running queries, and lock waits.

---

## 6. Safety

- Run load tests in a dedicated environment (e.g. staging), not production.
- Tenant provisioning creates real tenants and companies; use a test database and clean up or isolate test data as needed.
