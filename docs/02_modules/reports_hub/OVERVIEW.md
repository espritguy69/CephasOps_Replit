# Reports Hub Module

**Purpose:** Single place to search, run, and export key reports (Orders list, Materials list, Stock summary, Ledger, Scheduler utilization). Department-scoped; 403 when user has no access to the requested department.

---

## 1. Scope

- **Hub page** (`/reports`): Search by name/description/tags, filter by category, list report cards. Click a report → runner.
- **Runner page** (`/reports/:reportKey`): Filters from report definition schema, Run button, results table, pagination when paged, Export (CSV where supported).
- **Backend:** In-memory report registry; `GET api/reports/definitions`, `GET api/reports/definitions/{reportKey}`, `POST api/reports/{reportKey}/run`. Run is department-scoped (403 when not allowed).

---

## 2. API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `api/reports/definitions` | GET | List all report definitions (reportKey, name, description, tags, category, parameterSchema, supportsExport). |
| `api/reports/definitions/{reportKey}` | GET | Single report definition by key. |
| `api/reports/{reportKey}/run` | POST | Run report. Body: filter params (departmentId, fromDate, toDate, status, keyword, page, pageSize, etc.). Returns `{ items, totalCount, page?, pageSize? }`. 403 if user has no access to department. |
| `api/reports/orders-list/export` | GET | Export orders list (up to 10k rows). Query: format=csv\|xlsx\|pdf, departmentId, keyword?, status?, fromDate?, toDate?, assignedSiId?. Department scope required. |
| `api/reports/stock-summary/export` | GET | Export stock-summary. Query: format=csv\|xlsx\|pdf (default csv), departmentId, locationId?, materialId?. Department scope required. |
| `api/reports/ledger/export` | GET | Export ledger (up to 10k rows). Query: format=csv\|xlsx\|pdf, departmentId, materialId?, locationId?, orderId?, entryType?, fromDate?, toDate?. Department scope required. |
| `api/reports/materials-list/export` | GET | Export materials list. Query: format=csv\|xlsx\|pdf, departmentId, category?, isActive?. Department scope required. |
| `api/reports/scheduler-utilization/export` | GET | Export scheduler utilization (flattened slots). Query: format=csv\|xlsx\|pdf, departmentId, fromDate, toDate, siId?. Department scope required. |

---

## 3. Starter reports

| reportKey | Name | Backend source | Export |
|-----------|------|----------------|--------|
| orders-list | Orders list | `IOrderService.GetOrdersPagedAsync` (keyword, page, pageSize) | CSV, XLSX, PDF (`api/reports/orders-list/export`, up to 10k rows) |
| materials-list | Materials list | `IInventoryService.GetMaterialsAsync` | CSV, XLSX, PDF (`api/reports/materials-list/export`) |
| stock-summary | Stock summary | `IStockLedgerService.GetStockSummaryAsync` (ByLocation) | CSV, XLSX, PDF (`api/reports/stock-summary/export`) |
| ledger | Ledger report | `IStockLedgerService.GetLedgerAsync` (paged) | CSV, XLSX, PDF (`api/reports/ledger/export`, up to 10k rows) |
| scheduler-utilization | Scheduler utilization | `ISchedulerService.GetCalendarAsync` (flattened slots); dedicated `GET api/scheduler/utilization` | CSV, XLSX, PDF (`api/reports/scheduler-utilization/export`) |

---

## 4. Frontend

- **Routes:** `/reports`, `/reports/:reportKey`.
- **Layout:** PageShell (title, breadcrumbs, actions); designTokens for section headers.
- **Hub:** TanStack Query for definitions; search input; category dropdown; report cards (name, description, tags).
- **Runner:** Filters rendered from `parameterSchema` (string, **guid**, datetime, int, bool). **Guid-type params** use dropdowns where available: departmentId (departments), locationId (stock locations), materialId (materials), assignedSiId/siId (service installers); orderId remains text. Run; DataTable for results; Prev/Next when result has page/pageSize/totalCount; **Export** (format dropdown: CSV, XLSX, PDF) for orders-list, materials-list, stock-summary, ledger, scheduler-utilization when `supportsExport`.
- **Department:** `useDepartment()`; departmentId required for run when report has departmentId in schema; 403 message when access denied.

**Orders list (main page):** Uses `GET api/orders/paged` (keyword, page, pageSize, status, fromDate, toDate) for server-side search and pagination; Previous/Next; Export to Excel (current page).

---

## 5. Related docs

- **Implementation coverage and audit:** `docs/REPORTS_HUB_IMPLEMENTATION_COVERAGE.md` (audit, gaps, implementation summary, verification).
- **Inventory reports** (usage-summary, serial-lifecycle, stock-by-location-history): `02_modules/inventory/OVERVIEW.md`; those remain under `/inventory/reports/*`; Reports Hub adds a unified list and runner for key lists (orders, materials, stock, ledger, scheduler).
