# Module Dependency Map

**Related:** [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md) | [controller_service_map.md](controller_service_map.md)

**Purpose:** Upstream/downstream dependencies per major module; cross-cutting services; related frontend and docs.

---

## Dependency overview (ASCII)

```
Parser
  → Orders (draft approve → order)
  → Buildings (resolve address/building)
  → Background jobs (EmailIngest)

Orders
  → Scheduler (slots, assignment)
  → Inventory (materials, allocation)
  → Workflow (transitions, guards)
  → Billing (invoice readiness)
  → Notifications (status updates)

Scheduler
  → ServiceInstallers (availability, leave)
  → Orders (slot assignment)
  → Departments (scope)

Inventory
  → Buildings (bins, locations)
  → Orders (allocation)
  → RMA (returns)
  → Departments (scope)

Billing
  → Orders (invoice from order)
  → MyInvois (e-invoice)
  → Payments
  → Departments (scope)

Workflow
  → Orders (status transitions)
  → Notifications (side effects)
  → Scheduler (side effects)
  → Events (domain events)

Rates / Payroll
  → Orders (earnings)
  → ServiceInstallers (rate plans)
  → P&L (inputs)
  → Departments (scope)

P&L
  → Orders, Payroll, Overheads (aggregation)
  → Event store / replay (rebuild job)
  → Departments (scope)

Events / Event store
  → All domains (handlers consume events)
  → Operational replay / rebuild

Notifications
  → Orders, Scheduler (triggers)
  → SMS/WhatsApp/Email (delivery)
  → Event-driven dispatch worker
```

---

## Per-module summary

| Module | Upstream | Downstream | Cross-cutting | Frontend | Docs |
|--------|----------|------------|----------------|----------|------|
| **Parser** | Email (POP3/IMAP), Partner portals (manual) | Orders, Buildings | BackgroundJobProcessorService, EmailIngestionSchedulerService | Parser pages, Email accounts | 02_modules/email_parser, 20_workflow_email_to_order |
| **Orders** | Parser, Admin (manual create) | Scheduler, Inventory, Workflow, Billing, Notifications | WorkflowEngineService, DepartmentAccessService | Orders, Order status, Assignment | order_lifecycle_and_statuses, 02_modules/orders |
| **Scheduler** | Orders | ServiceInstallers, Slots | DepartmentAccessService | Calendar, Slots, SI availability | 02_modules/scheduler |
| **Inventory** | Buildings, Orders (allocation) | Billing (materials on invoice), RMA | StockLedgerService, DepartmentAccessService | Inventory, Bins, Warehouses | inventory_ledger_and_serials, 02_modules/inventory |
| **Billing** | Orders (ready for invoice) | MyInvois, Payments, P&L | DepartmentAccessService | Invoices, Payments, Invoice submission | billing_and_invoicing, billing_myinvois_flow |
| **Buildings** | — | Orders, Inventory (locations), Scheduler (zones) | DepartmentAccessService | Buildings, Types, Splitters | 02_modules/buildings |
| **Workflow** | Order status config | Orders (transitions), Notifications, Scheduler (side effects) | WorkflowEngineService | Workflow definitions, Guards | WORKFLOW_ENGINE, order_lifecycle_and_statuses |
| **Rates / Payroll** | Order types, SI rate plans, Partner rates | P&L, Payroll runs | RateEngineService, DepartmentAccessService | Rates, Payroll, GPON rate groups | rate_engine, payroll OVERVIEW |
| **P&L** | Orders, Payroll, Overheads | Reports | PnlRebuildSchedulerService, JobExecutionWorker | P&L drilldown | pnl_boundaries, 02_modules/pnl |
| **Notifications** | Events, Order/SI triggers | SMS, WhatsApp, Email (IUnifiedMessagingService, etc.) | NotificationDispatchWorkerHostedService | Notifications list, Templates | 02_modules/notifications, integrations/overview |
| **Events** | All domains (append) | EventStoreDispatcherHostedService, Replay/Rebuild | DomainEventDispatcher, IEventStore | Event store API, Replay UI | PHASE_8_PLATFORM_EVENT_BUS, EVENT_BUS_OPERATIONS_RUNBOOK |
| **Settings / Reference** | — | Orders, Scheduler, Billing, Workflow, Parser, etc. | DepartmentAccessService | Many settings pages | REFERENCE_TYPES, 02_modules/global_settings |
| **Departments / RBAC** | — | All department-scoped modules | DepartmentAccessService | Departments, Admin users | department_rbac |

---

**Refresh:** When adding new modules or changing dependency direction, update this map and CODEBASE_INTELLIGENCE_MAP §4 (core flows).
