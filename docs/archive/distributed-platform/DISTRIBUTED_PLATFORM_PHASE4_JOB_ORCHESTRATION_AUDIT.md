# Phase 4 Job Orchestration — Execution Landscape Audit

**Date:** 2026-03-09

---

## 1. BackgroundJob — Producers

| Producer | JobType(s) | Notes |
|----------|------------|--------|
| **LedgerReconciliationSchedulerService** | reconcileledgerbalancecache | Daily; no payload; single global job. |
| **SlaEvaluationSchedulerService** | slaevaluation | Scheduled; empty payload; CompanyId optional from payload. |
| **StockSnapshotSchedulerService** | populatestockbylocationsnapshots | Daily; periodEndDate, snapshotType (optional companyId). |
| **EmailIngestionSchedulerService** | emailingest | Per email account; payload: emailAccountId, etc. |
| **PnlRebuildSchedulerService** | (none — migrated) | Now uses IJobExecutionEnqueuer → JobExecution. |
| **InventoryController** | inventoryreportexport | On-demand; payload: reportType, filters, etc. |
| **ReplayJobEnqueuer** | operationalreplay | Replay operations; needs worker claim. |
| **RebuildJobEnqueuer** | operationalrebuild | Rebuild operations; needs worker claim. |
| **OrderAssignedOperationsHandler** | eventhandlingasync | Event-driven; payload includes event IDs. |
| **AsyncEventEnqueuer** | eventhandlingasync | Async event handling. |

---

## 2. BackgroundJobProcessorService — Job Types Handled

| JobType | Migrated? | Retry-safe | Coupling | Best next move |
|---------|-----------|------------|----------|----------------|
| pnlrebuild | Yes (Phase 3) | Yes | Low | Already on JobExecution. |
| reconcileledgerbalancecache | No | Yes | Low | **Migrate** — no payload, single call. |
| slaevaluation | No | Yes | Low | **Migrate** — scheduler-only, simple. |
| populatestockbylocationsnapshots | No | Yes | Low | **Migrate** — scheduler, payload simple. |
| emailingest | No | Partial | Medium | Keep legacy (email account lock). |
| notificationsend | No | Partial | Medium | Keep (notificationId). |
| notificationretention | No | Yes | Low | Can migrate later. |
| documentgeneration | No | Partial | High | Keep legacy. |
| myinvoisstatuspoll | No | Yes | Medium | Keep legacy. |
| inventoryreportexport | No | Partial | Medium | Keep legacy (API-triggered). |
| eventhandlingasync | No | No | High | Keep (replay/event bus). |
| operationalreplay | No | No | High | Keep (coordinator claim). |
| operationalrebuild | No | No | High | Keep (coordinator claim). |

---

## 3. JobExecution — Current Usage

- **PnlRebuildSchedulerService** enqueues via **IJobExecutionEnqueuer** ("PnlRebuild").
- **JobExecutionWorkerHostedService** claims, runs **PnlRebuildJobExecutor**, marks Succeeded/Failed, emits lifecycle events.
- **IJobExecutionStore**: AddAsync, ClaimNextPendingBatchAsync, MarkSucceededAsync, MarkFailedAsync only. No stale-lease recovery, no query API.

---

## 4. Risks and Gaps

- **Stale-lease risk:** JobExecution rows in **Running** with **ProcessingLeaseExpiresAtUtc** in the past are never reset; worker crash leaves them stuck.
- **Duplicate execution risk:** Low for JobExecution (claim with SKIP LOCKED). BackgroundJob: claim via IWorkerCoordinator reduces but does not eliminate race; reaper marks stale as Failed and does not re-queue.
- **Operational visibility:** No API or service to list pending/running/retry-scheduled/dead-letter JobExecutions; no attempt history beyond AttemptCount + LastError.
- **Lifecycle ambiguity:** Status values (Pending, Running, Succeeded, Failed, DeadLetter) are used but not formally documented; Failed + NextRunAtUtc is “retry scheduled” but not named.

---

## 5. Candidate Migrations (Phase 4)

1. **ReconcileLedgerBalanceCache** — High value, no payload, single call, retry-safe.  
2. **SlaEvaluation** — Scheduler-only, simple payload, retry-safe.  
3. **PopulateStockByLocationSnapshots** — Scheduler-driven, payload-based, retry-safe.

All three are scheduler-produced, low-coupling, and already implemented in BackgroundJobProcessorService; moving them to JobExecution + executors is straightforward.
