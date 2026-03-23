# API Surface Summary (Controllers by Module)

**Related:** [04_api/API_OVERVIEW](../04_api/API_OVERVIEW.md) | [04_api/API_CONTRACTS_SUMMARY](../04_api/API_CONTRACTS_SUMMARY.md) | [Department & RBAC](../business/department_rbac.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Core operations

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Orders** | OrdersController | Order CRUD; list; status; assignment; filters (status, partner, SI, building, date, department). |
| **Scheduler** | SchedulerController | Calendar; slots; SI availability; leave; utilization; department-scoped. |
| **Parser** | ParserController | Parse sessions; drafts; approve/create order; email ingestion. |
| **Inventory** | InventoryController | Materials; ledger; stock summary; receive/transfer/allocate/issue/return; reports; export; department-scoped. |
| **Billing** | BillingController | Invoices CRUD; PDF/preview; line items; submission; company/department context. |
| **Buildings** | BuildingsController, BuildingTypesController, InstallationMethodsController, InstallationTypesController | Buildings; building types; installation methods/types; merge; department-scoped. |

---

## 2. Organisation & people

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Departments** | DepartmentsController | Department CRUD; export; department-scoped. |
| **Service installers** | ServiceInstallersController, SiAppController | SI CRUD; SI app endpoints; skills; department-scoped. |
| **Users** | UsersController | Users; auth context; department-scoped where applicable. |
| **Companies** | CompaniesController | Company (single-company; may be minimal). |
| **Partners** | PartnerGroupsController (and partner-related) | Partner groups; partners. |

---

## 3. Settings (department-scoped where applicable)

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Order config** | OrderTypesController, OrderCategoriesController, OrderStatusesController, OrderStatusChecklistController | Order types/categories/statuses; checklist. |
| **Rates** | BillingRatecardController, RatesController | Partner rates; SI rate plans; rate engine. |
| **Payroll** | PayrollController | Payroll periods; runs; earnings; import/export. |
| **Business config** | BusinessHoursController, EscalationRulesController, ApprovalWorkflowsController, AutomationRulesController, SlaProfilesController | Business hours; escalation; approvals; automation; SLA. |
| **Workflow** | WorkflowDefinitionsController, GuardConditionDefinitionsController, SideEffectDefinitionsController | Workflow definitions; guards; side effects. |
| **Templates & docs** | DocumentTemplatesController, MaterialTemplatesController, EmailTemplatesController, ParserTemplatesController | Document/material/email/parser templates. |
| **Other settings** | KpiProfilesController, TimeSlotSettingsPage-related, SplitterTypesController, SplittersController, GlobalSettingsController, IntegrationSettingsController | KPI; time slots; splitters; global/integration settings. |

---

## 4. Reports & P&L

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Reports** | ReportsController, ReportDefinitionsController | Report definitions; run by key; export (CSV/XLSX/PDF); department-scoped. |
| **P&L** | PnlController | P&L summary; drilldown; orders; overheads; department-scoped. |

---

## 5. Other

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Tasks** | TasksController | Task CRUD; Kanban; department-scoped. |
| **Assets** | AssetsController | Assets; depreciation; maintenance. |
| **RMA** | RMAController | RMA requests and items. |
| **Files & docs** | FilesController, DocumentsController, BinsController | File storage; documents; bins. |
| **Notifications** | NotificationsController | In-app notifications; see also §6 for event-driven dispatch. |
| **Email** | EmailAccountsController, EmailsController | Email accounts; inbox/viewer. |
| **Messaging** | WhatsAppController, SmsController, MessagingController, SmsTemplatesController, WhatsAppTemplate-related | WhatsApp; SMS; templates. |
| **Background jobs** | BackgroundJobsController | Recent jobs; summary; trigger (if any). |
| **Admin user management** | AdminUsersController | List/create/update users; activate/deactivate; set roles; reset password; department memberships in create/edit. Sensitive actions audited (IAuditLogService). SuperAdmin/Admin only; route `api/admin/users`. |
| **Admin / health** | AdminController, DiagnosticsController, InfrastructureController | Health; diagnostics; system info. |
| **Logs** | LogsController | Audit logs. |
| **Invoice submission** | InvoiceSubmissionsController | Invoice submission history (MyInvois). |

---

## 6. Eventing, operational & observability

| Module | Controllers | Purpose |
|--------|-------------|---------|
| **Event store** | EventStoreController, EventLedgerController, EventsController | Event store append/query; event ledger; domain events API (read/replay). |
| **Job orchestration** | JobOrchestrationController | Orchestrated job enqueue/status (JobExecutionWorker pipeline). |
| **Operational replay / rebuild** | OperationalReplayController, OperationalRebuildController, OperationalTraceController, TraceController | Replay operations; state rebuild; operational trace and diagnostics. |
| **System workers** | SystemWorkersController, SystemSchedulerController | Worker heartbeat and scheduler status (internal/ops). |
| **Payout & financial alerts** | PayoutHealthController, FinancialAlertsController | Payout health dashboard; order financial alerts. |
| **Notifications** | NotificationsController | In-app notifications CRUD; list by user; mark read/archive. (Outbound dispatch: NotificationDispatchWorkerHostedService; see operations/background_jobs.md.) |
| **GPON rates** | GponRateGroupsController, GponRateGroupMappingsController, GponBaseWorkRatesController | GPON partner and SI rate configuration. |
| **Admin security** | AdminSecuritySessionsController | Admin session management (e.g. revoke). |

---

## 7. Convention

- **Department-scoped:** Most of Orders, Inventory, Scheduler, Departments, ServiceInstallers, Settings (OrderTypes, BuildingTypes, etc.), Reports, Pnl, Tasks use department context; 403 when user has no access to requested department.  
- **Auth:** JWT Bearer; optional refresh. AuthController: login, refresh, me, change-password (authenticated), change-password-required (unauthenticated, for forced password change). Login/refresh return 403 with `requiresPasswordChange: true` when user has MustChangePassword; no tokens until password is changed.
- **Admin user management (v1.2):** List/detail expose `lastLoginAtUtc`, `mustChangePassword`. Reset password supports `forceMustChangePassword` (default true) to require password change on next login.
- **Response envelope:** success, message, data, errors (see API_OVERVIEW).
