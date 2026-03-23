# Controller → Service Map

**Related:** [api_surface_summary.md](api_surface_summary.md) | [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md)

**Purpose:** Map major controller families to their primary application services and business domain. Focus on meaningful business surfaces; trivial helpers omitted.

---

## Core operations

| Controller | Main service(s) | Domain | Notable dependencies | Canonical docs |
|------------|-----------------|--------|----------------------|----------------|
| OrdersController | OrderService, WorkflowEngineService | Orders | SchedulerService, StockLedgerService, DepartmentAccessService | [order_lifecycle_and_statuses](../business/order_lifecycle_and_statuses.md), [02_modules/orders](../02_modules/orders/OVERVIEW.md) |
| SchedulerController | SchedulerService | Scheduler | DepartmentAccessService | [02_modules/scheduler](../02_modules/scheduler/OVERVIEW.md) |
| ParserController | ParserService, IEmailIngestionService | Parser | OrderService, BuildingService | [02_modules/email_parser](../02_modules/email_parser/OVERVIEW.md), [20_workflow_email_to_order](20_workflow_email_to_order.md) |
| InventoryController | StockLedgerService (IStockLedgerService) | Inventory | DepartmentAccessService | [inventory_ledger_and_serials](../modules/inventory_ledger_and_serials.md) |
| BillingController | Billing services, InvoiceSubmission | Billing | DepartmentAccessService | [billing_and_invoicing](../modules/billing_and_invoicing.md), [billing_myinvois_flow](../business/billing_myinvois_flow.md) |
| BuildingsController, BuildingTypesController, etc. | BuildingService, BuildingTypeService | Buildings | DepartmentAccessService | [02_modules/buildings](../02_modules/buildings/WORKFLOW.md) |

---

## Organisation & settings

| Controller | Main service(s) | Domain | Notable dependencies | Canonical docs |
|------------|-----------------|--------|----------------------|----------------|
| DepartmentsController | DepartmentAccessService, department CRUD | Departments | RBAC scope | [department_rbac](../business/department_rbac.md) |
| ServiceInstallersController, SiAppController | ServiceInstaller services | ServiceInstallers | SchedulerService | [si_app_journey](../business/si_app_journey.md) |
| OrderTypesController, OrderStatusesController, etc. | OrderTypeService, OrderStatusService | Settings | DepartmentAccessService | [REFERENCE_TYPES](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) |
| WorkflowDefinitionsController, GuardConditionDefinitionsController | Workflow definition services | Workflow | WorkflowEngineService | [WORKFLOW_ENGINE](../01_system/WORKFLOW_ENGINE.md) |
| RatesController, BillingRatecardController | RateEngineService, BillingRatecardService | Rates | DepartmentAccessService | [rate_engine](../02_modules/rate_engine/RATE_ENGINE.md) |
| PayrollController | PayrollService | Payroll | RateEngineService | [payroll OVERVIEW](../02_modules/payroll/OVERVIEW.md) |
| DocumentTemplatesController, EmailTemplatesController, etc. | DocumentTemplateService, EmailTemplateService | Settings | DepartmentAccessService | [02_modules/global_settings](../02_modules/global_settings/OVERVIEW.md) |
| GlobalSettingsController | GlobalSettingsService | Settings | — | [02_modules/global_settings](../02_modules/global_settings/OVERVIEW.md) |

---

## Reports, P&L, tasks, RMA, files

| Controller | Main service(s) | Domain | Notable dependencies | Canonical docs |
|------------|-----------------|--------|----------------------|----------------|
| ReportsController | Report runner (by key) | Reports | DepartmentAccessService | [02_modules/reports_hub](../02_modules/reports_hub/OVERVIEW.md) |
| PnlController | Pnl services | P&L | DepartmentAccessService | [pnl_boundaries](../business/pnl_boundaries.md), [02_modules/pnl](../02_modules/pnl/OVERVIEW.md) |
| TasksController | Task services | Tasks | DepartmentAccessService | — |
| RMAController | RMA services | RMA | — | [inventory_entities](../05_data_model/entities/inventory_entities.md) |
| FilesController, DocumentsController | File/document services | Files | IOneDriveSyncService (optional) | [integrations/overview](../integrations/overview.md) |

---

## Eventing, operational, observability

| Controller | Main service(s) | Domain | Notable dependencies | Canonical docs |
|------------|-----------------|--------|----------------------|----------------|
| EventStoreController, EventLedgerController, EventsController | EventStoreQueryService, IEventStore, DomainEventDispatcher | Events | Event store repository | [PHASE_8_PLATFORM_EVENT_BUS](../PHASE_8_PLATFORM_EVENT_BUS.md), [EVENT_BUS_OPERATIONS_RUNBOOK](../EVENT_BUS_OPERATIONS_RUNBOOK.md) |
| JobOrchestrationController | IJobExecutorRegistry, job execution store | Job orchestration | JobExecutionWorkerHostedService | [background_jobs](../operations/background_jobs.md) |
| OperationalReplayController, OperationalRebuildController | OperationalReplayExecutionService, EventBulkReplayService | Replay/rebuild | Event store | [OPERATIONAL_REPLAY_ENGINE_PHASE1](../OPERATIONAL_REPLAY_ENGINE_PHASE1.md) |
| SystemWorkersController, SystemSchedulerController | Worker heartbeat / scheduler status | Observability | WorkerHeartbeatHostedService | [background_jobs](../operations/background_jobs.md) |
| PayoutHealthController, FinancialAlertsController | Payout anomaly / financial alert services | Payout / financial | — | [background_jobs](../operations/background_jobs.md) |
| NotificationsController | NotificationService, INotificationDispatchRequestService | Notifications | NotificationDispatchWorkerHostedService (outbound) | [02_modules/notifications](../02_modules/notifications/OVERVIEW.md) |
| GponRateGroupsController, etc. | Gpon rate services | GPON rates | RateEngineService | [rate_engine](../02_modules/rate_engine/RATE_ENGINE.md) |

---

## Admin, auth, messaging

| Controller | Main service(s) | Domain | Notable dependencies | Canonical docs |
|------------|-----------------|--------|----------------------|----------------|
| AdminUsersController | AdminUserService | Admin | IAuditLogService, AuthService | [RBAC_MATRIX_REPORT](../RBAC_MATRIX_REPORT.md) |
| AuthController | AuthService | Auth | IEmailSendingService (optional) | — |
| EmailAccountsController, EmailsController | Email account / viewer services | Parser (email) | IEmailIngestionService | [02_modules/email_parser](../02_modules/email_parser/OVERVIEW.md) |
| WhatsAppController, SmsController, MessagingController | IWhatsAppMessagingService, ISmsMessagingService, IUnifiedMessagingService | Notifications | Template services | [integrations/overview](../integrations/overview.md) |
| BackgroundJobsController | Background job list/summary (read); trigger if exposed | Background jobs | BackgroundJobProcessorService | [background_jobs](../operations/background_jobs.md) |

---

**Refresh:** When adding new controller families or changing primary service ownership, update this map and [api_surface_summary.md](api_surface_summary.md).
