# Phase 10: Inventory Report Export Extraction

## A. Audit summary

- **Producer (legacy)**: `InventoryController.ScheduleReportExport` — builds payload (reportType, companyId, departmentId; for UsageSummary: fromDate, toDate, groupBy, materialId?, locationId?; for SerialLifecycle: serialNumbers; optional emailTo, emailAccountId), created BackgroundJob `"InventoryReportExport"`.
- **Executor (legacy)**: `BackgroundJobProcessorService.ProcessInventoryReportExportJobAsync` — used `IStockLedgerService` (GetUsageSummaryExportRowsAsync or GetSerialLifecycleExportRowsAsync), `ICsvService.ExportToCsvBytes`, then optionally `IEmailSendingService.SendEmailAsync` with `InMemoryFormFile` attachment.
- **Scope**: Per request (company/department from caller); optional email delivery.

## B. Convergence model chosen

- **Target: JobExecution** (same orchestration pipeline as email ingest, document generation, etc.).
- **Implementation**: New `InventoryReportExportJobExecutor` (JobType `"inventoryreportexport"`) parses same payload, calls ledger + CSV (same logic as legacy), optionally emails via `IEmailSendingService` and `InMemoryFormFile`. Producer: `InventoryController.ScheduleReportExport` uses `IJobExecutionEnqueuer.EnqueueWithIdAsync("inventoryreportexport", payloadJson, companyId)` and returns `Accepted` with `{ jobId }` (JobExecution id). No BackgroundJob is created.

## C. Producer/execution paths migrated

- **Producer**: `InventoryController.ScheduleReportExport` — enqueues via `EnqueueWithIdAsync("inventoryreportexport", payloadJson, companyId: companyId)` and returns `Accepted` with `jobId`.
- **Execution**: `JobExecutionWorkerHostedService` runs `InventoryReportExportJobExecutor.ExecuteAsync` (ledger rows → CSV → optional email).

## D. Legacy responsibility reduced

- **inventoryreportexport** no longer executed by BackgroundJobProcessorService; replaced by `ProcessInventoryReportExportJobDeprecatedAsync` (drain only). Comment updated.

## E. Idempotency/retry/operational behavior

- **Retry**: JobExecution retry/lease and backoff unchanged. Executor throws on invalid payload or service failure so job can retry.
- **Company**: Job enqueued with `companyId` for visibility/scoping.
- **API**: Caller receives job id for optional status polling (JobExecution table).

## F. Tests added

- None this phase. Executor and controller path can be covered in a follow-up.

## G. Migrations added

- None.

## H. Remaining debt after this phase

- Legacy BackgroundJob still: **eventhandlingasync**, **operationalreplay**, **operationalrebuild**.

## I. Files/docs created or updated

### Created
- `Executors/InventoryReportExportJobExecutor.cs`
- `docs/PHASE10_INVENTORY_REPORT_EXPORT_EXTRACTION.md`

### Updated
- `IJobExecutionEnqueuer` / `JobExecutionEnqueuer`: added `EnqueueWithIdAsync` returning `JobExecution.Id`.
- `InventoryController.cs`: injects `IJobExecutionEnqueuer`; `ScheduleReportExport` uses `EnqueueWithIdAsync` and returns `Accepted` with `{ jobId }`; no BackgroundJob creation.
- `BackgroundJobProcessorService.cs`: inventoryreportexport → `ProcessInventoryReportExportJobDeprecatedAsync` (drain only); removed full `ProcessInventoryReportExportJobAsync`; comment updated.
- `Program.cs`: registered `InventoryReportExportJobExecutor`.

## J. Recommended next phase

- **Phase 11: Event/replay/rebuild convergence** — eventhandlingasync, operationalreplay, operationalrebuild (event handling and operational replay/rebuild jobs).
