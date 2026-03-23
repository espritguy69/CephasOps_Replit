# Level 1 — Code & Feature Integrity Audit

**Date:** March 2026  
**Scope:** Controllers, Application services, DTO validation, transaction boundaries, EF hygiene, exception handling, null safety, background job failure safety.  
**Mode:** Analysis and documentation only; no code changes.

---

## 1. Executive summary

Code integrity is **moderate with known hotspots**. Controller–service separation is mostly respected, but **14 controllers inject `ApplicationDbContext`** (Clean Architecture violation). One application service (**DocumentGenerationService**) exceeds 1,500 lines and injects **Infrastructure** (`ApplicationDbContext`). **Application** references **Microsoft.AspNetCore.Http** (IFormFile) in Parser and Settings—acceptable for file handling but a boundary leak. DTO validation is **patchy** (some FluentValidation/DataAnnotations; many endpoints rely on model binding only). Command pipeline has **idempotency and retry** for commands; event store has retry/non-retryable classification. Transaction boundaries and SaveChangesAsync are widespread; explicit **BeginTransaction** is used in sensitive paths (e.g. ledger, billing). Largest regression risks: **OrdersController** (~1,280 lines, 13 dependencies + DbContext), **InventoryController** (~1,182 lines, 9 dependencies + DbContext), **DocumentGenerationService** (~1,388 lines), **BackgroundJobProcessorService** (~978 lines, orchestration sprawl), **OrderService** (19 constructor dependencies, 16 SaveChangesAsync call sites).

---

## 2. Scope audited

| Area | Location | Method |
|------|----------|--------|
| API Controllers | `backend/src/CephasOps.Api/Controllers/` | Grep for ControllerBase, constructor params, ApplicationDbContext |
| Application services | `backend/src/CephasOps.Application/` | Service files, constructor deps, line counts (from discovery + spot checks) |
| DTO / validation | Api + Application DTOs | Grep for Required, IValidator, FluentValidation, DataAnnotations |
| Transactions | Application | Grep for SaveChangesAsync, BeginTransaction, CommitTransaction |
| Background jobs | BackgroundJobProcessorService, JobExecutionWorkerHostedService, EventStoreDispatcherHostedService | Read + background_jobs.md |
| Exception / retry | Commands pipeline, EventStore dispatcher, Program.cs | IdempotencyBehavior, RetryBehavior, EnableRetryOnFailure |

---

## 3. Findings

### 3.1 Controller–service boundary violations

**Controllers injecting ApplicationDbContext (P1 – architecture violation):**

| Controller | Purpose of DbContext usage | Risk |
|------------|---------------------------|------|
| **OrdersController** | Direct queries (e.g. order list/filters) alongside IOrderService | Duplication; bypasses application layer |
| **InventoryController** | Queries for list/export/reports | Same |
| **PayrollController** | Payroll data queries | Same |
| **RatesController** | Rate/GPON data queries | Same |
| **BuildingsController** | Building/tree queries | Same |
| **BackgroundJobsController** | Job list/summary queries | Same |
| **BillingRatecardController** | Ratecard/line queries | Same |
| **DiagnosticsController** | Diagnostic queries | Lower (admin) |
| **EmailsController** | Email/parser data | Same |
| **UsersController** | User list/data | Same |
| **SlaMonitorController** | SLA monitor data | Same |
| **AdminRolesController** | Roles/permissions | Same |
| **BinsController** | Bins (settings) | Same |

**Recommendation:** Move all read/write through application services; remove DbContext from controllers. Refactor is non-trivial for Orders and Inventory (many endpoints).

---

### 3.2 God services / oversized classes

| Service | Approx. lines | Constructor deps | Risk | Notes |
|---------|----------------|------------------|------|-------|
| **DocumentGenerationService** | ~1,388 | 5 (+ Handlebars) | **P1** | Single file; template types, Handlebars, QuestPDF, Carbone; injects ApplicationDbContext. Split candidates: template loader, PDF renderer, placeholder resolution. |
| **BackgroundJobProcessorService** | ~978 | 4 (IServiceProvider, ILogger, options, IWorkerIdentity) | **P1** | Orchestration sprawl: 10+ job types resolved via GetRequiredService per run. |
| **OrderService** | ~3,000+ | 19 | **P1** | God service: order CRUD, status, assignment, materials, payout snapshot, workflow, SLA, automation, notifications, inventory. |
| **WorkflowEngineService** | ~667 | 11 | **P1** | Single point for all transitions; EventStore, Scheduler, guard/side-effect registries. |
| **ParserService** | ~1,800+ | Many | **P2** | Large; Excel/PDF/MSG parsing, session management. |
| **EmailIngestionService** | Large | Many | **P2** | 20 SaveChangesAsync call sites; ingest + workflow transitions. |
| **SchedulerService** | Large | Many | **P2** | 8 SaveChangesAsync; direct _context.Orders; 5× GetRequiredService&lt;IWorkflowEngineService&gt;. |
| **BuildingService** | Large | 8 SaveChanges | **P2** | _context.Orders for count/merge/move. |
| **BillingService** | Large | 3 SaveChanges | **P2** | _context.Orders, _context.Invoices. |

---

### 3.3 Controllers doing business logic / thick controllers

- **OrdersController:** Uses _context for queries in addition to IOrderService; mixes authorization, DTO mapping, and filtering. Risk: **P2** – logic could drift from OrderService.
- **InventoryController:** Same pattern: _context for some reads; 9 dependencies + DbContext. Risk: **P2**.
- **ReportsController:** No DbContext; uses IOrderService, IStockLedgerService, ISchedulerService, IReportExportFormatService only. **Healthy.**

No controller was found to implement **core business rules** (e.g. workflow transition rules, pricing) inline; the main issue is **data access bypass** via DbContext.

---

### 3.4 DTO validation coverage

- **FluentValidation / IValidator:** Used in some areas (e.g. commands, parser); not uniformly applied to all API request DTOs.
- **DataAnnotations ([Required], etc.):** Present in some DTOs; grep shows limited use in Api layer.
- **Risk:** Endpoints that accept JSON/query and bind to DTOs without validators can receive invalid or missing data; server may throw or persist bad state. **P2** – recommend audit of public API DTOs and add validation where missing.

---

### 3.5 Transaction boundaries and EF query safety

- **SaveChangesAsync:** Used in 80+ Application service files; no single pattern (some single-step, some multi-step with explicit transaction).
- **Explicit transactions:** Used in ledger (StockLedgerService), billing (InvoiceSubmissionService), and other multi-step writes. Replay and rebuild use locks and checkpoints.
- **EF query safety:** No systematic use of AsNoTracking for read-only queries in controllers that use _context; risk of accidental change tracking. **P2** – document read-only usage and prefer services that expose read DTOs.

---

### 3.6 Null safety and exception handling

- **Nullable reference types:** Project uses C# nullable context; no full audit of null-dereference risk.
- **Exception handling:** Controllers generally rely on global exception handling/filters; application services throw domain or InvalidOperationException. Event store dispatcher classifies non-retryable and marks poison.
- **Background jobs:** BackgroundJobProcessorService catches per-job exceptions and logs; job marked Failed. EventStoreDispatcherHostedService marks Failed/NonRetryable. **No critical gap identified.**

---

### 3.7 Background job failure safety

- **Legacy BackgroundJob table:** Processed by BackgroundJobProcessorService; job states Queued, Running, Succeeded, Failed; retries with backoff where configured.
- **JobExecution (new pipeline):** JobExecutionWorkerHostedService; IJobExecutorRegistry; status and NextRunAtUtc for retries.
- **Event store:** EventStoreDispatcherHostedService; RetryCount, NextRetryAtUtc; non-retryable classification for poison.
- **Idempotency:** Command pipeline has IdempotencyBehavior (command IdempotencyKey or options); external integrations have ExternalIdempotencyRecord. Not all job types have explicit idempotency keys (e.g. some scheduler-enqueued jobs). **P2** – document which job types are idempotent by design and which need keys.

---

### 3.8 Logging and consistency

- ILogger used across controllers and services. No systematic correlation ID in every path; event store has CorrelationId. **P3.**

---

## 4. Risk classification

| Category | Level | Count / scope |
|----------|-------|----------------|
| Controllers with DbContext | **P1** | 14 controllers |
| Services >1,500 lines or 10+ deps | **P1** | DocumentGenerationService, OrderService, WorkflowEngineService, BackgroundJobProcessorService |
| Application → Infrastructure (DbContext) | **P1** | DocumentGenerationService, many Application services (by design; Document uses it directly) |
| Application → Microsoft.AspNetCore.Http | **P2** | Parser/Settings (IFormFile); acceptable for file uploads |
| Missing DTO validation | **P2** | Many API DTOs not audited for validators |
| Transaction/read-only hygiene | **P2** | Prefer service layer for all reads; document AsNoTracking |
| Job idempotency coverage | **P2** | Some job types not explicitly idempotent |
| Thick controllers (LOC/deps) | **P2** | OrdersController, InventoryController |

---

## 5. Recommended actions

| Priority | Action | Refactor safety |
|----------|--------|-------------------|
| P1 | Remove ApplicationDbContext from all controllers; introduce or use existing query services (e.g. OrderQueryService, InventoryQueryService) for read-only endpoints. | Medium – many endpoints; do per-controller. |
| P1 | Do not add further constructor dependencies to OrderService; consider facade or split (e.g. OrderStatusService, OrderAssignmentService). | High – large blast radius. |
| P1 | Document and limit new job types in BackgroundJobProcessorService; prefer JobExecution + IJobExecutor for new work. | Low – additive. |
| P2 | Audit public API DTOs and add FluentValidation or DataAnnotations where required. | Low. |
| P2 | For Application services that only read, use AsNoTracking where applicable. | Low. |
| P2 | Document idempotency model per job type (scheduler, event store, command pipeline). | Documentation only. |
| P3 | Consider splitting DocumentGenerationService into template resolution, HTML render, and PDF export. | Medium – internal only. |

---

## 6. Quick wins vs deep refactors

- **Quick wins:** Add validation to high-risk API DTOs; document job idempotency; add AsNoTracking to read-only service queries.
- **Deep refactors:** Controller DbContext removal (Orders, Inventory, Payroll, Rates, Buildings, BillingRatecard, BackgroundJobs, etc.); OrderService split; DocumentGenerationService split.

---

## 7. Related artifacts

- [Service dependency graph](../architecture/service_dependency_graph.md)
- [Service sprawl analysis](../architecture/service_sprawl_analysis.md)
- [Architecture integrity audit](../architecture/architecture_integrity_audit.md)
- [Controller sprawl watch](../architecture/controller_sprawl_watch.md)
- [Background jobs](../operations/background_jobs.md)
