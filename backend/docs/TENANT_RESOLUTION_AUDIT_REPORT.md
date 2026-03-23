# Tenant Resolution Audit Report — Request-Time CompanyId Usage

**Date:** 2026-03-12  
**Scope:** Request-time tenant-aware logic; ensure all use `ITenantProvider` (canonical effective company) and do not read `CurrentUser.CompanyId` / `ICurrentUserService.CompanyId` directly.

---

## 1. Summary

- **Files scanned:** Controllers, middleware, authorization, and API services under `backend/src` (CephasOps.Api, CephasOps.Application).
- **Unsafe usages changed:** All request-time reads of `_currentUserService.CompanyId` or `_currentUser.CompanyId` in tenant-scoping logic were replaced with `_tenantProvider.CurrentTenantId` (or `RequireCompanyId(_tenantProvider)` where a 403 on missing tenant is required).
- **Safe usages left unchanged:** Login-time resolution in AuthService, TenantProvider’s internal JWT step, entity/DTO properties (e.g. `entity.CompanyId`), and background job payload `job.CompanyId`.
- **Single resolution path:** No second tenant resolution path was introduced; all request-time tenant context flows through `ITenantProvider`.

---

## 2. Files Scanned (by area)

### Controllers (backend/src/CephasOps.Api/Controllers)

All controllers under `CephasOps.Api/Controllers` were checked for:

- `_currentUserService.CompanyId`
- `_currentUser.CompanyId`
- `ScopeCompanyId()` implementations that returned `_currentUser.CompanyId`

### Middleware (backend/src/CephasOps.Api/Middleware)

- `TenantGuardMiddleware.cs` — already uses `ITenantProvider.GetEffectiveCompanyIdAsync` + `CurrentTenantId`.
- `SubscriptionEnforcementMiddleware.cs` — already uses `ITenantProvider.CurrentTenantId`.
- `RequestLogContextMiddleware.cs` — **updated** to use `ITenantProvider.CurrentTenantId` for Serilog `CompanyId` instead of `currentUser?.CompanyId`.

### Services

- `TenantProvider.cs` — **intentionally** reads `_currentUser.CompanyId` as step 2 of the canonical resolution (JWT); this is the only place that should read JWT company for tenant resolution.
- `TenantContextService.cs` — uses `ITenantProvider.CurrentTenantId` only.
- Auth (login) — see “Safe usages” below.

### Authorization / Helpers

- No request-time tenant logic was found that read `CurrentUser.CompanyId` directly; control plane and SLA checks were updated to use `ITenantProvider` where they had used `_currentUser.CompanyId`.

---

## 3. Unsafe Usages Changed

### 3.1 Controllers with `ScopeCompanyId()` or equivalent

| File | Change |
|------|--------|
| `EventStoreController.cs` | Injected `ITenantProvider`; `ScopeCompanyId()` now returns `_tenantProvider.CurrentTenantId` when not SuperAdmin. |
| `EventLedgerController.cs` | Same. |
| `OperationalRebuildController.cs` | Same. |
| `IntegrationController.cs` | Same. |
| `OperationalTraceController.cs` | Same. |
| `ObservabilityController.cs` | Same. |
| `OperationalReplayController.cs` | Same. |
| `TraceController.cs` | Same. |
| `EventsController.cs` | Same. |
| `CommandOrchestrationController.cs` | Same (ScopeCompanyId). |

### 3.2 Controllers that used `_currentUserService.CompanyId` for tenant scoping

All of the following now use `ITenantProvider` (injected where missing) and `_tenantProvider.CurrentTenantId` (or `RequireCompanyId(_tenantProvider)`) for request-time company:

- BillingController, BillingRatecardController, PayrollController, RatesController  
- ServiceProfilesController, ServiceProfileMappingsController, GponBaseWorkRatesController, GponRateGroupsController, GponRateGroupMappingsController  
- FilesController, DocumentsController, DocumentTemplatesController  
- MaterialCategoriesController, MaterialTemplatesController  
- PartnersController, SplittersController (including `dto.CompanyId ?? _tenantProvider.CurrentTenantId`)  
- VerticalsController, OrderTypesController, OrderCategoriesController, OrderStatusChecklistController (both controller classes in file)  
- BuildingsController, BuildingTypesController  
- SchedulerController, ServiceInstallersController  
- InventoryController, ParserController, OrdersController  
- RMAController, NotificationsController  
- BackgroundJobsController, DepartmentsController  
- SplitterTypesController, InstallationTypesController  
- AssetsController, ReportsController  
- FinancialAlertsController, SiAppController  
- ControlPlaneController (tenant diagnostics: `companyId ?? _tenantProvider.CurrentTenantId`)  
- SlaMonitorController (`ApplyCompanyFilter` / `ApplyCompanyFilterRules` and breach rule check now use `_tenantProvider.CurrentTenantId`)  

### 3.3 Middleware

| File | Change |
|------|--------|
| `RequestLogContextMiddleware.cs` | Serilog `CompanyId` now taken from `ITenantProvider.CurrentTenantId` instead of `currentUser?.CompanyId`. |

### 3.4 Controllers that already had ITenantProvider

These already used `RequireCompanyId(_tenantProvider)` for most actions; remaining direct `_currentUserService.CompanyId` usages were replaced with `_tenantProvider.CurrentTenantId`:

- BillingRatecardController (one action), PayrollController (one action), RatesController (two actions)  
- ParserController, InventoryController, InstallationTypesController  

---

## 4. Safe Usages Intentionally Left Unchanged

### 4.1 Login-time / token issuance

- **AuthService.cs**  
  - `ResolveUserCompanyIdAsync` (and its call sites for login/refresh) resolves user company from DB (User.CompanyId, then department company) and sets JWT `company_id`.  
  - **Not changed**; this is login-time resolution only.

### 4.2 Canonical resolution implementation

- **TenantProvider.cs**  
  - Reads `_currentUser.CompanyId` as step 2 of the canonical precedence (JWT).  
  - **Not changed**; this is the single place that should read JWT company for tenant resolution.

### 4.3 Non–request-time / entity usage

- **Application and Infrastructure**  
  - Entity/DTO properties (e.g. `entity.CompanyId`, `order.CompanyId`, `dto.CompanyId`) and query filters (e.g. `Where(x => x.CompanyId == companyId)`) where `companyId` is passed in from a controller that now gets it from `ITenantProvider`.  
- **JobExecutionWorkerHostedService**  
  - Uses `job.CompanyId` from the job payload (background, not request context).  
- **EF configurations / indexes**  
  - References to `CompanyId` on entity types (e.g. indexes, config).  

All of the above were left as-is.

---

## 5. Ambiguous / Manual Review

- **BuildingTypesController, ReportsController**  
  - Use both company and department context (`IDepartmentAccessService`, `IDepartmentRequestContext`). Company for request-time tenant is now taken from `ITenantProvider`; department scope logic was not changed. No product/architecture change required.  
- **OrdersController**  
  - One action uses `companyId = (condition) ? _tenantProvider.CurrentTenantId : null` for optional profitability/financial alerts; behavior preserved with effective company from `ITenantProvider`.  

No other ambiguous cases were identified; no second resolution path was introduced.

---

## 6. Resolution Precedence (unchanged)

1. **X-Company-Id** (SuperAdmin override)  
2. **JWT CompanyId** (from `ICurrentUserService.CompanyId` inside TenantProvider only)  
3. **Department → Company fallback** (when JWT company missing)  
4. **Unresolved**  

All tenant-aware request-time logic now relies on this single path via `ITenantProvider` (and, where applicable, `GetEffectiveCompanyIdAsync` called by TenantGuardMiddleware).

---

## 7. Build and Tests

- **Build:** `dotnet build` for `CephasOps.Api` succeeds (0 warnings, 0 errors).  
- **TenantProviderTests:** Existing tests remain valid; they mock `ITenantProvider` / resolution behavior.

---

**Report generated:** 2026-03-12.  
**Rule:** All tenant-aware request-time logic MUST use `ITenantProvider` and MUST NOT read `CurrentUser.CompanyId` / `ICurrentUserService.CompanyId` directly (except inside TenantProvider for the JWT step).
