# Implementation Truth Inventory

**Last run:** 2026-02-09  
**Source:** Backend code, frontend App and pages, EF Core schema (ApplicationDbContextModelSnapshot).  
**Live PostgreSQL:** Not connected for this run; schema and reference data inferred from code and migrations.

**Purpose:** Single snapshot of what is actually implemented (tables, entities, API, UI). Docs must align to this; no invented behavior.

---

## 1. Live PostgreSQL Schema and Reference Data

### 1.1 Connection and scope

- **Live DB connected this run:** NO (psql not available / connection not attempted from this environment).
- **Schema source:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` and `ApplicationDbContext.cs` DbSets.
- **Reference data source:** `DatabaseSeeder.cs`, `AddInstallationMethodsTable.sql`, `20241127_AddDepartmentIdToInstallationMethods.sql`, `20250106_SeedAllReferenceData.sql` (if present).

### 1.2 Tables (from EF snapshot)

CephasOps-relevant tables in `public` (convention). Table names from snapshot:

| Module | Tables |
|--------|--------|
| **Orders** | Orders, OrderTypes, OrderCategories, OrderStatusLogs, OrderReschedules, OrderBlockers, OrderDockets, OrderMaterialReplacements, OrderMaterialUsage, OrderNonSerialisedReplacements, OrderStatusChecklistItems, OrderStatusChecklistAnswers |
| **Buildings** | Buildings, BuildingTypes, InstallationMethods, BuildingBlocks, BuildingContacts, BuildingRules, BuildingSplitters, BuildingDefaultMaterials, HubBoxes, Poles, Splitters, SplitterPorts, SplitterTypes, Streets |
| **Companies** | Companies, CompanyDocuments, CostCentres, Partners, PartnerGroups, Verticals |
| **Departments** | Departments, DepartmentMemberships, MaterialAllocations |
| **Inventory** | Materials, MaterialAttributes, MaterialCategories, MaterialPartners, MaterialTags, MaterialVerticals, MovementTypes, SerialisedItems, StockAllocations, StockBalances, StockByLocationSnapshots, StockLedgerEntries, StockLocations, StockMovements, LedgerBalanceCaches, LocationTypes, delivery_orders, delivery_order_items |
| **Billing** | Invoices, InvoiceLineItems, InvoiceSubmissionHistory, BillingRatecards, Payments, SupplierInvoices, SupplierInvoiceLineItems |
| **Payroll** | JobEarningRecords, PayrollLines, PayrollPeriods, PayrollRuns, SiRatePlans |
| **Rates** | RateCards, RateCardLines, CustomRates, GponPartnerJobRates, GponSiJobRates, GponSiCustomRates |
| **P&L** | PnlDetailPerOrders, PnlFacts, PnlPeriods, PnlTypes, OverheadEntries |
| **Parser** | ParseSessions, ParsedOrderDrafts, EmailAccounts, EmailAttachments, EmailMessages, EmailTemplates, ParserRules, ParserTemplates, VipEmails, VipGroups |
| **Scheduler** | ScheduledSlots, SiAvailabilities, SiLeaveRequests |
| **Service installers** | ServiceInstallers, ServiceInstallerContacts, ServiceInstallerSkills, Skills |
| **Workflow** | WorkflowDefinitions, WorkflowTransitions, WorkflowJobs, BackgroundJobs, SystemLogs |
| **Settings** | GlobalSettings, MaterialTemplates, MaterialTemplateItems, DocumentTemplates, DocumentPlaceholderDefinitions, GeneratedDocuments, KpiProfiles, TimeSlots, approval_steps, approval_workflows, automation_rules, Bins, Brands, business_hours, customer_preferences, escalation_rules, guard_condition_definitions, payment_terms, ProductTypes, public_holidays, ServicePlans, side_effect_definitions, sla_profiles, sms_gateways, sms_templates, tax_codes, Teams, vendors, whatsapp_templates |
| **Users/RBAC** | Users, UserCompanies, Roles, UserRoles, Permissions, RolePermissions, RefreshTokens |
| **RMA** | RmaRequests, RmaRequestItems |
| **Procurement** | purchase_orders, purchase_order_items, suppliers |
| **Sales** | quotations, quotation_items |
| **Projects** | projects, boq_items |
| **Assets** | Assets, AssetDepreciationEntries, AssetDisposals, AssetMaintenanceRecords, AssetTypes |
| **Files** | Files |
| **Audit** | AuditLogs, AuditOverrides |
| **Tasks** | TaskItems |
| **Notifications** | Notifications, NotificationSettings |
| **Other** | MaterialMaterialTags, MaterialMaterialVerticals |

### 1.3 Reference tables: row presence and example codes (from code/seeds)

| Table | Source | Row presence | Example codes (code-only) |
|-------|--------|---------------|---------------------------|
| **OrderTypes** | DatabaseSeeder.SeedDefaultOrderTypesAsync | Seeded (when seed runs) | ACTIVATION, MODIFICATION_INDOOR, MODIFICATION_OUTDOOR, ASSURANCE, VALUE_ADDED_SERVICE |
| **OrderCategories** | DatabaseSeeder.SeedDefaultOrderCategoriesAsync | Seeded | FTTH, FTTO, FTTR, FTTC |
| **InstallationMethods** | AddInstallationMethodsTable.sql; 20241127_AddDepartmentIdToInstallationMethods.sql | Seeded by SQL | PRELAID, NON_PRELAID, SDU_RDF |
| **BuildingTypes** | DatabaseSeeder.SeedDefaultBuildingTypesAsync | Seeded | CONDO, APARTMENT, SERVICE_APT, FLAT, TERRACE, SEMI_DETACHED, BUNGALOW, TOWNHOUSE, OFFICE_TOWER, OFFICE, SHOP_OFFICE, MALL, HOTEL, MIXED, INDUSTRIAL, WAREHOUSE, EDUCATIONAL, GOVERNMENT, OTHER |
| **Departments** | DatabaseSeeder.SeedGponDepartmentAsync | Seeded (GPON) | GPON |
| **SplitterTypes** | DatabaseSeeder.SeedDefaultSplitterTypesAsync | Seeded | (from seeder) |
| **Partners** | No seed in DatabaseSeeder / 20250106 | Configurable; may be empty | — |
| **PartnerGroups** | Configurable | Configurable; may be empty | — |

---

## 2. Backend Implementation

### 2.1 Entities and enums

- **Entities:** `backend/src/CephasOps.Domain/` — by aggregate: Orders (Order, OrderType, OrderCategory, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialReplacement, OrderMaterialUsage, OrderNonSerialisedReplacement, OrderStatusChecklistItem, OrderStatusChecklistAnswer), Buildings (Building, BuildingType, InstallationMethod, BuildingContact, BuildingRules, BuildingBlock, BuildingSplitter, BuildingDefaultMaterial, HubBox, Pole, Splitter, SplitterPort, SplitterType, Street), Companies (Company, Partner, PartnerGroup, Vertical, CostCentre, CompanyDocument), Departments (Department, DepartmentMembership, MaterialAllocation), Billing (Invoice, InvoiceLineItem, InvoiceSubmissionHistory, BillingRatecard, Payment, SupplierInvoice, SupplierInvoiceLineItem), Inventory (Material, MaterialCategory, MaterialPartner, MaterialTag, MaterialVertical, MaterialAttribute, StockLocation, StockBalance, StockMovement, StockLedgerEntry, StockAllocation, SerialisedItem, MovementType, LocationType, LedgerBalanceCache, StockByLocationSnapshot, DeliveryOrder), Parser (ParseSession, ParsedOrderDraft, EmailMessage, EmailAttachment, EmailAccount, ParserTemplate, EmailTemplate, ParserRule, VipEmail, VipGroup), Scheduler (ScheduledSlot, SiAvailability, SiLeaveRequest), ServiceInstallers (ServiceInstaller, ServiceInstallerContact, Skill, ServiceInstallerSkill), Payroll (PayrollPeriod, PayrollRun, PayrollLine, JobEarningRecord, SiRatePlan), Pnl (PnlPeriod, PnlFact, PnlDetailPerOrder, PnlType, OverheadEntry), Workflow (WorkflowDefinition, WorkflowTransition, WorkflowJob, BackgroundJob, SystemLog), Users (User, Role, UserRole, Permission, RolePermission, RefreshToken), Settings (see Domain/Settings/Entities), RMA (RmaRequest, RmaRequestItem), Rates (GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, RateCard, RateCardLine, CustomRate), Assets, Audit, Files, Notifications, Tasks, Procurement, Sales, Projects.
- **Order status enum (source of truth):** `backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs` — values: Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, DocketsReceived, DocketsVerified, DocketsUploaded, ReadyForInvoice, Invoiced, SubmittedToPortal, Completed, Blocker, ReschedulePendingApproval, Rejected, Cancelled, Reinvoice (17 total).
- **Other enums:** ServiceIdType (Orders), BlockerCategory, BlockerReason, StockLedgerEntryType, StockAllocationStatus, PaymentMethod, PaymentType, etc. (see Domain/*/Enums).

### 2.2 Services and workflow behavior

- **Key services:** OrderService, BillingService, WorkflowEngineService, SchedulerService, PnlService, InventoryService, RateEngineService, InvoiceSubmissionService, EmailIngestionService, ParserService (paths under `backend/src/CephasOps.Application/`).
- **Workflow:** DB-driven (WorkflowDefinitions, WorkflowTransitions); fallback in OrderStatusesController for status changes when no workflow transition matches. WorkflowEngineService applies guards and side effects. See `docs/operations/workflow_engine_validation_gpon.md`.

### 2.3 API surface (controllers)

Controllers under `backend/src/CephasOps.Api/Controllers/` (grouped by module):

- **Orders:** OrdersController, OrderTypesController, OrderCategoriesController, InstallationTypesController (uses OrderCategoryService), OrderStatusesController, OrderStatusChecklistController.
- **Scheduler:** SchedulerController.
- **Parser:** ParserController.
- **Inventory:** InventoryController.
- **Billing:** BillingController, BillingRatecardController, InvoiceSubmissionsController, PaymentsController, SupplierInvoices (SupplierInvoicesController).
- **Buildings:** BuildingsController, BuildingTypesController, InstallationMethodsController, BuildingDefaultMaterialsController, SplittersController, SplitterTypesController.
- **Departments:** DepartmentsController.
- **Service installers:** ServiceInstallersController, SiAppController.
- **Partners:** PartnersController, PartnerGroupsController.
- **Users/Auth:** UsersController, AuthController.
- **Settings:** OrderTypesController, OrderCategoriesController, InstallationTypesController, OrderStatusesController, KpiProfilesController, MaterialTemplatesController, DocumentTemplatesController, EmailTemplatesController, ParserTemplatesController, WorkflowDefinitionsController, GuardConditionDefinitionsController, SideEffectDefinitionsController, BusinessHoursController, EscalationRulesController, ApprovalWorkflowsController, AutomationRulesController, SlaProfilesController, TimeSlotsController, GlobalSettingsController, IntegrationSettingsController, MaterialCategoriesController, BinsController, BrandsController, ProductTypesController, ServicePlansController, TeamsController, TaxCodesController, PaymentTermsController, VendorsController, SmsGatewayController, SmsTemplatesController, WhatsAppTemplatesController, ReportDefinitionsController, etc.
- **Reports:** ReportsController.
- **P&L:** PnlController, PnlTypesController.
- **Payroll:** PayrollController.
- **Rates:** BillingRatecardController, RatesController.
- **RMA:** RMAController.
- **Email:** EmailAccountsController, EmailsController, EmailSendingController, EmailRulesController.
- **Messaging:** MessagingController, SmsController, WhatsAppController.
- **Background jobs:** BackgroundJobsController.
- **Workflow:** WorkflowController, WorkflowDefinitionsController, GuardConditionDefinitionsController, SideEffectDefinitionsController.
- **Admin/health:** AdminController, DiagnosticsController, InfrastructureController, LogsController.
- **Assets:** AssetsController, AssetTypesController.
- **Tasks:** TasksController.
- **Files/Docs:** FilesController, DocumentsController, BinsController.
- **Companies:** CompaniesController.
- **Verticals:** VerticalsController.
- **Notifications:** NotificationsController, NotificationTemplatesController.
- **Vip:** VipEmailsController, VipGroupsController.

### 2.4 Background jobs and integrations

- **Hosted services (Program.cs):** BackgroundJobProcessorService, EmailIngestionSchedulerService, StockSnapshotSchedulerService, EmailCleanupService.
- **Job types (from background_jobs.md + code):** EmailIngest, PnlRebuild, NotificationSend, NotificationRetention, DocumentGeneration, MyInvoisStatusPoll, InventoryReportExport, ReconcileLedgerBalanceCache, PopulateStockByLocationSnapshots.
- **Integrations implemented:** Email (POP3/IMAP) via EmailIngestionSchedulerService; MyInvois (invoice submission + status polling); WhatsApp/SMS (Twilio, WhatsApp Cloud API, null provider); OneDrive (IOneDriveSyncService, File entity fields). Partner portals: no API; manual status updates.

---

## 3. Frontend UI Reality

### 3.1 Routes and pages

From `frontend/src/App.tsx` (protected routes under MainLayout):

| Path | Component (page) |
|------|------------------|
| /dashboard | DashboardPage |
| /orders | OrdersListPage |
| /orders/create | CreateOrderPage |
| /orders/:orderId | OrderDetailPage |
| /scheduler | CalendarPage |
| /scheduler/timeline | InstallerSchedulerPage |
| /scheduler/availability | SIAvailabilityPage |
| /orders/parser, /orders/parser/dashboard, /orders/parser/sessions/:id, /orders/parser/list, /orders/parser/snapshots | ParserListingPage, ParserDashboardPage, ParseSessionDetailsPage, ParserSnapshotViewerPage |
| /email | EmailManagementPage |
| /inventory, /inventory/list, /inventory/stock-summary, /inventory/ledger, /inventory/receive, /inventory/transfer, /inventory/allocate, /inventory/issue, /inventory/return | InventoryDashboardPage, InventoryListPage, etc. |
| /reports, /reports/:reportKey | ReportsHubPage, ReportRunnerPage |
| /inventory/reports, usage, serial-lifecycle, stock-trend | InventoryReportsIndexPage, InventoryUsageByPeriodPage, etc. |
| /rma | RMAListPage |
| /billing, /billing/invoices, /billing/invoices/:id, /billing/invoices/:id/edit | InvoicesListPage, InvoiceDetailPage, InvoiceEditPage |
| /payroll/periods, /payroll/runs, /payroll/earnings | PayrollPeriodsPage, PayrollRunsPage, PayrollEarningsPage |
| /pnl/summary, /pnl/drilldown, /pnl/overheads | PnlSummaryPage, PnlDrilldownPage, PnlOverheadsPage |
| /kpi/dashboard, /kpi/profiles | KpiDashboardPage, KpiProfilesPageDedicated |
| /notifications | NotificationsCenterPage |
| /accounting, /accounting/supplier-invoices, /accounting/payments | AccountingDashboardPage, SupplierInvoicesPage, PaymentsPage |
| /assets, /assets/list, /assets/:id, /assets/maintenance, /assets/depreciation | AssetsDashboardPage, AssetsListPage, AssetDetailPage, etc. |
| /tasks/kanban | TasksKanbanPage |
| /inventory/warehouse-layout | WarehouseLayoutPage |
| /buildings/treegrid | BuildingsTreeGridPage |
| /admin/background-jobs | BackgroundJobsPage |
| /settings/* | Many settings routes (company, departments, partners, order-types, order-categories, installation-types, installation-methods, building-types, splitter-types, materials, material-templates, document-templates, kpi-profiles, email, pnl-types, asset-types, order-statuses, time-slots, buildings, workflow/definitions, guard-conditions, side-effects, etc.) |
| /workflow/definitions, /workflow/guard-conditions, /workflow/side-effects | WorkflowDefinitionsPage, GuardConditionsPage, SideEffectsPage |
| /tasks, /tasks/my, /tasks/department/:departmentId | TasksListPage, MyTasksPage, DepartmentTasksPage |
| /documents, /doc-templates/new, /doc-templates/:id | DocumentsPage, DocumentTemplateEditorPage |
| /files | FilesPage |
| /buildings, /buildings/list, /buildings/new, /buildings/:id | BuildingsDashboardPage, BuildingsListPage, BuildingDetailPage |
| /parser (under settings/email or features) | Parser-related (e.g. ParserTemplatesPage) |

### 3.2 Fields and filters (major pages)

- **Orders list:** Filters: Search (keyword), Status (ORDER_STATUSES – API-aligned), Partner (getPartners() – API-driven, partnerId GUID), Date (single/range). Grid: ServiceId, TicketId, Customer, Building, Order Type, Partner–Category, Inst. Type, Inst. Method, Priority, Status, Installer, Appointment, Actions.
- **Order detail:** Summary: Type, Partner (derivedPartnerCategoryLabel or partnerName), Installation Type (orderCategoryCode), Installation Method (installationMethodName), Status, Created. Full order fields from OrderDto.
- **Invoices list:** Partner filter and column (getPartners()); status; date range; grid columns include partnerName.
- **P&L drilldown:** Filters: period, partner, department, etc. Table: Order–Category (orderCategoryCode), Partner–Category (derivedPartnerCategoryLabel ?? partnerName), Period, Revenue, etc.
- **Scheduler:** Calendar and timeline; slot cards show derivedPartnerCategoryLabel or partnerName; unassigned orders panel shows same.

### 3.3 Constants vs API-driven data

- **Order status:** Frontend uses ORDER_STATUSES (from types/scheduler or constants) aligned to backend OrderStatus; status filter and dropdowns are effectively API-aligned (status strings sent to API).
- **Partner filter (orders):** API-driven via getPartners(); value sent is partnerId (GUID). No hardcoded PARTNER list (removed from constants/orders.ts).
- **Order type filter:** If present, may use ORDER_TYPE constants (Activation, ModificationIndoor, etc.) or API; order type on order is from backend (OrderTypeId resolved to name).
- **Reference data (settings):** Order types, Order categories, Installation types (same as Order categories), Building types, Installation methods, Splitter types, Partners, Departments — all CRUD via API (settings pages).
- **Installation Types UI:** Served by api/installation-types (InstallationTypesController → OrderCategoryService); same data as Order Categories.

---

*End of inventory. Use this document to diff against docs and to update DOCS_INVENTORY, DOCS_MAP, and core docs.*
