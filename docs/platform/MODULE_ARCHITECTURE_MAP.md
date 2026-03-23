# CephasOps Platform — Module Architecture Map

**Purpose:** Map existing implementations for orders, scheduling, inventory, billing, payroll, integration, and event platform. Module boundaries and communication via domain events.

---

## 1. Lead Management

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Parsed drafts | ParserService, ParsedOrderDraft | Application/Parser, Domain/Parser | Email ingest → draft; CreateOrderFromDraftAsync creates order. |
| Leads (if distinct) | — | — | No separate lead entity; drafts and orders cover pipeline. |

---

## 2. Work Orders (Orders)

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Order CRUD | IOrderService, OrderService | Application/Orders/Services | CreateOrderAsync, CreateFromParsedDraftAsync, UpdateOrderAsync, ChangeOrderStatusAsync, GetOrdersPagedAsync. |
| Order entity | Order | Domain/Orders/Entities | CompanyId, PartnerId, Status, BuildingId, AssignedSiId, etc. |
| Order types/categories | IOrderTypeService, IOrderCategoryService | Application/Orders/Services | OrderType, OrderCategory. |
| Status checklist | IOrderStatusChecklistService | Application/Orders/Services | Per-status checklist items. |
| API | OrdersController | Api/Controllers | REST for orders; department/company scoped. |
| Events | OrderCreatedEvent, OrderStatusChangedEvent, OrderAssignedEvent | Application/Events | OrderCreated: emitted from OrderService (this integration). OrderStatusChanged/OrderAssigned: WorkflowEngineService. |

---

## 3. Scheduling

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Scheduler | ISchedulerService | Application/Scheduler/Services | Slots, assignment, availability. |
| Time slots | TimeSlotsController | Api/Controllers | Time slot management. |
| Workflow jobs | WorkflowEngineService, WorkflowJob | Application/Workflow, Domain/Workflow | Transition execution; order status → Assigned triggers OrderAssignedEvent. |
| API | SchedulerController | Api/Controllers | Scheduling endpoints. |

---

## 4. Installer Workforce

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Service installers | ServiceInstallersController, SI entities | Api/Controllers, Domain/ServiceInstallers | Installer CRUD, assignment. |
| Tasks | InstallerTask, TasksController | Domain, Api/Controllers | Order-linked tasks; OrderAssignedOperationsHandler creates task on OrderAssignedEvent. |
| SI app | SiAppController | Api/Controllers | Mobile-first installer app. |

---

## 5. Inventory

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Inventory | IInventoryService | Application/Inventory/Services | Stock, movements. |
| Stock ledger | IStockLedgerService | Application/Inventory | Ledger, allocations. |
| Material usage | OrderMaterialUsageService, RecordMaterialUsageAsync | Application/Orders/Services | Record usage per order. |
| Warehouses, bins | WarehousesController, BinsController | Api/Controllers | Location hierarchy. |
| API | InventoryController | Api/Controllers | Inventory endpoints; tenant-scoped. |

---

## 6. Billing

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Invoicing | IBillingService, BillingService | Application/Billing/Services | Create invoice, GenerateInvoicePdfAsync. |
| Invoice submissions | InvoiceSubmissionsController | Api/Controllers | MyInvois submission. |
| Subscription (SaaS) | ITenantSubscriptionService, TenantSubscription | Application/Billing/Subscription, Domain/Billing | Plans, tenant subscriptions. |
| Billing plans | BillingPlansController, TenantSubscriptionsController | Api/Controllers | Plan and subscription management. |
| API | BillingController | Api/Controllers | Billing endpoints. |

---

## 7. Payroll

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Payroll | PayrollController, payroll services | Api/Controllers, Application | Periods, runs, job earnings. |
| Payout snapshots | IOrderPayoutSnapshotService | Application/Rates/Services | Snapshot on OrderCompleted/Completed. |
| Rates | RatesController, Gpon* controllers | Api/Controllers | Rate cards, GPON rates. |
| PnL | PnlController | Api/Controllers | P&L reporting. |

---

## 8. Integration Bus

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Outbound | IOutboundIntegrationBus, OutboundIntegrationDelivery | Application/Integration | PublishAsync(PlatformEventEnvelope); deliveries per endpoint. |
| Inbound | IInboundWebhookRuntime, InboundWebhookReceipt | Application/Integration | POST webhooks; receipt + idempotency. |
| Connectors | IConnectorRegistry, ConnectorDefinition, ConnectorEndpoint | Application/Integration | Per-event-type and per-connector. |
| Forwarding | IntegrationEventForwardingHandler | Application/Integration | Domain events → outbound bus. |
| API | IntegrationController, WebhooksController | Api/Controllers | Connectors, deliveries, receipts, replay. |

---

## 9. Event Platform

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Store | IEventStore, EventStoreEntry, EventStoreRepository | Domain/Events, Infrastructure/Persistence | AppendInCurrentTransaction, AppendAsync, claim, MarkProcessed. |
| Bus | IEventBus, IDomainEventDispatcher | Application/Events | PublishAsync, DispatchAsync; subscribe via IDomainEventHandler&lt;T&gt;. |
| Worker | EventStoreDispatcherHostedService | Application/Events | Poll, claim, dispatch. |
| Replay | IEventReplayService, IOperationalReplayExecutionService | Application/Events/Replay | Single and batch replay; tenant-scoped. |
| Observability | GET /api/observability/events, EventStoreController | Api/Controllers | List, filter, retry, lineage. |
| Docs | docs/event-platform/ | docs | Architecture, lifecycle, handlers, replay, tenant safety. |

---

## 10. Automation Engine

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Rules | AutomationRule entity, IAutomationRuleService | Domain/Settings/Entities, Application/Settings/Services | TriggerType (StatusChange, TimeBased, EventBased), ActionType (AssignToUser, ChangeStatus, Notify, etc.). |
| API | AutomationRulesController | Api/Controllers | CRUD rules; company-scoped. |
| Event-driven execution | (This integration) | Application/Automation or Application/Events | Handler for OrderCompletedEvent (and other events) → evaluate rules → execute actions (e.g. GenerateInvoice). |

---

## 11. Reporting

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Report definitions | ReportRegistry, ReportDefinitionHubDto | Api/Reports | GetDefinitions, GetDefinition(reportKey). |
| Run report | ReportsController | Api/Controllers | Run by key; department/company scope; IOrderService, IInventoryService, ISchedulerService, etc. |
| Export | IReportExportFormatService, ICsvService | Application | CSV and other formats. |
| Tenant safety | Department and company filters | ReportsController | Enforced on run; 403 when no access. |

---

## 12. SaaS Management

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Tenants | Companies, TenantProvisioningController, TenantsController | Api/Controllers | Multi-tenant; CompanyId. |
| Subscriptions | TenantSubscription, TenantSubscriptionsController | Domain/Billing, Api/Controllers | Plan, status, subscribe/cancel. |
| Feature flags | (This integration) | — | PlanFeature, FeatureFlag or TenantFeatureFlag for module enablement per tenant. |

---

## 13. Control Plane

| Area | Implementation | Location | Notes |
|------|----------------|----------|-------|
| Summary | ControlPlaneController | Api/Controllers | GET /api/admin/control-plane; list of capability groups (event-store, job-orchestration, integration, trace, observability, tenants, billing). |
| **Operations overview** | OperationsOverviewController | Api/Controllers | GET /api/admin/operations/overview; compact summary of job executions, event store (24h), payout health, system health. Internal operational visibility only (SuperAdmin/Admin + JobsView). See backend/docs/operations/OPERATIONAL_OBSERVABILITY_REPORT.md. |
| Event replay | EventStoreController | Api/Controllers | Retry, replay by event id. |
| Job replay | JobOrchestrationController, BackgroundJobsController | Api/Controllers | Job runs, enqueue. |
| Integration diagnostics | IntegrationController | Api/Controllers | Connectors, deliveries, receipts. |
| Tenant diagnostics | (This integration) | — | Optional tenant health/diagnostics endpoint. |

---

## 14. Module Boundaries and Event Flow

- **Orders:** Emits OrderCreatedEvent (on create), OrderStatusChangedEvent / OrderAssignedEvent (via WorkflowEngineService). Consumes: workflow transitions, automation rules.
- **Scheduler:** Consumes order/assignment; exposes slots and assignment. Communicates via workflow and order status.
- **Inventory:** Consumes material usage; emits MaterialIssuedEvent / MaterialReturnedEvent (this integration). Reports consume inventory data.
- **Billing:** Emits InvoiceGeneratedEvent (this integration). Can be triggered by automation (OrderCompleted → GenerateInvoice).
- **Payroll:** Emits PayrollCalculatedEvent (this integration). Consumes order/payout data.
- **Integrations:** Consumes domain events (forwarding handler); exposes outbound/inbound APIs.
- **Automation:** Consumes domain events (OrderCompleted, etc.); executes rules and actions (ChangeStatus, Notify, GenerateInvoice).
- **Reporting:** Consumes all modules (read-only); always filters by tenant/department.
- **Event platform:** Central bus and store; all modules publish or consume via events.

---

## 15. Cross-References

- Event platform: `docs/event-platform/`
- Architecture: `docs/architecture/`
- API surface: `docs/architecture/api_surface_summary.md`
