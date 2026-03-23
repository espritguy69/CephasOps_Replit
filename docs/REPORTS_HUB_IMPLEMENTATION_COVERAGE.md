# Reports Hub — Implementation Coverage Report (Step 1 — Audit)

**Date:** 2025-02-03  
**Scope:** Single “Reports Hub” with search, run, filters, table results, optional export.  
**Rule:** No code written until this report is produced and gaps are agreed.

---

## Post-implementation status (Feb 2026)

| Requirement | Status | Evidence |
|-------------|--------|----------|
| **Reports Hub** | **DONE** | `/reports` (ReportsHubPage), PageShell, search + category filter, report cards. |
| **Search by name/keywords/tags** | **DONE** | Hub search input; ReportRegistry with tags; filter by category. |
| **Open a report** | **DONE** | `/reports/:reportKey` (ReportRunnerPage), breadcrumbs, schema-driven filters. |
| **Fill filters + run (table results)** | **DONE** | Parameter schema → filter UI; POST run; DataTable; pagination when paged. |
| **Guid-type filter dropdowns** | **DONE** | ReportRunnerPage: departmentId, locationId, materialId, assignedSiId/siId use Select dropdowns (departments, locations, materials, service installers); orderId remains text. |
| **Export** | **DONE** | CSV, XLSX, PDF for materials-list, stock-summary, ledger, scheduler-utilization; `GET api/reports/{report}/export?format=csv|xlsx|pdf`; Export format dropdown + button when `supportsExport`. |
| **Department / RBAC** | **DONE** | DepartmentContext; 403 on run when not allowed. |
| **Orders list (main page)** | **DONE** | Orders list page uses `GET api/orders/paged` (keyword, page, pageSize, filters); server-side search and pagination; Previous/Next. |

See §7–8 and `02_modules/reports_hub/OVERVIEW.md` for implementation details and verification.

---

## 1. Requirement vs status summary (audit snapshot)

| Requirement | Status | Evidence |
|-------------|--------|----------|
| **Reports Hub** (single page listing reports, searchable) | **NOT FOUND** | No route `/reports`; only `/inventory/reports` exists. |
| **Search reports by name/keywords/tags** | **NOT FOUND** | No hub; no report registry with tags/keywords. |
| **Open a report** (navigate to runner) | **PARTIAL** | Inventory reports: card grid → fixed routes (usage, serial-lifecycle, stock-trend). No generic `/reports/:reportKey`. |
| **Fill filters (department, date, status, location, installer, etc.)** | **PARTIAL** | Each inventory report page has its own filters; Orders/Materials/Stock/Ledger/Scheduler have filter params on existing endpoints. No single schema-driven filter UI. |
| **Run report (table results)** | **PARTIAL** | Inventory report endpoints return JSON; some pages show tables. Orders/Materials/Stock/Ledger/Scheduler are list/summary endpoints, not unified “run report” pattern. |
| **Export (CSV/Excel/PDF)** | **PARTIAL** | CSV export exists for: usage-summary, serial-lifecycle (inventory), materials list. No unified “export” flag per report type; no Excel/PDF for these. |
| **Department scoping / RBAC** | **DONE** | DepartmentContext (frontend), IDepartmentAccessService + IDepartmentRequestContext (backend), 403 on unauthorized department. |
| **ProblemDetails + correlationId on errors** | **DONE** | GlobalExceptionHandler, CorrelationIdMiddleware, ApiSmokeTests verify. |

---

## 2. Existing report infrastructure

### 2.1 Backend

- **ReportDefinitionsController**  
  - **Path:** `api/report-definitions` (not `api/reports/definitions`).  
  - **File:** `backend/src/CephasOps.Api/Controllers/ReportDefinitionsController.cs`  
  - **Behaviour:** CRUD for “scheduled report” definitions (company-scoped, companyId query). **Not** a registry of runnable reports for a hub.  
  - **Model:** `ReportDefinition` entity + `ReportDefinitionDto`: Id, CompanyId, Code, Name, Description, Category, Format, Schedule, LastGenerated, IsActive.  
  - **Gaps:** No `reportKey`, no `tags[]`, no parameter schema, no endpoint mapping, no `supportsExport`. Not department-scoped.

- **Inventory report endpoints** (department-scoped, 403 on wrong department):
  - `GET api/inventory/reports/usage-summary` — fromDate, toDate, groupBy, materialId, locationId, departmentId, page, pageSize.  
    **File:** `InventoryController.cs` (reports section).  
  - `GET api/inventory/reports/serial-lifecycle` — serialNumber/serialNumbers, departmentId, page, pageSize.  
  - `GET api/inventory/reports/stock-by-location-history` — fromDate, toDate, snapshotType, materialId, locationId, departmentId, page, pageSize.  
  - `GET api/inventory/reports/usage-summary/export` — CSV.  
  - `GET api/inventory/reports/serial-lifecycle/export` — CSV.  
  - `POST api/inventory/reports/export/schedule` — schedule job (UsageSummary or SerialLifecycle).

- **Stock summary & ledger** (department-scoped):
  - `GET api/inventory/stock-summary` — departmentId, locationId, materialId.  
    **File:** `InventoryController.cs` (GetStockSummary).  
  - `GET api/inventory/ledger` — departmentId, materialId, locationId, orderId, entryType, fromDate, toDate, page, pageSize.  
    **File:** `InventoryController.cs` (GetLedger).

- **Orders list** (department-scoped, 403):
  - `GET api/orders` — status, partnerId, assignedSiId, buildingId, fromDate, toDate, departmentId.  
  - **File:** `OrdersController.cs` (GetOrders).  
  - **Gaps:** No keyword/search; no pagination (returns `List<OrderDto>`).

- **Materials list** (department-scoped):
  - `GET api/inventory/materials` — departmentId, category, search, isActive.  
  - **File:** `InventoryController.cs` (GetMaterials).  
  - **Gaps:** No explicit `isSerialised` filter (MaterialDto has IsSerialised; filter can be added if needed).  
  - `GET api/inventory/materials/export` — CSV (departmentId, category, isActive).

- **Scheduler**
  - **File:** `backend/src/CephasOps.Api/Controllers/SchedulerController.cs`  
  - Endpoints: calendar, slots, **utilization** (flattened slots by date range), si-availability, unassigned-orders, create/update slot, etc.  
  - **Dedicated utilization:** `GET api/scheduler/utilization?fromDate=&toDate=&departmentId=&siId=` returns flattened schedule slots; same data as Reports Hub scheduler-utilization report.

- **Error handling:**  
  `Program.cs` (AddProblemDetails, CorrelationIdMiddleware), `GlobalExceptionHandler.cs` (ProblemDetails + correlationId in extensions).  
  **File:** `backend/src/CephasOps.Api/ExceptionHandling/GlobalExceptionHandler.cs`, `Middleware/CorrelationIdMiddleware.cs`.

### 2.2 Frontend

- **Routes:**  
  - **File:** `frontend/src/App.tsx`  
  - Only report-related routes: `/inventory/reports`, `/inventory/reports/usage`, `/inventory/reports/serial-lifecycle`, `/inventory/reports/stock-trend`.  
  - **No** `/reports` or `/reports/:reportKey`.

- **Inventory reports hub:**  
  - **File:** `frontend/src/pages/inventory/InventoryReportsIndexPage.tsx`  
  - Hardcoded `REPORTS` array (path, label, description, icon). No backend-driven definitions; no search; no tags.  
  - Uses `useDepartment()`; shows “select department” when no department.

- **API / client:**  
  - **File:** `frontend/src/api/client.ts` — apiClient with get/post/put/delete, `getApiBaseUrl()`, `ensureDepartmentParam`, `buildHeaders` (auth + X-Department-Id).  
  - **File:** `frontend/src/api/inventoryReports.ts` — usage-summary, stock-by-location-history, serial-lifecycle + export helpers.  
  - **File:** `frontend/src/api/reportDefinitions.ts` — getReportDefinitions, getReportDefinition, create/update/delete.  
  - **Path mismatch:** Frontend reportDefinitions API uses `/settings/report-definitions` (so full URL `/api/settings/report-definitions`). Backend controller is `api/report-definitions`. These do not match unless proxy or backend adds a “settings” prefix.

- **TanStack Query:** Used in BuildingMergePage, ParserDashboardPage, ParserListingPage, useDocumentTemplates, WarehouseLayoutPage, TasksListPage, SplitterTopologyPage. Pattern exists to reuse.

- **Department:** `DepartmentContext` and `useDepartment()` used in multiple pages; client injects departmentId via params/headers.

- **Global search / command palette:** Not found in frontend.

### 2.3 Database

- **ReportDefinition:** Entity in `CephasOps.Domain.Settings.Entities.ReportDefinition`; EF Core. Used for scheduled report config (Format, Schedule), not for “report registry” with reportKey/tags/parameter schema.

---

## 3. Phase 1 starter reports — backend capability

| Report | Backend capability | Endpoint(s) | Filters today | Export | Gaps |
|--------|--------------------|-------------|----------------|--------|------|
| **1) Orders list** | Exists | `GET api/orders`, `GET api/reports/orders-list/export` | departmentId, status, keyword, fromDate, toDate, assignedSiId, page, pageSize | CSV, XLSX, PDF (up to 10k rows) | — |
| **2) Materials list** | Exists | `GET api/inventory/materials`, `GET api/reports/materials-list/export` | departmentId, category, search, isActive | CSV, XLSX, PDF | Optional isSerialised filter. |
| **3) Stock summary** | Exists | `GET api/inventory/stock-summary`, `GET api/reports/stock-summary/export` | departmentId, locationId, materialId | CSV, XLSX, PDF | — |
| **4) Ledger** | Exists | `GET api/inventory/ledger`, `GET api/reports/ledger/export` | departmentId, materialId, locationId, orderId, entryType, fromDate, toDate, page, pageSize | CSV, XLSX, PDF (up to 10k rows) | — |
| **5) Scheduler utilization** | Exists | `GET api/scheduler/utilization`, `GET api/reports/scheduler-utilization/export` | departmentId, fromDate, toDate, siId | CSV, XLSX, PDF | Dedicated utilization endpoint and report export implemented. |

---

## 4. Gaps and minimal actions

### 4.1 Reports Hub (single place + search)

- **Gap:** No `/reports` page; no central list of “runnable” reports; no search by name/keywords/tags.  
- **Minimal action:**  
  - **Option A:** Add a **report registry** in backend (e.g. in-memory or code-first) returning a list of report definitions (reportKey, name, description, tags, category, parameter schema, supportsExport). Expose e.g. `GET api/reports/definitions` (department-safe: return only reports user is allowed to run).  
  - **Option B:** If reusing existing ReportDefinition entity, extend it (e.g. reportKey, tags, parameter schema, endpoint mapping, supportsExport) and add a dedicated “hub list” endpoint that is department-aware.  
  - Frontend: add routes `/reports` and `/reports/:reportKey`; ReportsHubPage (search + filter by keyword/tags); ReportRunnerPage (filters from schema, run, table, export).

### 4.2 Run report + schema-driven filters

- **Gap:** No single “run report by key” endpoint and no parameter schema driving the hub/runner UI.  
- **Minimal action:**  
  - Either add `GET/POST api/reports/{reportKey}` that dispatches to existing services (orders, inventory, scheduler) with a single contract, **or** keep using existing endpoints and have the report registry map reportKey → endpoint + parameter schema.  
  - Frontend ReportRunnerPage: render filters from definition schema; call the appropriate backend (existing or unified); show table; handle 403.

### 4.3 Export

- **Gap:** Not every “report” has export; no generic “export this report” flag in one place.  
- **Minimal action:** In report registry/definition, set `supportsExport` and optionally export format (CSV/Excel/PDF). Use existing CSV where available; add Excel/PDF only where already supported or required.

### 4.4 Orders list report

- **Gap:** No keyword search; no pagination.  
- **Minimal action:** Add optional `keyword` (or `search`) and `page`, `pageSize` to `GET api/orders`; return paged result (e.g. items + totalCount). Optional: `appointmentFrom`, `appointmentTo` for appointment date range.

### 4.5 Scheduler utilization report

- **Status (addressed):** Dedicated `GET api/scheduler/utilization` (fromDate, toDate, departmentId, siId) returns flattened schedule slots. Reports Hub scheduler-utilization supports export (CSV, XLSX, PDF) via `GET api/reports/scheduler-utilization/export`.

### 4.6 ReportDefinitions frontend path

- **Gap:** Frontend calls `/api/settings/report-definitions`, backend is `/api/report-definitions`.  
- **Minimal action:** Align: either frontend uses `report-definitions` (no “settings”) or backend adds a route/prefix for `settings/report-definitions`.

---

## 5. What to reuse (no new framework)

- **Department scoping:** Keep using DepartmentContext, X-Department-Id, IDepartmentAccessService, ResolveDepartmentScopeAsync; 403 on unauthorized department.  
- **Inventory report pattern:** Same filter + JSON + CSV export pattern; reuse for “report” wrapper if adding unified endpoint.  
- **ApiResponse envelope and error handling:** Keep ProblemDetails + correlationId.  
- **Frontend:** ApiClient, TanStack Query, existing inventory report pages as reference for filter + table + export.  
- **Existing endpoints:** Prefer wrapping or mapping to existing orders, inventory, ledger, scheduler endpoints rather than duplicating logic.

---

## 6. Implementation coverage summary

- **Reports Hub + searchable list:** NOT FOUND — need new hub page + report list (from registry or extended ReportDefinition).  
- **Open report + run results:** PARTIAL — inventory has per-report pages and endpoints; need generic runner and optional unified run endpoint.  
- **Export:** DONE for Reports Hub — materials-list, stock-summary, ledger, scheduler-utilization support CSV, XLSX, PDF via `api/reports/{report}/export?format=csv|xlsx|pdf`. Inventory usage-summary, serial-lifecycle remain CSV.  
- **RBAC / department:** DONE — reuse existing patterns.  
- **Errors (ProblemDetails + correlationId):** DONE.

**Next step:** Proceed to Step 2 (target UX) and Step 3 (backend contract: report registry or extend ReportDefinitions) using this report; then implement (Steps 4–5) and verify (Step 6).

---

## 7. Implementation summary (post Step 5)

**Added (minimal):**

- **Backend**
  - `Api/DTOs/ReportsHubDtos.cs`: `ReportDefinitionHubDto`, `ReportParameterSchemaDto`, `RunReportResultDto`, `RunReportRequestDto`.
  - `Api/Reports/ReportRegistry.cs`: In-memory registry of 5 reports (orders-list, materials-list, stock-summary, ledger, scheduler-utilization) with reportKey, name, description, tags, category, parameterSchema, supportsExport.
  - `Api/Controllers/ReportsController.cs`: `GET api/reports/definitions`, `GET api/reports/definitions/{reportKey}`, `POST api/reports/{reportKey}/run`. Run dispatches to existing `IOrderService`, `IInventoryService`, `IStockLedgerService`, `ISchedulerService`; department resolved via `IDepartmentAccessService` (403 when not allowed).
- **Frontend**
  - `types/reports.ts`: Types for hub definitions and run request/result.
  - `api/reports.ts`: `getReportDefinitions`, `getReportDefinition`, `runReport`, `exportMaterialsReport`, `isForbiddenError`.
  - `pages/reports/ReportsHubPage.tsx`: Search bar, category filter, report cards; fetches definitions via TanStack Query; click → `/reports/:reportKey`.
  - `pages/reports/ReportRunnerPage.tsx`: Filters from definition `parameterSchema`, Run button, results table (DataTable), Export CSV for materials-list; uses `useDepartment()` and shows 403 message when access denied.
  - Routes: `/reports`, `/reports/:reportKey` in `App.tsx`; sidebar link “Reports Hub” → `/reports`.

**Reused:** DepartmentContext, apiClient (X-Department-Id), existing services, ApiResponse, ProblemDetails/correlationId, TanStack Query, Card/Button/DataTable/Skeleton.

**Later enhancement (Feb 2026):** Orders list report now supports keyword search and pagination: `IOrderService.GetOrdersPagedAsync` (keyword, page, pageSize), `OrderListResultDto`; ReportRegistry orders-list parameter schema includes page, pageSize; ReportsController RunOrdersListAsync uses GetOrdersPagedAsync.

---

## 8. Step 6 — Verification

**Backend integration tests** (`backend/tests/CephasOps.Api.Tests/Integration/ReportsIntegrationTests.cs`):

- `GetDefinitions_Returns200_AndListOfReports`: GET api/reports/definitions returns 200, Success true, Data array with at least one report (reportKey, name).
- `RunReport_UserInDeptA_RequestingDeptB_Returns403`: User in Dept A only; POST api/reports/orders-list/run with departmentId=Dept B returns 403 and message "do not have access to this department".
- `RunReport_UserInDeptA_RequestingDeptA_Returns200_AndNonErrorSchema`: Same seed; POST run with departmentId=Dept A returns 200, Success true, Data with items (array) and totalCount.

**Frontend smoke tests** (`frontend/e2e/smoke.spec.ts`):

- `/reports – not 404, not blank, search visible`: /reports loads; heading "Reports Hub" or search placeholder or main visible.
- `/reports/orders-list – not 404, not blank`: /reports/orders-list loads; heading "Orders list" or "Filters"/"Run report"/"select a department"/"report not found" or main visible.

**How to run verification:**

- Backend: `dotnet test --filter "ReportsIntegration"` from `backend/tests/CephasOps.Api.Tests`.
- Frontend E2E: `npx playwright test smoke.spec.ts --grep "/reports"` (or run full smoke suite) with app and API running.
