# SaaS Regression Guardrails

**Purpose:** Permanent regression protection so future development does not reintroduce single-company assumptions or tenant-safety regressions. CephasOps is **FULLY SAAS HARDENED**; these invariants and rules must be preserved.

**See also:** [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md), [SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../../backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md), [SAAS_CODE_REVIEW_CHECKLIST.md](../remediation/SAAS_CODE_REVIEW_CHECKLIST.md), [SAAS_TEST_COVERAGE_INDEX.md](../remediation/SAAS_TEST_COVERAGE_INDEX.md).

---

## 1. Tenant isolation invariants

- **Tenant-scoped reads/writes** occur only when `TenantScope.CurrentTenantId` is set to a valid company ID, or under an explicit, documented platform bypass.
- **Tenant context** is set by API middleware from `ITenantProvider`, by job runner from `job.CompanyId`, by event dispatcher/replay from event `CompanyId`, and by webhook runtime from request `CompanyId`.
- **Missing tenant context** in tenant-owned paths must **fail closed**: throw (e.g. `InvalidOperationException` or `TenantSafetyGuard.AssertTenantContext()`), never treat as “all companies.”
- **Runtime code** must use `TenantScopeExecutor` for setting scope or bypass; manual `TenantScope.CurrentTenantId` / `EnterPlatformBypass()` / `ExitPlatformBypass()` are allowed only in DatabaseSeeder and ApplicationDbContextFactory.

---

## 2. Financial isolation invariants

- **Finance-sensitive paths** (billing, payments, payouts, ledger) must use `FinancialIsolationGuard` (e.g. `RequireTenantOrBypass`, `RequireCompany`, `RequireSameCompany`) where applicable.
- **Cross-tenant financial reads/writes** are forbidden unless under a documented platform bypass (e.g. platform-only reporting).
- **Invoice/payment ownership** must be validated against current tenant; returning another tenant’s data (e.g. invoice company ≠ current tenant) must not leak and must fail closed where appropriate.

---

## 3. EventStore consistency invariants

- **Append path** must satisfy `EventStoreConsistencyGuard` (e.g. `RequireTenantOrBypassForAppend`, `RequireCompanyWhenEntityContext`, parent/root/stream consistency).
- **Dispatch and replay** must run each event under the correct tenant scope or explicit platform bypass via `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...)`.
- **Event metadata** (CompanyId, EventType, etc.) must be consistent with entity context and stream; duplicate append must be rejected.

---

## 4. Background job tenant rules

- **Tenant-owned jobs** must run under `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...)` with `companyId` from the job payload (e.g. `job.CompanyId`).
- **Platform-wide jobs** (e.g. retention, reap) must use `TenantScopeExecutor.RunWithPlatformBypassAsync(...)` and be documented.
- **Nullable companyId** may use `RunWithTenantScopeOrBypassAsync` only when explicitly justified; never default to “all companies.”
- **Job executors** that are tenant-scoped must throw when `companyId` is null or `Guid.Empty` instead of proceeding.

---

## 5. Reports / export tenant rules

- **Exports and reports** that are tenant-scoped must resolve company from authenticated context or job payload and filter all data by that company.
- **Export jobs** (e.g. inventory report export) must require a valid tenant context and throw when missing; no silent fallback to “all companies.”
- **Platform-wide reports** (e.g. operations dashboard) must be behind role/permission checks (e.g. SuperAdmin, AdminTenantsView) and use platform bypass only for aggregation, not for exposing raw cross-tenant data to tenant users.

---

## 6. SI-app tenant rules

- **SI-app** runs in request context; tenant is set by API middleware before controller execution.
- **Order and material access** is constrained by EF global query filter (`CompanyId == TenantScope.CurrentTenantId`) and by SI assignment (order must be assigned to current SI).
- **Defense-in-depth:** Services called from SI-app that perform tenant-scoped persistence should assert tenant context where appropriate (e.g. `TenantSafetyGuard.AssertTenantContext()`).

---

## 7. Forbidden patterns

Do **not** introduce or restore the following:

| Pattern | Why forbidden |
|--------|----------------|
| **`Guid.Empty` meaning “all companies”** | Treating empty as “all tenants” bypasses isolation; use explicit platform bypass only where documented. |
| **`companyId ?? Guid.Empty` in tenant-scoped paths** | Silently defaults to empty and can be interpreted as “all”; must throw or use explicit bypass instead. |
| **Missing tenant context fallback** | Tenant-owned code must not silently proceed when company is null/empty; fail closed. |
| **“Single-company mode” assumptions in active code** | No branching on “single company” for tenant filtering or scope; multi-tenant is the only mode. |
| **Unsafe `IgnoreQueryFilters`** | Use only where explicitly justified and documented; must not expose cross-tenant data to tenant callers. |
| **Background jobs without tenant scope enforcement** | Jobs must run under `TenantScopeExecutor` with correct scope or documented platform bypass. |
| **Manual scope handling in runtime services** | Use `TenantScopeExecutor`; no manual `TenantScope.CurrentTenantId` / `EnterPlatformBypass` in services. |
| **New platform bypass without documentation** | Every bypass must be justified and listed in architecture/operations docs. |

---

## 8. Required tests for new tenant-scoped features

When adding or changing tenant-scoped behaviour:

1. **Same-tenant:** Behaviour is correct when a valid `CompanyId` is set.
2. **Cross-tenant:** Access or mutation with a different company fails (no cross-tenant read/write).
3. **Missing tenant:** Null or `Guid.Empty` company in tenant-owned path throws (no silent fallback).

Prefer existing patterns: `TenantFallbackRemovalTests`, `TenantIsolationIntegrationTests`, `BillingServiceFinancialIsolationTests`, `EventStoreConsistencyGuardTests`, `SiAppTenantIsolationTests`. See [SAAS_TEST_COVERAGE_INDEX.md](../remediation/SAAS_TEST_COVERAGE_INDEX.md) for the full index.
