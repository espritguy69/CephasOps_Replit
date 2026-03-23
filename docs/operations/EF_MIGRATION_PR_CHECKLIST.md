# EF Migration — PR / Review Checklist

Use this checklist when reviewing a PR that adds or touches migrations. Keep it concise; link to full workflow and rules for detail.

---

## Before merge

| Check | Pass? | Notes |
|-------|-------|--------|
| **Created via official path?** | ☐ | Migration was created with `backend/scripts/create-migration.ps1` or the documented `dotnet ef migrations add` command (Infrastructure project + Api startup). Not created by hand. |
| **Both .cs and .Designer.cs?** | ☐ | The new migration has both the main `.cs` and the `.Designer.cs` file. No new migration without a Designer. |
| **Hygiene validator passes?** | ☐ | `backend/scripts/validate-migration-hygiene.ps1` exits 0. No FAIL; warnings are acknowledged or fixed. |
| **Not unexpectedly large?** | ☐ | The new migration diff is in line with the intended model change. If it is huge (many tables/lines), it may be snapshot drift — do not merge without escalation. |
| **Snapshot drift?** | ☐ | The migration does not look like a full snapshot sync (hundreds of unrelated changes). If it does, stop and escalate to a dedicated snapshot reconciliation pass. |
| **Intended schema only?** | ☐ | The migration modifies only the schema that the PR describes. No unrelated table/column changes. |
| **Script-only migrations untouched?** | ☐ | None of the 47 script-only (no-Designer) migrations were modified, renamed, or deleted. |
| **No historical migration edits?** | ☐ | No existing migration files (any of the 142) were edited, renamed, reordered, or removed. If any were: **stop** and reject the PR unless there is an explicit, documented exception. |
| **Classification checked?** | ☐ | Run `backend/scripts/classify-migration-state.ps1` (or `validate-migration-hygiene.ps1 -Classify`). Reaction matches classification: A = ensure counts updated when new; B/C = manifest + counts or recreate; D = do not merge without snapshot drift review; E = align docs/validator. |

---

## If any check fails

- **Missing Designer:** Request the author to recreate the migration using `backend/scripts/create-migration.ps1` so both files are generated.
- **Validator fail:** Author must fix (e.g. ensure new migration has Designer; update expected counts only when adding a new migration with Designer).
- **Very large migration:** Request review for snapshot drift; do not merge until confirmed or escalated per `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md`.
- **Historical migration touched:** Reject unless there is a documented, approved reason. Default is do not touch.
- **Classification B/C (script-only):** If intentional, author must add to manifest and update counts; if accidental, author must recreate with create-migration.ps1. Do not merge with undocumented script-only migration.
- **Classification E (baseline drift):** Author or reviewer must update validator and docs so counts match repo before merge.

---

## Classification quick reference

| Classification | Reviewer action |
|----------------|-----------------|
| **A** (Normal EF) | Accept if validator passes; remind author to update counts when adding next migration. |
| **B/C** (Script-only) | Block until either: (1) migration added to manifest and counts updated, or (2) migration recreated with Designer. |
| **D** (Snapshot drift risk) | Block until migration size is reviewed and snapshot drift is confirmed or ruled out. |
| **E** (Baseline drift) | Block until validator expected counts and docs are updated to match repo. |

---

## References

- **Workflow:** `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md`
- **Auto-classification:** `docs/operations/EF_MIGRATION_AUTO_CLASSIFICATION_DECISION.md`, `backend/scripts/classify-migration-state.ps1`
- **Authoring rules:** `docs/operations/EF_FUTURE_AUTHORING_RULES.md`
- **Guardrail decision:** `docs/operations/EF_MIGRATION_GUARDRAIL_DECISION.md`
- **Next step / when to escalate:** `docs/operations/EF_MIGRATION_NEXT_STEP_DECISION.md`
