# Operational Runbooks

**Date:** 2026-03-13

Short runbooks for common SaaS platform operations.

---

## 1. New tenant signup (self-service)

1. Tenant submits `POST /api/platform/signup` with company name, code, admin name, email, password.
2. Backend validates and provisions (tenant, company, default departments, trial subscription, admin user).
3. If 409: company code, slug, or email already in use – advise user to choose different values.
4. Tenant admin logs in with `POST /api/auth/login` and completes onboarding wizard steps via `GET/PATCH /api/onboarding/status`.

---

## 2. Tenant cannot log in

1. Run **tenant diagnostics:** `GET /api/platform/tenants/{tenantId}/diagnostics` (resolve tenantId from slug or support ticket).
2. Check: user count, company status, subscription status, trial end.
3. If **trial expired:** extend trial via `PATCH /api/platform/tenants/{tenantId}/subscription` (e.g. set `TrialEndsAtUtc` or update plan).
4. If **company suspended:** resume via `POST /api/platform/tenants/{tenantId}/resume`.
5. If **password / lockout:** use existing admin password reset or unlock flows.

---

## 3. Tenant over storage quota

1. Confirm with **usage** or **diagnostics** (or `GET /api/platform/usage/tenants/{tenantId}`).
2. Options:  
   - Increase limit: `PATCH /api/platform/tenants/{tenantId}/subscription` with `StorageLimitBytes`.  
   - Ask tenant to delete files or upgrade plan.
3. Uploads will return 403 "Storage quota exceeded" until under limit.

---

## 4. Failed or stuck job for a tenant

1. Identify job: use job/execution listing or logs (filter by TenantId / CompanyId).
2. Call **retry:** `POST /api/platform/support/tenants/{tenantId}/jobs/{jobExecutionId}/retry`.
3. Confirm job status becomes Pending and is picked up by worker; check logs if it fails again.

---

## 5. Reproduce issue as tenant (impersonation)

1. Get tenant id (e.g. from diagnostics or tenant list).
2. Call `POST /api/platform/support/impersonate` with `{ "tenantId": "<guid>" }`.
3. Use returned `AccessToken` as Bearer in API client; operate as that tenant’s admin for the token lifetime.
4. Audit log will show impersonation event.

---

## 6. Dashboard and analytics

- **Dashboard:** `GET /api/platform/analytics/dashboard` (SuperAdmin).
- Returns: active/total tenant count, current/previous month usage summary (storage, API calls, orders, jobs), total storage, job volume last 30 days.
- Data source: `TenantMetricsDaily`, `TenantMetricsMonthly`, `Tenants`. Ensure aggregation job runs (e.g. daily) so metrics are up to date.

---

## 7. Platform Guardian (operational view)

- **Platform health (single view):** `GET /api/platform/analytics/platform-health` – aggregated tenants, anomalies, failed jobs, performance flag.
- **Anomalies:** `GET /api/platform/analytics/anomalies` – tenant anomaly events (filter by severity, tenantId).
- **Drift:** `GET /api/platform/analytics/drift` – config drift report vs baseline.
- **Performance:** `GET /api/platform/analytics/performance-health` – queue lag, degraded tenants.
- See [Platform Guardian README](../platform_guardian/README.md) and [PLATFORM_GUARDIAN_SUMMARY.md](../platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md).

---

## 8. References

- [SUPPORT_PROCEDURES.md](SUPPORT_PROCEDURES.md) – Support endpoints and security.
- [SAAS_OPERATIONS_GUIDE.md](../saas_scaling/SAAS_OPERATIONS_GUIDE.md) – Full operations guide.
- [Platform Guardian](../platform_guardian/README.md) – Query safety, anomaly, drift, performance, security audit.
- [Production Architecture](../production_architecture/PRODUCTION_INFRASTRUCTURE_SUMMARY.md) – Deployment topology, worker roles, CI/CD, [Production Runbooks](../production_architecture/PRODUCTION_RUNBOOKS.md), staged rollout.
