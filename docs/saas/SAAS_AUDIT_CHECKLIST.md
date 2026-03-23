# SaaS Audit Checklist

**Date:** 2026-03-13  
**Purpose:** Checklist for verifying tenant safety when adding or changing code. Use in PR reviews and before releasing tenant-sensitive features.

---

## 1. Tenant Scope and Bypass

- [ ] **TenantScope** is set (or platform bypass used) **before** any path that can call **SaveChangesAsync** on tenant-scoped entities.
- [ ] Any block that sets **TenantScope.CurrentTenantId** or calls **EnterPlatformBypass()** restores in a **finally** block (or uses **TenantScopeExecutor**); exception: design-time factory.
- [ ] No new **blanket bypass**; any new **EnterPlatformBypass** is justified and documented in [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md) or architecture doc.
- [ ] **Hosted services, schedulers, dispatchers, replay, webhooks, workers:** Use **TenantScopeExecutor** only (RunWithTenantScopeAsync, RunWithPlatformBypassAsync, RunWithTenantScopeOrBypassAsync); no manual scope/bypass.

---

## 2. Null-Company and Fail-Closed

- [ ] **Tenant-owned** operations (enqueue job, create notification, create dispatch) when company is null: **skip or early-return with log**, or throw/403; do **not** create tenant-scoped entities or treat null/empty as "all tenants."
- [ ] **API:** Tenant-required routes are blocked by TenantGuardMiddleware when tenant cannot be resolved (403).

---

## 3. Queries and FindAsync

- [ ] **No FindAsync** on tenant-scoped entities without an explicit **CompanyId** check (FindAsync bypasses global query filters). Use tenant-scoped query (e.g. FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId)) or FindAsync + immediate CompanyId check and abort if mismatch.
- [ ] **IgnoreQueryFilters:** Every use on tenant-scoped data is constrained by **CompanyId** (or equivalent) so wrong-tenant data is never returned. Prefer **TenantSafetyGuard.AssertTenantContext()** or run inside a documented platform bypass.

---

## 4. New Tenant-Scoped Entities

- [ ] New tenant-owned entities inherit from **CompanyScopedEntity** (or have **CompanyId** and are registered in **TenantSafetyGuard.IsTenantScopedEntityType** and in EF global query filters).
- [ ] SaveChanges will then enforce tenant context and tenant-integrity (entity CompanyId == CurrentTenantId) for those types when not in platform bypass.

---

## 5. Authorization

- [ ] **Platform-only** actions (e.g. list tenants, create tenant, platform health) are restricted to platform admin and use explicit platform bypass where needed; normal API remains tenant-scoped.
- [ ] **Tenant-scoped** endpoints use **RequireCompanyId** or equivalent so that missing tenant → 403.

---

## 6. Tests

- [ ] New or modified **tenant-scoped** paths have regression tests (scope set, restore in finally, null-company behaviour).
- [ ] Tests that insert tenant-scoped entities set **TenantScope** (or bypass) in setup so SaveChanges does not throw.

---

## 7. Documentation

- [ ] New bypass or new tenant-scoped area is documented in [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md) or [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md) and linked from [backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md) if appropriate.

---

*See: backend [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md), [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md).*
