# Service Sprawl Watch

**Related:** [high_coupling_modules.md](high_coupling_modules.md) | [module_fragility_map.md](module_fragility_map.md) | [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md)

**Purpose:** Identify services that are oversized or overly central—many constructor dependencies, many domain responsibilities, or used by many controllers/workers. Governance-level only; no code changes.

**Last scan:** March 2026 (Level 15 watchdog).

---

## Services with sprawl risk

| Service | Module | Sprawl risk | Symptoms | Related docs | Monitoring priority |
|---------|--------|-------------|----------|---------------|----------------------|
| **OrderService** | Orders | **High** | 19 constructor dependencies (DbContext, Logger, BuildingService, BlockerValidation, WorkflowEngine, WorkflowDefinitions, SlaProfile, AutomationRule, BusinessHours, EscalationRule, ApprovalWorkflow, OrderType, Notification, Encryption, MaterialTemplate, Inventory, EffectiveScopeResolver, OrderPayoutSnapshot). Used by OrdersController, ParserService, EmailIngestionService, ReportsController, NotificationDispatchRequestService, AgentModeService, OrderStatusChangedNotificationHandler. ~3000+ LOC. Orchestrates order CRUD, status, assignment, materials, payout snapshot; touches Buildings, Workflow, Settings (6+), Notifications, Inventory, Rates. | order_lifecycle_and_statuses, controller_service_map, high_coupling_modules | **P1** – Do not add further constructor dependencies without extracting a facade or splitting responsibilities. |
| **WorkflowEngineService** | Workflow | **High** | 11 constructor dependencies; single point for all order status transitions. Injects SchedulerService, EventStore, AuditLogService; side-effect registry invokes Notifications, Scheduler. Used by OrderService, OrderStatusesController, SchedulerService (5× runtime), EmailIngestionService, InvoiceSubmissionService, AgentModeService, EmailSendingService. | WORKFLOW_ENGINE, refactor_danger_zones | **P1** – Central to lifecycle; any new side-effect or guard type increases sprawl. |
| **BackgroundJobProcessorService** | Workflow (workers) | **High** | Single processor for 10+ job types; resolves IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, IJobRunRecorderForEvents, EInvoiceProviderFactory per job type via GetRequiredService. Not constructor sprawl but orchestration sprawl—one type name branches to many domains. | background_jobs, worker_dependency_risks | **P1** – New job types add hidden coupling; document each in background_jobs.md. |
| **SchedulerService** | Scheduler | **High** | Large codebase; many methods query _context.Orders directly (slot–order linkage). Resolves IWorkflowEngineService in five code paths (hidden). Mixed: scheduling logic + order reads + workflow resolution. | 02_modules/scheduler, hidden_dependencies | **P1** – Reduce _context.Orders usage and replace GetRequiredService with constructor injection when refactoring. |
| **BillingService** | Billing | **Medium–High** | Queries _context.Orders and _context.Invoices; invoice creation from orders; CRUD and PDF. Not as many constructor deps as OrderService but direct cross-domain DbContext access. | billing_and_invoicing, refactor_danger_zones | **P2** – Billing correctness critical; avoid adding more Order/Inventory access paths. |
| **BuildingService** | Buildings | **Medium** | Queries _context.Orders (OrdersCount, merge, move orders). Multiple responsibilities: building CRUD, merge, order count. | 02_modules/buildings, dependency_leak_watch | **P2** – Merge and order-move are sensitive; keep Order access behind clear methods. |
| **InvoiceSubmissionService** | Billing | **Medium** | Injects IWorkflowEngineService (Billing → Workflow for status transitions). Single responsibility but tight coupling to Workflow. | billing_myinvois_flow, hidden_dependencies | **P2** – Already documented; avoid adding more workflow transitions from Billing. |

---

## Sprawl signals (reference)

- **Too many constructor dependencies:** e.g. 10+ for a single service.
- **Used by many controllers/workers:** e.g. 5+ distinct call sites across modules.
- **Mixed orchestration and business logic:** service both coordinates other services and implements domain rules.
- **Runtime service resolution:** GetRequiredService hides dependencies from constructor and from dependency graphs.
- **Direct DbContext access to other domains:** _context.Orders / _context.Invoices in non-Orders/non-Billing services.
- **Too many methods or branches:** very large LOC or many conditional paths (qualitative; use as secondary signal).

---

**Refresh:** Re-scan when new services are added, constructor parameter counts increase, or new GetRequiredService calls appear in core services. Update this table and ARCHITECTURE_WATCHDOG_REPORT § Service sprawl.
