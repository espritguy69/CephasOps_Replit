# GPON Payout Snapshot Health & Anomaly Dashboard – Deliverable

## Executive summary

A **read-only** Payout Health Dashboard was added to give operations and finance visibility into:

- **Snapshot health:** How many completed orders have a payout snapshot vs are missing one, and coverage percentage.
- **Anomaly visibility:** Counts for legacy fallback, custom override, orders with resolution warnings, zero payout on completed orders, and negative margin (when P&L data exists).
- **Top unusual payouts:** Orders whose payout is more than 2× the average for the same rate group/path.
- **Recent snapshots:** Last 20 snapshots with order, amount, path, and calculated time.

No payout calculation, snapshot creation, or write logic was added or changed. All data comes from existing tables: `OrderPayoutSnapshots`, `Orders`, and `PnlDetailPerOrders`.

---

## Endpoints / queries added

### Backend (read-only)

| Item | Description |
|------|-------------|
| **GET /api/payout-health/dashboard** | Returns `PayoutHealthDashboardDto`: snapshot health, anomaly summary, top unusual payouts (up to 20), recent snapshots (up to 20). Requires `[Authorize]`. |
| **IPayoutHealthDashboardService.GetDashboardAsync** | Application service that runs only read queries (see below). |

### Queries used (all read-only)

1. **Snapshot health**
   - Count of orders with `Status` in (`Completed`, `OrderCompleted`).
   - Count of `OrderPayoutSnapshots` whose `OrderId` is in that completed set.
   - Derived: completed with snapshot, completed missing snapshot, total completed, coverage %.

2. **Anomaly summary**
   - From `OrderPayoutSnapshots`: counts where `PayoutPath == "Legacy"`, `"CustomOverride"`, `FinalPayout == 0`.
   - Orders with warnings: snapshots with non-null `ResolutionTraceJson` where JSON `warnings` array length > 0 (parsed in app).
   - Negative margin: count of `PnlDetailPerOrders` with `OrderId` in snapshot set and `ProfitForOrder < 0`.

3. **Top unusual payouts**
   - Load snapshots; group by `(RateGroupId, PayoutPath)`; compute average `FinalPayout` per group.
   - Keep snapshots where `FinalPayout > 2 * groupAverage`; sort by `FinalPayout` desc; take 20.

4. **Recent snapshots**
   - `OrderPayoutSnapshots` ordered by `CalculatedAt` desc, take 20, project to DTO.

---

## UI page / components added

| Location | Description |
|----------|-------------|
| **Route** | `/reports/payout-health` |
| **Page** | `frontend/src/pages/reports/PayoutHealthDashboardPage.tsx` |
| **Nav** | Main → “Payout Health” (sidebar), plus “Dashboards” section on Reports Hub with “Payout Health” card. |
| **API** | `frontend/src/api/payoutHealth.ts` – `getPayoutHealthDashboard()`. |
| **Types** | `frontend/src/types/payoutHealth.ts` – DTOs aligned with backend. |

### Page contents

- **Snapshot health:** Cards for Coverage %, Completed with snapshot, Completed missing snapshot, Total completed.
- **Anomaly summary:** Cards for Legacy fallback, Custom override, Orders with warnings, Zero payout (completed), Negative margin (P&L).
- **Top unusual payouts:** Table – Order (link), Final payout, Path, Group avg, Multiple of avg, Calculated at.
- **Recent snapshots:** Table – Order (link), Final payout, Path, Calculated at.
- Refresh button; loading skeletons; error state.

---

## Anomaly rules used

| Rule | Source | Definition |
|------|--------|------------|
| **Legacy fallback** | `OrderPayoutSnapshots.PayoutPath` | `PayoutPath == "Legacy"`. |
| **Custom override** | `OrderPayoutSnapshots.PayoutPath` | `PayoutPath == "CustomOverride"`. |
| **Orders with warnings** | `OrderPayoutSnapshots.ResolutionTraceJson` | JSON has `warnings` array with at least one element. |
| **Zero payout (completed)** | `OrderPayoutSnapshots.FinalPayout` | `FinalPayout == 0` (snapshot exists ⇒ order was completed at snapshot time). |
| **Negative margin** | `PnlDetailPerOrders` joined to snapshot orders | `ProfitForOrder < 0` (only when P&L data exists). |
| **Top unusual payouts** | `OrderPayoutSnapshots` grouped by rate group/path | `FinalPayout > 2 × average(FinalPayout)` for same `(RateGroupId, PayoutPath)`; top 20 by `FinalPayout`. |

---

## Confirmation: payout logic unchanged

- **No changes** to:
  - `IOrderPayoutSnapshotService` / `OrderPayoutSnapshotService`
  - `IOrderProfitabilityService` or any rate/payout calculation
  - Snapshot creation (e.g. in `OrderService.ChangeOrderStatusAsync`)
  - Any write path to `OrderPayoutSnapshots` or orders
- **New code** is read-only:
  - `PayoutHealthController`: single GET that returns dashboard DTO.
  - `PayoutHealthDashboardService`: only reads from `Orders`, `OrderPayoutSnapshots`, `PnlDetailPerOrders`.
- Snapshot immutability and existing behaviour are unchanged.

---

## Files touched (summary)

**Backend**

- `backend/src/CephasOps.Application/Rates/DTOs/PayoutHealthDashboardDto.cs` (new)
- `backend/src/CephasOps.Application/Rates/Services/IPayoutHealthDashboardService.cs` (new)
- `backend/src/CephasOps.Application/Rates/Services/PayoutHealthDashboardService.cs` (new)
- `backend/src/CephasOps.Api/Controllers/PayoutHealthController.cs` (new)
- `backend/src/CephasOps.Api/Program.cs` (register `IPayoutHealthDashboardService`)

**Frontend**

- `frontend/src/types/payoutHealth.ts` (new)
- `frontend/src/api/payoutHealth.ts` (new)
- `frontend/src/pages/reports/PayoutHealthDashboardPage.tsx` (new)
- `frontend/src/App.tsx` (route `/reports/payout-health`)
- `frontend/src/components/layout/Sidebar.tsx` (nav item “Payout Health”)
- `frontend/src/pages/reports/ReportsHubPage.tsx` (Dashboards section + Payout Health card)

**Docs**

- `docs/PAYOUT_HEALTH_DASHBOARD_DELIVERABLE.md` (this file)
