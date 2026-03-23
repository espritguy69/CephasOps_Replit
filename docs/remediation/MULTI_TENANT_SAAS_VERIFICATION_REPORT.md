# Multi-Tenant SaaS Verification Report

**Date:** 2026-03-13  
**Type:** Read-only verification audit  
**Scope:** Code, docs, tests, prompts, UI flows, reports, exports, workers, SI-app paths  
**Objective:** Confirm whether CephasOps is operationally safe as a multi-tenant SaaS system or still behaves like a single-company system / could bypass tenant isolation.

No code, schema, migrations, or refactors were applied; audit only.

---

## 1. Executive Summary

The repository has **substantial multi-tenant safeguards** (TenantScopeExecutor, TenantSafetyGuard, EventStoreConsistencyGuard, FinancialIsolationGuard, SiWorkflowGuard, tenant-scoped event dispatch/replay, platform analytics and platform support behind SuperAdmin, and many tenant-isolation tests). However, **several runtime paths still implement “single-company mode” semantics**: when `CompanyId` is missing or `Guid.Empty`, they **omit tenant filtering** and can return or mutate data across tenants. The main enabler is **CurrentUserService** returning `Guid.Empty` when the JWT has no company claim, and multiple application services treating `Guid.Empty` as “do not filter by company.”

**Highest impact:** Parser (sessions/drafts), Payroll (SI rate plans and related), Notifications (resolve users by role), WorkflowDefinitions, EmailAccountService, AssetService, and StockLedgerService all have branches that skip or weaken company filtering when `companyId` is null/empty. This makes the system **NOT YET SAFE** for multi-tenant production until those paths are removed or replaced with strict tenant resolution and fail-safe behaviour.

---

## 2. Scope Reviewed

| Area | What was reviewed |
|------|-------------------|
| **Backend services** | Parser, Payroll, Notifications, Workflow, Billing, Orders, Inventory (StockLedger), Assets, Auth, Admin, Departments, Email accounts/templates, EventStore, platform analytics/support |
| **Controllers** | PlatformAnalytics, PlatformSupport, ControlPlane, Billing, ReportDefinitions, Reports, Inventory (reports/export), Orders, Admin |
| **Financial / payout flows** | BillingService (ResolveInvoiceLine — company-scoped), PaymentService, OrderPayoutSnapshotService, FinancialIsolationGuard usage |
| **EventStore** | EventStoreDispatcherHostedService (RunWithTenantScopeOrBypassAsync per entry), EventReplayService (scopeCompanyId check, executor), EventStoreConsistencyGuard |
| **Background workers** | BackgroundJobProcessorService (IgnoreQueryFilters + scope per job), JobExecutionStore (platform bypass for reset), schedulers using TenantScopeExecutor, InventoryReportExportJobExecutor |
| **Platform / admin** | PlatformAnalyticsController (SuperAdmin/Admin), PlatformSupportController (SuperAdmin, Impersonate/tenant diagnostics), AdminService (materialized view refresh, cache flush) |
| **Frontend** | api/client (company/department context, X-Company-Id), SideEffectDefinitionsPage (companyId fallback), query/cache usage |
| **Exports / reports** | Inventory report export (usage-summary, serial-lifecycle), ReportDefinitionsController (GetById without companyId), ReportsController (department scope), inventory/building/department/company export endpoints |
| **SI-app** | frontend-si api (si-app.ts companyId comments and validation) |
| **Tests** | TenantIsolation, FinancialIsolation, TenantSafetyInvariant, SingleCompanyModeRemoval, BillingServiceFinancialIsolation, OrderPayoutSnapshotServiceFinancialIsolation, TenantBoundaryTests |
| **Docs / prompts** | Single-company wording in docs (architecture, business, modules, operations, remediation), cursor rules (already updated per prior audit) |

Searches included: “single-company”, “single company”, “multi-department”, “global company”, “companyId optional”, “multi-company removed”; `IgnoreQueryFilters`, `FromSql`/`ExecuteSqlRaw`; manual `TenantScope.CurrentTenantId` / `EnterPlatformBypass`/`ExitPlatformBypass`; `TenantScopeExecutor`; `Guid.Empty` / `companyId == null` in application code.

---

## 3. High-Risk Findings

### 3.1 CurrentUserService returns Guid.Empty when JWT has no company claim

**File:** `backend/src/CephasOps.Application/Interfaces/CurrentUserService.cs` (lines 43–45)

**Evidence:** When the company ID claim is missing or invalid, the getter returns `Guid.Empty` with comments: “Single-company mode: return Guid.Empty to bypass company filtering” and “Services check for Guid.Empty to return all data without company filter.”

**Risk:** Any service that receives this value and treats `Guid.Empty` as “no company filter” will expose or mutate data across tenants. This is the main enabler for the behaviours below.

**Classification:** HIGH — systemic; enables cross-tenant behaviour in multiple services.

---

### 3.2 ParserService: multiple methods do not filter by companyId

**File:** `backend/src/CephasOps.Application/Parser/Services/ParserService.cs`

**Evidence:**  
- `GetParseSessionsAsync`: comment “Single-company mode: Don't filter by companyId - return all sessions regardless of companyId”; query has no `CompanyId` filter.  
- `GetParseSessionByIdAsync`: “Don't filter by companyId - return session regardless of companyId”; session by id only.  
- `GetParsedOrderDraftsAsync`: after validating session belongs to company, drafts are loaded with “Single-company mode: Don't filter by companyId at all” — no `CompanyId` on drafts query.  
- Similar comments/missing filters at lines 1897, 1939, 2109 (failed sessions, drafts, records).

**Risk:** Parse sessions and drafts from all tenants can be returned to a caller whose context is one tenant.

**Classification:** HIGH — cross-tenant read of parser data.

---

### 3.3 PayrollService: Guid.Empty skips company filter

**File:** `backend/src/CephasOps.Application/Payroll/Services/PayrollService.cs`

**Evidence:** Comments “Single-company mode: if companyId is Guid.Empty, don't filter by company” at lines 515, 586, 637, 723, 800. Queries use `companyId == Guid.Empty ? _context.SiRatePlans.AsQueryable() : _context.SiRatePlans.Where(...)` (and similar for other entities).

**Risk:** With `Guid.Empty` (e.g. from CurrentUserService), SI rate plans and related payroll data from all tenants are returned or updated.

**Classification:** HIGH — financial/payroll data cross-tenant.

---

### 3.4 NotificationService: ResolveUsersByRoleAsync has no CompanyId filter

**File:** `backend/src/CephasOps.Application/Notifications/Services/NotificationService.cs` (lines 209–214)

**Evidence:** Comment “CompanyId filter removed - single-company mode”. Query on UserRoles/Role/User has no company or tenant filter; returns all active users with the given role across tenants.

**Risk:** Notifications or escalation logic can target users in other tenants.

**Classification:** HIGH — cross-tenant user resolution for notifications.

---

### 3.5 WorkflowDefinitionsService: Guid.Empty returns all workflows

**File:** `backend/src/CephasOps.Application/Workflow/Services/WorkflowDefinitionsService.cs`

**Evidence:** Multiple branches “Single-company mode: if companyId is Guid.Empty, return all workflows regardless of CompanyId” (e.g. lines 39–41, 95–96, 137, 249–250, 328–329). Queries use `companyId == Guid.Empty ? _context.WorkflowDefinitions... : _context.WorkflowDefinitions.Where(... CompanyId ...)`.

**Risk:** Workflow definitions from other tenants can be read or used when company context is empty.

**Classification:** HIGH — workflow configuration cross-tenant.

---

### 3.6 EmailAccountService: Guid.Empty returns all accounts

**File:** `backend/src/CephasOps.Application/Parser/Services/EmailAccountService.cs`

**Evidence:** Multiple “In single-company mode (companyId == Guid.Empty), return all accounts regardless of CompanyId” (lines 36–37, 82–83, 179–180, 223–224, 243–244). When `companyId == Guid.Empty`, queries omit CompanyId filter; one path sets `CompanyId = companyId == Guid.Empty ? null : companyId`.

**Risk:** Email accounts (and thus parser behaviour) from other tenants can be listed or used.

**Classification:** HIGH — parser and email configuration cross-tenant.

---

### 3.7 AssetService: null or Guid.Empty skips company filter

**File:** `backend/src/CephasOps.Application/Assets/Services/AssetService.cs`

**Evidence:** Line 238 comment “In single-company mode, companyId might be null or Guid.Empty - don't filter by company in that case”. Multiple branches use `if (companyId.HasValue && companyId.Value != Guid.Empty)` to decide whether to filter; when false, company filter is omitted.

**Risk:** Asset data from other tenants can be read or modified when company context is missing/empty.

**Classification:** HIGH — asset data cross-tenant.

---

### 3.8 StockLedgerService: effectiveCompanyId == Guid.Empty omits company filter

**File:** `backend/src/CephasOps.Application/Inventory/Services/StockLedgerService.cs`

**Evidence:** Widespread use of `var effectiveCompanyId = companyId ?? Guid.Empty` and conditions like `(effectiveCompanyId == Guid.Empty || s.CompanyId == effectiveCompanyId)` or `companyId == Guid.Empty || e.CompanyId == companyId` (e.g. lines 40, 106, 759, 958, 1084, 1196). When caller passes null or Guid.Empty, queries are not restricted by company.

**Risk:** Inventory, serials, allocations, and ledger data from all tenants can be read or written (e.g. exports, reports, movements).

**Classification:** HIGH — inventory and ledger cross-tenant.

---

### 3.9 InventoryReportExportJobExecutor: companyId ?? Guid.Empty passed to ledger

**File:** `backend/src/CephasOps.Application/Workflow/JobOrchestration/Executors/InventoryReportExportJobExecutor.cs` (lines 88, 102)

**Evidence:** `companyId` comes from `job.CompanyId` and payload; then `companyId ?? Guid.Empty` is passed to `GetUsageSummaryExportRowsAsync` and `GetSerialLifecycleExportRowsAsync`. StockLedgerService treats Guid.Empty as “no company filter.”

**Risk:** If a job is enqueued without CompanyId (or with null), the export can include data from all tenants. Controller enqueue path uses request companyId; risk is from other enqueue paths or misconfiguration.

**Classification:** HIGH — report export cross-tenant when CompanyId is missing.

---

## 4. Medium-Risk Findings

### 4.1 ReportDefinitionsController.GetById has no companyId

**File:** `backend/src/CephasOps.Api/Controllers/ReportDefinitionsController.cs` (GetById(Guid id))

**Evidence:** GetById(id) calls `_service.GetByIdAsync(id)` with no company or tenant parameter. If the service or DbContext does not enforce tenant scope (e.g. global filter or explicit companyId), any tenant could read another tenant’s report definition by ID.

**Risk:** Cross-tenant read of report definitions; depends on service/EF global filter. Should be verified and, if not scoped, fixed.

**Classification:** MEDIUM — possible cross-tenant read; needs verification.

---

### 4.2 BaseWorkRateService: manual TenantScope assignment

**File:** `backend/src/CephasOps.Application/Rates/Services/BaseWorkRateService.cs` (lines 27, 71, 79, 96)

**Evidence:** Code sets `TenantScope.CurrentTenantId = companyId` and restores in finally. Cursor rules require runtime services to use TenantScopeExecutor instead of manual scope.

**Risk:** Pattern is easy to get wrong (e.g. missing restore on exception); inconsistent with project standard. Service does constrain by companyId in logic; risk is maintainability and future misuse.

**Classification:** MEDIUM — pattern violation and maintainability; not an immediate data leak if companyId is always valid.

---

### 4.3 Frontend SideEffectDefinitionsPage: companyId fallback

**File:** `frontend/src/pages/settings/SideEffectDefinitionsPage.tsx` (line 38)

**Evidence:** Comment “Single-company mode: fallback to first department's companyId if activeDepartment doesn't have one”. Fallback logic may not align with multi-tenant (e.g. wrong tenant if user has departments in more than one company).

**Risk:** UI could send the wrong tenant context for side-effect definitions; depends on auth/department context correctness.

**Classification:** MEDIUM — UI/flow clarity and correctness in multi-tenant.

---

### 4.4 SI-app: companyId comments and validation

**File:** `frontend-si/src/api/si-app.ts` (lines 193–198)

**Evidence:** Comments “In single-company mode, companyId may be null” and “For now, we'll require it” with throw if !companyId. Behaviour is tenant-safe (requires companyId), but wording reinforces single-company assumption.

**Risk:** Misleading for future changes; no runtime cross-tenant leak.

**Classification:** MEDIUM — documentation/clarity; could lead to someone “relaxing” the check.

---

### 4.5 EmailIngestionService: single-company mode branch

**File:** `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs` (line 282)

**Evidence:** “In single-company mode (companyId == Guid.Empty), return account regardless of CompanyId”. Same pattern as EmailAccountService.

**Risk:** Same as EmailAccountService when companyId is Guid.Empty — cross-tenant account resolution.

**Classification:** MEDIUM — same family as HIGH 3.6; severity lower if caller is always scoped (e.g. scheduler with companyId).

---

### 4.6 Documentation still describes single-company as current

**Files:** e.g. `docs/architecture/00_company-systems-overview.md`, `docs/03_business/STORYBOOK.md`, `docs/02_modules/companies/WORKFLOW.md`, `docs/operations/scope_not_handled.md`, `docs/01_system/MULTI_COMPANY_ARCHITECTURE.md`, `docs/07_frontend/ui/COMPANY_SETTINGS.md`, `docs/architecture/CODEBASE_INTELLIGENCE_MAP.md`, `docs/operations/system_evolution_risk.md`, and others (see audit appendix).

**Evidence:** Phrases such as “Single Company”, “single-company mode”, “one global company”, “single company only (no dropdown)”, “single-company/multi-tenant capable”, “single-company assumptions” appear in architecture, business, and operational docs.

**Risk:** Future implementers may assume single-company and introduce or preserve unsafe patterns.

**Classification:** MEDIUM — misleading documentation; no direct runtime impact.

---

## 5. Low-Risk Findings

### 5.1 IgnoreQueryFilters usage that is tenant-safe

**Files:** OrderService (DeleteOrderAsync), BillingService (ResolveInvoiceLineFromOrder), PlatformSupportController (Impersonate), BackgroundJobProcessorService (claim then scope per job).

**Evidence:** IgnoreQueryFilters is used but followed by explicit `Where(o => o.CompanyId == ...)` or TenantScope set before EF use; or SuperAdmin-only with tenant-scoped lookup (e.g. companyId from tenant).

**Classification:** LOW — acceptable use; no change required for tenant safety.

---

### 5.2 Manual TenantScope / EnterPlatformBypass in tests and allowed contexts

**Files:** Multiple test files (TenantIsolation, FinancialIsolation, BillingServiceFinancialIsolation, etc.), DatabaseSeeder, ApplicationDbContextFactory, JobExecutionStore (ResetStuckRunningAsync), AuthService (login lookup then set scope), Program.cs (middleware setting TenantScope from TenantProvider).

**Evidence:** Tests and seed/design-time code set TenantScope or use bypass; JobExecutionStore and AuthService use bypass in documented, limited ways; middleware sets scope from ITenantProvider.

**Classification:** LOW — allowed by .cursor/rules; no remediation for tenant safety.

---

### 5.3 DepartmentAccessService IgnoreQueryFilters only in Testing

**File:** `backend/src/CephasOps.Application/Departments/Services/DepartmentAccessService.cs` (lines 45–47)

**Evidence:** `IgnoreQueryFilters()` is applied only when `_hostEnvironment.EnvironmentName` is “Testing”. Production path uses normal query (tenant filter applies if present).

**Classification:** LOW — test-only; no production impact.

---

### 5.4 StockLedgerService _isTesting IgnoreQueryFilters

**File:** `backend/src/CephasOps.Application/Inventory/Services/StockLedgerService.cs`

**Evidence:** Multiple `if (_isTesting) query = query.IgnoreQueryFilters();`. Used only in test mode.

**Classification:** LOW — test-only.

---

### 5.5 Legacy single-company comments in code

**Evidence:** Various “single-company mode” or “Guid.Empty” comments in interfaces (e.g. IMovementValidationService), DTOs (DepartmentCompanyResolutionResult), and operational docs. No code path changes suggested here; comments are outdated or historical.

**Classification:** LOW — legacy wording; cleanup recommended but not blocking.

---

## 6. Test Coverage Gaps

- **ParserService:** No tests found that assert cross-tenant isolation for GetParseSessionsAsync, GetParseSessionByIdAsync, or GetParsedOrderDraftsAsync (e.g. tenant A must not see tenant B’s sessions/drafts).
- **PayrollService:** No tests that assert Guid.Empty or null companyId is rejected or that data is never returned for another tenant.
- **NotificationService ResolveUsersByRoleAsync:** No test that verifies users from only the current tenant are returned when company context is set.
- **WorkflowDefinitionsService / EmailAccountService / AssetService:** Same gap — no explicit cross-tenant read tests for the Guid.Empty branches.
- **StockLedgerService:** Tests use _isTesting and IgnoreQueryFilters; no test that when companyId is null/empty the service fails or refuses to return cross-tenant data (preferred behaviour for SaaS).
- **CurrentUserService:** No test that when JWT has no company claim the system fails safely (e.g. 403 or equivalent) rather than returning Guid.Empty and enabling “all data” behaviour.
- **InventoryReportExportJobExecutor:** No test that job with null CompanyId either fails or is strictly scoped to a single tenant.
- **ReportDefinitionsController GetById:** No test that GetById(id) cannot return another tenant’s definition when tenant context is set.

Recommended: add tenant-isolation tests for the above (same-tenant allowed, cross-tenant forbidden, missing-tenant fails safely).

---

## 7. Recommended Next Actions

1. **Remove “single-company” behaviour from tenant identity**  
   - Change CurrentUserService so that when the JWT has no valid company claim it does **not** return Guid.Empty. Options: return null and require callers to fail (e.g. 403), or resolve company from department/ITenantProvider and fail if unresolved. Document expected behaviour and update all callers that currently rely on Guid.Empty.

2. **Enforce tenant scope in application services**  
   - ParserService, PayrollService, NotificationService (ResolveUsersByRoleAsync), WorkflowDefinitionsService, EmailAccountService, EmailIngestionService, AssetService: remove branches that treat Guid.Empty or null as “no company filter”. Require a valid companyId for tenant-scoped operations; otherwise throw or return empty/forbidden. Add CompanyId to ResolveUsersByRoleAsync (or resolve from current tenant) and filter by it.

3. **StockLedgerService**  
   - Do not treat `companyId ?? Guid.Empty` as “all tenants”. Require a non-empty companyId for tenant-scoped methods, or use TenantScope.CurrentTenantId and fail if missing. Ensure exports and reports never run with “all companies”.

4. **InventoryReportExportJobExecutor**  
   - Require job.CompanyId (or validated companyId from payload); do not pass Guid.Empty to the ledger. Reject or fail the job if companyId is null/empty.

5. **ReportDefinitionsController.GetById**  
   - Verify that GetById is tenant-scoped (e.g. via global filter or explicit companyId from context). If not, add tenant parameter or resolve from context and filter.

6. **BaseWorkRateService**  
   - Refactor to use TenantScopeExecutor.RunWithTenantScopeAsync(companyId, ...) instead of manually setting/restoring TenantScope, in line with .cursor/rules.

7. **Frontend and SI-app**  
   - Replace “single-company” fallbacks and comments with explicit tenant-from-auth/department context and document that companyId is required for tenant-scoped SI operations.

8. **Documentation**  
   - Update architecture, business, and operational docs to state that CephasOps is multi-tenant SaaS and to remove or qualify “single-company” and “one global company” wording. Prefer “tenant” and “company (tenant)” and clarify where single-tenant deployment is an option.

9. **Test coverage**  
   - Add the tenant-isolation and fail-safe tests listed in §6.

---

## 8. Final Verdict

**NOT YET SAFE**

Reason: Multiple application services still implement “single-company mode” when `companyId` is null or `Guid.Empty`, and CurrentUserService explicitly returns `Guid.Empty` to “bypass company filtering.” That combination allows cross-tenant reads (and in some cases potential writes) for parser, payroll, notifications, workflow definitions, email accounts, assets, and inventory/ledger. Until those paths are removed or replaced with strict tenant resolution and fail-safe behaviour (and validated by tests), the system should not be considered operationally safe for multi-tenant SaaS production.

---

## 9. Targeted Verification Checklist

| # | Area | Status | Evidence-based note |
|---|------|--------|---------------------|
| 1 | **Financial isolation** | **PARTIAL** | FinancialIsolationGuard is used in BillingService, PaymentService, OrderPayoutSnapshotService, PnlService, BillingRatecardService, PayrollService, PayoutAnomalyService, and RateEngineService. However, PayrollService (and possibly others) still has “single-company” branches when companyId is Guid.Empty that skip company filter (e.g. SiRatePlans, SI payroll data). So financial isolation is enforced in many paths but **weakened** where Guid.Empty is accepted as “no filter.” |
| 2 | **EventStore consistency guard** | **VERIFIED** | EventStoreConsistencyGuard is implemented and used: RequireTenantOrBypassForAppend, RequireParentRootCompanyMatch. EventStoreRepository uses it; EventStoreDispatcherHostedService dispatches per entry with TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(entry.CompanyId, ...); EventReplayService checks scopeCompanyId vs entry.CompanyId and uses the executor. Append and parent/root linkage are guarded; dispatcher and replay scope by CompanyId. |
| 3 | **Operational observability dashboard** | **VERIFIED** | PlatformAnalyticsController (SuperAdmin/Admin) and ObservabilityController exist. TenantMetricsAggregationHostedService and platform analytics (dashboard, tenant-health, anomalies, drift) aggregate per-tenant metrics. Dashboard and tenant-health endpoints are for platform admins and do not expose one tenant’s raw data to another; they list tenants and per-tenant health. No evidence of cross-tenant data leak in observability paths. |
| 4 | **SI-App workflow hardening** | **PARTIAL** | SI-app API requires companyId for device scan recording and throws if missing; backend SI-app routes are under `/companies/{companyId}/si-app/...` so tenant is in the path. Docs reference SI_APP_WORKFLOW_HARDENING_REPORT and SI_OPERATIONAL_INTELLIGENCE_DATA_INVENTORY. However, SI-app frontend comments still say “single-company mode, companyId may be null” and some backend services (e.g. workflow, payroll, parser) that SI flows may depend on still have Guid.Empty branches. So SI **API** is tenant-scoped by URL, but **dependencies** (workflow, payroll, parser) are not fully hardened. |

---

**End of report. No files were modified except this document.**
