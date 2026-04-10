# Payout Anomaly Alerting – Design and Deliverables

## Executive summary

Automated **payout anomaly alerts** notify operations and finance when high-severity payout anomalies are detected. This is **read-only alerting**: it does not change payout calculation, anomaly detection rules, or payroll. There is no auto-blocking of payroll in this phase.

- **Trigger:** New high-severity anomalies (and optionally repeated medium-severity by type/context).
- **Duplicate prevention:** Same anomaly is not re-alerted on the same channel within a configurable window (default 24 hours).
- **Channels:** Email first; design allows adding Slack/Telegram later.
- **Tracking:** Each alert is recorded (anomaly fingerprint, channel, status, retry count) for auditing and deduplication.

---

## Alerting design

### Flow

1. **Trigger**  
   Alerting is run on demand (e.g. POST `api/payout-health/run-anomaly-alerts`) or by an external scheduler (cron). It is not tied to payroll or payout calculation.

2. **Candidates**  
   The service uses the existing **anomaly detection** (same as the dashboard):
   - Fetches anomalies for the last 30 days via `IPayoutAnomalyService.GetAnomaliesAsync`.
   - Keeps **High** severity and, if enabled, **Medium** (repeated by type/context).
   - No changes to detection rules or thresholds.

3. **Duplicate prevention**  
   For each channel, anomalies that already have a **successful** alert (status `Sent`) in the last **N hours** (configurable, default 24) are excluded. Only “new” anomalies for that window are sent.

4. **Send**  
   For each registered sender (e.g. Email):
   - Builds one email (or equivalent) with the list of anomalies.
   - Sends to configured or request-supplied recipients.
   - Records one `PayoutAnomalyAlert` row per anomaly per channel (status `Sent` or `Failed`).

5. **Recording**  
   Every send attempt is stored in `PayoutAnomalyAlerts`: anomaly fingerprint, channel, `SentAtUtc`, status, retry count, optional error message. This supports:
   - Deduplication (no re-alert within the window).
   - Auditing and “last alerted” in the UI.

### Options (config)

- **EmailAccountId** (optional): Which email account to use; if not set, first active account is used.
- **DefaultRecipientEmails**: Comma-separated default recipients when the API request does not provide any.
- **DuplicatePreventionHours**: Window in hours (default 24) during which the same anomaly is not re-alerted on the same channel.
- **IncludeMediumRepeated**: When true, medium-severity anomalies (same type/context) are included in the run; can also be overridden per request.

Config section: `PayoutAnomalyAlert` (e.g. in `appsettings.json` or environment).

---

## Duplicate-prevention strategy

- **Per (anomaly fingerprint, channel):** We consider an anomaly “already alerted” if there is at least one `PayoutAnomalyAlert` row with:
  - Same `AnomalyFingerprintId`
  - Same `Channel`
  - `Status = Sent`
  - `SentAtUtc` within the last `DuplicatePreventionHours` hours.
- **No per-recipient window:** The same anomaly is not re-sent on the same channel within the window regardless of recipient list. Recipient list can be overridden per run; deduplication is by fingerprint + channel + time only.
- **Failed sends:** Rows with `Status = Failed` do not suppress a retry; only `Sent` counts for the window. Retry count is stored for future use (e.g. backoff or cap).

---

## Channels supported

| Channel   | Status   | Notes                                                                 |
|----------|----------|-----------------------------------------------------------------------|
| **Email**| Supported| Single email per run to configured or request recipients; HTML table. |
| **Slack**| Not implemented | Can be added by implementing `IPayoutAnomalyAlertSender` and registering in DI. |
| **Telegram** | Not implemented | Same as Slack. |

Senders are registered in DI; the alert service iterates over `IEnumerable<IPayoutAnomalyAlertSender>` and calls each with the same filtered anomaly list and recipients. Adding a new channel does not require changes to payout or anomaly logic.

---

## Alert tracking (data model)

- **Table:** `PayoutAnomalyAlerts`
- **Fields:**  
  `Id`, `AnomalyFingerprintId`, `Channel`, `SentAtUtc`, `Status` (e.g. Sent / Failed / Pending), `RetryCount`, `ErrorMessage`, `RecipientId` (optional).
- **Use:**  
  - Decide “already alerted” in the last N hours (duplicate prevention).  
  - Show “Alerted” and “Last alerted at” on the anomaly list/detail (from latest `Sent` row per fingerprint).

---

## UI

- **Anomaly list:** Columns “Alerted” and “Last alerted” (or “Last alert time”) from merged alert data (`Alerted` boolean, `LastAlertedAt` UTC).
- **Run alerts:** Button “Run anomaly alerts” (permission: payout anomaly review) that calls the run-alerts API and shows a short result (e.g. “Alerts sent: N anomaly(ies)” or “No new anomalies”).

---

## Files changed

### Backend

- **Domain:** `backend/src/CephasOps.Domain/Rates/Entities/PayoutAnomalyAlert.cs` (new).
- **Infrastructure:**  
  - `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Rates/PayoutAnomalyAlertConfiguration.cs` (new).  
  - `ApplicationDbContext.cs`: `DbSet<PayoutAnomalyAlert>`, snapshot updated.  
  - `Migrations/20260308180000_AddPayoutAnomalyAlerts.cs` (new).  
  - `Migrations/ApplicationDbContextModelSnapshot.cs`: add `PayoutAnomalyAlert` entity.
- **Application:**  
  - `Rates/PayoutAnomalyAlertOptions.cs` (new).  
  - `Rates/PayoutAnomalyConstants.cs`: alert status and channel constants.  
  - `Rates/DTOs/PayoutAnomalyDto.cs`: `Alerted`, `LastAlertedAt`.  
  - `Rates/DTOs/PayoutAnomalyAlertDto.cs` (new): run request/result DTOs.  
  - `Rates/Services/IPayoutAnomalyAlertSender.cs`, `EmailPayoutAnomalyAlertSender.cs` (new).  
  - `Rates/Services/IPayoutAnomalyAlertService.cs`, `PayoutAnomalyAlertService.cs` (new).  
  - `Rates/Services/PayoutAnomalyService.cs`: merge alert info in `MergeReviewInfoAsync` (Alerted / LastAlertedAt).  
- **API:**  
  - `Program.cs`: register options and alert sender/service.  
  - `Controllers/PayoutHealthController.cs`: inject alert service; POST `run-anomaly-alerts`.  
- **Scripts:** `backend/scripts/add-payout-anomaly-alerts-table.sql` (idempotent create table).

### Frontend

- **Types:** `frontend/src/types/payoutHealth.ts`: `alerted`, `lastAlertedAt` on anomaly DTO; `RunPayoutAnomalyAlertsRequestDto`, `RunPayoutAnomalyAlertsResultDto`.
- **API:** `frontend/src/api/payoutHealth.ts`: `postRunAnomalyAlerts`.
- **UI:** `frontend/src/pages/reports/PayoutAnomaliesPage.tsx`: “Alerted” column, “Last alerted” display, “Run anomaly alerts” button and result message.

### Docs

- **Design:** `docs/PAYOUT_ANOMALY_ALERTING.md` (this file).

---

## Confirmation: payout logic unchanged

- **Payout calculation:** No code paths that compute order payouts, snapshots, or payroll amounts were modified. Alerting only **reads** anomaly results.
- **Anomaly detection:** All rules and thresholds remain in `PayoutAnomalyService` and related constants; no changes. The alert service calls the same `GetAnomaliesAsync` used by the dashboard and applies only severity/filtering and duplicate-prevention.
- **Payroll:** No automatic hold or block. Alerts are notifications only; any future payroll hold would be a separate, explicit feature.

---

## Deployment note

If migrations are applied via idempotent SQL (e.g. as in AGENTS.md), ensure the `PayoutAnomalyAlerts` table exists. You can run `backend/scripts/add-payout-anomaly-alerts-table.sql` or include the equivalent in your idempotent migration script.
