# Refactor Sequence Plan

**Related:** [safe_refactor_zones.md](safe_refactor_zones.md) | [refactor_danger_zones.md](refactor_danger_zones.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** A suggested order for refactoring modules if future cleanup or decomposition is planned. Refactor from least risky → most critical; each step should leave the system stable and tested.

---

## Suggested order (least → most critical)

| Order | Module / area | Rationale |
|-------|----------------|-----------|
| 1 | **Reports** | Read-only aggregation; no state changes. Add tests for report keys and exports; then refactor runner or add new report keys. |
| 2 | **Settings / reference data (non-workflow)** | OrderTypes, BuildingTypes, DocumentTemplates, KpiProfiles, GlobalSettings, etc. CRUD and lookup; avoid changing WorkflowDefinitions/GuardConditions/SideEffects in same pass. |
| 3 | **Admin / Auth / user management** | Isolated from order and billing flows. Secure and test RBAC and session behavior before touching core domains. |
| 4 | **Tasks, Assets, RMA** | Bounded domains; RMA tied to Inventory but contained. |
| 5 | **Files / Documents** | File and document CRUD; optional OneDrive. Keep sync contract stable. |
| 6 | **Notifications (templates and delivery config)** | Template CRUD and provider config; avoid changing dispatch worker or event handlers in this step. |
| 7 | **Parser (ingestion and templates only)** | Email ingestion and parsing; avoid changing “approve → order” path initially. Stabilize EmailIngest job and parser health. |
| 8 | **Buildings** | BuildingService and merge; reduce _context.Orders usage by going through IOrderService or clear query APIs if added. |
| 9 | **Scheduler (internal only)** | Reduce direct _context.Orders usage in SchedulerService; prefer IOrderService or read-only order APIs. Replace GetRequiredService&lt;IWorkflowEngineService&gt; with constructor injection and make Workflow dependency explicit. |
| 10 | **Inventory (ledger consumers first)** | Refactor job executors (ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots) and report usage; keep IStockLedgerService contract stable. |
| 11 | **Billing (non–status-change paths)** | Invoice CRUD and PDF; avoid changing InvoiceSubmissionService and MyInvoisStatusPoll behavior in this step. |
| 12 | **Rates / Payroll (read and calculate)** | RateEngineService and PayrollService interfaces; add tests for payout and payroll runs. Do not change OrderPayoutSnapshot creation in OrderService yet. |
| 13 | **P&L** | Pnl services and rebuild job; depends on Orders, Payroll, Rates. Stabilize after Rates. |
| 14 | **Workflow (guards and side effects)** | Guard and side-effect implementations; make Scheduler and Notification dependencies explicit; add transition tests. |
| 15 | **Orders (read and query only)** | Order query APIs and DTOs; avoid changing status change paths. |
| 16 | **Event store (handlers and replay)** | Handler contracts and replay semantics; version or document event shapes. |
| 17 | **Billing (InvoiceSubmission and MyInvois)** | InvoiceSubmissionService and Workflow status transitions; MyInvoisStatusPoll job. High impact; full regression and staging. |
| 18 | **Inventory (ledger write path)** | StockLedgerService write methods; single source of truth. Last inventory step. |
| 19 | **Orders (status and lifecycle)** | OrderService status changes, WorkflowEngineService, and OrderPayoutSnapshot. Core lifecycle. |
| 20 | **Event system (dispatcher and append)** | EventStoreDispatcherHostedService and IEventStore append/dispatch. Highest coupling and replay impact. |

---

## Principles

- **Dependency direction:** Prefer refactoring consumers before producers when the producer is central (e.g. stabilize callers of WorkflowEngineService before changing WorkflowEngineService).
- **Visibility:** Replace GetRequiredService with constructor injection where possible so dependencies appear in [controller_service_map.md](controller_service_map.md) and [hidden_dependencies.md](hidden_dependencies.md).
- **Tests:** Each step should add or run regression tests for the area touched; danger zones (order lifecycle, billing, ledger) need integration tests.
- **No big-bang:** Do not refactor Orders, Workflow, and Scheduler in one pass; break into “read vs write” and “explicit dependencies” first.

---

**Refresh:** When module boundaries or coupling change, reorder or split steps. Keep [refactor_danger_zones.md](refactor_danger_zones.md) and [high_coupling_modules.md](high_coupling_modules.md) in sync.
