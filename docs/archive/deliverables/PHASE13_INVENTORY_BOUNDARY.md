# Phase 13: Inventory Boundary

## Purpose

Document the boundary between **inventory** and the distributed job pipeline. Inventory report export was migrated in Phase 10; this phase records the resulting boundary.

---

## 1. Inventory background work

| Flow | Before Phase 10 | After Phase 10 | Pipeline |
|------|------------------|----------------|----------|
| **Inventory report export** (UsageSummary / SerialLifecycle CSV, optional email) | BackgroundJob `"InventoryReportExport"` created by `InventoryController.ScheduleReportExport`; executed by `BackgroundJobProcessorService.ProcessInventoryReportExportJobAsync`. | `InventoryController.ScheduleReportExport` uses `IJobExecutionEnqueuer.EnqueueWithIdAsync("inventoryreportexport", ...)` and returns `Accepted` with `jobId`. Executor: `InventoryReportExportJobExecutor`. Legacy BackgroundJob path is drain-only. | **JobExecution** |

---

## 2. Inventory module – no other background work

- **Location:** `backend/src/CephasOps.Application/Inventory/`
- **Schedulers / HostedServices:** None in the Inventory namespace.
- **Other BackgroundJob/JobExecution types:** None. Stock ledger, materials, allocations, and CSV export are either request-scoped or triggered by the single report-export job above.

---

## 3. Related work (outside Inventory namespace)

- **Populate stock-by-location snapshots:** `PopulateStockByLocationSnapshotsJobExecutor` (Phase 4), enqueued by `StockSnapshotSchedulerService` — not in Inventory namespace but touches stock/snapshot data.
- **Reconcile ledger balance cache:** `ReconcileLedgerBalanceCacheJobExecutor` (Phase 4), enqueued by `LedgerReconciliationSchedulerService` — ledger/inventory-adjacent.

---

## 4. Summary

| Area | Background work | Pipeline | Notes |
|------|------------------|----------|--------|
| Inventory report export | ScheduleReportExport → CSV (± email) | JobExecution (Phase 10) | Only inventory-specific job type. |
| Stock snapshots / ledger reconciliation | Separate schedulers + executors | JobExecution (Phase 4) | Inventory-related; not under Inventory module. |

**Phase 13 deliverable:** This boundary document. No code changes (Phase 10 already completed the migration).
