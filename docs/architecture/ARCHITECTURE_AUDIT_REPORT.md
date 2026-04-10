# Architecture Audit Report

**Status:** Active  
**Last audit:** March 2026  
**Scope:** Code vs documentation alignment; module boundaries; source-of-truth enforcement.  
**Constraint:** Documentation and audit only; no application code changes.

---

## 1. Executive summary

### Alignment

- **Docs and architecture are broadly aligned.** The existing /docs structure (01_system through 08_infrastructure, overview, business, operations, dev, architecture, integrations, modules, archive) matches the repository and is preserved. Canonical source-of-truth docs for process flow, order lifecycle, billing, inventory, RBAC, background jobs, and developer onboarding are in place and referenced from the portal.
- **Single-company, GPON-first** is correctly reflected in product overview, scope_not_handled, and MULTI_COMPANY_STORYBOOK (with disclaimer). Department-scoped RBAC and the main flow (Email → Order → Schedule → Field → Docket → Invoice → Payment) are documented and match code intent.

### Strengths

- Clear canonical docs for order lifecycle (business/order_lifecycle_and_statuses.md), billing (modules/billing_and_invoicing.md), inventory (modules/inventory_ledger_and_serials.md).
- DOCS_MAP A–P and governance files (DOCS_INVENTORY, DOCUMENTATION_ALIGNMENT_CHECKLIST, _discrepancies, DOCS_STATUS) are maintained.
- Architecture folder (00_company-systems-overview, 10_system-architecture-flow, 20/21 workflows, api_surface_summary, data_model_overview) provides a consistent entry point.
- Event bus and platform phase docs (PHASE_8_PLATFORM_EVENT_BUS, EVENT_BUS_OPERATIONS_RUNBOOK, phase summaries in archive) exist; operational replay and notification dispatch are described in phase/summary docs.

### Gaps addressed in this audit

- **API surface summary** did not list eventing/operational controllers (EventStore, EventLedger, Events, JobOrchestration, OperationalRebuild, OperationalReplay, OperationalTrace, Trace, SystemWorkers, SystemScheduler) or NotificationsController, PayoutHealth, FinancialAlerts, GPON rate controllers. **Action:** Section 6 added to api_surface_summary.md.
- **Background jobs doc** did not list all hosted services (NotificationDispatchWorkerHostedService, EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService, JobExecutionWorkerHostedService, WorkerHeartbeatHostedService, MissingPayoutSnapshotSchedulerService, JobPollingCoordinatorService). **Action:** operations/background_jobs.md updated with full hosted-service list.
- **No single architecture audit report** at docs root. **Action:** This document created.

### Remaining risks

- Deeper code-vs-doc line-by-line verification (e.g. every controller action, every job type) was not performed; spot-check and naming-based discovery only.
- Some phase/design docs (e.g. DISTRIBUTED_PLATFORM_*, EVENT_BUS_*) are in root or operations; eventing could be summarized in one architecture doc for onboarding (optional, not done in this pass).
- DOCS_IMPLEMENTATION_TRUTH_INVENTORY may be stale if not re-run after schema or controller changes; recommend re-running periodically.

---

## 2. Repository structure observed

### Top-level

- **backend/** – .NET 10 solution (Api, Application, Domain, Infrastructure); tests in backend/tests.
- **frontend/** – React 18 + TypeScript + Vite; Syncfusion, TanStack Query.
- **frontend-si/** – SI app (mobile-first PWA).
- **docs/** – Current doc system (numbered 01–08, 99_appendix, overview, business, operations, dev, architecture, integrations, modules, archive).
- **archive/** – Snapshot of older docs (new_docs_snapshot).
- **infra/, scripts/, tests/** – Infrastructure and test assets.

### Backend layers

- **Api:** Controllers only; no business logic. 90+ controller files (some duplicates from path normalization).
- **Application:** Services by domain (Parser, Workflow, Scheduler, Events, Notifications, Rates, etc.); HostedServices (BackgroundService) for schedulers and workers.
- **Domain:** Entities, enums, interfaces (IEventStore, workflow, etc.).
- **Infrastructure:** EF Core, repositories, persistence (EventStoreRepository, NotificationDispatchStore, etc.).

### Controllers (by domain, from code)

- **Core:** Orders, Scheduler, Parser, Inventory, Billing, Buildings (+ BuildingTypes, InstallationMethods, InstallationTypes).
- **Org/people:** Departments, ServiceInstallers, SiApp, Users, Companies, Partners (PartnerGroups).
- **Settings:** OrderTypes, OrderCategories, OrderStatuses, OrderStatusChecklist, OrderChecklistAnswers, BillingRatecard, Rates, Payroll, BusinessHours, EscalationRules, ApprovalWorkflows, AutomationRules, SlaProfiles, WorkflowDefinitions, GuardConditionDefinitions, SideEffectDefinitions, DocumentTemplates, MaterialTemplates, EmailTemplates, ParserTemplates, KpiProfiles, TimeSlots, SplitterTypes, Splitters, GlobalSettings, IntegrationSettings, NotificationTemplates, GponRateGroups, GponRateGroupMappings, GponBaseWorkRates, ServiceProfiles, ServiceProfileMappings, etc.
- **Reports & P&L:** Reports, ReportDefinitions, Pnl.
- **Other:** Tasks, Assets, RMA, Files, Documents, Bins, Notifications, EmailAccounts, Emails, EmailSending, WhatsApp, Sms, Messaging, SmsTemplates, WhatsAppTemplates, SmsGateway, BackgroundJobs, AdminUsers, AdminRoles, AdminSecuritySessions, Admin, Diagnostics, Infrastructure, Logs, InvoiceSubmissions, Payments, PaymentTerms, SupplierInvoices, VipEmails, VipGroups, Warehouses, BuildingDefaultMaterials, etc.
- **Eventing / operational:** EventStore, EventLedger, Events, JobOrchestration, OperationalRebuild, OperationalReplay, OperationalTrace, Trace, SystemWorkers, SystemScheduler, PayoutHealth, FinancialAlerts.

### Hosted services (from code)

- BackgroundJobProcessorService  
- EmailIngestionSchedulerService  
- StockSnapshotSchedulerService  
- LedgerReconciliationSchedulerService  
- PnlRebuildSchedulerService  
- SlaEvaluationSchedulerService  
- PayoutAnomalyAlertSchedulerService  
- EmailCleanupService  
- NotificationDispatchWorkerHostedService  
- EventStoreDispatcherHostedService  
- EventBusMetricsCollectorHostedService  
- JobExecutionWorkerHostedService  
- WorkerHeartbeatHostedService  
- MissingPayoutSnapshotSchedulerService  
- JobPollingCoordinatorService  

---

## 3. Verified architectural truths

| Truth | Evidence |
|-------|----------|
| **Single-company** | One company context in code; docs (product_overview, scope_not_handled, MULTI_COMPANY_STORYBOOK) state single-company. |
| **GPON main workflow** | Order lifecycle and workflow docs describe GPON; 07_gpon_order_workflow seed and OrderStatus enum align. |
| **Department-scoped RBAC** | DepartmentScopeExtensions and controller checks; RBAC_MATRIX_REPORT and department_rbac.md describe it. |
| **Main flow** | Email → Parser → Order → Schedule → Field → Docket → Invoice → Payment documented in process_flows, order_lifecycle, architecture 20/21. |
| **Event store / outbox** | IEventStore, EventStoreRepository, EventStoreDispatcherHostedService; Phase 7/8 docs and EVENT_BUS_OPERATIONS_RUNBOOK. |
| **Notification dispatch** | NotificationDispatch entity, NotificationDispatchWorkerHostedService, OrderStatusNotificationDispatchHandler; Phase 2 summary and notifications OVERVIEW. |
| **Background job model** | BackgroundJob table, BackgroundJobProcessorService; JobExecutionWorkerHostedService for orchestrated jobs; operations/background_jobs.md. |
| **Ledger as source of truth** | No direct StockBalance.Quantity writes; ledger-driven; modules/inventory_ledger_and_serials.md and inventory OVERVIEW. |
| **Workflow engine** | WorkflowEngineService, ExecuteTransitionAsync, DB-driven transitions; 01_system/WORKFLOW_ENGINE, business/order_lifecycle_and_statuses. |

---

## 4. Drift findings

### A. Docs behind code (corrected in this pass)

- **architecture/api_surface_summary.md:** Missing eventing/operational controllers and explicit NotificationsController; GPON rate controllers not listed. **Action:** New section 6 added; Notifications row clarified.
- **operations/background_jobs.md:** Missing several hosted services (NotificationDispatchWorker, EventStoreDispatcher, EventBusMetricsCollector, JobExecutionWorker, WorkerHeartbeat, MissingPayoutSnapshotScheduler, JobPollingCoordinator). **Action:** Section 2 table updated.

### B. Docs overstating capabilities

- None identified. Scope_not_handled and MULTI_COMPANY_STORYBOOK correctly limit partner API, multi-company, offline SI, etc.

### C. Misplaced docs

- Event bus and phase docs are in docs root and docs/operations; acceptable. Archive used for legacy phase docs.

### D. Unresolved / deferred

- **DOCS_IMPLEMENTATION_TRUTH_INVENTORY:** May be stale; run Global Truth Guardian or equivalent when schema/controllers change (see DOCS_STATUS).
- **Eventing single doc:** Optional future doc (e.g. architecture/event_bus.md) as single onboarding entry for event store, replay, notification dispatch; not created in this pass.

---

## 5. Module boundary findings

| Module | Status | Notes |
|--------|--------|-------|
| **Orders** | Coherent | OrdersController, OrderStatusesController, workflow; docs match. |
| **Parser** | Coherent | ParserController, email ingestion, templates; 02_modules/email_parser and operations/background_jobs align. |
| **Scheduler** | Coherent | SchedulerController, slots, SI availability; SystemSchedulerController for internal scheduling. |
| **Inventory** | Coherent | InventoryController, ledger, bins; modules/inventory_ledger_and_serials canonical. |
| **Billing** | Coherent | BillingController, InvoiceSubmissions, Payments; modules/billing_and_invoicing canonical. |
| **Workflow** | Coherent | WorkflowController, WorkflowDefinitions, GuardConditionDefinitions, SideEffectDefinitions; workflow engine docs. |
| **Rates / payroll** | Coherent | RatesController, PayrollController, GponRateGroups, GponRateGroupMappings, GponBaseWorkRates; 02_modules/rate_engine, payroll. |
| **Notifications** | Coherent | NotificationsController (in-app); NotificationDispatch worker and handlers for outbound; 02_modules/notifications OVERVIEW. |
| **Events / event store** | Coherent | EventStoreController, EventLedgerController, EventsController; EventStoreDispatcherHostedService; phase docs and runbook. |
| **Operational replay / rebuild** | Coherent | OperationalReplayController, OperationalRebuildController, OperationalTraceController, TraceController; operational docs. |
| **Job orchestration** | Coherent | JobOrchestrationController, JobExecutionWorkerHostedService; DISTRIBUTED_PLATFORM_PHASE3_JOB_ORCHESTRATION_* docs. |
| **Payout health / financial alerts** | Coherent | PayoutHealthController, FinancialAlertsController; payout anomaly and alerting docs. |

No module was found to be materially blurred or oversized beyond what docs describe; settings/configuration are spread across many controllers as documented.

---

## 6. Documentation actions taken

| Action | Detail |
|--------|--------|
| **Created** | docs/ARCHITECTURE_AUDIT_REPORT.md (this file). |
| **Updated** | docs/architecture/api_surface_summary.md – added section 6 (Eventing, operational & observability) listing EventStore, EventLedger, Events, JobOrchestration, OperationalRebuild, OperationalReplay, OperationalTrace, Trace, SystemWorkers, SystemScheduler, PayoutHealth, FinancialAlerts, NotificationsController, GPON rate controllers; clarified Notifications row. |
| **Updated** | docs/operations/background_jobs.md – added missing hosted services to section 2 (NotificationDispatchWorkerHostedService, EventStoreDispatcherHostedService, EventBusMetricsCollectorHostedService, JobExecutionWorkerHostedService, WorkerHeartbeatHostedService, MissingPayoutSnapshotSchedulerService, JobPollingCoordinatorService). |
| **Updated** | docs/_discrepancies.md – set "Last validated" to March 2026; added architecture audit note in section 4 (Deferred) or validation summary. |
| **Updated** | docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md – added Architecture audit (March 2026) pass; api_surface_summary and background_jobs drift fixes. |
| **Portal** | docs/README.md and 00_QUICK_NAVIGATION.md – linked to ARCHITECTURE_AUDIT_REPORT where appropriate. |

No docs were moved or archived in this pass. No links were broken.

---

## 7. Remaining architecture risks

- **Stale implementation inventory:** DOCS_IMPLEMENTATION_TRUTH_INVENTORY should be re-run after significant schema or API changes.
- **Eventing onboarding:** New developers may need to read several docs (PHASE_8_PLATFORM_EVENT_BUS, EVENT_BUS_OPERATIONS_RUNBOOK, phase summaries); optional consolidation into one architecture/event_bus.md later.
- **Background job types:** Job type list in operations/background_jobs.md should be kept in sync when new job types are added (e.g. OperationalReplay, OperationalRebuild in processor).

---

## 8. Recommended next architecture docs

- **Optional:** architecture/event_bus.md – Single entry for event store, dispatcher, replay, notification dispatch, and observability (condensed from PHASE_8 and runbook).
- **Optional:** architecture/module_boundaries.md – One-page matrix of modules, main controllers, and dependencies (for onboarding).
- **When deployment changes:** Update 08_infrastructure and operations runbooks; consider deployment_topology.md if multi-node or worker topology becomes relevant.

---

**End of report.**  
**Entry points:** [docs/README.md](README.md) | [00_QUICK_NAVIGATION.md](00_QUICK_NAVIGATION.md) | [architecture/README.md](architecture/README.md) | [_discrepancies.md](_discrepancies.md)
