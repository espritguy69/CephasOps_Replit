# SaaS Operations Guide

**Date:** 2026-03-13

Operational guide for running CephasOps as a multi-tenant SaaS platform: admin operations, usage tracking, subscription model, and support tooling.

---

## 1. Tenant provisioning

- Use **POST /api/platform/tenants/provision** (SuperAdmin only).
- Required body: CompanyName, CompanyCode, AdminFullName, AdminEmail.
- Optional: Slug (defaults from CompanyCode), AdminPassword (if empty, temp password generated and MustChangePassword=true), **PlanSlug (default trial)**; a **default trial billing plan** is created by database seed so provisioning without PlanSlug always resolves. TrialDays (default 14), DefaultTimezone, DefaultLocale, InitialStatus (e.g. Trial, Active).
- Check availability: **GET /api/platform/tenants/check-code?code=** and **GET /api/platform/tenants/check-slug?slug=**.
- After provisioning, tenant admin can log in with AdminEmail and the provided (or temp) password.

---

## 2. Subscription model

- Each tenant has zero or one **active** subscription (TenantSubscription with Status Active or Trialing).
- **TrialEndsAtUtc:** When set, access is denied after this time until the tenant upgrades or renews.
- **SeatLimit / StorageLimitBytes:** Optional; enforce in app (e.g. before adding user or uploading file) via **ISubscriptionEnforcementService**.
- **BillingPlan** defines BillingCycle (Monthly/Yearly), Price, Currency; **TenantSubscription** links tenant to plan and stores TrialEndsAtUtc, NextBillingDateUtc.

### Subscription admin (SuperAdmin)

- **GET /api/platform/tenants/{tenantId}/subscription** – View current subscription (active/trialing or latest).
- **PATCH /api/platform/tenants/{tenantId}/subscription** – Update plan (PlanSlug), Status, TrialEndsAtUtc, NextBillingDateUtc, SeatLimit, StorageLimitBytes. Only provided fields are updated; invalid plan or status returns 400.

### Changing company status

- **PATCH /api/platform/tenants/companies/{companyId}/status** with body `{ "status": "Active" | "Suspended" | "Disabled" | "Trial" | "Archived" | "PendingProvisioning" }`.
- **POST /api/platform/tenants/{tenantId}/suspend** and **POST .../resume** resolve company by tenant and set status.

---

## 3. Usage tracking

- **Metered metrics:** Stored in **TenantUsageRecord** (monthly buckets): TotalUsers, ActiveUsers, OrdersCreated, BackgroundJobsExecuted, ReportExports, **ApiCalls**, **StorageBytes**.
- API calls are incremented automatically by **TenantUsageRecordingMiddleware** per request (tenant-scoped only).
- **StorageBytes:** Updated by **FileService** on upload (increment) and delete (decrement) via **ITenantUsageService.RecordStorageDeltaAsync**; tenant-scoped, no double-counting across tenants.
- **GET /api/platform/usage/tenants/{tenantId}** and **.../by-month?year=&month=** return usage for platform admin.
- Tenant-facing usage (if exposed) via **ITenantUsageQueryService** and **TenantUsageController** (current tenant).

---

## 4. Admin operations

- **List tenants:** **GET /api/platform/tenants?search=&skip=0&take=50**
- **Tenant diagnostics:** **GET /api/platform/tenants/{tenantId}/diagnostics** (user count, order count, subscription status).
- **Usage reports:** Use platform usage endpoints above; aggregate from **TenantMetricsDaily** / **TenantMetricsMonthly** for historical reports.

---

## 5. Support tooling

- **Suspend tenant:** POST /api/platform/tenants/{tenantId}/suspend (sets company status to Suspended; subscription middleware will deny access).
- **Resume tenant:** POST /api/platform/tenants/{tenantId}/resume.
- **Diagnostics:** Use GET .../diagnostics to see user/order counts and subscription state before making changes.
- **Logs:** Structured logs include **TenantId** and **CompanyId**; filter by tenant in your log aggregation.

---

## 6. Rate limits and quotas

- **Tenant rate limit:** See [TENANT_RATE_LIMITING.md](TENANT_RATE_LIMITING.md). **TenantRateLimitOptions** (SaaS:TenantRateLimit): Enabled, RequestsPerMinute (default 100), RequestsPerHour (default 1000), optional plan overrides (Trial/Standard/Enterprise). Returns 429 when exceeded; log **TenantRateLimitExceeded** for metrics.
- **Storage quota:** **FileService** enforces **ISubscriptionEnforcementService.IsWithinStorageLimitAsync** before upload and records usage (RecordStorageDeltaAsync) on upload/delete. When the tenant would exceed StorageLimitBytes, upload returns **403 Forbidden** with message "Storage quota exceeded. Cannot upload file."
- **Seat limit:** Check **ISubscriptionEnforcementService.IsWithinSeatLimitAsync** before creating/inviting users.

---

## 7. Metrics aggregation

- **TenantMetricsAggregationHostedService** runs every 24 hours: aggregates previous day into **TenantMetricsDaily** and, on the 1st, previous month into **TenantMetricsMonthly**.
- Data source: **TenantUsageRecord** (same metric keys). Use these tables for billing and historical dashboards.

---

## 8. Security

- All platform admin endpoints require **SuperAdmin** and the appropriate permission (AdminTenantsView, AdminTenantsEdit).
- Tenant provisioning and status changes must never be exposed to non–platform users.
- Keep **POST /api/platform/tenants/provision** and list/diagnostics/suspend/resume behind strong auth and audit.

---

## 9. Operations hardening

See **[SAAS_OPERATIONS_HARDENING_REPORT.md](SAAS_OPERATIONS_HARDENING_REPORT.md)** for the hardening phase: default trial plan seed, subscription GET/PATCH, storage tracking and quota enforcement, and operational caveats.

---

## 10. Scale & reliability (SaaS scaling)

- **Indexes:** [TENANT_INDEX_AUDIT.md](TENANT_INDEX_AUDIT.md) — Ensure tenant-scoped queries use indexes (Orders, JobExecutions, Files, Users, etc.).
- **Job fairness & resilience:** [JOB_ISOLATION.md](JOB_ISOLATION.md), [PLATFORM_RESILIENCE.md](PLATFORM_RESILIENCE.md) — Tenant job fairness, stuck job watchdog, retry limits.
- **Tenant health:** **GET /api/platform/analytics/tenant-health** — [TENANT_OBSERVABILITY.md](TENANT_OBSERVABILITY.md).
- **Storage lifecycle:** [STORAGE_LIFECYCLE.md](STORAGE_LIFECYCLE.md) — File tiers and **StorageLifecycleService**.
- **Load testing:** [LOAD_TEST_PLAN.md](LOAD_TEST_PLAN.md), **tools/load_testing/seed_test_tenants.ps1**.
- **Readiness summary:** [SAAS_SCALE_READINESS_REPORT.md](SAAS_SCALE_READINESS_REPORT.md).
