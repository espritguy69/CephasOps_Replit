# Product Gap Audit — CephasOps Frontend

**Date:** 2026-03-23  
**Scope:** Main admin app (`frontend/`) and SI app (`frontend-si/`)  
**Purpose:** Identify gaps between what's built and what's needed for real operational use, prioritized by business importance.

---

## Legend

| Category | Meaning |
|---|---|
| ✅ Complete & Usable | Production-ready, all key flows work |
| 🟢 Backend-Ready, Minor UX Gaps | Core works end-to-end, small polish items |
| 🟡 Visually/UX Incomplete | Page exists but missing important UX elements |
| 🟠 Action Flows Incomplete | Key operational workflows missing or broken |
| 🔴 Missing Critical Operational State | Entire feature absent or non-functional |

---

## 1. Orders Module

**Status: ✅ Complete & Usable**

### What's Built
- **List View:** Server-side pagination, debounced search, status/partner/date filters, bulk import (from email parser or file upload), Excel export, per-row status update, PDF download.
- **Detail View:** Tabbed interface (Details, Billing, Process Checklist, Notes). WorkflowTransitionButton for state-machine-driven transitions. Material Collection Alert. RMA/replacement warning banners. SMS/WhatsApp messaging ("Send Job Update", "SI On The Way"). Internal and partner notes with inline editing.
- **Create/Edit:** ~2400-line form with dynamic fields by Order Type. Parser Review mode with snapshot viewer, confidence scores, duplicate detection, material matching. Zod validation with conditional rules.
- **Loading/Error/Empty:** LoadingSpinner, EmptyState, toast error notifications consistently applied.

### Gaps (Minor)
- **G-ORD-1** *(Low)*: Dashboard shows hardcoded `SAMPLE_ORDERS` fallback when API fails — should show explicit error state instead of sample data.
- **G-ORD-2** *(Low)*: No bulk status-update on the list page (individual row action only).

---

## 2. Scheduler / Dispatch Module

**Status: ✅ Complete & Usable**

### What's Built
- **CalendarPage:** List-based view grouped by time slots. Manual and bulk assignment. Drag-and-drop (dnd-kit) for installer assignment. Schedule Draft→Confirmed flow. Date navigation, status filter, unassigned view toggle.
- **InstallerSchedulerPage (Timeline):** Fresha-style horizontal resource scheduler. Draft→Confirmed→Posted lifecycle. Reschedule approval/rejection. Conflict detection with alerts. Department/installer filters, Day/Week views.
- **SIAvailabilityPage:** Date-based availability lookup with loading/error/empty states.
- **Supporting Components:** SchedulerDetailDrawer, TimeChangeDialog, CompletionConfirmDialog, RescheduleDialog, OrderHistoryDialog.

### Gaps (Minor)
- **G-SCH-1** *(Low)*: No week-at-a-glance summary view for capacity planning across the whole week.
- **G-SCH-2** *(Low)*: SIAvailabilityPage uses basic HTML date input instead of a date picker component consistent with the rest of the app.

---

## 3. Inventory Module

**Status: ✅ Complete & Usable**

### What's Built
- **Dashboard:** KPIs (Total Materials, Serialized vs Non-Serial, Stock Value, Low Stock). Recent Movements sidebar. Warehouse Overview. Quick Stock In/Out modals.
- **List:** Tabbed "Materials" vs "Stock Levels". Add Material (permission-gated).
- **Stock Operations:** Full ledger-based CRUD — Receive (bulk + serialised), Transfer, Allocate, Issue, Return. All order-aware with serialised mode support.
- **Ledger/Summary:** Detailed audit log with Material/Location/Type/Order/Date filters. Stock Summary with On Hand/Reserved/Available.
- **Reports:** Usage by Period (CSV export), Serial Lifecycle trace, Stock Trend (daily/weekly/monthly snapshots).
- **Warehouse Layout:** Interactive visual warehouse/bin layout (Syncfusion).
- **RMA:** Dedicated RMAListPage.
- **Department Scope:** Enforced across all stock-altering pages.
- **Loading/Error/Empty:** Consistent — LoadingSpinner, Skeleton, EmptyState, 403 Access Denied handling.

### Gaps (Minor)
- **G-INV-1** *(Low)*: InventoryListPage has minimal filter UI beyond search — no category or status filters visible in the list view itself (compared to the rich filters on Ledger page).
- **G-INV-2** *(Low)*: No stocktake/cycle-count feature for physical inventory reconciliation.

---

## 4. Parser / Intake Module

**Status: ✅ Complete & Usable**

### What's Built
- **Listing:** Advanced filtering (status, source, date, search). Bulk approve/reject with validation notes. Inline editing modal. Duplicate detection. Material matching. Auto-refresh on window focus + 60s polling.
- **Dashboard:** Key metrics (sessions, success rate, pending, total drafts). Manual "Parse Now" trigger. Confidence distribution charts. Error analytics. Logs export (CSV/JSON).
- **Session Details:** Metadata, draft listing, retry logic, contextual email links.
- **Snapshot Viewer:** Syncfusion PDF viewer for source document verification. Approve/Reject workflow.

### Gaps
- **G-PAR-1** *(Low)*: No parser rule configuration UI — rules appear to be backend-managed only; operators cannot tune parsing rules from the admin UI.

---

## 5. Billing / Invoicing Module

**Status: ✅ Complete & Usable**

### What's Built
- **List:** Searchable, filterable with summary stats (Total, Draft, Sent, Paid, Overdue). Create Invoice modal with multi-line items.
- **Detail:** Full invoice display with line items (order-specific metadata). Status transitions (Draft→Sent→Paid). Quick action buttons. "Change Status" modal. Delete with confirmation. Overdue badge auto-calculation.
- **Edit:** Metadata and line item editing. Content lock when status is "Sent".
- **PDF/Print:** Backend PDF generation via `generateInvoicePdf`. Print Preview with server-rendered HTML in iframe.
- **e-Invoice (MyInvois):** Portal submission, rejection handling, status refresh.
- **Templates:** InvoiceDocument component with dedicated CSS.

### Gaps (Minor)
- **G-BIL-1** *(Medium)*: No dedicated Payments page within the billing module — payment recording is available via the Accounting module's PaymentsPage but not directly accessible from the Billing section navigation.
- **G-BIL-2** *(Low)*: No credit note or partial payment tracking within the invoice detail — only full "Mark as Paid".

---

## 6. Reports / KPI Module

**Status: ✅ Complete & Usable**

### What's Built
- **Reports Hub:** Dynamic report definitions from API. Category filtering. Search. Link to Payout Health.
- **Report Runner:** Generic parameter-based engine. Supports date, number, dropdown filters. Pagination. Export (CSV, Excel, PDF).
- **Payout Health:** Snapshot coverage, alert response summaries, anomaly detection.
- **Payout Anomalies:** Drill-down for specific flagged anomalies.
- **KPI Dashboard:** Total Jobs, On-Time Rate, Late Jobs, SLA Exceeded. Breakdown by Order Type.
- **KPI Profiles:** CRUD for KPI targets per Order Type / Building Type.

### Gaps
- None identified — module is functionally complete.

---

## 7. Dashboard

**Status: 🟢 Backend-Ready, Minor UX Gaps**

### What's Built
- KPI cards: Today's Orders, Active Installers, Pending NWO, Overdue CWO (with trend indicators).
- Charts: OrdersTrendChart (30-day), OrdersByPartnerChart.
- Recent Orders table with status badges, row actions (view/edit).
- Date range filter (Today, Yesterday, Last 7/30 days, Custom).
- Status filter for recent orders.

### Gaps
- **G-DASH-1** *(High)*: **Not role-aware.** Single "Operations Dashboard" view for all users. Finance users see the same operations-focused dashboard. No role-specific widgets (e.g., finance users should see revenue/billing KPIs, warehouse users should see inventory alerts, SI managers should see installer performance).
- **G-DASH-2** *(Medium)*: **No actionable alerts/blockers panel.** Missing a dedicated "Needs Attention" section that aggregates: stuck orders, SLA-near-breach, failed background jobs, unreconciled payments, low-stock alerts.
- **G-DASH-3** *(Medium)*: **Hardcoded SAMPLE_ORDERS fallback** — when the API fails, the dashboard renders fake sample data instead of showing an error state. This can mislead operators into thinking they see real orders.
- **G-DASH-4** *(Low)*: No "Quick Actions" panel for frequent tasks (create order, parse now, view schedule).

---

## 8. Insight Dashboards (Command Center)

**Status: ✅ Complete & Usable**

### What's Built
- **Platform Dashboard:** Cross-tenant health (SuperAdmin only).
- **Tenant Dashboard:** Company-level performance.
- **Operations Dashboard:** Daily control metrics, stuck orders.
- **Financial Dashboard:** Revenue, payouts, profit margins.
- **Risk Dashboard:** Aggregated risk signals.
- **Operational Intelligence:** "Explainable AI" — at-risk orders/installers/buildings with rule-based reasons.
- **SLA Breach Dashboard:** Severity-based view with countdown to breach.

### Gaps
- None identified — comprehensive suite.

---

## 9. Settings / Admin Module

**Status: 🟢 Backend-Ready, Minor UX Gaps**

### What's Built
- **Company:** Full CRUD for profile, localization settings.
- **Departments:** Full CRUD with Active/Inactive toggle, Cost Centre linking.
- **Partners:** Full CRUD with groups, categorization, contact management.
- **Materials:** Full CRUD + Bulk Actions + Import/Export. Barcode scanning, serialisation flags, default costs, verticals/tags.
- **Rates:** Rate Designer with simulation sandbox. Rate Groups with base work rates. SI Rate Plans. Partner Rates.
- **Service Profiles:** Full CRUD with profile mappings.
- **Workflow:** Workflow Definitions with Guard Conditions and Side Effects. Approval Workflows (Syncfusion inline editing).
- **Document Templates:** Full CRUD + duplication + Carbone engine integration + editor.
- **Reference Data:** Order Types, Order Categories, Building Types, Installation Methods, Splitter Types, Verticals, Material Categories, Material Templates, PnL Types, Asset Types, Order Statuses, Time Slots, SLA Configuration, Automation Rules, Escalation Rules, Business Hours, Skills Management.
- **Department-Scoped Master Data:** GPON, CWO, NWO each have their own scoped settings via DepartmentMasterDataWrapper.
- **Integrations:** IntegrationsPage for third-party config.
- **Email Setup:** EmailSetupPage for email account configuration.

### Gaps
- **G-SET-1** *(Medium)*: **No workflow visual editor.** Workflow definitions are list/form-based. A node-based visual editor showing the state machine would significantly improve usability for complex workflows.
- **G-SET-2** *(Low)*: Some settings routes have inconsistent path patterns — e.g., `/settings/settings/skills-management` (double "settings" prefix in the nested route) and `/settings/settings/automation-rules`.
- **G-SET-3** *(Low)*: No "Test" or "Dry Run" capability for Automation Rules or Escalation Rules before activating them.

---

## 10. Admin / System Module

**Status: ✅ Complete & Usable**

### What's Built
- **User Management:** Listing, search, role/status filtering, creation/editing, role assignment, password reset, session revocation. Security activity logs and active session monitoring.
- **Background Jobs:** Dashboard (success rates, P95 duration, jobs/h). Failed/running/stuck/recent job lists. Retry, correlation ID, timeline view.
- **Event Bus Monitor:** Throughput/failure metrics, top failing types/tenants, event tracing, handler logs, retry/replay.
- **Workers:** Active/stale worker instances, heartbeats, job ownership.
- **Operational Replay:** Batch replay with detail views.
- **State Rebuilder:** Operational state rebuild from Event Store/Ledger.
- **Event Ledger:** Operational ledger foundation.
- **Trace Explorer:** Search by Correlation/Event/Job/Entity ID. Unified execution timeline.
- **SLA Monitor:** Near-breach tracking with severity views.
- **SI Insights:** Orders visibility for SI performance analysis.
- **Platform Observability:** Cross-tenant health dashboard (SuperAdmin only).
- **Security Activity:** Auth event log.
- **Role Permissions:** RBAC v2 permissions matrix.

### Gaps
- **G-ADM-1** *(Low)*: No audit log export feature from the admin pages.

---

## 11. Payroll Module

**Status: ✅ Complete & Usable**

### What's Built
- **Periods:** View/manage payroll periods (year/month). Create Period action.
- **Runs:** KPI stats (Total, Draft, Pending, Finalized, Paid). Financial summary. Finalize/Pay/Export actions. Status-based tabs.
- **Earnings:** Job earning records with permission-based column rendering (rate visibility by role).

### Gaps
- None identified — functionally complete.

---

## 12. P&L Module

**Status: ✅ Complete & Usable**

### What's Built
- **Summary:** Revenue/Costs/Gross Profit/Net Profit KPIs. Waterfall and Trend charts. Period filtering.
- **Drilldown:** Per-order profitability. Complex filtering (Period, Partner, Department, SI, Order Type, KPI Result). Full financial breakdown modal with rate source tracking.
- **Overheads:** CRUD for cost centres and periods.

### Gaps
- None identified — functionally complete.

---

## 13. Accounting Module

**Status: ✅ Complete & Usable**

### What's Built
- **Dashboard:** Cash flow summary (Income, Expenses, Net). A/R and A/P tracking. Overdue invoice alerts.
- **Supplier Invoices:** Multi-line item support with tax rates and P&L type associations. Status workflow (Draft→Pending→Approved→Paid).
- **Payments:** Incoming/outgoing payment management. Reconciliation and voiding with reason.

### Gaps
- None identified — functionally complete.

---

## 14. Assets Module

**Status: ✅ Complete & Usable**

### What's Built
- **Dashboard:** Summary cards (Total Assets, Total Value, Current Book Value, Depreciation %). Status indicators for Under Maintenance, Disposed, Upcoming Maintenance. Assets by Type breakdown. Upcoming Maintenance list (next 30 days).
- **Asset Register (List):** Search and filter by type/status. Data table with Asset Tag, Name, Type, Location, Costs, Status. Add/edit modal with full fields.
- **Asset Detail:** Tabbed interface (Overview, Maintenance, Depreciation). Depreciation settings (Method, Useful Life, Salvage Value). Maintenance history with inline add. Depreciation schedule with period-level detail. Action buttons (Add Maintenance, Dispose, Delete).
- **Maintenance Schedule:** Overdue/Due This Week/Upcoming indicators. Tabbed views (Upcoming vs All). Type and status filters. Schedule new and mark complete.
- **Depreciation Report:** Financial summary. Run Depreciation with preview mode. Post to GL for finalization. Period-based and detailed entry views.

### Gaps
- None identified — functionally complete.

---

## 15. Buildings Module

**Status: 🟢 Backend-Ready, Minor UX Gaps**

### What's Built
- **Dashboard:** KPIs (Total/Active Buildings, Total Orders, Growth, Materials, States). Recent Buildings list with search. Navigation to add/list.
- **List:** Filter by Property Type, Installation Method, State, Active Status. Bulk export/delete. Import from CSV with template download.
- **Detail:** ~2100-line comprehensive page. Tabbed interface (General, Contacts & Maintenance, House Rules, Infrastructure, Default Materials). MDU (Block/Splitter) and SDU (Street/Hub Box/Pole) infrastructure hierarchies. Capacity tracking (used vs total ports). Role-based contact management.
- **TreeGrid:** Syncfusion TreeGrid with Building→Block→Floor→Unit hierarchy. Color-coded utilization bars.
- **Building Merge:** Admin tool for merging duplicates with order reassignment and preview.

### Gaps
- **G-BLD-1** *(Medium)*: **TreeGrid uses sample/hardcoded data.** The Syncfusion TreeGrid view is functional but not wired to a production API endpoint — uses static sample hierarchy.
- **G-BLD-2** *(Low)*: Buildings list "Deactivate" action is stubbed with "not implemented" toast despite API support existing.

---

## 16. Tasks Module

**Status: ✅ Complete & Usable**

### What's Built
- **Tasks List:** Search, status/priority/user filters, multi-column sorting. Task creation via modal. Status updates. Responsive mobile card view alongside desktop table.
- **Kanban Board:** Syncfusion Kanban with drag-and-drop (TODO→In Progress→Review→Done). Department swimlanes. Priority color-coding.
- **My Tasks:** Auto-categorized sections (Overdue, Pending, In Progress, Completed). Status-based filtering.
- **Department Tasks:** Department-scoped view with scoped task creation.

### Gaps
- None identified — functionally complete.

---

## 17. Email Module

**Status: ✅ Complete & Usable**

### What's Built
- **Email Management:** Unified Inbox/Sent views. Email composition with template support and placeholders. Mailbox management. RBAC-filtered visibility (SuperAdmin vs Admin).
- **Parser Integration:** Email Parser Statistics widget with real-time stats. Manual "Parse Now" and "Poll Inbox" triggers.
- **Configuration:** Mailbox management, email rules, and parser template configuration.

### Gaps
- None identified — functionally complete.

---

## 18. Documents & Files

**Status: ✅ Complete & Usable**

### What's Built
- **Documents Page:** Lists generated documents (Invoices, Dockets, etc.) with type, reference entity (Order/SI), and generation timestamp. Direct download/view.
- **Files Page:** General-purpose file management. Upload/download. Metadata tracking (name, category, size, date).
- **Document Template Editor:** Full template editing with Carbone engine integration, variable insertion, placeholder validation.

### Gaps
- None identified — functionally complete.

---

## 19. Notifications Module

**Status: ✅ Complete & Usable**

### What's Built
- **Notifications Center:** Segregated views (Unread, Read, Archived). Filter by status and type (VIP Email, Task Assigned, System, etc.). Bulk "Mark All Read".
- **Notifications Page:** Standard listing with NotificationContext state management. Consistent filtering.

### Gaps
- None identified — functionally complete.

---

## 20. Operations (Dockets & Payout Breakdown)

**Status: ✅ Complete & Usable**

### What's Built
- **Dockets Page:** Workflow management for docket lifecycle (Received→Verified→Rejected→Uploaded). Built-in checklist verification (Splitter, Port, ONU, Photos) required before "Verified" status. Rejection with mandatory reason for SI communication.
- **Installer Payout Breakdown:** Order-level payout transparency. Supports both "Snapshot" (persisted) and "Live" (calculated) breakdowns. Installer-level earnings aggregation.

### Gaps
- None identified — functionally complete.

---

## 21. SI App (frontend-si)

**Status: 🟡 Visually/UX Incomplete (core job flows work, but mobile UX architecture significantly diverges from spec)**

### What's Built
- **Dashboard:** KPI cards (Total Jobs, Pending, In Progress, Completed). Recent Jobs list. Admin-specific view with Low Stock Alerts.
- **Jobs List:** All assigned jobs with status badges and color mapping per architecture spec.
- **Job Detail:** Full guided workflow (ASSIGNED→ON_THE_WAY→MET_CUSTOMER→START_WORK→COMPLETE). Serialized and non-serialized material entry. Assurance replacement flow (Record Replacement, Mark Faulty). Photo upload/proof capture. Completion review with validation. Reschedule request modal.
- **Earnings:** Period-based earnings display with detailed table (Order ID, Type, Rate, Final Pay). SubconRoute protection.
- **Materials Scan:** Camera-based QR/barcode scanner (html5-qrcode). Real-time inventory validation. GPS location capture per scan.
- **Materials Tracking:** Materials list, Stock Levels, Movement history tabs.
- **Material Returns:** Faulty items, replacements, RMA status. Status filtering (Faulty, Returned, RMA Created).
- **Orders Tracking:** Admin-only view with date/status/SI filters.
- **Service Installers:** SI management page within the app.
- **Auth:** ProtectedRoute + SubconRoute for role-based access.

### Gaps
- **G-SI-1** *(High)*: **No tab-based filtering on Jobs List.** Architecture spec (Section 6) requires Today/Upcoming/History tabs for job cards. Currently shows a flat list without temporal grouping. Installers need "what's next today" vs "upcoming" distinction.
- **G-SI-2** *(High)*: **Missing Home Screen as specified.** Architecture spec (Section 5) defines a rich Home Screen with "Good Morning, [Name]", Active Job Card with [OPEN JOB][NAVIGATE][CALL] buttons, Earnings Summary with bonus tier progress, and Quick Actions (Scan Device, My Inventory, Report Problem, Help). The current DashboardPage is a simplified KPI card layout without these operational elements.
- **G-SI-3** *(Medium)*: **No "Current Job" persistent bar.** Architecture spec defines a persistent bar ("Current Job: Mr Lim – 2.4 km away [OPEN]") visible across all tabs when a job is active. Not implemented.
- **G-SI-4** *(Medium)*: **No bottom tab navigation.** Architecture spec (Section 3) requires 5 primary tabs: Home | Jobs | Scan | Earnings | Profile. The current app uses a sidebar-style MainLayout, not the mobile-first bottom tab pattern expected for a field app.
- **G-SI-5** *(Medium)*: **No Profile/Support screens.** Architecture spec (Section 4) lists Profile tab and Support screens (Help Center, Call Admin, Technical Support, Report App Issue). None are implemented.
- **G-SI-6** *(Medium)*: **No Extra Work flow.** Architecture spec (Section 12) defines a customer-facing extra work flow (Select Service → Calculate Price → Customer Approval → Upload Proof → Record Payment → Generate Receipt). Not implemented in the Job Detail page.
- **G-SI-7** *(Medium)*: **No Installer Inventory screen.** Architecture spec (Section 18) requires a "My Inventory" screen showing serialized devices in hand and non-serialized stock levels. The MaterialsTrackingPage exists but doesn't match the specified "My Inventory" concept (installer-scoped view of what they're carrying).
- **G-SI-8** *(Low)*: Earnings breakdown lacks distinction between Base Job, Extra Work, and Bonus Incentive categories (architecture spec Section 17). Currently shows combined "Amount" column.
- **G-SI-9** *(Low)*: "Next Bonus Tier" progress indicator (architecture spec Section 5) not visible in the dashboard.
- **G-SI-10** *(Low)*: No OTP verification screen in the auth flow (architecture spec Section 4 lists it).

---

## 22. Role-Based Experience

**Status: 🟡 Visually/UX Incomplete**

### What's Built
- **Route Protection:** ProtectedRoute (authentication gate), SettingsProtectedRoute (settings access by role/permission).
- **Sidebar Visibility:** Permission-based nav item visibility using RBAC v2 permissions with legacy role fallback. SuperAdmin bypasses all checks.
- **Access Denied State:** SettingsProtectedRoute shows "Access Denied" with explanation and Go Back button.
- **Department Scoping:** DepartmentMasterDataWrapper isolates settings by department code (GPON/CWO/NWO).
- **RBAC v2:** Backend returns permission arrays; sidebar and some pages check `user.permissions`.

### Gaps
- **G-ROLE-1** *(High)*: **No role-specific dashboard.** All roles see the same Operations Dashboard (see G-DASH-1). Finance, warehouse, and SI-manager personas need tailored landing experiences.
- **G-ROLE-2** *(Medium)*: **No no-access fallback for non-settings protected routes.** Routes like `/billing`, `/payroll`, `/pnl` have `permission` annotations in the sidebar nav but the routes themselves are not wrapped in any permission guard — only the sidebar link is hidden. A user who knows the URL can navigate directly.
- **G-ROLE-3** *(Medium)*: **Inconsistent permission granularity.** Some sidebar items use fine-grained permissions (`orders.view`, `billing.view`, `inventory.view`) while others have no permission check at all (Dashboard, Notifications, some insight dashboards). No consistent enforcement at the route level.
- **G-ROLE-4** *(Low)*: Admin pages use SettingsProtectedRoute (role-based) but the role check is broad — any of SuperAdmin, Admin, Director, HeadOfDepartment, or Supervisor can access any admin page. No per-page permission differentiation within admin.

---

## 23. Cross-Cutting Concerns

### Token Expiry / Auth UX

**Status: 🟢 Backend-Ready, Minor UX Gaps**

- Token auto-refresh every 15 minutes via AuthContext.
- 401 responses clear tokens and redirect to login.
- `mustChangePassword` redirection handled.
- ErrorBoundary wraps the entire app.

#### Gaps
- **G-AUTH-1** *(Medium)*: **No user-facing session expiry warning.** When the token refresh fails or the session is about to expire, the user is silently redirected to login with no warning or "session expired" toast. Work in progress (unsaved forms) is lost without notice.
- **G-AUTH-2** *(Low)*: No "Remember me" or configurable session duration option on the login page.

### API Error Handling

**Status: 🟢 Backend-Ready, Minor UX Gaps**

- Most pages use toast notifications for API errors.
- 403 handling on inventory pages with access denied UI.
- Loading states via LoadingSpinner or Skeleton components.

#### Gaps
- **G-ERR-1** *(Medium)*: **Inconsistent error presentation.** Some pages show inline error banners (SIAvailabilityPage), some only show toasts (most pages), and some silently fall back to sample data (DashboardPage). No unified error boundary strategy per page section.
- **G-ERR-2** *(Low)*: No retry button on error states — user must manually refresh the page.

### Mobile / Responsive Design

**Status: 🟡 Visually/UX Incomplete**

#### Gaps
- **G-MOB-1** *(Medium)*: **Main admin app not optimized for mobile.** The sidebar-based layout with dense data tables is desktop-first. No responsive breakpoints for table columns or stacked card layouts on smaller screens.
- **G-MOB-2** *(High — for SI App)*: **SI app uses desktop-style layout instead of mobile-first patterns.** The architecture spec calls for bottom tab navigation, large touch targets, and one-hand usability. The current implementation uses a sidebar-style MainLayout more suitable for desktop/tablet.

---

## Prioritized Gap Summary

> **Note:** G-DASH-1 and G-ROLE-1 describe the same root problem (no role-aware dashboard) and are merged below. G-SI-4 and G-MOB-2 describe the same root problem (SI app not mobile-first) and are merged below.

### Priority 1 — Critical for Operational Use

| ID | Module | Gap | Business Impact |
|---|---|---|---|
| G-SI-2 | SI App | Missing rich Home Screen per architecture spec | Installers lack daily overview, active job card, quick actions |
| G-SI-4 / G-MOB-2 | SI App | No bottom tab navigation; desktop-style layout instead of mobile-first | Core field operations tool doesn't match mobile usage pattern; unusable one-handed |
| G-SI-1 | SI App | No Today/Upcoming/History tabs on job list | Installers can't distinguish today's vs future work |
| G-DASH-1 / G-ROLE-1 | Dashboard / Roles | Not role-aware — single operations view for all roles | Finance/warehouse/SI-manager personas see irrelevant data; no tailored landing |
| G-ROLE-2 | Roles | No route-level permission guards (only sidebar hiding) | **Security:** Users with direct URLs can access unauthorized modules (e.g., `/billing`, `/payroll`). Backend authorization may mitigate data exposure, but UI still renders unauthorized pages. |
| G-ROLE-3 | Roles | Inconsistent permission granularity at route level | **Security:** Some modules (Dashboard, Notifications, some insights) have no permission check at route or sidebar level |

### Priority 2 — Important for Production Readiness

| ID | Module | Gap | Business Impact |
|---|---|---|---|
| G-SI-3 | SI App | No persistent "Current Job" bar | Installers lose context when navigating between tabs |
| G-SI-6 | SI App | No Extra Work flow | Can't capture customer-paid work, losing revenue tracking |
| G-SI-5 | SI App | No Profile/Support screens | No in-app help or admin contact for field installers |
| G-SI-7 | SI App | No "My Inventory" (installer-scoped) screen | Installers can't verify what stock they're carrying |
| G-DASH-2 | Dashboard | No actionable alerts/blockers panel | Operators miss critical items needing attention |
| G-DASH-3 | Dashboard | Hardcoded sample data fallback | Operators may see fake data when API is down |
| G-AUTH-1 | Auth | No session expiry warning | Users lose unsaved work without notice on token expiry |
| G-ERR-1 | Cross-Cutting | Inconsistent error presentation | Confusing UX when different pages handle errors differently |
| G-BIL-1 | Billing | No direct payment recording from billing section | Finance must navigate to separate Accounting module for payments |
| G-BLD-1 | Buildings | TreeGrid uses sample/hardcoded data | Advanced view not connected to production data |
| G-SET-1 | Settings | No visual workflow editor | Complex workflow definitions hard to understand in list/form view |

### Priority 3 — Nice-to-Have Improvements

| ID | Module | Gap | Business Impact |
|---|---|---|---|
| G-SI-8 | SI App | Earnings lack Base/Extra/Bonus breakdown | Less transparency for installers on income sources |
| G-SI-9 | SI App | No bonus tier progress indicator | Installers can't see incentive targets |
| G-SI-10 | SI App | No OTP verification in auth flow | Reduced security for field app |
| G-ORD-1 | Orders | Sample data fallback on dashboard | Minor — already covered by G-DASH-3 |
| G-ORD-2 | Orders | No bulk status update on list | Efficiency improvement for high-volume operations |
| G-INV-1 | Inventory | Minimal filters on list view | Inventory list less usable for large catalogs |
| G-INV-2 | Inventory | No stocktake/cycle-count feature | Physical inventory reconciliation is manual |
| G-PAR-1 | Parser | No parser rule configuration UI | Parser tuning requires backend access |
| G-BIL-2 | Billing | No credit note or partial payment | Limited invoice flexibility |
| G-BLD-2 | Buildings | "Deactivate" action stubbed despite API support | Minor feature incomplete |
| G-SET-2 | Settings | Inconsistent settings route paths | Minor navigation confusion |
| G-SET-3 | Settings | No dry-run for automation/escalation rules | Risk of deploying untested rules |
| G-ADM-1 | Admin | No audit log export | Compliance reporting requires manual extraction |
| G-SCH-1 | Scheduler | No weekly capacity summary view | Planning across full week requires switching days |
| G-SCH-2 | Scheduler | Inconsistent date picker on SI Availability | Minor UI inconsistency |
| G-AUTH-2 | Auth | No "Remember me" option | Minor convenience |
| G-ERR-2 | Cross-Cutting | No retry button on error states | User must refresh page manually |
| G-MOB-1 | Cross-Cutting | Admin app not mobile-optimized | Desktop-first is acceptable for admin, but tablet use limited |
| G-ROLE-4 | Roles | Admin pages lack per-page permission differentiation | All admin-level roles see everything in admin section |

---

## Module Completeness Matrix

| Module | Category | Notes |
|---|---|---|
| Orders | ✅ Complete & Usable | Rich create/edit/detail with parser integration |
| Scheduler/Dispatch | ✅ Complete & Usable | Two scheduler views + availability management |
| Inventory | ✅ Complete & Usable | Full ledger-based operations + reports |
| Parser/Intake | ✅ Complete & Usable | Automated ingestion with manual review |
| Billing/Invoicing | ✅ Complete & Usable | Full lifecycle + e-Invoice integration |
| Reports/KPI | ✅ Complete & Usable | Dynamic report engine + KPI profiles |
| Insight Dashboards | ✅ Complete & Usable | 7 specialized operational dashboards |
| Payroll | ✅ Complete & Usable | Period/run/earnings management |
| P&L | ✅ Complete & Usable | Summary + drilldown + overheads |
| Accounting | ✅ Complete & Usable | Dashboard + supplier invoices + payments |
| Admin/System | ✅ Complete & Usable | Comprehensive observability and management |
| Assets | ✅ Complete & Usable | Full lifecycle — acquisition, maintenance, depreciation |
| Tasks | ✅ Complete & Usable | List + Kanban + My Tasks + Department Tasks |
| Email | ✅ Complete & Usable | Inbox/compose + parser integration + config |
| Documents & Files | ✅ Complete & Usable | Generated docs + general file management |
| Notifications | ✅ Complete & Usable | Center with filtering + bulk actions |
| Operations (Dockets) | ✅ Complete & Usable | Docket workflow + payout breakdown |
| Buildings | 🟢 Backend-Ready, Minor UX Gaps | TreeGrid needs API hookup; Deactivate stubbed |
| Settings/Admin | 🟢 Backend-Ready, Minor UX Gaps | Missing visual workflow editor |
| Dashboard | 🟢 Backend-Ready, Minor UX Gaps | Not role-aware, no alerts panel |
| SI App | 🟡 Visually/UX Incomplete | Core job flows work; mobile UX architecture diverges significantly from spec |
| Role-Based Experience | 🟡 Visually/UX Incomplete | Sidebar hiding works; route-level guards incomplete — security concern |
| Cross-Cutting (Auth/Errors/Mobile) | 🟢 Backend-Ready, Minor UX Gaps | Token handling solid; error UX inconsistent |

---

## Route Coverage Appendix

Every top-level route from `frontend/src/App.tsx` and `frontend-si/src/App.tsx` is covered in the audit sections above. The following mapping confirms coverage:

| Route Group | Audit Section |
|---|---|
| `/dashboard` | §7 Dashboard |
| `/orders`, `/orders/create`, `/orders/:id` | §1 Orders |
| `/orders/parser/*` | §4 Parser/Intake |
| `/scheduler/*` | §2 Scheduler/Dispatch |
| `/inventory/*`, `/rma` | §3 Inventory |
| `/billing/*` | §5 Billing/Invoicing |
| `/payroll/*` | §11 Payroll |
| `/pnl/*` | §12 P&L |
| `/accounting/*` | §13 Accounting |
| `/reports/*` | §6 Reports/KPI |
| `/kpi/*` | §6 Reports/KPI |
| `/insights/*` | §8 Insight Dashboards |
| `/assets/*` | §14 Assets |
| `/buildings/*` | §15 Buildings |
| `/tasks/*` | §16 Tasks |
| `/email` | §17 Email |
| `/documents`, `/doc-templates/*` | §18 Documents & Files |
| `/files` | §18 Documents & Files |
| `/notifications` | §19 Notifications |
| `/operations/*` | §20 Operations |
| `/workflow/*` | §9 Settings |
| `/settings/*` | §9 Settings |
| `/admin/*` | §10 Admin/System |
| Auth routes (`/login`, `/change-password`, etc.) | §23 Cross-Cutting (Auth) |
| SI App: `/dashboard` | §21 SI App |
| SI App: `/jobs`, `/jobs/:id` | §21 SI App |
| SI App: `/orders` | §21 SI App |
| SI App: `/materials/*` | §21 SI App |
| SI App: `/earnings` | §21 SI App |
| SI App: `/service-installers` | §21 SI App |
| SI App: `/login` | §21 SI App |
