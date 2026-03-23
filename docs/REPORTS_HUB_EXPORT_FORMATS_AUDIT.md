# Reports Hub Export Formats — Implementation Coverage Report (Step 1 Audit)

**Date:** 2026-02-03  
**Scope:** Upgrade Reports Hub export to support Excel (.xlsx) and PDF (in addition to CSV). Audit only; no code changes in this step.

---

## 1. Frontend: Reports Hub page and export trigger

### 1.1 Where export is triggered

- **Page:** Export is triggered from the **Report Runner** page, not the Hub listing.
- **File:** `frontend/src/pages/reports/ReportRunnerPage.tsx`
- **Trigger:** Single button "Export CSV" (lines 294–298). `handleExport` (lines 108–136) is called on click.
- **Hub page:** `frontend/src/pages/reports/ReportsHubPage.tsx` only links to runner and mentions "export when available"; it does not perform export.

### 1.2 API client functions used for export

- **File:** `frontend/src/api/reports.ts`
- **Functions:**
  - `exportMaterialsReport(params)` — calls `GET ${base}/inventory/materials/export?…` (InventoryController), then `downloadCsvFromResponse`.
  - `exportStockSummaryReport(params)` — calls `GET ${getApiBaseUrl()}/reports/stock-summary/export?…`, then `downloadCsvFromResponse`.
  - `exportLedgerReport(params)` — calls `GET ${getApiBaseUrl()}/reports/ledger/export?…`, then `downloadCsvFromResponse`.
- **Helper:** `downloadCsvFromResponse(res, defaultName)` (lines 36–45): checks `res.ok`, reads `res.blob()`, parses filename from `Content-Disposition`, creates `<a download>` and triggers click. **All export is backend file download** (no client-side CSV generation).

### 1.3 How CSV is produced

- **Backend file download only.** Frontend uses `fetch(…export URL)` with `Authorization: Bearer ${token}`, then triggers browser download from the response blob. Filename is taken from `Content-Disposition` when present, else a default (e.g. `stock-summary-YYYY-MM-DD.csv`).

---

## 2. Backend: Report export endpoints and CSV production

### 2.1 Reports Hub export endpoints (ReportsController)

- **File:** `backend/src/CephasOps.Api/Controllers/ReportsController.cs`
- **Routes:**
  - `GET api/reports/stock-summary/export` — action `ExportStockSummary` (lines 141–176).  
    Query: `departmentId`, `locationId`, `materialId`.  
    Resolves department via `ResolveDepartmentScopeAsync`; 403 if no access.  
    Data: `_stockLedgerService.GetStockSummaryAsync(…)` → `summary.ByLocation` (`List<StockByLocationDto>`).  
    Output: `_csvService.ExportToCsvBytes(rows)` → `File(csvBytes, "text/csv", fileName)`.
  - `GET api/reports/ledger/export` — action `ExportLedger` (lines 179–233).  
    Query: `departmentId`, `materialId`, `locationId`, `orderId`, `entryType`, `fromDate`, `toDate`.  
    Same department resolution and 403 behaviour.  
    Data: `_stockLedgerService.GetLedgerAsync(filter, …)` with `PageSize = 10_000` → `result.Items` (`List<LedgerEntryDto>`).  
    Output: `_csvService.ExportToCsvBytes(result.Items)` → `File(csvBytes, "text/csv", fileName)`.

### 2.2 Materials export (used by Reports Hub for materials-list)

- **File:** `backend/src/CephasOps.Api/Controllers/InventoryController.cs`
- **Route:** `GET api/inventory/materials/export` — action `ExportMaterials` (lines 951–999).  
  Query: `departmentId`, `category`, `isActive`.  
  Department resolved via `ResolveDepartmentScopeAsync`; 403 if no access.  
  Data: `_inventoryService.GetMaterialsAsync(…)` → mapped to `MaterialCsvDto` list.  
  Output: `_csvService.ExportToCsvBytes(csvData)` → `File(csvBytes, "text/csv", fileName)`.

### 2.3 Service that generates report rows (reuse for CSV/Excel/PDF)

- **Stock-summary:** `IStockLedgerService.GetStockSummaryAsync(Guid? companyId, Guid? departmentId, Guid? locationId, Guid? materialId, CancellationToken)`  
  Returns `StockSummaryResultDto`; export uses `.ByLocation` (`List<StockByLocationDto>`).  
  DTOs: `backend/src/CephasOps.Application/Inventory/DTOs/LedgerDtos.cs` (StockByLocationDto, StockSummaryResultDto).
- **Ledger:** Same service `GetLedgerAsync(LedgerFilterDto, Guid? companyId, Guid? departmentId, CancellationToken)`  
  Returns `LedgerListResultDto` with `.Items` (`List<LedgerEntryDto>`).  
  DTOs: same file (LedgerEntryDto, LedgerListResultDto).
- **Materials:** `IInventoryService.GetMaterialsAsync(…)` returns material DTOs; controller maps to `MaterialCsvDto` for CSV.  
  Materials export is in InventoryController; extending Reports Hub to Excel/PDF for materials-list can either call the same data path or a shared “report rows” helper.

**Conclusion:** All formats should share the same data layer: call the existing services (GetStockSummaryAsync, GetLedgerAsync, GetMaterialsAsync) once and pass the resulting rows to CSV, Excel, and PDF writers. No duplicate query logic.

---

## 3. Existing PDF / Excel infrastructure in repo

### 3.1 Excel (.xlsx)

- **Library:** **Syncfusion.XlsIO** (ExcelEngine).  
  References: `CephasOps.Application` and `CephasOps.Infrastructure` (Syncfusion.XlsIO.Net.Core 31.1.17, Syncfusion.XlsIORenderer.Net.Core 31.1.17).
- **Usage:**  
  - `backend/src/CephasOps.Application/Departments/Services/DepartmentDeploymentService.cs` — `ExportDepartmentDataAsync` (lines 358–382): `new ExcelEngine()`, `application.Workbooks.Create(1)`, fill sheets, `workbook.SaveAs(stream)`, return `stream.ToArray()`.  
  - Company export uses same pattern (CompanyDeploymentService).
- **No ClosedXML, EPPlus, or NPOI** found.

### 3.2 PDF

- **Libraries in use:**
  1. **Syncfusion.Pdf** (Syncfusion.Pdf.Net.Core 31.1.17) — e.g. `backend/src/CephasOps.Application/Billing/Services/BillingService.cs`: `GenerateInvoicePdfAsync` uses `PdfDocument`, `PdfStandardFont`, `page.Graphics.DrawString`, etc.
  2. **QuestPDF** (QuestPDF 2025.7.4 in CephasOps.Application) — e.g. `DocumentGenerationService` (templates), `ExcelDataReaderToPdfConverter` (table-style PDF from data).
- **No iText, PdfSharp, or DinkToPdf** in current codebase (PdfSharpCore removed per comment).

### 3.3 Other

- **ICsvService:** `backend/src/CephasOps.Application/Common/Services/CsvService.cs` — CsvHelper-based `ExportToCsvBytes<T>(IEnumerable<T>)`. Used by ReportsController and InventoryController for CSV.
- **IDocumentGenerationService:** Template-based document generation (Handlebars + QuestPDF); not currently used for tabular report export.
- **FileResult / Content-Disposition:** Controllers use `File(byte[], contentType, fileDownloadName)`. ASP.NET Core sets `Content-Disposition: attachment; filename="..."` when `fileDownloadName` is provided.

---

## 4. Current export path (UI → API → service → file)

| Step | Component | Detail |
|------|-----------|--------|
| 1 | UI | ReportRunnerPage: user clicks "Export CSV" → `handleExport()` |
| 2 | API client | `exportStockSummaryReport` / `exportLedgerReport` / `exportMaterialsReport` build query string, `fetch(exportUrl)` with Bearer token |
| 3 | Backend route | ReportsController: `GET api/reports/stock-summary/export`, `GET api/reports/ledger/export`; or InventoryController: `GET api/inventory/materials/export` |
| 4 | Auth & RBAC | Resolve department via `ResolveDepartmentScopeAsync`; 403 if unauthorized |
| 5 | Data | Stock: `_stockLedgerService.GetStockSummaryAsync` → `.ByLocation`. Ledger: `_stockLedgerService.GetLedgerAsync` → `.Items`. Materials: `_inventoryService.GetMaterialsAsync` → map to MaterialCsvDto |
| 6 | Format | `_csvService.ExportToCsvBytes(rows)` |
| 7 | Response | `File(csvBytes, "text/csv", fileName)` with filename like `stock-summary-YYYY-MM-dd.csv` |

---

## 5. What can be reused for Excel and PDF

- **Data:** Reuse existing service calls and DTOs. No new queries; optionally introduce a small internal helper that returns “report rows” (e.g. `List<StockByLocationDto>`, `List<LedgerEntryDto>`, or a shared row type) so CSV/Excel/PDF all consume the same list.
- **Excel:** Reuse **Syncfusion ExcelEngine** (same as DepartmentDeploymentService): create workbook, one worksheet, write header row from DTO properties, write data rows, `SaveAs(stream)`, return bytes. No new NuGet package.
- **PDF:** Either **Syncfusion.Pdf** (like BillingService) or **QuestPDF** (like ExcelDataReaderToPdfConverter). QuestPDF is already used for table-style output and is a good fit for a simple title + timestamp + filters summary + table with pagination.
- **RBAC:** Keep current pattern: same department resolution and 403 behaviour for any new format (format is just another query parameter or route variant).
- **Frontend:** Reuse `downloadCsvFromResponse`-style flow: `fetch` with `format=csv|xlsx|pdf`, blob response, filename from `Content-Disposition`, trigger download. Generalise the helper to handle any file type (e.g. `downloadFileFromResponse(res, defaultFileName)`).

---

## 6. Minimal changes required (summary)

| Area | Change |
|------|--------|
| **API contract** | Extend export endpoints with `format` query (e.g. `format=csv|xlsx|pdf`). Default `csv` to keep current behaviour. Same filters as today. Response: file download with correct Content-Type and Content-Disposition. |
| **Backend – data** | No duplication: keep calling GetStockSummaryAsync / GetLedgerAsync / GetMaterialsAsync. Optionally extract a private method that returns (reportName, rows, columns) for shared use by CSV/Excel/PDF. |
| **Backend – CSV** | Unchanged: continue using `_csvService.ExportToCsvBytes(rows)` when `format=csv` or default. |
| **Backend – Excel** | Add Syncfusion ExcelEngine usage in ReportsController (or a dedicated report export service in Application): build one sheet, header + rows, return `File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName)`. Same for materials if materials-list is included in multi-format export. |
| **Backend – PDF** | Add simple table PDF (QuestPDF or Syncfusion.Pdf): title (report name), timestamp, optional filter summary, table of rows. Return `File(pdfBytes, "application/pdf", fileName)`. |
| **Frontend** | Add format choice (dropdown or split button: CSV / Excel / PDF). Export function calls same URL with `format=xlsx` or `format=pdf`. Use a single download helper for blob + filename. No .css/.md/.js changes; TS/TSX only. |
| **Materials** | Materials export today lives under InventoryController. Options: (a) add `format` to `GET api/inventory/materials/export` and implement xlsx/pdf there, or (b) add `GET api/reports/materials-list/export?format=…` in ReportsController that uses same data and RBAC. (b) keeps all Reports Hub exports under one controller and consistent with stock-summary/ledger. |

---

## 7. Exact file paths and function names (reference)

| Purpose | File path | Function / member |
|--------|-----------|-------------------|
| Export button & handler | `frontend/src/pages/reports/ReportRunnerPage.tsx` | `handleExport`, button "Export CSV" (lines 294–298) |
| Export API client | `frontend/src/api/reports.ts` | `exportMaterialsReport`, `exportStockSummaryReport`, `exportLedgerReport`, `downloadCsvFromResponse` |
| Stock-summary export | `backend/src/CephasOps.Api/Controllers/ReportsController.cs` | `ExportStockSummary` |
| Ledger export | `backend/src/CephasOps.Api/Controllers/ReportsController.cs` | `ExportLedger` |
| Materials export | `backend/src/CephasOps.Api/Controllers/InventoryController.cs` | `ExportMaterials` |
| CSV generation | `backend/src/CephasOps.Application/Common/Services/CsvService.cs` | `ExportToCsvBytes<T>` |
| Stock summary data | `backend/.../Inventory/Services/IStockLedgerService.cs` | `GetStockSummaryAsync` |
| Ledger data | Same | `GetLedgerAsync` |
| Materials data | `backend/.../Inventory/Services/IInventoryService.cs` | `GetMaterialsAsync` |
| Excel (existing) | `backend/.../Departments/Services/DepartmentDeploymentService.cs` | `ExportDepartmentDataAsync`, uses `ExcelEngine`, `Workbooks.Create`, `SaveAs` |
| PDF (existing) | `backend/.../Billing/Services/BillingService.cs` | `GenerateInvoicePdfAsync` (Syncfusion.Pdf); `backend/.../Parser/Services/Converters/ExcelDataReaderToPdfConverter.cs` (QuestPDF) |
| Report definitions | `backend/src/CephasOps.Api/Reports/ReportRegistry.cs` | `GetAll()`, `GetByKey()`; supportsExport = true for materials-list, stock-summary, ledger |
| Department RBAC | `backend/.../Controllers/ReportsController.cs` | `ResolveDepartmentScopeAsync` via `_departmentAccessService.ResolveDepartmentScopeAsync` |

---

## 8. API contract summary (current → proposed)

- **Current:**  
  - `GET api/reports/stock-summary/export?departmentId=&locationId=&materialId=` → CSV.  
  - `GET api/reports/ledger/export?departmentId=&...&fromDate=&toDate=` → CSV.  
  - `GET api/inventory/materials/export?departmentId=&category=&isActive=` → CSV (used by Hub for materials-list).
- **Proposed (to implement in Step 2):**  
  - Same routes, add optional `format=csv|xlsx|pdf` (default `csv`).  
  - Response: file download; Content-Type `text/csv` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | `application/pdf`; Content-Disposition with filename `{reportKey}-{date}.{csv|xlsx|pdf}`.  
  - RBAC unchanged: department-scoped; 403 if user has no access to the resolved department.

---

---

## 9. Step 2 implementation summary (post-implementation)

### Files changed

**Backend**
- `backend/src/CephasOps.Application/Common/Services/IReportExportFormatService.cs` (new)
- `backend/src/CephasOps.Application/Common/Services/ReportExportFormatService.cs` (new) — Syncfusion XlsIO + QuestPDF
- `backend/src/CephasOps.Api/Program.cs` — register `IReportExportFormatService`
- `backend/src/CephasOps.Api/Controllers/ReportsController.cs` — inject `IReportExportFormatService`; add `format` query to stock-summary/export and ledger/export; add `GET api/reports/materials-list/export?format=...`; CSV/Excel/PDF branching
- `backend/tests/CephasOps.Api.Tests/Integration/ReportsIntegrationTests.cs` — tests for CSV/XLSX/PDF content-types and 403 on wrong department

**Frontend (TS/TSX only)**
- `frontend/src/api/reports.ts` — `ExportFormat` type; `downloadFileFromResponse`; `format` param and reports URLs for materials-list, stock-summary, ledger
- `frontend/src/pages/reports/ReportRunnerPage.tsx` — `exportFormat` state; format Select (CSV / Excel / PDF); pass `format` into export calls

### API contract (final)

| Route | Method | Query params | Response |
|-------|--------|--------------|----------|
| `api/reports/stock-summary/export` | GET | `format=csv\|xlsx\|pdf` (default csv), `departmentId`, `locationId?`, `materialId?` | File: text/csv, application/vnd...spreadsheetml.sheet, or application/pdf; filename `stock-summary-YYYY-MM-dd.{csv\|xlsx\|pdf}` |
| `api/reports/ledger/export` | GET | `format=csv\|xlsx\|pdf` (default csv), `departmentId`, filters... | File: same; filename `ledger-YYYY-MM-dd.{csv\|xlsx\|pdf}` |
| `api/reports/materials-list/export` | GET | `format=csv\|xlsx\|pdf` (default csv), `departmentId`, `category?`, `isActive?` | File: same; filename `materials-YYYY-MM-dd.{csv\|xlsx\|pdf}` |

Department RBAC unchanged: 403 when user has no access to the resolved department.

### Manual verification

1. **Reports Hub:** Open a report with export (e.g. Stock summary, Ledger, Materials list). Select CSV / Excel / PDF from the dropdown, click Export. CSV opens as text/downloads; Excel opens in Excel; PDF opens in viewer. Filename includes report key and date.
2. **RBAC:** As a user in Dept A, request export with `departmentId` of Dept B (e.g. change department in header or call API with Dept B id). Expect 403.

---

**End of Step 1 audit and Step 2 implementation.**
