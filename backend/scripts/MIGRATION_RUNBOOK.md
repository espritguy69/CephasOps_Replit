# Migration operations runbook

Safe procedures for applying and auditing EF Core migrations for CephasOps. Do not drop data or production tables; prefer additive and idempotent steps.

**Schema guard (platform safety):** To prevent migration history and live schema from drifting apart, apply migrations only through the **canonical path** (bundle or full idempotent script). **Never** insert a migration ID into `__EFMigrationsHistory` unless the **full** schema for that migration has been applied. Always run **post-apply verification** (`check-migration-state.sql`). See `backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md`.

**Before adding a new migration:** Use the **official entry point** `backend/scripts/create-migration.ps1 -MigrationName "YourDescriptiveName"` (from repo root or backend/). It runs the EF command, verifies both `.cs` and `.Designer.cs`, and runs `validate-migration-hygiene.ps1`. If the new migration is unexpectedly large, stop and treat as snapshot drift—do not commit blindly. See `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md`, `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`, and `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`.

**Platform safety / guard failures:** For what to do when tenant-safety, financial isolation, or EventStore consistency guards fail (or when CI reports artifact drift or bypass misuse), see `backend/docs/operations/PLATFORM_SAFETY_OPERATOR_RESPONSE.md`. For what is surfaced vs missing in observability, see `backend/docs/operations/OPERATIONAL_OBSERVABILITY_INVENTORY.md`.

---

## Validation: avoiding DLL lock when running EF tools

**Symptom:** `dotnet ef database update` or `dotnet ef migrations list` fails with build errors like "The process cannot access the file '...CephasOps.Infrastructure.dll' because it is being used by another process" (MSB3026 / MSB3027).

**Root cause:** The EF tools build the startup project (CephasOps.Api). That build copies dependency outputs (Domain, Infrastructure, Application) into the Api `bin` directory. If a CephasOps.Api process is already running (e.g. `dotnet run`, debugger, or another terminal), it has those DLLs loaded and the copy fails.

**Workaround:**

1. **Stop any running Api** — Close the debugger, stop `dotnet run` or any host that loads CephasOps.Api, then run EF commands from a clean terminal.
2. **Use `--no-build` when build is already up to date** — Run `dotnet build` once (when no process is locking), then run `dotnet ef migrations list --no-build` or `dotnet ef database update --no-build ...` so the tools do not trigger a rebuild.

**Safe validation sequence (local):**

```bash
cd backend/src/CephasOps.Api
# Ensure no Api is running, then:
dotnet build -v q
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef migrations list --no-build --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
dotnet ef database update --no-build --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

---

## Recommended deployment workflow

Use this order for deploying schema changes to any environment:

1. **Backup database** — Take a full backup (e.g. `pg_dump`) before applying migrations.
2. **Run migration bundle** (or idempotent script if bundle is not used) — Apply pending migrations.
3. **Run repair scripts if needed** — If the bundle only applies the “discovered” chain, run `repair-password-reset-tokens-schema.sql` (and any other repair scripts) when the corresponding tables are missing.
4. **Run verification** — Execute `check-migration-state.sql` and confirm expected tables/columns exist.
5. **Record deployment audit (optional)** — After verification (and any migration-specific smoke test), record the rollout in the **MigrationAudit** table using `record-migration-audit.sql`. See `backend/docs/operations/MIGRATION_AUDIT.md`.

See **Example deployment workflow** at the end of this runbook for a concrete sequence.

---

## Migration bundle (recommended for deployment)

A migration bundle is a self-contained executable that applies all discovered migrations. It does not require the .NET SDK or EF tools on the target machine—only the .NET runtime. Use it in CI/CD or when deploying to a server.

### Generate the bundle

From repo root or `backend/`:

```bash
cd backend/src/CephasOps.Api
dotnet ef migrations bundle --output ../../scripts/CephasOps.MigrationsBundle.exe --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

Optional: `--configuration Release` for a Release build. The output path can be a directory (e.g. `../../scripts/`) or a full filename (e.g. `../../scripts/CephasOps.MigrationsBundle.exe`).

### Execute the bundle

Run the executable from the directory that contains it (e.g. `backend/scripts/` if you used the output path above).

**Option A — Connection string in command:**

```bash
./CephasOps.MigrationsBundle.exe --connection "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD;SslMode=Disable;Include Error Detail=true"
```

**Option B — Connection via environment (safer for production):**

```bash
set ConnectionStrings__DefaultConnection=Host=...;Database=cephasops;...
./CephasOps.MigrationsBundle.exe
```

On Linux/macOS use `export ConnectionStrings__DefaultConnection=...`. The bundle reads the connection from configuration when not passed via `--connection`.

**Option C — Startup project appsettings:**

If the bundle was built with a startup project that has `appsettings.json` (or environment-specific), it may use that at runtime. Prefer explicit `--connection` or environment variable in production.

The bundle applies only migrations that are **discovered** (those with a `.Designer.cs`). It does not apply the **47** script-only migrations that lack Designers. After running the bundle, run repair scripts if those schema objects are required (see **When to run repair scripts** below).

### When to use the bundle vs idempotent script

| Use bundle when | Use idempotent script when |
|-----------------|----------------------------|
| You want a single executable for CI/CD or server deploy. | You need a reviewable SQL file or must run migrations via psql. |
| Target has .NET runtime but not SDK or `dotnet ef`. | Target has only database access (psql / SQL client). |
| You are applying only the “discovered” migration chain. | You need to apply a custom set of migrations or a one-off script (e.g. PayoutAnomalyReview). |
| You prefer not to ship connection strings in scripts. | You run scripts in a controlled environment where connection is already configured. |

**Fall back to idempotent script when:**

- The bundle fails (e.g. connection or runtime issue) and you can run SQL directly.
- You restored from a backup and need to apply migrations that the bundle does not include (e.g. legacy migrations without Designer).
- Policy requires a reviewable SQL artifact before applying changes.
- The target environment cannot run the bundle (no .NET runtime).

---

## When to use `dotnet ef database update`

Use when:

- The database is in sync with the migration chain (no known history/schema drift).
- You are developing locally and the last applied migration in `__EFMigrationsHistory` matches what the tooling reports.
- No previous failure was caused by a missing index or missing table (e.g. IX_PasswordResetTokens_TokenHash).

**Command (from repo root):**

```bash
cd backend/src/CephasOps.Api
dotnet ef database update --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

If you see "No migrations were applied. The database is already up to date", the discovered chain is fully applied. If you see an error (e.g. index does not exist), stop and use the idempotent/repair path below.

---

## When to use an idempotent SQL script

Use when:

- You are deploying to an environment (e.g. production) and want a single, reviewable script.
- You restored from a backup and need to bring the schema forward.
- `dotnet ef database update` has failed due to drift (e.g. missing index/table) and you have applied the stabilization fix (see `docs/EF_MIGRATION_STABILIZATION.md`).
- AGENTS.md or project docs specify using the idempotent script for initial setup (e.g. after restoring a backup).

**Generate script:**

```bash
cd backend/src/CephasOps.Api
dotnet ef migrations script --idempotent --output ../../scripts/generated-migrations-idempotent.sql --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

Then run the script against the target database (e.g. with `psql -f`). The script includes conditional logic so already-applied migrations are skipped. **Do not** manually insert into `__EFMigrationsHistory` for migrations that the idempotent script already records (the script inserts rows when it applies a migration).

If you run a **custom** idempotent script (e.g. only PayoutAnomalyReview), then you may need to insert that migration ID into `__EFMigrationsHistory` once, so future runs do not re-apply it.

---

## When to use a repair script

Use when:

- `__EFMigrationsHistory` shows migrations applied (e.g. 20260308163857 and 20260311120000) but the **PasswordResetTokens** table (or its indexes) are missing.
- You applied PayoutAnomalyReview via script and never ran the migration that creates PasswordResetTokens.

**PasswordResetTokens schema repair:**

```bash
# From backend/ or repo root; set connection as needed
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/repair-password-reset-tokens-schema.sql
```

Safe to run multiple times. Does **not** insert into `__EFMigrationsHistory`.

**When to run repair scripts after bundle:** Run `repair-password-reset-tokens-schema.sql` if `check-migration-state.sql` shows that the `PasswordResetTokens` table does not exist (e.g. when the bundle only applies the discovered chain and that table is created by a migration without a Designer). Other repair or idempotent scripts (e.g. `apply-payout-anomaly-review-migration.sql`) may be needed if you rely on schema from the 47 script-only migrations; run them only when the corresponding tables are missing and you have confirmed it is safe.

**Event Store Phase 7:** The migration `20260312100000_AddEventStorePhase7LeaseAndAttemptHistory` has no Designer and is not in the discovered chain. To add EventStore lease/error columns and the `EventStoreAttemptHistory` table (and any prerequisite EventStore columns such as `NextRetryAtUtc`, `ProcessingStartedAtUtc`), run the idempotent script:  
`psql ... -f backend/scripts/apply-EventStorePhase7LeaseAndAttemptHistory.sql`.  
The script inserts the migration row into `__EFMigrationsHistory`. Verify with `check-migration-state.sql` (Phase 7 section).

---

## Auditing migration state

1. **Database: history and key schema**
   - Run: `backend/scripts/check-migration-state.sql` with psql.
   - Inspect: `__EFMigrationsHistory` (ordered); existence of `PasswordResetTokens`, `OrderPayoutSnapshots`, `PayoutAnomalyReviews`, `PayoutSnapshotRepairRuns`; **model-required integration-bus tables** (`ConnectorDefinitions`, `ConnectorEndpoints`, `ExternalIdempotencyRecords`, `OutboundIntegrationAttempts`); `OrderPayoutSnapshots.Provenance` column; `RefreshTokens.UserAgent`.

2. **Repo: migrations missing Designer**
   - Run: `backend/scripts/audit-migration-designers.ps1` (Windows) or inspect `docs/MIGRATION_HYGIENE.md` for the list.
   - Migrations without a `.Designer.cs` are not discovered by `dotnet ef migrations list` / `dotnet ef database update`.

3. **Compare**
   - If history contains migration IDs that are **not** in the discovered chain (e.g. 20260311120000), those were likely applied via a script + manual insert. Future updates will not re-apply them as long as they remain in history.

---

## Execution strategy by environment

| Environment type | Normal `dotnet ef database update` | Idempotent script | Prechecks / notes |
|------------------|------------------------------------|--------------------|-------------------|
| **A. Existing DB with history/schema drift** | Only if you have applied stabilization fixes (20260308163857, 20260309053019, 20260309065620) and no Api is running. May still fail on later migrations if schema was applied out of order. | **Preferred.** Generate idempotent script, review, apply via psql. Run repair scripts for missing tables (PasswordResetTokens, etc.). | Backup. Run `check-migration-state.sql`. See `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` for no-Designer migrations. |
| **B. Existing DB mostly aligned** | Yes, if last applied migration in history matches the discovered chain and no process locks the Api DLLs. Use `--no-build` after a successful build. | Use if `database update` fails (e.g. column already exists). | Backup. Ensure no Api running when running EF tools. |
| **C. New / greenfield DB** | Yes, after initial DB create. Run from a clean state (no Api running). | Alternative: apply full idempotent script from repo (e.g. after restore from backup). | Backup not needed for empty DB. For restore-from-backup, use idempotent script per AGENTS.md. |

**No-Designer migrations:** **47** migrations have no `.Designer.cs` and are **not** applied by `dotnet ef database update` or the bundle. Do **not** assume they run via EF. Apply via idempotent or repair scripts when required. See `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` and `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`. **Do not delete or archive** historical migrations; formal re-baseline only per `docs/operations/EF_REBASELINE_PLAN.md`. Authoritative cleanup and future rules: `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`, `docs/operations/EF_FUTURE_AUTHORING_RULES.md`.

**Graph integrity and InitialBaseline:** The migration graph has been checked (discoverable chain and script-only set are coherent); see `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` and `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md`. **InitialBaseline** is a **future strategy only** — do **not** perform a baseline cutover casually. Use `docs/operations/EF_INITIALBASELINE_STRATEGY.md` and `docs/operations/EF_BASELINE_CUTOVER_DECISION_MATRIX.md` when the team adopts a two-track model. Optional script: `backend/scripts/check-migration-graph-integrity.ps1`.

---

## Adding a new migration

1. Create with EF tools (keeps Designer and chain consistent):
   ```bash
   cd backend/src/CephasOps.Api
   dotnet ef migrations add YourMigrationName --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
   ```
2. Review the generated `.cs` and `.Designer.cs`; do not remove the Designer.
3. For risky operations (e.g. dropping an index), prefer idempotent SQL in the migration (e.g. `DROP INDEX IF EXISTS ...`) so existing and new environments both succeed.
4. Apply via `dotnet ef database update` or via an idempotent script, depending on environment (see above).

---

## If `dotnet ef database update` fails

1. Note the exact migration name and error (e.g. "DROP INDEX ... does not exist").
2. Check `docs/EF_MIGRATION_STABILIZATION.md` for known fixes (e.g. IX_PasswordResetTokens_TokenHash).
3. If the failure is drift (missing table/index), run the appropriate repair script, then retry `dotnet ef database update` if the next step would be applying a migration that is still pending.
4. If the failure persists, generate an idempotent script from the current branch and apply it manually; then insert the applied migration ID(s) into `__EFMigrationsHistory` only for migrations that were actually applied by that script.
5. Do not drop real tables or clear `__EFMigrationsHistory` to “fix” the chain; use additive repairs only.

---

## Example deployment workflow

1. **Backup**
   ```bash
   pg_dump -h HOST -p 5432 -U postgres -d cephasops -F c -f cephasops_backup_$(date +%Y%m%d_%H%M%S).dump
   ```

2. **Apply migrations (choose one)**
   - **Bundle:**  
     `./CephasOps.MigrationsBundle.exe --connection "YOUR_CONNECTION_STRING"`  
     (Or set `ConnectionStrings__DefaultConnection` and run without `--connection`.)
   - **Idempotent script:**  
     Generate with `dotnet ef migrations script --idempotent --output ../../scripts/generated-migrations-idempotent.sql ...`, then:  
     `psql -h HOST -p 5432 -U postgres -d cephasops -f backend/scripts/generated-migrations-idempotent.sql`

3. **Repair scripts (if needed)**
   - If `PasswordResetTokens` is missing:  
     `psql ... -f backend/scripts/repair-password-reset-tokens-schema.sql`
   - If `PayoutAnomalyReviews` is missing and you use that feature:  
     `psql ... -f backend/scripts/apply-payout-anomaly-review-migration.sql`
   - If Event Store Phase 7 (lease/attempt history) is required and not in the discovered chain:  
     `psql ... -f backend/scripts/apply-EventStorePhase7LeaseAndAttemptHistory.sql`

4. **Verify**
   ```bash
   psql -h HOST -p 5432 -U postgres -d cephasops -f backend/scripts/check-migration-state.sql
   ```
   Confirm: `__EFMigrationsHistory` has expected rows; `PasswordResetTokens`, `OrderPayoutSnapshots`, `PayoutAnomalyReviews`, `PayoutSnapshotRepairRuns` exist where expected; **ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts** exist (model-required); `OrderPayoutSnapshots` has `Provenance` column; `RefreshTokens` has `UserAgent`.
