# Controller Sprawl Watch

**Related:** [api_surface_summary.md](api_surface_summary.md) | [controller_service_map.md](controller_service_map.md) | [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md)

**Purpose:** Identify controller families that are expanding too broadly—too many endpoints, mixed concerns, or touching too many services/domains. Governance-level only.

**Last scan:** March 2026 (Level 15 watchdog). **Controller count:** 113 files under Api/Controllers (includes path normalization duplicates in glob; unique count ~90+ as in api_surface_summary).

---

## Controller sprawl risk

| Controller family | Domain | Sprawl risk | Why it matters | Related canonical docs |
|-------------------|--------|-------------|----------------|------------------------|
| **InventoryController** | Inventory | **Medium** | Many endpoints: materials, ledger, stock summary, receive/transfer/allocate/issue/return, reports, export, bins, warehouses. Single controller covers full inventory surface; department-scoped. | inventory_ledger_and_serials, api_surface_summary §1 |
| **OrdersController** | Orders | **Medium** | Core CRUD, list, status, assignment, filters (status, partner, SI, building, date, department). Primary order API; not fragmented but very central. | order_lifecycle_and_statuses, 02_modules/orders |
| **SchedulerController** | Scheduler | **Medium** | Calendar, slots, SI availability, leave, utilization. Single controller for all scheduler operations; mirrors SchedulerService breadth. | 02_modules/scheduler |
| **BillingController** | Billing | **Medium** | Invoices CRUD, PDF/preview, line items, submission. Billing surface in one controller; InvoiceSubmissionsController separate. | billing_and_invoicing, API_OVERVIEW |
| **Settings (aggregate)** | Settings | **Low–Medium** | Many small controllers (OrderTypes, BuildingTypes, DocumentTemplates, EmailTemplates, GlobalSettings, SlaProfiles, AutomationRules, etc.). Sprawl is across many controllers, not one; each is narrow. | REFERENCE_TYPES, 02_modules/global_settings |
| **ReportsController** | Reports | **Low** | Run by key, export; uses IOrderService, IStockLedgerService, ISchedulerService. Read-only aggregation; single controller for report runner. | 02_modules/reports_hub |
| **Eventing / operational** | Events, observability | **Low** | EventStoreController, EventLedgerController, EventsController, JobOrchestrationController, OperationalReplay/Rebuild/Trace, SystemWorkers, SystemScheduler, PayoutHealth, FinancialAlerts. Many controllers but each focused; documented in api_surface_summary §6. | EVENT_BUS_OPERATIONS_RUNBOOK, background_jobs |

---

## Sprawl signals (reference)

- **Too many endpoints in one controller:** one controller handling 20+ actions across multiple sub-domains.
- **Mixed concerns:** e.g. same controller handling CRUD and eventing/admin.
- **Too many controller variants for one module:** e.g. many small controllers that could be grouped (acceptable if each is single-purpose).
- **Controllers touching too many services:** e.g. one controller injecting 5+ unrelated application services.

Current state: No single controller family exceeds recommended endpoint breadth to the point of "high" sprawl; Inventory and Orders are the largest and are already documented. Settings are intentionally many small controllers.

---

**Refresh:** Re-scan when new controller files are added or when a controller gains many new actions or injected services. Update this table and ARCHITECTURE_WATCHDOG_REPORT § Controller sprawl.
