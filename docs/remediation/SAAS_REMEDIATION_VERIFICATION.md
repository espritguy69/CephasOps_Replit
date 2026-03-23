# SaaS Remediation Verification

**Date:** 2026-03-13  
**Scope:** Verification of tenant-safe behavior after SaaS remediation pass.

---

## What was checked

1. **Tenant context resolution**  
   - TenantGuardMiddleware blocks requests when `GetEffectiveCompanyIdAsync()` yields no tenant.  
   - Program.cs sets `TenantScope.CurrentTenantId` from `ITenantProvider.CurrentTenantId` after the guard.  
   - No change; behavior confirmed.

2. **Tenant-owned reads**  
   - OrderService.GetOrderByIdAsync: when companyId is null, global query filter (TenantScope) still applies; comment updated for clarity.  
   - FileService: DownloadFileAsync, DeleteFileAsync, GetFileMetadataAsync already scoped by companyId. GetFileContentAsync now takes optional companyId and resolves effective company (companyId ?? TenantScope.CurrentTenantId); returns null when no company context.

3. **Tenant-owned writes**  
   - Order create, file upload, and other create paths stamp CompanyId. No change; verified.

4. **Cross-tenant query safety**  
   - IgnoreQueryFilters usages reviewed: all either (a) explicitly constrain by CompanyId in the same query, or (b) run under documented platform bypass (e.g. EventPlatformRetentionService, BackgroundJobProcessorService).  
   - No unconstrained cross-tenant reads identified.

5. **Dashboard / aggregates**  
   - OperationsOverviewService uses scopeCompanyId: null for platform-wide admin dashboard. Intentional; no change.

6. **Background jobs / hosted services**  
   - JobExecutionWorkerHostedService, BackgroundJobProcessorService, EventStoreDispatcherHostedService, EventReplayService, and other schedulers use TenantScopeExecutor (tenant scope or platform bypass) as documented.  
   - TenantMetricsAggregationHostedService now runs TenantMetricsAggregationJob inside `TenantScopeExecutor.RunWithPlatformBypassAsync` so platform-wide aggregation is explicit and any future tenant-scoped writes would not trigger TenantSafetyGuard incorrectly.

7. **File / document access**  
   - GetFileContentAsync: now tenant-safe; when companyId is not provided, uses TenantScope.CurrentTenantId; when neither is set, returns null and logs. Callers (ParserReplayService, EmailIngestionService, BillingController) updated to pass companyId where available. CarboneRenderer calls with null (relies on request-scoped TenantScope).  
   - GetFileInfoAsync (second pass): now tenant-safe; same pattern as GetFileContentAsync (optional companyId, effective company from companyId ?? TenantScope.CurrentTenantId, return null when no context, query scoped by CompanyId). Single caller (CarboneRenderer) passes null; request scope applies.

8. **Platform-wide bypasses retained**  
   - As per TENANT_SAFETY_DEVELOPER_GUIDE and TENANT_SCOPE_EXECUTOR_COMPLETION: DatabaseSeeder, ApplicationDbContextFactory, retention services, schedulers (platform enumeration), provisioning, webhook/event with no company, job reap, and now TenantMetricsAggregationHostedService (platform-wide metrics aggregation). All documented.

---

## What was fixed

| Item | Fix |
|------|-----|
| FileService.GetFileContentAsync | Added optional `Guid? companyId`; resolve effective company from `companyId ?? TenantScope.CurrentTenantId`; if empty return null; query with `f.CompanyId == effectiveCompanyId`. |
| GetFileContentAsync callers | ParserReplayService, EmailIngestionService, BillingController pass companyId where available. CarboneRenderer passes null (request scope). |
| TenantMetricsAggregationHostedService | Wrapped job execution in `TenantScopeExecutor.RunWithPlatformBypassAsync`. |
| OrderService.GetOrderByIdAsync | Comment clarified: when companyId is null, global filter still applies (TenantScope from middleware). |
| Command idempotency (CommandProcessingLog) | IdempotencyBehavior prefixes key with `{CompanyId:N}:` when TenantScope.CurrentTenantId is set; platform operations use raw key. |
| External idempotency (inbound webhooks) | ExternalIdempotencyStore scopes by (ConnectorKey, CompanyId, IdempotencyKey); unique index and all lookups updated. |

---

## What remains uncertain

- **Frontend pages:** No automated check of frontend; manual QA recommended for pages that might show blank or wrong totals (orders, dashboards, reports).

---

## Manual QA checklist

- [ ] Log in as tenant user; confirm orders list and order detail load and show only that company’s data.  
- [ ] Log in as SuperAdmin; switch company via X-Company-Id (or UI); confirm data switches.  
- [ ] Upload and download file as tenant; confirm file is only visible within that company.  
- [ ] Run tenant metrics aggregation (or wait for daily run); confirm no errors and TenantMetricsDaily / TenantMetricsMonthly rows exist per tenant.  
- [ ] Operations overview (platform admin): confirm job counts, event store, payout health load.  
- [ ] Invoice PDF download: confirm PDF is generated and file content is returned for the correct company.

---

## Platform-wide bypasses retained (explicit list)

- DatabaseSeeder (bootstrap only).  
- ApplicationDbContextFactory.CreateDbContext (design-time).  
- EventPlatformRetentionService (platform-wide retention).  
- NotificationRetentionService (when companyId null).  
- Schedulers that enumerate tenants: EmailIngestionSchedulerService, PnlRebuildSchedulerService, LedgerReconciliationSchedulerService, StockSnapshotSchedulerService, MissingPayoutSnapshotSchedulerService, PayoutAnomalyAlertSchedulerService (RunWithPlatformBypassAsync for enumeration).  
- CompanyProvisioningService.  
- InboundWebhookRuntime when request.CompanyId is null.  
- EventStoreDispatcherHostedService / EventReplayService when entry.CompanyId is null.  
- BackgroundJobProcessorService (reap path).  
- TenantMetricsAggregationHostedService (platform-wide aggregation of TenantUsageRecords into TenantMetricsDaily/Monthly).  
- PlatformAnalyticsService, PerformanceWatchdogService, TenantAnomalyDetectionService (platform admin).  
- AuthService (platform bypass for user-by-email lookup, refresh token, etc.).

---

## Tenant-owned areas corrected (explicit list)

- **FileService.GetFileContentAsync:** Now requires effective company (parameter or TenantScope); returns null when missing; query explicitly scoped by CompanyId.  
- **TenantMetricsAggregationHostedService:** Execution boundary documented and wrapped in RunWithPlatformBypassAsync so platform-wide intent is explicit and guard behavior is consistent.  
- **FileService.GetFileInfoAsync (second pass):** Same tenant-safe pattern as GetFileContentAsync; no file metadata returned without effective company context.  
- **OrdersController (manual QA):** GetOrders and GetOrdersPaged now pass `_tenantProvider.CurrentTenantId` so orders list and paged list are explicitly tenant-scoped; SuperAdmin company switch applies to both.  
- **RatesController GetRateCard:** RequireCompanyId and explicit `.Where(rc => rc.CompanyId == companyId)` so rate card detail is explicitly tenant-scoped.  
- **Frontend (SuperAdmin company switch):** API client sends X-Company-Id when a company-ID getter is set; DepartmentContext sets the getter from activeDepartment?.companyId ?? departments[0]?.companyId so settings, rates, files, and reports all use the same effective company when SuperAdmin selects a department.

---

## Second-pass remediation findings (2026-03-13)

### Fixed now

| Item | Fix |
|------|-----|
| **GetFileInfoAsync** | Added optional `Guid? companyId = null`. Resolve effective company from `companyId ?? TenantScope.CurrentTenantId`; if empty return null; query with `f.Id == fileId && f.CompanyId == effectiveCompanyId.Value`. Prevents file metadata leak when called without tenant scope (e.g. from job). CarboneRenderer continues to call with null (request scope). |

### Safe to defer

| Item | Reason |
|------|--------|
| **DiagnosticsController (check-seeding)** | Platform diagnostic endpoint (no [Authorize]; /api/diagnostics/check-seeding). Queries Users by email, Departments by Name "GPON", and counts on OrderTypes/OrderCategories/etc. When no tenant scope (e.g. unauthenticated or health check), global filter allows all rows. Intentional for “is DB seeded?” check. CEPHAS004 on Departments is by Name, not Id. No change; document as platform diagnostic. |
| **RatesController FindAsync (CEPHAS004)** | PartnerGroups, Partners, OrderTypes, OrderCategories, GponSiJobRates, ServiceInstallers use FindAsync(id). FindAsync is subject to EF global query filter, so in tenant-scoped requests only current-tenant entities are returned. Risk is low; adding explicit CompanyId to every lookup would require threading _tenantProvider.CurrentTenantId through many methods. Defer as defense-in-depth improvement; no unsafe cross-tenant load in normal flows. |
| **Idempotency key tenant prefix** | Already documented; optional follow-up. |

### Manual QA required

| Area | What to verify |
|------|----------------|
| **Order detail page** | Frontend calls GET /orders/{id} without companyId; backend uses _tenantProvider.CurrentTenantId. Confirm as tenant user: order list and detail show only current company; 404 for other-company order ID. |
| **Rates pages** | Rate designer, GPON rates list/detail: confirm list and detail load and are scoped to current company (global filter applies). |
| **File list / download** | Confirm file list and download are company-scoped; no files from other tenants. |
| **Settings pages (companyId fallback)** | GuardConditionDefinitions, BusinessHours, ApprovalWorkflows use “Single-company mode: fallback to first department’s companyId”. Confirm correct company is used when multiple departments/companies exist (e.g. SuperAdmin with X-Company-Id). |
| **Reports / exports** | Orders list report, materials report: confirm data and exports are tenant-scoped. |

---

## Manual QA–driven fixes (orders list tenant scoping)

**Issue reproduced:** Orders list (GET /orders) and paged orders list (GET /orders/paged) could rely only on the EF global query filter instead of explicit tenant scoping, because the controller passed `companyId = null` (GetOrders) or set companyId only when profitability/alert flags were true (GetOrdersPaged).

**Root cause:** Backend. Legacy comment "Company feature removed - companyId is always null" and conditional use of `_tenantProvider.CurrentTenantId` only for enrichment in GetOrdersPaged.

**Fix applied:**

- **OrdersController.GetOrders:** Use `companyId = _tenantProvider.CurrentTenantId` instead of `(Guid?)null`, so the service layer explicitly filters by company (consistent with GetOrder detail and with SuperAdmin X-Company-Id behavior).
- **OrdersController.GetOrdersPaged:** Use `companyId = _tenantProvider.CurrentTenantId` always; profitability/alert flags only control whether response is enriched with RevenueAmount/PayoutAmount/ProfitAmount and alert fields, not whether the query is tenant-scoped.

**Result:** Orders list and paged list are now explicitly tenant-scoped in the application layer; SuperAdmin company switch (X-Company-Id) correctly limits both endpoints to the selected company.

---

## Manual QA–driven fixes (rates detail, settings fallback, SuperAdmin switch)

**1. Rates detail explicit tenant scoping**

- **Symptom:** Rate card detail (GET /api/rates/ratecards/{id}) was queried by Id only; CEPHAS004 and defense-in-depth called for explicit company scope.
- **Root cause:** Backend. GetRateCard did not call RequireCompanyId and did not filter by CompanyId.
- **Fix:** Backend. RequireCompanyId added; query now uses `.Where(rc => rc.Id == id && rc.CompanyId == companyId)` so rate card detail is explicitly tenant-scoped.

**2. Settings pages company fallback and SuperAdmin company switch**

- **Symptom:** SuperAdmin could not consistently “switch company” for settings, rates, files, and reports because the frontend did not send X-Company-Id; backend only uses that header when provided.
- **Root cause:** Frontend. API client did not send X-Company-Id; effective company from department context (activeDepartment?.companyId || departments[0]?.companyId) was only used for queryKey/enabled on settings pages, not for the request.
- **Fix:** Frontend. API client now supports an optional company-ID getter; when set, X-Company-Id is sent on every request. DepartmentContext sets the getter to the current effective company (activeDepartment?.companyId ?? departments[0]?.companyId). Backend TenantProvider already prefers X-Company-Id for SuperAdmin, so selecting a department (or having a first department) now drives the effective company for all API calls. Settings pages (Guard conditions, Business hours, Approval workflows) already use the same fallback for queryKey; now that value is also sent as X-Company-Id so SuperAdmin company switch is consistent across settings, rates, files, and reports.

---

## Manual QA checklist (backend + frontend)

### Backend

- [ ] GET /orders (list): as tenant user, only current company’s orders; as SuperAdmin with X-Company-Id, only that company’s orders.
- [ ] GET /orders/{id}: as tenant user, 200 for own-company order and 404 for other-company order ID; SuperAdmin with X-Company-Id sees only that company.
- [ ] File upload/download: file visible only for the company that uploaded it; GET /files and download scoped by company.
- [ ] GET /api/rates/* (rate cards, GPON rates): list and detail return only current tenant’s data (global filter applied).
- [ ] Tenant metrics aggregation: hosted service runs without TenantSafetyGuard errors; TenantMetricsDaily/Monthly populated per tenant.
- [ ] Operations overview (platform): job counts, event store, payout health load when called with platform scope.
- [ ] Document generation (invoice PDF, Carbone): template and generated file load with correct company context; no cross-tenant file metadata from GetFileInfoAsync/GetFileContentAsync.

### Frontend

- [ ] Order list page: loads and shows only current company’s orders; no blank list when tenant context is set.
- [ ] Order detail page: loads for valid order ID in current company; 404 or error for other-company order ID; department context does not break load.
- [ ] Rates / Rate designer: list and detail load; data matches current company (no wrong-tenant rates).
- [ ] File list and file download: only current company’s files; download works and returns correct file.
- [ ] Settings pages (Guard conditions, Business hours, Approval workflows): companyId fallback (e.g. first department’s companyId) shows correct company’s data when multiple departments exist.
- [ ] Reports hub / exports: orders list and materials reports are tenant-scoped; export CSV/Excel contains only current company data.
- [ ] SuperAdmin: switching company (e.g. X-Company-Id or UI) updates orders, rates, and files to selected company; no mixed data.
