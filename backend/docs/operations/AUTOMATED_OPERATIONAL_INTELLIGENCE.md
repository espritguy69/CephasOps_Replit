# Automated Operational Intelligence

**Purpose:** Production-safe, rule-based operational intelligence layer that detects and surfaces patterns (installer risk, building/site risk, order risk, tenant risk, SLA/delay risk) so operations teams can act before problems escalate. Read-heavy, tenant-safe, and explainable.

## Overview

The intelligence layer sits on top of existing SaaS isolation, dashboards, and event bus. It does **not** replace or duplicate:

- Multi-tenant SaaS isolation
- Financial isolation
- Platform observability dashboard
- OperationalInsightsService (Operations Control, Risk & Quality, etc.)
- Tenant activity timeline or anomaly events

It **adds**:

- **Explainable rule engine** that flags orders, installers, buildings, and tenants with risk signals and clear reasons
- **Tenant-scoped API** for summary, orders-at-risk, installers-at-risk, buildings-at-risk, tenant-risk-signals
- **Optional platform summary** (admin-only) for aggregate counts across tenants
- **Operational Intelligence** dashboard under Command Center in the frontend

## Data Sources (Phase 1 Discovery)

| Source | Tenant-scoped | Use in intelligence |
|--------|----------------|---------------------|
| **Orders** | Yes (CompanyId) | Status, UpdatedAt, AssignedSiId, RescheduleCount, BuildingId, KpiDueAt, CreatedAt |
| **OrderBlockers** | Yes | Count per order/installer/building; blocker themes |
| **OrderMaterialReplacements** | Yes | Count per order/installer/building; replacement-heavy rules |
| **OrderReschedule** | Yes | Reschedule count on order (also Order.RescheduleCount) |
| **OrderStatusLog** | Yes | Last activity for “silent” order rule |
| **ServiceInstallers** | Yes | Active installers; risk signals per installer |
| **Buildings** | Yes | Building/site grouping for recurrence rules |
| **SlaBreaches** | Yes | Already used in Operations Control; KpiDueAt for “nearing breach” |
| **TenantActivityEvents** | By TenantId | Evidence source; not yet wired for persistence in v1 |
| **TenantAnomalyEvents** | By TenantId | Existing anomalies (StuckOrdersAnomaly, etc.); optional future: write critical intelligence signals |
| **TenantMetricsDaily** | By TenantId | Health score; tenant risk can combine with order patterns |
| **JobEarningRecords** | Yes | Not used in v1 (financial; read-only signals only where already exposed) |

All intelligence queries are **read-only** and **company-scoped** for tenant endpoints. Platform summary uses `TenantScopeExecutor.RunWithPlatformBypassAsync` for aggregation only and returns safe counts, not tenant business data.

## Implemented Rules

### Order risk signals

- **StuckOrder:** AssignedSiId set, status ≠ Completed, UpdatedAt older than configured threshold (default 4h). Severity: Critical.
- **LikelyStuckSoon:** Same as above but within “likely stuck soon” window (default 75% of threshold). Severity: Warning.
- **RescheduleHeavy:** RescheduleCount ≥ threshold (default 2). Severity: Warning.
- **BlockerAccumulation:** Blocker count on order ≥ threshold (default 2). Severity: Warning.
- **ReplacementHeavy:** Material replacement count on order ≥ threshold (default 2). Severity: Warning.
- **SilentOrder:** No status log activity for longer than threshold (default 6h) with installer assigned. Severity: Info.
- **SlaNearingBreach:** KpiDueAt set and elapsed time ≥ configured % of (KpiDueAt − CreatedAt). Severity: Warning.

### Installer risk signals

- **InstallerRepeatedBlockers:** Blocker count (by RaisedBySiId) in lookback (default 30 days) ≥ threshold (default 3). Severity: Warning/Critical by count.
- **InstallerHighReplacements:** Replacement count (by ReplacedBySiId) in lookback ≥ threshold (default 5). Severity: Warning/Critical by count.
- **InstallerStuckOrders:** Count of currently stuck orders assigned to installer. Severity: Warning/Critical by count.
- **InstallerIssueRatio:** Issue count (blockers + replacements) on last N assigned orders above ratio vs baseline. Severity: Warning.

### Building/site risk signals

- **BuildingRepeatedBlockers:** Number of orders at same BuildingId with blockers ≥ threshold (default 3). Severity: Warning.
- **BuildingRepeatedReplacements:** Replacement count at same building in lookback ≥ threshold (default 4). Severity: Warning/Critical by count.

### Tenant risk signals

- **TenantStuckSpike:** Stuck orders count ≥ threshold (default 5). Severity: Warning/Critical by count.
- **TenantAbnormalReplacementRatio:** Replacements this month / completed orders this month ≥ threshold (default 15%). Severity: Warning.

## Explanation Model

Every signal includes:

- **RuleCode:** e.g. `StuckOrder`, `InstallerRepeatedBlockers`
- **Summary:** Short human-readable reason
- **Detail:** Optional extra context (counts, thresholds, timestamps)
- **SourceCount:** Evidence count where applicable
- **Severity:** Info | Warning | Critical

No black-box scoring. All flags are deterministic and documented.

## Severity Thresholds (Configurable)

Configured via `OperationalIntelligenceOptions` (section `OperationalIntelligence` in appsettings). Defaults:

| Option | Default | Description |
|--------|---------|-------------|
| StuckOrderThresholdHours | 4 | Hours without update → stuck |
| LikelyStuckSoonPercentOfThreshold | 0.75 | % of threshold → “likely stuck soon” |
| ReplacementHeavyPerOrderThreshold | 2 | Replacements on order → replacement-heavy |
| ReplacementLookbackDays | 30 | Lookback for installer/building replacement counts |
| InstallerReplacementCountThreshold | 5 | Replacements by installer in lookback → flag |
| InstallerBlockerCountThreshold | 3 | Blockers by installer in lookback → flag |
| InstallerPeerWindowSize | 20 | Last N orders for issue ratio |
| InstallerIssueRatioThreshold | 0.25 | Issue ratio above this → flag |
| RescheduleHeavyThreshold | 2 | RescheduleCount on order → flag |
| OrderBlockerCountThreshold | 2 | Blockers on order → flag |
| SilentOrderThresholdHours | 6 | No status log for this long → silent |
| BuildingRecurrenceThreshold | 3 | Orders with blockers at building → flag |
| BuildingReplacementThreshold | 4 | Replacements at building in lookback → flag |
| TenantStuckOrdersAnomalyThreshold | 5 | Stuck count → tenant risk |
| TenantAbnormalReplacementRatioThreshold | 0.15 | Replacement ratio in month → tenant risk |
| SlaNearingBreachPercent | 0.85 | Elapsed % of SLA duration → nearing breach |
| MaxResultsPerList | 50 | Cap per list (orders/installers/buildings) |

## Permissions and Tenant Safety

- **Tenant endpoints** (`/api/insights/operational-intelligence/*`): Require company context via `RequireCompanyId(_tenantProvider)`. All queries filter by `CompanyId`. No cross-tenant data.
- **Platform summary** (`GET /api/insights/platform-operational-intelligence`): Admin only (`SuperAdmin,Admin` + `AdminTenantsView`). Uses `TenantScopeExecutor.RunWithPlatformBypassAsync` for read-only aggregation. Returns only aggregate counts (e.g. total orders at risk across tenants), not per-tenant business data.
- **Tenant isolation:** Unchanged. No new writes to tenant-scoped entities from the intelligence service. No weakening of TenantSafetyGuard or financial logic.

## API Endpoints

| Method | Path | Scope | Description |
|--------|------|--------|-------------|
| GET | `/api/insights/operational-intelligence/summary` | Tenant | Summary counts (orders/installers/buildings at risk, severity bands) |
| GET | `/api/insights/operational-intelligence/orders-at-risk` | Tenant | Orders with risk signals; optional `?severity=` |
| GET | `/api/insights/operational-intelligence/installers-at-risk` | Tenant | Installers with risk signals; optional `?severity=` |
| GET | `/api/insights/operational-intelligence/buildings-at-risk` | Tenant | Buildings with risk signals; optional `?severity=` |
| GET | `/api/insights/operational-intelligence/tenant-risk-signals` | Tenant | Tenant-level risk signals |
| GET | `/api/insights/platform-operational-intelligence` | Platform | Aggregated summary (admin only) |

## Frontend

- **Route:** `/insights/intelligence`
- **Menu:** Command Center → Operational Intelligence
- **Content:** Summary cards, at-risk orders table, at-risk installers table, at-risk buildings table, severity filter, expandable reason rows
- **Tenant:** Uses same company context as other dashboards; tenant switch invalidates data (reload on navigate or use query keys with company/department for future React Query)

## Integration with Existing Dashboards

- **Command Center:** New “Operational Intelligence” item; at-risk orders/installers/buildings live here.
- **Operations Control:** Continues to show stuck orders list from OperationalInsightsService; intelligence adds explainable reasons and more risk dimensions.
- **Risk & Quality:** Continues to show complaints, failures, reschedules; intelligence adds installer/building patterns and tenant-level signals.
- **Platform Health:** Optional: platform summary can be surfaced in admin views; no change to existing platform-health endpoint.

## Tenant Activity and Anomaly Events (Phase 7)

- **Current:** Intelligence is computed read-only; no new TenantAnomalyEvent writes in v1.
- **Existing:** OperationalInsightsService already raises TenantAnomalyEvent for StuckOrdersAnomaly, HighFailureRate, AbnormalMaterialReplacements.
- **Future:** Optionally persist critical intelligence signals (e.g. critical installer/building risk) as TenantAnomalyEvent with a dedicated Kind, with deduplication to avoid spam.

## Five SaaS Scaling Mistakes This Layer Helps Prevent

1. **Reacting too late to operational issues** — Stuck and “likely stuck soon” rules give early warning before orders pile up.
2. **Hiding recurring quality problems in raw dashboards** — Installer and building recurrence rules surface patterns (blockers, replacements) that are hard to see in flat lists.
3. **Missing installer or building issue patterns** — Explicit installer-risk and building-risk signals with reasons make recurring failure sites visible.
4. **Letting SLA breaches emerge without early warning** — “Nearing breach” and tenant stuck-spike rules give time to reallocate or escalate.
5. **Relying on manual ops review instead of explainable automated detection** — Rule-based, explainable signals reduce dependence on manual scanning of dashboards.

## Implementation Summary (Phase 1)

| Area | Created/Modified |
|------|------------------|
| **Backend services** | `IOperationalIntelligenceService`, `OperationalIntelligenceService` (rule engine), `OperationalIntelligenceOptions` |
| **DTOs** | `OperationalIntelligenceDto.cs`: IntelligenceExplanationDto, InstallerRiskSignalDto, BuildingRiskSignalDto, OrderRiskSignalDto, TenantRiskSignalDto, SlaDelayRiskDto, OperationalIntelligenceSummaryDto |
| **Endpoints** | Tenant: `GET .../operational-intelligence/summary`, `/orders-at-risk`, `/installers-at-risk`, `/buildings-at-risk`, `/tenant-risk-signals`. Platform: `GET .../platform-operational-intelligence` (admin) |
| **Frontend** | `api/operationalIntelligence.ts` (client + keys), `pages/insights/OperationalIntelligenceDashboard.tsx`, route `/insights/intelligence`, Command Center menu |
| **Rules implemented** | StuckOrder, LikelyStuckSoon, RescheduleHeavy, BlockerAccumulation, ReplacementHeavy, SilentOrder, SlaNearingBreach; InstallerRepeatedBlockers, InstallerHighReplacements, InstallerStuckOrders, InstallerIssueRatio; BuildingRepeatedBlockers, BuildingRepeatedReplacements; TenantStuckSpike, TenantAbnormalReplacementRatio |
| **Tests** | `OperationalIntelligenceApiTests` (9 tests: tenant 200/403, platform 200/403), `OperationalIntelligenceServiceTests` (9 tests: empty company throws, summary, stuck-order rule) |
| **Docs** | `AUTOMATED_OPERATIONAL_INTELLIGENCE.md`, OPERATIONAL_DASHBOARDS.md, SAAS_ARCHITECTURE_MAP.md, SAAS_REMEDIATION_CHANGELOG.md |

**Tenant isolation:** Unchanged. All tenant intelligence endpoints use `RequireCompanyId` and company-scoped queries. Platform summary uses `TenantScopeExecutor.RunWithPlatformBypassAsync` for read-only aggregation only.

## Future Enhancements

- **Phase 2 (suggested):** Live War Room map, SLA breach engine, proactive alert subscriptions.
- **Persistence:** Optional TenantAnomalyEvent for critical intelligence signals with rate limiting.
- **Thresholds UI:** Admin UI for OperationalIntelligenceOptions if needed.
- **More rules:** Repeating blocker themes, quality-risk trends over time, reschedule patterns by installer/building.
