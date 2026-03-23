# EF Core migration hygiene

This document provides a migration inventory, lists migrations missing Designer files, and describes the long-term migration strategy. See also `EF_MIGRATION_STABILIZATION.md` for the index-drift fix and `backend/scripts/MIGRATION_RUNBOOK.md` for day-to-day operations.

---

## 1. Migration inventory

- **Location:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`
- **Snapshot:** `ApplicationDbContextModelSnapshot.cs` (single source of truth for current model).
- **Total migrations (main .cs):** 142 (from audit script `backend/scripts/audit-migration-designers.ps1`).
- **With Designer:** 95 have a matching `.Designer.cs` and are **discovered** by `dotnet ef migrations list` and `dotnet ef database update`.
- **Without Designer:** 47. EF tooling does not list or apply them in the normal chain. See **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** for the full inventory and **`docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`** for recovery classification.

---

## 2. Migrations missing .Designer.cs

These migrations have a main `.cs` file but **no** `.Designer.cs`. They are **not** discovered by `dotnet ef migrations list` / `dotnet ef database update` unless a Designer is added. **Count: 47** (from `backend/scripts/audit-migration-designers.ps1`). The full list and apply/repair script references are in **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** (§2.2). Recovery classification (script-only vs manual intervention) is in **`docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`**. **Auto-classification** of new migrations (A/B/C/D/E) is in **`docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`**; run `backend/scripts/classify-migration-state.ps1` for classification and required action.

**Duplicate timestamps:** 20260308140000, 20260308150000, 20260308180000, 20260309120000, 20260309150000, 20260309180000, 20260309190000, 20260309200000, 20260310180000, 20260311120000 each have two migrations; ordering is by full migration ID. Do not rename without explicit need and documentation.

---

## 3. Discovered migration chain vs __EFMigrationsHistory

- **Discovered chain:** What `dotnet ef migrations list` shows is determined by migrations that have a `.Designer.cs` with `[Migration("...")]`. There are **94** such migrations; the **last** is **20260309120000_AddJobRunEventId**. This is the **active EF path**; do not delete or archive any of them.
- **__EFMigrationsHistory:** May contain rows that were applied via idempotent SQL scripts and then inserted manually (e.g. **20260311120000_AddPayoutAnomalyReview**). So the database can be “ahead” of the discovered chain.
- **47 no-Designer migrations** are **not** applied by `dotnet ef database update`. Apply them via idempotent or repair scripts when required. See runbook and `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`. Do not delete or rename historical migrations; formal re-baseline only per `docs/operations/EF_REBASELINE_PLAN.md`.

---

## 4. Recommended long-term migration strategy

1. **New migrations**
   - Always create migrations with the EF Core tools:  
     `dotnet ef migrations add <Name> --project backend/src/CephasOps.Infrastructure --startup-project backend/src/CephasOps.Api`  
   - This generates both the main `.cs` and the `.Designer.cs`. Do not delete the Designer.

2. **Applying migrations**
   - **Preferred:** `dotnet ef database update` when the discovered chain is intact and no known drift.
   - **When to use idempotent SQL:** Restore from backup, production deploy where you need a single script, or when `dotnet ef database update` fails due to drift. Generate with:  
     `dotnet ef migrations script --idempotent --output <path> --project backend/src/CephasOps.Infrastructure --startup-project backend/src/CephasOps.Api`
   - **After applying a script manually:** Insert the corresponding row(s) into `__EFMigrationsHistory` only for migrations that were actually applied by that script (see runbook).

3. **Existing migrations without Designer**
   - **Do not** regenerate Designer files for already-applied migrations without a clear need and a safe process (snapshot/chain can be corrupted). Treat them as “legacy”; apply schema changes via idempotent scripts and repair scripts where needed.
   - For **new** migrations, always keep the Designer so they stay in the discovered chain.

4. **Drift and repair**
   - Use `backend/scripts/check-migration-state.sql` (and any audit script in the repo) to compare `__EFMigrationsHistory` with schema (e.g. PasswordResetTokens, key indexes).
   - Use `backend/scripts/repair-password-reset-tokens-schema.sql` when the PasswordResetTokens table/indexes are missing.
   - Document any one-off repair in `EF_MIGRATION_STABILIZATION.md` or this file.

5. **Baseline / new environments**
   - For a new DB from backup: run the idempotent migration script (see AGENTS.md) then run any repair scripts if the backup predates certain migrations.
   - Do not reset the database or drop tables to “fix” history; use additive, idempotent fixes only.

6. **Snapshot reconciliation**
   - A snapshot reconciliation audit was run; see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md`. **No sync migration was kept** (drift was moderate-to-large; deferred to re-baseline or a dedicated idempotent sync pass). If a trial migration **SyncModelSnapshot_ReconciliationCheck** is present, remove it with `dotnet ef migrations remove` before committing.

---

## 5. Risks still remaining

- **Migrations without Designer:** The listed migrations are not in the discovered chain; `dotnet ef database update` will not apply them. Rely on idempotent scripts and repair scripts.
- **Duplicate timestamps:** Ten timestamps have two migrations each (see full audit); ordering is by full migration ID. Do not rename. Be consistent when generating scripts or inserting into history.
- **History gaps:** If history was built from scripts and manual inserts, there can be gaps (e.g. 20260308190000 missing from history). Schema repair scripts do not insert into history; document any manual insert.
- **Other environments:** Stale migration code (e.g. old 20260308163857 without idempotent drop) can still cause failures until the fixed migration is deployed.

---

## 6. Graph integrity and InitialBaseline (governance)

- **Migration graph integrity** has been checked and documented: see `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` and `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md`. The discoverable chain (94) and script-only set (47) are structurally coherent and fully governed.
- **InitialBaseline** is a **future strategy only**; see `docs/operations/EF_INITIALBASELINE_STRATEGY.md` and `docs/operations/EF_BASELINE_CUTOVER_DECISION_MATRIX.md`. **No one should perform a baseline cutover casually**; use the cutover matrix and team decision before creating or applying an InitialBaseline.
- **Graph check script:** `backend/scripts/check-migration-graph-integrity.ps1` (optional; complements the validator; reports counts, duplicate timestamps, latest discoverable, graph health).

---

## 7. Files and scripts reference

| Item | Path |
|------|------|
| **Graph integrity audit** | `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` |
| **Graph integrity decision** | `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md` |
| **InitialBaseline strategy** | `docs/operations/EF_INITIALBASELINE_STRATEGY.md` |
| **Baseline cutover matrix** | `docs/operations/EF_BASELINE_CUTOVER_DECISION_MATRIX.md` |
| Schema reconciliation pass | `docs/operations/EF_SCHEMA_RECONCILIATION_PASS.md` |
| No-Designer migrations manifest | `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` |
| Operational closure decision | `docs/operations/EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md` |
| **Migration recovery (full audit)** | `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` |
| **No-Designer recovery classification** | `docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md` |
| **Snapshot reconciliation report** | `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md` |
| **Re-baseline plan** | `docs/operations/EF_REBASELINE_PLAN.md` |
| **Recovery validation** | `docs/operations/EF_MIGRATION_RECOVERY_VALIDATION.md` |
| **Recovery final decision** | `docs/operations/EF_MIGRATION_RECOVERY_DECISION.md` |
| **Final cleanup audit** | `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md` |
| **Snapshot reconciliation audit** | `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` |
| **Snapshot reconciliation decision** | `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md` |
| **Baseline mismatch audit** | `docs/operations/EF_MIGRATION_BASELINE_MISMATCH_AUDIT.md` |
| **Baseline mismatch decision** | `docs/operations/EF_MIGRATION_BASELINE_MISMATCH_DECISION.md` |
| **Future authoring rules** | `docs/operations/EF_FUTURE_AUTHORING_RULES.md` |
| **Safe migration workflow** | `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` |
| **Guardrail decision** | `docs/operations/EF_MIGRATION_GUARDRAIL_DECISION.md` |
| **Guardrail gap analysis** | `docs/operations/EF_MIGRATION_GUARDRAIL_GAP_ANALYSIS.md` |
| **Guardrail improvements audit** | `docs/operations/EF_MIGRATION_GUARDRAIL_IMPROVEMENTS_AUDIT.md` |
| **Migration PR checklist** | `docs/operations/EF_MIGRATION_PR_CHECKLIST.md` |
| **Next step / when to escalate** | `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md` |
| Stabilization (index drift) | `docs/EF_MIGRATION_STABILIZATION.md` |
| Migration runbook | `backend/scripts/MIGRATION_RUNBOOK.md` (includes validation blocker, execution by environment) |
| Audit DB state | `backend/scripts/check-migration-state.sql` |
| Audit migration files | `backend/scripts/audit-migration-designers.ps1` |
| **Official migration creation** | `backend/scripts/create-migration.ps1` (single entry point; runs validator) |
| **Validate migration hygiene** | `backend/scripts/validate-migration-hygiene.ps1` (run before commit / CI) |
| **Check graph integrity** | `backend/scripts/check-migration-graph-integrity.ps1` (optional; counts, duplicate timestamps, latest discoverable) |
| PasswordResetTokens repair | `backend/scripts/repair-password-reset-tokens-schema.sql` |
| PayoutAnomalyReview idempotent | `backend/scripts/apply-payout-anomaly-review-migration.sql` |
| Migration bundle (generate) | See `backend/scripts/MIGRATION_RUNBOOK.md` — `dotnet ef migrations bundle --output ../../scripts/CephasOps.MigrationsBundle.exe ...` (output is gitignored; build in CI or pre-deploy). |

---

## 8. Deliverable summary (hygiene pass)

- **Migration inventory:** Section 1 above (**142** total, **95** with Designer, **47** without). See `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md` and run `backend/scripts/audit-migration-designers.ps1`.
- **Missing Designer list:** Section 2; also produced by `audit-migration-designers.ps1`.
- **Long-term strategy:** Section 4 (new migrations with EF tools, when to use update vs idempotent script, no Designer regeneration for already-applied migrations, drift repair, baseline).
- **Files added/updated:**
  - **Added:** `docs/MIGRATION_HYGIENE.md`, `backend/scripts/MIGRATION_RUNBOOK.md`, `backend/scripts/audit-migration-designers.ps1`.
  - **Updated:** `docs/EF_MIGRATION_STABILIZATION.md` (links to runbook and hygiene).
- **Risks remaining:** Section 5 (migrations without Designer, duplicate timestamps, history gaps, other environments).
