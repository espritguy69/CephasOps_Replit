# High-Risk Conversion Risk Register — Single-Company to SaaS

**Date:** 2026-03-13

Focused list of the most dangerous areas when converting a single-company app into SaaS, prioritized by **severity** and **likelihood**. Grounded in CephasOps modules and common conversion failures.

---

## Severity definitions

- **Critical:** Data breach or cross-tenant data exposure; financial or compliance impact; makes SaaS launch unsafe.
- **High:** Incorrect tenant behavior (wrong data, wrong scope) that is detectable and fixable but blocks or risks launch.
- **Medium:** Degraded behavior or edge-case leakage; should be fixed before or soon after launch.
- **Low:** Minor or rare; acceptable with documented mitigation.

---

## 1. Critical severity

| Risk ID | Area | Description | Likelihood | Mitigation / validation |
|---------|------|-------------|------------|-------------------------|
| CR-1 | **List endpoints without tenant filter** | Any list (orders, buildings, SIs, invoices, materials, partners) returns all tenants’ data because CompanyId filter is missing or bypassed. | Medium (if EF global filter and TenantScope are correct, low) | All list APIs must go through services that use current tenant; run [07_tenant_isolation_attack_surface.md](07_tenant_isolation_attack_surface.md); integration tests with two tenants. **UsersController** (fixed 2026-03-13): now filters by User.CompanyId when not SuperAdmin. **WarehousesController** GetById/Update/Delete/Create now tenant-checked (Warehouse is not CompanyScopedEntity). |
| CR-2 | **Detail-by-ID returns another tenant’s entity** | GET /api/orders/{id} or similar returns 200 with another tenant’s order when ID is guessed or leaked. | Medium | Explicit tests: request with other-tenant ID must return 404 (or 403); never return body. **WarehousesController.GetById** and **UsersController.GetUser(id)** (fixed 2026-03-13): return 404 when resource belongs to another tenant. |
| CR-3 | **Reports or exports include other tenants’ data** | Report run or export (orders-list, ledger, stock-summary, materials, scheduler) uses wrong scope or ignores tenant. | Medium | ReportsController uses _tenantProvider.CurrentTenantId only (no query override); department scope via ResolveDepartmentScopeAsync; export tests with two tenants. |
| CR-4 | **Background job runs in wrong tenant scope** | Job executes with no scope or wrong CompanyId; writes/reads another tenant’s data (e.g. P&L, notifications, invoices). | Medium | BackgroundJobProcessorService uses RunWithTenantScopeOrBypassAsync(job.CompanyId ?? payload); all enqueue paths set job.CompanyId; see [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md). |
| CR-5 | **Event dispatch/replay with wrong scope** | Event handler or replay runs under wrong tenant; appends events or updates entities for another tenant. | Medium | EventStoreDispatcherHostedService and EventReplayService use RunWithTenantScopeOrBypassAsync(entry.CompanyId); regression suite in developer guide. |

---

## 2. High severity

| Risk ID | Area | Description | Likelihood | Mitigation / validation |
|---------|------|-------------|------------|-------------------------|
| HI-1 | **Search / autocomplete returns cross-tenant results** | Order search, building/partner/material autocomplete use query that bypasses tenant filter (e.g. IgnoreQueryFilters without AssertTenantContext). | Medium | Audit all search/autocomplete; ensure tenant in filter; add automated tests. |
| HI-2 | **Payroll or P&L includes another tenant’s orders** | Payroll run or P&L rebuild reads orders/rates from wrong company. | Medium | Payroll and Pnl services receive and use companyId; jobs run under TenantScopeExecutor; UAT with two tenants. |
| HI-3 | **Invoice/MyInvois submission with wrong tenant data** | Invoice generation or e-invoice submission uses another tenant’s credentials or partner data. | Low–Medium | Billing and MyInvois paths scoped by company; job CompanyId set when enqueuing. |
| HI-4 | **Notification sent to wrong tenant or with wrong data** | Notification created without CompanyId or dispatch targets users from another tenant. | Medium | NotificationService and NotificationDispatchRequestService require/fail on null company for tenant-owned create; skip with log when company null. |
| HI-5 | **SI app shows or updates another tenant’s jobs** | SI app list or detail returns Tenant B jobs for Tenant A SI; or transition applied to Tenant B order. | Medium | SiAppController and getAssignedJobs(companyId, siId); order transition validates order belongs to company. |
| HI-6 | **Department scope spans tenants** | Department-scoped endpoint (e.g. report run) allows resolving a department that belongs to another tenant. | Medium | ResolveDepartmentScopeAsync / DepartmentAccessService must restrict to current tenant’s departments. |
| HI-7 | **File/document access cross-tenant** | Download or view document by ID returns file from another tenant. | Medium | FilesController, DocumentsController: ensure tenant scope on lookup; test with other-tenant file ID. |

---

## 3. Medium severity

| Risk ID | Area | Description | Likelihood | Mitigation / validation |
|---------|------|-------------|------------|-------------------------|
| ME-1 | **Dashboard/KPI aggregates wrong tenant** | Dashboard or KPI endpoint uses unresolved or default company and shows mixed or wrong tenant. | Medium | All dashboard data sources take companyId from ITenantProvider/current context. |
| ME-2 | **Audit/history views show other tenant’s records** | Order history, event ledger, or audit log list returns entries from another tenant. | Medium | Event store and audit queries filtered by company/tenant. |
| ME-3 | **Parser/email ingestion creates drafts in wrong tenant** | Email ingestion job or parser assigns drafts to wrong CompanyId. | Medium | Email accounts and parse sessions are company-scoped; ingestion scheduler sets scope per account/company. |
| ME-4 | **Scheduler calendar or slots show wrong tenant** | Calendar or slot list returns another tenant’s slots or SIs. | Medium | SchedulerService.GetCalendarAsync(companyId, …); department within tenant. |
| ME-5 | **Retry/replay processes wrong tenant’s events** | Replay or retry operation dispatches event under wrong scope. | Low | EventReplayService uses entry.CompanyId; scope check before dispatch. |
| ME-6 | **Admin/support tool exposes all tenants in one list** | Admin list (e.g. companies, tenants) is acceptable; but operational lists (orders, users) must not mix tenants without explicit admin intent. | Low | Distinguish platform-admin (Tenant list) vs tenant-scoped (Orders list); RequireCompanyId and TenantGuard. |
| ME-7 | **Cache or in-memory state reused across tenants** | In-memory cache keyed by non-tenant key returns Tenant B data to Tenant A request. | Low | Review caches; key by companyId where data is tenant-specific. |

---

## 4. Lower severity (still track)

| Risk ID | Area | Description | Likelihood | Mitigation / validation |
|---------|------|-------------|------------|-------------------------|
| LO-1 | **Deep link / direct URL with other-tenant ID** | User bookmarks or is sent link with another tenant’s entity ID; must get 404/403, not data. | Medium | Same as detail-by-ID; E2E or manual test. |
| LO-2 | **Concurrency: one tenant’s load affects another** | Under load, Tenant A’s requests slow or fail because of Tenant B’s activity. | Low | Load test with 2+ tenants; monitor per-tenant metrics if available. |
| LO-3 | **Stale job reap marks wrong tenant’s job** | Reap logic updates jobs from all tenants (platform bypass); ensure only state update, no business data mix. | Low | ReapStaleRunningJobsAsync uses RunWithPlatformBypassAsync only for state update; no tenant data mixed. |

---

## 5. Summary for release gating

- **Release-blocking:** Any **Critical** (CR-1–CR-5) or **High** (HI-1–HI-7) that is **confirmed** (reproducible) must be fixed before declaring SaaS-ready, unless explicitly accepted with mitigation and sign-off.
- **Conditional go:** Remaining **High** or **Critical** with a documented workaround and short-term fix plan may be accepted for limited launch (e.g. single tenant first) with strict conditions.
- **Medium/Low:** Should be scheduled and fixed; do not block launch if no data leakage and workaround exists.

Use this register in conjunction with [01_master_checklist.md](01_master_checklist.md) and [10_go_no_go_criteria.md](10_go_no_go_criteria.md) for sign-off.
