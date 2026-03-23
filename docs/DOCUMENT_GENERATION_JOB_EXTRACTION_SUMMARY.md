# Document Generation Job Extraction — Phase 5 Summary

**Date:** 2026-03-10

---

## A. Document generation flow audited

- **Audit:** `docs/DOCUMENT_GENERATION_JOB_EXTRACTION_AUDIT.md`
- **Findings:** No current producers of documentgeneration BackgroundJob; sync API only (DocumentsController, BillingController). Processor consumed documentgeneration if enqueued. Payload: documentType, entityId, companyId, userId?, referenceEntity?, templateId?, format?, dataJson?. Output: GeneratedDocument row + file. Duplicate risk on retry without guard.

---

## B. Document generation paths moved to JobExecution

- **Job type:** "DocumentGeneration".
- **Producer:** IDocumentGenerationJobEnqueuer + DocumentGenerationJobEnqueuer build payload and call IJobExecutionEnqueuer.EnqueueAsync("DocumentGeneration", payloadJson, companyId, ...).
- **API:** POST api/documents/generate-async (body: DocumentType, EntityId, ReferenceEntity?, TemplateId?, Format?, DataJson?, ReplaceExisting?) enqueues via IDocumentGenerationJobEnqueuer. CompanyId/userId from current user.
- **Executor:** DocumentGenerationJobExecutor runs IDocumentGenerationService.GenerateDocumentAsync; validates payload; implements idempotent skip when replaceExisting is false.

---

## C. New executor/contracts introduced

- **DocumentGenerationJobExecutor** (JobType = "DocumentGeneration"): Parses payload (documentType, entityId, companyId, userId, referenceEntity, templateId, format, dataJson, replaceExisting). Validates required fields. If !replaceExisting and existing document exists for (companyId, documentType, referenceEntity, entityId), skips and returns true. Otherwise builds GenerateDocumentDto and calls IDocumentGenerationService.GenerateDocumentAsync.
- **IDocumentGenerationJobEnqueuer** / **DocumentGenerationJobEnqueuer**: EnqueueAsync(documentType, entityId, companyId, userId?, referenceEntity?, templateId?, format?, dataJson?, replaceExisting?, correlationId?, causationId?, ct). Serializes payload and enqueues via IJobExecutionEnqueuer with CompanyId for audit.

---

## D. Idempotency/retry behavior defined

- **Idempotent skip:** When replaceExisting is not true, executor calls GetGeneratedDocumentsAsync(companyId, referenceEntity, entityId, documentType). If any document exists, logs and returns true without calling GenerateDocumentAsync. Retries after failure therefore do not create duplicate documents unless replaceExisting is true.
- **Retry:** JobExecution retry/dead-letter semantics unchanged (MaxAttempts, backoff, DeadLetter). Document generation failures (template missing, entity not found, render errors) throw and are retried by the worker.

---

## E. Operational visibility updated

- Document generation jobs appear in existing JobExecution query path (api/job-orchestration/summary, pending, running, dead-letter). Filter by JobType = "DocumentGeneration" for document-specific visibility. No new API added.

---

## F. BackgroundJob responsibility reduced

- **BackgroundJobProcessorService:** documentgeneration removed from legacy switch. Case "documentgeneration" throws NotSupportedException ("migrated to JobExecution; use IDocumentGenerationJobEnqueuer or IJobExecutionEnqueuer"). ProcessDocumentGenerationJobAsync method removed. Class comment updated: documentgeneration listed as migrated.

---

## G. Tests added

- **DocumentGenerationJobExecutorTests:** ExecuteAsync throws when documentType missing, entityId missing, companyId missing; skips when document exists and replaceExisting false (GenerateDocumentAsync not called); calls GenerateDocumentAsync when no existing document. CompanyId from job used.
- **DocumentGenerationJobEnqueuerTests:** EnqueueAsync calls IJobExecutionEnqueuer with "DocumentGeneration", payload contains documentType/entityId/companyId, CompanyId propagated; throws when CompanyId empty or DocumentType null.

---

## H. Migrations added

- None. JobExecutions table and schema unchanged.

---

## I. Remaining legacy job debt

- **BackgroundJob** still used for: emailingest, notificationsend, notificationretention, myinvoisstatuspoll, inventoryreportexport, eventhandlingasync, operationalreplay, operationalrebuild.

---

## J. Recommended Phase 6 extraction candidate

- **Notification send** (notificationsend) or **notification retention** (notificationretention): both are notification-boundary and could align with NotificationDispatch/JobExecution. Alternatively **myinvoisstatuspoll** (single-entity, retry-safe).

---

## K. Files/docs created or updated

**Created**
- docs/DOCUMENT_GENERATION_JOB_EXTRACTION_AUDIT.md
- docs/DOCUMENT_GENERATION_JOB_EXTRACTION_SUMMARY.md
- Application/Workflow/JobOrchestration/Executors/DocumentGenerationJobExecutor.cs
- Application/Workflow/JobOrchestration/IDocumentGenerationJobEnqueuer.cs
- Application/Workflow/JobOrchestration/DocumentGenerationJobEnqueuer.cs
- Api/Controllers/DocumentsController: GenerateDocumentAsync endpoint + GenerateDocumentAsyncRequest
- tests/.../JobOrchestration/DocumentGenerationJobExecutorTests.cs
- tests/.../JobOrchestration/DocumentGenerationJobEnqueuerTests.cs

**Updated**
- Api/Program.cs (register DocumentGenerationJobExecutor, IDocumentGenerationJobEnqueuer)
- Application/Workflow/Services/BackgroundJobProcessorService.cs (documentgeneration migrated, ProcessDocumentGenerationJobAsync removed, comment updated)
- Api/Controllers/DocumentsController.cs (inject IDocumentGenerationJobEnqueuer, POST generate-async)
