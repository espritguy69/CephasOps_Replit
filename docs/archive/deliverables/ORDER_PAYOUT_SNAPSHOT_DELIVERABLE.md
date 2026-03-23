# Immutable Payout Snapshots — Deliverable

## Executive summary

**Immutable payout snapshots** are now persisted when an order reaches a completed state (`OrderCompleted` or `Completed`). The snapshot stores the full payout calculation (base amount, modifier trace, final payout, resolution path, and trace) so that historical payouts remain stable even if pricing rules change later. The **Installer Payout Breakdown** page prefers the stored snapshot when present and falls back to live resolution; a badge shows **"Snapshot"** or **"Live calculation"**.

- **Payout calculation logic:** Unchanged. RateEngineService is not modified; the snapshot captures the result **after** resolution.
- **Snapshot:** Written once when the order is completed; no update path (immutable).
- **API:** New endpoint `GET /api/orders/{id}/payout-snapshot` returns snapshot if exists, else resolves live; response includes `source: "Snapshot" | "Live"` and the same result shape as rate resolution.

---

## Entity design: OrderPayoutSnapshot

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | PK |
| OrderId | Guid | Order (job) ID; unique index (one snapshot per order) |
| CompanyId | Guid? | Company at time of snapshot |
| InstallerId | Guid? | Assigned SI at time of snapshot |
| RateGroupId | Guid? | Matched rate group (when path is BaseWorkRate) |
| BaseWorkRateId | Guid? | Matched base work rate ID |
| ServiceProfileId | Guid? | Matched service profile (when used) |
| CustomRateId | Guid? | Matched custom rate (when path is CustomOverride) |
| LegacyRateId | Guid? | Matched legacy SI job rate (when path is Legacy) |
| BaseAmount | decimal? | Base amount before modifiers |
| ModifierTraceJson | string (text) | JSON array of modifier steps |
| FinalPayout | decimal | Final payout amount |
| Currency | string(3) | e.g. MYR |
| ResolutionMatchLevel | string(64) | Custom \| ExactCategory \| ServiceProfile \| BroadRateGroup \| Legacy |
| PayoutPath | string(64) | CustomOverride \| BaseWorkRate \| Legacy |
| ResolutionTraceJson | string (text) | JSON: steps + warnings |
| CalculatedAt | DateTime (UTC) | When the calculation was performed |

**Immutability:** No update or delete in application logic; insert only. One row per order (unique index on OrderId).

---

## Migration impact

- **New table:** `OrderPayoutSnapshots` with columns above and unique index on `OrderId`.
- **Migration file:** `20260309120000_AddOrderPayoutSnapshot.cs`.
- **Apply:** Run `dotnet ef database update` from the API project (or apply the migration SQL). If you use a design-time snapshot for future migrations, run `dotnet ef migrations add` once after this to refresh the model snapshot if needed.

---

## Files changed

### Backend (created)

| File | Purpose |
|------|--------|
| `Domain/Rates/Entities/OrderPayoutSnapshot.cs` | Entity (no CompanyScopedEntity; immutable) |
| `Infrastructure/Persistence/Configurations/Rates/OrderPayoutSnapshotConfiguration.cs` | EF configuration, unique OrderId |
| `Infrastructure/Persistence/Migrations/20260309120000_AddOrderPayoutSnapshot.cs` | Migration Up/Down |
| `Application/Rates/DTOs/OrderPayoutSnapshotDto.cs` | OrderPayoutSnapshotDto, OrderPayoutSnapshotResponseDto |
| `Application/Rates/Services/IOrderPayoutSnapshotService.cs` | Interface: create, get by order, get payout with snapshot or live |
| `Application/Rates/Services/OrderPayoutSnapshotService.cs` | Implementation: map resolution result ↔ snapshot, JSON trace |

### Backend (modified)

| File | Change |
|------|--------|
| `Infrastructure/Persistence/ApplicationDbContext.cs` | DbSet\<OrderPayoutSnapshot\> |
| `Application/Orders/Services/OrderService.cs` | Inject IOrderPayoutSnapshotService; after successful status change to OrderCompleted or Completed, call CreateSnapshotForOrderIfEligibleAsync (in integrations block, non-fatal on failure) |
| `Api/Program.cs` | Register IOrderPayoutSnapshotService / OrderPayoutSnapshotService |
| `Api/Controllers/OrdersController.cs` | Inject IOrderPayoutSnapshotService; add GET `{id}/payout-snapshot` returning OrderPayoutSnapshotResponseDto |

### Frontend (modified)

| File | Change |
|------|--------|
| `api/orders.ts` | Add `OrderPayoutSnapshotResponse`, `getOrderPayoutSnapshot(orderId, params)` |
| `pages/operations/InstallerPayoutBreakdownPage.tsx` | Use `getOrderPayoutSnapshot` instead of `getOrderPayoutBreakdown`; store `source`; pass `source` to panel |
| `components/installer-payout/InstallerPayoutBreakdownPanel.tsx` | Add optional prop `source?: 'Snapshot' \| 'Live' \| null`; render badge "Snapshot" or "Live calculation" when present |

---

## How snapshot creation is triggered

1. **Trigger:** When order status is changed (via workflow) to **OrderCompleted** or **Completed**.
2. **Where:** In `OrderService.ChangeOrderStatusAsync`, after the workflow execution succeeds and after the existing integrations (SLA, automation rules, delivery order, escalation). A new step (5) calls `_orderPayoutSnapshotService.CreateSnapshotForOrderIfEligibleAsync(orderId, cancellationToken)`.
3. **Behaviour:** Create is **idempotent**: if a snapshot already exists for the order, the service returns without doing anything. Otherwise it loads the order, calls `IOrderProfitabilityService.GetOrderPayoutBreakdownAsync` (same resolution as payout-breakdown), and if resolution succeeds it maps the result to `OrderPayoutSnapshot` and saves. Failures are logged and do **not** fail the status change.
4. **No domain event or background job:** Implemented as a direct service call in the same request to keep the change minimal and avoid new infrastructure.

---

## API behaviour

- **GET /api/orders/{id}/payout-snapshot**  
  - Access control: same as order (department scope, company).  
  - If a snapshot exists for the order: returns `{ source: "Snapshot", result: GponRateResolutionResult }` (result built from snapshot).  
  - If no snapshot: resolves live via existing payout breakdown logic and returns `{ source: "Live", result: GponRateResolutionResult }`.  
  - Response shape is the same as the existing payout-breakdown endpoint plus `source`; the Installer Payout Breakdown page uses this single endpoint and shows the badge from `source`.

---

## Confirmation: pricing logic unchanged

- **RateEngineService:** Not modified. No changes to resolution math or logic.
- **OrderProfitabilityService.GetOrderPayoutBreakdownAsync:** Not modified; used as-is to produce the result that is then copied into the snapshot.
- **Snapshot:** Captures the **output** of the existing resolution only; it is not used in any calculation path. Reading a snapshot only reconstructs a result DTO for display.

---

## Success criteria (met)

- Historical payouts remain stable after rate changes: completed orders use the stored snapshot when available.
- Finance and support can audit payouts using the stored snapshot (base, modifiers, path, trace) via the Installer Payout Breakdown page and the payout-snapshot API.
- Snapshot is created when the order/job reaches a completed state (OrderCompleted or Completed).
- Snapshot is immutable once saved (no update API; one row per order).
- UI prefers snapshot and shows a clear "Snapshot" vs "Live calculation" badge.
