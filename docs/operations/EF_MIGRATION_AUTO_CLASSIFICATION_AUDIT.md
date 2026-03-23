# EF Migration Auto-Classification — Audit

**Date:** Auto-classification layer pass.  
**Purpose:** Document current detection capability, gaps, and what will be automated vs left manual.

---

## 1. Current authoritative baseline

- **141** total main migration `.cs` files  
- **94** EF-discoverable (with `.Designer.cs`)  
- **47** intentional script-only (no `.Designer.cs`)  

Source: `backend/scripts/validate-migration-hygiene.ps1`, `docs/operations/EF_POST_BASELINE_DRIFT_DECISION.md`.

---

## 2. Current detection capability

### 2.1 validate-migration-hygiene.ps1

| Check | What it does | Implicit classification |
|-------|----------------|---------------------------|
| Count main files | Lists all `.cs` excluding Designer and Snapshot | — |
| Count missing Designer | Collects main files that have no matching `.Designer.cs` | — |
| Compare to expected | Fails if missing count ≠ 47; warns if total or withDesigner ≠ expected | **E** (baseline drift) when counts differ; **C**-like when missing > expected (treats as "new without Designer") |
| Manifest exists | Checks `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` exists | — |
| Bad names | Warns on temp/test/fix/migration1/etc. | Quality signal only |
| Large files | Warns when any main file > 500 lines | **D** (snapshot drift risk) when new migration is large |

**Strengths:** Correct counts, fail on extra no-Designer, warn on size and naming. No DB dependency; read-only.  
**Gaps:** No explicit A/B/C/D/E label. No "newest migration" focus. No single-line classification summary. Human must infer category from fail/warn text. No guidance that distinguishes "intentional script-only" (B) vs "accidental incomplete" (C).

### 2.2 create-migration.ps1

| Behavior | What it does |
|----------|----------------|
| Creates migration | Runs `dotnet ef migrations add` with correct project/context/output-dir |
| Verifies Designer | Checks newest main file has matching `.Designer.cs`; exits 1 if not |
| Runs validator | Invokes validate-migration-hygiene.ps1 after creation |
| On validator fail | Prints "STOP AND REVIEW" and next steps (generic) |

**Strengths:** Single entry point; ensures Designer exists for just-created migration; runs validator.  
**Gaps:** No explicit classification output. No category-specific message (e.g. "A: next step X" vs "D: do not commit"). If validator passes, script does not print a classification label or "required next action" for reviewers.

### 2.3 .github/workflows/migration-hygiene.yml

| Behavior | What it does |
|----------|----------------|
| Trigger | PR/push to main or development when `backend/.../Migrations/**/*.cs` change |
| Step | Runs `validate-migration-hygiene.ps1` from backend/ |
| On failure | Echoes error and docs; job fails |

**Strengths:** CI enforces validator; no DB; narrow path trigger.  
**Gaps:** Logs show only validator output; no explicit "CLASSIFICATION: …" in logs. Reviewer must infer from fail message.

### 2.4 .cursor/rules/ef-migration-governance.mdc

Defines categories A–E and governance rules; used by AI/human when working on migrations. No automated classification; human/agent must apply the model.

### 2.5 Existing migration docs

- **EF_SAFE_MIGRATION_WORKFLOW.md:** Steps for creation, verification, size check; mentions snapshot drift; one place still says "44" script-only (should be 47).
- **EF_MIGRATION_PR_CHECKLIST.md:** Checklist items; references validator; one place says "44 script-only" (should be 47).
- **EF_FUTURE_AUTHORING_RULES.md:** Rules and re-baseline guidance.
- **EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md:** List of 47 script-only migrations.

Docs describe what to do but do not reference an explicit "classification" or a single script that prints it.

---

## 3. What will be automated

- **Explicit classification label:** A script (new or integrated) will output one of: **A** (Normal EF), **B/C** (Script-only — intentional vs accidental left to human), **D** (Snapshot drift risk), **E** (Baseline drift).
- **Newest migration focus:** Classifier will identify the newest migration by filename and report whether it has a Designer and whether it is large.
- **Actionable summary:** One-line classification plus "Required next action" (e.g. "Update manifest and counts" vs "Do not commit; recreate with create-migration.ps1").
- **Post-create feedback:** After `create-migration.ps1` runs the validator, it will run the classifier and print the classification and category-specific next steps.
- **Optional report artifact:** Classifier can optionally write a short report (e.g. `docs/operations/EF_MIGRATION_LAST_CLASSIFICATION.txt` or console-only) with newest migration ID, Designer presence, classification, and required action for review-time use.

---

## 4. What will remain manual by design

- **B vs C distinction:** Automation cannot know if a no-Designer migration was intentional or accidental. The classifier will report "Script-only (B or C)" and give both actions; human/reviewer decides.
- **Whether to update baseline:** When a new normal EF migration is added, the decision to increment expected counts and update docs is human (or a separate, explicit "I added one with Designer" step). The classifier will remind that counts must be updated when applicable.
- **Snapshot reconciliation:** Deciding to run a full snapshot sync or re-baseline remains a documented, deliberate process; the classifier only flags risk (D).
- **Historical migration edits:** No automation will approve or modify historical migrations; governance rules and PR checklist remain the authority.

---

## 5. Summary

| Area | Current strength | Gap | Automation added |
|------|------------------|-----|-------------------|
| Counts | Validator enforces expected no-Designer and warns on total/withDesigner | No explicit E label | Classifier outputs E when counts differ |
| Newest migration | create-migration verifies Designer for just-created | No classification label or category-specific action | Classifier identifies newest; outputs A/D; create-migration prints it |
| No-Designer new | Validator fails when missing > 47 | No B vs C; no "update manifest" vs "recreate" guidance | Classifier outputs script-only (B/C) with both actions |
| Large migration | Validator warns on line count | No explicit D label | Classifier outputs D when newest is large |
| Review | PR checklist and workflow docs | No single "classification" summary | Optional report or console summary with classification and action |
