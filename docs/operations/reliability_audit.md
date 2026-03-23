# Reliability & Production Safety Audit

**Date:** March 2026  
**Purpose:** Audit background jobs, workflow transitions, idempotency, retries, transaction boundaries, and external integrations for production incident risk. Analysis only; no code changes.

**Related:** [background_jobs.md](background_jobs.md) | [myinvois_production_runbook.md](myinvois_production_runbook.md) | [level1_code_integrity.md](../engineering/level1_code_integrity.md)

---

## 1. Executive summary

**Reliability overview:** The system uses **lease-based claiming** (EventStore, JobExecution, NotificationDispatch) and **idempotency** (command pipeline, external webhooks) to reduce duplicate work. **JobExecution** and **EventStore** use **FOR UPDATE SKIP LOCKED** (or equivalent) and lease expiry for safe multi-worker processing. **Command** execution is protected by IdempotencyBehavior and RetryBehavior; **EF Core** has EnableRetryOnFailure for transient DB errors. Gaps: **legacy BackgroundJob** table does not use the same claim/lease pattern as JobExecution (single processor, polling); **scheduler-enqueued jobs** (e.g. PnlRebuild, EmailIngest) can be enqueued more than once if scheduler runs overlap; **workflow transitions** are not inherently idempotent (same transition twice may be rejected by guard or state). **MyInvois** submission is recorded with submission history; status poll job can re-run—idempotency of poll is “update if status changed.” **Document generation** and **notification dispatch** are at-least-once with retries; duplicate send risk exists if not guarded by business idempotency (e.g. notification by template+order+channel).

---

## 2. Job safety matrix

| System | Claim / lease | Duplicate execution risk | Retry policy | Idempotency |
|--------|----------------|---------------------------|--------------|-------------|
| **JobExecution** (IJobExecutor) | ClaimNextPendingBatchAsync with FOR UPDATE SKIP LOCKED; nodeId + leaseExpiresAtUtc; ResetStuckRunningAsync | **Low** – only one worker can claim a row | MarkFailedAsync with NextRunAtUtc backoff (60s, 300s, 900s, 3600s); MaxAttempts → DeadLetter | Per job instance (one row = one logical job). Executors must be idempotent by payload (e.g. PnlRebuild for period X). |
| **EventStoreDispatcherHostedService** | ClaimNextPendingBatchAsync (EventStore); lease; retry count and non-retryable classification | **Low** – claimed events processed once per claim | RetryCount, NextRetryAtUtc; IsNonRetryable → poison | Event append is append-only. Handlers (e.g. ledger) must be idempotent by SourceEventId + Family. |
| **NotificationDispatchWorkerHostedService** | ClaimNextPendingBatchAsync (NotificationDispatchStore); nodeId + lease | **Medium** – if send fails after claim release, same row can be claimed again; duplicate send possible | Status Sent/Failed/DeadLetter; optional retry via NextRunAtUtc if implemented | Depends on template+recipient+context; no global idempotency key in audit. |
| **BackgroundJobProcessorService** (legacy) | Polls BackgroundJob table; processing log TryClaimAsync for some job types | **Medium** – no FOR UPDATE SKIP LOCKED on BackgroundJob; multiple workers could pick same job if not single-instance | Job state Queued→Running→Succeeded/Failed; retries via job config | Some job types use CommandProcessingLogStore (idempotency key in payload); not all. |
| **Scheduler-enqueued jobs** | Schedulers (e.g. PnlRebuildSchedulerService) enqueue one job per run; no dedupe of “same period” | **Medium** – if two scheduler ticks overlap, two PnlRebuild for same period can be enqueued | Via JobExecution or BackgroundJob retry | PnlRebuild: idempotent by period. EmailIngest: per account/session. ReconcileLedgerBalanceCache: should be idempotent. |
| **Command pipeline** | IdempotencyBehavior: TryClaimAsync by IdempotencyKey; cached result returned on replay | **Low** – same key returns cached result | RetryBehavior in pipeline; EF EnableRetryOnFailure | **Yes** – CommandProcessingLogStore; optional RequireIdempotency. |
| **Inbound webhooks** | ExternalIdempotencyStore.TryClaimAsync(connectorKey, idempotencyKey, …) | **Low** – duplicate request same key → conflict | N/A | **Yes** – external idempotency key. |

---

## 3. Idempotency coverage assessment

| Area | Covered | Mechanism | Gap |
|------|---------|-----------|-----|
| **Commands** | Yes | IdempotencyBehavior + CommandProcessingLogStore; IdempotencyKey on command or options | Commands without IdempotencyKey are not idempotent; RequireIdempotency can enforce. |
| **External outbound (MyInvois)** | Partial | Submission history recorded; status poll updates by submission id | Resubmit same invoice could create duplicate submission if not guarded by business rule (e.g. “already submitted”). |
| **Inbound webhooks** | Yes | ExternalIdempotencyStore per connectorKey + ExternalIdempotencyKey | — |
| **Event store handlers** | By design | Ledger: idempotent by (SourceEventId, Family). Others documented per handler. | All handlers must be implemented idempotently; no global key. |
| **JobExecution executors** | By design | One row = one job; executor must be idempotent by payload (e.g. PnlRebuild(period)) | Document each executor’s idempotency contract. |
| **Notification send** | Not global | Per NotificationDispatch row; same row retried until Sent/DeadLetter | Duplicate “same” notification (e.g. same order+template) if two rows created; business layer should dedupe. |
| **Document generation** | Depends | One job per document request; regenerate overwrites or new file | If same request enqueued twice, two files possible; use idempotency key in payload if needed. |

---

## 4. Retry and failure handling assessment

| Component | Retry | Dead letter / poison | Notes |
|-----------|--------|------------------------|-------|
| **EF Core** | EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 5s) | N/A | Transient DB errors. |
| **Command pipeline** | RetryBehavior (transient) | N/A | Logging; no dead letter for commands. |
| **EventStore** | RetryCount, NextRetryAtUtc; maxRetriesBeforeDeadLetter | IsNonRetryable → poison; non-retryable classifier | MarkProcessedAsync(success: false, isNonRetryable). |
| **JobExecution** | AttemptCount, NextRunAtUtc backoff; MaxAttempts | Status DeadLetter when isNonRetryable or AttemptCount >= MaxAttempts | ResetStuckRunningAsync for stale Running. |
| **NotificationDispatch** | Optional NextRunAtUtc (if implemented) | Status Failed/DeadLetter | Worker marks Sent/Failed/DeadLetter. |
| **BackgroundJob (legacy)** | Job-level retry config | Failed state | ReapStaleRunningJobsAsync for stuck Running. |
| **MyInvois** | Provider/HTTP retry (if any) | Submission history status; order transition to Rejected | See myinvois_production_runbook. |

---

## 5. Transactionality concerns

- **WorkflowEngineService:** Uses AppendInCurrentTransaction for event store (outbox pattern); same transaction as business data where applicable. **Safe** when DbContext transaction is active.
- **JobExecutionStore.ClaimNextPendingBatchAsync:** Uses explicit BeginTransactionAsync and FOR UPDATE SKIP LOCKED; Commit after update. **Safe** for concurrent workers.
- **StockLedgerService / InvoiceSubmissionService:** Multiple SaveChangesAsync or explicit transaction usage in Application (see Level 1 grep). **Partial write risk** if a step fails after first SaveChanges—document critical paths and ensure single transaction for multi-step writes where required.
- **Replay / Rebuild:** Use locks (ReplayExecutionLock, RebuildExecutionLock) and checkpoints; operational runbooks describe safe usage.

---

## 6. Recovery gaps

- **Legacy BackgroundJob:** No lease in table; single BackgroundJobProcessorService instance assumed. If multiple instances run, duplicate processing possible. **Recommendation:** Run single instance or migrate to JobExecution.
- **Scheduler overlap:** Schedulers that enqueue “one per run” can enqueue twice if run duration exceeds interval. **Recommendation:** Document “at most once per interval” or add “last enqueue time” guard per job type.
- **Notification duplicate send:** No application-level idempotency key for “same notification.” **Recommendation:** Dedupe by (OrderId, TemplateId, Channel) or similar in business layer when creating NotificationDispatch rows.

---

## 7. Top production incident scenarios

| Scenario | Likelihood | Impact | Mitigation |
|----------|------------|--------|------------|
| Duplicate order status transition (double-click or retry) | Medium | Wrong state or duplicate side effects | Workflow engine rejects invalid transition; command idempotency for API-triggered transitions. |
| MyInvois submission twice for same invoice | Low | Duplicate submission to LHDN | Business rule: do not resubmit if already Submitted/Accepted; record submission id. |
| Event store handler throws repeatedly (poison) | Medium | Event stuck; lag grows | Non-retryable classification; dead-letter; operational replay/repair runbooks. |
| JobExecution executor fails all retries | Medium | Job DeadLetter; manual fix | Alert on DeadLetter count; runbook for PnlRebuild/Ledger/Replay. |
| Ledger partial write (e.g. allocation without ledger entry) | Low | Ledger inconsistency | Use single transaction for allocation + ledger; reconciliation job. |
| Scheduler enqueues two PnlRebuild for same period | Low | Double work; possible contention | Idempotent by period; or “skip if already running for period” in scheduler. |

---

## 8. Prioritized hardening recommendations

| Priority | Action | Refactor safety |
|----------|--------|-------------------|
| P1 | Document idempotency contract per JobExecution executor (PnlRebuild, ReconcileLedgerBalanceCache, SlaEvaluation, etc.) in background_jobs.md. | Doc only. |
| P1 | Ensure only one instance of BackgroundJobProcessorService in production, or add lease/claim to BackgroundJob table. | Code change; high impact. |
| P2 | Add “last enqueue” or “in progress” guard for scheduler-enqueued jobs (e.g. PnlRebuild for current month) to avoid overlapping enqueues. | Medium. |
| P2 | NotificationDispatch: document or add business-level dedupe (e.g. unique constraint or check before insert) for “same” notification. | Low–medium. |
| P2 | MyInvois: ensure “already submitted” check before calling provider submit. | Low. |
| P3 | Single transaction for critical multi-step writes (ledger, invoice submission) where not already in place. | Medium. |

---

## 9. Related artifacts

- [Background jobs](background_jobs.md)
- [MyInvois production runbook](myinvois_production_runbook.md)
- [Level 1 code integrity](../engineering/level1_code_integrity.md)
- [Event store operations](../EVENT_BUS_OPERATIONS.md) (if present)
- [Migration integrity watch](migration_integrity_watch.md)
