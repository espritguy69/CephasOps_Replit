# CephasOps Platform Boundaries and Extraction Roadmap

**Date:** 2026-03-09  
**Purpose:** Define current bounded contexts, target distributed boundaries, ownership rules, forbidden patterns, and future extraction order.  
**Phase 1:** No deployable split; boundaries are logical and enforced by convention and contracts.

---

## 1. Current bounded contexts (logical)

| Context | Owner module | Key entities / services | Integration style today |
|---------|----------------|--------------------------|---------------------------|
| **Identity / Tenant** | Departments, Users, Companies (removed but CompanyId remains) | User, Role, Permission, Department, DepartmentAccess | In-process; JWT and department scope. |
| **Orders** | Orders | Order, OrderType, OrderCategory, OrderService | In-process; workflow and scheduler call OrderService. |
| **Workflow** | Workflow | WorkflowEngineService, WorkflowJob, WorkflowDefinition | Emits events (outbox); inline side effects and scheduler. |
| **Job orchestration** | Workflow + Workers | BackgroundJob, BackgroundJobProcessorService, WorkerCoordinator | In-process job queue; DB-backed. |
| **Inventory** | Inventory | StockLedgerService, StockLedgerEntry, SerialisedItem, LedgerBalanceCache | In-process; used by Orders and Billing. |
| **Billing** | Billing | BillingService, Invoice, InvoiceSubmissionService | In-process; depends on Orders. |
| **Payroll / Payout** | Payroll, Rates | PayrollService, rate engine, payout snapshots | In-process; depends on Orders and ServiceInstallers. |
| **Notifications** | Notifications | NotificationService, templates, OrderStatusChangedNotificationHandler | In-process; called from workflow and event handler. |
| **Reporting / Read models** | Pnl, Reports, Events (ledger) | PnlService, LedgerEntry, WorkflowTransitionHistoryEntry | In-process; some projection-style handlers. |
| **Parser / Ingestion** | Parser | ParserService, EmailIngestionService, ParsedOrderDraft | In-process; creates drafts/orders. |
| **Scheduler (slots)** | Scheduler | SchedulerService, ScheduledSlot | In-process; called from Workflow. |
| **Events / Bus** | Events | EventStore, EventStoreDispatcherHostedService, EventProcessingLog | Outbox + inbox; internal event bus. |

---

## 2. Target distributed boundaries (future)

Same list, with clear ownership and integration rules:

- **Identity/Tenant** — Auth, users, roles, departments, tenant (CompanyId) context. All other boundaries resolve tenant from request or event.
- **Orders** — Order lifecycle, status, types, categories. Consumes: workflow events. Exposes: order CRUD and status.
- **Workflow** — Transitions, definitions, guard conditions, side-effect orchestration. Emits: workflow and order events. Consumes: scheduler, notifications via events.
- **Job orchestration** — Background job queue, worker coordination, job definitions. Can be extracted as “Job API” later.
- **Inventory** — Stock ledger, serials, balance cache. Consumes: order/ledger events. Exposes: stock and movement APIs.
- **Billing** — Invoices, MyInvois. Consumes: order data. Exposes: invoice APIs.
- **Payroll/Payout** — Earnings, rates, payout snapshots, anomaly alerts. Consumes: order and SI data.
- **Notifications** — Templates, channels (SMS, WhatsApp, Email). Consumes: notification events. Exposes: send and template APIs.
- **Reporting/Read models** — P&L, dashboards, ledger-based timeline. Consumes: events; writes projections.
- **Parser** — Email ingestion, drafts. Can emit “draft created” / “order created” events later.
- **Scheduler** — Slots, availability. Called by workflow; can become event-driven.
- **Events/Bus** — Stays as platform capability: outbox, inbox, dispatch, replay.

---

## 3. Ownership rules

- Each context has a **primary application assembly area** (folder/namespace) in CephasOps.Application and corresponding Domain entities.
- **Commands and queries** that modify or read a context’s aggregate should live in that context’s application layer.
- **Cross-context writes** are allowed only via: (1) domain events (outbox), or (2) documented exceptions (e.g. workflow writing to Order status in same transaction). No ad-hoc direct DB writes from one context into another context’s tables without documentation.
- **Reads** across contexts are allowed for orchestration and reporting but should move to projections/read models where they become heavy or shared.

---

## 4. Forbidden cross-module access patterns

- **Do not** add new direct references from one bounded context’s “write” path into another context’s repository or aggregate without documenting as technical debt.
- **Do not** publish domain events without going through the event envelope (EventId, EventType, Version, CompanyId, CorrelationId, CausationId, etc.) and without using the outbox (AppendInCurrentTransaction in same transaction as state change) where consistency is required.
- **Do not** bypass idempotency: event handlers must be idempotent; use EventProcessingLogStore (inbox) for at-most-once completion per handler.
- **Do not** introduce new “fire-and-forget” event publish that is not durable (no AppendInCurrentTransaction + worker dispatch).

---

## 5. Future extraction order (recommended)

1. **Notifications** — Clear interface (INotificationService); event-driven triggers; minimal shared state.  
2. **Job orchestration** — BackgroundJob + processor already isolated; could become a separate worker service with same DB or message queue.  
3. **Payroll/Payout** — Rate engine and payout logic; read-heavy and report-heavy; good candidate after read-model strategy.  
4. **Inventory** — Critical path; extract after event-driven integration is proven for orders and ledger.  
5. **Reporting** — Projections and read models first; then reporting API can sit on top of read-model store.  
6. **Workflow** — Core orchestration; extract once notifications and scheduler are event-driven and boundaries are stable.

---

## 6. Service extraction seams (Phase 1)

Without splitting deployables, the following contracts define seams:

- **Notifications:** `INotificationService` — send and template operations. Implementation today: in-process. Future: same interface, implementation can call notification service API or message.
- **Workflow:** `IWorkflowEngineService` — execute transition, get allowed transitions, can transition. Implementation: in-process. Future: same interface, implementation can call workflow API.
- **Job orchestration:** `IWorkerCoordinator`, `BackgroundJobProcessorService` (job type execution). Contract: job payloads and types; future: job queue could be external with same payload contract.
- **Payroll:** `IPayrollService`, rate resolution interfaces. Contract: inputs/outputs for payroll and rates; future: payroll service API.
- **Inventory:** `IStockLedgerService`, `IInventoryService`. Contract: movements, balances, serials; future: inventory API.
- **Reporting:** PnlService, report services. Contract: query DTOs and parameters; future: reporting API or read-model queries.

All new cross-boundary integration in Phase 1 must use these (or new) interfaces and domain events where applicable; no hidden direct dependency chains.
