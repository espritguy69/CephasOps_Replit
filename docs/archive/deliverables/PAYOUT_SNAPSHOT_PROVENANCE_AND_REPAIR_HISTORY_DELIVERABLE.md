# Snapshot Provenance and Repair-Run History – Deliverable

## Executive summary

Provenance and repair-run history were added so operations can:

- **Distinguish** snapshots created during normal completion flow from those created by the repair job (or backfill).
- **Audit** repair runs via a persisted history (latest run summary and recent runs table on the Payout Health Dashboard).

All changes are **additive**: existing snapshots are left unchanged and get a default provenance of **Unknown**. No payout calculation or snapshot math was changed.

---

## Entity changes

### 1. OrderPayoutSnapshot (additive)

| Change | Description |
|--------|-------------|
| **New property** | `Provenance` (string, max 32). Values: `NormalFlow`, `RepairJob`, `Backfill`, `ManualBackfill`, `Unknown`. |
| **Default** | Existing rows and new rows without an explicit value use `Unknown`. Migration sets default `'Unknown'` for the new column. |

**Domain**

- `CephasOps.Domain.Rates.SnapshotProvenance` – static constants: `NormalFlow`, `RepairJob`, `Backfill`, `ManualBackfill`, `Unknown`.
- `OrderPayoutSnapshot.Provenance` with default `SnapshotProvenance.Unknown`.

### 2. PayoutSnapshotRepairRun (new)

New entity for repair-run history (read-only from app logic; only the scheduler writes).

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | PK |
| StartedAt | DateTime | Run start (UTC) |
| CompletedAt | DateTime? | Run end (UTC) |
| TotalProcessed | int | Orders considered |
| CreatedCount | int | Snapshots created |
| SkippedCount | int | Ineligible (e.g. no resolution) |
| ErrorCount | int | Orders that threw |
| ErrorOrderIdsJson | string? | JSON array of failed order IDs |
| TriggerSource | string | `Scheduler` or `Manual` |
| Notes | string? | Optional note (e.g. "No completed orders without snapshot") |

**Domain**

- `CephasOps.Domain.Rates.RepairRunTriggerSource` – `Scheduler`, `Manual`.
- `CephasOps.Domain.Rates.Entities.PayoutSnapshotRepairRun` – entity as above.

---

## Migration added

**File:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260310120000_AddSnapshotProvenanceAndRepairRunHistory.cs`

- **OrderPayoutSnapshots:** add column `Provenance` (varchar 32, NOT NULL, default `'Unknown'`).
- **PayoutSnapshotRepairRuns:** new table with columns above; index on `StartedAt` descending.

**Backfill handling**

- Existing snapshots are **not** updated. They receive the column default `'Unknown'`.
- **Documented meaning of Unknown:** snapshots created before provenance was introduced, or origin not recorded. Dashboard shows them as “Unknown (pre-provenance)”.

---

## How provenance is assigned

| Path | Provenance set |
|------|----------------|
| **OrderService.ChangeOrderStatusAsync** | Calls `CreateSnapshotForOrderIfEligibleAsync(orderId, ct)` with no third argument → default **NormalFlow**. |
| **MissingPayoutSnapshotRepairService** | Calls `CreateSnapshotForOrderIfEligibleAsync(orderId, ct, SnapshotProvenance.RepairJob)` → **RepairJob**. |
| **Future backfill / manual** | Call `CreateSnapshotForOrderIfEligibleAsync(..., SnapshotProvenance.Backfill)` or `ManualBackfill` as needed. |

**IOrderPayoutSnapshotService**

- `CreateSnapshotForOrderIfEligibleAsync(Guid orderId, CancellationToken cancellationToken = default, string? provenance = null)`.
- If `provenance` is null or empty, **NormalFlow** is used when creating the snapshot.

No existing call sites were required to pass a third argument; OrderService continues to get NormalFlow by default.

---

## Scheduler integration

**MissingPayoutSnapshotSchedulerService.RunRepairAsync**

- Before calling the repair service: record `StartedAt = DateTime.UtcNow`.
- After `DetectMissingPayoutSnapshotsAsync`: record `CompletedAt = DateTime.UtcNow`.
- Insert one **PayoutSnapshotRepairRun** per run with:
  - TotalProcessed, CreatedCount, SkippedCount, ErrorCount from `MissingPayoutSnapshotRepairResult`.
  - ErrorOrderIdsJson = JSON array of error order IDs (or null if none).
  - TriggerSource = `RepairRunTriggerSource.Scheduler`.
  - Notes = e.g. "No completed orders without snapshot" when TotalProcessed == 0.
- A run record is written **every** time the scheduler runs (including when there is nothing to process). Repair logic itself is unchanged.

---

## Dashboard updates

### Backend

- **PayoutSnapshotHealthDto:** added `NormalFlowCount`, `RepairJobCount`, `UnknownProvenanceCount`, `BackfillCount`, `ManualBackfillCount` (counts of snapshots by provenance).
- **PayoutHealthDashboardDto:** added `LatestRepairRun` (nullable) and `RecentRepairRuns` (last 10).
- **RepairRunSummaryDto:** Id, StartedAt, CompletedAt, TotalProcessed, CreatedCount, SkippedCount, ErrorCount, TriggerSource, Notes.
- **PayoutHealthDashboardService:**
  - Snapshot health now includes a grouped count by `Provenance`.
  - `GetLatestRepairRunAsync` and `GetRecentRepairRunsAsync(10)` read from `PayoutSnapshotRepairRuns`.

### Frontend (Payout Health Dashboard)

- **Snapshot provenance:** new row of cards under Snapshot health: Normal flow, Repaired later, Unknown (pre-provenance), Backfill, Manual backfill.
- **Latest repair run:** section with one card showing last run (Started, Completed, Processed, Created, Skipped, Errors, Trigger, Notes). Rendered only when `latestRepairRun` is present.
- **Recent repair runs:** table (Started, Processed, Created, Skipped, Errors, Trigger) for up to 10 runs. Rendered only when there is at least one run.

---

## Confirmation: payout logic unchanged

- **No changes** to:
  - Payout calculation (rate resolution, modifiers, final amount).
  - Snapshot **content** (same fields and math; only **Provenance** was added).
  - When snapshots are created (still only when status becomes Completed/OrderCompleted, or via repair).
- **Additive only:**
  - New column on `OrderPayoutSnapshots` with a default; no backfill of existing rows.
  - New table and scheduler write; no change to repair **logic**, only recording the result.
  - New optional parameter on `CreateSnapshotForOrderIfEligibleAsync` with a default; existing callers unchanged.

---

## Files touched (summary)

**Domain**

- `SnapshotProvenance.cs` (new)
- `RepairRunTriggerSource.cs` (new)
- `Entities/OrderPayoutSnapshot.cs` (Provenance property)
- `Entities/PayoutSnapshotRepairRun.cs` (new)

**Infrastructure**

- `Configurations/Rates/OrderPayoutSnapshotConfiguration.cs` (Provenance)
- `Configurations/Rates/PayoutSnapshotRepairRunConfiguration.cs` (new)
- `ApplicationDbContext.cs` (DbSet PayoutSnapshotRepairRuns)
- `Migrations/20260310120000_AddSnapshotProvenanceAndRepairRunHistory.cs` (new)

**Application**

- `Rates/Services/IOrderPayoutSnapshotService.cs` (optional provenance parameter)
- `Rates/Services/OrderPayoutSnapshotService.cs` (provenance in create + MapToDto)
- `Rates/Services/MissingPayoutSnapshotRepairService.cs` (pass RepairJob)
- `Rates/Services/MissingPayoutSnapshotSchedulerService.cs` (write repair run after each run)
- `Rates/DTOs/OrderPayoutSnapshotDto.cs` (Provenance)
- `Rates/DTOs/PayoutHealthDashboardDto.cs` (provenance counts, RepairRunSummaryDto, LatestRepairRun, RecentRepairRuns)
- `Rates/Services/PayoutHealthDashboardService.cs` (provenance counts, latest/recent repair runs)

**Tests**

- `OrderServiceIntegrationTests.cs` (mock updated for optional third parameter)

**Frontend**

- `types/payoutHealth.ts` (provenance fields, RepairRunSummaryDto, latestRepairRun, recentRepairRuns)
- `api/payoutHealth.ts` (defaults for new fields)
- `pages/reports/PayoutHealthDashboardPage.tsx` (provenance cards, latest repair run card, recent repair runs table)

**Docs**

- `docs/PAYOUT_SNAPSHOT_PROVENANCE_AND_REPAIR_HISTORY_DELIVERABLE.md` (this file)

---

## Success criteria

- Operations can distinguish **normal** snapshot creation (Normal flow) from **repaired later** (Repair job) and see Unknown/Backfill/Manual backfill counts.
- Repair run history is **persisted** and **visible** (latest run summary + recent runs table on the dashboard).
- Payout Health Dashboard gives a fuller operational picture (provenance + repair history).
- No pricing or payout calculation logic was changed; only additive provenance and repair-run persistence were added.
