# SaaS Scaling Architecture

**Date:** 2026-03-13

This document describes the CephasOps SaaS scaling architecture: tenant provisioning, subscriptions, usage tracking, platform admin, metrics, and operational safeguards.

---

## 1. Overview

CephasOps operates as a multi-tenant SaaS platform with:

- **Tenant** → **Company** (1:1 for current model) → Departments, Users, Orders, etc.
- **TenantSubscription** links a tenant to a **BillingPlan** with trial, limits, and billing cycle.
- **TenantUsageRecord** stores metered usage per tenant per month (users, jobs, storage, API calls).
- **TenantMetricsDaily** and **TenantMetricsMonthly** store aggregated metrics for reporting and billing.
- **Platform admin** APIs (SuperAdmin only) for tenant list, diagnostics, subscription management, usage reports, suspend/resume.

---

## 2. Tenant provisioning

- **Endpoint:** `POST /api/platform/tenants/provision`
- **Service:** `ICompanyProvisioningService` / `CompanyProvisioningService`
- **Creates in one transaction:**
  - **Tenant** (Name, Slug, IsActive)
  - **Company** (LegalName, Code, TenantId, Status, DefaultTimezone, etc.)
  - **Default departments** (Operations, Finance, Inventory, Scheduler, Admin)
  - **Tenant admin user** (with Admin role and department memberships)
  - **TenantSubscription** (trial or specified plan; TrialEndsAtUtc, BillingCycle, NextBillingDateUtc)
- **Uniqueness:** Company code, tenant slug, and admin email are validated; duplicate throws 409.
- **Trial:** If no `PlanSlug` is provided, a default plan with slug **"trial"** or first active plan is used with `TrialEndsAtUtc` (default 14 days, configurable via `TrialDays`). A **default trial BillingPlan** (slug `trial`) is ensured by **DatabaseSeeder.EnsureDefaultTrialBillingPlanAsync()** at seed time so provisioning without explicit PlanSlug always resolves.

See [TENANT_PROVISIONING_FLOW.md](TENANT_PROVISIONING_FLOW.md) for the detailed flow.

---

## 3. Subscription system

### 3.1 TenantSubscription entity

- **TenantId**, **BillingPlanId**, **Status** (Active, Cancelled, PastDue, Trialing)
- **StartedAtUtc**, **CurrentPeriodEndUtc**, **TrialEndsAtUtc**
- **BillingCycle** (Monthly, Yearly)
- **SeatLimit** (max users), **StorageLimitBytes** (max storage)
- **NextBillingDateUtc**, **ExternalSubscriptionId**

### 3.2 Enforcement

- **SubscriptionAccessService** (existing): Evaluates company status and subscription status; denies access when tenant suspended, subscription cancelled, past due, or **trial expired** (TrialEndsAtUtc &lt; now).
- **SubscriptionEnforcementMiddleware:** Runs after TenantGuard; calls `GetAccessForCompanyAsync`; returns 403 with denial reason when not allowed.
- **ISubscriptionEnforcementService:** Optional stricter checks (seat limit, storage limit) for specific operations.

### 3.3 Subscription middleware

- **SubscriptionEnforcementMiddleware** uses `ISubscriptionAccessService.GetAccessForCompanyAsync(companyId)`.
- Skips auth paths (login, refresh, forgot-password, change-password-required, reset-password-with-token) and Testing environment.

---

## 4. Usage tracking

- **ITenantUsageService** / **TenantUsageService:** Records usage to **TenantUsageRecord** (per tenant, per metric, per month).
- **Metric keys:** OrdersCreated, InvoicesGenerated, BackgroundJobsExecuted, ReportExports, TotalUsers, ActiveUsers, **ApiCalls**, **StorageBytes**.
- **TenantUsageRecordingMiddleware:** After each request, increments ApiCalls for the current tenant (when tenant is set).
- **StorageBytes:** Updated by **FileService** on file upload (RecordStorageDeltaAsync +bytes) and delete (-bytes); enforcement via **ISubscriptionEnforcementService.IsWithinStorageLimitAsync** before upload (returns 403 when quota exceeded).
- **ITenantUsageQueryService:** Returns current month or date-range usage for platform admin and tenant-facing dashboards.

---

## 5. Platform admin console

All endpoints require **SuperAdmin** and appropriate permission.

| Endpoint | Description |
|----------|-------------|
| `GET /api/platform/tenants` | List tenants with company and subscription summary (search, skip, take) |
| `GET /api/platform/tenants/{tenantId}/diagnostics` | User count, order count, subscription status, trial/billing dates |
| `GET /api/platform/tenants/{tenantId}/subscription` | Get current tenant subscription (active/trialing or latest) |
| `PATCH /api/platform/tenants/{tenantId}/subscription` | Update subscription (plan, status, trial end, limits) |
| `POST /api/platform/tenants/provision` | Provision new tenant |
| `PATCH /api/platform/tenants/companies/{companyId}/status` | Set company status (Active, Suspended, etc.) |
| `POST /api/platform/tenants/{tenantId}/suspend` | Set company status to Suspended |
| `POST /api/platform/tenants/{tenantId}/resume` | Set company status to Active |
| `GET /api/platform/usage/tenants/{tenantId}` | Current month usage |
| `GET /api/platform/usage/tenants/{tenantId}/by-month?year=&month=` | Usage for a given month |

Subscription management (list, get active, subscribe, cancel) is available via **ITenantSubscriptionService**; platform can expose these as needed.

---

## 6. Tenant metrics (daily / monthly)

- **TenantMetricsDaily:** One row per tenant per date (DateUtc, ActiveUsers, TotalUsers, OrdersCreated, BackgroundJobsExecuted, StorageBytes, ApiCalls).
- **TenantMetricsMonthly:** One row per tenant per year/month with same dimensions.
- **TenantMetricsAggregationJob:** Aggregates from **TenantUsageRecord** into daily and monthly tables.
- **TenantMetricsAggregationHostedService:** Runs daily (aggregates previous day and, on the 1st, previous month).

---

## 7. Observability

- **RequestLogContextMiddleware:** Pushes **CompanyId** and **TenantId** (same as company for request scope) into Serilog **LogContext** so all structured logs include tenant.
- Use `LogContext.PushProperty("TenantId", tenantId)` in jobs/handlers when running under tenant scope.
- **Tenant health:** **GET /api/platform/analytics/tenant-health** returns per-tenant health (API requests, job failures, storage, HealthStatus). See [TENANT_OBSERVABILITY.md](TENANT_OBSERVABILITY.md).
- **Platform Guardian:** Query safety audit, tenant anomaly detection, config drift, performance watchdog, security surface audit, and aggregated **GET /api/platform/analytics/platform-health**. See [Platform Guardian](../platform_guardian/README.md) and [PLATFORM_GUARDIAN_SUMMARY.md](../platform_guardian/PLATFORM_GUARDIAN_SUMMARY.md).
- **Production infrastructure:** Deployment topology, worker separation, cache strategy, observability, environment config, database ops, CI/CD, runbooks, staged rollout. See [Production Infrastructure](../production_architecture/PRODUCTION_INFRASTRUCTURE_SUMMARY.md).

---

## 8. Operational safeguards (scale & reliability)

- **Tenant rate limiting:** [TENANT_RATE_LIMITING.md](TENANT_RATE_LIMITING.md) — **TenantRateLimitMiddleware**, per-tenant limits (default 100/min, 1000/hour), plan overrides, 429 and TenantRateLimitExceeded logging.
- **Job isolation:** [JOB_ISOLATION.md](JOB_ISOLATION.md) — Tenant fairness (max jobs per tenant per cycle), **JobExecutionWorkerOptions** (MaxJobsPerTenantPerCycle, TenantJobFairnessEnabled), execution logging (TenantId, JobType, ExecutionTimeMs).
- **Platform resilience:** [PLATFORM_RESILIENCE.md](PLATFORM_RESILIENCE.md) — Retry limits (MaxAttempts), stuck job recovery (**JobExecutionWatchdogService**, **ResetStuckRunningAsync**), tenant isolation of failures.
- **Storage lifecycle:** [STORAGE_LIFECYCLE.md](STORAGE_LIFECYCLE.md) — **File** LastAccessedAtUtc / StorageTier (Hot/Warm/Cold/Archive), **StorageLifecycleService**.
- **Tenant query indexes:** [TENANT_INDEX_AUDIT.md](TENANT_INDEX_AUDIT.md) — Indexes for (CompanyId), (CompanyId, CreatedAt), (CompanyId, Status), (CompanyId, UserId) on Orders, JobExecutions, Files, Users, etc.
- **Load testing:** [LOAD_TEST_PLAN.md](LOAD_TEST_PLAN.md) and **tools/load_testing/seed_test_tenants.ps1** for seeding 50 tenants and running concurrent-tenant, job-spike, file-upload, and reporting scenarios.
- **Storage quota:** Enforce via **ISubscriptionEnforcementService.IsWithinStorageLimitAsync** before large uploads; persist StorageBytes in TenantUsageRecord. **StorageLifecycleService** tiers files (Hot/Warm/Cold/Archive) for lifecycle and retention.
- **Seat limit:** Enforce via **ISubscriptionEnforcementService.IsWithinSeatLimitAsync** before inviting/creating users.

---

## 9. Database

- **TenantSubscriptions:** New columns TrialEndsAtUtc, BillingCycle, SeatLimit, StorageLimitBytes, NextBillingDateUtc (migration `SaasScalingSubscriptionAndMetrics`).
- **TenantMetricsDaily**, **TenantMetricsMonthly:** New tables (same migration).

---

## 10. Dependencies

- Existing: TenantScopeExecutor, TenantSafetyGuard, ITenantProvider, TenantGuardMiddleware.
- New/updated: IPlatformAdminService, TenantMetricsAggregationJob, ISubscriptionEnforcementService, TenantUsageRecordingMiddleware, TenantRateLimitMiddleware.
