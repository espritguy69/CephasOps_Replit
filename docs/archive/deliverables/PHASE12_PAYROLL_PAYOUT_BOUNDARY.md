# Phase 12: Payroll / Payout Boundary

## Purpose

Document the boundary between **payroll**, **payout**, and the distributed job pipeline (JobExecution vs scheduler-hosted work). No code migration in this phase; audit and boundary only.

---

## 1. Payroll module

- **Location:** `backend/src/CephasOps.Application/Payroll/`
- **Background work:** **None.** Payroll is driven by API/UI: periods, runs, items, rate plans. No BackgroundJob types, no JobExecution types, no hosted schedulers in the Payroll namespace.
- **Boundary:** Payroll is **in-process and request-scoped** (controllers → services → DB). P&L rebuild (which consumes payroll data) is already on JobExecution (`PnlRebuildJobExecutor`, Phase 3).

---

## 2. Payout (Rates) module – scheduler-hosted work

Two **scheduler-hosted** background flows live in **Rates** and do **not** use BackgroundJob or JobExecution:

| Flow | Service | Trigger | What it does | Observability |
|------|---------|---------|--------------|---------------|
| **Missing Payout Snapshot Repair** | `MissingPayoutSnapshotSchedulerService` | Every 24h (BackgroundService loop) | Calls `IMissingPayoutSnapshotRepairService.DetectMissingPayoutSnapshotsAsync`; creates snapshots for completed orders that lack one; writes `PayoutSnapshotRepairRun`. | Optional `IJobRunRecorder.StartAsync(JobType: "MissingPayoutSnapshotRepair")` for JobRuns table; no BackgroundJob. |
| **Payout Anomaly Alert** | `PayoutAnomalyAlertSchedulerService` | Configurable interval (e.g. 1–168h), when `PayoutAnomalyAlert:SchedulerEnabled=true` | Calls `IPayoutAnomalyAlertService.RunAlertsAsync`; evaluates anomalies, sends alerts; `IAlertRunHistoryService.RecordRunAsync` + `PayoutAnomalyAlertRun`. | Optional `IJobRunRecorder.StartAsync(JobType: "PayoutAnomalyAlert")` for JobRuns; no BackgroundJob. |

- **Boundary:** These remain **scheduler-hosted** (single process, no job queue). They are **not** handled by `BackgroundJobProcessorService` (no switch cases for them). JobRun recording is for **observability only** (display in admin/dashboards).
- **Optional future:** Could migrate to JobExecution (scheduler enqueues one job per run, executor runs the same logic) for unified visibility and retry in the same pipeline. Not required for Phase 12.

---

## 3. Other payout-related work already on JobExecution

- **P&L Rebuild** (consumes payroll/P&L data): `PnlRebuildJobExecutor` (Phase 3), enqueued by `PnlRebuildSchedulerService` or API.
- **Document generation** (e.g. payroll-related docs): `DocumentGenerationJobExecutor` (Phase 5).

---

## 4. Summary

| Area | Background work | Pipeline | Notes |
|------|------------------|----------|--------|
| Payroll | None | — | Request/API only. |
| Payout snapshot repair | MissingPayoutSnapshotSchedulerService | Scheduler (BackgroundService) | Optional JobRun observability; no queue. |
| Payout anomaly alert | PayoutAnomalyAlertSchedulerService | Scheduler (BackgroundService) | Optional JobRun observability; no queue. |
| P&L / payroll-adjacent | PnlRebuild, DocumentGeneration | JobExecution | Already migrated. |

**Phase 12 deliverable:** This boundary document. No code changes.
