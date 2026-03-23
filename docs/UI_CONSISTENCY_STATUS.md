# UI Consistency Status

**Last updated:** 3 February 2026  
**Purpose:** Record what is done vs backlog for UI/UX alignment (Admin Portal + SI App).  
**Source:** [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) audit and subsequent patches.

**Initiative status:** P0, P1 (PageShell), P2 (SI primitives, Toast, DataTable, SI Card), P3-6 (Syncfusion vs DataTable rule), and audit items #4, #6, #16, #20 are complete. Remaining work is **apply-when-touching** (P3 standards, status badges, etc.); no further batch work planned unless prioritised.

---

## Completed

| Workstream | Scope | Evidence |
|------------|--------|----------|
| **P0 Quick Wins** | Admin + SI | [P0_UI_CONSISTENCY_PATCH_SUMMARY.md](P0_UI_CONSISTENCY_PATCH_SUMMARY.md): PageShell on Orders list/detail, Stock Summary, Ledger, Scheduler, Buildings; StatusBadge + helpers; EmptyState/LoadingSpinner on those pages; SI PageHeader, StatusBadge, Button/EmptyState/LoadingSpinner aligned; Jobs list and Orders tracking use PageHeader + StatusBadge. |
| **Theme alignment** | SI only | [THEME_ALIGNMENT_SUMMARY.md](THEME_ALIGNMENT_SUMMARY.md): SI CSS variables aligned to Admin purple (263) palette. |
| **Global styles** | Admin + SI | [GLOBAL_STYLES_ALIGNMENT_SUMMARY.md](GLOBAL_STYLES_ALIGNMENT_SUMMARY.md): Typography tokens, body font/line-height, h1–h3 base styles, `.page-pad`; SI font set to Public Sans. |
| **Syncfusion styling** | Admin only | [SYNCFUSION_STYLING_FIX_SUMMARY.md](SYNCFUSION_STYLING_FIX_SUMMARY.md): Grid/TreeGrid/Kanban/Scheduler overrides in `frontend/src/index.css`; removed inline styles from SyncfusionGrid, BuildingsTreeGridPage, TasksKanbanPage. |
| **PR gate** | Reference | `frontend/src/dev/uiConsistencyGate.ts`: Checklist for PageShell/PageHeader, status badges, empty/loading, libraries, Syncfusion usage. |
| **Toast unification (P2-2)** | SI | SI now has ToastProvider + useToast (context-based) with showToast, showSuccess, showError, showWarning, showInfo, dismissToast; optional duration (default 5s, aligned with Admin). Toasts render; API aligned with Admin. |
| **SI shared Modal (P2-1)** | SI | SI now has shared `Modal` component (isOpen, onClose, title, size, closeOnOverlayClick, closeOnEscape). MarkFaultyModal and RescheduleRequestModal refactored to use it. |
| **P3 standards documented (P3-1–P3-5)** | Doc | Feb 2026: Forms validation, Inputs, Design tokens, Breadcrumbs, Icons standards added to [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § Recommended UI Standard. See [UI_CONSISTENCY_BACKLOG.md](UI_CONSISTENCY_BACKLOG.md) for apply-when-touching notes. |
| **SI Skeleton (P2-1 / audit #6)** | SI | Feb 2026: SI has shared `Skeleton`; Jobs list, Job detail, Orders tracking, Dashboard, Service Installers, Materials Tracking, Material Returns, Materials Scan (Recent Scans) use skeleton loading. Apply to remaining SI pages when touching. |
| **SI Breadcrumbs (P2-1)** | SI | Feb 2026: SI has shared `Breadcrumbs` component (items with label/path/active). Use for nested views outside PageHeader. |
| **SI Tabs (P2-1)** | SI | Feb 2026: SI has shared `Tabs` and `TabPanel` (defaultActiveTab, onTabChange; TabPanel: label, icon?, disabled?). Use for tabbed content. |
| **SI DataTable (P2-1 / P2-3)** | SI | Feb 2026: SI has shared `DataTable` (columns, data, loading, pagination, sortable, onRowClick; mobile cards + desktop table). Orders tracking uses it. Use for tabular list views when adding or refactoring. |
| **SI Card (audit #4)** | SI | Feb 2026: SI `Card` supports optional `title`, `subtitle`, and `footer` (ReactNode) for alignment with Admin when needed. Existing usages unchanged (all optional). |
| **SI Job detail – Tabs + Breadcrumbs** | SI | Feb 2026: Job detail uses PageHeader (dynamic title), Breadcrumbs (Jobs → customer name), and Tabs (Details, Materials, Photos). First SI page using Tabs and Breadcrumbs. |
| **Admin InstallerSchedulerPage – Skeleton** | Admin | Feb 2026: Scheduler loading/init use layout-matched Skeleton (filter bar, 4 stat cards, sidebar + calendar area) instead of fullPage LoadingSpinner. Linter fixes: unassignedOrders→allUnassignedOrders, si.code→employeeId/siLevel, Badge destructive→warning, TreeView/ScheduleComponent types. |
| **Admin CalendarPage – Skeleton** | Admin | Feb 2026: Calendar loading uses PageShell + layout-matched Skeleton (date nav, filter bar, installer panel + time slots grid) instead of fullPage LoadingSpinner. |
| **Scheduler module complete (InstallerSchedulerPage + CalendarPage)** | Admin | Feb 2026: Both use PageShell; breadcrumbs Scheduler (path `/scheduler`) → Timeline or Calendar; layout-matched Skeleton loading; InstallerSchedulerPage: EmptyState for no unassigned jobs, conflict warnings, Syncfusion Schedule + TreeView; CalendarPage: drag-and-drop calendar, StatusFilterBar, InstallerPanel. |
| **Skeleton vs LoadingSpinner rule (Phase-2)** | Admin + SI | Feb 2026: Rule documented in JSDoc in both `skeleton.tsx` and `LoadingSpinner.tsx`. Use Skeleton when layout is known (list, table, cards, dashboard); use LoadingSpinner when layout is unknown or a simple centered spinner is preferred. See [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) § States. |
| **SI EarningsPage** | SI | Feb 2026: Earnings stub replaced with full page using getMyEarnings API, period filter, DataTable, Skeleton loading, EmptyState, Breadcrumbs, PageHeader. |

**Summary:** P0 is done; theme and global styles are aligned; Syncfusion (Admin) looks consistent; toast API is unified; SI has shared Modal, Skeleton, Breadcrumbs, Tabs, and DataTable; Job detail uses Tabs and Breadcrumbs; P3 standards (forms, inputs, tokens, breadcrumbs, icons) are documented in the playbook; there is a defined standard and gate.

---

## Backlog (P2 / P3 and open audit findings)

### P2 – Component consolidation

- **SI missing primitives:** Modal, Skeleton, Breadcrumbs, Tabs, DataTable done Feb 2026. SI Card has optional title, subtitle, footer (audit #4 done).
- **Toast system:** Unified Feb 2026. SI now has ToastProvider + same API (showSuccess/Error/Warning/Info + duration).
- **DataTable in SI:** P2-3 done Feb 2026; SI has shared DataTable. Use for tabular list views when adding or refactoring SI pages.

### P3 – Legacy / consistency

- **Forms (P3-1):** Standard documented Feb 2026 (react-hook-form + zod for non-trivial forms; shared error display). Apply when adding/refactoring forms.
- **Inputs (P3-2):** Standard documented Feb 2026 (TextInput for form fields; Input headless for custom wrappers). Apply when adding/refactoring forms.
- **Design tokens (P3-3):** Standard documented Feb 2026 (designTokens for typography, spacing, input height; SectionCard for grouped sections). Apply when adding/refactoring pages.
- **Breadcrumbs (P3-4):** Standard documented Feb 2026 (prefer PageShell breadcrumbs; standalone Breadcrumbs only outside PageShell). Apply when adding/refactoring pages.
- **Icons (P3-5):** Standard documented Feb 2026 (sm h-4 w-4, md h-5 w-5, lg h-6 w-6). Apply when touching buttons/nav.
- **Syncfusion vs DataTable (P3-6):** Done Feb 2026. See [SYNCFUSION_VS_DATATABLE_RULE.md](SYNCFUSION_VS_DATATABLE_RULE.md) for when to use Grid vs DataTable.
### Audit findings still open (summary)

| # | Category | Status |
|---|----------|--------|
| 4 | Cards | **Done.** SI Card has optional title, subtitle, footer (Feb 2026). |
| 6 | Loading | SI has Skeleton; Jobs list, Job detail, Orders tracking, Dashboard, Service Installers, Materials Tracking, Material Returns, Materials Scan (Recent Scans) use it. Apply to remaining SI pages when touching. |
| 7 | Toasts | P2-2 done; SI has same API. |
| 8 | Inputs | Standard documented Feb 2026 (P3-2); apply when touching forms. |
| 9 | Status badges | P0 replaced inline on key pages; other pages may still use inline pills. |
| 10 | Tables | Standard documented Feb 2026 (P2-3); DataTable in SI when tabular. |
| 11 | Spacing | Improved via PageShell/PageHeader and .page-pad; some pages may still vary. |
| 12 | Breadcrumbs | Standard documented Feb 2026 (P3-4); apply when touching pages. |
| 13 | Section headings | Inconsistent; SectionCard unused. |
| 14 | Forms validation | Standard documented Feb 2026 (P3-1); apply when touching forms. |
| 16 | Modal | **Done.** P2-1; SI has shared Modal. |
| 17 | Icons | Standard documented Feb 2026 (P3-5); apply when touching buttons/nav. |
| 18 | Design tokens | Standard documented Feb 2026 (P3-3); apply when touching pages. |
| 19 | Syncfusion vs custom | Gate exists; no “refactor existing” rule. |
| 20 | SI missing primitives | Modal, Skeleton, Breadcrumbs, Tabs, DataTable done (P2-1/P2-3). SI Card has title/subtitle/footer (Feb 2026). |

---

## P1 – PageShell and worst offenders

**P1 PageShell audit: complete (Feb 2026).** All Admin Portal content pages (inventory, orders, buildings, parser, accounting, documents, assets, billing, RMA, workflow, P&L, payroll, settings, email, files, scheduler, reports, admin, KPI, notifications, dashboard) use PageShell with consistent loading and empty states. Auth (Login) and test pages are out of scope.

P0 fixed **Orders list, Order detail, Stock Summary, Ledger, Scheduler, Buildings**. P1 cleanup (Feb 2026) added PageShell + consistent empty/loading to:

- **Inventory:** Receive, Allocate, Issue, Return, Transfer, Dashboard, List, Reports index, Serial lifecycle, Stock trend, Usage by period (some already use PageShell where listed in “Files Changed” in P0 summary).
- **InventoryReceivePage:** PageShell "Receive Stock", breadcrumbs Inventory / Receive; EmptyState for no-department and access-denied.
- **InventoryIssuePage:** PageShell "Issue Stock", breadcrumbs Inventory / Issue; EmptyState for no-department and access-denied.
- **InventoryDashboardPage:** PageShell "Inventory" with actions (Refresh, Stock In, Stock Out); loading uses PageShell + LoadingSpinner.
- **CreateOrderPage:** PageShell "Create Order" with dynamic breadcrumbs (Parser/Review/Create or Orders/Create) and actions (Snapshot, Approve, Reject, Cancel, Save); loading uses PageShell + LoadingSpinner.
- **InventoryAllocatePage:** PageShell "Allocate Stock to Order", breadcrumbs Inventory / Allocate; EmptyState for no-department and access-denied.
- **InventoryReturnPage:** PageShell "Return Stock", breadcrumbs Inventory / Return; EmptyState for no-department and access-denied.
- **InventoryTransferPage:** PageShell "Transfer Stock", breadcrumbs Inventory / Transfer; EmptyState for no-department and access-denied.
- **InventoryListPage:** PageShell "Inventory Management" with actions (Add Material); loading uses PageShell + LoadingSpinner.
- **InventoryReportsIndexPage:** PageShell "Inventory Reports", breadcrumbs Inventory / Reports; EmptyState for no-department.
- **InventorySerialLifecyclePage:** PageShell "Serial Lifecycle" with actions (Export CSV), breadcrumbs Inventory / Reports / Serial lifecycle; EmptyState for no-department and access-denied.
- **InventoryStockTrendPage:** PageShell "Stock Trend by Location", breadcrumbs Inventory / Reports / Stock trend; EmptyState for no-department and access-denied.
- **InventoryUsageByPeriodPage:** PageShell "Usage by Period" with actions (Export CSV), breadcrumbs Inventory / Reports / Usage; EmptyState for no-department and access-denied.

**P1 audit (Feb 2026, second batch):** PageShell added to Buildings list/dashboard/detail, Parser Snapshot Viewer, Accounting Dashboard, and Documents:
- **BuildingsListPage:** PageShell "Buildings", breadcrumbs Buildings; actions (Import/Export, Refresh, Add Building); loading uses PageShell + LoadingSpinner.
- **BuildingsDashboardPage:** PageShell "Buildings", breadcrumbs Buildings; actions (Add Building); loading/empty use PageShell + LoadingSpinner/EmptyState.
- **BuildingDetailPage:** PageShell with dynamic title (New Building / building name), breadcrumbs Buildings / (name or New); actions (Back, StatusBadge when not new); loading uses PageShell + LoadingSpinner.
- **ParserSnapshotViewerPage:** PageShell "Parser Snapshot Viewer", breadcrumbs Parser / Snapshot Viewer; actions (Refresh); standalone Breadcrumbs and custom header removed; loading uses PageShell + LoadingSpinner.
- **AccountingDashboardPage:** PageShell "Accounting Dashboard", breadcrumbs Accounting; loading/empty use PageShell + LoadingSpinner/EmptyState.
- **DocumentsPage:** PageShell "Generated Documents", breadcrumbs Documents; loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, third batch):** PageShell added to Assets, Billing (Invoices list), and RMA:
- **AssetsDashboardPage:** PageShell "Asset Management", breadcrumbs Assets; loading/empty use PageShell + LoadingSpinner/EmptyState.
- **AssetsListPage:** PageShell "Assets", breadcrumbs Assets; actions (Add Asset); loading uses PageShell + LoadingSpinner.
- **InvoicesListPage:** PageShell "Billing & Invoices", breadcrumbs Billing / Invoices; actions (Filters, Create Invoice); standalone Breadcrumbs and custom header removed; loading uses PageShell + LoadingSpinner.
- **RMAListPage:** PageShell "RMA Management", breadcrumbs RMA; actions (Create RMA Request); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, fourth batch):** PageShell added to Asset detail, Depreciation Report, Maintenance Schedule, and Invoice detail:
- **AssetDetailPage:** PageShell with dynamic title (asset name), breadcrumbs Assets / (asset tag or name); actions (Back, Add Maintenance, Dispose, Delete); loading/not-found use PageShell + LoadingSpinner/EmptyState.
- **DepreciationReportPage:** PageShell "Depreciation Report", breadcrumbs Assets / Depreciation Report; actions (Run Depreciation, Post to GL); loading uses PageShell + LoadingSpinner.
- **MaintenanceSchedulePage:** PageShell "Maintenance Schedule", breadcrumbs Assets / Maintenance; actions (Schedule Maintenance); loading uses PageShell + LoadingSpinner.
- **InvoiceDetailPage:** PageShell with title "Invoice {number}", breadcrumbs Billing / Invoices / (number); actions (Back, StatusBadge, OVERDUE); standalone Breadcrumbs and custom header removed; loading/not-found use PageShell + LoadingSpinner/EmptyState.

**P1 audit (Feb 2026, fifth batch):** PageShell added to Workflow (3 pages) and P&L (4 pages):
- **WorkflowDefinitionsPage:** PageShell "Workflow Definitions", breadcrumbs Workflow; actions (Create Workflow); standalone Breadcrumbs and custom header removed; loading uses PageShell + LoadingSpinner.
- **SideEffectsPage:** PageShell "Side Effect Definitions", breadcrumbs Workflow / Side Effects; actions (Create Side Effect when canManage); loading uses PageShell + LoadingSpinner.
- **GuardConditionsPage:** PageShell "Guard Condition Definitions", breadcrumbs Workflow / Guard Conditions; actions (Create Guard Condition when canManage); loading uses PageShell + LoadingSpinner.
- **PnlSummaryPage:** PageShell "P&L Summary", breadcrumbs P&L; actions (period select); loading uses PageShell + LoadingSpinner.
- **PnlOrdersPage:** PageShell "P&L by Order", breadcrumbs P&L / Orders; actions (Filter); loading uses PageShell + LoadingSpinner.
- **PnlOverheadsPage:** PageShell "Overheads", breadcrumbs P&L / Overheads; actions (Add Overhead); loading uses PageShell + LoadingSpinner.
- **PnlDrilldownPage:** PageShell "P&L Drill-Down by Order", breadcrumbs P&L / Drill-Down; actions (Export, Refresh); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, sixth batch):** PageShell added to Payroll (3 pages):
- **PayrollEarningsPage:** PageShell "Job Earnings", breadcrumbs Payroll / Earnings; actions (Filter); loading uses PageShell + LoadingSpinner.
- **PayrollPeriodsPage:** PageShell "Payroll Periods", breadcrumbs Payroll / Periods; actions (Year input, Create Period); loading uses PageShell + LoadingSpinner.
- **PayrollRunsPage:** PageShell "Payroll Runs", breadcrumbs Payroll / Runs; actions (count badge, Refresh, Export, Create Run); loading uses PageShell + LoadingSpinner.

**P1 PageShell audit complete.** All high-traffic Admin modules (inventory, orders, buildings, parser, accounting, documents, assets, billing, RMA, workflow, pnl, payroll) now use PageShell.

**P1 audit (Feb 2026, seventh batch – settings):** PageShell added to 5 settings sub-pages:
- **DepartmentsPage:** PageShell "Departments", breadcrumbs Settings / Departments; actions (Add Department); loading uses PageShell + LoadingSpinner.
- **PartnersPage:** PageShell "Partners", breadcrumbs Settings / Partners; actions (Export, Add Partner); loading uses PageShell + LoadingSpinner.
- **MaterialsPage:** PageShell "Materials", breadcrumbs Settings / Materials; actions (ImportExportButtons, Add Material); loading uses PageShell + LoadingSpinner.
- **DocumentTemplatesPage:** PageShell "Document Templates", breadcrumbs Settings / Document Templates; actions (Cards/Table view toggle, New Template); loading uses PageShell + LoadingSpinner.
- **ServiceInstallersPage:** PageShell "Service Installers", breadcrumbs Settings / Service Installers; actions (ImportExportButtons, Export Excel, ADD); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, eighth batch – settings reference data):** PageShell added to 5 more settings sub-pages:
- **AssetTypesPage:** PageShell "Asset Types", breadcrumbs Settings / Asset Types; actions (Add Asset Type); loading uses PageShell + LoadingSpinner.
- **BuildingTypesPage:** PageShell "Building Types", breadcrumbs Settings / Building Types; actions (Add Building Type); loading uses PageShell + LoadingSpinner.
- **OrderTypesPage:** PageShell "Job Types", breadcrumbs Settings / Job Types; actions (Add Job Type); loading uses PageShell + LoadingSpinner; added loading early return.
- **PartnerGroupsPage:** PageShell "Partner Groups", breadcrumbs Settings / Partner Groups; actions (Add Group); loading uses PageShell + LoadingSpinner.
- **MaterialCategoriesPage:** PageShell "Material Categories", breadcrumbs Settings / Material Categories; actions (Add Category when canManage); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, ninth batch – settings):** PageShell added to 5 more settings sub-pages:
- **OrderStatusesPage:** PageShell with dynamic title "{selectedWorkflow} Workflow Statuses", breadcrumbs Settings / Workflow Statuses; actions (Flow View / List View toggle, Add Status); standalone Breadcrumbs and custom header removed; loading uses PageShell + LoadingSpinner.
- **PartnerRatesPage:** PageShell "Partner Rates (PU Rates)", breadcrumbs Settings / Partner Rates; actions (ImportExportButtons, Refresh, Add Rate); loading uses PageShell + LoadingSpinner.
- **MaterialTagsPage:** PageShell "Material Tags", breadcrumbs Settings / Material Tags; actions (Add Tag when canManage); loading uses PageShell + LoadingSpinner.
- **OrderCategoriesPage:** PageShell "Order Categories", breadcrumbs Settings / Order Categories; actions (Add Order Category); loading uses PageShell + LoadingSpinner.
- **MaterialVerticalsPage:** PageShell "Material Verticals", breadcrumbs Settings / Material Verticals; actions (Add Vertical when canManage); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, tenth batch – settings):** PageShell added to 5 more settings sub-pages:
- **SplittersPage:** PageShell "Splitters", breadcrumbs Settings / Splitters; actions (Add Splitter); loading uses PageShell + LoadingSpinner.
- **SplitterTypesPage:** PageShell "Splitter Types", breadcrumbs Settings / Splitter Types; actions (Add Splitter Type); loading uses PageShell + LoadingSpinner.
- **MaterialTemplatesPage:** PageShell "Material Templates", breadcrumbs Settings / Material Templates; actions (Create Template); loading uses PageShell + LoadingSpinner.
- **InstallationMethodsPage:** PageShell "Installation Methods", breadcrumbs Settings / Installation Methods; actions (Add Installation Method); loading uses PageShell + LoadingSpinner.
- **InstallationTypesPage:** PageShell "Service Categories", breadcrumbs Settings / Service Categories; actions (Add Service Category); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, eleventh batch – remaining settings):** PageShell added to 7 more settings sub-pages:
- **CompanyProfilePage:** PageShell with dynamic title (Edit/Create company profile), breadcrumbs Settings / Company Profile; loading uses PageShell + LoadingSpinner.
- **PnlTypesPage:** PageShell "P&L Types", breadcrumbs Settings / P&L Types; actions (Add P&L Type); loading uses PageShell + LoadingSpinner.
- **SkillsManagementPage:** PageShell "Skills Management", breadcrumbs Settings / Skills; actions (Add Skill when canManage); loading uses PageShell + LoadingSpinner.
- **VerticalsPage:** PageShell "Verticals", breadcrumbs Settings / Verticals; actions (Add Vertical); loading uses PageShell + LoadingSpinner.
- **SplitterMasterPage:** PageShell "Splitter Master", breadcrumbs Settings / Splitters; actions (Refresh); loading uses PageShell + LoadingSpinner.
- **RateEngineManagementPage:** PageShell "GPON Rate Engine", breadcrumbs Settings / Rate Engine; actions (Calculate, Refresh); loading uses PageShell + LoadingSpinner.
- **SiRatePlansPage:** PageShell "SI Rate Plans", breadcrumbs Settings / SI Rate Plans; actions (ImportExportButtons, Refresh, Add Rate Plan); loading uses PageShell + LoadingSpinner.

**P1 audit (Feb 2026, twelfth batch – final settings):** PageShell added to 4 remaining settings pages:
- **DocumentTemplateEditorPage:** PageShell with dynamic title (Edit/Create Document Template), breadcrumbs Settings / Document Templates / (name or New); actions (Back, Save Draft, Publish, Duplicate when edit, Test Render); loading uses PageShell + Card.
- **EmailSetupPage:** PageShell "Email Setup", breadcrumbs Settings / Email; tab navigation and content unchanged.
- **MaterialSetupPage:** PageShell "Material Setup", breadcrumbs Settings / Materials; tab navigation and content unchanged.
- **TimeSlotSettingsPage:** PageShell "Time Slot Settings", breadcrumbs Settings / Time Slots; actions (Seed Defaults when empty, Add Time Slot); loading uses PageShell + LoadingSpinner.

Settings PageShell audit complete. KpiProfilesPage (settings) now has PageShell (added Feb 2026: title "KPI Profiles", breadcrumbs Settings / KPI Profiles, actions Add KPI Profile; loading uses PageShell + LoadingSpinner). Any future settings sub-pages can use PageShell when added.

**P1 audit (Feb 2026, thirteenth batch – remaining app pages):** PageShell added to 6 pages that were missing it:
- **PaymentsPage:** PageShell "Payments", breadcrumbs Accounting / Payments; actions (Add Payment); loading uses PageShell + LoadingSpinner.
- **SupplierInvoicesPage:** PageShell "Supplier Invoices", breadcrumbs Accounting / Supplier Invoices; actions (Add Invoice); loading uses PageShell + LoadingSpinner.
- **EmailManagementPage:** PageShell "Email Management", breadcrumbs Email; actions (Parse Now, Parser Review, Compose, Poll Inbox); content in container.
- **FilesPage:** PageShell "Files", breadcrumbs Files; actions (Upload File); loading uses PageShell + LoadingSpinner.
- **CalendarPage:** PageShell "Calendar Schedule", breadcrumbs Scheduler / Calendar; actions (Draft Mode badge, Bulk Assign, Confirm Schedule); loading uses PageShell + LoadingSpinner; date nav and filter bar kept in toolbar.
- **SIAvailabilityPage:** PageShell "SI Availability", breadcrumbs Scheduler / SI Availability; actions (date picker); loading uses PageShell + LoadingSpinner.

Admin Portal PageShell coverage is now complete for all main app pages (accounting, email, files, scheduler).

---

## References

- [UI_CONSISTENCY_REPORT.md](UI_CONSISTENCY_REPORT.md) – Full audit and recommended standards.
- [UI_CONSISTENCY_BACKLOG.md](UI_CONSISTENCY_BACKLOG.md) – Backlog items for P2/P3 (created Feb 2026).
- [SYNCFUSION_VS_DATATABLE_RULE.md](SYNCFUSION_VS_DATATABLE_RULE.md) – When to use DataTable/StandardListTable vs Syncfusion Grid (P3-6 done).
