# EF Safe Migration Workflow

**Strict, official workflow for adding EF Core migrations.** Follow this every time. See also `docs/operations/EF_FUTURE_AUTHORING_RULES.md`, `backend/scripts/MIGRATION_RUNBOOK.md`, `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`, and `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`.

---

## 1. Before you start

- Read this file and `docs/operations/EF_FUTURE_AUTHORING_RULES.md`.
- Ensure no one is relying on a running Api process that locks Infrastructure DLLs (or use `--no-build` after a clean build).
- Ensure the codebase builds from `backend/`: `dotnet build`.

---

## 2. How to create a migration (only supported way)

- **Always** create migrations via the **official script** or the documented EF command. **Never** manually create migration `.cs` or `.Designer.cs` files.
- **Always** use the Infrastructure project as the migration project and the Api as the startup project.

**Official entry point (recommended) — from repo root or backend/:**

```powershell
# From repo root:
.\backend\scripts\create-migration.ps1 -MigrationName "AddOrderStatusChecklist"

# From backend/:
.\scripts\create-migration.ps1 -MigrationName "AddOrderStatusChecklist"
```

The script runs `dotnet ef migrations add`, verifies both `.cs` and `.Designer.cs` exist for the new migration, and runs the hygiene validator. If validation fails, it tells you to stop and review.

**Good migration names:** `AddOrderStatusChecklist`, `AddPasswordResetTokens`, `AddJobRunEventId`  
**Bad migration names:** `temp`, `test`, `fix`, `migration1`, `newmigration`, `update`, `wip`

**Alternative (manual command):** From `backend/src/CephasOps.Api`:  
`dotnet ef migrations add <MigrationName> --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --context ApplicationDbContext --output-dir Persistence/Migrations`  
Then run `.\backend\scripts\validate-migration-hygiene.ps1` yourself.

- After generation, **confirm** that **both** the main `.cs` and the `.Designer.cs` exist. If the Designer is missing, the migration will not be in the active EF chain and `dotnet ef database update` will not apply it.

---

## 3. Immediately after creating a migration

1. **Verify Designer exists**  
   Run the hygiene validation script (see below). It must not report the new migration as missing a Designer.

2. **Check migration size (snapshot drift early-warning)**  
   - If the new migration file is **unexpectedly large** (e.g. hundreds of lines of changes, or touches many unrelated tables), **stop**.
   - Do **not** commit blindly. This often indicates **snapshot drift** (the model snapshot is behind the current domain model).
   - Compare the diff against the actual model changes you intended. If the scope is far larger than your change, treat it as possible snapshot drift.
   - **Escalate:** Do not commit a huge migration. Prefer a dedicated snapshot reconciliation pass (see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md` and `docs/operations/EF_FUTURE_AUTHORING_RULES.md`).

3. **Run the validation script**  
   From `backend/` or repo root:  
   `.\backend\scripts\validate-migration-hygiene.ps1`  
   Fix any reported issues (e.g. missing Designer) before committing.

---

## 4. What never to do

- **Never** manually create or copy migration files.
- **Never** delete or rename existing migrations (including the 47 script-only ones).
- **Never** remove the `.Designer.cs` of a migration you just added.
- **Never** commit a suspiciously huge migration without reviewing for snapshot drift.
- **Never** assume the 47 no-Designer migrations are applied by `dotnet ef database update`; use scripts per runbook and manifest.

---

## 5. Script-only migrations vs active chain

- **Active EF chain:** 95 migrations with a `.Designer.cs`. These are applied by `dotnet ef database update` and the bundle.
- **Script-only:** 47 migrations have no Designer. They are **not** applied by EF; apply them via idempotent or repair scripts when needed. See `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`.
- **New migrations** must always get a Designer so they stay in the active chain. Do not add "script-only" style migrations by hand; use the EF command so both files are generated.

---

## 6. Before opening a migration PR

1. Create the migration using **`backend/scripts/create-migration.ps1`** (or the equivalent `dotnet ef migrations add` command).
2. Confirm both `.cs` and `.Designer.cs` exist (the script checks this automatically).
3. Ensure `validate-migration-hygiene.ps1` passes (the script runs it; or run it manually before commit).
4. If the migration is unexpectedly large, stop and escalate (snapshot drift); do not commit.
5. Reviewers will use **`docs/operations/EF_MIGRATION_PR_CHECKLIST.md`**. When to stop and escalate: **`docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`**.

---

## 7. Validation and auto-classification

| Script | Purpose | When to run |
|--------|---------|-------------|
| `backend/scripts/create-migration.ps1` | **Official entry point.** Creates migration, verifies both files, runs validator and classifier. | Use this to create every new migration. |
| `backend/scripts/validate-migration-hygiene.ps1` | Checks Designer presence, counts, naming quality, and size warning. Optional: `-Classify` runs the classifier after validation. | After adding a migration; before committing; CI. |
| `backend/scripts/classify-migration-state.ps1` | **Auto-classification.** Outputs one of A/B/C/D/E and REQUIRED ACTION. Optional: `-ReportPath "docs/operations/EF_MIGRATION_LAST_CLASSIFICATION.txt"` for review-time report. | After creating a migration; during PR review to see classification and action. |
| `backend/scripts/audit-migration-designers.ps1` | Lists all migrations missing Designer (read-only audit). | When you need the full list of the 47 script-only migrations. |

Run from repo root or `backend/`:

```powershell
.\backend\scripts\validate-migration-hygiene.ps1
.\backend\scripts\classify-migration-state.ps1
# Optional: write report for reviewers
.\backend\scripts\classify-migration-state.ps1 -ReportPath "docs/operations/EF_MIGRATION_LAST_CLASSIFICATION.txt"
```

**How to react to classification:** See **§8. Classification and required actions** below and `docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`.

---

## 8. Classification and required actions

Every migration state is classified automatically (A/B/C/D/E). React as follows:

| Classification | Meaning | Required action |
|----------------|---------|------------------|
| **A. NORMAL EF MIGRATION** | Newest migration has Designer; counts match baseline. | Proceed. When you add a new migration with Designer, update ExpectedTotalMainCount and ExpectedWithDesignerCount in the validator and docs. |
| **B/C. SCRIPT-ONLY (intentional vs accidental)** | New migration(s) without Designer; counts differ. | **If intentional:** Add to `EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` and update validator + docs. **If accidental:** Do not commit; recreate with `create-migration.ps1` so a Designer is generated. |
| **D. SNAPSHOT DRIFT RISK** | Newest migration has Designer but is unusually large. | Do not commit without review. Compare diff to intended changes; if scope is far larger, stop and escalate to snapshot reconciliation. |
| **E. BASELINE DRIFT** | Counts differ from authoritative baseline (142/95/47). | Align validator expected counts and all docs (manifest, MIGRATION_HYGIENE.md, runbook, PR checklist) with actual state. |

---

## 9. One-line rule

**Every new EF migration must be generated through the official EF command path, include a Designer file, and be reviewed for unexpected size before commit.**

---

## 10. Snapshot reconciliation outcome

A snapshot reconciliation audit was completed; see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md`. **No sync migration was created or kept** — drift was classified as moderate-to-large (trial migration was 909 lines with 15 new tables). Snapshot reconciliation is **deferred** to a re-baseline or a dedicated idempotent sync pass. If you see a migration named **SyncModelSnapshot_ReconciliationCheck**, it was a trial migration and must be removed with `dotnet ef migrations remove` before committing.

---

## 11. Graph integrity and InitialBaseline

- **Migration graph integrity** has been checked: the discoverable chain (94) and script-only set (47) are structurally coherent. See `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` and `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md`.
- **InitialBaseline** is a **future strategy only**. Do **not** perform a baseline cutover casually. Use `docs/operations/EF_INITIALBASELINE_STRATEGY.md` and `docs/operations/EF_BASELINE_CUTOVER_DECISION_MATRIX.md` when the team decides to adopt a two-track model (legacy vs baseline for new environments).
