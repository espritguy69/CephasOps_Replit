# Data Isolation Rules

**Date:** 2026-03-13  
**Purpose:** Define tenant-scoped, platform-scoped, and shared reference data; list required safeguards; state that cross-tenant access must fail safely.

---

## 1. Tenant-Scoped Entities

Data that **belongs to one tenant (company)**. All such entities have **CompanyId** (or are owned by an entity that does) and are subject to:

- EF Core **global query filters** (filter by TenantScope.CurrentTenantId).
- **TenantSafetyGuard** on SaveChanges (tenant context or platform bypass required; tenant-integrity check: entity CompanyId must match CurrentTenantId when context is set).
- **No** use of FindAsync without an explicit CompanyId check (FindAsync bypasses query filters); use tenant-scoped queries (id + CompanyId) instead.

Examples: Order, Invoice, Material, User, Department, Partner, BackgroundJob, JobExecution, Notification, ParseSession, File, and all entities inheriting **CompanyScopedEntity**.

---

## 2. Platform-Scoped Entities

Data that is **not** tied to a single tenant. No CompanyId filter; access only in **platform bypass** or in controlled platform-only code paths.

Examples: **Tenant**, **TenantSubscription**, **TenantFeatureFlags**, **TenantOnboardingProgress**, **WorkerInstance** (job coordinator). Platform health and control-plane data (e.g. list of tenants, job backlog counts across tenants) are also platform-scoped.

---

## 3. Shared Reference Entities

Read-only or system reference data with **no tenant key**. Not filtered by CompanyId; same data visible to all tenants.

Examples: country list, system enums, verticals (if shared). Use only where there is no tenant-specific variant.

---

## 4. Required Safeguards

| Safeguard | Purpose |
|-----------|---------|
| **TenantScope** (AsyncLocal) | Holds current tenant (CompanyId) for the async context. Set by API middleware or TenantScopeExecutor; read by EF filters and TenantSafetyGuard. |
| **TenantGuardMiddleware** | Blocks tenant-required routes when effective company cannot be resolved; returns 403. |
| **ITenantProvider** | Resolves effective CompanyId per request (X-Company-Id, JWT, department fallback). Single source of truth for request-time tenant. |
| **EF global query filters** | Applied to CompanyScopedEntity and other tenant-scoped types; filter by TenantScope.CurrentTenantId so queries only return current tenant's rows. |
| **SaveChanges tenant validation** | TenantSafetyGuard in ApplicationDbContext.SaveChangesAsync: require tenant context (or platform bypass) when saving tenant-scoped entities; enforce entity CompanyId == CurrentTenantId for Modified/Deleted and for Added when CompanyId is set. |
| **Service-level tenant checks** | Controllers and application services use RequireCompanyId or explicit companyId parameter; avoid passing null/empty as "all tenants." |
| **Explicit platform bypasses** | Only in documented locations (seeding, design-time factory, retention, scheduler enumeration, provisioning, webhook/event with no company). Use TenantScopeExecutor.RunWithPlatformBypassAsync; no "convenience" bypass. |

---

## 5. Cross-Tenant Access Must Fail Safely

- **Intent:** No path may return or modify another tenant's data unless it is an explicit, authorised platform operation (e.g. platform admin listing tenants).
- **Means:** Tenant resolution before any tenant-scoped read/write; RequireCompanyId on tenant-required APIs; no "single company mode" (null/empty companyId meaning "all"); tenant-scoped queries (id + CompanyId) instead of FindAsync for tenant-scoped entities; SaveChanges tenant-integrity check.
- **Failure mode:** If tenant cannot be resolved when required → 403. If entity is not in current tenant → 404 or 403. If SaveChanges detects CompanyId mismatch → throw and do not persist.

---

*See: [TENANCY_MODEL.md](TENANCY_MODEL.md), [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md), backend [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md), [EF_TENANT_SCOPE_SAFETY.md](../../backend/docs/architecture/EF_TENANT_SCOPE_SAFETY.md).*
