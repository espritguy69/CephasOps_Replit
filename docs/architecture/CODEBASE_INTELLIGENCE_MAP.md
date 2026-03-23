# Codebase Intelligence Map

**Status:** Active  
**Last updated:** March 2026  
**Purpose:** Single hub for architecture intelligence: modules, controllers, services, entities, workers, integrations, and canonical doc coverage. Use for onboarding, architecture audits, and safe refactoring.

**Source of truth:** Actual repository (backend, frontend, docs). This map is derived from code and docs; when code changes, refresh the linked maps and this index.

---

## 1. Repository shape

| Folder | Role |
|--------|------|
| **backend/** | .NET 10 solution: Api (controllers), Application (services, hosted services), Domain (entities, interfaces), Infrastructure (EF, repositories, external adapters). |
| **frontend/** | React 18 + TypeScript + Vite; Syncfusion; TanStack Query; admin portal. |
| **frontend-si/** | SI app (mobile-first PWA); job list, status updates, fieldwork. |
| **docs/** | Documentation hierarchy: 01_system … 08_infrastructure, 99_appendix, overview, business, operations, dev, architecture, integrations, modules, archive. |
| **infra/** | Infrastructure as code (k8s, terraform, monitoring). |
| **scripts/** | DB seeds, migration helpers, runbooks. |
| **tests/** | E2E scenarios; manual test checklists. |

---

## 2. Runtime architecture

- **Backend:** ASP.NET Core 10 API; Clean Architecture (Api → Application → Domain ← Infrastructure). Department-scoped RBAC; JWT auth. Single-company context.
- **Admin frontend:** React SPA; proxies /api to backend; Syncfusion grids, scheduler, reports.
- **SI frontend:** React PWA; SI app endpoints; job list and status transitions.
- **Background workers:** In-process HostedServices (schedulers, event dispatcher, notification dispatch, job execution worker, etc.). See [background_worker_map.md](background_worker_map.md).
- **Eventing:** Event store (outbox); EventStoreDispatcherHostedService; replay and rebuild surfaces. See [ARCHITECTURE_AUDIT_REPORT.md](../ARCHITECTURE_AUDIT_REPORT.md) and EVENT_BUS_OPERATIONS_RUNBOOK.

---

## 3. Major domains / modules

| Module | Purpose | Main controllers | Main services | Canonical docs |
|--------|---------|------------------|---------------|----------------|
| **Orders** | Order CRUD, status, assignment | OrdersController, OrderStatusesController, OrderStatusChecklistController | OrderService, WorkflowEngineService | [business/order_lifecycle_and_statuses](../business/order_lifecycle_and_statuses.md), [02_modules/orders](../02_modules/orders/OVERVIEW.md) |
| **Parser** | Email ingest, parse, drafts, approve→order | ParserController | ParserService, EmailIngestionService, IParserTemplateService | [02_modules/email_parser](../02_modules/email_parser/OVERVIEW.md), [01_system/EMAIL_PIPELINE](../01_system/EMAIL_PIPELINE.md) |
| **Scheduler** | Slots, SI availability, leave, utilization | SchedulerController | SchedulerService | [02_modules/scheduler](../02_modules/scheduler/OVERVIEW.md) |
| **Inventory** | Ledger, stock, materials, bins | InventoryController, BinsController, WarehousesController | StockLedgerService, IStockLedgerService | [modules/inventory_ledger_and_serials](../modules/inventory_ledger_and_serials.md), [02_modules/inventory](../02_modules/inventory/OVERVIEW.md) |
| **Billing** | Invoices, PDF, MyInvois, payments | BillingController, InvoiceSubmissionsController, PaymentsController | Billing services, InvoiceSubmission | [modules/billing_and_invoicing](../modules/billing_and_invoicing.md), [business/billing_myinvois_flow](../business/billing_myinvois_flow.md) |
| **Buildings** | Buildings, types, installation methods, splitters | BuildingsController, BuildingTypesController, SplittersController, etc. | BuildingService, BuildingTypeService | [02_modules/buildings](../02_modules/buildings/WORKFLOW.md) |
| **Workflow** | Transitions, guards, side effects | WorkflowController, WorkflowDefinitionsController, GuardConditionDefinitionsController | WorkflowEngineService | [01_system/WORKFLOW_ENGINE](../01_system/WORKFLOW_ENGINE.md), [business/order_lifecycle_and_statuses](../business/order_lifecycle_and_statuses.md) |
| **Rates / Payroll** | SI rates, partner rates, payroll runs | RatesController, PayrollController, GponRateGroupsController, etc. | RateEngineService, PayrollService | [02_modules/rate_engine](../02_modules/rate_engine/RATE_ENGINE.md), [02_modules/payroll](../02_modules/payroll/OVERVIEW.md), [business/payroll_rate_overview](../business/payroll_rate_overview.md) |
| **P&L** | Analytics, drilldown | PnlController | Pnl services | [02_modules/pnl](../02_modules/pnl/OVERVIEW.md), [business/pnl_boundaries](../business/pnl_boundaries.md) |
| **Reports** | Run by key, export | ReportsController, ReportDefinitionsController | Report runner | [02_modules/reports_hub](../02_modules/reports_hub/OVERVIEW.md) |
| **Notifications** | In-app notifications; outbound dispatch (SMS/WhatsApp/email) | NotificationsController | NotificationService, NotificationDispatchRequestService, INotificationDeliverySender | [02_modules/notifications](../02_modules/notifications/OVERVIEW.md) |
| **Events / Event store** | Append, query, replay | EventStoreController, EventLedgerController, EventsController | EventStoreQueryService, DomainEventDispatcher, replay services | [PHASE_8_PLATFORM_EVENT_BUS](../PHASE_8_PLATFORM_EVENT_BUS.md), [EVENT_BUS_OPERATIONS_RUNBOOK](../EVENT_BUS_OPERATIONS_RUNBOOK.md) |
| **Operational replay / rebuild** | Replay, state rebuild, trace | OperationalReplayController, OperationalRebuildController, OperationalTraceController, TraceController | OperationalReplayExecutionService, EventBulkReplayService | [OPERATIONAL_REPLAY_ENGINE_PHASE1](../OPERATIONAL_REPLAY_ENGINE_PHASE1.md), [OPERATIONAL_REPLAY_ENGINE_PHASE2](../OPERATIONAL_REPLAY_ENGINE_PHASE2.md) |
| **Job orchestration** | Enqueue/run orchestrated jobs | JobOrchestrationController | JobExecutionWorkerHostedService, IJobExecutorRegistry | [operations/background_jobs](../operations/background_jobs.md), DISTRIBUTED_PLATFORM_PHASE3_JOB_ORCHESTRATION_* |
| **Settings / Reference data** | Order types, building types, templates, global settings | Many (OrderTypes, BuildingTypes, DocumentTemplates, GlobalSettings, etc.) | OrderTypeService, BuildingTypeService, DocumentTemplateService, GlobalSettingsService, etc. | [05_data_model/REFERENCE_TYPES](../05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md), [02_modules/global_settings](../02_modules/global_settings/OVERVIEW.md) |
| **Departments / RBAC** | Department CRUD, access scope | DepartmentsController | DepartmentAccessService | [business/department_rbac](../business/department_rbac.md), [02_modules/department](../02_modules/department/OVERVIEW.md) |
| **Admin / Auth** | Users, roles, security | AdminUsersController, AdminRolesController, AuthController, AdminSecuritySessionsController | AdminUserService, AuthService | [RBAC_MATRIX_REPORT](../RBAC_MATRIX_REPORT.md) |

---

## 4. Core system flows

| Flow | Path | Key components | Docs |
|------|------|----------------|------|
| **Parser → Order** | Email ingest → ParseSession → ParsedOrderDraft → approve → Order | EmailIngestionService, ParserService, OrderService | [business/process_flows](../business/process_flows.md), [20_workflow_email_to_order](20_workflow_email_to_order.md) |
| **Order → Scheduling** | Order Assigned → ScheduledSlot | SchedulerService, WorkflowEngineService | [21_workflow_order_lifecycle](21_workflow_order_lifecycle.md) |
| **Scheduling → SI execution** | SI app: job list, status (OnTheWay, MetCustomer, OrderCompleted, Blocker) | SiAppController, SchedulerService | [business/si_app_journey](../business/si_app_journey.md) |
| **Field → Docket** | Docket receive, verify, reject, upload to partner | Order status flow (DocketsReceived, DocketsVerified, DocketsUploaded) | [business/docket_process](../business/docket_process.md) |
| **Docket → Billing** | ReadyForInvoice → Invoice → MyInvois → SubmittedToPortal | BillingController, InvoiceSubmissionService | [modules/billing_and_invoicing](../modules/billing_and_invoicing.md) |
| **Billing → Payment / Payroll / P&L** | Payments; payroll runs; P&L rebuild job | PayrollController, PnlController, PnlRebuildSchedulerService | [business/payroll_rate_overview](../business/payroll_rate_overview.md), [business/pnl_boundaries](../business/pnl_boundaries.md) |
| **Eventing / Replay** | Event store append → EventStoreDispatcherHostedService → handlers; Replay APIs | EventStoreDispatcherHostedService, OperationalReplayExecutionService | [EVENT_BUS_OPERATIONS_RUNBOOK](../EVENT_BUS_OPERATIONS_RUNBOOK.md), [PHASE_8_PLATFORM_EVENT_BUS](../PHASE_8_PLATFORM_EVENT_BUS.md) |

---

## 5. Architecture hotspots

| Hotspot | Why it matters | Doc coverage | Drift risk |
|---------|----------------|--------------|------------|
| **Event store & dispatcher** | Outbox pattern; replay; many handlers | PHASE_8, EVENT_BUS_OPERATIONS_RUNBOOK, phase docs in archive | Medium – keep api_surface and background_worker_map updated |
| **Notification dispatch** | Event-driven outbound (SMS/WhatsApp/email); worker pipeline | operations/background_jobs, 02_modules/notifications, Phase 2 summary | Low |
| **Workflow engine** | All order status transitions; guards; side effects | WORKFLOW_ENGINE, order_lifecycle_and_statuses, workflow_engine_validation_gpon | Low |
| **Inventory ledger** | Single source of truth; no direct balance writes | modules/inventory_ledger_and_serials, 02_modules/inventory | Low |
| **Settings / reference data** | Many controllers and entities; department-scoped | REFERENCE_TYPES_AND_RELATIONSHIPS, 02_modules/global_settings, DATA_MODEL_INDEX | Medium – many small surfaces |
| **Rates / payroll / P&L** | RateEngine, payroll runs, P&L rebuild, payout anomaly | rate_engine, payroll OVERVIEW, business/payroll_rate_overview, pnl_boundaries | Medium |
| **Background job processor** | Single processor + many job types; orchestrated job worker | operations/background_jobs, background_worker_map | Low after recent doc update |

---

## 6. Governance cross-links

| Artifact | Purpose |
|----------|---------|
| [ARCHITECTURE_AUDIT_REPORT.md](../ARCHITECTURE_AUDIT_REPORT.md) | Code vs docs alignment; drift findings; module boundaries. |
| [DOCS_INVENTORY.md](../DOCS_INVENTORY.md) | Doc inventory and status. |
| [_discrepancies.md](../_discrepancies.md) | Code vs docs mismatches; accepted gaps; deferred. |
| [DOCUMENTATION_ALIGNMENT_CHECKLIST.md](../DOCUMENTATION_ALIGNMENT_CHECKLIST.md) | Required doc set (A–P) and completion. |
| [DOCS_MAP.md](../DOCS_MAP.md) | Required doc set; canonical vs reference. |
| [api_surface_summary.md](api_surface_summary.md) | Controllers by module. |
| [data_model_overview.md](data_model_overview.md) | Key entities; link to DATA_MODEL_INDEX. |
| [controller_service_map.md](controller_service_map.md) | Controller → service mapping. |
| [module_dependency_map.md](module_dependency_map.md) | Module dependencies. |
| [background_worker_map.md](background_worker_map.md) | Hosted services and workers. |
| [integration_map.md](integration_map.md) | External and internal integrations. |
| [entity_domain_map.md](entity_domain_map.md) | Entities by domain. |
| [CODEBASE_INTELLIGENCE_REPORT.md](../CODEBASE_INTELLIGENCE_REPORT.md) | Summary of intelligence layer and findings. |
| [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md) | Refactor safety audit: coupling, fragility, safe/danger zones, sequence plan. |
| [high_coupling_modules.md](high_coupling_modules.md) | Modules ranked by coupling risk. |
| [hidden_dependencies.md](hidden_dependencies.md) | Hidden service and DbContext dependencies. |
| [module_fragility_map.md](module_fragility_map.md) | Per-module fragility assessment. |
| [safe_refactor_zones.md](safe_refactor_zones.md) | Lower-risk refactor areas. |
| [refactor_danger_zones.md](refactor_danger_zones.md) | High-risk refactor areas. |
| [refactor_sequence_plan.md](refactor_sequence_plan.md) | Suggested refactor order. |
| [worker_dependency_risks.md](worker_dependency_risks.md) | Worker dependency and coupling risks. |
| [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md) | Periodic watchdog: drift, sprawl, leaks, boundary regression. |
| [service_sprawl_watch.md](service_sprawl_watch.md) | Oversized or centralizing services. |
| [controller_sprawl_watch.md](controller_sprawl_watch.md) | Controller families growing too broad. |
| [dependency_leak_watch.md](dependency_leak_watch.md) | Hidden links, cycles, cross-domain leakage. |
| [worker_coupling_watch.md](worker_coupling_watch.md) | Worker coupling and risk trend. |
| [module_boundary_regression.md](module_boundary_regression.md) | Module boundary status: stable / drifting / high risk. |
| [ARCHITECTURE_GOVERNANCE_SYSTEMS.md](ARCHITECTURE_GOVERNANCE_SYSTEMS.md) | Index of named systems: Change Impact Predictor, Architecture Policy Engine, Auto Documentation Sync, Architecture Risk Dashboard, Self-Maintaining Architecture, Portal, Governance logs. |

---

**Refresh triggers:** New controller families; new domain services or workers; new integrations; workflow or eventing changes; RBAC changes. Update this map and the linked maps; then run an architecture audit if needed.
