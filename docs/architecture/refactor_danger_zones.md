# Refactor Danger Zones

**Related:** [safe_refactor_zones.md](safe_refactor_zones.md) | [high_coupling_modules.md](high_coupling_modules.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Areas where refactoring is high-risk: critical path, financial correctness, single source of truth, or heavy cross-domain coupling. Changes here require full regression tests, clear rollback, and often feature flags.

---

## Danger zones (high-risk refactor)

| Zone | Risks | Why dangerous |
|------|--------|----------------|
| **Order lifecycle (status transitions)** | Broken transitions, wrong guard evaluation, side effects not firing or firing twice. | WorkflowEngineService is the single entry point for order status changes. Orders, Scheduler, Billing, Parser, and Agent depend on it. SchedulerService resolves IWorkflowEngineService in five places (hidden). Changing transition rules or guard/side-effect contracts can block or corrupt the main flow. |
| **Billing calculation and invoice creation** | Wrong amounts, duplicate invoices, MyInvois submission failures. | BillingService queries _context.Orders and builds invoices; InvoiceSubmissionService calls IWorkflowEngineService for status (e.g. SubmittedToPortal). MyInvoisStatusPoll job in BackgroundJobProcessorService. Financial and compliance impact; rollback may require manual correction. |
| **Inventory ledger (writes and reconciliation)** | Incorrect balances, double issue, reconciliation job failures. | StockLedgerService is the single source of truth; no direct StockBalance.Quantity writes. OrderService uses MaterialCollectionService and IInventoryService (legacy). BackgroundJobProcessorService runs ReconcileLedgerBalanceCache and PopulateStockByLocationSnapshots. Refactoring ledger write paths or job handlers can corrupt stock or reports. |
| **Event store and dispatcher** | Handlers not running, duplicate dispatch, replay divergence. | EventStoreDispatcherHostedService and many domain handlers. Replay and rebuild depend on event semantics. Adding/removing handlers or changing envelope format can break replay or downstream consumers. |
| **Workflow transitions (guards and side effects)** | Transitions allowed when they should be blocked; notifications or scheduler updates skipped. | GuardConditionValidatorRegistry and SideEffectExecutorRegistry are settings-driven but code-implemented. NoSchedulingConflictsValidator resolves ISchedulerService. Changing guard or side-effect behavior affects every transition that uses them. |
| **Rates and payroll (payout calculation)** | Wrong SI payout, wrong payroll run totals, P&L drift. | RateEngineService used by PayrollService and OrderProfitabilityService (P&L). OrderService injects IOrderPayoutSnapshotService; snapshots created on order completion. PayoutAnomalyAlertSchedulerService and MissingPayoutSnapshotSchedulerService. Financial and operational; rate or snapshot logic changes need thorough testing. |
| **Scheduler–Order–Workflow triangle** | Slot/order inconsistency, double assignment, conflict check wrong. | SchedulerService queries _context.Orders in many methods; WorkflowEngineService injects ISchedulerService; SchedulerService resolves IWorkflowEngineService in five places. Circular dependency and hidden resolution; refactor of one can break the other two. |
| **Parser → Order (draft approve)** | Duplicate orders, wrong order data, building or workflow not applied. | ParserService and EmailIngestionService use IOrderService (and optional IWorkflowEngineService). Single path from email to order; bugs here create bad data at the source. |
| **Notification dispatch (outbound)** | Missed or duplicate SMS/WhatsApp/email; wrong template or recipient. | NotificationDispatchWorkerHostedService and INotificationDeliverySender. OrderStatusChangedNotificationHandler resolves IOrderService. User-visible and sometimes compliance-relevant; template and delivery contracts must stay stable. |

---

## Mitigation when refactoring danger zones

- **Order / Workflow / Scheduler:** Prefer explicit interfaces and constructor injection over GetRequiredService; add integration tests for transition + slot + conflict scenarios; consider feature flags for new behavior.
- **Billing / Invoice:** Keep invoice generation and MyInvois submission behind tests; run MyInvoisStatusPoll in a test environment; document rollback (e.g. resubmit, manual portal update).
- **Ledger:** No new direct balance writes; all changes via IStockLedgerService; run ReconcileLedgerBalanceCache and snapshot jobs in staging; validate report outputs after refactor.
- **Event store / dispatcher:** Version event payloads or handlers if needed; run replay tests; keep EVENT_BUS_OPERATIONS_RUNBOOK updated.
- **Rates / Payroll / P&L:** Lock down RateEngineService and OrderPayoutSnapshotService contracts; add payroll and P&L rebuild tests; validate payout anomaly alerts in staging.

---

**Refresh:** When adding new critical paths (e.g. new order status, new billing step, new ledger operation), add or update a danger zone and mitigation.
