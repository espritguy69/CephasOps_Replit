# Tenant fallback removal remediation report

**Date:** 2026-03-13  
**Objective:** Remove unsafe single-company fallback logic that treated `companyId == Guid.Empty` (or null) as "no filter / all companies" and enforce fail-safe tenant resolution across CephasOps.

**Constraints applied:** No database schema changes, no new migrations, no refactor of unrelated code. No changes to platform observability or EventStore logic.

---

## 1. Summary

All identified services now require a valid non-empty `CompanyId` for tenant-scoped operations. Callers that previously passed `Guid.Empty` or relied on "single-company mode" will receive `InvalidOperationException` with a clear message. Queries always include a `CompanyId` filter; `TenantSafetyGuard.AssertTenantContext()` is used where appropriate.

---

## 2. Files changed

### 2.1 CurrentUserService

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Interfaces/CurrentUserService.cs` | When company claim is missing or parses to `Guid.Empty`, `CompanyId` getter throws `InvalidOperationException("Tenant context missing: CompanyId claim required.")` instead of returning `Guid.Empty`. |

### 2.2 Tenant resolution (API)

| File | Change |
|------|--------|
| `backend/src/CephasOps.Api/Services/TenantProvider.cs` | Step 2 (JWT company) reads company claim from `HttpContext.User` directly so department fallback does not trigger the new throw. |

### 2.3 Application services (Guid.Empty bypass removed)

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` | All single-company branches removed. Methods throw when `companyId == Guid.Empty`, call `TenantSafetyGuard.AssertTenantContext()`, and filter by `CompanyId` (GetParseSessionsAsync, GetParseSessionByIdAsync, GetParsedOrderDraftsAsync, GetParsedOrderDraftByIdAsync, GetFailedParseSessionsAsync, GetParsedOrderDraftsWithFiltersAsync, GetParserStatisticsAsync, GetParserAnalyticsAsync). Auto-create-building helper throws if companyId is null/empty. |
| `backend/src/CephasOps.Application/Payroll/Services/PayrollService.cs` | Guid.Empty branches removed; throw + AssertTenantContext + strict CompanyId filter in GetSiRatePlansAsync, GetSiRatePlanByIdAsync, CreateSiRatePlanAsync, UpdateSiRatePlanAsync, DeleteSiRatePlanAsync. |
| `backend/src/CephasOps.Application/Notifications/Services/NotificationService.cs` | `ResolveUsersByRoleAsync` requires non-null, non–Guid.Empty companyId, calls `TenantSafetyGuard.AssertTenantContext()`, filters by `User.CompanyId == companyId.Value`. |
| `backend/src/CephasOps.Application/Workflow/Services/WorkflowDefinitionsService.cs` | All Guid.Empty branches removed; throw + AssertTenantContext + CompanyId filter in GetWorkflowDefinitionsAsync, GetWorkflowDefinitionAsync, GetEffectiveWorkflowDefinitionAsync, and related definition lookups. |
| `backend/src/CephasOps.Application/Parser/Services/EmailAccountService.cs` | All single-company branches removed; throw + AssertTenantContext + CompanyId filter in GetEmailAccountsAsync, GetEmailAccountByIdAsync, CreateEmailAccountAsync, UpdateEmailAccountAsync, DeleteEmailAccountAsync, TestConnectionAsync. GetEmailAccountsAsync now filters by `ea.CompanyId == companyId` only. |
| `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs` | `TriggerPollAsync`: require non-empty companyId, throw + AssertTenantContext, filter account by `ea.CompanyId == companyId`. |
| `backend/src/CephasOps.Application/Assets/Services/AssetService.cs` | All "if (companyId.HasValue && companyId.Value != Guid.Empty)" style branches replaced with throw when null/empty + AssertTenantContext + always filter by companyId in GetAssetsAsync, GetAssetByIdAsync, CreateAssetAsync, UpdateAssetAsync, DeleteAssetAsync, GetAssetSummaryAsync, GetMaintenanceRecordsAsync, UpdateMaintenanceRecordAsync, DeleteMaintenanceRecordAsync, GetUpcomingMaintenanceAsync, CreateDisposalAsync, ApproveDisposalAsync, GetDisposalsAsync. |
| `backend/src/CephasOps.Application/Inventory/Services/StockLedgerService.cs` | Every `companyId ?? Guid.Empty` replaced with guard (throw when !companyId.HasValue \|\| companyId.Value == Guid.Empty) + `effectiveCompanyId = companyId.Value`; all queries filter by CompanyId. |
| `backend/src/CephasOps.Application/Tasks/Services/TaskService.cs` | `GetTaskByIdAsync`: removed Guid.Empty bypass; throw when companyId == Guid.Empty, AssertTenantContext, always filter by CompanyId. |

### 2.4 Background jobs

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Workflow/JobOrchestration/Executors/InventoryReportExportJobExecutor.cs` | After resolving companyId from job/payload, if null or Guid.Empty throws `InvalidOperationException("Background job requires tenant context.")`. Uses `effectiveCompanyId = companyId.Value` for export methods. |

### 2.5 Controllers

| File | Change |
|------|--------|
| `backend/src/CephasOps.Api/Controllers/PayrollController.cs` | Import path returns BadRequest when companyId is null/empty; filters ServiceInstallers, Departments, InstallationMethods by companyId.Value. |
| `backend/src/CephasOps.Api/Controllers/EmailAccountsController.cs` | Comment updated: "Multi-tenant SaaS — IngestAllEmailsAsync runs per authenticated context." |
| `backend/src/CephasOps.Api/Controllers/AssetsController.cs` | Comments updated: "Multi-tenant SaaS — CompanyId required; resolved from tenant context." (all single-company comments removed). |

### 2.6 Comments and XML docs

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Parser/Services/IParserService.cs` | Param doc: "Company context (multi-tenant SaaS — required)." |
| `backend/src/CephasOps.Application/Inventory/Services/IMovementValidationService.cs` | Param doc: "Company ID (multi-tenant SaaS — required)." |

---

## 3. Logic removed

- **CurrentUserService:** Returning `Guid.Empty` when no company claim; now throws.
- **ParserService:** Branches that skipped CompanyId filter when `companyId == Guid.Empty`; auto-create-building returning null for null/empty companyId.
- **PayrollService:** Branches that treated Guid.Empty as "no filter" for SI rate plans and related operations.
- **NotificationService:** Allowing null/Guid.Empty companyId in `ResolveUsersByRoleAsync` and filtering by "all companies".
- **WorkflowDefinitionsService:** Returning definitions without CompanyId filter when companyId was Guid.Empty.
- **EmailAccountService:** Returning or mutating accounts without strict CompanyId when companyId was Guid.Empty; including `ea.CompanyId == null` in GetEmailAccountsAsync.
- **EmailIngestionService:** In TriggerPollAsync, loading account regardless of CompanyId when companyId == Guid.Empty.
- **AssetService:** Optional company filter (`if (companyId.HasValue && companyId.Value != Guid.Empty)`); now always required.
- **StockLedgerService:** `effectiveCompanyId = companyId ?? Guid.Empty` and any query branch that skipped CompanyId when effectiveCompanyId was Guid.Empty.
- **TaskService:** GetTaskByIdAsync skipping CompanyId filter when companyId == Guid.Empty.
- **InventoryReportExportJobExecutor:** Using `companyId ?? Guid.Empty` for export; now requires tenant context and throws if missing.
- **PayrollController:** Import allowing Guid.Empty and filtering with "companyId == Guid.Empty" in Where clauses.

---

## 4. Tenant enforcement added

- **CurrentUserService:** Throw when company claim missing or Guid.Empty.
- **All updated services:**  
  - `if (companyId == Guid.Empty)` or `if (!companyId.HasValue || companyId.Value == Guid.Empty)` → throw `InvalidOperationException` with message "Tenant context missing: CompanyId required." (or "Background job requires tenant context." for job executor).  
  - `TenantSafetyGuard.AssertTenantContext()` where applicable.  
  - All queries filter by `CompanyId` (or `companyId.Value` for nullable).
- **Controllers:** Return 400 BadRequest when companyId is null/empty where appropriate (e.g. Payroll import).

---

## 5. Tests added

| File | Description |
|------|-------------|
| `backend/tests/CephasOps.Application.Tests/TenantIsolation/TenantFallbackRemovalTests.cs` | **Missing-tenant → exception:** CurrentUserService_WhenNoCompanyClaim_Throws; PayrollService_GetSiRatePlansAsync_WhenCompanyIdEmpty_Throws; WorkflowDefinitionsService_GetWorkflowDefinitionsAsync_WhenCompanyIdEmpty_Throws; NotificationService_ResolveUsersByRoleAsync_WhenCompanyIdEmpty_Throws, WhenCompanyIdNull_Throws; EmailAccountService_GetEmailAccountsAsync_WhenCompanyIdEmpty_Throws; AssetService_GetAssetsAsync_WhenCompanyIdNull_Throws, WhenCompanyIdEmpty_Throws; StockLedgerService_GetUsageSummaryExportRowsAsync_WhenCompanyIdEmpty_Throws, WhenCompanyIdNull_Throws; InventoryReportExportJobExecutor_WhenPayloadMissingCompanyId_Throws. |

Tests verify: missing tenant (no claim, null, or Guid.Empty) → exception; same-tenant behaviour is covered by existing service tests where callers pass valid companyId.

**Test fix (remediation follow-up):** `AssetServiceCreateDisposalTests.CreateDisposalAsync_WhenCompanyIdProvided_DoesNotCreateDisposalForAssetFromAnotherCompany` was updated to set `TenantScope.CurrentTenantId = companyA` before calling `CreateDisposalAsync`, so `TenantSafetyGuard.AssertTenantContext()` passes and the service then correctly throws "*not found*" for the other-company asset.

---

## 6. Not changed (by design)

- **Database schema / migrations:** None.
- **Platform observability:** No changes.
- **EventStore logic:** No changes.
- **DatabaseSeeder, CompanyService:** Comments about "single-company model" (one company record / seed) left as-is; they do not refer to Guid.Empty bypass.
- **PnlRebuildSchedulerService:** Comment about "first company from DB" left as-is (scheduler context; no Guid.Empty bypass in scope).

---

## 7. Verification

- Build: `dotnet build` for backend (Application + Api) succeeds.
- Application tests: `dotnet test --filter "FullyQualifiedName~TenantFallbackRemovalTests"` — **11 tests passed** (CurrentUserService, PayrollService, WorkflowDefinitionsService, NotificationService×2, EmailAccountService, AssetService×2, StockLedgerService×2, InventoryReportExportJobExecutor).

Callers that previously passed null or Guid.Empty into the updated methods will now receive `InvalidOperationException` and must supply a valid tenant context (e.g. from `TenantScopeExecutor` or resolved company id).

---

## 8. Completion pass (remaining high-risk services)

### 8.1 Remaining services completed

All target high-risk services from the multi-tenant SaaS verification report have been remediated:

| Service | Status | Notes |
|---------|--------|-------|
| CurrentUserService | ✓ | No CompanyId claim → throw (already in place). |
| ParserService | ✓ | All public read methods throw when companyId empty; filter by CompanyId. |
| PayrollService | ✓ | All public methods require companyId; no Guid.Empty bypass. |
| NotificationService (ResolveUsersByRoleAsync) | ✓ | Requires companyId; throws when null/empty; filter by User.CompanyId. |
| WorkflowDefinitionsService | ✓ | All definition reads throw when companyId empty; filter by CompanyId. |
| EmailAccountService | ✓ | All methods require companyId; strict CompanyId filter. |
| AssetService | ✓ | All methods throw when companyId null/empty; always filter by CompanyId. |
| StockLedgerService | ✓ | All paths throw when companyId null/empty; no `companyId ?? Guid.Empty`; private validators throw on Guid.Empty. |
| InventoryReportExportJobExecutor | ✓ | Throws "Background job requires tenant context." when companyId missing; uses effectiveCompanyId only. |

### 8.2 Exact fallback branches removed (this pass)

- **ParsedOrderDraftEnrichmentService:** `TryAutoCreateBuildingAsync` — replaced "return null" when `companyId == null || companyId == Guid.Empty` with `throw InvalidOperationException("Tenant context missing: CompanyId required for auto-create building.")`.
- **StockLedgerService:**  
  - `ValidateMaterialAndLocationAsync` / `ValidateLocationAsync`: added throw when `companyId == Guid.Empty`; removed "if (companyId != Guid.Empty && ...)" so company check is always enforced.  
  - `GetUsageSummaryExportRowsAsync`: removed "if (effectiveCompanyId != Guid.Empty)" and always apply `e.CompanyId == effectiveCompanyId` in the Where clause.  
  - `GetStockSummaryAsync`: unwrapped "if (effectiveCompanyId != Guid.Empty)" blocks; cache and ledger/reserved queries always filter by `effectiveCompanyId`.  
  - `GetLedgerAsync`: always apply `query.Where(e => e.CompanyId == effectiveCompanyId)` (removed conditional).  
  - `GetLedgerDerivedBalancesAsync`: same — always filter cache and ledger/reserved by `effectiveCompanyId`.

### 8.3 Methods now enforcing tenant context

- **ParsedOrderDraftEnrichmentService:** `TryAutoCreateBuildingAsync` — throws when companyId null/empty.
- **StockLedgerService:** `ValidateMaterialAndLocationAsync`, `ValidateLocationAsync` — throw when companyId == Guid.Empty; `GetLedgerAsync`, `GetStockSummaryAsync`, `GetUsageSummaryExportRowsAsync`, `GetLedgerDerivedBalancesAsync` — always filter by CompanyId (no conditional bypass).

### 8.4 Tests added (this pass)

- **NotificationService:** ResolveUsersByRoleAsync_WhenCompanyIdEmpty_Throws, ResolveUsersByRoleAsync_WhenCompanyIdNull_Throws.
- **EmailAccountService:** GetEmailAccountsAsync_WhenCompanyIdEmpty_Throws.
- **AssetService:** GetAssetsAsync_WhenCompanyIdNull_Throws, GetAssetsAsync_WhenCompanyIdEmpty_Throws.
- **StockLedgerService:** GetUsageSummaryExportRowsAsync_WhenCompanyIdEmpty_Throws, GetUsageSummaryExportRowsAsync_WhenCompanyIdNull_Throws.
- **InventoryReportExportJobExecutor:** WhenPayloadMissingCompanyId_Throws.

### 8.5 Verification run summary

- `dotnet build` — backend Application + tests: **succeeded**.
- `dotnet test --filter "FullyQualifiedName~TenantFallbackRemovalTests"` — **11 passed**, 0 failed.

---

## 9. Status: tenant fallback removal

**The original high-risk tenant fallback findings from the multi-tenant SaaS verification report are now fully remediated for the in-scope services.**

- **CurrentUserService:** No CompanyId claim → throw; no return of Guid.Empty.
- **ParserService, PayrollService, NotificationService (ResolveUsersByRoleAsync), WorkflowDefinitionsService, EmailAccountService, AssetService, StockLedgerService, InventoryReportExportJobExecutor:** No Guid.Empty or missing-company bypass; tenant context required; queries always filtered by CompanyId; missing tenant → InvalidOperationException (or "Background job requires tenant context." for the executor).

**Status: COMPLETE** for the target services listed in the remediation scope. No remaining "single-company mode" or Guid.Empty-as-all-companies behaviour in these services.
