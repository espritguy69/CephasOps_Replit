# Refactor Safety Report

**Date:** March 2026  
**Pass:** Level 14 – Refactor safety audit  
**Scope:** Documentation and analysis only; no application code changed.

**Baseline:** [CODEBASE_INTELLIGENCE_MAP.md](architecture/CODEBASE_INTELLIGENCE_MAP.md), [controller_service_map.md](architecture/controller_service_map.md), [module_dependency_map.md](architecture/module_dependency_map.md), [background_worker_map.md](architecture/background_worker_map.md), and related intelligence docs.

---

## 1. Executive summary

### Overall refactor safety level

**Moderate risk.** The codebase has clear domain boundaries in places (Parser, Reports, Settings CRUD) but **high coupling** in the core flow: Orders, Workflow, Scheduler, and Billing are tightly intertwined with constructor and runtime (GetRequiredService) dependencies, and several services access other domains’ entities via DbContext (BuildingService→Orders, BillingService→Orders, SchedulerService→Orders). Refactoring the central path (order lifecycle, billing, inventory ledger, event store) without tests and a clear sequence is risky.

### Main risks

- **Orders** and **Workflow** are the most coupled: OrderService injects 15+ services; WorkflowEngineService is used by Orders, Scheduler, Billing, Parser, Agent; SchedulerService resolves IWorkflowEngineService in five places (hidden).
- **Hidden dependencies:** Runtime resolution (GetRequiredService) in SchedulerService, BackgroundJobProcessorService, and event handlers obscures coupling; cross-domain _context.Orders / _context.Invoices usage in BuildingService, BillingService, SchedulerService.
- **Workers:** BackgroundJobProcessorService and EventStoreDispatcherHostedService touch many modules; job-type → handler and event → handler mappings are not fully documented in one place.
- **Financial and operational criticality:** Billing, Inventory ledger, Rates/Payroll, and payout snapshots are high-impact; refactor there needs regression tests and staging validation.

---

## 2. High-coupling modules (summary)

| Module | Level | Reason (short) |
|--------|--------|----------------|
| Orders | High | Referenced by Parser, Scheduler, Billing, Workflow, Notifications, Reports, Agent, Buildings; OrderService has 15+ injected services. |
| Workflow | High | Used by Orders, Scheduler, Billing, Parser, Agent; injects Scheduler; central to all status transitions. |
| Billing | High | Orders, Inventory, Rates, Workflow; MyInvoisStatusPoll job; InvoiceSubmissionService uses WorkflowEngineService. |
| Scheduler | High | Many _context.Orders usages; injected into Workflow; resolves Workflow at runtime in five places. |
| Inventory | Medium–High | StockLedgerService used by Orders, Reports, SI, job executors; ledger is source of truth. |

Detail: [architecture/high_coupling_modules.md](architecture/high_coupling_modules.md).

---

## 3. Hidden dependencies (summary)

- **Runtime resolution:** SchedulerService → IWorkflowEngineService (5x); NoSchedulingConflictsValidator → ISchedulerService; OrderStatusChangedNotificationHandler → IOrderService, IGlobalSettingsService; BackgroundJobProcessorService → many domain services per job type.
- **Cross-domain DbContext:** BuildingService → _context.Orders; BillingService → _context.Orders, _context.Invoices; SchedulerService → _context.Orders in many methods.
- **Workers:** BackgroundJobProcessorService resolves IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, and others per job type.

Detail: [architecture/hidden_dependencies.md](architecture/hidden_dependencies.md).

---

## 4. Fragile modules (summary)

| Module | Fragility | Main reason |
|--------|-----------|-------------|
| Orders | High | Large; 15+ deps; central to flow; notification and job handlers. |
| Workflow | High | Single point for status changes; used everywhere. |
| Billing | High | Financial correctness; MyInvois; Workflow coupling. |
| Inventory | High | Ledger single source of truth; job executors. |
| Scheduler | High | Large; direct Order queries; hidden Workflow dependency. |
| Rates / Payroll | High | Payout and payroll; P&L and workers. |

Detail: [architecture/module_fragility_map.md](architecture/module_fragility_map.md).

---

## 5. Safe refactor zones

- Report generation (read-only), settings/reference CRUD (non-workflow), admin/auth, non-critical jobs (EmailCleanup, NotificationRetention), Tasks, Assets, RMA, Files/Documents (non–OneDrive logic), messaging templates, diagnostics/health, event bus metrics.

Detail: [architecture/safe_refactor_zones.md](architecture/safe_refactor_zones.md).

---

## 6. Dangerous refactor zones

- Order lifecycle (WorkflowEngineService, transitions, guards, side effects).
- Billing calculation and invoice creation (BillingService, InvoiceSubmissionService, MyInvoisStatusPoll).
- Inventory ledger writes and reconciliation jobs.
- Event store and dispatcher (handlers, replay).
- Workflow guards and side effects (NoSchedulingConflictsValidator, Scheduler/Notification).
- Rates and payroll (RateEngineService, OrderPayoutSnapshot, payout anomaly).
- Scheduler–Order–Workflow triangle (circular/hidden deps).
- Parser → Order (draft approve).
- Notification dispatch (outbound).

Detail: [architecture/refactor_danger_zones.md](architecture/refactor_danger_zones.md).

---

## 7. Worker dependency risks (summary)

- **BackgroundJobProcessorService:** Highest risk; resolves 10+ domain services by job type; no single doc listing job type → handler → service.
- **EventStoreDispatcherHostedService:** All domains via handlers; handler set is DI-driven, not explicit in docs.
- **OrderStatusChangedNotificationHandler:** Resolves IOrderService, IGlobalSettingsService; event handler coupling.

Detail: [architecture/worker_dependency_risks.md](architecture/worker_dependency_risks.md).

---

## 8. Suggested refactor strategy

1. **Stabilize and document:** Keep [high_coupling_modules.md](architecture/high_coupling_modules.md), [hidden_dependencies.md](architecture/hidden_dependencies.md), and [worker_dependency_risks.md](architecture/worker_dependency_risks.md) updated when adding services or workers. Add a job-type → handler registry and an event-handler index when touching job processor or event bus.
2. **Refactor in sequence:** Start from Reports and Settings (non-workflow), then Admin/Auth, then Parser (ingestion only), Buildings, Scheduler (reduce _context.Orders and explicit Workflow injection), Inventory consumers, Billing (non–status-change), Rates/Payroll/P&L, then Workflow, Orders (read then status), Event store last. See [architecture/refactor_sequence_plan.md](architecture/refactor_sequence_plan.md).
3. **Reduce hidden deps:** Replace GetRequiredService with constructor injection where feasible (e.g. SchedulerService → IWorkflowEngineService) so dependencies appear in maps and in IDE.
4. **Danger zones:** Any change in order lifecycle, billing, ledger, or event dispatcher must have integration/regression tests and staging validation; prefer feature flags for behavioral changes.

---

## 9. Artifacts created

| Document | Purpose |
|----------|---------|
| [architecture/high_coupling_modules.md](architecture/high_coupling_modules.md) | Modules ranked by coupling risk. |
| [architecture/hidden_dependencies.md](architecture/hidden_dependencies.md) | Service→service, GetRequiredService, and DbContext cross-access. |
| [architecture/module_fragility_map.md](architecture/module_fragility_map.md) | Per-module fragility (size, coupling, workers, criticality). |
| [architecture/safe_refactor_zones.md](architecture/safe_refactor_zones.md) | Lower-risk areas to refactor. |
| [architecture/refactor_danger_zones.md](architecture/refactor_danger_zones.md) | High-risk areas and mitigation. |
| [architecture/refactor_sequence_plan.md](architecture/refactor_sequence_plan.md) | Suggested refactor order (least → most critical). |
| [architecture/worker_dependency_risks.md](architecture/worker_dependency_risks.md) | Worker service dependencies and hidden coupling. |
| [REFACTOR_SAFETY_REPORT.md](REFACTOR_SAFETY_REPORT.md) | This report. |

---

**Definition of done (Level 14):** Fragile modules identified; high-coupling areas documented; hidden dependencies mapped; safe and danger zones documented; refactor sequence plan and worker dependency risks documented; REFACTOR_SAFETY_REPORT exists; docs portal links to the new analysis.
