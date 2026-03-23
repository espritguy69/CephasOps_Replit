# EF Migration Auto-Classification — Decision

**Purpose:** Record what automatic classification was added, how it works, what developers and reviewers see, and what remains manual or deferred.

**Related:** `docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_AUDIT.md` (current capability and gaps), `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` (§8), `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`, `.cursor/rules/ef-migration-governance.mdc`.

---

## 1. What auto-classification was added

| Item | Description |
|------|-------------|
| **Classifier script** | `backend/scripts/classify-migration-state.ps1` — read-only, no DB. Compares migration folder state to authoritative baseline (142 total, 95 with Designer, 47 without). Identifies newest migration, checks Designer presence and size. Outputs one classification (A/B/C/D/E) and a REQUIRED ACTION. Optional `-ReportPath` writes a short report file for review. |
| **Validator -Classify** | `validate-migration-hygiene.ps1 -Classify` — runs the classifier after validation so one command gives both validation and classification. |
| **create-migration.ps1** | After creating a migration and running the validator, the creation script now runs the classifier and prints classification and category-specific next steps. On validator failure it still runs the classifier so the developer sees the classification and action. |
| **CI** | `.github/workflows/migration-hygiene.yml` — added a step that runs `classify-migration-state.ps1` (with continue-on-error) so CI logs show the classification and required action. Job pass/fail is still determined only by the validator. |
| **Docs** | `EF_SAFE_MIGRATION_WORKFLOW.md` (§7–8), `EF_MIGRATION_PR_CHECKLIST.md`, `MIGRATION_HYGIENE.md`, `EF_FUTURE_AUTHORING_RULES.md`, `EF_MIGRATION_GUARDRAIL_DECISION.md` updated to reference the classifier and how to react to each classification. |

---

## 2. How it works

1. **Baseline:** The classifier uses the same expected counts as the validator: 142 total main migrations, 95 with Designer, 47 without (script-only).
2. **Inputs:** It reads only the migrations folder (main `.cs` files, Designer presence, line count of newest). No database, no EF runtime.
3. **Logic:**
   - If **counts differ** from baseline → **E** (baseline drift). Subcases: extra no-Designer → E + B/C message (update manifest and counts, or recreate); extra with-Designer → E + “update counts.”
   - Else if **newest migration has Designer and is large** (>500 lines) → **D** (snapshot drift risk).
   - Else if **newest has Designer** and counts match → **A** (normal EF migration).
   - Else (counts match, newest has no Designer) → **A** (baseline state; newest is one of the 47 script-only).
4. **Output:** Console: `CLASSIFICATION: …` and `REQUIRED ACTION: …`. Optionally a report file with timestamp, newest migration ID, classification, and action.

---

## 3. What remains manual by design

- **B vs C:** The script cannot know whether a no-Designer migration was intentional (B) or accidental (C). It outputs a combined B/C message with both actions: if intentional, update manifest and counts; if accidental, recreate with create-migration.ps1. The developer or reviewer decides.
- **Updating baseline:** When a new normal EF migration is added, the developer must update ExpectedTotalMainCount and ExpectedWithDesignerCount in the validator and in docs. The classifier reminds; it does not edit files.
- **Snapshot reconciliation:** Deciding to run a full snapshot sync or re-baseline is a separate, documented process. The classifier only flags risk (D).
- **Historical migrations:** No automation edits or approves changes to historical migrations.

---

## 4. What developers see when a migration appears

- **After `create-migration.ps1`:** The script runs the validator, then the classifier. Console shows:
  - Validation result (PASS/FAIL and any warnings).
  - **CLASSIFICATION:** one of A / B or C / D / E.
  - **REQUIRED ACTION:** exact next step (e.g. “Update ExpectedTotalMainCount…”, “add to manifest and update counts”, “do not commit without review”, “align docs and validator”).
  - Next-steps bullets that reference the classification (A: update counts when adding next; D: do not commit until reviewed; etc.).
- **Optional report:** Running `classify-migration-state.ps1 -ReportPath "docs/operations/EF_MIGRATION_LAST_CLASSIFICATION.txt"` writes a short report for reviewers.

---

## 5. What reviewers must do

- Run **`backend/scripts/classify-migration-state.ps1`** (or **`validate-migration-hygiene.ps1 -Classify`**) when reviewing a PR that touches migrations.
- Use the **Classification quick reference** in `docs/operations/EF_MIGRATION_PR_CHECKLIST.md`:
  - **A:** Accept if validator passes; remind author to update counts when adding the next migration.
  - **B/C:** Block until migration is either added to manifest and counts updated, or recreated with Designer.
  - **D:** Block until migration size is reviewed and snapshot drift is confirmed or ruled out.
  - **E:** Block until validator expected counts and docs are updated to match the repo.
- Do not merge with an undocumented script-only migration or with baseline drift (counts/docs out of sync).

---

## 6. What is still deferred

- **Snapshot reconciliation:** A full snapshot sync or re-baseline is deferred. The classifier only flags D (snapshot drift risk); it does not run or recommend a specific sync.
- **Re-baseline:** Formal re-baseline for new environments remains per `docs/operations/EF_REBASELINE_PLAN.md`; not changed by this pass.
- **Designer regeneration:** No Designers are generated for the 47 script-only migrations; unchanged.

---

## 7. Authoritative baseline (unchanged)

- **142** total main migration `.cs` files  
- **95** EF-discoverable (with `.Designer.cs`)  
- **47** intentional script-only (no `.Designer.cs`)  

These values are the expected baseline until explicitly and safely changed. The classifier and validator both use them.
