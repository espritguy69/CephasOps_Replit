# Hidden Dependencies

**Related:** [controller_service_map.md](controller_service_map.md) | [background_worker_map.md](background_worker_map.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Record dependency patterns that are not obvious from module boundaries: runtime resolution (GetRequiredService), cross-domain DbContext access, and workers invoking services outside their “home” module.

---

## Service → service (constructor-injected)

| Source | Target | Risk | Notes |
|--------|--------|------|-------|
| OrderService | WorkflowEngineService | High | All order status changes go through workflow. |
| OrderService | IBuildingService, IInventoryService, IOrderPayoutSnapshotService, INotificationService, many Settings services | Medium | OrderService has 15+ constructor dependencies; refactor of any of these can break Orders. |
| WorkflowEngineService | ISchedulerService | High | Workflow side effects (e.g. slot creation) call Scheduler; circular risk with Scheduler→Workflow. |
| InvoiceSubmissionService | IWorkflowEngineService | High | Billing drives order status (e.g. SubmittedToPortal); tight coupling. |
| ParserService | IOrderService | Medium | Draft approve → order creation; documented. |
| EmailIngestionService | IOrderService, IWorkflowEngineService? | Medium | Optional workflow for Cancel/Blocker; order creation on approve. |
| NotificationDispatchRequestService | IOrderService | Medium | Notification payload/resolution may need order data. |
| AgentModeService | IOrderService, IWorkflowEngineService, IInvoiceSubmissionService | Medium | Agent mode touches orders, workflow, billing. |
| ReportsController | IOrderService, IStockLedgerService, ISchedulerService | Low–Medium | Read-only aggregation; no state changes. |
| MaterialCollectionService (Orders) | IStockLedgerService | Medium | Orders → Inventory for materials. |
| SiAppMaterialService | IStockLedgerService | Medium | SI app → Inventory. |
| OrderProfitabilityService (Pnl) | IRateEngineService | Medium | P&L uses rate engine for payout. |
| PayrollService | IRateEngineService | High | Payroll calculation depends on RateEngineService. |

---

## Runtime resolution (GetRequiredService) — hidden from constructor

| Source | Target | Risk | Notes |
|--------|--------|------|-------|
| SchedulerService | IWorkflowEngineService | High | Resolved in five code paths (slot/conflict handling). Not visible on SchedulerService constructor; creates hidden Workflow dependency. |
| NoSchedulingConflictsValidator (Workflow) | ISchedulerService | Medium | Guard validator resolves Scheduler at runtime. |
| OrderStatusChangedNotificationHandler | IOrderService, IGlobalSettingsService | Medium | Event handler; resolves at runtime. |
| BackgroundJobProcessorService | IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, IJobRunRecorderForEvents, EInvoiceProviderFactory | High | Single job processor resolves many domain services per job type. Changing a job handler or service signature can break job execution. |
| PayoutAnomalyAlertSchedulerService | ApplicationDbContext, IPayoutAnomalyAlertService | Low–Medium | Standard worker pattern. |
| MissingPayoutSnapshotSchedulerService | ApplicationDbContext, IMissingPayoutSnapshotRepairService | Low–Medium | Repair service in Rates. |
| EmailCleanupService | ApplicationDbContext, IFileService | Low | Parser/file cleanup. |
| EventStoreDispatcherHostedService | IEventStore, IEventTypeRegistry, IDomainEventDispatcher | Medium | Event pipeline; documented. |
| WorkerHeartbeatHostedService | IWorkerCoordinator | Low | Infrastructure. |

---

## Cross-domain DbContext / entity access

| Source | Target entity set | Risk | Notes |
|--------|-------------------|------|-------|
| BuildingService | _context.Orders | Medium | OrdersCount, merge building (move orders), hasOrders checks. Buildings module reads/writes Order.BuildingId. |
| BillingService | _context.Orders, _context.Invoices | High | Invoice creation from orders; invoice CRUD. Billing owns Invoice but queries Orders directly. |
| SchedulerService | _context.Orders | High | Many methods load Order for slot.OrderId; slot–order linkage. Scheduler does not use IOrderService for reads. |

---

## Workers affecting multiple modules

| Worker | Modules affected | Risk | Notes |
|--------|------------------|------|-------|
| BackgroundJobProcessorService | Parser, P&L, Inventory, Billing, Events, Replay, Rebuild, SLA | High | Dispatches to EmailIngest, PnlRebuild, ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots, MyInvoisStatusPoll, slaevaluation, OperationalRebuild, OperationalReplay, etc. |
| EventStoreDispatcherHostedService | All domains (via handlers) | High | Any handler can touch any domain; replay/rebuild amplify. |
| NotificationDispatchWorkerHostedService | Notifications, Orders (for context) | Medium | INotificationDeliverySender; dispatch request may carry order context. |
| JobExecutionWorkerHostedService | P&L, Replay, etc. (IJobExecutorRegistry) | Medium | Runs PnlRebuild, OperationalReplay, etc. |

---

**Refresh:** When adding new GetRequiredService usage, new _context.XXX in a different domain, or new job types/handlers, add an entry here. Prefer constructor injection for visibility.
