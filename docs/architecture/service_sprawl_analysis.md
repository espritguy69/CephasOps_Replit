# Service Sprawl Analysis

**Date:** March 2026  
**Purpose:** Rank Application services by sprawl risk (P1 Critical, P2 Medium, P3 Healthy) and list split recommendations. Analysis only; no code changes.

**Related:** [service_dependency_graph.md](service_dependency_graph.md) | [service_sprawl_watch.md](service_sprawl_watch.md) | [level1_code_integrity.md](../engineering/level1_code_integrity.md)

---

## 1. Executive summary

**P1 Critical Risk** services are **OrderService**, **WorkflowEngineService**, **BackgroundJobProcessorService**, **SchedulerService**. They have high constructor or runtime dependency count, broad responsibility, or hidden coupling. **P2 Medium Risk** includes **DocumentGenerationService**, **BillingService**, **BuildingService**, **InvoiceSubmissionService**, **ParserService**, **EmailIngestionService**. **P3 Healthy** covers most small, single-purpose services (e.g. TaxCodeService, TeamService, many Settings CRUD services). Refactor should be **proactive** for P1 only where adding a new dependency or job type is imminent; otherwise **deferred** with governance (no new deps without ADR or split plan).

---

## 2. P1 – Critical sprawl risk

| Service | Why risky | Current blast radius | Likely growth failure mode | Split recommendation | Refactor |
|---------|-----------|----------------------|-----------------------------|----------------------|----------|
| **OrderService** | 19 constructor dependencies; ~3,000+ LOC; order CRUD, status, assignment, materials, payout, workflow, SLA, automation, notifications, inventory. | OrdersController, Parser, EmailIngestion, Reports, NotificationDispatchRequestService, AgentModeService, OrderStatusChangedNotificationHandler. Any bug or change in OrderService can affect 7+ call sites. | Adding another integration (e.g. new notification channel or approval step) forces another constructor dep; testing and deployment risk increase. | Extract facade or split: e.g. OrderStatusCoordinator (workflow only), OrderAssignmentService, OrderMaterialService; keep OrderService as thin orchestrator. | Proactive only if new dep planned; else deferred. |
| **WorkflowEngineService** | 11 deps; single point for all entity status transitions; injects Scheduler, EventStore, AuditLog, guard/side-effect registries. | OrderService, OrderStatusesController, SchedulerService (5×), EmailIngestionService, InvoiceSubmissionService, AgentModeService, EmailSendingService. | Every new guard type or side-effect type touches this service; registries grow. | Extract guard/side-effect execution to a dedicated executor service; keep transition resolution and entity status read in WorkflowEngineService. | Deferred; document “no new constructor deps without split.” |
| **BackgroundJobProcessorService** | Single processor for 10+ job types; resolves IEmailIngestionService, IPnlService, IStockLedgerService, IInvoiceSubmissionService, IOperationalRebuildService, IOperationalReplayExecutionService, ISlaEvaluationService, IEventStore, IJobRunRecorderForEvents, EInvoiceProviderFactory, etc. per job via GetRequiredService. | All scheduler-enqueued legacy jobs; no single controller but many domains. | New job type = new GetRequiredService in switch; hidden coupling grows; testing requires full scope. | Migrate remaining job types to JobExecution + IJobExecutor; document each legacy type in background_jobs.md; block new legacy job types. | Proactive: new jobs must use JobExecution. |
| **SchedulerService** | Large codebase; many methods query _context.Orders; resolves IWorkflowEngineService in five code paths (hidden). | SchedulerController, ReportsController, WorkflowEngineService (side effects), NoSchedulingConflictsValidator. | Adding more slot types or workflow-triggered logic keeps Scheduler↔Workflow cycle. | Replace GetRequiredService&lt;IWorkflowEngineService&gt; with constructor-injected IWorkflowEngineService; reduce _context.Orders usage via IOrderService or query service. | Deferred; document in refactor sequence. |

---

## 3. P2 – Medium sprawl risk

| Service | Why risky | Blast radius | Split recommendation | Refactor |
|---------|-----------|--------------|----------------------|----------|
| **DocumentGenerationService** | ~1,388 LOC; single file; Handlebars, QuestPDF, Carbone; injects ApplicationDbContext. | DocumentTemplatesController, DocumentGenerationJobExecutor. | Split: template load + placeholder resolution, HTML render (Handlebars), PDF export (QuestPDF/Carbone), file save. | Deferred. |
| **BillingService** | Queries _context.Orders and _context.Invoices; invoice creation from orders. | BillingController, InvoiceSubmissionsController, BackgroundJobProcessorService (MyInvoisStatusPoll). | Keep Billing correctness; avoid new Order/Inventory access paths. Consider InvoiceFromOrderService for creation path only. | Deferred. |
| **BuildingService** | Queries _context.Orders (OrdersCount, merge, move). | BuildingsController, OrderService, Parser (IBuildingService). | Keep Order access behind clear methods; document merge/move as sensitive. | Deferred. |
| **InvoiceSubmissionService** | Injects IWorkflowEngineService (Billing → Workflow). | Billing flow, MyInvoisStatusPoll job. | Already documented; avoid adding more workflow transitions from Billing. | Deferred. |
| **ParserService** | Large; Excel/PDF/MSG parsing; many SaveChangesAsync. | ParserController, EmailIngestionService, BackgroundJobProcessorService (EmailIngest). | Consider ParseSessionService vs FileParsingService split later. | Deferred. |
| **EmailIngestionService** | 20 SaveChangesAsync; ingest + workflow transitions. | Parser flow, EmailIngest job. | Document transaction boundaries; consider smaller units of work. | Deferred. |

---

## 4. P3 – Healthy

- Most Settings services (OrderTypeService, BuildingTypeService, TaxCodeService, TeamService, etc.): single responsibility, few deps.
- ReportDefinitionService, KpiProfileService, NotificationTemplateService: bounded.
- JobExecutionWorkerHostedService + IJobExecutorRegistry: explicit per-executor registration; no hidden sprawl.
- EventStoreDispatcherHostedService: single responsibility (dispatch events); retry/poison handling in one place.

---

## 5. Services that should eventually be split

| Service | Suggested split | Priority |
|---------|-----------------|----------|
| **OrderService** | OrderStatusCoordinator, OrderAssignmentService, OrderMaterialService (or facade) | P1 |
| **WorkflowEngineService** | Guard/side-effect executor extracted; keep transition engine | P2 |
| **DocumentGenerationService** | Template resolution, Render (Handlebars), PDF export, File save | P2 |
| **BackgroundJobProcessorService** | Migrate to JobExecution; retain legacy processor for backward compatibility only | P1 |
| **ParserService** | Session management vs file parsing (optional) | P3 |

---

## 6. Related artifacts

- [Service dependency graph](service_dependency_graph.md)
- [Service sprawl watch](service_sprawl_watch.md)
- [Level 1 code integrity](../engineering/level1_code_integrity.md)
- [Background jobs](../operations/background_jobs.md)
