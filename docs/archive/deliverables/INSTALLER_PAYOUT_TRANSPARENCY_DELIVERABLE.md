# Installer Payout Transparency — Deliverable

## Executive summary

A new **Operations → Installer Payout Breakdown** flow gives support, finance, and installers a clear view of how payouts are calculated for real jobs. Users can look up an order by ID to see base work rate, modifiers applied, final payout, and resolution details (matched rule, rate group, service profile, warnings). An optional **Installer earnings summary** shows total jobs, total payout, and average payout per installer.

- **Payout logic:** Unchanged. The feature uses the existing `RateEngineService` resolution; no changes to calculation or production data.
- **Data source:** Read-only. Order payout breakdown is computed on demand via the same resolution used at payroll time; earnings summary uses existing job earning records.

---

## Files created

| File | Purpose |
|------|--------|
| `frontend/src/components/installer-payout/InstallerPayoutBreakdownPanel.tsx` | Renders base rate, modifiers, final payout, and job-level trace (matched rule, rate group, service profile, path, warnings). |
| `frontend/src/components/installer-payout/InstallerEarningsSummaryPanel.tsx` | Renders installer name, jobs completed, total payout, average payout from `JobEarningRecord[]`. |
| `frontend/src/components/installer-payout/index.ts` | Barrel export for installer-payout components. |
| `frontend/src/pages/operations/InstallerPayoutBreakdownPage.tsx` | Page: Order ID input, Load breakdown, optional installer selector and earnings summary. |
| `docs/INSTALLER_PAYOUT_TRANSPARENCY_DELIVERABLE.md` | This deliverable. |

---

## Files modified

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Pnl/Services/IOrderProfitabilityService.cs` | Added `GetOrderPayoutBreakdownAsync(Guid orderId, Guid? companyId, DateTime? referenceDate, CancellationToken)` returning `GponRateResolutionResult?`. |
| `backend/src/CephasOps.Application/Pnl/Services/OrderProfitabilityService.cs` | Implemented `GetOrderPayoutBreakdownAsync`: load order (with Partner), resolve SI level, build `GponRateResolutionRequest`, call `IRateEngineService.ResolveGponRatesAsync`, return result. |
| `backend/src/CephasOps.Api/Controllers/OrdersController.cs` | Added `GET api/orders/{id}/payout-breakdown` (optional `departmentId`, `referenceDate`). Uses existing order access control then `GetOrderPayoutBreakdownAsync`. |
| `frontend/src/api/orders.ts` | Added `getOrderPayoutBreakdown(orderId, params?)` and `GetOrderPayoutBreakdownParams`. |
| `frontend/src/App.tsx` | Route ` /operations/installer-payout-breakdown` → `InstallerPayoutBreakdownPage`. |
| `frontend/src/components/layout/Sidebar.tsx` | Added Operations item “Installer Payout Breakdown” (path `/operations/installer-payout-breakdown`, icon DollarSign). |

---

## Example payout breakdown (UI)

Example of what the installer payout breakdown shows:

```
Base Work Rate (Residential Fiber)   MYR 100.00
Installation Method modifier         +MYR 10.00    100.00 → 110.00
SI Tier modifier                     ×1.1          110.00 → 121.00
------------------------------------------
Final Installer Payout               MYR 121.00

Resolution details
  Matched rule: BaseWorkRate  ID: abc12345…
  Rate group: xyz98765…  Base work rate: def45678…
  Path: BaseWorkRate  Match level: ExactCategory
  Warnings: (if any)
```

- **Custom override path:** Shows “Custom override” and final amount only (no modifier list).
- **Job-level trace:** Matched rule, payout source, rate IDs, rate group / base work rate / service profile / legacy IDs as returned by the engine, plus path, match level, and warnings.

---

## Confirmation: pricing logic unchanged

- **RateEngineService:** Not modified. Resolution logic and formulas are unchanged.
- **Resolve endpoint:** Unchanged. New endpoint is `GET orders/{id}/payout-breakdown`, which builds a resolution request from the order and calls the same engine once.
- **Production tables:** No new writes. Breakdown is computed from existing order and rate data; earnings summary reads existing `JobEarningRecords`.
- **Payroll / payout behaviour:** Unchanged. This feature is read-only and does not affect how payouts are calculated or stored at payroll run time.

---

## Success criteria (met)

- Support, finance, and installers can see **why** a payout happened (base + modifiers + path).
- **How** modifiers affected the amount is visible (add/multiply steps with before/after).
- **How much** an installer earned is visible per order (breakdown) and optionally in aggregate (earnings summary).
- All of this is done without changing live payout calculation or production data.
