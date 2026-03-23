# Feature Completion Matrix & Completion Status Report

**Last run:** 2026-02-09  
**Scope:** Entire repo (backend + frontend + frontend-si + migrations + seeds)  
**Reference:** [DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md](./DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md), [_discrepancies.md](./_discrepancies.md)  
**Operational goal:** GPON contractor end-to-end flow — Email → Order → Assign/Schedule → SI execution → Docket → Invoice → MyInvois → Payment tracking  

**Phase 0 + Phase 1 + Phase 2 + Phase 3 + Phase 4 + Phase 5:** COMPLETE (2026-02-09)

**Overall GPON Go-Live Readiness:** ✅ **Ready** — Phases 0–5 complete. Core modules (Orders, Scheduler, SI app, Dockets, Billing, MyInvois, Inventory, P&L, Reports) operational. See [GO_LIVE_READINESS_CHECKLIST_GPON.md](./GO_LIVE_READINESS_CHECKLIST_GPON.md).

---

## Executive Summary

**What is production-ready today**

- **Core order flow (email → order → schedule → SI execution):** Email ingestion, parser, order CRUD, scheduler (calendar + timeline), SI app (jobs, status transitions, materials, photos, reschedule, mark faulty) are implemented end-to-end with backend, frontend, and DB support.
- **Docket path:** OrderDocket entity, DocketsReceived → DocketsVerified → DocketsUploaded statuses; DocketsPage at /operations/dockets (checklist, verify, mark uploaded, file upload); Job Docket document generation.
- **Billing/Invoices:** Invoice CRUD, PDF generation, line items, partner filtering, InvoiceSubmissionHistory, InvoiceSubmissionsController, MyInvois provider (backend + status poll job) are implemented. UI: InvoicesListPage, InvoiceDetailPage, InvoiceEditPage. MyInvois requires configuration (GlobalSettings) and credentials.
- **Inventory/Ledger:** StockLedgerEntry, StockAllocation, SerialisedItem, StockBalance, StockMovement, LedgerBalanceCache, StockByLocationSnapshots; receive/allocate/issue/return/transfer; multiple inventory pages; Reports Hub with stock/ledger reports.
- **RBAC/Departments:** DepartmentMembership, DepartmentAccessService, department-scoped filtering on Orders, Inventory, Scheduler, Reports, and many settings. RBAC_MATRIX_REPORT.md documents endpoints.
- **Rates/Payroll/P&L:** RateEngineService (GponPartnerJobRate, GponSiJobRate), PayrollService, PnlService; SiRatePlans, PartnerRates, P&L summary/drilldown/overheads; JobEarningRecords.
- **Workflow engine:** DB-driven WorkflowDefinitions, WorkflowTransitions; WorkflowEngineService with guards and side effects; fallback in OrderStatusesController. Script `07_gpon_order_workflow.sql` (idempotent) seeds full GPON lifecycle; run via `run-all-seeds.ps1`.
- **Integrations:** Email (POP3/IMAP), MyInvois (provider + status poll), WhatsApp/SMS (Twilio, null provider), OneDrive (IOneDriveSyncService). Partner portals: no API; manual.
- **Reporting:** ReportsController, ReportRegistry, ReportRunnerPage; orders, materials, stock, ledger, scheduler reports.

**Gaps affecting go-live**

- ~~Workflow transitions~~ **Resolved:** `07_gpon_order_workflow.sql` seeds full GPON lifecycle including Blocker→Assigned; run-all-seeds.ps1.
- ~~Docket admin UI~~ **Resolved:** `/operations/dockets` page with checklist, verify, mark uploaded, file upload.
- ~~MyInvois runbook~~ **Resolved:** `docs/operations/myinvois_production_runbook.md`.
- Partner portal: manual; no automated docket/invoice submission (out of scope).

---

## Feature Completion Matrix

| Module | Backend | Frontend | DB | Evidence | Operational Readiness | Risk |
|--------|---------|----------|-----|----------|------------------------|------|
| **Orders** | Implemented | Implemented | Implemented | OrdersController, OrderService, Order entity; OrdersListPage, OrderDetailPage, CreateOrderPage; Orders, OrderTypes, OrderCategories, OrderStatusLogs, etc. | Ready | MINOR |
| **Scheduler** | Implemented | Implemented | Implemented | SchedulerController, SchedulerService; CalendarPage, InstallerSchedulerPage, SIAvailabilityPage; ScheduledSlots, SiAvailabilities, SiLeaveRequests | Ready | MINOR |
| **SI app** | Implemented | Implemented | Implemented | SiAppController, OrderStatusesController, workflow transitions; frontend-si: JobsListPage, JobDetailPage, materials/photos/checklist/reschedule; Orders API, scheduler API, workflow API | Ready | MINOR |
| **Dockets** | Implemented | Implemented | Implemented | OrderDocket entity, DocketUploadedValidator; DocketsPage at /operations/dockets (filter, checklist, verify, mark uploaded, file upload); OrderStatusesController; OrderDockets table | Ready | MINOR |
| **Billing/Invoices** | Implemented | Implemented | Implemented | BillingController, BillingService, InvoiceSubmissionsController; InvoicesListPage, InvoiceDetailPage, InvoiceEditPage; Invoices, InvoiceLineItems, InvoiceSubmissionHistory | Ready | MINOR |
| **MyInvois** | Implemented | Implemented | Implemented | InvoiceSubmissionService, MyInvoisApiProvider, IntegrationSettingsController; MyInvoisStatusPoll job; IntegrationsPage; myinvois_production_runbook.md | Ready | MINOR |
| **Inventory/Ledger** | Implemented | Implemented | Implemented | InventoryController, InventoryService, StockLedgerService; InventoryDashboardPage, LedgerPage, Receive/Transfer/Allocate/Issue/Return; StockLedgerEntries, StockAllocations, SerialisedItems, LedgerBalanceCache | Ready | MINOR |
| **Rates/Rate Engine** | Implemented | Implemented | Implemented | RatesController, RateEngineService, BillingRatecardController; SiRatePlansPage, PartnerRatesPage, RateEngineManagementPage; GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, RateCards | Ready | MINOR |
| **Payroll** | Implemented | Implemented | Implemented | PayrollController, PayrollService; PayrollPeriodsPage, PayrollRunsPage, PayrollEarningsPage; PayrollPeriods, PayrollRuns, PayrollLines, JobEarningRecords, SiRatePlans | Ready | MINOR |
| **P&L** | Implemented | Implemented | Implemented | PnlController, PnlService; PnlSummaryPage, PnlDrilldownPage, PnlOverheadsPage; PnlFacts, PnlDetailPerOrders, PnlPeriods, PnlTypes, OverheadEntries | Ready | MINOR |
| **Workflow Engine** | Implemented | Implemented | Implemented | WorkflowEngineService, WorkflowDefinitionsController; 07_gpon_order_workflow.sql (run-all-seeds.ps1); WorkflowDefinitions, WorkflowTransitions | Ready | MINOR |
| **Notifications** | Implemented | Implemented | Implemented | NotificationService, SmsMessagingService, WhatsAppMessagingService; NotificationsCenterPage; Notifications, NotificationSettings | Ready | MINOR |
| **Integrations** | Implemented | Partial | Implemented | Email (EmailIngestionSchedulerService), MyInvois, WhatsApp/SMS, OneDrive; IntegrationsPage; EmailAccounts, GlobalSettings | Ready | MINOR |
| **RBAC/Departments** | Implemented | Implemented | Implemented | DepartmentAccessService, DepartmentMembership; department-scoped controllers; SettingsProtectedRoute; Departments, DepartmentMemberships | Ready | MINOR |
| **Buildings** | Implemented | Implemented | Implemented | BuildingsController, BuildingTypesController, InstallationMethodsController; BuildingsListPage, BuildingDetailPage, BuildingsTreeGridPage; Buildings, BuildingTypes, InstallationMethods, Splitters, etc. | Ready | MINOR |
| **Partners** | Implemented | Implemented | Implemented | PartnersController, PartnerGroupsController; PartnersPage, PartnerGroupsPage; Partners, PartnerGroups | Ready | MINOR |
| **RMA** | Implemented | Implemented | Implemented | RMAController; RMAListPage; RmaRequests, RmaRequestItems | Partial (list only) | MINOR |
| **Files/Uploads** | Implemented | Implemented | Implemented | FilesController; FilesPage; Files entity | Ready | MINOR |
| **Background Jobs** | Implemented | Implemented | Implemented | BackgroundJobProcessorService, EmailIngestionSchedulerService, StockSnapshotSchedulerService; BackgroundJobsPage; BackgroundJobs, WorkflowJobs | Ready | MINOR |
| **Parser** | Implemented | Implemented | Implemented | ParserController, EmailIngestionService; ParserListingPage, ParserDashboardPage, ParseSessionDetailsPage, ParserSnapshotViewerPage; ParseSessions, ParsedOrderDrafts, EmailAccounts, etc. | Ready | MINOR |
| **Reports Hub** | Implemented | Implemented | Implemented | ReportsController, ReportRegistry; ReportsHubPage, ReportRunnerPage; ReportDefinitions (in-memory registry) | Ready | MINOR |
| **Tasks** | Implemented | Implemented | Implemented | TasksController; TasksListPage, MyTasksPage, DepartmentTasksPage, TasksKanbanPage; TaskItems | Ready | MINOR |
| **Assets** | Implemented | Implemented | Implemented | AssetsController; AssetsDashboardPage, AssetsListPage, AssetDetailPage; Assets, AssetDepreciationEntries, etc. | Ready | MINOR |
| **Accounting** | Implemented | Implemented | Implemented | SupplierInvoicesController, PaymentsController; SupplierInvoicesPage, PaymentsPage; SupplierInvoices, Payments | Ready | MINOR |
| **Procurement/Sales/Projects** | Stubs | Missing | Partial | purchase_orders, quotations, projects entities; no dedicated controllers/pages for full CRUD | Not Ready | MINOR |

---

## Completion % per Module (Evidence-Based)

| Module | % | Rationale |
|--------|---|-----------|
| Orders | 95 | Full CRUD, filters, status flow, workflow; create from parser |
| Scheduler | 90 | Calendar, timeline, unassigned panel, availability; slot CRUD |
| SI app | 90 | Jobs, transitions, materials, photos, checklist, reschedule, mark faulty; earnings (SubconRoute) |
| Dockets | 90 | Backend + DocketsPage (/operations/dockets); checklist, verify, upload; file attach |
| Billing/Invoices | 90 | Full invoice lifecycle, PDF, submission history; edit page |
| MyInvois | 85 | Provider, submission, status poll job; config UI; myinvois_production_runbook.md |
| Inventory/Ledger | 90 | Full lifecycle, ledger, allocations, serials, reports |
| Rates/Payroll/P&L | 85 | Rate engine, payroll, P&L; some edge cases (Kingsman/Menorah) unclear |
| Workflow Engine | 90 | Engine + guards + side effects; 07_gpon_order_workflow.sql seeded via run-all-seeds; Blocker→Assigned in DB |
| Notifications | 85 | SMS, WhatsApp, notification center; provider config |
| RBAC/Departments | 90 | Department scoping, access service, RBAC matrix |
| Parser | 90 | Email ingest, parse, drafts, sessions, snapshots |
| Reports Hub | 85 | Definitions, run, export; department scope |
| RMA | 60 | List page; RMA flow may be partial |
| Procurement/Sales/Projects | 15 | Entities only; no operational UI |

**Scoring rules:**
- **90–100%:** End-to-end implemented, operational.
- **70–89%:** Core path works; gaps in UI, config, or edge cases.
- **40–69%:** Backend or UI partial; missing key flows.
- **10–39%:** Stubs/entities only; not operational.

---

## Top 10 Completion Blockers (by Impact)

| # | Blocker | Impact | Module |
|---|---------|--------|--------|
| 1 | ~~Workflow transitions not auto-seeded~~ **Resolved** — 07_gpon_order_workflow.sql in run-all-seeds.ps1 | — | — |
| 2 | ~~Blocker → Assigned not in fallback~~ **Resolved** — Added to DB workflow + fallback | — | — |
| 3 | ~~MyInvois production config~~ **Resolved** — myinvois_production_runbook.md | — | — |
| 4 | ~~Docket admin UI~~ **Resolved** — /operations/dockets | — | — |
| 5 | ~~InvoiceRejected vs Rejected naming~~ **Resolved** — Display "Invoice Rejected"; code retains Rejected | — | — |
| 6 | ~~Assigned → Blocker~~ **Resolved** — In DB workflow + fallback | — | — |
| 7 | **Partner portal** — No API; manual docket/invoice submission. | MAJOR | Integrations |
| 8 | **EmailCleanupService double registration** — Scoped + HostedService; may cause confusion. | MINOR | Background Jobs |
| 9 | **Debug logging in Program.cs** — Hardcoded path; should be dev-only. | MINOR | Infrastructure |
| 10 | **Syncfusion fallback key** — Production should use env var only. | MINOR | Frontend |

---

## Known Discrepancies

See **[docs/_discrepancies.md](./_discrepancies.md)** for the audit register:
- **Closed:** Workflow alignment (A–J), multi-company clarification, Kingsman/Menorah, Quotation
- **Open – Must Fix:** Root README broken links, debug logging to hardcoded path
- **Accepted Gaps:** EmailCleanupService dual registration, Syncfusion fallback, DocketsPage "SI will be notified", data_model overview, CurrencyExchangeService
- **Deferred:** CurrencyExchange external API, MaterialsDisplay faulty status, profile nav, Storybook, 06_ai notes, doc inventory

---

## Reality vs Docs

| Area | Docs claim | Reality |
|------|------------|---------|
| Workflow | All transitions via workflow | DB workflow seeded (07_gpon_order_workflow.sql); fallback when no match |
| Docket | Receive → verify → upload | DocketsPage at /operations/dockets; checklist, verify, upload, file attach |
| MyInvois | E-invoice submission | Backend implemented; myinvois_production_runbook.md; credentials via config |
| SI app | Full journey | Implemented; earnings behind SubconRoute |
| Inventory ledger | Source of truth | Implemented; LedgerBalanceCache, StockByLocationSnapshots exist |
| RBAC | Department-scoped | Implemented; DepartmentAccessService used across controllers |

---

**End of report.**
