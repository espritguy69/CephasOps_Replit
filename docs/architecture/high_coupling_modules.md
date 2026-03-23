# High-Coupling Modules

**Related:** [module_dependency_map.md](module_dependency_map.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Modules ranked by coupling risk for refactor planning. Based on codebase scan: controller/service injection, service→service and service→DbContext usage across domains.

---

## Coupling level summary

| Module | Coupling level | Reason |
|--------|----------------|--------|
| **Orders** | **High** | Referenced by Parser (IOrderService), Scheduler (WorkflowEngineService + direct _context.Orders in SchedulerService), Billing (BillingService queries Orders; InvoiceSubmissionService uses WorkflowEngineService), Workflow (WorkflowEngineService, guards), Notifications (NotificationDispatchRequestService, OrderStatusChangedNotificationHandler), Reports (IOrderService, IStockLedgerService, ISchedulerService), Agent (AgentModeService), Buildings (BuildingService queries _context.Orders). OrderService itself injects 15+ services: Buildings, Workflow, Settings (SLA, Automation, BusinessHours, Escalation, Approval, OrderType, MaterialTemplate), Notifications, Inventory, Rates (OrderPayoutSnapshot). |
| **Workflow** | **High** | Used by Orders (OrderService), OrderStatusesController, Scheduler (SchedulerService resolves IWorkflowEngineService in five code paths), Parser (EmailIngestionService optional), Billing (InvoiceSubmissionService), Agent (AgentModeService), EmailSendingService. WorkflowEngineService injects ISchedulerService, IEventStore, IAuditLogService; side-effect registry can invoke Notifications, Scheduler. Central to all order status transitions. |
| **Billing** | **High** | Depends on Orders (BillingService queries _context.Orders for invoice creation; InvoiceSubmissionService calls IWorkflowEngineService for status transitions), Inventory (materials on invoice lines), Rates (BillingRatecard), MyInvois (e-invoice). BackgroundJobProcessorService resolves IInvoiceSubmissionService for MyInvoisStatusPoll job. |
| **Scheduler** | **High** | Depends on Orders (SchedulerService queries _context.Orders in many methods; slot–order linkage). Injected into WorkflowEngineService (side effects). Used by SchedulerController, ReportsController; NoSchedulingConflictsValidator resolves ISchedulerService. Workflow engine calls back into Scheduler for slot/conflict operations. |
| **Inventory** | **Medium–High** | StockLedgerService used by Orders (MaterialCollectionService), InventoryController, ReportsController, SI (SiAppMaterialService); BackgroundJobProcessorService resolves IStockLedgerService for ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots, InventoryReportExport. InventoryService (legacy) still injected into OrderService. Ledger is single source of truth—changes affect Billing (materials), RMA, Reports. |
| **Rates / Payroll** | **Medium** | RateEngineService used by RatesController, PayrollService, Pnl (OrderProfitabilityService). OrderService injects IOrderPayoutSnapshotService (Rates). P&L rebuild and payout anomaly workers touch Orders, Payroll, Rates. |
| **Parser** | **Medium** | Entry point; creates orders via IOrderService, resolves buildings via IBuildingService. Isolated in terms of callers (EmailIngestionSchedulerService, BackgroundJobProcessorService for EmailIngest), but depends on Orders and Buildings. |
| **Notifications** | **Medium** | Cross-cutting: OrderService, Workflow (side effects), NotificationDispatchRequestService (IOrderService), OrderStatusChangedNotificationHandler (IOrderService, IGlobalSettingsService). Worker (NotificationDispatchWorkerHostedService) and template services used by many modules. |
| **Events / Event store** | **Medium** | All domains can append; EventStoreDispatcherHostedService and replay/rebuild consume. WorkflowEngineService optionally injects IEventStore. Not many direct service-to-service links, but event handlers create indirect coupling. |
| **Settings / Reference** | **Medium** | Consumed by almost every module (OrderTypes, BuildingTypes, SLA, Automation, BusinessHours, Escalation, Approval, MaterialTemplate, etc.). OrderService alone injects many settings services. No single “Settings” service; many small services. |
| **Buildings** | **Medium** | BuildingService queries _context.Orders (OrdersCount, merge, move). Used by Parser, Orders (OrderService), Inventory (bins/locations). |
| **Departments / RBAC** | **Medium** | DepartmentAccessService injected in 20+ controllers and DepartmentScopeExtensions; cross-cutting for scope checks. |
| **Reports** | **Low–Medium** | ReportsController uses IOrderService, IStockLedgerService, ISchedulerService; aggregates across domains but does not drive transitions. |
| **P&L** | **Medium** | Depends on Orders, Payroll, Overheads; uses RateEngineService via OrderProfitabilityService; PnlRebuildSchedulerService and JobExecutionWorker (PnlRebuild) enqueue jobs. |

---

## Explanation of ratings

- **High:** Module is (1) referenced by five or more other modules or (2) has many incoming and outgoing service dependencies and/or direct DbContext access to other domains. Refactoring without a clear contract and tests is risky.
- **Medium:** Module is referenced by several others or is a cross-cutting concern (Notifications, Settings, Events); or has significant but bounded coupling (Parser, Inventory, Rates).
- **Low–Medium:** Primarily consumer of other domains (Reports) or mostly self-contained with few callers.

---

**Refresh:** When adding or removing service dependencies or _context usage across domains, re-scan and update this map. See [hidden_dependencies.md](hidden_dependencies.md) for service→service and DbContext cross-access detail.
