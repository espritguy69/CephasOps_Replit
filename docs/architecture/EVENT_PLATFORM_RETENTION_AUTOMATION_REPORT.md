# Event Platform — Retention and Archival Automation Report

**Date:** Retention and Archival Automation pass.  
**Purpose:** Required final output for the retention/archival automation phase.

---

## A. Tables covered

| Table | Eligibility | Cutoff | Default retention |
|-------|-------------|--------|--------------------|
| **EventStore** | Status in (Processed, DeadLetter) | ProcessedAtUtc ?? CreatedAtUtc &lt; cutoff | 90 days |
| **EventProcessingLog** | State = Completed | CompletedAtUtc &lt; cutoff | 90 days |
| **OutboundIntegrationDeliveries** | Status = Delivered | DeliveredAtUtc &lt; cutoff | 60 days |
| **OutboundIntegrationAttempts** | — | Cascade-deleted with parent delivery | (no separate window) |
| **InboundWebhookReceipts** | Status = Processed | ProcessedAtUtc &lt; cutoff | 90 days |
| **ExternalIdempotencyRecords** | CompletedAtUtc not null | CompletedAtUtc &lt; cutoff | 7 days |

Pending, Processing, Failed, DeadLetter (EventStore); Failed/DeadLetter (Outbound); HandlerFailed (Inbound) are **never** deleted by retention.

---

## B. Retention model implemented

- **Per-category retention days:** Configurable per table/category (EventStoreProcessedAndDeadLetterDays, EventProcessingLogCompletedDays, OutboundDeliveredDays, InboundProcessedDays, ExternalIdempotencyCompletedDays). Set to **0** to disable cleanup for that category.
- **Batch cap:** MaxDeletesPerTablePerRun (default 1000) limits rows deleted per table per run to avoid long locks.
- **Delete order:** EventProcessingLog → EventStore → OutboundIntegrationDeliveries → InboundWebhookReceipts → ExternalIdempotencyRecords. OutboundIntegrationAttempts are removed by FK cascade when the parent delivery is deleted.
- **Idempotent:** Each run selects up to N eligible IDs per table and deletes; next run continues from the next oldest. Restart-safe.

---

## C. Worker/services added

| Component | Location | Role |
|-----------|----------|------|
| **EventPlatformRetentionOptions** | Application/Integration | Config POCO; section EventPlatformRetention. |
| **IEventPlatformRetentionService** | Application/Integration | Interface: RunRetentionAsync() → EventPlatformRetentionResult. |
| **EventPlatformRetentionResult** | Application/Integration | DTO: per-table deleted counts, RunStartedAtUtc, RunCompletedAtUtc, Errors. |
| **EventPlatformRetentionService** | Application/Integration | Implementation: uses ApplicationDbContext, batch select + ExecuteDeleteAsync per table. |
| **EventPlatformRetentionWorkerHostedService** | Application/Integration | BackgroundService: periodic RunRetentionAsync (interval from config). |

---

## D. Config added

**Section:** `EventPlatformRetention`

| Key | Default | Description |
|-----|---------|--------------|
| Enabled | true | When false, worker does not run. |
| RunIntervalSeconds | 86400 | Interval between retention runs (24h). |
| EventStoreProcessedAndDeadLetterDays | 90 | 0 = skip. |
| EventProcessingLogCompletedDays | 90 | 0 = skip. |
| OutboundDeliveredDays | 60 | 0 = skip. |
| InboundProcessedDays | 90 | 0 = skip. |
| ExternalIdempotencyCompletedDays | 7 | 0 = skip. |
| MaxDeletesPerTablePerRun | 1000 | Batch size per table per run. |

---

## E. Safety rules enforced

- **Only completed/success rows:** Processed/DeadLetter (EventStore), Completed (EventProcessingLog), Delivered (Outbound), Processed (Inbound), completed idempotency records. No Pending/Processing/Failed/HandlerFailed.
- **Batch limit:** No more than MaxDeletesPerTablePerRun rows per table per run.
- **Order:** Log before EventStore; deliveries (with cascade) before receipts; idempotency last.
- **Logging:** Per-table delete count and cutoff; summary with TotalDeleted and errors. No secrets.
- **Errors:** Exceptions are caught, added to result.Errors, and logged; run completes and returns result.

---

## F. Tests added

| Test | File | What it does |
|------|------|--------------|
| TotalDeleted_sums_all_category_counts | EventPlatformRetentionTests.cs | Asserts EventPlatformRetentionResult.TotalDeleted is sum of the five category counts. |
| TotalDeleted_when_all_zero_is_zero | EventPlatformRetentionTests.cs | Asserts default result has TotalDeleted 0. |
| Defaults_match_documentation | EventPlatformRetentionOptionsTests | Asserts all option defaults match the documented values. |
| SectionName_is_EventPlatformRetention | EventPlatformRetentionOptionsTests | Asserts section name constant. |
| RunRetentionAsync_with_all_days_zero_does_not_throw_and_returns_zero_deleted | EventPlatformRetentionServiceTests | In-memory DbContext; options all 0; RunRetentionAsync(); asserts TotalDeleted 0, no errors, RunCompletedAtUtc >= RunStartedAtUtc. |

Also fixed **DocumentGenerationJobEnqueuerTests** to pass **maxAttempts** (8th parameter) in the EnqueueAsync mock/verify so the test project builds.

---

## G. Docs/runbook updated

- **backend/scripts/EVENT_PLATFORM_RUNBOOK.md:** Section 8 rewritten to describe automated retention: worker, config section, tables and eligibility, default retention, batch behavior, logging, idempotency, manual trigger. One legacy Policy bullet left (could not remove due to encoding).
- **docs/architecture/EVENT_PLATFORM_DATA_MODEL.md:** Retention paragraphs for EventStore, OutboundIntegrationDeliveries, and Inbound/ExternalIdempotency updated to reference the retention worker and default days.

---

## H. Remaining event platform debt

- **Optional admin endpoint:** POST (or GET) to trigger retention once (e.g. `/api/integration/retention/run`) for on-demand runs without changing worker config.
- **Metrics:** Emit a metric or health counter for “last retention run” and “total deleted last run” if operational dashboards are added.
- **EventProcessingLog vs EventStore order:** Currently we delete EventProcessingLog first, then EventStore. EventProcessingLog has no FK to EventStore, so either order is valid; document the chosen order in code comments.
- **Runbook:** Remove the remaining legacy “Policy” bullet in §8 if desired (character encoding issue prevented automated removal).
- **EventStoreAttemptHistory:** Not included in this retention pass; can be added later with a separate retention window (e.g. delete attempt history older than 30 days) if the table grows.

---

**End of report.**
