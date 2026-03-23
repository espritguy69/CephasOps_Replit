# Phase 15: Workflow / Job Convergence Completion Summary

## Scope

Completion summary for the **distributed platform job convergence** (Phases 8–13): migration of remaining legacy BackgroundJob workloads to JobExecution and boundary documentation.

---

## 1. Migrations completed (Phases 8–11)

| Phase | Job type(s) | Producer change | Executor | Legacy handler |
|-------|-------------|-----------------|----------|----------------|
| **8** | emailingest | EmailIngestionSchedulerService → IJobExecutionEnqueuer; dedup by pending/running per account | EmailIngestJobExecutor | Drain-only |
| **9** | myinvoisstatuspoll | InvoiceSubmissionService.RecordSubmissionAsync (MyInvois) → EnqueueAsync(..., nextRunAtUtc: +2 min) | MyInvoisStatusPollJobExecutor | Drain-only |
| **10** | inventoryreportexport | InventoryController.ScheduleReportExport → EnqueueWithIdAsync; returns jobId | InventoryReportExportJobExecutor | Drain-only |
| **11** | eventhandlingasync | AsyncEventEnqueuer → IJobExecutionEnqueuer only (no BackgroundJob) | EventHandlingAsyncJobExecutor | Drain-only |
| **11** | operationalreplay | ReplayJobEnqueuer → enqueue JobExecution; operation.BackgroundJobId = null | OperationalReplayJobExecutor | Drain-only |
| **11** | operationalrebuild | RebuildJobEnqueuer → enqueue JobExecution; operation.BackgroundJobId = null | OperationalRebuildJobExecutor | Drain-only |

**Enqueuer API (Phase 11):** `IJobExecutionEnqueuer` gained optional `maxAttempts` (default 5); replay/rebuild use 2.

---

## 2. Legacy BackgroundJob processor state

- **BackgroundJobProcessorService** still runs for backward compatibility (drain remaining rows and any old jobs).
- All previously “active” job types above are **deprecation no-ops** in the switch; they log and return success (drain only).
- **Throw (migrated):** pnlrebuild, reconcileledgerbalancecache, slaevaluation, populatestockbylocationsnapshots, documentgeneration — must use JobExecution.
- **No BackgroundJob types** for payroll/payout schedulers (MissingPayoutSnapshotRepair, PayoutAnomalyAlert); those are scheduler-hosted only.

---

## 3. Boundary docs (Phases 12–13)

- **Phase 12 – Payroll/Payout:** `docs/PHASE12_PAYROLL_PAYOUT_BOUNDARY.md`. Payroll has no background work; payout snapshot repair and payout anomaly alert are scheduler-hosted (BackgroundService), with optional IJobRunRecorder observability; no JobExecution migration.
- **Phase 13 – Inventory:** `docs/PHASE13_INVENTORY_BOUNDARY.md`. Inventory report export is on JobExecution (Phase 10); no other inventory-specific background work.

---

## 4. JobExecution job types (post-convergence)

| JobType | Executor | Producer / enqueuer |
|---------|----------|---------------------|
| pnlrebuild | PnlRebuildJobExecutor | PnlRebuildSchedulerService, API |
| reconcileledgerbalancecache | ReconcileLedgerBalanceCacheJobExecutor | LedgerReconciliationSchedulerService |
| slaevaluation | SlaEvaluationJobExecutor | SlaEvaluationSchedulerService |
| populatestockbylocationsnapshots | PopulateStockByLocationSnapshotsJobExecutor | StockSnapshotSchedulerService |
| documentgeneration | DocumentGenerationJobExecutor | IDocumentGenerationJobEnqueuer / API |
| emailingest | EmailIngestJobExecutor | EmailIngestionSchedulerService |
| myinvoisstatuspoll | MyInvoisStatusPollJobExecutor | InvoiceSubmissionService (after MyInvois submit) |
| inventoryreportexport | InventoryReportExportJobExecutor | InventoryController.ScheduleReportExport |
| eventhandlingasync | EventHandlingAsyncJobExecutor | AsyncEventEnqueuer (DomainEventDispatcher) |
| operationalreplay | OperationalReplayJobExecutor | ReplayJobEnqueuer |
| operationalrebuild | OperationalRebuildJobExecutor | RebuildJobEnqueuer |

---

## 5. Optional follow-ups

- **Reporting/read models (Phase 14):** Any reporting-specific background jobs or read-model rebuilds can be audited and, if present, documented or moved to JobExecution using the same pattern.
- **Payout schedulers:** MissingPayoutSnapshotSchedulerService and PayoutAnomalyAlertSchedulerService can optionally be refactored to enqueue JobExecution and use executors for a single pipeline and retry semantics.
- **Legacy table:** Once all in-flight BackgroundJob rows for deprecated types have drained, consider retiring or archiving the BackgroundJob processor or table per ops runbook.

---

## 6. Phase docs index

| Doc | Content |
|-----|---------|
| PHASE8_EMAIL_INGEST_EXTRACTION.md | Email ingest → JobExecution |
| PHASE9_MYINVOIS_STATUS_POLL_EXTRACTION.md | MyInvois status poll → JobExecution |
| PHASE10_INVENTORY_REPORT_EXPORT_EXTRACTION.md | Inventory report export → JobExecution |
| PHASE11_EVENT_REPLAY_REBUILD_CONVERGENCE.md | EventHandlingAsync, OperationalReplay, OperationalRebuild → JobExecution |
| PHASE12_PAYROLL_PAYOUT_BOUNDARY.md | Payroll/payout boundary (scheduler vs JobExecution) |
| PHASE13_INVENTORY_BOUNDARY.md | Inventory boundary (report export on JobExecution) |
| PHASE15_WORKFLOW_CONVERGENCE_COMPLETION_SUMMARY.md | This summary |
