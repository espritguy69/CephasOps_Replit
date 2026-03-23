# EF Migration Guardrail — Improvements Audit

**Purpose:** Audit the current guardrail system and document what is strong, what remains manual, what can be safely automated next, and what should not be automated yet.

**Source of truth:** 139 total migrations, 95 active (with Designer), 44 script-only (no Designer). No historical migrations modified.

---

## 1. What is already strong

| Area | Current state |
|------|----------------|
| **Designer enforcement** | `validate-migration-hygiene.ps1` fails when migrations missing Designer count ≠ 44. Prevents new script-only migrations from slipping in. |
| **CI guardrail** | `.github/workflows/migration-hygiene.yml` runs the validator on PR/push when migration files change. No DB dependency. |
| **Documentation** | `EF_SAFE_MIGRATION_WORKFLOW.md`, `EF_FUTURE_AUTHORING_RULES.md`, `MIGRATION_RUNBOOK.md`, and `EF_MIGRATION_GUARDRAIL_DECISION.md` define the one-line rule, snapshot drift early-warning, and do-not-touch rules. |
| **Creation helper** | `create-migration.ps1` runs `dotnet ef migrations add` with correct project paths. Does not yet verify Designer or run the validator. |
| **Large-file warning** | Validator warns when any migration main file exceeds 500 lines (snapshot drift signal). |
| **Count consistency** | Validator reports total / with Designer / missing and warns when totals diverge from expected (139 / 95 / 44). |

---

## 2. What remains manual

| Item | Risk | Mitigation |
|------|------|------------|
| Running the validator after creating a migration | Developer may forget; PR catches it but only after commit. | Run the validator automatically from `create-migration.ps1` after generation. |
| Checking that the new migration has both .cs and .Designer.cs | Easy to miss if only the script is used and something goes wrong. | Have `create-migration.ps1` verify both files exist for the newly created migration and run the validator. |
| Migration naming quality | Names like `temp`, `fix`, `migration1` are not caught. | Add a naming-quality check in the validator (warn on obviously bad names). |
| Snapshot-only changes | Someone could edit `ApplicationDbContextModelSnapshot.cs` without adding a migration. | Document the check; optional lightweight heuristic (e.g. warn if Snapshot was modified and no new migration file in same timeframe) only if non-brittle. Prefer documentation. |
| PR review consistency | Reviewers may not have a shared checklist. | Add `EF_MIGRATION_PR_CHECKLIST.md` and reference it from workflow and guardrail decision. |

---

## 3. What can be safely automated next

| Improvement | Implementation | Safe? |
|-------------|----------------|------|
| **Official creation path** | Make `create-migration.ps1` the single entry point: run `dotnet ef migrations add`, verify both .cs and .Designer.cs for the new migration, run `validate-migration-hygiene.ps1`, and surface a clear stop/review message if validation fails or warns. | Yes. Additive; no migration logic changed. |
| **Validator: naming quality** | In the validator, for each migration main file, extract the name part (after timestamp). If it matches bad patterns (e.g. `temp`, `test`, `fix`, `aaa`, `migration1`, `newmigration`, `migration`), emit a WARN with guidance. Keep the list small and practical. | Yes. Read-only; no false positives if patterns are conservative. |
| **Validator: clearer output** | Add a final summary (PASS / WARN / FAIL) and for each WARN a short "What to do:" line. | Yes. |
| **Validator: snapshot misuse** | Document only: "If you changed ApplicationDbContextModelSnapshot.cs without adding a migration, stop and add a migration or revert." Optionally: warn if Snapshot is newer than the latest migration file (by write time). Latter can be brittle; prefer documentation. | Doc only is safe; optional heuristic is low confidence. |
| **PR checklist** | Add `EF_MIGRATION_PR_CHECKLIST.md` with a short operational checklist. Link from workflow and guardrail docs. | Yes. |

---

## 4. What should NOT be automated yet

| Item | Reason |
|------|--------|
| Snapshot repair / reconciliation | Deferred to a dedicated pass; not part of guardrails. |
| Regenerating Designers for the 44 | Would change historical state; explicitly out of scope. |
| DB-dependent checks in the validator | Keeps CI and local runs simple; no connection string or DB required. |
| Blocking commits (pre-commit hook) | Not all repos use hooks; CI already blocks PR merge. Document as optional. |
| Detecting "which migration is new" via git | Possible but adds git dependency and branch logic; count-based check is sufficient. |

---

## 5. Proposed next-step implementation

1. **Strengthen `validate-migration-hygiene.ps1`**  
   - Add naming-quality check (bad patterns → WARN).  
   - Add final PASS/WARN/FAIL summary and "What to do:" for warnings.  
   - Optionally: brief snapshot-misuse note in output or docs (no brittle file-time check).

2. **Make `create-migration.ps1` the single official entry point**  
   - Keep existing `dotnet ef migrations add` invocation; fix paths for cross-platform.  
   - After add: resolve the newest migration (by name/timestamp), verify both .cs and .Designer.cs exist.  
   - Run `validate-migration-hygiene.ps1`.  
   - If validation fails or reports warnings: print clear "Stop and review" message; do not auto-apply or auto-commit.

3. **Add `EF_MIGRATION_PR_CHECKLIST.md`**  
   - Short checklist: official path, both files, validator pass, size/drift, no historical edits.

4. **Align docs**  
   - Point all workflow/guardrail/runbook references to: official script, validator, PR checklist, next-step decision.

5. **Add `EF_MIGRATION_NEXT_STEP_DECISION.md`**  
   - What was improved this pass; single creation path; when to stop and escalate; snapshot reconciliation as next major concern.

6. **CI**  
   - Optionally improve workflow step description so failure message is clearer; no new triggers or DB.

---

## 6. Summary

- **Strong today:** Designer count enforcement, CI on migration changes, docs and one-line rule, large-file warning.  
- **Improve:** Single creation path with post-add verification and validator run; naming warning; clearer validator output; PR checklist; next-step doc.  
- **Do not automate yet:** Snapshot repair, Designer regeneration, DB checks, brittle snapshot-only detection.
