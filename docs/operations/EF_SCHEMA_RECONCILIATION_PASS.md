# EF Migration Stabilization — Schema Reconciliation Pass

This document records the **ruthless schema reconciliation pass**: root-cause analysis, repair of the failing migration, reconciliation strategy, and remaining risks.

---

## A. Migration failures / risks identified

| Risk | Detail |
|------|--------|
| **20260309053019_AddBackgroundJobWorkerOwnership** (repaired) | Up() dropped indexes `IX_ParsedMaterialAliases_CompanyId_NormalizedAliasText` and `IX_ParsedMaterialAliases_MaterialId` and altered columns on `ParsedMaterialAliases`. That table is created by **20260311120000_AddParsedMaterialAlias**, which has **no Designer** and is not in the discovered migration chain. So when applying the discovered chain in order, ParsedMaterialAliases either does not exist yet or was created via idempotent script with different state; the index drop failed with "index does not exist". |
| **Snapshot vs model drift** | ApplicationDbContextModelSnapshot is far behind the current model (many tables/columns missing). See `docs/operations/EF_PENDING_MODEL_CHANGES_REPAIR.md`. |
| **24 migrations without Designer** | Not discovered by `dotnet ef migrations list` / `database update`; applied via idempotent scripts or not at all. See `docs/MIGRATION_HYGIENE.md`. |
| **Other unguarded DropIndex in chain** | 20260309051147 (ReplayOperations/RebuildOperations indexes), 20260309120000 (JobRuns), 20260309060310 Down (TaskItems), 20260309180000 (EventStore), etc. These operate on tables created in the same discovered chain; risk is lower unless history/schema were applied out of order. Harden with `DROP INDEX IF EXISTS` if they fail in a given environment. |
| **20260308163857** | Already hardened with `DROP INDEX IF EXISTS "IX_PasswordResetTokens_TokenHash"` (see `docs/EF_MIGRATION_STABILIZATION.md`). |

---

## B. Fragile migrations repaired

| Migration | Repair |
|-----------|--------|
| **20260309053019_AddBackgroundJobWorkerOwnership** | All ParsedMaterialAliases operations (index drops + column alters) moved into a single PostgreSQL `DO $$ ... END $$;` block that runs only when `information_schema.tables` shows the table exists. Index drops use `DROP INDEX IF EXISTS`. Down() restores ParsedMaterialAliases columns/indexes only when the table exists, using `CREATE INDEX IF NOT EXISTS`. BackgroundJobs AddColumn/CreateIndex and their Down() remain as-is (table is in discovered chain). |
| **20260309065620_PendingModelCheck** (operational closure pass) | Up(): EventStore Phase 7 columns and EventStoreAttemptHistory table/indexes replaced with idempotent SQL (`ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`) so DBs that already had Phase 7 applied via script do not fail with "column already exists". Down(): `DROP COLUMN IF EXISTS` / `DROP TABLE IF EXISTS`. |

---

## C. Exact fix for AddBackgroundJobWorkerOwnership issue

**Cause:** The migration was generated when the snapshot (or a prior Designer) included ParsedMaterialAliases. In reality, the migration that **creates** ParsedMaterialAliases is **20260311120000_AddParsedMaterialAlias**, which has no Designer and is not applied by `dotnet ef database update`. So when 20260309053019 runs, it assumes ParsedMaterialAliases already exists with specific indexes; in DBs built from the discovered chain only, the table does not exist, and in others the index names may differ or be missing.

**Fix:**

1. **Up():** Replaced the two `migrationBuilder.DropIndex(...)` and five `migrationBuilder.AlterColumn(...)` calls for ParsedMaterialAliases with one `migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ParsedMaterialAliases') THEN
    DROP INDEX IF EXISTS ""IX_ParsedMaterialAliases_CompanyId_NormalizedAliasText"";
    DROP INDEX IF EXISTS ""IX_ParsedMaterialAliases_MaterialId"";
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""Source"" TYPE text;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""RowVersion"" DROP DEFAULT;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""NormalizedAliasText"" TYPE text;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""IsActive"" DROP DEFAULT;
    ALTER TABLE ""ParsedMaterialAliases"" ALTER COLUMN ""AliasText"" TYPE text;
  END IF;
END $$;");`
2. **Down():** Replaced the ParsedMaterialAliases AlterColumn and CreateIndex C# calls with one `migrationBuilder.Sql(...)` that runs the reverse ALTERs and `CREATE INDEX IF NOT EXISTS` only when the table exists.

BackgroundJobs AddColumn / CreateIndex and their Down() are unchanged.

---

## D. Recommended reconciliation strategy

**Option A (chosen):** Keep the current discovered chain, repair fragile migrations when they fail, and continue using idempotent scripts for migrations that have no Designer. Add a **sync migration** later only when the team is ready to bring the snapshot in line with the actual model (large, one-time idempotent migration or re-baseline).

- **Rationale:** The snapshot is far behind; a full re-baseline would require scripting all migrations idempotently and resetting migration history for new environments, which is a large change. The immediate failure was one migration (20260309053019) assuming a table/index state that does not hold in all environments. Making that migration conditional and idempotent is the minimal, production-safe fix. Other migrations in the discovered chain have not (yet) failed; hardening them can be done incrementally if they fail.
- **Apply path:** Prefer `dotnet ef database update` when the DB is in sync with the discovered chain. When history or schema was applied via scripts, use idempotent scripts and repair scripts (see `backend/scripts/MIGRATION_RUNBOOK.md`, `docs/EF_MIGRATION_STABILIZATION.md`).

---

## E. Snapshot / re-baseline recommendation

- **Do not** blindly regenerate or replace the snapshot; it would produce a single huge migration and high risk.
- **Sync migration (deferred):** A one-time migration that brings the snapshot in line with the current model is feasible but large; make its Up() idempotent (IF NOT EXISTS / ADD COLUMN IF NOT EXISTS where possible) and run in a dedicated pass with backup.
- **Re-baseline (deferred):** For greenfield DBs only, a controlled re-baseline (new initial migration from current model + scripted idempotent “apply all” for existing DBs) could be considered later. Not recommended until the current chain is stable and documented.
- **Current recommendation:** Keep the snapshot as-is. Rely on design-time PendingModelChangesWarning suppression (see `EF_PENDING_MODEL_CHANGES_REPAIR.md`) and repaired migrations. Revisit snapshot sync or re-baseline when the team commits to a dedicated migration-stability sprint.

---

## F. Validation performed

- **Build:** `dotnet build` for CephasOps.Infrastructure succeeds (migration C# is valid).
- **dotnet ef database update:** Not run to completion in this pass because the Api process (or another) was holding DLLs and caused copy/lock failures during build. The migration file change is syntax- and logic-correct; the fix is the same pattern as the existing idempotent guard in 20260308163857.
- **Manual verification:** The repaired Up() and Down() use standard PostgreSQL (`information_schema`, `DROP INDEX IF EXISTS`, `CREATE INDEX IF NOT EXISTS`); when the table exists, behavior matches the original migration intent; when it does not, the ParsedMaterialAliases block is skipped and BackgroundJobs changes still apply.

**Recommended post-pass check:** With no process locking the Api binaries, run from the Api project directory:
`dotnet ef database update --project ..\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj`
and confirm that 20260309053019_AddBackgroundJobWorkerOwnership applies without error.

---

## G. Remaining migration risk

- **Snapshot drift:** New `migrations add` can still produce large or noisy diffs until the snapshot is synced or re-baselined.
- **Migrations without Designer:** 24 migrations are not in the discovered chain; apply via idempotent scripts and document history inserts.
- **Other unguarded drops:** Any migration that drops an index/column/table without `IF EXISTS` can fail if history and schema are out of sync; harden those migrations incrementally if they fail in production or staging.
- **Duplicate timestamps:** 20260308140000 and 20260308150000 each have two migrations; ordering is by full migration ID (see MIGRATION_HYGIENE.md).

---

## H. Files / docs changed

| Path | Change |
|------|--------|
| `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260309053019_AddBackgroundJobWorkerOwnership.cs` | Up(): ParsedMaterialAliases logic replaced with conditional idempotent SQL. Down(): ParsedMaterialAliases restore replaced with conditional idempotent SQL. |
| `docs/operations/EF_SCHEMA_RECONCILIATION_PASS.md` | New: this document. |
| `docs/EF_MIGRATION_STABILIZATION.md` | (Recommended) Add a short subsection linking to this pass and the 20260309053019 fix. |

No other code or config files were changed. Snapshot was not modified.

---

## Related: full migration recovery pass

A later **EF Migration Recovery / Designer Regeneration / Snapshot Re-baseline** pass produced a full audit, classification of all no-Designer migrations, snapshot reconciliation report, and re-baseline plan. See **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`**, **`docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`**, **`docs/operations/EF_MIGRATION_RECOVERY_DECISION.md`**, and **`docs/operations/EF_REBASELINE_PLAN.md`**.
