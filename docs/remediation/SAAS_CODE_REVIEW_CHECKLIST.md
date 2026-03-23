# SaaS Code Review Checklist

**Use this checklist** when reviewing PRs that touch tenant-scoped services, persistence, background jobs, events, exports, or the SI-app. Answer yes/no; any “no” or “unsure” should be resolved before merge.

---

## Context and scope

| # | Question | Y / N |
|---|----------|-------|
| 1 | Does this feature require tenant context (company-scoped data or actions)? | |
| 2 | If no tenant context is required, is the execution clearly platform-wide and documented? | |

---

## Reads and writes

| # | Question | Y / N |
|---|----------|-------|
| 3 | Are all tenant-scoped reads filtered by `CompanyId` (or equivalent) / global query filter? | |
| 4 | Can missing tenant context **fail closed** (throw) instead of defaulting to “all companies”? | |
| 5 | Can cross-tenant reads or writes occur in any path introduced or changed? | |
| 6 | Is `IgnoreQueryFilters` used only where justified and documented, with no cross-tenant exposure to tenant callers? | |

---

## Background jobs and execution

| # | Question | Y / N |
|---|----------|-------|
| 7 | Are background jobs executed with **tenant scope** (`RunWithTenantScopeAsync`) or **explicit platform bypass** (`RunWithPlatformBypassAsync`), never with unset or implicit “all companies”? | |
| 8 | Do new job executors throw when `companyId` is null or `Guid.Empty` for tenant-owned jobs? | |

---

## Exports and reports

| # | Question | Y / N |
|---|----------|-------|
| 9 | Are exports/reports tenant-safe (scoped to one company or behind platform-only access)? | |
| 10 | Do platform-wide report endpoints enforce role/permission (e.g. SuperAdmin, AdminTenantsView)? | |

---

## EventStore and events

| # | Question | Y / N |
|---|----------|-------|
| 11 | Are event append paths using `EventStoreConsistencyGuard` where applicable? | |
| 12 | Are dispatch/replay paths using `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`? | |

---

## Financial and SI-app

| # | Question | Y / N |
|---|----------|-------|
| 13 | Are finance-sensitive paths using `FinancialIsolationGuard` (RequireTenantOrBypass / RequireCompany / RequireSameCompany) where applicable? | |
| 14 | If the change touches the SI-app: is Order/material access constrained by tenant and SI assignment? | |

---

## Tests

| # | Question | Y / N |
|---|----------|-------|
| 15 | Were **same-tenant** tests added or updated for the new/affected behaviour? | |
| 16 | Were **cross-tenant** (or isolation) tests added or updated where relevant? | |
| 17 | Were **missing-tenant** (fail-closed) tests added or updated for tenant-owned paths? | |

---

**Reference:** [SAAS_REGRESSION_GUARDRAILS.md](../operations/SAAS_REGRESSION_GUARDRAILS.md), [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md).
