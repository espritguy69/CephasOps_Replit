# EF Migration Guardrail — Decision

**Purpose:** Record what protection was added, what was not automated, and what developers must do before opening a migration PR. Aligns with `docs/operations/EF_MIGRATION_FINAL_CLEANUP_DECISION.md`.

---

## 1. What protection was added

| Item | Description |
|------|-------------|
| **Gap analysis** | `docs/operations/EF_MIGRATION_GUARDRAIL_GAP_ANALYSIS.md` — documents current scripts, CI, docs, and missing guardrails. |
| **Official safe workflow** | `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` — strict workflow: always use EF command, verify Designer, check size, run validation script before commit. |
| **Validation script** | `backend/scripts/validate-migration-hygiene.ps1` — non-destructive checks: (a) fail if migrations missing Designer count ≠ 47, (b) report totals and consistency, (c) manifest check, (d) naming quality (warn on temp/test/fix etc.), (e) warn on suspiciously large migration files. Optional `-Classify` runs the classifier. Run from `backend/` or repo root; also run automatically by the official creation script. |
| **Auto-classification** | `backend/scripts/classify-migration-state.ps1` — outputs classification (A/B/C/D/E) and REQUIRED ACTION. Run after creation or during review. See `docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`. |
| **Official creation entry point** | `backend/scripts/create-migration.ps1` — single way to create migrations: runs `dotnet ef migrations add`, verifies both .cs and .Designer.cs for the new migration, runs the validator and classifier; on failure, tells developer to stop and review. See `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`. |
| **PR checklist** | `docs/operations/EF_MIGRATION_PR_CHECKLIST.md` — short checklist for reviewers: official path, both files, validator pass, classification, size/drift, no historical edits. |
| **Improvements audit** | `docs/operations/EF_MIGRATION_GUARDRAIL_IMPROVEMENTS_AUDIT.md` — what was strong, what was improved, what was not automated. |
| **Snapshot drift early-warning** | Added to `EF_SAFE_MIGRATION_WORKFLOW.md`, `EF_FUTURE_AUTHORING_RULES.md` (§8), and `MIGRATION_RUNBOOK.md`: if a newly generated migration is unexpectedly large, stop; do not commit blindly; escalate to snapshot reconciliation. |
| **CI guardrail** | `.github/workflows/migration-hygiene.yml` — runs on PR/push when migration `.cs` files change; runs `validate-migration-hygiene.ps1`; fails if a new migration is added without a Designer (or baseline count is wrong). No database update. |
| **Doc fixes** | Runbook and future rules: link to safe workflow and validation script; counts 141 / 94 / 47 (see baseline mismatch and post-baseline drift resolution). Auto-classification layer added; see `EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`. |

---

## 2. What was intentionally not automated

- **Snapshot repair:** Not performed. Snapshot drift remains deferred; only early-warning and guidance were added.
- **Designer regeneration:** No automation to add Designers to the 47 script-only migrations.
- **Migration reordering or renaming:** No tooling; docs only (do not touch).
- **DB-dependent checks in validation script:** The script only inspects files and counts; it does not connect to a database. CI does not run `dotnet ef database update` in the hygiene workflow.

---

## 3. Whether CI was changed

**Yes.** A new workflow was added:

- **File:** `.github/workflows/migration-hygiene.yml`
- **Trigger:** Pull requests and pushes to `main` and `development` when files under `backend/src/CephasOps.Infrastructure/Persistence/Migrations/**/*.cs` change.
- **Job:** Runs `backend/scripts/validate-migration-hygiene.ps1` on `ubuntu-latest` (PowerShell Core). Exits non-zero if the script fails (e.g. new migration without Designer).
- **No** change to existing workflows (e2e, versioning, etc.). No database update in this workflow.

---

## 4. What developers must do before opening a migration PR

1. Create the migration **only** via **`backend/scripts/create-migration.ps1`** (or the documented `dotnet ef migrations add` command). Do not create migration files by hand. The script verifies both files and runs the validator.
2. Confirm that **both** the main `.cs` and the `.Designer.cs` exist for the new migration (the script checks this).
3. Ensure **`validate-migration-hygiene.ps1`** passes (the script runs it; or run it manually before commit). Fix any failure or warning.
4. If the **new migration is unexpectedly large**, stop. Do not commit. Treat as snapshot drift and escalate per `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` and `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`.
5. Reviewers use **`docs/operations/EF_MIGRATION_PR_CHECKLIST.md`**. Follow the one-line rule below.

---

## 5. What still remains deferred to sync migration or re-baseline

- **Snapshot reconciliation:** The model snapshot may still be behind the domain model. A one-time sync migration or a full re-baseline is deferred (see `docs/operations/EF_REBASELINE_PLAN.md` and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md`).
- **Designer for the 47:** No regeneration; they remain script-only. Apply via scripts per manifest and runbook.
- **Archival:** No migrations were archived; all remain in `Migrations/` for traceability.

---

## 6. One-line rule for future migrations

**Every new EF migration must be generated through the official EF command path, include a Designer file, and be reviewed for unexpected size before commit.**

---

## 7. References

| Doc / asset | Role |
|-------------|------|
| `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` | Official workflow; snapshot drift early-warning; official script and validator. |
| `docs/operations/EF_FUTURE_AUTHORING_RULES.md` | Authoring rules; §8 snapshot drift. |
| `docs/operations/EF_MIGRATION_FINAL_CLEANUP_DECISION.md` | Final cleanup state (94 active, 45 script-only; no deletions/archival). |
| `docs/operations/EF_MIGRATION_PR_CHECKLIST.md` | PR review checklist. |
| `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md` | What was improved; single creation path; when to stop and escalate; next major concern. |
| `docs/operations/EF_MIGRATION_GUARDRAIL_IMPROVEMENTS_AUDIT.md` | Guardrail improvements audit. |
| `backend/scripts/MIGRATION_RUNBOOK.md` | Operations; official script; validation. |
| `backend/scripts/create-migration.ps1` | **Single official migration creation entry point.** |
| `backend/scripts/validate-migration-hygiene.ps1` | Local and CI validation. |
| `backend/scripts/classify-migration-state.ps1` | Auto-classification and required action (A/B/C/D/E). |
| `backend/scripts/check-migration-graph-integrity.ps1` | Optional graph check: counts, duplicate timestamps, latest discoverable, health. |
| `.github/workflows/migration-hygiene.yml` | CI guardrail on migration file changes; failure messaging. |
| `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` | Graph inventory and structural summary. |
| `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md` | Discoverable chain and script-only set integrity; safe for continued authoring. |
| `docs/operations/EF_INITIALBASELINE_STRATEGY.md` | Future InitialBaseline + current schema strategy (not executed). |
| `docs/operations/EF_BASELINE_CUTOVER_DECISION_MATRIX.md` | When to continue governed model vs idempotent sync vs InitialBaseline. |
