# Payout Anomaly Response Tracking – Design and Deliverables

## Executive summary

**Response tracking** connects anomaly alerts with anomaly review status so operations can see whether alerted anomalies are being acted on. All changes are additive and read-only with respect to payout and detection; duplicate-prevention logic is unchanged.

- **Response states:** Alerted + Open, Alerted + Acknowledged, Alerted + Investigating, Alerted + Resolved, Alerted + False positive, and **stale** (alerted, still open, no action after N hours).
- **Backend:** Read-only query support for alert response summary counts, stale alerted anomalies list, and average time to first action.
- **UI:** Alert response summary cards and stale section on Payout Anomalies; Last action time column; optional compact “Alert response” block on Payout Health dashboard.

---

## Response tracking model

We derive everything from existing data:

- **Alerted** = has at least one successful `PayoutAnomalyAlert` (Status = Sent) for the anomaly fingerprint.
- **Last alert time** = max(SentAtUtc) for that fingerprint.
- **Review status** = from `PayoutAnomalyReview` (Open, Acknowledged, Investigating, Resolved, FalsePositive).
- **Last action time** = `PayoutAnomalyReview.UpdatedAt ?? CreatedAt` (last acknowledge/assign/resolve/comment).

**Response states (all “alerted”):**

| State | Description |
|-------|-------------|
| Alerted · Open | Alerted, review status Open or no review |
| Alerted · Acknowledged | Alerted, review status Acknowledged |
| Alerted · Investigating | Alerted, review status Investigating |
| Alerted · Resolved | Alerted, review status Resolved |
| Alerted · False positive | Alerted, review status FalsePositive |
| **Stale** | Alerted + (Open or no review) + last alert was more than **StaleThresholdHours** ago |

**Stale rule:** An anomaly is **stale** when:

- It has been **alerted** (at least one Sent alert), and  
- Review status is **Open** or there is **no review**, and  
- **LastAlertedAt + StaleThresholdHours < now** (UTC).

Default **StaleThresholdHours** = 24 (configurable in `PayoutAnomalyAlert:StaleThresholdHours`).

---

## Backend

- **LastActionAt:** Added to `PayoutAnomalyDto`; set in `MergeReviewInfoAsync` from `review.UpdatedAt ?? review.CreatedAt`.
- **AlertResponseSummaryDto:** AlertedOpen, AlertedAcknowledged, AlertedInvestigating, AlertedResolved, AlertedFalsePositive, StaleCount, AverageTimeToFirstActionMinutes.
- **IPayoutAnomalyResponseTrackingService:**
  - `GetAlertResponseSummaryAsync(filter)` – counts by status and stale; average time from last alert to first action (for anomalies that have been acted on).
  - `GetStaleAlertedAnomaliesAsync(filter, limit)` – list of stale anomalies (oldest alert first), up to limit.
- **API:** GET `api/payout-health/alert-response-summary` (query: from, to, installerId, anomalyType, severity, companyId), GET `api/payout-health/stale-alerted-anomalies` (query: from, to, limit).

Summary and stale list are computed from the same merged anomaly set (up to 500 items by filter). No new tables; no changes to payout or alert logic.

---

## UI

- **Payout Anomalies page**
  - **Alert response** section: summary cards (Alerted · Open, Alerted · Acknowledged, Alerted · Investigating, Alerted · Resolved, Alerted · False +, Stale, Avg time to first action).
  - **Stale alerted anomalies** section: table of stale items (severity, type, last alert, reason, order) when any exist.
  - **Table columns:** Alerted, Last alert time, Review status, Assigned to, **Last action** (new), plus existing columns.
- **Payout Health dashboard**
  - **Alert response** block: Alerted · Open, Stale count, Avg time to first action, and “View anomalies →” link.

---

## Files changed

### Backend

- **Application:**  
  - `Rates/PayoutAnomalyAlertOptions.cs`: added `StaleThresholdHours` (default 24).  
  - `Rates/DTOs/PayoutAnomalyDto.cs`: added `LastActionAt`.  
  - `Rates/DTOs/PayoutAnomalyAlertDto.cs`: added `AlertResponseSummaryDto`.  
  - `Rates/Services/PayoutAnomalyService.cs`: merge `LastActionAt` in `MergeReviewInfoAsync`.  
  - `Rates/Services/IPayoutAnomalyResponseTrackingService.cs`, `PayoutAnomalyResponseTrackingService.cs` (new).
- **API:**  
  - `Program.cs`: register `IPayoutAnomalyResponseTrackingService`.  
  - `PayoutHealthController.cs`: inject response tracking service; GET `alert-response-summary`, GET `stale-alerted-anomalies`.

### Frontend

- **types/payoutHealth.ts:** `lastActionAt` on `PayoutAnomalyDto`; `AlertResponseSummaryDto`.
- **api/payoutHealth.ts:** `getAlertResponseSummary`, `getStaleAlertedAnomalies`.
- **PayoutAnomaliesPage.tsx:** Alert response summary cards, stale anomalies section, Last action column; queries for summary and stale list.
- **PayoutHealthDashboardPage.tsx:** Alert response block (compact) with link to anomalies.

### Docs

- **docs/PAYOUT_ANOMALY_RESPONSE_TRACKING.md** (this file).

---

## Response states shown

- Alerted · Open  
- Alerted · Acknowledged  
- Alerted · Investigating  
- Alerted · Resolved  
- Alerted · False positive  
- Stale (no action after threshold)  
- Avg time to first action (minutes)

---

## Stale rule used

**Stale** = anomaly is **alerted** AND (review status is **Open** or **no review**) AND **LastAlertedAt** is older than **StaleThresholdHours** (default 24).  
Config key: `PayoutAnomalyAlert:StaleThresholdHours`.

---

## Confirmation: payout logic unchanged

- **Payout calculation:** Not modified.  
- **Anomaly detection:** Not modified.  
- **Alert duplicate-prevention:** Not modified.  
- All new behaviour is read-only aggregation and display over existing anomaly, review, and alert data.
