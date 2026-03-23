# CephasOps – Admin Frontend Page Inventory

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## 1. Page Organization

The admin frontend is organized into functional modules, each containing related pages. Pages are located in `frontend/src/pages/`.

---

## 2. Core Operational Pages

### 2.1 Dashboard
**File:** `DashboardPage.tsx`  
**Route:** `/dashboard`  
**Purpose:** Main dashboard with KPIs, charts, and quick actions  
**API Dependencies:** 
- Dashboard statistics
- KPI data
- Recent orders
- Unread notifications

**State Management:** TanStack Query for data fetching

---

### 2.2 Orders Module

#### Orders List (Standard)
**File:** `orders/OrdersListPage.tsx`  
**Route:** `/orders`  
**Purpose:** List all orders with filters and search  
**API Dependencies:** `GET /api/orders`  
**Key Features:**
- Status filtering
- Partner filtering
- Date range filtering
- SI assignment filtering
- Order detail navigation

#### Orders List (Enhanced)
**File:** `orders/OrdersListPageEnhanced.tsx`  
**Route:** `/orders-enhanced`  
**Purpose:** Syncfusion-enhanced orders list with advanced features  
**API Dependencies:** `GET /api/orders`  
**Key Features:
- Syncfusion DataGrid
- Advanced filtering
- Column customization
- Export to Excel
- Bulk operations

#### Order Detail
**File:** `orders/OrderDetailPage.tsx`  
**Route:** `/orders/:orderId`  
**Purpose:** View and edit order details  
**API Dependencies:**
- `GET /api/orders/{id}`
- `POST /api/workflow/{orderId}/transition`
- `GET /api/orders/{orderId}/checklist`
- `POST /api/orders/{orderId}/checklist/answers`

**Key Features:**
- Order information display
- Status transition buttons
- Checklist completion
- Material usage tracking
- Docket upload
- Status history timeline

#### Create Order
**File:** `orders/CreateOrderPage.tsx`  
**Route:** `/orders/create`  
**Purpose:** Manually create new order  
**API Dependencies:** `POST /api/orders`  
**Key Features:**
- Order form
- Partner selection
- Customer details
- Appointment scheduling
- Building selection

---

### 2.3 Scheduler Module

#### Calendar (Standard)
**File:** `scheduler/CalendarPage.tsx`  
**Route:** `/scheduler`  
**Purpose:** Calendar view of SI assignments  
**API Dependencies:**
- `GET /api/scheduler/slots`
- `GET /api/scheduler/unassigned`

**Key Features:**
- Day/Week/Month view
- SI assignment
- Unassigned orders list
- Drag-and-drop (future)

#### Calendar (Enhanced)
**File:** `scheduler/CalendarPageEnhanced.tsx`  
**Route:** `/scheduler/enhanced`  
**Purpose:** Syncfusion Schedule component  
**API Dependencies:** Same as standard calendar  
**Key Features:**
- Syncfusion Schedule
- Rich calendar UI
- Advanced scheduling features

#### SI Availability
**File:** `scheduler/SIAvailabilityPage.tsx`  
**Route:** `/scheduler/availability`  
**Purpose:** Manage SI availability and leave  
**API Dependencies:**
- `GET /api/scheduler/si-availability/{siId}`
- `POST /api/scheduler/si-leave`

**Key Features:**
- SI availability calendar
- Leave request management
- Availability editing

---

### 2.4 Parser Module

#### Parse Session Review
**File:** `parser/ParseSessionReviewPage.tsx`  
**Route:** `/orders/parser`  
**Purpose:** Review and approve/reject parsed order drafts  
**API Dependencies:**
- `GET /api/parser/sessions`
- `GET /api/parser/sessions/{id}`
- `POST /api/parser/sessions/{id}/approve`
- `POST /api/parser/sessions/{id}/reject`

**Key Features:**
- Parsed draft list
- Draft detail view
- Approval/rejection actions
- Duplicate detection

#### Parser Snapshot Viewer
**File:** `parser/ParserSnapshotViewerPage.tsx`  
**Route:** `/orders/parser/snapshots`  
**Purpose:** View parser snapshots and debugging info  
**API Dependencies:** `GET /api/parser/sessions/{id}/snapshot`

---

### 2.5 Inventory Module

#### Inventory Dashboard
**File:** `inventory/InventoryDashboardPage.tsx`  
**Route:** `/inventory`  
**Purpose:** Inventory overview and KPIs  
**API Dependencies:** Inventory statistics

#### Inventory List (Standard)
**File:** `inventory/InventoryListPage.tsx`  
**Route:** `/inventory/list`  
**Purpose:** List materials and stock levels  
**API Dependencies:**
- `GET /api/inventory/materials`
- `GET /api/inventory/stock`

#### Inventory List (Enhanced)
**File:** `inventory/InventoryListPageEnhanced.tsx`  
**Route:** `/inventory-enhanced`  
**Purpose:** Syncfusion-enhanced inventory list  
**Key Features:**
- Advanced filtering
- Stock level tracking
- Material movement history

#### Warehouse Layout
**File:** `inventory/WarehouseLayoutPage.tsx`  
**Route:** `/inventory/warehouse-layout`  
**Purpose:** Visual warehouse layout (Syncfusion Diagram)  
**Key Features:**
- Visual warehouse representation
- Bin locations
- Stock visualization

---

### 2.6 Billing Module

#### Invoices List
**File:** `billing/InvoicesListPage.tsx`  
**Route:** `/billing/invoices`  
**Purpose:** List invoices with filters  
**API Dependencies:** `GET /api/billing/invoices`  
**Key Features:**
- Status filtering
- Partner filtering
- Date range filtering
- Invoice creation
- Portal submission status

#### Invoice Detail
**File:** `billing/InvoiceDetailPage.tsx`  
**Route:** `/billing/invoices/:id`  
**Purpose:** View invoice details and PDF  
**API Dependencies:**
- `GET /api/billing/invoices/{id}`
- `GET /api/documents/{invoiceId}/pdf`

**Key Features:**
- Invoice information
- Line items display
- PDF preview
- Payment tracking
- Submission history

---

### 2.7 Payroll Module

#### Payroll Periods
**File:** `payroll/PayrollPeriodsPage.tsx`  
**Route:** `/payroll/periods`  
**Purpose:** List and manage payroll periods  
**API Dependencies:** `GET /api/payroll/periods`

#### Payroll Runs
**File:** `payroll/PayrollRunsPage.tsx`  
**Route:** `/payroll/runs`  
**Purpose:** List and manage payroll runs  
**API Dependencies:**
- `GET /api/payroll/runs`
- `POST /api/payroll/runs`
- `POST /api/payroll/runs/{id}/finalise`

#### Payroll Earnings
**File:** `payroll/PayrollEarningsPage.tsx`  
**Route:** `/payroll/earnings`  
**Purpose:** View SI earnings for a period  
**API Dependencies:** `GET /api/payroll/earnings`

---

### 2.8 P&L Module

#### P&L Summary
**File:** `pnl/PnlSummaryPage.tsx`  
**Route:** `/pnl/summary`  
**Purpose:** High-level P&L summary  
**API Dependencies:** `GET /api/pnl/summary`  
**Key Features:**
- Period selection
- Revenue vs costs
- Profit margins
- Department breakdown

#### P&L Drilldown
**File:** `pnl/PnlDrilldownPage.tsx`  
**Route:** `/pnl/drilldown`  
**Purpose:** Detailed P&L drilldown  
**API Dependencies:** `GET /api/pnl/drilldown`  
**Key Features:**
- Per-order P&L
- Cost breakdown
- Partner analysis

#### P&L Overheads
**File:** `pnl/PnlOverheadsPage.tsx`  
**Route:** `/pnl/overheads`  
**Purpose:** Manage overhead entries  
**API Dependencies:**
- `GET /api/pnl/overheads`
- `POST /api/pnl/overheads`
- `DELETE /api/pnl/overheads/{id}`

---

### 2.9 Accounting Module

#### Accounting Dashboard
**File:** `accounting/AccountingDashboardPage.tsx`  
**Route:** `/accounting`  
**Purpose:** Accounting overview

#### Supplier Invoices
**File:** `accounting/SupplierInvoicesPage.tsx`  
**Route:** `/accounting/supplier-invoices`  
**API Dependencies:** `GET /api/accounting/supplier-invoices`

#### Payments
**File:** `accounting/PaymentsPage.tsx`  
**Route:** `/accounting/payments`  
**API Dependencies:** `GET /api/billing/payments`

---

### 2.10 Assets Module

#### Assets Dashboard
**File:** `assets/AssetsDashboardPage.tsx`  
**Route:** `/assets`  
**Purpose:** Assets overview

#### Assets List
**File:** `assets/AssetsListPage.tsx`  
**Route:** `/assets/list`  
**API Dependencies:** `GET /api/assets`

#### Asset Detail
**File:** `assets/AssetDetailPage.tsx`  
**Route:** `/assets/:id`  
**API Dependencies:** `GET /api/assets/{id}`

#### Maintenance Schedule
**File:** `assets/MaintenanceSchedulePage.tsx`  
**Route:** `/assets/maintenance`  
**API Dependencies:** `GET /api/assets/maintenance`

#### Depreciation Report
**File:** `assets/DepreciationReportPage.tsx`  
**Route:** `/assets/depreciation`  
**API Dependencies:** `GET /api/assets/depreciation`

---

### 2.11 Buildings Module

#### Buildings Dashboard
**File:** `buildings/BuildingsDashboardPage.tsx`  
**Route:** `/buildings`  
**Purpose:** Buildings overview

#### Buildings List
**File:** `buildings/BuildingsListPage.tsx`  
**Route:** `/buildings/list`  
**API Dependencies:** `GET /api/buildings`

#### Building Detail
**File:** `buildings/BuildingDetailPage.tsx`  
**Route:** `/buildings/:id`  
**API Dependencies:** `GET /api/buildings/{id}`

#### Buildings Tree Grid
**File:** `buildings/BuildingsTreeGridPage.tsx`  
**Route:** `/buildings/treegrid`  
**Purpose:** Syncfusion TreeGrid for hierarchical building view

---

### 2.12 RMA Module

#### RMA List
**File:** `rma/RMAListPage.tsx`  
**Route:** `/rma`  
**API Dependencies:** `GET /api/rma`

---

### 2.13 Tasks Module

#### Tasks List
**File:** `tasks/TasksListPage.tsx`  
**Route:** `/tasks`  
**API Dependencies:** `GET /api/tasks`

#### Tasks Kanban
**File:** `tasks/TasksKanbanPage.tsx`  
**Route:** `/tasks/kanban`  
**Purpose:** Syncfusion Kanban board for tasks  
**API Dependencies:** `GET /api/tasks`

---

### 2.14 Workflow Module

#### Workflow Definitions
**File:** `workflow/WorkflowDefinitionsPage.tsx`  
**Route:** `/workflow/definitions`  
**API Dependencies:** `GET /api/workflow-definitions`

#### Guard Conditions
**File:** `workflow/GuardConditionsPage.tsx`  
**Route:** `/workflow/guard-conditions`  
**API Dependencies:** `GET /api/workflow/guard-conditions`

#### Side Effects
**File:** `workflow/SideEffectsPage.tsx`  
**Route:** `/workflow/side-effects`  
**API Dependencies:** `GET /api/workflow/side-effects`

---

### 2.15 Email Module

#### Email Management
**File:** `email/EmailManagementPage.tsx`  
**Route:** `/email`  
**API Dependencies:**
- `GET /api/emails`
- `POST /api/email/ingest`

---

### 2.16 Documents Module

#### Documents
**File:** `documents/DocumentsPage.tsx`  
**Route:** `/documents`  
**API Dependencies:** `GET /api/documents`

---

### 2.17 Files Module

#### Files
**File:** `files/FilesPage.tsx`  
**Route:** `/files`  
**API Dependencies:** `GET /api/files`

---

## 3. Settings Pages

All settings pages are under `/settings/*` and protected by `SettingsProtectedRoute`.

### 3.1 Core Settings (9 pages)
- Partners (Enhanced)
- Service Installers (Enhanced)
- Departments (Enhanced)
- Buildings (Enhanced)
- Order Types (Enhanced)
- Order Statuses (Enhanced)
- Installation Methods (Enhanced)
- Material Categories
- Materials (Enhanced)

### 3.2 Operations & HR (8 pages)
- Asset Types (Enhanced)
- Cost Centers (Enhanced)
- Teams (Enhanced)
- Roles (Enhanced)
- Product Types (Enhanced)
- Service Plans (Enhanced)
- Brands (Enhanced)
- Vendors (Enhanced)

### 3.3 Inventory & Finance (4 pages)
- Warehouses (Enhanced)
- Bins (Enhanced)
- Payment Terms (Enhanced)
- Tax Codes (Enhanced)

### 3.4 Templates (4 pages)
- Document Templates (Enhanced)
- Email Templates (Enhanced)
- SMS Templates (Enhanced)
- WhatsApp Templates (Enhanced)

### 3.5 System & Reports (5 pages)
- Report Definitions (Enhanced)
- System Settings (Enhanced)
- KPI Profiles (Enhanced)
- SLA Configuration
- Automation Rules
- Approval Workflows
- Business Hours
- Escalation Rules
- Guard Condition Definitions
- Side Effect Definitions
- Rate Plans (Enhanced)
- Notification Templates (Enhanced)

### 3.6 Standard Settings Pages
- Company Profile
- Company Deployment
- Department Deployment
- Departments
- Verticals
- Partners
- Partner Groups
- Service Installers
- SI Rates
- Partner Rates
- Rate Engine
- Order Types
- Installation Types
- Building Types
- Installation Methods
- Splitter Types
- Splitters
- Materials
- Document Templates
- KPI Profiles
- Email Setup
- P&L Types
- Asset Types
- Order Statuses
- Time Slots
- Buildings

### 3.7 Visual Features
- Splitter Topology (`/settings/splitter-topology`) - Syncfusion Diagram

---

## 4. State Management

### 4.1 TanStack Query
- **Data Fetching:** All API calls use TanStack Query
- **Caching:** Automatic caching and invalidation
- **Mutations:** Optimistic updates for better UX

### 4.2 Context API
- **AuthContext:** User authentication state
- **DepartmentContext:** Active department selection
- **NotificationContext:** Notification state
- **ThemeContext:** Theme preferences
- **CompanySettingsContext:** Company settings

### 4.3 Local State
- **React Hooks:** `useState`, `useEffect` for component state
- **Form State:** React Hook Form for form management

---

## 5. UI Logic Patterns

### 5.1 Data Fetching Pattern
```typescript
const { data, isLoading, error } = useQuery({
  queryKey: ['orders', filters],
  queryFn: () => getOrders(filters)
});
```

### 5.2 Mutation Pattern
```typescript
const mutation = useMutation({
  mutationFn: createOrder,
  onSuccess: () => {
    queryClient.invalidateQueries(['orders']);
    toast.success('Order created');
  }
});
```

### 5.3 Form Handling Pattern
```typescript
const form = useForm({
  resolver: zodResolver(schema),
  defaultValues: {}
});

const onSubmit = (data) => {
  mutation.mutate(data);
};
```

---

## 6. Important Components

### 6.1 Layout Components
- `MainLayout` - Main app layout with sidebar
- `SettingsLayout` - Settings pages layout

### 6.2 UI Components (shadcn/ui)
- `Button`, `Input`, `Select`, `Dialog`, `Table`, `Card`, etc.

### 6.3 Syncfusion Components
- DataGrid, TreeGrid, Schedule, Kanban, Charts, Diagram, etc.

### 6.4 Custom Components
- `OrderStatusChecklistDisplay` - Checklist display
- `OrderStatusChecklistManager` - Checklist management
- `SchedulerCalendar` - Calendar component
- `InventoryGrid` - Inventory grid

---

## 7. Routing

### 7.1 Route Configuration
- **File:** `App.tsx` and `routes/enhancedRoutes.tsx`
- **Router:** React Router DOM v6
- **Protected Routes:** `ProtectedRoute` and `SettingsProtectedRoute`

### 7.2 Route Structure
```
/ → /dashboard
/orders → Orders list
/orders/:orderId → Order detail
/orders/create → Create order
/scheduler → Calendar
/settings/* → Settings pages
```

---

## 8. API Integration

### 8.1 API Client
- **File:** `api/client.ts`
- **Features:** Base URL, JWT injection, department ID injection

### 8.2 API Modules
- One module per domain (orders.ts, billing.ts, etc.)
- Exported functions for API calls

### 8.3 Hooks
- Custom hooks in `hooks/` directory
- Wrap TanStack Query for domain-specific logic

---

**Document Status:** This page inventory reflects the current production system as of December 2025.

