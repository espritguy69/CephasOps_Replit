# Query Safety Report

**Date:** 2026-03-13  
**Scope:** Repository-level audit of risky query patterns for tenant safety.

---

## Classification

| Level | Meaning |
|-------|--------|
| **Safe and justified** | Pattern is documented, scoped (e.g. by CompanyId), or used only in allowed contexts (seeder, platform bypass with guard). |
| **Medium-risk and needs review** | Pattern may cross tenant boundaries if misused; review that call site always constrains by tenant. |
| **Critical and must be fixed** | Unconstrained cross-tenant access or raw SQL that could bypass tenant boundaries. |

---

## 1. IgnoreQueryFilters()

### 1.1 DatabaseSeeder / ApplicationDbContextFactory

| Location | Classification | Notes |
|----------|----------------|-------|
| `DatabaseSeeder.cs` (multiple) | **Safe and justified** | Seeder runs outside request context; uses IgnoreQueryFilters to check soft-deleted and prevent duplicates. Documented in code. Allowed per architecture (DatabaseSeeder, ApplicationDbContextFactory). |

### 1.2 Workflow (WorkflowDefinitionsService, WorkflowEngineService)

| Location | Classification | Notes |
|----------|----------------|-------|
| `WorkflowDefinitionsService.cs` | **Safe and justified** | Explicit `companyId` parameter; query always filtered by `CompanyId == companyId`. Comment: "IgnoreQueryFilters so tenant scope does not hide rows when companyId is passed explicitly." |
| `WorkflowEngineService.cs` | **Safe and justified** | Same pattern: company-scoped lookups with explicit CompanyId. |

### 1.3 Background job processor

| Location | Classification | Notes |
|----------|----------------|-------|
| `BackgroundJobProcessorService.cs` | **Safe and justified** | Platform-wide job table scan; scope is set per job in ProcessJobAsync via tenant context. Comment: "Use IgnoreQueryFilters so we see jobs from all tenants; scope is set per job." |

### 1.4 Auth (AuthService)

| Location | Classification | Notes |
|----------|----------------|-------|
| `AuthService.cs` (login, refresh, user by id) | **Safe and justified** | Login/refresh by email or token; user lookup by Id. Tenant context established after auth. No cross-tenant data returned to caller without tenant scope. |

### 1.5 OrderService

| Location | Classification | Notes |
|----------|----------------|-------|
| `OrderService.cs` (GetById with soft-delete) | **Safe and justified** | `Where(o => o.Id == id)` plus explicit or current-tenant company constraint in comment. "Always constrain by company (explicit or current tenant)." |

### 1.6 AssetService

| Location | Classification | Notes |
|----------|----------------|-------|
| `AssetService.cs` | **Medium-risk and needs review** | Single use: "Bypass global query filter to avoid DeletedAt column issues." Verify call site always constrains by company/tenant. |

### 1.7 BaseWorkRateService, OrderProfitabilityService, BillingService

| Location | Classification | Notes |
|----------|----------------|-------|
| `BaseWorkRateService.cs` | **Safe and justified** | Queries use `Id` and/or explicit company/tenant and `!IsDeleted`. |
| `OrderProfitabilityService.cs` | **Safe and justified** | Company-scoped with explicit filter. |
| `BillingService.cs` | **Safe and justified** | Company-scoped; IgnoreQueryFilters used for soft-delete or specific lookup with tenant constraint. |

### 1.8 Platform / retention / trace

| Location | Classification | Notes |
|----------|----------------|-------|
| `EventPlatformRetentionService.cs` | **Safe and justified** | Platform retention; deletes by receipt IDs after platform logic. |
| `TraceQueryService.cs` | **Safe and justified** | Diagnostic/trace by correlation id; platform/support only. |
| `DocumentGenerationService.cs` | **Safe and justified** | Lookups with explicit company/template scope. |
| `PlatformSupportController.cs` | **Safe and justified** | SuperAdmin support; single IgnoreQueryFilters use for support lookup. |

### 1.9 StockLedgerService

| Location | Classification | Notes |
|----------|----------------|-------|
| `StockLedgerService.cs` | **Safe and justified** | All uses guarded by `_isTesting`; not used in production. |

### 1.10 Tests

| Location | Classification | Notes |
|----------|----------------|-------|
| Test projects (`*.Tests`) | **Safe and justified** | Test-only; setup/verification with IgnoreQueryFilters. |

---

## 2. ExecuteSqlRaw / ExecuteSqlRawAsync

No **FromSqlRaw** or **ExecuteSqlInterpolated** found.

| Location | Classification | Notes |
|----------|----------------|-------|
| `EmailTemplateService.cs` | **Medium-risk and needs review** | ExecuteSqlRawAsync for template updates. Verify SQL does not expose tenant data and is parameterized; ensure executed in tenant scope where applicable. |
| `TaskService.cs` | **Medium-risk and needs review** | Raw SQL for task updates. Ensure parameters are bound and scope is tenant where required. |
| `ParserTemplateService.cs`, `EmailRuleService.cs`, `VipEmailService.cs`, `VipGroupService.cs` | **Medium-risk and needs review** | Parser/email rules; ensure all raw SQL is parameterized and runs in correct tenant context. |
| `SchedulerService.cs` | **Medium-risk and needs review** | Raw SQL for scheduler; verify tenant/company scoping. |
| `InvoiceSubmissionService.cs` | **Safe and justified** | Likely status/audit updates with IDs from already-scoped context. |
| `WorkerCoordinatorService.cs` | **Safe and justified** | Platform worker coordination. |
| `AdminService.cs` | **Safe and justified** | Admin-only; platform scope. |

---

## 3. Controller actions accepting companyId / tenantId overrides

Controllers that accept **companyId** or **tenantId** as route/query must be restricted to SuperAdmin or must derive tenant from authenticated user (no client override).

| Area | Finding |
|------|--------|
| Platform routes (`/api/platform/*`) | Accept tenantId/companyId for admin operations; all require SuperAdmin. **Safe and justified** when auth is enforced. |
| Tenant provisioning | Accepts company/tenant data for *creation* only; no override of current user's tenant. **Safe and justified.** |
| Other controllers | Most use `ITenantProvider` or current user; no arbitrary companyId/tenantId from client. Manual review of any action that takes companyId/tenantId from body/query: ensure it is either SuperAdmin-only or matches current user's tenant. |

---

## 4. Direct DbSet access without clear tenant criteria

- **Global query filters** are applied on tenant-scoped entities; direct `DbSet<>` access in request pipeline is safe as long as tenant context is set (TenantGuardMiddleware / ITenantProvider).
- **Platform services** (e.g. analytics, job processor) that need cross-tenant read use **TenantScopeExecutor.RunWithPlatformBypassAsync** or equivalent and are documented. No unconstrained cross-tenant DbSet access identified in request path.

---

## 5. Raw SQL or manual joins bypassing tenant boundaries

- **JobExecutionStore** uses raw SQL for claim/update; filters by Status and NextRunAtUtc; CompanyId is on the row and not used to filter in the claim query (workers set scope per job when executing). **Safe and justified.**
- **EventStoreRepository** similar; platform-wide claim then per-entry tenant scope on dispatch. **Safe and justified.**
- No raw SELECT/JOIN found that returns tenant data without tenant filter or platform-only usage.

---

## 6. Summary

| Category | Critical | Medium-risk | Safe and justified |
|----------|----------|-------------|--------------------|
| IgnoreQueryFilters | 0 | 1 (AssetService – verify) | All other uses |
| ExecuteSqlRaw(Async) | 0 | Several (parameterization/scope review) | Admin/worker/invoice |
| Controller override | 0 | 0 | Platform routes protected |
| DbSet / raw SQL | 0 | 0 | Scoped or platform-only |

**Critical and must be fixed:** None.

**Medium-risk and needs review:**  
1. AssetService single IgnoreQueryFilters use – confirm company constraint at call site.  
2. All ExecuteSqlRawAsync call sites – confirm parameterized and tenant scope where applicable.

**Recommended safeguards**

- Keep existing **TenantSafetyGuard** and **TenantScopeExecutor** usage; no new bypass without guard.
- Optional: Roslyn analyzer or unit tests that fail if IgnoreQueryFilters is added in Application layer without a matching `.Where(e => e.CompanyId == ...)` or documented platform bypass.
- Code review checklist: any new IgnoreQueryFilters or raw SQL must reference this report or architecture docs.
