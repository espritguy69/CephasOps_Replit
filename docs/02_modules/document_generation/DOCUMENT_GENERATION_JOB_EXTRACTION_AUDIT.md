# Document Generation — Job Extraction Audit (Phase 5)

**Date:** 2026-03-10

---

## 1. Where documentgeneration jobs are produced

- **No current producers** in the codebase. No scheduler, API, or handler enqueues a BackgroundJob with JobType "documentgeneration".
- Document generation is invoked **synchronously** only: DocumentsController (invoices, dockets, RMA) and BillingController (invoice PDF) call IDocumentGenerationService directly.
- BackgroundJobProcessorService **consumes** documentgeneration jobs (ProcessDocumentGenerationJobAsync) if any were enqueued (e.g. via a generic job API or future feature).

---

## 2. Document types and payload

- **Processor payload** (ProcessDocumentGenerationJobAsync): documentType (required), entityId (required), companyId (required), userId (optional), referenceEntity (optional, default "Generic"), templateId (optional), format (optional, default "Pdf"), dataJson (optional JSON for Generic type).
- **Document types** (from DocumentGenerationService): Invoice, JobDocket, RmaForm, PurchaseOrder, Quotation, BOQ, DeliveryOrder, PaymentReceipt, Generic (and aliases Po, Quote, etc.).
- **GenerateDocumentDto** (used by IDocumentGenerationService.GenerateDocumentAsync): DocumentType, ReferenceEntity, ReferenceId (= entityId), TemplateId, Format, AdditionalData.

---

## 3. Output storage and duplicate risk

- **Output:** IDocumentGenerationService creates a **GeneratedDocument** row (CompanyId, DocumentType, ReferenceEntity, ReferenceId, TemplateId, FileId, GeneratedAt, etc.) and stores the PDF via IFileService (SavePdfToStorageAsync).
- **Duplicate risk:** Each run creates a **new** GeneratedDocument and a new file. Retries or duplicate enqueues produce multiple documents for the same reference unless guarded.
- **Idempotency:** Not currently enforced. We will add an optional guard: if an existing GeneratedDocument exists for (CompanyId, DocumentType, ReferenceEntity, ReferenceId) and payload does not request "replaceExisting", skip generation and return success.

---

## 4. Dependencies and company scoping

- **Dependencies:** ApplicationDbContext, IDocumentTemplateService, IFileService, ICarboneRenderer (template resolution and rendering). Company-scoped templates and entity loading (e.g. Invoice by companyId).
- **Company:** CompanyId is required; processor throws if missing. All generation is company-scoped.

---

## 5. Retry safety

- **Safe to retry:** Yes, if we treat "skip when document already exists" as success (idempotent). Otherwise retries create duplicate documents.
- **Failure handling:** Template missing, entity not found, render errors throw; processor marks job failed and schedules retry. We keep the same behavior in the executor.

---

## 6. Summary

| Item | Finding |
|------|--------|
| Producers | None (sync API only). We will add JobExecution path and optional async API. |
| Payload | documentType, entityId, companyId, userId?, referenceEntity?, templateId?, format?, dataJson?, replaceExisting? |
| Output | GeneratedDocument + file; duplicate possible without guard. |
| Idempotency | Add guard: skip if existing document when replaceExisting ≠ true. |
| Company | Required; preserved on JobExecution and in executor. |
