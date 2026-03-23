# EF Migration Re-baseline Plan

**Status:** Plan only; **not executed** in this recovery pass.  
**When to use:** When the team decides to restore long-term snapshot hygiene and is willing to maintain a two-track migration strategy (legacy chain for existing DBs, baseline for new/greenfield).

---

## 1. Why re-baseline might be needed

- **44 migrations** have no Designer and are not discoverable; the snapshot was never updated for them.
- The **ApplicationDbContextModelSnapshot** is far behind the current domain model and behind the “logical” end of the discovered chain.
- Adding new migrations produces **noisy, large diffs** and PendingModelChangesWarning is suppressed rather than fixed.
- A **single “sync” migration** that adds all missing model elements would be very large and hard to review/test.

Re-baseline is an option when the above cost becomes unacceptable and the team wants a clean baseline for **new** databases while preserving the existing chain for **existing** databases.

---

## 2. Prerequisites

- Full backup of any database that will be migrated.
- Agreement on which environments use “legacy chain” vs “baseline”.
- No in-place deletion or rename of existing migration files; legacy chain remains in the repo for reference and for existing DBs.

---

## 3. Backup and environment constraints

- **Backup:** Full `pg_dump` (or equivalent) before any schema change in any environment.
- **Existing DBs:** Must **not** have their `__EFMigrationsHistory` wiped or replaced unless a deliberate migration path (e.g. “apply baseline SQL then insert single baseline row”) is followed and documented.
- **New/greenfield DBs:** Can start from a baseline migration only if the baseline script is idempotent and creates the full schema.

---

## 4. Proposed re-baseline steps (high level)

1. **Preserve current state**
   - Keep all existing migration files (including no-Designer).
   - Optionally move them under a subfolder (e.g. `Migrations/Legacy/`) for clarity; only if tooling and docs are updated so that EF does not load them for the new baseline context (or use a separate DbContext for baseline).

2. **Create baseline snapshot**
   - Create a **new** DbContext-derived snapshot that matches the **current** domain model (e.g. by running `dotnet ef migrations add Baseline --output-dir Migrations/Baseline` on a **temporary** project that has no prior migrations, then copying the generated snapshot and migration into the real project, or by generating the snapshot from the current model in a controlled way). Exact mechanics depend on whether you use a second context or a single context with a new “baseline” migration folder.

3. **Single baseline migration**
   - One migration (e.g. `YYYYMMDD_Baseline`) whose `Up()` contains **idempotent** SQL that creates all tables/columns/indexes that the current model expects (e.g. `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`). Generate or hand-author; review thoroughly.

4. **History for new DBs**
   - For a **new** database: run the baseline migration (or its SQL script); insert one row into `__EFMigrationsHistory` for the baseline migration ID. No legacy migration IDs.

5. **History for existing DBs**
   - **Do not** remove or alter existing `__EFMigrationsHistory` rows. Continue applying legacy migrations (or their idempotent scripts) as today. Optionally, after all legacy migrations are applied, add a **sync** step (one idempotent migration or script that brings schema in line with the baseline) and record that in history; this is optional and environment-specific.

6. **Documentation**
   - Document which environments use legacy chain vs baseline.
   - Update runbook: when to use bundle/update (legacy) vs baseline script (greenfield).
   - Document how to add **new** migrations after re-baseline (new migrations add to the baseline snapshot).

---

## 5. Staged rollout

| Stage | Action |
|-------|--------|
| **Dev** | Optional: create a new DB from baseline script; run app; verify. |
| **Staging** | Keep using legacy chain + idempotent scripts until baseline is validated. |
| **Prod** | Never switch prod to baseline without backup and tested rollback; prefer legacy chain + scripts. |

---

## 6. Effects on existing databases

- **No change** if you do not run any new “baseline” migration against them. Existing DBs keep their current `__EFMigrationsHistory` and continue to receive legacy migrations or idempotent scripts as today.
- If you later add a “sync to baseline” migration for existing DBs, it must be **idempotent** (IF NOT EXISTS, etc.) and recorded in history only once.

---

## 7. What this recovery pass did not do

- Did **not** create a baseline folder or new context.
- Did **not** generate baseline migration or snapshot.
- Did **not** modify existing migration files or snapshot.
- Only produced this **plan** for when the team chooses to execute a re-baseline.
