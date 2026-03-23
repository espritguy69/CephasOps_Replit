# EF Migration — Next Step Decision

**Purpose:** State what was improved in this pass, the single official migration-creation path, what the next developer must do, what remains deferred, when to stop and escalate, and whether snapshot reconciliation is the next major concern.

---

## 1. What autonomous improvements were added in this pass

| Improvement | Description |
|-------------|-------------|
| **Guardrail improvements audit** | `docs/operations/EF_MIGRATION_GUARDRAIL_IMPROVEMENTS_AUDIT.md` — what is strong, what was manual, what was automated, what was not. |
| **Stronger validation script** | `validate-migration-hygiene.ps1`: naming-quality check (warn on temp/test/fix/migration1 etc.), final PASS/WARN/FAIL summary, "What to do:" for each warning, snapshot-misuse note. |
| **Single official creation path** | `backend/scripts/create-migration.ps1`: runs `dotnet ef migrations add` with correct paths, verifies new migration has both .cs and .Designer.cs, runs the validator, and tells the developer to stop and review if validation fails. |
| **PR checklist** | `docs/operations/EF_MIGRATION_PR_CHECKLIST.md` — short checklist for reviewers: official path, both files, validator pass, size/drift, no historical edits. |
| **CI messaging** | On failure, the migration-hygiene workflow now runs a step that prints what to do and links to the workflow and PR checklist. |
| **Doc alignment** | Safe workflow, future rules, runbook, guardrail decision, and MIGRATION_HYGIENE.md updated to reference the official script, validator, and PR checklist. |

---

## 2. The single official migration-creation path

**One way to create a migration:**

- **From repo root:**  
  `.\backend\scripts\create-migration.ps1 -MigrationName "YourDescriptiveName"`

- **From backend/:**  
  `.\scripts\create-migration.ps1 -MigrationName "YourDescriptiveName"`

The script will:
1. Run `dotnet ef migrations add` with the Infrastructure project and Api startup project.
2. Verify the newest migration has both `.cs` and `.Designer.cs`.
3. Run `validate-migration-hygiene.ps1`.
4. If validation fails, print "Stop and review" and exit 1; do not commit.

**Alternative (same outcome required):** Run the equivalent `dotnet ef migrations add` command from the Api directory as documented in `EF_SAFE_MIGRATION_WORKFLOW.md`, then run the validator yourself. The script is the recommended single entry point.

---

## 3. What the next developer should do before creating any new migration

1. Read `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` and `docs/operations/EF_FUTURE_AUTHORING_RULES.md`.
2. Use **only** `backend/scripts/create-migration.ps1` (or the documented `dotnet ef migrations add` command). Do not create migration files by hand.
3. Ensure the migration has a **descriptive name** (e.g. `AddOrderStatusChecklist`), not `temp`, `test`, `fix`, or `migration1`.
4. After creation, the script runs the validator; if it fails or warns, **stop and fix** before committing.
5. If the new migration is **unexpectedly large**, do not commit; treat as snapshot drift and escalate (see below).
6. Before opening a PR, run `validate-migration-hygiene.ps1` again and ensure it passes. Reviewers will use `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`.

---

## 4. What remains deferred

- **Snapshot reconciliation:** A reconciliation **audit** was run (see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md`). **No sync migration was created or kept** — the trial migration was 909 lines and classified as suspiciously large; reconciliation is deferred to re-baseline or a dedicated idempotent sync pass. If a trial migration **SyncModelSnapshot_ReconciliationCheck** exists, run `dotnet ef migrations remove` before committing.
- **Designer for the 47:** No regeneration. The 47 script-only migrations remain without Designers; apply via scripts per manifest and runbook.
- **Archival / re-baseline:** No migrations archived; no re-baseline in this pass.
- **Pre-commit hooks:** Not added; CI and the official script are the enforcement. Hooks can be added locally if desired and documented.

---

## 5. When to stop and escalate instead of proceeding

- **New migration is huge:** If the generated migration is far larger than your intended model change (many tables, hundreds of lines), **stop**. Do not commit. Escalate to a dedicated snapshot reconciliation pass.
- **Validator fails:** Do not commit until it passes (or you have a documented, approved exception). Fix by using the official creation path and ensuring the new migration has a Designer.
- **You edited the Snapshot by hand:** If you changed `ApplicationDbContextModelSnapshot.cs` without adding a migration, **stop**. Add a migration or revert the snapshot change.
- **You need to change an existing migration:** Do not edit, rename, or delete historical migrations without an explicit, documented, approved reason. Default is **stop** and escalate.
- **You need to add a migration without a Designer:** Do not. Every new migration must have a Designer. The 47 script-only set is closed; new schema goes through the EF path.

---

## 6. Is snapshot reconciliation the next major migration concern?

**Yes.** A snapshot reconciliation **audit** was completed: see `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` and `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_DECISION.md`. **No sync migration was kept** (trial migration was too large; decision: deferred re-baseline). The codebase has:

- A clear single creation path and validator.
- PR checklist and CI to block new migrations without Designer.
- Early-warning guidance for large migrations.
- A documented snapshot audit and decision (reconciliation deferred).

**Graph integrity:** The migration graph (95 discoverable, 47 script-only) has been audited and is structurally coherent; see `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md` and `docs/operations/EF_MIGRATION_GRAPH_AND_BASELINE_FINAL_DECISION.md`. **InitialBaseline** is a future strategy only; do not perform a baseline cutover without the cutover matrix and team decision.

The snapshot is still **not** guaranteed in sync with the current model. So:

- **Day-to-day:** Add new migrations via the official script; keep them small and intentional.
- **Next major pass:** When the team is ready, either (a) re-baseline for new environments per `EF_REBASELINE_PLAN.md`, or (b) implement a dedicated, idempotent sync migration with full review. Do not do that as part of a normal feature PR.

---

## 7. References

| Doc | Role |
|-----|------|
| `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` | Official workflow; how to create; snapshot drift. |
| `docs/operations/EF_FUTURE_AUTHORING_RULES.md` | Authoring rules; what never to do. |
| `docs/operations/EF_MIGRATION_PR_CHECKLIST.md` | PR review checklist. |
| `docs/operations/EF_MIGRATION_GUARDRAIL_DECISION.md` | What guardrails were added; one-line rule. |
| `docs/operations/EF_MIGRATION_GUARDRAIL_IMPROVEMENTS_AUDIT.md` | Improvements audit. |
| `backend/scripts/create-migration.ps1` | Single official creation entry point. |
| `backend/scripts/validate-migration-hygiene.ps1` | Hygiene validation. |
