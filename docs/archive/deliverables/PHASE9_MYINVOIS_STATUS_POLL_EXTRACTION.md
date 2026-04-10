# Phase 9: MyInvois Status Poll Extraction

## A. Audit summary

- **Executor (legacy)**: `BackgroundJobProcessorService.ProcessMyInvoisStatusPollJobAsync` — payload `submissionHistoryId` (guid). Loads `InvoiceSubmissionHistory`, gets provider via `EInvoiceProviderFactory`, calls `provider.GetInvoiceStatusAsync(submission.SubmissionId)`, then `IInvoiceSubmissionService.UpdateSubmissionStatusAsync(submissionHistoryId, status, rejectionReason, null, null)`.
- **Producers**: None in codebase. No code path created a BackgroundJob with JobType `myinvoisstatuspoll`. Intended use was likely after invoice submission to portal (poll status after a delay).
- **Scope**: Per submission history record; company comes from submission/invoice.

## B. Convergence model chosen

- **Target: JobExecution** (same orchestration pipeline as email ingest, SLA, etc.).
- **Implementation**: New `MyInvoisStatusPollJobExecutor` (JobType `myinvoisstatuspoll`) parses `submissionHistoryId`, loads submission via `IInvoiceSubmissionService.GetSubmissionByHistoryIdAsync`, calls provider `GetInvoiceStatusAsync(submission.SubmissionId)`, then `UpdateSubmissionStatusAsync`. Producer: after `RecordSubmissionAsync` when `portalType` is MyInvois, enqueue a JobExecution with `nextRunAtUtc = now + 2 minutes` so status is polled after portal processing.

## C. Producer/execution paths migrated

- **Producer**: `InvoiceSubmissionService.RecordSubmissionAsync` — after saving a MyInvois submission, enqueues `myinvoisstatuspoll` via `IJobExecutionEnqueuer.EnqueueAsync(..., nextRunAtUtc: now + 2 min, companyId: invoice.CompanyId)`.
- **Execution**: `JobExecutionWorkerHostedService` runs `MyInvoisStatusPollJobExecutor.ExecuteAsync` (load submission, get provider, get status, update submission).

## D. Legacy responsibility reduced

- **myinvoisstatuspoll** no longer executed by BackgroundJobProcessorService; replaced by `ProcessMyInvoisStatusPollJobDeprecatedAsync` (drain only). Comment updated.

## E. Idempotency/retry/operational behavior

- **Retry**: JobExecution retry/lease and backoff unchanged. Executor throws on provider failure so job can retry.
- **Company**: Job enqueued with `companyId: invoice.CompanyId` for visibility/scoping.

## F. Tests added

- None this phase. Executor and enqueue path can be covered in a follow-up.

## G. Migrations added

- None.

## H. Remaining debt after this phase

- Legacy BackgroundJob still: **inventoryreportexport**, **eventhandlingasync**, **operationalreplay**, **operationalrebuild**.

## I. Files/docs created or updated

### Created
- `Executors/MyInvoisStatusPollJobExecutor.cs`
- `docs/PHASE9_MYINVOIS_STATUS_POLL_EXTRACTION.md`

### Updated
- `IInvoiceSubmissionService.cs`: added `GetSubmissionByHistoryIdAsync`.
- `InvoiceSubmissionService.cs`: added `GetSubmissionByHistoryIdAsync`; optional `IJobExecutionEnqueuer`; after `RecordSubmissionAsync` for MyInvois, enqueue `myinvoisstatuspoll` with 2-min delay.
- `BackgroundJobProcessorService.cs`: myinvoisstatuspoll → deprecation no-op; removed `ProcessMyInvoisStatusPollJobAsync`; comment updated.
- `Program.cs`: registered `MyInvoisStatusPollJobExecutor`.

## J. Recommended next phase

- **Phase 10: Inventory Report Export Extraction** — move inventoryreportexport to JobExecution, preserve file/result behavior, deprecate legacy.
