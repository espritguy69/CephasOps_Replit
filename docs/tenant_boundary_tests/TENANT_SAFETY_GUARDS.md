# Tenant Safety Guards — Phase 4

Architectural safeguards that prevent developers from accidentally bypassing tenant isolation. This document describes what guards exist, where enforcement happens, how platform bypass works, and rules developers must follow.

**This phase does not change business behaviour.** It only adds enforcement and defensive validation.

---

## 1. Tenant-owned entities

The following are treated as tenant-scoped for guard enforcement:

- **All entities inheriting `CompanyScopedEntity`** (Order, RateCard, File, Invoice, Material, GuardConditionDefinition, Department, Partner, Building, WorkflowJob, etc.).
- **User** (tenant-scoped by CompanyId).
- **BackgroundJob**, **JobExecution** (workflow/job run records).
- **OrderPayoutSnapshot**, **InboundWebhookReceipt**.
- **Warehouse**, **Bin** (Settings entities with CompanyId).
- **AuditLog**, **JobRun** (have CompanyId; null allowed for system-wide events/jobs).

Tenant-owned writes must have a valid tenant context (or explicit platform bypass). Tenant-owned reads should be scoped by CompanyId or global query filter unless a documented bypass applies.

---

## 2. Guards implemented

| Guard | Location | Purpose |
|-------|----------|---------|
| **SaveChanges / SaveChangesAsync** | `ApplicationDbContext` | Before persisting: (1) If no tenant context and no platform bypass → throw when any tenant-scoped entity is Added/Modified/Deleted. (2) If tenant context is set → throw when any such entity’s CompanyId does not match current tenant. |
| **TenantSafetyGuard.AssertTenantContext** | `TenantSafetyGuard` | Throws if `TenantScope.CurrentTenantId` is null/empty and platform bypass is not active. Use before IgnoreQueryFilters or other high-risk paths. |
| **TenantScopeGuard.RequireTenantContext** | `TenantScopeGuard` | Delegates to `TenantSafetyGuard.AssertTenantContext`. Use in background jobs, batch operations, import pipelines, maintenance tasks to fail fast when tenant context is missing. |
| **RequireCompanyId (API)** | Controllers / `ControllerExtensions` | Returns 403 when `ITenantProvider.CurrentTenantId` is null/empty. Used by tenant-owned API endpoints. |
| **TenantGuardMiddleware** | API pipeline | Resolves tenant from headers/JWT and sets tenant context for the request. |

---

## 3. Where enforcement happens

- **Persistence:** `ApplicationDbContext.ValidateTenantScopeBeforeSave()` is called from both `SaveChanges()` and `SaveChangesAsync()`. No tenant-scoped entity can be saved without tenant context (or bypass), and no entity can be saved with a CompanyId that does not match the current tenant.
- **API:** Tenant-owned controllers use `RequireCompanyId(_tenantProvider)` or equivalent so that missing company context returns 403/400.
- **Background jobs / hosted services:** Must run tenant-owned work inside `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...)`. Platform-wide work must use `TenantScopeExecutor.RunWithPlatformBypassAsync(...)` and document why bypass is safe.
- **High-risk reads:** Before using `IgnoreQueryFilters()` on tenant-scoped sets, call `TenantSafetyGuard.AssertTenantContext()` (or `TenantScopeGuard.RequireTenantContext()`) and then apply an explicit CompanyId filter, or ensure the operation runs under a documented platform bypass.

---

## 4. Platform bypass

**When it is allowed:** Only in the cases listed in `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md` (seeding, design-time DbContext, retention/cleanup across tenants, scheduler loops that enumerate tenants, provisioning, webhooks/events with no company, stale job reap, etc.). Every bypass must be paired with `ExitPlatformBypass()` in a `finally` block (except design-time factory).

**How it works:** `TenantSafetyGuard.EnterPlatformBypass()` increments a counter; `ExitPlatformBypass()` decrements it. When the counter is greater than zero, SaveChanges validation is skipped and `AssertTenantContext()` does not throw. Prefer `TenantScopeExecutor.RunWithPlatformBypassAsync(...)` so enter/exit and restoration are consistent.

---

## 5. IgnoreQueryFilters usage (reviewed)

| Location | Usage | Safe because |
|----------|--------|---------------|
| **OrderProfitabilityService** | Orders, ServiceInstallers | Explicit `.Where(o => o.CompanyId == companyId)` and `.Where(s => s.CompanyId == order.CompanyId)`. |
| **DepartmentAccessService** | Departments | Used only when `IsSuperAdmin`; intent is platform-wide lookup. |
| **DatabaseSeeder** | OrderTypes, OrderCategories, BuildingTypes, SplitterTypes, Skills, Materials, MaterialCategories, DocumentTemplates | Runs under `EnterPlatformBypass`; all queries use explicit CompanyId or documented seed rules. |
| **EventPlatformRetentionService** | InboundWebhookReceipts | Runs under platform bypass; retention deletes by status and date. |
| **AuthService** | Users, RefreshTokens | Auth lookup by email/token is platform-wide; tenant scope is set for subsequent tenant-scoped work. |
| **BillingService** | Orders, BillingRatecards | Explicit `CompanyId` filter in Where. |
| **OrderService** | Orders | Calls `TenantSafetyGuard.AssertTenantContext()` and constrains by companyId. |
| **AssetService** | Assets | `.Where(a => a.CompanyId == disposal.CompanyId)`. |
| **BackgroundJobProcessorService** | BackgroundJobs | Platform operation: lists jobs from all tenants to assign work; scope is set per job in ProcessJobAsync. |
| **PlatformSupportController** | Users | `.Where(u => u.CompanyId == companyId)`. |
| **StockLedgerService** | SerialisedItem, ServiceInstaller, StockAllocation, etc. | Production path uses filters; `_isTesting` branches use IgnoreQueryFilters only in tests. |

All usages either have an explicit tenant filter after IgnoreQueryFilters or run under a documented platform bypass.

---

## 6. Raw SQL (FromSqlRaw / ExecuteSqlRaw / ExecuteSqlInterpolated) — reviewed

| Location | Query type | Tenant safety |
|----------|------------|----------------|
| **WorkerCoordinatorService** | UPDATE BackgroundJobs by Id | Claim/release by job id; job selection is platform-managed; tenant scope set when job runs. |
| **AdminService** | REFRESH MATERIALIZED VIEW | Platform-wide maintenance. |
| **SchedulerService** | INSERT ScheduledSlots, SiAvailabilities | CompanyId included in VALUES. |
| **InvoiceSubmissionService** | UPDATE InvoiceSubmissionHistory | WHERE InvoiceId and CompanyId (or CompanyId IS NULL). |
| **TaskService** | INSERT/UPDATE/DELETE TaskItems | CompanyId in VALUES and WHERE. |
| **ParserTemplateService, VipGroupService, VipEmailService, EmailRuleService, EmailTemplateService** | INSERT/UPDATE/DELETE | CompanyId in WHERE or VALUES. |

Tenant-owned raw SQL includes CompanyId in conditions or values; platform-wide usage is documented.

---

## 7. Background jobs and hosted services

- **Tenant-owned work** must run inside `TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...)` so that `TenantScope.CurrentTenantId` is set and SaveChanges validates correctly.
- **Platform-wide work** (retention, cleanup, scheduler loops that iterate tenants, job claim/release) must use `TenantScopeExecutor.RunWithPlatformBypassAsync(...)` or Enter/Exit in finally, and be documented (see developer guide).
- **Job processor** sets tenant scope per job from `job.CompanyId` before executing the job delegate.

---

## 8. Rules developers must follow

1. **Tenant-owned writes:** Do not add/update/delete tenant-scoped entities without setting `TenantScope.CurrentTenantId` (e.g. via API middleware or TenantScopeExecutor) or without using a documented platform bypass.
2. **Tenant-owned reads:** Do not use `IgnoreQueryFilters()` on tenant-scoped entity sets without either an explicit CompanyId filter afterward or a documented platform bypass. Prefer calling `TenantScopeGuard.RequireTenantContext()` (or `TenantSafetyGuard.AssertTenantContext()`) before IgnoreQueryFilters when in a tenant path.
3. **New bypasses:** Do not introduce new bypasses for convenience. Any new bypass must be justified, documented in the developer guide or this doc, and must pair `ExitPlatformBypass()` in a `finally` block (except design-time DbContext creation).
4. **API endpoints:** Tenant-owned endpoints must use `RequireCompanyId` or equivalent so that missing company context returns 403/400.
5. **Background jobs / hosted services:** Use TenantScopeExecutor for tenant scope or platform bypass; do not set TenantScope or EnterPlatformBypass manually in runtime services except where the developer guide explicitly allows.

---

## 9. References

- **Developer guide:** `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`
- **Security and tenant safety:** `backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md`
- **TenantScopeExecutor:** `CephasOps.Infrastructure.Persistence.TenantScopeExecutor`
- **Boundary tests:** `docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md`
