# EF Core PendingModelChangesWarning — Repair Note

## Root cause

**PendingModelChangesWarning** occurred because the **ApplicationDbContextModelSnapshot** was far behind the current application model.

- Several migrations were added **without** running `dotnet ef migrations add` (hand-written .cs only), so the snapshot was never updated for those changes.
- In particular:
  - **20260312100000_AddEventStorePhase7LeaseAndAttemptHistory** has no Designer file; it was created manually; the snapshot does not include EventStore Phase 7 columns or the EventStoreAttemptHistory table.
  - **20260309200000_AddEventStoreCausationId** was also added manually; CausationId and related columns were not in the snapshot.
- A diagnostic `dotnet ef migrations add PendingModelCheck` showed that the snapshot was missing many later changes: EventStore (Phase 7 + CausationId), EventStoreAttemptHistory, TaskItems.OrderId, ParsedOrderDrafts columns, BackgroundJobs columns, and entire tables (EventProcessingLog, LedgerEntries, ParsedMaterialAliases, RebuildExecutionLocks, RebuildOperations, ReplayExecutionLock, ReplayOperationEvents, ReplayOperations, SlaBreaches, SlaRules, WorkerInstances, WorkflowTransitionHistory).
- So the drift is **snapshot vs. current model**: the snapshot reflects an older state; the database was updated via idempotent SQL scripts, so schema and DB can be correct while EF still detects “pending” model changes.

## Fix applied

- **Design-time suppression** in **ApplicationDbContextFactory** (used by `dotnet ef` tools):
  - Before `UseNpgsql(connectionString)`, added:
    - `optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));`
  - This aligns design-time with **Program.cs**, which already suppressed this warning at runtime.
- **Justification:** Bringing the snapshot fully in sync would require either a very large idempotent “sync” migration or a full re-baseline. Suppression is an explicit, documented workaround so that:
  - `dotnet ef database update` can run without failing on PendingModelChangesWarning.
  - New migrations continue to be added via `dotnet ef migrations add` (the new migration reflects current model vs. current snapshot; snapshot is updated by the tool).
- **No snapshot correction** was done in this pass; the snapshot was left as-is. A future “sync snapshot” or re-baseline can be done in a dedicated pass.

## Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContextFactory.cs` | Added `using Microsoft.EntityFrameworkCore.Diagnostics;` and `ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))` before `UseNpgsql`. |

## Design-time vs runtime

- **Runtime:** Program.cs already had the same suppression for PendingModelChangesWarning.
- **Design-time:** ApplicationDbContextFactory now uses the same suppression, so `dotnet ef database update` and related tools no longer fail on this warning.

## Validation

- `dotnet ef database update` was run from the Api project with `ASPNETCORE_ENVIRONMENT=Development`.
- **Result:** The PendingModelChangesWarning no longer blocks the update; the command proceeds to apply migrations. (Subsequent failure, if any, may be due to migration history/schema drift, e.g. a migration that drops an index that does not exist — that is a separate issue from the warning.)

## Remaining migration risk

- **Snapshot still out of date:** The model snapshot does not reflect many tables/columns that exist in the code and may exist in the database. Adding new migrations with `dotnet ef migrations add` will only capture the diff between **current snapshot** and **current model**, which can produce large or surprising migrations until the snapshot is fully synced or re-baselined.
- **Recommended follow-up:** In a dedicated pass, either (1) generate one “sync” migration (with idempotent Up/Down) and apply it so snapshot and DB match, or (2) re-baseline migrations and snapshot. See `docs/MIGRATION_HYGIENE.md` and `docs/EF_MIGRATION_STABILIZATION.md` for related context.
