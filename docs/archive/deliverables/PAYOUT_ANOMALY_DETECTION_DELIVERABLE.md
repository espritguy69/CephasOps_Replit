# Payout Anomaly Detection — Deliverable

## Executive summary

A **read-only anomaly detection layer** has been added on top of existing payout snapshot and earnings data. Operations and finance can use it to quickly spot suspicious payout patterns or pricing misconfiguration. **No payout calculation logic, snapshot math, or payroll behaviour was changed**; detection is monitoring/flagging only.

- **Backend:** `IPayoutAnomalyService` / `PayoutAnomalyService` run a set of explainable rules over snapshot and P&amp;L data and return anomalies and summary counts.
- **API:** Read-only endpoints under `GET /api/payout-health`: `anomaly-summary`, `anomalies`, `anomaly-clusters`.
- **UI:** **Payout Anomalies** page (Reports → Payout Anomalies) with summary cards, filters, recent anomalies table, and top clusters; drill-through links to order payout breakdown, Rate Designer, and Payout Health Dashboard.

---

## Anomaly rules implemented

| Rule | Description | Severity |
|------|-------------|----------|
| **A. High payout vs peer** | Payout &gt; 2× average for same RateGroup + ServiceProfile/OrderCategory + InstallationMethod + PayoutPath | High/Medium by multiple |
| **B. Excessive custom overrides** | Installer has custom override on more than N completed jobs in last 30 days (N=5) | Medium |
| **C. Excessive legacy fallback** | Context (CompanyId + OrderTypeId) uses legacy fallback above threshold in last 30 days (10) | Medium |
| **D. Repeated warnings** | Same installer has resolution warnings above threshold in last 30 days (3) | Low |
| **E. Zero payout** | Completed snapshot with FinalPayout = 0 | Medium |
| **F. Negative margin cluster** | Same context has more than N negative-margin jobs in last 30 days (3) | High |
| **G. Installer deviation** | Installer average payout for similar jobs deviates &gt; 50% above peer average | High/Medium by ratio |

Rules are transparent and support-friendly; each anomaly includes a `reason` and context (order, installer, path, baseline, etc.).

---

## Default thresholds (configurable later)

Defined in `PayoutAnomalyConstants.cs` / `PayoutAnomalyThresholds`:

| Threshold | Default | Description |
|-----------|---------|-------------|
| HighPayoutMultipleOfPeer | 2.0 | Flag when payout &gt; peer average × this |
| ExcessiveCustomOverrideCount | 5 | Max custom overrides per installer in lookback |
| ExcessiveLegacyFallbackCount | 10 | Max legacy fallback per context in lookback |
| RepeatedWarningsCount | 3 | Max jobs with warnings per installer in lookback |
| NegativeMarginClusterCount | 3 | Max negative-margin jobs per context in lookback |
| InstallerDeviationAbovePeerPercent | 0.5 | Flag when installer avg &gt; peer × (1 + this) |
| LookbackDays | 30 | Window for time-based rules |

---

## Backend files added

| File | Purpose |
|------|--------|
| `Application/Rates/PayoutAnomalyConstants.cs` | Anomaly types, severity, thresholds |
| `Application/Rates/DTOs/PayoutAnomalyDto.cs` | PayoutAnomalyDto, PayoutAnomalyDetectionSummaryDto, PayoutAnomalyClusterDto, PayoutAnomalyFilterDto, PayoutAnomalyListResultDto |
| `Application/Rates/Services/IPayoutAnomalyService.cs` | GetAnomalySummaryAsync, GetAnomaliesAsync, GetTopClustersAsync |
| `Application/Rates/Services/PayoutAnomalyService.cs` | Rule execution over snapshot rows + PnlDetailPerOrder; summary aggregation; list filter + page; cluster building |

### Backend files modified

| File | Change |
|------|--------|
| `Api/Program.cs` | Register `IPayoutAnomalyService` / `PayoutAnomalyService` |
| `Api/Controllers/PayoutHealthController.cs` | Inject anomaly service; add GET `anomaly-summary`, `anomalies`, `anomaly-clusters` with query params (from, to, installerId, anomalyType, severity, payoutPath, companyId, page, pageSize, top) |
| `Api/Controllers/LogsController.cs` | Fix return type for GetSecurityActivity (pre-existing build fix) |

---

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/payout-health/anomaly-summary` | Summary counts by anomaly type and severity (optional: from, to, installerId, anomalyType, severity, payoutPath, companyId) |
| GET | `/api/payout-health/anomalies` | Paged list of anomalies (same filters + page, pageSize) |
| GET | `/api/payout-health/anomaly-clusters` | Top clusters (e.g. installers with most custom overrides; from, to, top) |

All read-only; no payload or payroll impact.

---

## Frontend files added

| File | Purpose |
|------|--------|
| `pages/reports/PayoutAnomaliesPage.tsx` | Payout Anomalies page: summary cards, filters (date, type, severity), recent anomalies table, top clusters, links to dashboard / Rate Designer / order |

### Frontend files modified

| File | Change |
|------|--------|
| `types/payoutHealth.ts` | PayoutAnomalyDto, PayoutAnomalyDetectionSummaryDto, PayoutAnomalyClusterDto, PayoutAnomalyFilterParams, PayoutAnomalyListResultDto |
| `api/payoutHealth.ts` | getPayoutAnomalySummary, getPayoutAnomalies, getPayoutAnomalyClusters with filter params |
| `App.tsx` | Route `/reports/payout-health/anomalies` → PayoutAnomaliesPage |
| `components/layout/Sidebar.tsx` | Reports: "Payout Anomalies" link to `/reports/payout-health/anomalies` |
| `pages/reports/PayoutHealthDashboardPage.tsx` | "View Payout Anomalies →" link in anomaly summary section |

---

## Confirmation: payout logic unchanged

- **RateEngineService / payout calculation:** Not modified.
- **OrderPayoutSnapshot creation / snapshot math:** Not modified.
- **Payroll / payroll hold:** No automatic block or hold; anomaly detection is read-only. No payroll logic was added or changed.

---

## Follow-up TODOs (payout anomaly governance)

Planned for a later phase:

- Allow finance/admin to **acknowledge** an anomaly.
- Add **notes/comment trail** per anomaly.
- Mark **false positive** vs **confirmed issue**.
- **Link anomaly** to pricing change or override review.
- Optional **alerting** (email/Slack) for high-severity anomalies.
- Optional **payroll hold recommendation** for confirmed anomalies (still no automatic block in this phase).

These are documented in the codebase (e.g. TODO comments or product backlog) and are out of scope for this read-only monitoring deliverable.
