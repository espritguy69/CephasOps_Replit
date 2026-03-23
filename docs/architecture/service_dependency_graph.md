# Service Dependency Graph

**Date:** March 2026  
**Purpose:** Internal dependency topology for Application and key Infrastructure services. Used for sprawl analysis and architecture integrity. No code changes.

**Related:** [service_sprawl_analysis.md](service_sprawl_analysis.md) | [high_coupling_modules.md](high_coupling_modules.md) | [level1_code_integrity.md](../engineering/level1_code_integrity.md)

---

## 1. Executive summary

The application layer has a **hub-and-spoke** pattern with **OrderService** and **WorkflowEngineService** as the two main hubs. **SchedulerService** and **BackgroundJobProcessorService** add orchestration hubs with **runtime resolution** (GetRequiredService), which hides fan-out in static dependency graphs. **Settings** (many small services) and **DepartmentAccessService** are cross-cutting. Domain leakage: **Application** services inject **ApplicationDbContext** (Infrastructure) and a few use **Microsoft.AspNetCore.Http**; **Domain** has no Infrastructure references.

---

## 2. Most central services (fan-in / fan-out)

| Service | Constructor deps (approx) | Fan-out (downstream) | Fan-in (callers) | Role |
|---------|---------------------------|----------------------|------------------|------|
| **OrderService** | 19 | Building, BlockerValidation, WorkflowEngine, WorkflowDefinitions, SlaProfile, Automation, BusinessHours, Escalation, Approval, OrderType, Notification, Encryption, MaterialTemplate, Inventory, EffectiveScopeResolver, OrderPayoutSnapshot, DbContext | OrdersController, ParserController, EmailIngestionService, ReportsController, NotificationDispatchRequestService, AgentModeService, OrderStatusChangedNotificationHandler, SchedulerService (indirect) | **Hub** – order lifecycle |
| **WorkflowEngineService** | 11 | WorkflowDefinitions, OrderPricingContextResolver, EffectiveScopeResolver, GuardConditionValidatorRegistry, SideEffectExecutorRegistry, SchedulerService, AuditLogService, EventStore, PlatformEventEnvelopeBuilder, DbContext | OrderService, OrderStatusesController, SchedulerService (5× resolved), EmailIngestionService, InvoiceSubmissionService, AgentModeService, EmailSendingService, ExecuteWorkflowTransitionHandler | **Hub** – all status transitions |
| **SchedulerService** | Many | DbContext (Orders, ScheduledSlots, etc.), IWorkflowEngineService (resolved at runtime 5×) | SchedulerController, ReportsController, WorkflowEngineService (side effects), NoSchedulingConflictsValidator | **Hub** – scheduling + hidden Workflow |
| **BackgroundJobProcessorService** | 4 (scope-based resolution) | IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, IJobRunRecorderForEvents, EInvoiceProviderFactory, DocumentGenerationService, etc. | Enqueued by schedulers; no direct controller | **Orchestrator** – job type → service |
| **DocumentGenerationService** | 5 + Handlebars | ApplicationDbContext, IDocumentTemplateService, IFileService, ICarboneRenderer | DocumentTemplatesController, DocumentGenerationJobExecutor | **Large leaf** – doc gen |
| **BillingService** | Multiple | DbContext (Orders, Invoices), BillingRatecard, Payment | BillingController, InvoiceSubmissionsController, BackgroundJobProcessorService (MyInvoisStatusPoll) | **Cross-domain** – Orders + Invoices |
| **BuildingService** | Multiple | DbContext (Orders, Buildings, etc.) | BuildingsController, OrderService, Parser (IBuildingService) | **Cross-domain** – Buildings + Order count/merge |
| **InvoiceSubmissionService** | Multiple | IWorkflowEngineService, MyInvois | BillingController, BackgroundJobProcessorService | **Billing → Workflow** |
| **StockLedgerService** | Multiple | DbContext (ledger, allocations) | InventoryController, OrderService (MaterialCollectionService), ReportsController, BackgroundJobProcessorService, SI | **Inventory hub** |
| **DepartmentAccessService** | Low | — | 20+ controllers, DepartmentScopeExtensions | **Cross-cutting** |

---

## 3. Dependency hotspots (visual summary)

```
                    ┌─────────────────────┐
                    │   OrdersController   │
                    │   (+ DbContext)      │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐     ┌──────────────────┐
                    │    OrderService     │────│ WorkflowEngine │
                    │    (19 deps)        │     │    (11 deps)   │
                    └──────────┬──────────┘     └────────┬───────┘
                               │                         │
         ┌─────────────────────┼─────────────────────┐   │
         │                     │                     │   │
    BuildingService      InventoryService      SchedulerService
    (_context.Orders)    (OrderService)        (_context.Orders
         │                     │               + GetRequiredService
         │                     │                IWorkflowEngineService)
         │                     │                     │
         └─────────────────────┴─────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │ BackgroundJobProc    │
                    │ (GetRequiredService │
                    │  per job type)       │
                    └─────────────────────┘
```

---

## 4. Services with ≥10 dependencies (P1 critical sprawl risk)

| Service | Count | Notes |
|---------|-------|------|
| **OrderService** | 19 | See level1_code_integrity and service_sprawl_analysis. |
| **WorkflowEngineService** | 11 | Central to transitions; guard/side-effect registries add indirect deps. |
| **UnifiedMessagingService** | 11 (from grep) | Notifications; many channel/template deps. |

---

## 5. Circular / suspicious dependency paths

- **Scheduler ↔ Workflow:** WorkflowEngineService injects ISchedulerService (side effects). SchedulerService resolves IWorkflowEngineService at runtime in five code paths. **Cycle:** Workflow → Scheduler (constructor), Scheduler → Workflow (runtime). Documented in dependency_leak_watch / hidden_dependencies.
- No other **constructor-level** cycles were identified; runtime resolution hides further cycles in BackgroundJobProcessorService (job types can touch any domain).

---

## 6. Cross-module DbContext usage

| Service | Module | DbSet(s) from other domain | Risk |
|---------|--------|----------------------------|------|
| **BuildingService** | Buildings | Orders | P2 – count, merge, move |
| **BillingService** | Billing | Orders, Invoices | P2 – invoice from orders |
| **SchedulerService** | Scheduler | Orders (direct) | P2 – slot–order linkage |
| **DocumentGenerationService** | Settings | Multiple (templates, entities) | P2 – read-only for render |

---

## 7. Likely future split boundaries

- **OrderService:** Order CRUD vs Order status/transition vs Order assignment vs Order materials vs Payout snapshot. Suggested: facade + OrderStatusService, OrderAssignmentService, or move workflow coordination behind a dedicated coordinator.
- **WorkflowEngineService:** Keep transition engine; extract guard/side-effect discovery or execution to a separate component to avoid adding more constructor deps.
- **BackgroundJobProcessorService:** Migrate remaining job types to JobExecution + IJobExecutor; keep legacy processor for backward compatibility only.
- **DocumentGenerationService:** Template resolution, placeholder binding, PDF render, file save – each could be a smaller service.

---

## 8. Related artifacts

- [Service sprawl analysis](service_sprawl_analysis.md)
- [High coupling modules](high_coupling_modules.md)
- [Hidden dependencies](hidden_dependencies.md)
- [Level 1 code integrity](../engineering/level1_code_integrity.md)
