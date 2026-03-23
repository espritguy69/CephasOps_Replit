# Scheduled Payout Anomaly Alerts – Design and Deliverables

## Executive summary

**Scheduled payout anomaly alerts** run the existing alert pipeline automatically on a configurable interval (e.g. every 6 or 24 hours). No payout or anomaly-detection logic is changed; the manual POST endpoint remains; duplicate-prevention behavior is unchanged. If the database is unavailable, the run is skipped safely; if the email sender fails, the failure is logged and the scheduler continues.

- **Scheduler:** Background hosted service; when enabled, runs alerting every `SchedulerIntervalHours` (default 6).
- **Config:** `SchedulerEnabled`, `SchedulerIntervalHours`, plus existing `DefaultRecipientEmails`, `IncludeMediumRepeated`, `DuplicatePreventionHours`.
- **Run history:** Table `PayoutAnomalyAlertRuns` stores each run (scheduler or manual) with evaluated/sent/skipped/error counts and trigger source. Latest run is exposed to the UI.
- **Safety:** DB unavailable → skip run and log; sender exception → log and continue; duplicate prevention unchanged.

---

## Scheduler

- **Service:** `PayoutAnomalyAlertSchedulerService` (BackgroundService).
- **Behavior:** If `SchedulerEnabled` is false, the service logs and waits indefinitely (no runs). If enabled, it loops with `Task.Delay(interval)` and each iteration:
  - Checks DB with `CanConnectAsync`; if unavailable, logs and skips.
  - Builds a request from options (DefaultRecipientEmails, IncludeMediumRepeated).
  - Calls `IPayoutAnomalyAlertService.RunAlertsAsync` (same as manual endpoint).
  - Logs: run start, evaluated/sent/skipped/errors, run completed.
  - Records the run via `IAlertRunHistoryService.RecordRunAsync` (trigger Scheduler).
- **Interval:** Configurable in hours (default 6); clamped to 1–168 (1h–7d).

---

## Configuration added

Under `PayoutAnomalyAlert` (e.g. `appsettings.json` or environment):

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SchedulerEnabled` | bool | false | When true, background scheduler runs alerting on interval. |
| `SchedulerIntervalHours` | int | 6 | Hours between scheduled runs (1–168). |
| `DefaultRecipientEmails` | string | "" | Already existed; used by scheduler when no request override. |
| `IncludeMediumRepeated` | bool | false | Already existed; used by scheduler. |
| `DuplicatePreventionHours` | int | 24 | Already existed; unchanged. |

---

## Logging

- **Scheduler start:** "Payout anomaly alert scheduler started (interval: Xh, duplicate prevention: Yh)".
- **Scheduler stop:** "Payout anomaly alert scheduler stopped".
- **Run start:** "Scheduled payout anomaly alert run starting (trigger: Scheduler)".
- **Run completed:** "Scheduled payout anomaly alert run completed: evaluated=X, sent=Y, skipped=Z, errors=W".
- **Alerts skipped:** Reflected in "skipped" in the completed log line (duplicate prevention).
- **Send failures:** Per-error "Payout anomaly alert run error: {Error}"; and "Scheduled payout anomaly alert run failed" on exception.
- **DB unavailable:** "Database not available, skipping scheduled payout anomaly alert run".

---

## Safety

- **DB unavailable:** `CanConnectAsync` returns false → log warning and skip run; no exception thrown.
- **Email sender fails:** Exception is caught in the alert service and in the scheduler; run is logged as completed with errors; run history is still recorded; scheduler continues to the next delay.
- **Duplicate prevention:** Unchanged; same logic as manual run (no re-alert within `DuplicatePreventionHours` for the same anomaly/channel).

---

## Run history

- **Table:** `PayoutAnomalyAlertRuns` (entity `PayoutAnomalyAlertRun`).
- **Columns:** Id, StartedAt, CompletedAt, EvaluatedCount, SentCount, SkippedCount, ErrorCount, TriggerSource (Scheduler | Manual).
- **Recording:** Scheduler and manual POST both call `IAlertRunHistoryService.RecordRunAsync` after each run.
- **API:** GET `api/payout-health/alert-runs/latest` returns the most recent run (for UI).

---

## UI

- **Payout Anomalies page:** When run history exists, a "Latest alert run" section shows the latest run: started/completed time, trigger (Scheduler/Manual), evaluated, sent, skipped (duplicate window), and error count.

---

## Files changed

### Backend

- **Domain:** `AlertRunTriggerSource.cs` (new), `Entities/PayoutAnomalyAlertRun.cs` (new).
- **Application:**  
  - `PayoutAnomalyAlertOptions.cs`: added `SchedulerEnabled`, `SchedulerIntervalHours`.  
  - `DTOs/PayoutAnomalyAlertDto.cs`: added `SkippedCount` to result; added `PayoutAnomalyAlertRunDto`.  
  - `Services/IAlertRunHistoryService.cs`, `AlertRunHistoryService.cs` (new).  
  - `Services/PayoutAnomalyAlertService.cs`: set `result.SkippedCount`.  
  - `Services/PayoutAnomalyAlertSchedulerService.cs` (new).
- **Infrastructure:**  
  - `PayoutAnomalyAlertRunConfiguration.cs` (new).  
  - `ApplicationDbContext.cs`: DbSet `PayoutAnomalyAlertRuns`.  
  - `Migrations/20260310180000_AddPayoutAnomalyAlertRuns.cs` (new).  
  - `ApplicationDbContextModelSnapshot.cs`: added `PayoutAnomalyAlertRun` entity.
- **API:**  
  - `Program.cs`: register `IAlertRunHistoryService`, `PayoutAnomalyAlertSchedulerService`.  
  - `PayoutHealthController.cs`: inject `IAlertRunHistoryService`; record manual run after POST run-anomaly-alerts; GET `alert-runs/latest`.
- **Scripts:** `scripts/add-payout-anomaly-alert-runs-table.sql` (idempotent).

### Frontend

- **types/payoutHealth.ts:** `skippedCount` on result DTO; `PayoutAnomalyAlertRunDto`.
- **api/payoutHealth.ts:** `getLatestAlertRun()`.
- **pages/reports/PayoutAnomaliesPage.tsx:** query latest alert run; "Latest alert run" section.

### Docs

- **docs/PAYOUT_ANOMALY_ALERT_SCHEDULER.md** (this file).

---

## Confirmation: payout logic unchanged

- **Payout calculation:** Not modified. Scheduler only triggers the existing alert run, which reads anomaly data.
- **Anomaly detection:** Not modified. Same `GetAnomaliesAsync` and rules.
- **Manual endpoint:** POST `api/payout-health/run-anomaly-alerts` is unchanged and still available; manual runs are also recorded in run history.
- **Duplicate prevention:** Unchanged; same window and logic as before.
