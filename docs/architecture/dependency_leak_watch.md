# Dependency Leak Watch

**Related:** [hidden_dependencies.md](hidden_dependencies.md) | [module_dependency_map.md](module_dependency_map.md) | [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md)

**Purpose:** Detect structural leakage—runtime resolution hiding coupling, shared DbContext crossing domain boundaries, and suspected dependency cycles. Feeds refactor safety and hidden_dependencies.

**Last scan:** March 2026 (Level 15 watchdog).

---

## Leak register

| Source | Target | Leak type | Severity | Notes |
|--------|--------|-----------|----------|-------|
| SchedulerService | IWorkflowEngineService | Runtime resolution (GetRequiredService ×5) | High | Not visible on constructor; creates hidden Workflow dependency. Potential cycle: Workflow → Scheduler (constructor), Scheduler → Workflow (runtime). |
| NoSchedulingConflictsValidator (Workflow) | ISchedulerService | Runtime resolution | Medium | Guard validator resolves Scheduler at runtime; Workflow → Scheduler. |
| OrderStatusChangedNotificationHandler | IOrderService, IGlobalSettingsService | Runtime resolution | Medium | Event handler in Notifications; reaches into Orders and Settings. |
| BackgroundJobProcessorService | IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, … | Runtime resolution per job type | High | One processor; many domains; coupling hidden behind job type string. |
| BuildingService | _context.Orders | Cross-domain DbContext | Medium | OrdersCount, merge (move orders), hasOrders. Buildings reading/writing Order.BuildingId. |
| BillingService | _context.Orders, _context.Invoices | Cross-domain DbContext | High | Invoice creation from orders; Billing owns Invoice but reads Orders directly. |
| SchedulerService | _context.Orders | Cross-domain DbContext | High | Many methods load Order for slot.OrderId; Scheduler does not use IOrderService for reads. |
| WorkflowEngineService | ISchedulerService | Constructor | High | Workflow → Scheduler (explicit). Scheduler then calls back to Workflow via GetRequiredService → cycle risk. |
| InvoiceSubmissionService | IWorkflowEngineService | Constructor | High | Billing → Workflow for status transitions (e.g. SubmittedToPortal). |

---

## Suspected cycle risk

- **Scheduler ↔ Workflow:** WorkflowEngineService injects ISchedulerService; SchedulerService resolves IWorkflowEngineService in five places. Bidirectional dependency; cycle is indirect (runtime on one side). **Mitigation:** Document in hidden_dependencies; prefer explicit constructor injection on both sides and consider facade to break cycle if refactoring.

---

## Leak types (reference)

- **Runtime resolution:** GetRequiredService / GetService in a service or handler; dependency not visible on constructor.
- **Cross-domain DbContext:** _context.XXX where XXX is an entity set owned by another module (e.g. Buildings accessing Orders).
- **Service locator:** Use of IServiceProvider to resolve services by type or key inside business logic.
- **Event/handler reaching into core:** Domain event handler that resolves and calls a core domain service (e.g. OrderService) instead of being triggered by that service.

---

**Refresh:** When adding new GetRequiredService usage, new _context.XXX in a different domain, or new event handlers that resolve core services, add a row and update hidden_dependencies.md and ARCHITECTURE_WATCHDOG_REPORT § Dependency leak.
