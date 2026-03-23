# Module Boundary Regression Watch

**Related:** [module_dependency_map.md](module_dependency_map.md) | [high_coupling_modules.md](high_coupling_modules.md) | [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md)

**Purpose:** Check whether module boundaries are staying clean or drifting—Orders absorbing non-order concerns, Workflow becoming universal, Scheduler performing order logic directly, Billing reaching too far, etc. Governance-level only.

**Last scan:** March 2026 (Level 15 watchdog).

---

## Module boundary status

| Module | Boundary status | Observed issues | Affected dependencies | Risk notes | Related canonical docs |
|--------|-----------------|-----------------|------------------------|------------|-------------------------|
| **Orders** | **Drifting** | OrderService has 19 constructor deps (Buildings, Workflow, 6+ Settings, Notifications, Inventory, Rates). Central to flow; many modules depend on Orders. Not regressed since Level 14 but boundary is already broad. | Parser, Scheduler, Billing, Workflow, Notifications, Reports, Agent, Buildings | Adding more deps to OrderService would worsen drift. Prefer facades or events for cross-cutting concerns. | order_lifecycle_and_statuses, 02_modules/orders |
| **Workflow** | **Drifting** | WorkflowEngineService is universal dependency for order status; used by Orders, Scheduler, Billing, Parser, Agent. Injects Scheduler, EventStore, Audit. Boundary is intentionally cross-cutting but creates cycle risk with Scheduler. | Orders, Scheduler, Billing, Parser, Agent | No new callers in this scan; drift is structural (documented). | WORKFLOW_ENGINE, refactor_danger_zones |
| **Scheduler** | **Drifting** | SchedulerService queries _context.Orders in many methods; resolves IWorkflowEngineService in five places. Performs order reads and slot–order linkage inside Scheduler; not using IOrderService for reads. | Orders, Workflow (runtime) | Reducing direct Order access and making Workflow dependency explicit would improve boundary. | 02_modules/scheduler, hidden_dependencies |
| **Billing** | **Drifting** | BillingService queries _context.Orders for invoice creation; InvoiceSubmissionService calls IWorkflowEngineService for status. Billing correctly owns Invoice but reaches into Orders and Workflow. | Orders, Workflow | Already documented; avoid adding more Order or Workflow touchpoints. | billing_and_invoicing, billing_myinvois_flow |
| **Buildings** | **Drifting** | BuildingService queries _context.Orders (OrdersCount, merge, move). Buildings module owns Building but reads/writes Order.BuildingId. | Orders | Merge and order-move are explicit features; boundary leak is accepted but should not grow. | 02_modules/buildings |
| **Inventory** | **Stable** | StockLedgerService is single source of truth; used by Orders (MaterialCollectionService), Reports, SI, job executors. No new cross-domain writes; legacy IInventoryService still in OrderService. | Orders, Reports, SI, Workers | Ledger boundary is clear; legacy InventoryService in OrderService is technical debt, not new regression. | inventory_ledger_and_serials |
| **Parser** | **Stable** | Creates orders via IOrderService; resolves buildings via IBuildingService. Entry point; does not bypass domain boundaries. | Orders, Buildings | Isolated; no new leaks in this scan. | 02_modules/email_parser |
| **Notifications** | **Stable** | Cross-cutting by design; OrderStatusChangedNotificationHandler resolves IOrderService (documented). No new core write paths. | Orders, Workflow (side effects) | Acceptable; handler coupling documented in hidden_dependencies. | 02_modules/notifications |
| **Settings / Reference** | **Stable** | Consumed by many modules; many small services. No single Settings service doing business orchestration. | All (consumers) | Boundary is "many small read-only or CRUD services"; no regression. | REFERENCE_TYPES, 02_modules/global_settings |
| **Events / Event store** | **Stable** | All domains can append; dispatcher and replay consume. Handler set is DI-driven; no new structural leak in this scan. | All (handlers) | Document handler index when event surface grows. | PHASE_8_PLATFORM_EVENT_BUS |
| **Rates / Payroll** | **Stable** | RateEngineService used by Payroll, P&L; OrderService injects IOrderPayoutSnapshotService. Coupling documented; no new callers. | Orders, P&L | Financial; avoid adding new consumers of RateEngine without doc update. | rate_engine, payroll OVERVIEW |
| **Reports** | **Stable** | Read-only aggregation; uses IOrderService, IStockLedgerService, ISchedulerService. Does not drive transitions. | Orders, Inventory, Scheduler | Clean consumer boundary. | 02_modules/reports_hub |

---

## Boundary signals (reference)

- **Orders absorbing non-order concerns:** New responsibilities (e.g. billing logic, notification rules) added to OrderService.
- **Workflow becoming universal:** New domain types (e.g. non-Order entities) transitioning through WorkflowEngineService without doc update.
- **Scheduler performing order/business logic:** New _context.Orders usage or new workflow resolution in SchedulerService.
- **Billing reaching into operational domains:** New Order or Inventory access paths in Billing services.
- **Settings leaking into business orchestration:** A single Settings service starting to coordinate Orders/Billing/Workflow.
- **Parser bypassing boundaries:** Parser creating entities directly or calling services outside Orders/Buildings.
- **Notifications/events coupled to core write paths:** New handlers that perform order/billing/inventory writes instead of side effects.

---

**Refresh:** Re-scan when a module gains new constructor dependencies, new _context.XXX usage in another domain, or new callers from unrelated modules. Update this table and ARCHITECTURE_WATCHDOG_REPORT § Module boundary.
