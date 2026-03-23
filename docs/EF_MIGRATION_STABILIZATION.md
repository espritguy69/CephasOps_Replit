# EF Core migration history stabilization

## Root cause

- **Reported failure:** During `dotnet ef database update`, a step tried to **drop** index `IX_PasswordResetTokens_TokenHash` and failed because the index (or the `PasswordResetTokens` table) did not exist.
- **Exact migration associated with the failure:** The error was observed while **applying** `20260308163857_AddUserAgentToRefreshToken`. The migration’s **Up()** in source only adds the `UserAgent` column to `RefreshTokens`; it does **not** contain any `DropIndex` for `IX_PasswordResetTokens_TokenHash`. So either:
  - An older build of that migration (or a different code path) once contained a drop for that index, or
  - The drop was emitted from another migration or from a snapshot diff in some environments.
- **Drift:** In the audited database:
  - `__EFMigrationsHistory` included `20260308163857_AddUserAgentToRefreshToken` and `20260311120000_AddPayoutAnomalyReview`, but **not** `20260308190000_AddPasswordResetTokens`.
  - The **PasswordResetTokens** table did **not** exist (and therefore the index did not exist).
  - So history and schema were out of sync: history suggested a later state than the actual schema for the middle migrations.

## Exact repair applied

1. **Idempotent guard in `20260308163857_AddUserAgentToRefreshToken`**
   - At the **start** of **Up()**, added:
     - `migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PasswordResetTokens_TokenHash"";");`
   - So whenever this migration runs, any attempt to drop that index is done in an idempotent way:
     - If the index exists (e.g. drifted DB), it is dropped and the rest of the migration runs.
     - If the index (or table) does not exist, the statement is a no-op and the migration continues.
   - No data or tables are dropped; only the index drop is made safe.

2. **Repair script for missing schema**
   - **File:** `backend/scripts/repair-password-reset-tokens-schema.sql`
   - Creates the **PasswordResetTokens** table and indexes **only if they do not exist** (PostgreSQL `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`).
   - Use when the DB has a gap in applied migrations and is missing the `PasswordResetTokens` table (e.g. after applying PayoutAnomalyReview via idempotent script and skipping intermediate migrations).
   - Safe to run multiple times. It does **not** insert into `__EFMigrationsHistory`.

## Will `dotnet ef database update` work now?

- **Environments where `20260308163857` has not been applied yet:** Yes. When that migration runs, the new `DROP INDEX IF EXISTS` ensures the step that was failing (drop of `IX_PasswordResetTokens_TokenHash`) no longer causes an error, so the migration can complete and future updates can proceed.
- **Environments where `20260308163857` is already applied (e.g. current DB):** `dotnet ef database update` already reports “No migrations were applied. The database is already up to date” because the latest migration known to the tooling is applied. The repair script is for bringing **schema** in line with the model when the **PasswordResetTokens** table (and index) are missing; it does not change migration history.

## Remaining migration risks

- **Migrations without Designer files:** Migrations such as `20260308180000_AddLastLoginAndMustChangePasswordToUser`, `20260308190000_AddPasswordResetTokens`, `20260308200000_AddLockoutFieldsToUser`, and `20260311120000_AddPayoutAnomalyReview` have no `.Designer.cs` in the repo. The EF Core tools discover migrations via the assembly; without the Designer (and its `[Migration("...")]` attribute), these may not appear in the migration list. So:
  - The “latest” migration the tooling sees may be `20260308163857_AddUserAgentToRefreshToken`.
  - Applying PayoutAnomalyReview via the idempotent SQL script and inserting `20260311120000_AddPayoutAnomalyReview` into `__EFMigrationsHistory` is consistent with the current approach but leaves a gap for the migrations in between (e.g. PasswordResetTokens, Lockout fields). The repair script fixes the PasswordResetTokens schema only.
- **Other environments:** Any environment that still has an older version of `20260308163857` that performs a non-idempotent drop of `IX_PasswordResetTokens_TokenHash` will be fixed by deploying the updated migration (with `DROP INDEX IF EXISTS`). Environments that already applied the current migration are unaffected; use the repair script if the table/index are missing.

## Verification

- **Check migration history:**  
  `SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";`
- **Check PasswordResetTokens:**  
  `SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PasswordResetTokens');`  
  and  
  `SELECT indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'PasswordResetTokens';`
- **Check script:**  
  `backend/scripts/check-migration-state.sql` (run with psql) prints history, table existence, and indexes.
- **Runbook:**  
  `backend/scripts/MIGRATION_RUNBOOK.md` — when to use `dotnet ef database update` vs idempotent script vs repair script.
- **Hygiene:**  
  `docs/MIGRATION_HYGIENE.md` — migration inventory, missing Designer list, long-term strategy.
- **Schema reconciliation (20260309053019):**  
  `docs/operations/EF_SCHEMA_RECONCILIATION_PASS.md` — repair of AddBackgroundJobWorkerOwnership (ParsedMaterialAliases index/table drift), reconciliation strategy, and remaining risks.

## Summary

| Item | Detail |
|------|--------|
| **Root cause** | A migration step tried to drop `IX_PasswordResetTokens_TokenHash` which did not exist; history and schema were out of sync (e.g. PayoutAnomalyReview applied via script, PasswordResetTokens never applied). |
| **Exact migration** | Failure occurred while applying **20260308163857_AddUserAgentToRefreshToken** (drop not present in current migration source). |
| **Repair** | (1) Idempotent `DROP INDEX IF EXISTS "IX_PasswordResetTokens_TokenHash"` at start of **Up()** of **20260308163857**. (2) Optional schema repair script for missing **PasswordResetTokens** table/indexes. |
| **Future `dotnet ef database update`** | Should work where **20260308163857** is still pending; already “up to date” where it is applied. Use repair script if table/index are missing. |
| **Risks** | Several migrations have no Designer files; tooling may not list them. Use idempotent scripts and repair script where history was applied out of order. |
