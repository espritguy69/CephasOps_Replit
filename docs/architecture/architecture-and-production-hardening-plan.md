# CephasOps — Architecture Clean-Up & Production Hardening Plan

**Scope:** Structural and operational organisation only. No new features. No business-logic refactors. No breaking changes before production.

---

## PART 1 — Architecture Audit

### 1.1 Backend Folder Layering

| Layer | Location | Role |
|-------|----------|------|
| **Api** | `backend/src/CephasOps.Api` | Host, controllers, middleware, DTOs, Swagger, Serilog bootstrap |
| **Application** | `backend/src/CephasOps.Application` | Services, DTOs, validators, interfaces; feature folders (Orders, Parser, Billing, Workflow, etc.) |
| **Domain** | `backend/src/CephasOps.Domain` | Entities, enums, domain interfaces (e.g. `IEInvoiceProvider`, `IGlobalSettingsReader`) |
| **Infrastructure** | `backend/src/CephasOps.Infrastructure` | Persistence (EF Core, Configurations, Migrations, Seeders), external services (MyInvois, Twilio, Carbone, etc.) |

**Findings:**

- **Clean separation:** Domain has no references to Application or Infrastructure. Application references Domain. Infrastructure references Application/Domain. Api references Application, Infrastructure, and Domain.
- **Minor layering note:** Api references Infrastructure directly (typical for DI composition in the host). Acceptable; no change required for production.
- **Cross-cutting:** Settings/Integration (MyInvois, SMS, WhatsApp, Carbone, Mail) live in Application (Settings services) and Infrastructure (providers). MyInvois credentials are stored in **GlobalSettings (DB)**, not appsettings—good for multi-tenant; ensure production secrets are not in appsettings.

### 1.2 Controllers Sprawl

- **Count:** 91 controller files, 92 controller classes (one file: `OrderStatusChecklistController.cs` exposes `OrderStatusChecklistController` and `OrderChecklistAnswersController`).
- **Location:** All under `CephasOps.Api/Controllers/` in a **single flat folder**; namespace `CephasOps.Api.Controllers`.
- **What is messy:**
  - No subfolders or route prefixes by module; discovery and ownership are harder as the API grows.
  - Logical groups exist only by naming convention: Auth/Admin/Users; Billing/Payments/InvoiceSubmissions; Parser/Emails/EmailAccounts/EmailRules; Workflow/BackgroundJobs/Scheduler; Notifications/Messaging/SMS/WhatsApp; Orders/OrderStatuses/OrderTypes; Inventory/Warehouses; Buildings/Infrastructure/Splitters; Settings (Global, Integration, DocumentTemplates); Reports.
- **What should be grouped:** Controllers by bounded context (see Part 2) for clearer ownership and future route/module isolation.
- **What should be separated:** Diagnostic/test endpoints (e.g. `TestsController`, `DiagnosticsController`) from production API surface; consider auth or environment-based access.
- **What must NOT be touched before prod:** Controller method signatures and route templates. Any reorganisation must preserve existing routes (e.g. `api/admin`, `api/orders`, etc.) so clients and frontends do not break.

### 1.3 Service Responsibility Boundaries

- **Application** is already organised by feature folders: Admin, Auth, Billing, Buildings, Companies, Departments, Files, Inventory, Notifications, Orders, Parser, Payroll, Pnl, Rates, RMA, Scheduler, Settings, SIApp, Tasks, Testing, Workflow, Gpon, etc.
- **Findings:**
  - **Clear boundaries:** Parser (email ingestion, rules, templates, PDF/Excel parsing), Billing (invoicing, payments, supplier invoices, ratecards), Workflow (engine, definitions, background job processor and schedulers), Inventory (stock ledger, movements, report export) are well-scoped.
  - **Settings is broad:** GlobalSettings, DocumentTemplate, ApprovalWorkflow, AutomationRule, SlaProfile, EscalationRule, TimeSlot, Warehouse, MaterialTemplate, KpiProfile, Brand, IntegrationSettings, DocumentGenerationService, TeamService. Acceptable for now; can be split later into Settings.Core vs Settings.Integrations if needed.
  - **Circular dependency already broken:** ParserService ↔ EmailIngestionService via `Lazy<IEmailIngestionService>` in Program.cs. Do not reintroduce direct circular references.
- **What must NOT be touched:** Service interfaces and their implementations that are used by controllers and background jobs; only organisation (e.g. moving files into module subfolders) if done without changing namespaces or contracts.

### 1.4 Cross-Module Dependencies

- **Api → Application, Infrastructure, Domain:** All application and infrastructure services are registered in `Program.cs` (single composition root). No circular project references.
- **Workflow → Parser, Pnl, Billing, Inventory, Notifications, Settings, Common:** Background job processor and schedulers correctly use scoped services via `IServiceProvider.CreateScope()`.
- **MyInvois:** Domain defines `IEInvoiceProvider`; Infrastructure has `MyInvoisApiProvider` and `NullEInvoiceProvider`. Registration in Program uses `NullEInvoiceProvider` by default; switching to MyInvois is configuration-driven (GlobalSettings). No change needed for structure.
- **Syncfusion:** Used in Application (Parser Excel/PDF, Billing PDF) and licensed in Program. License key must not be in source control (see Part 5).

### 1.5 Background Job Structure

| Component | Type | Interval | Purpose |
|-----------|------|----------|---------|
| **BackgroundJobProcessorService** | Worker | 30 s poll | Dequeues and executes jobs (EmailIngest, PnlRebuild, NotificationSend, etc.). |
| **EmailIngestionSchedulerService** | Scheduler | 30 s | Creates EmailIngest jobs per active email account (based on `EmailAccount.PollIntervalSec`). |
| **PnlRebuildSchedulerService** | Scheduler | 24 h | Enqueues one PnlRebuild job per day when none pending. |
| **StockSnapshotSchedulerService** | Scheduler | 6 h | Enqueues populatestockbylocationsnapshots. |
| **LedgerReconciliationSchedulerService** | Scheduler | 12 h | Enqueues reconcileledgerbalancecache. |
| **EmailCleanupService** | Worker | Config (Mail.CleanupJob.IntervalMinutes) | Mail viewer TTL cleanup. |

**Job types executed by BackgroundJobProcessorService:**

- `emailingest` — Email ingestion (critical for order flow).
- `pnlrebuild` — P&L aggregation (reporting).
- `notificationsend` — Send notification.
- `notificationretention` — Notification cleanup.
- `documentgeneration` — Document generation.
- `myinvoisstatuspoll` — MyInvois submission status polling.
- `inventoryreportexport` — Scheduled inventory report export.
- `reconcileledgerbalancecache` — Ledger balance cache reconciliation.
- `populatestockbylocationsnapshots` — Stock-by-location snapshots.

**Findings:**

- Single processor with a string-based switch is clear but could be formalised (e.g. job type registry) later—not required for production.
- Scheduler intervals are hardcoded (30s, 6h, 12h, 24h). For production, consider moving to config for tuning without redeploy.
- No dedicated “health” for each job type beyond what `BackgroundJobsController.GetHealthStatus()` and `AdminController.GetHealth` expose (email parser health, DB). A structured Background Job Health Dashboard is proposed in Part 3.

---

## PART 2 — Folder & Module Organisation Plan

### 2.1 Proposed Logical Grouping (Controllers and Application)

**Goal:** Group by bounded context for ownership, discoverability, and future optional route/module isolation—without changing existing HTTP routes or behaviour.

**Api — Controllers (suggested subfolders under `Controllers/`):**

```
Api/
  Controllers/
    Admin/           → AdminController, DiagnosticsController (optional)
    Auth/             → AuthController, UsersController
    Billing/          → BillingController, BillingRatecardController, PaymentsController,
                        InvoiceSubmissionsController, SupplierInvoicesController, PaymentTermsController, TaxCodesController
    Parser/           → ParserController, ParserTemplatesController, EmailAccountsController, EmailRulesController,
                        EmailsController, EmailTemplatesController, EmailSendingController, VipEmailsController, VipGroupsController
    Workflow/         → WorkflowController, WorkflowDefinitionsController, SchedulerController,
                        BackgroundJobsController, GuardConditionDefinitionsController, SideEffectDefinitionsController
    Orders/           → OrdersController, OrderStatusesController, OrderTypesController, OrderCategoriesController,
                        OrderStatusChecklistController (and OrderChecklistAnswersController)
    Inventory/        → InventoryController, WarehousesController, BinsController, MaterialCategoriesController,
                        MaterialTemplatesController
    Buildings/       → BuildingsController, BuildingTypesController, BuildingDefaultMaterialsController,
                        InfrastructureController, SplittersController, SplitterTypesController, InstallationMethodsController,
                        InstallationTypesController
    Notifications/    → NotificationsController, NotificationTemplatesController, MessagingController,
                        SmsController, SmsGatewayController, SmsTemplatesController, WhatsAppController, WhatsAppTemplatesController
    Settings/         → GlobalSettingsController, IntegrationSettingsController, DocumentTemplatesController,
                        ApprovalWorkflowsController, AutomationRulesController, SlaProfilesController, EscalationRulesController,
                        TimeSlotsController, KpiProfilesController, BusinessHoursController
    Companies/        → CompaniesController, PartnersController, PartnerGroupsController, VerticalsController, VendorsController
    Reports/          → ReportsController, ReportDefinitionsController, PnlController, PnlTypesController
    Assets/            → AssetsController, AssetTypesController
    ServiceInstallers/ → ServiceInstallersController, ServicePlansController, SkillsController
    Scheduler/         → (SchedulerController already in Workflow) or keep under Workflow
    Documents/         → DocumentsController, FilesController
    Other/             → DepartmentsController, TasksController, RMAController, PayrollController, RatesController,
                        ExcelToPdfController, TestsController, SiAppController
```

**Application — already module-based:**

- Keep existing feature folders: `Orders/`, `Parser/`, `Billing/`, `Workflow/`, `Inventory/`, `Settings/`, `Notifications/`, `Companies/`, `Pnl/`, `Admin/`, `Auth/`, etc.
- Optional: add an `Application/Modules/` mirror (e.g. `Application/Modules/Orders/`, `Application/Modules/Parser/`) only if you want a second grouping; current structure is already acceptable.

### 2.2 Why Module-Based Grouping Is Safer Long Term

- **Ownership:** Teams or maintainers can own a folder (e.g. Parser, Billing) without scanning 90+ flat files.
- **Scaling:** New features add controllers/services under one module instead of one more file in a flat list.
- **Route clarity:** Route prefixes can stay as today; grouping does not require changing `[Route("api/...")]`.
- **Future options:** If you later introduce module-specific middleware, feature flags, or API versioning, the structure supports it.

### 2.3 How to Migrate Gradually Without Breaking Routes

1. **Phase 1 (no route change):** Create subfolders under `Controllers/` and **move files only**. Keep namespace as `CephasOps.Api.Controllers` (or add sub-namespace, e.g. `CephasOps.Api.Controllers.Billing`) and **keep every `[Route("api/...")]` and method signature identical**. Build and run existing integration/smoke tests; confirm all routes respond as before.
2. **Phase 2 (optional):** Introduce route prefixes per folder via a base class or convention (e.g. `[Route("api/billing/[controller]")]`) only when you are ready to version or change URLs and can update all clients.
3. **Do not:** Change controller class names that are part of the route (if any convention uses them), or remove/rename action names that are part of the URL.

---

## PART 3 — Background Jobs Governance

### 3.1 Job Classification

| Job Type | Classification | Business Impact | SLA Expectation |
|----------|----------------|-----------------|-----------------|
| **EmailIngest** | Critical | Orders depend on email ingestion | At least one successful run per (2 × max poll interval) per active account |
| **MyInvoisStatusPoll** | Critical | Invoice submission status | Completion within configured poll window; failures may block invoice lifecycle |
| **NotificationSend** | Operational | User notifications | Best-effort; retries acceptable |
| **PnlRebuild** | Analytical | Reporting | Daily run; delay of one day acceptable |
| **populatestockbylocationsnapshots** | Analytical | Reporting | Every 6 h; delay acceptable |
| **reconcileledgerbalancecache** | Operational | Data consistency | Every 12 h |
| **InventoryReportExport** | Operational | Scheduled exports | Per schedule; failures alert owner |
| **DocumentGeneration** | Operational | Documents | Per request; retries acceptable |
| **NotificationRetention** | Operational | Cleanup | Best-effort |

### 3.2 SLA and Monitoring Requirements (Summary)

- **Critical (EmailIngest, MyInvoisStatusPoll):** Monitor “last success time” per account or per submission; alert if no success within 2× expected interval.
- **Operational:** Monitor failure rate and last run; alert on sustained failures (e.g. 3 consecutive failures).
- **Analytical:** Monitor last successful run; alert if no success in 2× schedule interval (e.g. PnlRebuild: 48 h).

### 3.3 Failure Escalation Rule (Proposal)

- **Critical jobs:** After max retries, mark job as Failed and raise alert (e.g. EmailIngest for account X has not succeeded in 2× poll interval). Optionally: create incident or notify on-call.
- **Operational/Analytical:** Log and optionally alert after N consecutive failures or after job has been Failed for longer than threshold.

### 3.4 Background Job Health Dashboard Structure

Design a single dashboard (e.g. under Admin or a dedicated “Background Jobs” UI) that shows:

| Column | Description |
|--------|-------------|
| Job type | EmailIngest, PnlRebuild, MyInvoisStatusPoll, etc. |
| Classification | Critical / Operational / Analytical |
| Last run (UTC) | Last time a job of this type completed (any account/entity) |
| Last success (UTC) | Last successful completion |
| Last failure (UTC) | Last failed completion |
| Failed count (24 h) | Number of failed executions in last 24 h |
| Queued count | Current queued jobs of this type |
| Running count | Currently running |
| Per-entity detail (where applicable) | e.g. EmailIngest: per-account last poll/success (reuse existing EmailParserHealthDto data) |

Data source: query `BackgroundJobs` table (and existing admin/email parser health endpoints) with aggregations by `JobType`, `State`, `CompletedAt`, `LastError`. No new job types or behaviour required—only aggregation and display.

### 3.5 Alert Rules (Examples)

- **EmailIngest:** “No successful EmailIngest run for account {AccountId} in the last {2 × PollIntervalSec} seconds” → Alert.
- **MyInvoisStatusPoll:** “No successful MyInvoisStatusPoll in the last 1 hour” (or 2× your poll interval) → Alert.
- **PnlRebuild:** “No successful PnlRebuild in the last 48 hours” → Alert.
- **Processor:** “BackgroundJobProcessorService has not processed any job in the last 10 minutes” (optional; may be normal if queue is empty).

Implement alerts via your existing monitoring stack (e.g. health endpoint consumers, log-based alerts, or application metrics). The dashboard above gives operators a single place to verify job health.

---

## PART 4 — Logging & Observability Organisation

### 4.1 Standardise Logging by Critical Module

**Parser**

- **MUST log:** Email account id/name (no credentials), ingest start/end, emails fetched count, parse sessions/drafts created, parse errors (template id, message id—no PII in body), Excel/PDF conversion success/failure (file name/size only).
- **MUST NOT log:** Passwords, tokens, full email bodies, attachment content, personally identifiable content (e.g. customer names in body if sensitive).

**Billing**

- **MUST log:** Invoice/submission id, MyInvois submit/status poll start/result (success/failure, status code), payment id and outcome; do not log full invoice payload or customer PII in logs.

**Workflow**

- **MUST log:** Workflow transition (entity type, id, transition name), side effect execution (name, success/failure), background job enqueue/start/complete/fail (job id, type, entity id if any).

**BackgroundJobs**

- **MUST log:** Job dequeue, start, success, failure (with job id, type, last error message); scheduler cycle skip (e.g. “no active accounts”, “DB not available”).

**EmailIngestion**

- **MUST log:** Per-account ingest start/end, counts (fetched, created, failed); connection or auth failures without credentials.

### 4.2 Log Format Structure

- **Structured:** Prefer structured properties (e.g. `{JobId}, {JobType}, {AccountId}`) so log aggregators can filter and alert.
- **Template:** Already in place: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}`. Keep it; ensure `SourceContext` is consistent (namespace/class).
- **Levels:** Error for failures and exceptions; Warning for retries and degraded behaviour; Information for normal completion; Debug/Verbose for development (reduce in production).

### 4.3 CorrelationId Usage

- **Current:** `CorrelationIdMiddleware` assigns or propagates `X-Correlation-Id`; `GlobalExceptionHandler` includes it in ProblemDetails and logs. Serilog is enriched with `CorrelationId` in `LogContext` for the request scope.
- **Requirement:** Ensure every request log and exception log in the pipeline includes CorrelationId. For background jobs, assign a new CorrelationId per job execution (e.g. at start of `ProcessSingleJob`) and push it to `LogContext` so all logs for that job carry the same id. No change to HTTP behaviour required.

### 4.4 Production Log Retention Policy

- **Retention:** Define a retention period (e.g. 30 days for file sink, 90 days in log aggregator if used). Currently Serilog file sink uses `retainedFileCountLimit: 7` (7 days). For production, increase or configure per environment (e.g. 30 days local, longer in centralised logging).
- **Location:** Keep logs outside web root; ensure `logs/` is not served. If using a log aggregator (e.g. Seq, ELK, Azure Monitor), ship logs there and optionally reduce local retention.
- **Sensitive data:** No passwords, tokens, or full PII in logs. Audit log sinks separately from application logs if required by policy.

---

## PART 5 — Configuration Structure

### 5.1 Environment-Specific Files

- **appsettings.json** — Base: logging levels, AllowedHosts, empty or placeholder ConnectionStrings, JWT placeholders, Cors, Carbone (non-secret), WhatsApp/Mail structure (no secrets), Encryption placeholders.
- **appsettings.Development.json** — Dev overrides: ConnectionStrings, LogLevel, DetailedErrors, DOTNET_WATCH_*, optional CORS. **Do not commit real secrets** (use user secrets or env).
- **appsettings.Staging.json** — Add: Staging-specific URLs, log levels, CORS; no production secrets.
- **appsettings.Production.json** — Add: Production AllowedHosts, stricter LogLevel (e.g. Information default), no secrets in file.

### 5.2 Where Each Setting Lives

| Setting | Location | Notes |
|---------|----------|--------|
| **DB connection** | Env / user secrets / secure vault; override `ConnectionStrings:DefaultConnection` | Never commit production connection strings. |
| **JWT** | appsettings base (placeholders); override Key/Issuer/Audience via env or vault in non-Dev | Key must be strong and secret in prod. |
| **Email (Mail)** | appsettings (Mail section): RetentionHours, BlockExternalImages, CleanupJob.IntervalMinutes, etc. | No mailbox passwords in config; those are in DB (encrypted). |
| **MyInvois** | **GlobalSettings (DB)** via IntegrationSettingsService (MyInvois_BaseUrl, _ClientId, _ClientSecret, _Enabled, _Environment) | Keep out of appsettings; already correct. |
| **SMS/WhatsApp** | Twilio/WhatsApp: config or GlobalSettings; WhatsAppCloudApi in appsettings (placeholders); override tokens via env | No tokens in source control. |
| **Syncfusion license** | Env or user secrets (e.g. `Syncfusion:LicenseKey`); set in code before use | Do not commit key. |
| **Carbone** | appsettings (Carbone section): BaseUrl, ApiVersion; ApiKey from env or vault | |
| **Encryption (Key/IV)** | Env or vault in prod; placeholders in base only | |

### 5.3 Ensure No Production Secrets in Source Control

- **Never commit** connection strings, JWT keys, API tokens, or Syncfusion license keys in any appsettings file. If **appsettings.Development.json** contains real credentials, add it to **.gitignore** or replace values with placeholders and use user secrets / env for every developer.
- Use **user secrets** for local dev: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` (and same for JWT, Syncfusion, etc.).
- Production: use environment variables or a secure vault; never commit production ConnectionStrings, JWT Key, Syncfusion key, or API tokens.

---

## PART 6 — Production Readiness Checklist

### 6.1 Go-Live Checklist

Use this as a gate before production deployment.

**Background jobs**

- [ ] BackgroundJobProcessorService starts and processes at least one job (e.g. trigger a test EmailIngest or PnlRebuild).
- [ ] EmailIngestionSchedulerService creates jobs for all active email accounts within 2× scheduler interval.
- [ ] PnlRebuildSchedulerService enqueues PnlRebuild when no pending job (after 24 h or manual trigger).
- [ ] StockSnapshot and LedgerReconciliation schedulers run without throwing (check logs).
- [ ] Background job health endpoint (e.g. `GET /api/backgroundjobs/health` or equivalent) returns Healthy when jobs are running and email accounts polled recently (or no accounts).

**Email ingest**

- [ ] At least one active email account configured with correct credentials (stored encrypted).
- [ ] Email ingest runs successfully (check logs for “Email ingest job completed successfully” or equivalent).
- [ ] Parser template is bound to the account (or default) so that orders can be created from emails.

**MyInvois**

- [ ] MyInvois settings configured in Integration Settings (GlobalSettings): BaseUrl, ClientId, ClientSecret, Enabled, Environment.
- [ ] Test submission and status poll (sandbox or staging) succeed; logs show no credential leakage.

**Parser template**

- [ ] Default or per-account parser template is bound and tested with a sample email/attachment so that parsed drafts appear as expected.

**Database**

- [ ] DB backup enabled and tested (restore verified).
- [ ] Connection string uses production DB and is supplied via env/vault (not in appsettings in repo).
- [ ] Migrations applied; no pending migrations in production.

**Logging**

- [ ] Logs written to intended path; retention and level appropriate for production (e.g. Information default, no sensitive data).
- [ ] CorrelationId present in request logs and exception logs (and optionally in background job logs).

**Deployment and operations**

- [ ] Rollback procedure documented (e.g. redeploy previous version, DB rollback plan if needed, feature flags if any).
- [ ] Health endpoint tested: `GET /api/admin/health` returns 200 when system is healthy and 503 when unhealthy (e.g. DB down).
- [ ] Admin account exists and can log in; admin health and background job health pages (or API) are accessible to ops.

**Smoke test scenario (document)**

- [ ] Document a short smoke test: e.g. (1) Login as admin, (2) Call GET /api/admin/health and GET background jobs health, (3) Trigger one email ingest (or wait for one run), (4) Create or view one order, (5) Submit one invoice to MyInvois (sandbox) and poll status. Execute and record result before go-live.

---

## Summary

- **Part 1:** Backend layering is clean; controller sprawl (92 controllers in one folder) and broad Settings scope are the main organisational issues. Background jobs are centralised in one processor and four schedulers; intervals are hardcoded.
- **Part 2:** Introduce controller subfolders by module without changing routes; keep Application structure as-is or optionally mirror under Modules.
- **Part 3:** Classify jobs (Critical / Operational / Analytical), define SLAs and failure escalation, and add a single Background Job Health Dashboard and alert rules (e.g. EmailIngest no success in 2× poll interval).
- **Part 4:** Standardise what must and must not be logged per module; keep current log format and CorrelationId; add CorrelationId to background job scope; set production log retention.
- **Part 5:** Separate appsettings by environment; keep secrets out of source control; MyInvois and sensitive config in DB/env/vault.
- **Part 6:** Use the go-live checklist (background jobs, email ingest, MyInvois, parser template, DB backup, logs, rollback, health, admin, smoke test) before production.

All of the above are organisation, governance, and production-hardening steps only—no new features and no breaking changes to existing business logic or routes.
