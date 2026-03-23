# TenantScopeExecutor — Completion Note

**Status:** Rollout and tenant-safety hardening complete.  
**Date:** 2026-03-13  
**Scope:** Documentation only; no production code changes.

This note records the **finished state** of tenant-safety execution patterns and the remaining intentional exceptions after the TenantScopeExecutor rollout and API hardening.

---

## 1. Final execution standard

| Intent | Method | When to use |
|--------|--------|-------------|
| **Tenant-owned work** | `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, work, ct)` | When the company is known and non-empty. Fails fast on `Guid.Empty`. |
| **Platform-owned work** | `TenantScopeExecutor.RunWithPlatformBypassAsync(work, ct)` | Only for intentional platform-wide operations (e.g. retention, reap, scheduler enumeration, provisioning). |
| **Nullable-company path** | `TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyIdOrNullForPlatformBypass, work, ct)` | **Only where explicitly justified:** e.g. event dispatch, webhook request, retention with optional company filter, job execution where `job.CompanyId` may be null. When company has value and is non-empty → tenant scope; when null or empty → platform bypass. Do not use for tenant-owned work with a known company; use `RunWithTenantScopeAsync` so invalid input fails fast. |

---

## 2. Remaining intentional exceptions

Manual `TenantSafetyGuard.EnterPlatformBypass()` / `ExitPlatformBypass()` (or Enter-only) remain in these places **by design**; they are not normal operational services and are documented exceptions:

| Location | Reason |
|----------|--------|
| **DatabaseSeeder** | Seeding runs once at app/setup time; Enter before seeding, Exit in finally. Process-bound; executor could be used here in future if desired. |
| **ApplicationDbContextFactory.CreateDbContext** | Design-time EF Core factory (e.g. migrations, tooling). Enter in CreateDbContext; no Exit (process exits). |

No other **runtime** operational paths should use manual Enter/Exit for scope or bypass; they have been rolled out to TenantScopeExecutor.

---

## 3. Rollout statement

**Manual runtime `EnterPlatformBypass` / `ExitPlatformBypass` usage has been rolled out to the executor for normal operational services.** Hosted services, schedulers, event dispatchers, replay flows, webhook runtimes, job workers, retention services, and provisioning that previously used manual try/finally with Enter/Exit or manual TenantScope restore now use `TenantScopeExecutor` (RunWithTenantScopeAsync, RunWithPlatformBypassAsync, or RunWithTenantScopeOrBypassAsync as appropriate). Behavior is unchanged; only the execution pattern is centralized and auditable.

---

## 4. Default for new code

**New hosted services, schedulers, dispatchers, replay flows, webhook runtimes, and workers should use TenantScopeExecutor by default.** Prefer `RunWithTenantScopeAsync` when the company is known; `RunWithPlatformBypassAsync` when the operation is intentionally platform-wide; and `RunWithTenantScopeOrBypassAsync` only when the flow is explicitly designed to accept null/empty company (e.g. event entry, webhook, optional retention filter). Do not introduce new manual Enter/Exit or manual TenantScope set/restore for operational code.

---

## 5. Cross-references

| Document | Purpose |
|----------|---------|
| [TENANT_SAFETY_DEVELOPER_GUIDE.md](TENANT_SAFETY_DEVELOPER_GUIDE.md) | Primary entry point: when to set scope, bypass rules, try/finally, PR checklist, test run, and section 3.1 (TenantScopeExecutor as preferred pattern). |
| [TENANT_SCOPE_EXECUTOR_VALIDATION_REPORT.md](TENANT_SCOPE_EXECUTOR_VALIDATION_REPORT.md) | Validation report for the initial executor introduction: root problem, design, abstraction, call sites, tests, rollout recommendations. |
| [TENANT_SAFETY_FINAL_VERIFICATION.md](../operations/TENANT_SAFETY_FINAL_VERIFICATION.md) | Final verification of the full tenant safety model (resolution, TenantScope, TenantSafetyGuard, bypass, jobs, documentation). |

---

*No production code was changed in this completion note; documentation only.*
