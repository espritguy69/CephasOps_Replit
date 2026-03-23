# Module Fragility Map

**Related:** [high_coupling_modules.md](high_coupling_modules.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Per-module fragility assessment: size/complexity, coupling, worker usage, and operational criticality. Used to prioritize refactor caution and test coverage.

---

## Fragility matrix

| Module | Fragility | Size/complexity | Coupling | Worker usage | Operational criticality | Reason |
|--------|-----------|------------------|----------|---------------|--------------------------|--------|
| **Orders** | **High** | Large (OrderService 3000+ LOC; many DTOs/entities) | High (15+ injected services; referenced by Parser, Scheduler, Billing, Workflow, Notifications, Reports, Agent, Buildings) | OrderStatusChangedNotificationHandler; job handlers create/update orders | Core: all flows start or pass through orders | Central domain; any break blocks main flow. |
| **Workflow** | **High** | Large (WorkflowEngineService, guards, side-effect registry) | High (used by Orders, Scheduler, Billing, Parser, Agent; injects Scheduler, EventStore, Audit) | Side effects invoked from transitions | Critical: every order status change | Single point of failure for lifecycle. |
| **Billing** | **High** | Medium–large (BillingService, InvoiceSubmissionService, MyInvois) | High (Orders, Inventory, Rates, Workflow; MyInvoisStatusPoll job) | BackgroundJobProcessorService (MyInvoisStatusPoll) | Financial correctness; compliance | Invoice and e-invoice errors are high impact. |
| **Inventory** | **High** | Medium (StockLedgerService, ledger-centric; legacy InventoryService) | Medium–High (Orders, Reports, SI, job executors) | ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots, InventoryReportExport | Ledger is source of truth; stock errors affect operations | No direct balance writes; refactor of ledger logic is risky. |
| **Scheduler** | **High** | Large (SchedulerService many methods; direct Order queries) | High (Workflow, Orders; SchedulerService resolves Workflow at runtime) | — | Slots and SI assignment drive field work | Coordination complexity; hidden Workflow dependency. |
| **Rates / Payroll** | **High** | Medium (RateEngineService, PayrollService, payout snapshots) | Medium (Orders, P&L; OrderService uses OrderPayoutSnapshotService) | PnlRebuild, MissingPayoutSnapshot, PayoutAnomalyAlert | Payout correctness; payroll runs | Financial and operational; rate changes affect P&L and payroll. |
| **Parser** | **Medium** | Large (EmailIngestionService, ParserService, many templates) | Medium (Orders, Buildings; job-driven) | EmailIngestionSchedulerService; BackgroundJobProcessorService (EmailIngest) | Entry point for orders | Mostly ingestion; approve→order is critical path. |
| **Events / Event store** | **Medium** | Medium (EventStore, dispatcher, replay/rebuild) | Medium (all domains append; handlers vary) | EventStoreDispatcherHostedService; replay/rebuild jobs | Replay and audit | Handler changes can break replay or downstream. |
| **Notifications** | **Medium** | Medium (NotificationService, dispatch, templates) | Medium (Orders, Workflow side effects, event handlers) | NotificationDispatchWorkerHostedService | User and SI notifications | Cross-cutting; failures may be non-blocking. |
| **P&L** | **Medium** | Medium (Pnl services, OrderProfitabilityService) | Medium (Orders, Payroll, RateEngine) | PnlRebuildSchedulerService; JobExecutionWorker (PnlRebuild) | Analytics only (not GL) | Rebuild job and rate dependency. |
| **Buildings** | **Medium** | Medium (BuildingService, types, merge) | Medium (Orders, Parser, Inventory; BuildingService queries Orders) | — | Address and location data | Merge and order move are sensitive. |
| **Settings / Reference** | **Medium** | High (many small services and controllers) | Medium (consumed everywhere) | — | Configuration and workflow definitions | Many small surfaces; low single-point risk. |
| **Reports** | **Low** | Medium (report runner, many report keys) | Low–Medium (reads Orders, Inventory, Scheduler) | InventoryReportExport (async job) | Read-only reporting | Safe if read-only contracts preserved. |
| **Departments / RBAC** | **Medium** | Small–medium (DepartmentAccessService, scope checks) | High (injected in 20+ controllers) | — | Access control | Cross-cutting; 403 behavior must stay consistent. |

---

## Fragility signals used

- **Size complexity:** Many controllers/services/entities or very large service files (e.g. OrderService, SchedulerService).
- **Coupling:** From [high_coupling_modules.md](high_coupling_modules.md) and [hidden_dependencies.md](hidden_dependencies.md).
- **Worker usage:** Module is used by background workers or event handlers that run without direct API call.
- **Operational criticality:** Affects orders, billing, payments, inventory, installer payouts, or compliance (MyInvois).

---

**Refresh:** When adding new services, workers, or critical flows, re-evaluate the row for that module and any modules it couples to.
