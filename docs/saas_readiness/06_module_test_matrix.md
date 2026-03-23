# Module-Specific SaaS Test Matrix

**Date:** 2026-03-13

Maps CephasOps modules to SaaS test coverage: risk, manual tests, automated tests, likely failure modes, and release-blocking severity. Use with [01_master_checklist.md](01_master_checklist.md) and [04_automated_test_scenarios.md](04_automated_test_scenarios.md).

---

## 1. Auth / Users / Roles

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | JWT or resolution missing companyId; user in multiple companies gets wrong tenant; role/permission applied across tenants; **user list returns all tenants** (fixed 2026-03-13). |
| **Manual tests** | Login as user A and B; verify JWT companyId; switch company (SuperAdmin only); attempt access without company (403); list users as A → only A's users. |
| **Automated tests** | Auth: JWT contains companyId; TenantGuard blocks when no company; SuperAdmin X-Company-Id override; non-SuperAdmin ignores override; **UsersController list/detail/by-department scoped to tenant**. |
| **Likely failure modes** | Department fallback ambiguous (user in 2 companies); JWT not including company_id; RequireCompanyId not used on sensitive endpoints. |
| **Release blocking** | Yes — auth and tenant resolution are foundational. |

---

## 2. Companies / Tenants / Provisioning

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Provisioning creates company with wrong or shared data; tenant list or company list leaks operational data across tenants. |
| **Manual tests** | Create new company via TenantProvisioningController/UI; log in as new tenant user; verify isolation from existing tenants. |
| **Automated tests** | Provisioning runs under platform bypass; new company has own CompanyId; list companies (admin) returns only intended scope. |
| **Likely failure modes** | Default settings or seed data copied from wrong tenant; CompanyId not set on provisioned entities. |
| **Release blocking** | High — onboarding must work and stay isolated. |

---

## 3. Orders

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | List returns all tenants’ orders; detail-by-ID returns another tenant’s order; search/export mixes tenants. |
| **Manual tests** | List orders as A and B; GET order by B’s id as A → 404; run order report and export; full lifecycle in one tenant. |
| **Automated tests** | Two-tenant list isolation; detail 404 for other-tenant ID; report/export scope; workflow transition sets event CompanyId. |
| **Likely failure modes** | IgnoreQueryFilters without AssertTenantContext; service method not receiving companyId; order search without tenant filter. |
| **Release blocking** | Yes — orders are core; list/detail/report/export must be isolated. |

---

## 4. Parser / Email ingestion

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Email account or parse session from tenant B creates drafts in tenant A; ingestion job runs without scope. |
| **Manual tests** | Configure email account for tenant A; ingest; verify drafts and sessions have CompanyId = A; tenant B cannot see A’s drafts. |
| **Automated tests** | Email ingestion job runs with correct CompanyId per account; parser creates drafts with session’s company; list drafts scoped. |
| **Likely failure modes** | EmailAccount or ParseSession not company-scoped; ingestion scheduler not setting scope per account/company. |
| **Release blocking** | High — parser is critical for order intake. |

---

## 5. Scheduler

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Calendar or slot list returns another tenant’s slots/SIs; assignment affects wrong tenant. |
| **Manual tests** | View calendar as A and B; assign order to SI in A; verify only A’s slots and SIs. |
| **Automated tests** | GetCalendarAsync(companyId, …) returns only that tenant’s data; scheduler-utilization report scoped. |
| **Likely failure modes** | SchedulerService method not filtering by companyId; department scope not within tenant. |
| **Release blocking** | High — scheduling is core to operations. |

---

## 6. SI app

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | SI sees another tenant’s jobs; transition applied to another tenant’s order; job list or detail leaks B’s data. |
| **Manual tests** | SI A login; list only A’s assigned jobs; transition; try B’s order ID → 404. SI B same. |
| **Automated tests** | getAssignedJobs(companyId, siId) returns only that company’s assigned jobs; GET order detail for B’s order as A SI → 404. |
| **Likely failure modes** | SiAppController or service not passing companyId; order transition not validating order’s company. |
| **Release blocking** | Yes — SI app is daily operations. |

---

## 7. Inventory / Ledger / Materials

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Materials list, stock, or ledger returns another tenant’s data; movement or allocation writes to wrong tenant; warehouse detail/mutations cross-tenant (fixed 2026-03-13). |
| **Manual tests** | List materials, stock summary, ledger, warehouses as A and B; export; verify scope. |
| **Automated tests** | Two-tenant list isolation for materials, ledger, warehouses; WarehousesController GetById/Update/Delete/Create tenant-checked; export stock-summary/ledger scoped; file/ID access 404 for other tenant. |
| **Likely failure modes** | Inventory or ledger query without companyId; StockLedgerService not receiving company; Warehouse not CompanyScopedEntity — controller now enforces. |
| **Release blocking** | High — inventory and ledger are financial and operational. |

---

## 8. Billing / Invoices / MyInvois

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Invoice list or detail from another tenant; MyInvois submission with wrong tenant’s credentials or data. |
| **Manual tests** | List invoices as A and B; GET invoice by B’s id as A → 404; generate and submit invoice for A only. |
| **Automated tests** | Invoice list and detail isolation; invoice generation and submission job use order’s CompanyId. |
| **Likely failure modes** | Billing service or controller not filtering by company; MyInvois job enqueued without CompanyId. |
| **Release blocking** | Yes — financial and compliance. |

---

## 9. Payroll / Rates

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Payroll run or rate card includes another tenant’s orders or rates; P&L rebuild writes wrong tenant. |
| **Manual tests** | Run payroll for tenant A; verify only A’s SIs and orders; run P&L for A and B separately. |
| **Automated tests** | Payroll and P&L services receive companyId and filter; P&L rebuild job runs under TenantScopeExecutor with companyId. |
| **Likely failure modes** | Payroll/Pnl service not scoped; job.CompanyId not set when enqueuing rebuild. |
| **Release blocking** | High — payroll and P&L are financial. |

---

## 10. P&L

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | P&L report or rebuild includes another tenant’s facts; dashboard shows mixed data. |
| **Manual tests** | View P&L for A and B; trigger rebuild for A; verify only A’s data. |
| **Automated tests** | PnlController and PnlRebuildService scoped; PnlRebuildSchedulerService uses RunWithPlatformBypassAsync for enumeration and RunWithTenantScopeAsync (or job) per company. |
| **Likely failure modes** | Rebuild job or scheduler not setting scope per company; P&L query without company filter. |
| **Release blocking** | High. |

---

## 11. Reports / Dashboards / Exports

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Report run or export returns another tenant’s data; dashboard aggregates wrong tenant; companyId override via query. |
| **Manual tests** | Run and export orders-list, stock-summary, ledger, materials-list, scheduler-utilization as A and B; verify scope. |
| **Automated tests** | Report run and export integration tests; companyId from _tenantProvider only (no query param override). |
| **Likely failure modes** | Report data path ignores tenant; department scope resolves to other tenant’s department. |
| **Release blocking** | Yes — reports and exports are high-visibility and high-risk. |

---

## 12. Notifications / Messaging

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Notification created for wrong tenant or dispatched to wrong users; retention deletes wrong tenant’s data. |
| **Manual tests** | Trigger notification in tenant A; verify recipient and content; retention run does not affect B. |
| **Automated tests** | NotificationService create with null company skips or fails; NotificationRetentionService uses RunWithTenantScopeOrBypassAsync(companyId). |
| **Likely failure modes** | Create notification without CompanyId; retention runs without per-tenant scope. |
| **Release blocking** | High — notifications are user-facing and can leak. |

---

## 13. Workflow engine / Rules / Automation

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Transition or side effect runs in wrong scope; event store entry has wrong CompanyId; automation rule triggers cross-tenant. |
| **Manual tests** | Execute transitions in tenant A; verify event store and side effects (notification, job) for A only. |
| **Automated tests** | Transition sets event CompanyId; OrderAssignedOperationsHandler enqueues job with CompanyId; event dispatch uses entry.CompanyId. |
| **Likely failure modes** | Handler or side effect not using current scope or event CompanyId; IgnoreQueryFilters in workflow without bypass. |
| **Release blocking** | High. |

---

## 14. Background jobs

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Job runs without scope or with wrong CompanyId; platform job mixes tenant data; reap affects wrong tenant. |
| **Manual tests** | Trigger tenant-scoped job (P&L, email ingest); verify scope in logs/observability; reap does not corrupt tenant data. |
| **Automated tests** | ProcessJobAsync uses RunWithTenantScopeOrBypassAsync(job.CompanyId); enqueue sets CompanyId; reap uses RunWithPlatformBypassAsync only for state. |
| **Likely failure modes** | job.CompanyId null when should be set; new job type enqueued without CompanyId. |
| **Release blocking** | Yes — jobs drive async operations. |

---

## 15. Files / Documents / Templates

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | File or document download returns another tenant’s file; template or generated document uses wrong tenant data. |
| **Manual tests** | Upload file for tenant A; as A download; as A try B’s file ID → 404. Generate document for A. |
| **Automated tests** | FilesController/DocumentsController detail 404 for other-tenant ID; template resolution uses current tenant. |
| **Likely failure modes** | File lookup without company filter; template or document generation not scoped. |
| **Release blocking** | High — documents can contain PII/financial data. |

---

## 16. Admin / Settings / Diagnostics

| Aspect | Detail |
|--------|--------|
| **SaaS risk** | Admin list (users, companies) or diagnostic endpoint returns operational data for all tenants in a way that leaks; settings change affects wrong tenant. |
| **Manual tests** | Admin: list companies/tenants (intended); list users with company filter; run diagnostics for current tenant only where applicable. |
| **Automated tests** | Admin endpoints require permission; tenant-scoped operational lists (e.g. orders) never return all tenants without explicit admin intent. |
| **Likely failure modes** | Diagnostic or health endpoint returns tenant-specific data without scope; settings CRUD without company. |
| **Release blocking** | Medium — admin must not weaken isolation. |

---

## Summary table

| Module | SaaS risk level | Release blocking |
|--------|----------------|------------------|
| Auth / Users / Roles | Critical | Yes |
| Companies / Tenants | High | High |
| Orders | Critical | Yes |
| Parser / Email ingestion | High | High |
| Scheduler | High | High |
| SI app | Critical | Yes |
| Inventory / Ledger | High | High |
| Billing / Invoices / MyInvois | Critical | Yes |
| Payroll / Rates | High | High |
| P&L | High | High |
| Reports / Dashboards / Exports | Critical | Yes |
| Notifications | High | High |
| Workflow / Automation | High | High |
| Background jobs | Critical | Yes |
| Files / Documents | High | High |
| Admin / Settings | Medium | Medium |
