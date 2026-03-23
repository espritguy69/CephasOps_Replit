# SLA Breach Engine

**Purpose:** Production-safe, rule-based SLA breach detection so operations can see which orders are nearing breach or already breached. Uses **Order.KpiDueAt** as the authoritative due time. Read-only, tenant-safe, explainable.

## Overview

The SLA Breach Engine is the next phase after Operational Intelligence. It moves from “orders likely at risk” to explicit **nearing-breach** and **breached** classification based on real due times.

- **NoSla** — Order has no KPI due time set.
- **OnTrack** — KpiDueAt is in the future and beyond the “nearing” threshold.
- **NearingBreach** — KpiDueAt is within the configured threshold (e.g. 30 minutes).
- **Breached** — Current time is past KpiDueAt; severity is Warning or Critical based on how long overdue.

All signals are explainable (e.g. “Order is in SLA breach: KPI due time passed 95 minutes ago.”).

## Data Sources

| Source | Use |
|--------|-----|
| **Order.KpiDueAt** | Authoritative due time. If null → NoSla. |
| **Order.Status** | Active = not Completed, Cancelled, Rejected; only active orders are classified. |
| **Order.UpdatedAt** | Fallback for last activity when no status log. |
| **OrderStatusLog** | Last activity time (max CreatedAt per order). |
| **OrderBlockers / OrderMaterialReplacements / RescheduleCount** | HasBlocker, HasReplacement, HasReschedule on each item (informational). |
| **SlaProfile / SlaRule** | Not used for classification in v1; KpiDueAt is the single source. SlaBreach table records from SlaEvaluationService remain separate. |

If an order has no valid SLA context (no KpiDueAt), it is classified as **NoSla** and never guessed.

## SLA States and Severity

| State | Condition | Severity |
|-------|-----------|----------|
| NoSla | KpiDueAt is null | Info |
| OnTrack | now < KpiDueAt and minutes to due > NearingBreachMinutes | Info |
| NearingBreach | now < KpiDueAt and minutes to due ≤ NearingBreachMinutes | Warning |
| Breached | now ≥ KpiDueAt | Warning when overdue < BreachedCriticalOverdueMinutes; Critical when overdue ≥ that |

## Thresholds (OperationalSlaOptions)

Section: `OperationalSla`.

| Option | Default | Description |
|--------|---------|-------------|
| NearingBreachMinutes | 30 | Order is “nearing breach” when due within this many minutes. |
| BreachedCriticalOverdueMinutes | 120 | Overdue ≥ this → Critical; else Warning. |
| MaxOrdersAtRisk | 100 | Cap on orders returned in orders-at-risk list. |

## API Endpoints

| Method | Path | Scope | Description |
|--------|------|--------|-------------|
| GET | `/api/insights/sla/summary` | Tenant | Distribution (OnTrack, NearingBreach, Breached, NoSla) for active orders. |
| GET | `/api/insights/sla/orders-at-risk` | Tenant | Orders in NearingBreach or Breached; optional `?breachState=` and `?severity=`. |
| GET | `/api/insights/platform-sla-summary` | Platform | Aggregate distribution across tenants (admin only). |

Tenant endpoints use `RequireCompanyId(_tenantProvider)` and company-scoped queries. Platform summary uses `TenantScopeExecutor.RunWithPlatformBypassAsync` for read-only aggregation.

## Frontend

- **Route:** `/insights/sla`
- **Menu:** Command Center → SLA Breach
- **Content:** Summary cards (On track, Nearing breach, Breached, No SLA), table of orders at risk with state, severity, due/overdue, explanation; filters by state and severity.
- **Operations Control:** Link “View SLA breach status (nearing / breached)” to `/insights/sla`.

## Permissions and Tenant Safety

- Tenant endpoints require company context; missing company → 403.
- All queries filter by CompanyId; no cross-tenant data.
- Platform summary is admin-only (SuperAdmin, Admin + AdminTenantsView); returns aggregate counts only.
- No writes to tenant-scoped entities; no change to financial logic or TenantSafetyGuard.

## Explanation Model

Every order item includes:

- **Explanation** — e.g. “Order is in SLA breach: KPI due time passed 95 minutes ago.” or “Order is nearing SLA breach: KPI due time is in 25 minutes (threshold 30 min).”
- **MinutesToDueOrOverdue** — Positive = minutes until due; negative = minutes overdue; null when NoSla.
- **BreachState**, **Severity** — As above.

## Future Phases

- Alert subscriptions (not in this pass).
- Optional persistence of critical SLA breaches as TenantAnomalyEvent (with deduplication).
- War Room integration; SlaProfile-based due time calculation (e.g. set KpiDueAt from profile when order is assigned).
- Business-hours-aware SLA windows if SlaProfile.ExcludeNonBusinessHours is used.

## Implementation Summary

| Area | Created/Modified |
|------|------------------|
| **Backend** | OperationalSlaOptions, SlaBreachDto (states, SlaBreachOrderItemDto, SlaBreachDistributionDto, SlaBreachSummaryDto), ISlaBreachService, SlaBreachService |
| **Endpoints** | SlaBreachController (summary, orders-at-risk), PlatformOperationalIntelligenceController (platform-sla-summary) |
| **Frontend** | api/slaBreach.ts, SlaBreachDashboard.tsx, route /insights/sla, Command Center menu, Operations Control link |
| **Tests** | SlaBreachApiTests (5), SlaBreachServiceTests (6) |

**Tenant isolation:** Unchanged. All tenant SLA endpoints use RequireCompanyId and company-scoped queries; platform summary is read-only aggregation only.
