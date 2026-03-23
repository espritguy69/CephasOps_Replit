# EF Core Tenant-Scope Safety Layer

**Date:** 2026-03-12  
**Status:** Implemented (defense-in-depth; complements TenantGuardMiddleware and controller RequireCompanyId).

---

## 1. Overview

CephasOps uses a **global query filter** in `ApplicationDbContext` so that EF Core queries are automatically scoped by the current tenant (company) when `TenantScope.CurrentTenantId` is set. This reduces the risk of accidental cross-tenant data access when a developer omits an explicit `CompanyId` filter.

- **Tenant source:** The API sets `TenantScope.CurrentTenantId` from `ITenantProvider.CurrentTenantId` in middleware (after TenantGuardMiddleware). Background jobs set it from the job’s `CompanyId` before executing. There is no second tenant-resolution path in the data layer.
- **Filter rule:** For each tenant-scoped entity, the filter is:  
  `TenantScope.CurrentTenantId == null || Entity.CompanyId == TenantScope.CurrentTenantId`  
  So when tenant context is **null** (e.g. migrations, design-time, or a platform-wide job), no tenant filter is applied. When it is set, only rows for that company are visible.

---

## 2. Which Entities Are Tenant-Filtered

### 2.1 CompanyScopedEntity (automatic)

All entity types that inherit from `Domain.Common.CompanyScopedEntity` get a global filter that combines:

- Soft delete: `!IsDeleted`
- Tenant: `TenantScope.CurrentTenantId == null || CompanyId == TenantScope.CurrentTenantId`

This covers the majority of domain entities (Orders, Invoices, Materials, Departments, etc.). See Domain for the full list.

### 2.2 Explicit filters (not CompanyScopedEntity)

The following types have a `CompanyId` (or equivalent) but do not inherit `CompanyScopedEntity`. They receive the **same tenant filter** (null = no filter, otherwise `CompanyId == TenantScope.CurrentTenantId`):

| Entity | Reason |
|--------|--------|
| **User** | Tenant-scoped by `User.CompanyId`; filter applied in DbContext. |
| **BackgroundJob** | Job queue; filter ensures API sees only current tenant’s jobs; worker claims via raw SQL (no filter). |
| **JobExecution** | Same as BackgroundJob; worker claims via raw SQL, then sets TenantScope per job for MarkSucceeded/MarkFailed. |
| **OrderPayoutSnapshot** | Snapshot per order; tenant-scoped by `CompanyId`. |
| **InboundWebhookReceipt** | Receipts per connector/company; tenant-scoped by `CompanyId`. |

---

## 3. Which Entities Are NOT Tenant-Filtered

- **Tenant, Company, BillingPlan, etc.** — Platform/root or shared reference data; no `CompanyId` or not tenant-scoped by design.
- **EventStoreEntry** — Has `CompanyId` but is not filtered in this layer; replay/rebuild and event-sourcing semantics are handled in application logic. Optional future addition if the team agrees.
- **PayoutSnapshotRepairRun** — Run history; no `CompanyId`; platform-wide.
- **Migrations / design-time** — No `TenantScope`; filters see `CurrentTenantId == null` and do not restrict rows.

---

## 4. How Effective Tenant Context Reaches the Data Layer

1. **HTTP request:** Middleware runs after authentication. An inline middleware sets `TenantScope.CurrentTenantId = tenantProvider.CurrentTenantId` (from `ITenantProvider`, which uses X-Company-Id for SuperAdmin, else JWT company_id, else DefaultCompanyId). In `finally` it sets `TenantScope.CurrentTenantId = null`.
2. **Background job:** Before executing a job, the worker sets `TenantScope.CurrentTenantId = job.CompanyId`. After the job it restores the previous value. So all EF queries during that job see the job’s company.
3. **Job claiming:** `JobExecutionStore.ClaimNextPendingBatchAsync` uses **raw SQL**, so it is not affected by the global filter. Workers can claim jobs for any company; execution is then scoped per job.
4. **Migrations / design-time:** `TenantScope.CurrentTenantId` is not set, so it remains null and the filter allows all rows (no tenant restriction).

---

## 5. SuperAdmin with X-Company-Id

- `ITenantProvider.CurrentTenantId` returns the header value when the user is SuperAdmin.
- Middleware sets `TenantScope.CurrentTenantId` from that value.
- All tenant-filtered EF queries therefore see only the selected company. No change to existing behavior; no hidden cross-tenant data.

---

## 6. Missing-Tenant Situations

- **Request with no tenant (e.g. blocked by TenantGuard):** The request does not reach the middleware that sets TenantScope, so no tenant-scoped controller/EF code runs. Fail-closed.
- **Request that bypasses guard (e.g. /api/auth, AllowNoTenant):** TenantScope may be set from DefaultCompanyId or left null by the provider. If null, EF filters do not restrict by company (all rows visible for those entity types). These paths are intentionally tenant-agnostic or auth-only.
- **Background worker (polling):** When claiming jobs, TenantScope is not set; claiming uses raw SQL. When executing a job, TenantScope is set to the job’s CompanyId, so EF during execution is tenant-scoped.

---

## 7. Explicit Bypasses (IgnoreQueryFilters)

Use of `IgnoreQueryFilters()` is limited and documented:

| Location | Purpose |
|----------|--------|
| **EventPlatformRetentionService** | InboundWebhookReceipts retention delete is platform-wide; bypasses tenant filter so old receipts for all companies can be purged. |
| **OrderService** | Load order by id including soft-deleted (single-order lookup). |
| **StockLedgerService** | Testing only (`_isTesting`); production does not bypass. |
| **DepartmentAccessService** | Testing only; production does not bypass. |
| **DatabaseSeeder** | Seeding (if enabled); legacy. |
| **AssetService** | One path for DeletedAt/soft-delete handling. |

Any new bypass must be justified and documented here or in code comments.

---

## 8. TenantSafetyGuard (Final Data-Layer Guard)

A final defensive layer ensures that **writes** to tenant-scoped entities do not proceed without tenant context unless a documented platform bypass is active.

- **SaveChangesAsync:** Before persisting, if `TenantSafetyGuard.IsPlatformBypassActive` is false and `TenantScope.CurrentTenantId` is null or `Guid.Empty`, any Added/Modified/Deleted entry whose type is tenant-scoped (CompanyScopedEntity, User, BackgroundJob, JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt) causes an `InvalidOperationException`. This fails closed instead of allowing a silent cross-tenant write.
- **Platform bypass:** Retention (EventPlatformRetentionService), seeding (DatabaseSeeder), and design-time (ApplicationDbContextFactory) call `TenantSafetyGuard.EnterPlatformBypass()` (and Exit where appropriate) so they are not blocked.
- **AssertTenantContext():** Services that use `IgnoreQueryFilters()` on tenant-scoped entities (e.g. OrderService.DeleteOrderAsync, AssetService.ApproveDisposalAsync) must call `TenantSafetyGuard.AssertTenantContext()` before the bypassed query so that missing tenant context fails fast.

See **docs/operations/TENANT_SAFETY_FINAL_VERIFICATION.md** §10 for full detail. This guard does not replace ITenantProvider, TenantGuardMiddleware, RequireCompanyId(), TenantScope, or global query filters.

---

## 9. Limitations and Intentional Exclusions

- **EventStoreEntry** is not globally filtered in this layer; replay and rebuild logic may span or target specific companies in application code.
- **SaveChanges** does not validate or rewrite `CompanyId`; the existing pattern (services set CompanyId when creating entities) is unchanged. It **does** require tenant context (or platform bypass) when saving tenant-scoped entity types.
- **Defense-in-depth only:** This layer does not replace controller/service discipline. Controllers should still use `RequireCompanyId()` and services should still scope by tenant where appropriate.
- **Admin / reporting:** Queries that must span companies (e.g. platform-wide job list) require an explicit `IgnoreQueryFilters()` and must be clearly documented and restricted (e.g. SuperAdmin-only).

---

## 10. Validation Matrix

| Case | Expected | Status |
|------|----------|--------|
| A. Tenant-owned query by normal user | Filtered to effective company | **Supported** (TenantScope from JWT) |
| B. Tenant-owned query by SuperAdmin with X-Company-Id | Filtered to header company | **Supported** |
| C. Tenant-owned query with missing tenant context | Fail-closed or safe (no broad leak) | **Supported** (guard blocks; or TenantScope null on bypass paths) |
| D. Global/shared entity query | Not tenant-filtered | **Supported** (no filter on those types) |
| E. Background job for a specific company | Works with TenantScope set from job | **Supported** |
| F. Platform-wide job (e.g. retention) | Not broken | **Supported** (IgnoreQueryFilters where needed) |
| G. Migrations / design-time | Unaffected | **Supported** (TenantScope null) |
| H. Explicit cross-company admin flow | Only via documented IgnoreQueryFilters | **Supported with limitation** (must be explicit and documented) |
