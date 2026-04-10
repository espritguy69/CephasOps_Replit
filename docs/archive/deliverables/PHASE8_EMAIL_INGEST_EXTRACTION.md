# Phase 8: Email Ingest Extraction

## A. Audit summary

- **Producer**: `EmailIngestionSchedulerService` (hosted) runs every 30s, loads active `EmailAccount`s with `PollIntervalSec > 0`, and for each account due for poll either created a legacy `BackgroundJob` (JobType `EmailIngest`, payload `{ emailAccountId }`) or now enqueues JobExecution (Phase 8).
- **Executor (legacy)**: `BackgroundJobProcessorService.ProcessEmailIngestJobAsync` read `emailAccountId` from payload and called `IEmailIngestionService.IngestEmailsAsync(emailAccountId)`. Parsing, attachments, and company context live inside `EmailIngestionService`; the job only triggers one poll per account.
- **Other triggers**: `EmailAccountsController.PollEmails` / `PollAllEmails` call `IEmailIngestionService.TriggerPollAsync` or `IngestEmailsAsync` directly (no job). These remain unchanged.
- **Scope**: One job per email account; payload is a single `emailAccountId`. Company comes from `EmailAccount.CompanyId` (used when enqueueing JobExecution).

## B. Convergence/extraction model chosen

- **Target: JobExecution** (existing orchestration pipeline).
- **Reason**: Email ingest is a discrete unit of work (poll one account); it fits the same pattern as SLA evaluation, PnL rebuild, etc. A dedicated parser-boundary worker would duplicate the “enqueue → claim → execute → mark done” machinery. Reusing JobExecution keeps one async execution model and preserves visibility via existing JobExecution queries.
- **Implementation**: New `EmailIngestJobExecutor` (JobType `emailingest`) parses `emailAccountId` from payload and calls `IEmailIngestionService.IngestEmailsAsync`. `EmailIngestionSchedulerService` now uses `IJobExecutionEnqueuer.EnqueueAsync("emailingest", payloadJson, companyId: account.CompanyId, priority: 1)` and skips enqueue when a pending/running JobExecution for that account already exists (deduplication by payload).

## C. Producer/execution paths migrated

- **Scheduler**: `EmailIngestionSchedulerService.ScheduleEmailIngestionJobsCoreAsync` no longer creates `BackgroundJob` rows. It checks `JobExecutions` for pending/running `emailingest` with same `emailAccountId` in payload; if none, it enqueues via `IJobExecutionEnqueuer.EnqueueAsync("emailingest", payloadJson, companyId: account.CompanyId, priority: 1)`.
- **Execution**: Runs in `JobExecutionWorkerHostedService` via `EmailIngestJobExecutor.ExecuteAsync` (calls `IEmailIngestionService.IngestEmailsAsync`). Parsing, attachment handling, and company/context behavior unchanged inside `EmailIngestionService`.

## D. Legacy responsibility reduced

- **emailingest** is no longer executed by `BackgroundJobProcessorService`. The handler was replaced by `ProcessEmailIngestJobDeprecatedAsync`, which logs deprecation and returns `true` (drain only). Top-of-file comment updated: emailingest deprecated (Phase 8); real execution via JobExecution + EmailIngestJobExecutor.

## E. Idempotency/retry/operational behavior

- **Deduplication**: Scheduler avoids enqueueing a second `emailingest` job for the same account while a pending or running one exists (query on `JobExecutions` by JobType and payload containing `emailAccountId`).
- **Retry**: JobExecution retry (AttemptCount, MaxAttempts, NextRunAtUtc) and worker lease/reset behavior unchanged. No change to `EmailIngestionService` retry semantics.
- **Operational**: Admin `GetEmailIngestionDiagnosticsAsync` now includes JobExecution `emailingest`: last success = max(legacy last success, JobExecution last success); 24h counts = legacy + JobExecution by state.

## F. Tests added

- No new test project tests in this pass. Existing `EmailIngestionServiceTests` and scheduler tests (if any) still apply to the service and scheduler behavior. Executor is a thin wrapper over `IEmailIngestionService.IngestEmailsAsync`; coverage can be added in a follow-up for executor + enqueue deduplication.

## G. Migrations added

- None. JobExecution table and schema already exist.

## H. Remaining debt after this phase

- Legacy BackgroundJob still owns: **myinvoisstatuspoll**, **inventoryreportexport**, **eventhandlingasync**, **operationalreplay**, **operationalrebuild**.
- **emailingest** remains in JobDefinitionProvider for observability; execution is deprecated (drain only).

## I. Files/docs created or updated

### Created
- `backend/src/CephasOps.Application/Workflow/JobOrchestration/Executors/EmailIngestJobExecutor.cs`
- `docs/PHASE8_EMAIL_INGEST_EXTRACTION.md`

### Updated
- `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`: use `IJobExecutionEnqueuer` and `JobExecutions` for deduplication; removed `BackgroundJob` creation and `IsStaleRunning`.
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs`: emailingest → `ProcessEmailIngestJobDeprecatedAsync`; removed `ProcessEmailIngestJobAsync`; updated comment.
- `backend/src/CephasOps.Api/Program.cs`: registered `EmailIngestJobExecutor`.
- `backend/src/CephasOps.Application/Admin/Services/AdminService.cs`: `GetEmailIngestionDiagnosticsAsync` now includes JobExecution emailingest for last success and 24h-by-state counts.

## J. Recommended next phase

- **Phase 9: MyInvois Status Poll Extraction** — move myinvoisstatuspoll into JobExecution (or an observable retry-safe path), preserve invoice/company scoping and external API behavior, deprecate legacy BackgroundJob ownership.
