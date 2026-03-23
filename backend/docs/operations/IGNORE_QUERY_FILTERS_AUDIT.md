# IgnoreQueryFilters() — Tenant-Safety Audit

**Date:** 2026-03-12  
**Purpose:** Audit every use of `IgnoreQueryFilters()` in the backend and test codebase; classify by tenant-safety and align with [TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md).

**Rules:** SAFE_EXPLICIT_COMPANY = paired with explicit CompanyId/tenant constraint, narrow, fail-closed when company missing. SAFE_PLATFORM = documented platform-wide/admin path. NEEDS_REVIEW = justification unclear or constraint indirect. UNSAFE = no tenant constraint, broad access, or normal business flow can cross tenants.

The automated audit script ([TENANT_SAFETY_AUTOMATED_AUDIT.md](TENANT_SAFETY_AUTOMATED_AUDIT.md)) also reports **QueryByIdOnly** (FindAsync/Find/Single on tenant-scoped sets). Reviewed-safe patterns (reference-data/DTO enrichment in company-scoped flows) are described there and in [TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md).

---

## 1) Total occurrence count

**Total: 22** distinct usage sites (one row per logical use in the audit table below). Raw `.IgnoreQueryFilters()` call count in backend/src and backend/tests (excluding generated files) is higher because some methods contain multiple calls (e.g. DatabaseSeeder, StockLedgerService, EventPlatformRetentionService); each is reviewed in context and grouped by file/method/entity.

---

## 2) Audit summary by classification

| Classification         | Count | Notes |
|------------------------|-------|--------|
| **SAFE_EXPLICIT_COMPANY** | 8   | Explicit CompanyId/tenant in query; fail-closed or caller guarantees. |
| **SAFE_PLATFORM**      | 12  | Platform bypass, retention, job processor, auth flows, seeder, test-only. |
| **NEEDS_REVIEW**       | 2   | OrderService when companyId null; AssetService asset load indirect. |
| **UNSAFE**             | 0   | None. |

---

## 3) Full audit table

| # | File | Method | Entity/table | Classification | Reason | Fix recommended? |
|---|------|--------|--------------|----------------|--------|-------------------|
| 1 | Application/Workflow/Services/WorkflowEngineService.cs | GetEntityCompanyIdAsync | Order | SAFE_EXPLICIT_COMPANY | IgnoreQueryFilters + Where(o => o.Id == entityId && o.CompanyId == companyId). Caller passes companyId. | No |
| 2 | Application/Workflow/Services/WorkflowEngineService.cs | GetCurrentEntityStatusAsync | Order | SAFE_EXPLICIT_COMPANY | IgnoreQueryFilters + Where(o => o.Id == entityId && o.CompanyId == companyId). | No |
| 3 | Application/Workflow/Services/WorkflowEngineService.cs | UpdateEntityStatusAsync | Order | SAFE_EXPLICIT_COMPANY | IgnoreQueryFilters + FirstOrDefaultAsync(o => o.Id == entityId && o.CompanyId == companyId.Value). Throws if companyId null for Order. | No |
| 4 | Application/Pnl/Services/OrderProfitabilityService.cs | CalculateOrderProfitabilityAsync | Order | SAFE_EXPLICIT_COMPANY | RequireCompany(companyId); IgnoreQueryFilters + Where(o => o.Id == orderId && o.CompanyId == companyId). | No |
| 5 | Application/Pnl/Services/OrderProfitabilityService.cs | CalculateOrderProfitabilityAsync | ServiceInstaller | SAFE_EXPLICIT_COMPANY | IgnoreQueryFilters + Where(s => s.Id == order.AssignedSiId && s.CompanyId == order.CompanyId). | No |
| 6 | Application/Billing/Services/BillingService.cs | ResolveInvoiceLineFromOrderAsync | Order | SAFE_EXPLICIT_COMPANY | RequireCompany(companyId); IgnoreQueryFilters + Where(o => o.Id == orderId && o.CompanyId == companyId). | No |
| 7 | Application/Billing/Services/BillingService.cs | ResolveInvoiceLineFromOrderAsync | BillingRatecard | SAFE_EXPLICIT_COMPANY | IgnoreQueryFilters + Where(br => br.CompanyId == companyId && ...). | No |
| 8 | Application/Rates/Services/RateEngineService.cs | ResolveGponPayoutRateInternalAsync | GponSiJobRate | SAFE_EXPLICIT_COMPANY | When companyId.HasValue: IgnoreQueryFilters + Where(r => r.CompanyId == companyId.Value). Else branch uses global filter (no IgnoreQueryFilters). | No |
| 9 | Application/Orders/Services/OrderService.cs | DeleteOrderAsync | Order | NEEDS_REVIEW | AssertTenantContext(); query by id; CompanyId filter only when companyId.HasValue. When companyId is null, no explicit company filter — relies on ambient tenant only. | Yes (see §4) |
| 10 | Application/Assets/Services/AssetService.cs | ApproveDisposalAsync | Asset | NEEDS_REVIEW | AssertTenantContext(); disposal loaded with company filter; asset loaded by disposal.AssetId only (no Asset.CompanyId in query). Indirect company scope via disposal. | Yes (see §4) |
| 11 | Application/Auth/Services/AuthService.cs | RefreshTokenAsync | RefreshToken (+ User) | SAFE_PLATFORM | Auth flow: no tenant at entry; lookup by TokenHash; then set TenantScope from user.CompanyId before any write. | No |
| 12 | Application/Auth/Services/AuthService.cs | ChangePasswordRequiredAsync | User | SAFE_PLATFORM | Lookup by email (no tenant); set scope from user.CompanyId before SaveChanges. | No |
| 13 | Application/Auth/Services/AuthService.cs | ForgotPasswordAsync | User | SAFE_PLATFORM | Lookup by email; no tenant context; set scope for subsequent write. | No |
| 14 | Application/Auth/Services/AuthService.cs | ResetPasswordWithTokenAsync | PasswordResetToken (+ User) | SAFE_PLATFORM | Lookup by token hash; set scope from user.CompanyId before write. | No |
| 15 | Application/Departments/Services/DepartmentAccessService.cs | GetAccessAsync | DepartmentMembership | SAFE_PLATFORM | IgnoreQueryFilters only when EnvironmentName == "Testing". Test-only. | No |
| 16 | Application/Integration/EventPlatformRetentionService.cs | RunRetentionAsync | InboundWebhookReceipt | SAFE_PLATFORM | Platform retention delete; runs under EnterPlatformBypass; IgnoreQueryFilters to select/delete old Processed receipts across tenants. | No |
| 17 | Application/Workflow/Services/BackgroundJobProcessorService.cs | ProcessLoop (runningMine) | BackgroundJob | SAFE_PLATFORM | Intentional: see jobs from all tenants; scope set per job in ProcessJobAsync. Comment documents. | No |
| 18 | Application/Workflow/Services/BackgroundJobProcessorService.cs | ProcessLoop (queuedUnclaimed) | BackgroundJob | SAFE_PLATFORM | Same as above. | No |
| 19 | Application/Workflow/Services/BackgroundJobProcessorService.cs | ReapStaleRunningJobsAsync | BackgroundJob | SAFE_PLATFORM | Platform maintenance: load all Running jobs; updates under EnterPlatformBypass/ExitPlatformBypass. | No |
| 20 | Infrastructure/Persistence/DatabaseSeeder.cs | SeedAsync (multiple helpers) | OrderType, OrderCategory, BuildingType, Skill, Material, MaterialCategory, DocumentTemplate | SAFE_PLATFORM | SeedAsync calls EnterPlatformBypass(); all usages have explicit CompanyId in predicate (e.g. ot.CompanyId == companyId, c.CompanyId == companyId). | No |
| 21 | Application/Inventory/Services/StockLedgerService.cs | Multiple methods | Serial, ServiceInstaller, Allocation, Material, StockLocation, Order, etc. | SAFE_PLATFORM | All IgnoreQueryFilters gated on _isTesting. Test-only. | No |
| 22 | tests/CephasOps.Application.Tests/Auth/AuthServiceTests.cs | (test method) | User | SAFE_PLATFORM | Test asserts user state after ChangePasswordRequiredAsync; IgnoreQueryFilters to read user by Id in test DB. Test-only. | No |

---

## 4) High-risk findings

### NEEDS_REVIEW #1 — OrderService.DeleteOrderAsync (Order)

- **Risk:** When `companyId` is null, the query is `Orders.IgnoreQueryFilters().Where(o => o.Id == id)` with no CompanyId filter. Caller may still be in tenant context (AssertTenantContext passes), but the query could return another tenant’s order if a wrong id were passed.
- **Production impact:** Low if all callers pass companyId (e.g. OrdersController passes resolved companyId). If any path ever calls with companyId == null, that path could soft-delete another tenant’s order.
- **Guideline:** TENANT_QUERY_SAFETY_GUIDELINES: “Never use IgnoreQueryFilters without an explicit company filter unless it is a documented platform-wide path.” This is a normal business flow.
- **Minimal fix:** When `!companyId.HasValue`, after `AssertTenantContext()`, add `query = query.Where(o => o.CompanyId == TenantScope.CurrentTenantId)` so the query is always company-constrained (either by parameter or by current tenant). Do not weaken TenantSafetyGuard or remove global filters.

### NEEDS_REVIEW #2 — AssetService.ApproveDisposalAsync (Asset)

- **Risk:** Disposal is loaded with company filter; asset is loaded with `IgnoreQueryFilters().Where(a => a.Id == disposal.AssetId)` — no explicit Asset.CompanyId. Scope is indirect (disposal is company-scoped; asset id comes from disposal). If disposal.AssetId pointed to another tenant’s asset (data corruption or FK not enforced per tenant), we could load and update it.
- **Production impact:** Low if referential integrity and data keep Asset in same company as AssetDisposal.
- **Minimal fix (defense-in-depth):** Add explicit company constraint: `.Where(a => a.Id == disposal.AssetId && a.CompanyId == disposal.CompanyId)` so we never load another tenant’s asset.

---

## 5) Recommended next actions

- **Manual review:** OrderService.DeleteOrderAsync when companyId is null; AssetService.ApproveDisposalAsync asset load.
- **Minimal fixes (optional but recommended):**
  1. **OrderService.DeleteOrderAsync:** When `companyId` is null, add `query = query.Where(o => o.CompanyId == TenantScope.CurrentTenantId)` after AssertTenantContext() so the query always has an explicit company constraint.
  2. **AssetService.ApproveDisposalAsync:** Add `&& a.CompanyId == disposal.CompanyId` to the asset query.
- **No action required** for all other sites: they are either SAFE_EXPLICIT_COMPANY or SAFE_PLATFORM and align with the guideline.

---

## 6) Alignment with TENANT_QUERY_SAFETY_GUIDELINES.md

- **Rule:** “Never use IgnoreQueryFilters without an explicit company filter unless it is a documented platform-wide path.”
- **Violations:**
  - **OrderService.DeleteOrderAsync:** When `companyId` is null, the Order query has no explicit company filter (only AssertTenantContext). This violates the documented rule for a normal business flow.
  - **AssetService.ApproveDisposalAsync:** Asset query has no explicit CompanyId; scope is indirect via disposal. Guideline prefers an explicit company constraint on the query.
- All other production usages either have an explicit CompanyId (or equivalent) in the query or are documented platform-wide paths (auth, retention, job processor, seeder, test-only).

---

## 7) Confirmation

- **No schema changes** were made.
- **No tenant protections** (TenantSafetyGuard, global query filters) were weakened or removed.
- **No broad automatic refactors** were performed. This audit is read-only; fixes above are recommended and optional, not applied in this pass.
