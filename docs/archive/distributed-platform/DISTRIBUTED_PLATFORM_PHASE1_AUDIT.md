# CephasOps Distributed Platform — Phase 1 Architecture Audit

**Date:** 2026-03-09  
**Purpose:** Identify bounded contexts, coupling, side effects, event behavior, tenant scoping, and insertion points for distributed readiness.  
**Scope:** Backend solution (Domain, Application, Infrastructure, Api). No frontend changes unless API contract or projection change.

---

## 1. Solution structure (current)

- **CephasOps.Domain** — Entities, value objects, `IDomainEvent`, `IEventStore`, `EventStoreEntry`, `EventProcessingLog`, workflow/scheduler/inventory/billing/payroll/parser/notifications/settings domain types.
- **CephasOps.Application** — Use cases, DTOs, services by feature: Orders, Workflow, Parser, Scheduler, Inventory, Billing, Payroll, Pnl, Notifications, Settings, Buildings, ServiceInstallers, Tasks, Companies/Partners, Departments, Audit, Events (dispatcher, replay, ledger), Workers, Rebuild, Trace, Admin, Rates, Sla, etc.
- **CephasOps.Infrastructure** — Persistence (`ApplicationDbContext`, EF configs, migrations), `EventStoreRepository`, file storage, external integrations.
- **CephasOps.Api** — Controllers, auth, health checks, hosted services (event dispatcher, background job processor, schedulers, worker heartbeat).

Single deploy: one process hosts API, event dispatcher, background job processor, and all schedulers. Department-scoped RBAC; optional CompanyId on entities (nullable after company-feature removal).

---

## 2. Bounded contexts (hidden in modular monolith)

| Area | Evidence | Notes |
|------|----------|--------|
| **Orders** | OrderService, Order entity, OrderType, OrderCategory, status lifecycle | Core aggregate; workflow and scheduler depend on it. |
| **Workflow** | WorkflowEngineService, WorkflowJob, WorkflowDefinition, transitions, guard conditions, side effects | Owns transition execution; emits domain events (transactional outbox). |
| **Parser / Email ingestion** | ParserService, EmailIngestionService, ParsedOrderDraft, EmailAccount, background job type `emailingest` | Creates order drafts; can create orders. |
| **Scheduler / Job orchestration** | SchedulerService, BackgroundJob, BackgroundJobProcessorService, JobDefinitions, WorkerCoordinator, JobPollingCoordinatorService | Generic job queue + workflow jobs; many job types (pnlrebuild, reconcileledgerbalancecache, etc.). |
| **Inventory** | StockLedgerService, StockLedgerEntry, StockBalance, SerialisedItem, LedgerBalanceCache, StockByLocationSnapshot | Strong consistency; used by orders and billing. |
| **Billing** | BillingService, Invoice, InvoiceLineItem, MyInvois submission | Depends on orders; invoice generation and submission. |
| **Payroll / Payout** | PayrollService, rate engine, earnings, payout snapshots, anomaly alerts | Depends on orders and service installers. |
| **Notifications** | NotificationService, OrderStatusChangedNotificationHandler, SMS/WhatsApp/Email templates | Triggered from workflow (inline call + event handler). |
| **Reporting / P&L** | PnlService, reports, rebuild jobs | Reads from orders, inventory, payroll; heavy reads. |
| **Events / Ledger** | EventStore, EventStoreDispatcherHostedService, EventProcessingLog, LedgerEntry, WorkflowTransitionHistory | Outbox + inbox; projection-style handlers. |

---

## 3. Tight coupling and cross-module access

- **WorkflowEngineService** — Depends on: SchedulerService (slot creation), OrderStatusChangedNotificationHandler (inline), IAuditLogService, IEventStore, IOrderPricingContextResolver, IEffectiveScopeResolver, guard/side-effect registries. Notification is invoked synchronously inside transition; event emission is correct (AppendInCurrentTransaction).
- **OrderService** — Large surface; touches buildings, inventory, workflow, scheduler, departments. Direct DB and service calls across concerns.
- **BackgroundJobProcessorService** — Switch on job type; calls into PnlService, InventoryService, StockLedgerService, report export, etc. Single place for job execution; acceptable for Phase 1.
- **Parser** — Creates drafts and can create orders; depends on OrderService/order creation. Single process; no event-driven order creation yet.
- **Department scope** — Many queries filter by department (or company); `IDepartmentAccessService`, `DepartmentRequestContext`. Department is the primary scope for RBAC and data visibility.

---

## 4. Synchronous side effects in request handlers

- **Workflow transition** — `OrderStatusChangedNotificationHandler.HandleAsync` is invoked synchronously during transition (in addition to event-driven path). Documented as “background notification handler” but runs inline before commit.
- **Audit** — Audit log written in same request (same transaction or immediate post-commit) in multiple services.
- **Document generation** — Some document generation triggered from API; heavy work may block request.
- **Scheduler slot creation** — WorkflowEngineService calls SchedulerService during transition (same request).

Recommendation: Move non-critical notifications and heavy recalculations to event handlers or background jobs; keep audit and critical slot creation in transaction where consistency is required.

---

## 5. Background job entry points

- **BackgroundJob** entity — JobType, Payload (JSON), State, Priority, ScheduledAt, WorkerId (ownership). Enqueued by: EmailIngestionSchedulerService, RebuildJobEnqueuer, ReplayJobEnqueuer, manual/API.
- **BackgroundJobProcessorService** — Polls/claims via WorkerCoordinator; dispatches by JobType (emailingest, pnlrebuild, reconcileledgerbalancecache, populatestockbylocationsnapshots, inventoryreportexport, etc.).
- **EventStoreDispatcherHostedService** — Claims from EventStore (Pending/Failed due retry); deserializes and dispatches via IDomainEventDispatcher. Separate from BackgroundJob.
- **Other hosted services** — EmailIngestionSchedulerService, StockSnapshotSchedulerService, LedgerReconciliationSchedulerService, PnlRebuildSchedulerService, SlaEvaluationSchedulerService, MissingPayoutSnapshotSchedulerService, WorkerHeartbeatHostedService, JobPollingCoordinatorService.

---

## 6. Event-like behavior already present

- **Domain events** — WorkflowTransitionCompletedEvent, OrderStatusChangedEvent, OrderAssignedEvent. Stored in EventStore; worker dispatches; handlers: WorkflowTransitionCompletedEventHandler, WorkflowTransitionHistoryProjectionHandler, WorkflowTransitionLedgerHandler, OrderLifecycleLedgerHandler, OrderAssignedOperationsHandler.
- **Transactional outbox** — WorkflowEngineService uses `IEventStore.AppendInCurrentTransaction(evt)` then `SaveChangesAsync` in same transaction. No other callers use AppendInCurrentTransaction today; any other “publish” uses dispatcher’s AppendAsync (separate transaction).
- **Idempotency** — EventProcessingLogStore: TryClaimAsync per (EventId, HandlerName); at most one successful completion per handler per event.
- **Replay** — EventStoreQueryService, EventReplayService, ReplayOperation, replay policies. Replay can target Projection to rebuild read models.

---

## 7. CompanyId / tenant scoping

- **Current state** — Company feature was “removed” (tables dropped, CompanyId made nullable). CompanyId still exists on many entities and DTOs; used for scoping in P&L, payroll, inventory, and workflow (companyId parameter). Single-company deployment is the norm; multi-department with department-scoped RBAC.
- **Gaps** — Some queries may not filter by CompanyId where they should for future multi-tenant; global queries for “all data” exist for SuperAdmin. No global query filter on DbContext for CompanyId (intentional for current single-company).
- **Events** — WorkflowEngineService sets CompanyId on all emitted events. EventStoreEntry has CompanyId; dispatcher and observability use it.

For distributed/multi-company readiness: treat CompanyId as tenant key; ensure all commands and queries that should be tenant-scoped accept or infer CompanyId; add hardening and documentation rather than breaking current single-company behavior.

---

## 8. Direct data-access shortcuts across modules

- **Application services** — Often inject ApplicationDbContext and query multiple aggregates (e.g. Order + Building + Inventory). No strict “one aggregate per application service” rule.
- **Reporting** — PnlService and report services run complex queries across Orders, Inventory, Payroll. Good candidates for read-model/projection later.
- **No shared kernel** — Domain is one assembly; no physical boundary between “contexts”; boundaries are logical and documented.

---

## 9. Reporting queries that should become projections

- **WorkflowTransitionHistory** — Already a projection (WorkflowTransitionHistoryProjectionHandler writes to WorkflowTransitionHistoryEntry). Read model for timeline.
- **LedgerEntry** — Event-sourced ledger (OrderLifecycleLedgerHandler, WorkflowTransitionLedgerHandler). Unified order history and timeline built from ledger.
- **P&L aggregates** — PnlService rebuilds from transactional data; rebuild job writes to Pnl tables. Could be event-driven projections in Phase 2.
- **Dashboard/counts** — Any heavy aggregation on Orders, Inventory, or Jobs could move to dedicated read-model tables fed by events.

---

## 10. Insertion points (least disruptive)

| Concern | Insertion point | Notes |
|--------|------------------|--------|
| **Event envelope** | IDomainEvent, DomainEvent, EventStoreEntry, EventStoreRepository | Add Version, CausationId; standardize EventType to ops.*.v1. |
| **Outbox** | Already in place | WorkflowEngineService pattern; document and ensure new features use AppendInCurrentTransaction + same transaction. |
| **Inbox** | EventProcessingLogStore | Already idempotent; document as processed-message/inbox. |
| **Worker pipeline** | EventStoreDispatcherHostedService, BackgroundJobProcessorService | Add correlation propagation; health and metrics already present. |
| **Tenant hardening** | Commands, queries, events | Audit CompanyId on new code; document scoping rules; optional global filter later. |
| **Projection foundation** | WorkflowTransitionHistory + Ledger | Already exist; document as first read-model strategy; add one more projection if needed. |
| **Extraction seams** | Application interfaces | INotificationService, IWorkflowEngineService, etc.; document ownership and future extraction order. |

---

## 11. Findings summary

- **Strengths:** EventStore with transactional outbox in workflow; idempotent handler processing; replay and observability; department-scoped RBAC; clear job types and worker coordination.
- **Risks:** Inline notification and some side effects in request path; no standardized event type versioning (ops.*.v1); CompanyId nullable and not enforced as tenant key; reporting largely transactional.
- **Next steps:** Standardize event envelope and event type names; document outbox/inbox pattern; add CausationId/Version to store; tenant-audit and scoping doc; projection strategy doc; extraction seams doc.
