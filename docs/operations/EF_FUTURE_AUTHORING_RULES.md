# EF Migration — Future Authoring Rules

Short, strict rules for migration hygiene and authoring. **Authoritative.** See also `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md`, `backend/scripts/MIGRATION_RUNBOOK.md`, `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`, and `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`.

---

## 1. Applying migrations in existing databases

- **Active path:** The **95** EF-discoverable migrations (those with a `.Designer.cs`) are applied by `dotnet ef database update` or the migration bundle. Use this when the DB is aligned with the discovered chain and no process is locking the Api DLLs (or use `--no-build` after a successful build).
- **Script path:** The **47** no-Designer migrations are **not** applied by `dotnet ef database update`. Apply them via idempotent or repair scripts from `backend/scripts/` when their schema is required. See `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` and `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`.
- **Backup** before applying migrations in staging or production. Prefer bundle or idempotent script for deploy.

---

## 2. Handling script-only migrations

- Do **not** assume any of the 47 no-Designer migrations run via `database update`. Check `__EFMigrationsHistory` and schema (e.g. `check-migration-state.sql`); apply the relevant script and document any manual insert into `__EFMigrationsHistory` only when the script does not do it.
- Do **not** regenerate Designer files for existing no-Designer migrations. Treat them as script history; apply via scripts.

---

## 3. Before adding a new migration

- Ensure the codebase builds (or use `--no-build` for EF commands after a successful build).
- **Use the official entry point:** `backend/scripts/create-migration.ps1 -MigrationName "YourDescriptiveName"` (from repo root or backend/). It runs the EF command, verifies both `.cs` and `.Designer.cs`, and runs the hygiene validator. **Do not delete the Designer.**
- Alternatively, run from the Api project:  
  `dotnet ef migrations add <Name> --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj`  
  then run `backend/scripts/validate-migration-hygiene.ps1`.
- For risky operations (e.g. dropping an index or column), use **idempotent SQL** in the migration (e.g. `DROP INDEX IF EXISTS`, `ADD COLUMN IF NOT EXISTS` where applicable) so drifted DBs do not fail.

---

## 4. When to use a sync migration

- **Sync migration:** A one-time migration whose `Up()` brings the database (and/or snapshot) in line with the current model using idempotent SQL. Use only when the team has decided to fix snapshot drift and is ready to review and test a large, idempotent migration. Requires backup and a dedicated pass.
- Do **not** add a sync migration casually; it will be large because the snapshot is behind the model. Prefer a formal pass (see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md`).

---

## 5. When to re-baseline instead of patching history

- **Re-baseline:** Create a new baseline (e.g. one “Initial” or “Baseline” migration from current model) for **new** environments while keeping the existing 142 migrations for **existing** DBs. Use only when the team commits to the two-track strategy and the plan in `docs/operations/EF_REBASELINE_PLAN.md`.
- Do **not** delete or rename historical migrations to “clean up.” Do **not** re-baseline in-place without a documented, approved plan.

---

## 6. Old migrations — what must never be deleted

- **All 95** discoverable migrations: part of the active EF chain. Deleting or moving any breaks `dotnet ef migrations list` and `database update`.
- **All 47** no-Designer migrations: script history; schema may exist in real DBs; runbooks and scripts reference them. Deleting or archiving would break traceability and script-based application.
- **Duplicate timestamps:** Ten pairs share a timestamp; ordering is by full migration ID. Do **not** rename or reorder.
- **Formal cleanup** (e.g. archival) only through a documented pass; bias toward preservation. See `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`.

---

## 7. One next step before adding another migration

- Read this file, `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md`, and the runbook. Use **`backend/scripts/create-migration.ps1`** to create the migration (it runs the validator and classifier). Ensure the new migration has a Designer and is in the discovered chain. React to the **classification** (A/B/C/D/E) and REQUIRED ACTION printed by the classifier. Before opening a PR, see **`docs/operations/EF_MIGRATION_PR_CHECKLIST.md`**, **`docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`**, and **`docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`** (when to stop and escalate).
- **Current chain and InitialBaseline:** The 95 discoverable migrations are the active EF path; the 47 script-only are governed. Graph integrity is documented in `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md`. **InitialBaseline** is a **future strategy only** (`docs/operations/EF_INITIALBASELINE_STRATEGY.md`); do not perform a baseline cutover without the cutover matrix and team agreement.

---

## 8. Snapshot drift early-warning

- If a **newly generated** migration is **unexpectedly large** (hundreds of lines, or touches many unrelated tables), **stop**. Do **not** commit blindly.
- Compare the diff to the model changes you actually made. If the scope is far larger than intended, treat it as **snapshot drift** (the model snapshot is behind the current domain model).
- **Do not** commit a huge migration. Escalate to a dedicated snapshot reconciliation pass. See `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md`, `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md`, and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md`. The validation script warns when any migration main file exceeds a line-count threshold; use that as a signal to review. A full reconciliation pass was run and **no sync migration was kept**; re-baseline or an idempotent sync remains a future, dedicated task.
