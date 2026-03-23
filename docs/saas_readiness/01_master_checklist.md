# SaaS Readiness Master Checklist

**Date:** 2026-03-13

Practical, execution-oriented checklist covering all major CephasOps application surfaces for SaaS readiness. Use for tracking progress; tie to [02_manual_uat_plan.md](02_manual_uat_plan.md) and [05_execution_order.md](05_execution_order.md).

---

## 1. Tenant isolation

| # | Item | Evidence / Notes |
|---|------|------------------|
| 1.1 | API requests without valid tenant (no JWT company / no department fallback) are blocked with 403 | TenantGuardMiddleware blocks; no business logic runs |
| 1.2 | List endpoints return only current tenant’s data (orders, buildings, SIs, materials, invoices, etc.) | Verify per controller: Orders, Buildings, ServiceInstallers, Inventory, Billing, etc. |
| 1.3 | Detail-by-ID endpoints return 404 when resource belongs to another tenant | e.g. GET /api/orders/{id}, GET /api/invoices/{id} with another company’s ID |
| 1.4 | Search and autocomplete are scoped to current tenant | Orders search, building/partner autocomplete, material search |
| 1.5 | Reports and exports contain only current tenant data; companyId from ITenantProvider only (no query override) | ReportsController uses _tenantProvider.CurrentTenantId; department scope enforced |
| 1.6 | File/document access is tenant-scoped (uploads, downloads, templates) | FilesController, DocumentsController, DocumentTemplatesController |
| 1.7 | Cross-tenant ID probing returns 404 or 403, never data | Explicit tests for GET with other-tenant IDs |
| 1.8 | SuperAdmin X-Company-Id override is optional and only for support; normal users cannot override | TenantProvider resolution order; no companyId in query for reports |

---

## 2. Single-tenant regression compatibility

| # | Item | Evidence / Notes |
|---|------|------------------|
| 2.1 | One tenant can complete full order lifecycle: draft → assigned → OnTheWay → MetCustomer → OrderCompleted → DocketsReceived → ReadyForInvoice → Invoiced → Completed | E2E or manual run per [08_single_tenant_regression_plan.md](08_single_tenant_regression_plan.md) |
| 2.2 | Assignment and scheduling work per tenant (slots, SI availability, calendar) | SchedulerController, SchedulerService; department + company scope |
| 2.3 | SI app shows only assigned jobs for that SI in that company; status transitions apply to correct order | SiAppController, getAssignedJobs(companyId, siId) |
| 2.4 | Blockers and reschedules create correct status and audit trail per tenant | Workflow transitions; EventStore entry CompanyId |
| 2.5 | Docket verification and upload flow work; dockets tied to correct order/tenant | OrderDocket, Docket verification workflow |
| 2.6 | Invoice generation and MyInvois submission use tenant’s data only; no cross-tenant invoice | BillingController, Invoice services, MyInvois job CompanyId |
| 2.7 | Payroll and P&L reflect only that tenant’s orders and rates | PayrollController, PnlController; company-scoped queries |
| 2.8 | Dashboards and KPIs show only current tenant data | Reports, operational overview scoped by company/department |

---

## 3. Tenant onboarding flow

| # | Item | Evidence / Notes |
|---|------|------------------|
| 3.1 | New company/tenant can be provisioned (TenantProvisioningController / Company provisioning) | CompanyProvisioningService under platform bypass |
| 3.2 | Provisioned tenant gets default settings, roles, and department structure where applicable | Seeding or provisioning logic |
| 3.3 | First user for new tenant can log in and receive JWT with correct companyId | AuthController; User.CompanyId or department→company resolution |
| 3.4 | New tenant can configure email accounts and parser templates without affecting others | EmailAccountsController, ParserTemplatesController; CompanyId on entities |
| 3.5 | New tenant can create orders, buildings, SIs, and run full flow without seeing other tenants’ data | Full UAT with new tenant |

---

## 4. Multi-tenant operational stability

| # | Item | Evidence / Notes |
|---|------|------------------|
| 4.1 | Two or more tenants active simultaneously; no cross-tenant data in lists or reports | Parallel UAT with Tenant A and Tenant B |
| 4.2 | Background jobs for Tenant A do not read/write Tenant B data | BackgroundJobProcessorService uses job.CompanyId; TenantScopeExecutor per job |
| 4.3 | Event dispatch and replay use event’s CompanyId for scope | EventStoreDispatcherHostedService, EventReplayService: RunWithTenantScopeOrBypassAsync(entry.CompanyId) |
| 4.4 | Schedulers that enumerate tenants (email ingestion, P&L rebuild, ledger reconciliation, etc.) run per-tenant work under correct scope | EmailIngestionSchedulerService, PnlRebuildSchedulerService use executor; SlaEvaluationSchedulerService per company |
| 4.5 | Webhook ingestion with company context runs in that tenant’s scope | InboundWebhookRuntime: RunWithTenantScopeOrBypassAsync(request.CompanyId) |
| 4.6 | No single-tenant assumption in shared caches or static state | Review in-memory caches, static dictionaries; ensure keyed by company where needed |

---

## 5. Permissions and admin boundaries

| # | Item | Evidence / Notes |
|---|------|------------------|
| 5.1 | RBAC permissions (e.g. orders.view, reports.export) apply within current tenant only | PermissionAuthorizationHandler + department scope; no cross-tenant bypass |
| 5.2 | Department-scoped endpoints restrict to user’s department memberships within current tenant | ResolveDepartmentScopeOrFailAsync; DepartmentAccessService filtered by tenant |
| 5.3 | Admin/super-admin actions (user management, tenant provisioning) are permission-gated; SuperAdmin can switch context via X-Company-Id where designed | AdminUsersController, TenantProvisioningController; audit where required |
| 5.4 | SI app users see only their company’s data and only their assigned jobs | SiAppController and SI app API pass companyId/siId |

---

## 6. Background jobs and automation

| # | Item | Evidence / Notes |
|---|------|------------------|
| 6.1 | Every job execution runs under correct tenant scope (job.CompanyId or payload company) | BackgroundJobProcessorService: RunWithTenantScopeOrBypassAsync(effectiveCompanyId, …) |
| 6.2 | Platform-wide operations (reap, retention, scheduler enumeration) use RunWithPlatformBypassAsync and do not mix tenant data in one transaction | ReapStaleRunningJobsAsync; retention services; see [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md) |
| 6.3 | Job enqueue path sets job.CompanyId from current context (API or scheduler) | Enqueue call sites set CompanyId when enqueuing tenant work |
| 6.4 | Idempotency: duplicate job runs do not double-apply side effects (notifications, P&L, invoice submit) | Per job type; see architecture guardrails |
| 6.5 | Job observability (JobRun, dashboard) shows tenant where applicable; no cross-tenant leak in logs | JobRunRecorder, SystemWorkersController, ObservabilityController |

---

## 7. Notifications and integrations

| # | Item | Evidence / Notes |
|---|------|------------------|
| 7.1 | Notifications are created and dispatched in tenant scope; no cross-tenant recipient visibility | NotificationService, NotificationDispatchRequestService; CompanyId required for tenant-owned create |
| 7.2 | Notification retention runs per-tenant or under explicit bypass | NotificationRetentionService: RunWithTenantScopeOrBypassAsync(companyId, …) |
| 7.3 | Outbound integrations (MyInvois, partner APIs) use tenant’s credentials and data only | Connector endpoints, invoice submission per company |
| 7.4 | Inbound webhooks with company context are processed in that company’s scope | InboundWebhookRuntime; request.CompanyId |

---

## 8. Data visibility (dashboards, reports, search, exports)

| # | Item | Evidence / Notes |
|---|------|------------------|
| 8.1 | Report definitions are global; report *run* uses current tenant + department scope only | ReportsController: companyId from _tenantProvider; ResolveDepartmentScopeAsync |
| 8.2 | Exports (orders-list, stock-summary, ledger, materials-list, scheduler-utilization) are tenant- and department-scoped; format csv/xlsx/pdf | All export actions use _tenantProvider.CurrentTenantId and resolvedDeptId |
| 8.3 | Dashboard and KPI data (orders by status, utilization, P&L) are tenant-scoped | Data sources pass companyId from tenant context |
| 8.4 | Search (orders, materials, buildings, partners) respects global query filter / tenant scope | No IgnoreQueryFilters without AssertTenantContext or documented bypass |
| 8.5 | Audit and history views (order history, event ledger) show only current tenant’s records | EventStore, ledger, audit log filtered by company |

---

## 9. Workflow correctness

| # | Item | Evidence / Notes |
|---|------|------------------|
| 9.1 | Workflow transitions execute in tenant context; event store entries carry CompanyId | WorkflowEngineService; EventStore append with scope |
| 9.2 | Side effects (notifications, job enqueue, document generation) are enqueued with same tenant | Handlers use current TenantScope or event CompanyId when enqueuing |
| 9.3 | SiWorkflowGuard and FinancialIsolationGuard enforce rules within tenant (no cross-tenant bypass) | Guards used in workflow and billing paths |
| 9.4 | Allowed transitions and guard conditions are evaluated against correct tenant’s data | Workflow definitions and guard conditions company-scoped |

---

## 10. Performance and concurrency

| # | Item | Evidence / Notes |
|---|------|------------------|
| 10.1 | Under load with multiple tenants, response times and success rates are acceptable | Load test with 2+ tenants; no tenant A slowdown due to tenant B |
| 10.2 | No cross-tenant lock contention (e.g. global locks that block all tenants) | Review locking; prefer tenant-scoped or per-entity locks |
| 10.3 | Background job processing does not starve one tenant’s jobs when another has many | Job queue fairness or per-tenant visibility; worker processes jobs from multiple tenants correctly scoped |
| 10.4 | Database queries use indexes and tenant filter (CompanyId) to avoid full scans | Key list/detail queries have CompanyId in filter |

---

## Sign-off

- **Tenant isolation:** _______________  
- **Single-tenant regression:** _______________  
- **Tenant onboarding:** _______________  
- **Multi-tenant stability:** _______________  
- **Permissions / admin:** _______________  
- **Background jobs:** _______________  
- **Notifications / integrations:** _______________  
- **Data visibility:** _______________  
- **Workflow correctness:** _______________  
- **Performance / concurrency:** _______________

**Overall SaaS readiness (per [10_go_no_go_criteria.md](10_go_no_go_criteria.md)):** Go / No-Go / Conditional
