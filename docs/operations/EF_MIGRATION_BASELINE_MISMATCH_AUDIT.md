# EF Migration Baseline Mismatch — Audit

**Purpose:** Identify the exact migration causing the baseline mismatch between expected (95 with Designer / 44 without) and observed (94 with Designer / 45 without).

---

## 1. Mismatch summary

| Source | Total main | With Designer | Missing Designer |
|--------|------------|----------------|-------------------|
| **Expected (docs/validator)** | 139 | 95 | 44 |
| **Observed (audit script)** | 139 | 94 | 45 |

One migration is counted differently: the workspace has **one more** migration without a Designer than the documented 44, so **one fewer** with Designer than the documented 95.

---

## 2. Exact migration causing the mismatch

| Field | Value |
|-------|--------|
| **Migration name** | 20260309230000_AddJobExecutions |
| **Timestamp** | 20260309230000 |
| **.Designer.cs exists** | **No** (in workspace) |
| **Discoverable by EF** | No (no Designer) |
| **In 44 script-only list (docs)** | **No** — it is **not** listed in `EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2 or in `EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`. |
| **In 95 discoverable list (docs)** | No (the 95 list ends at 20260309120000_AddJobRunEventId). |

**Conclusion:** **20260309230000_AddJobExecutions** is a script-only migration (no Designer in repo) but was **never included** in the documented list of 44 no-Designer migrations. So the true script-only count is **45**, and the true discoverable count is **94**.

---

## 3. Whether the missing Designer is new, historical, intentional, or accidental

- **Historical:** The migration file exists and has no Designer in the repo; it was never in the “discoverable” list in the full audit.
- **Intentional script-only:** It fits the pattern of other JobRuns/JobExecution-related script-only migrations (e.g. 20260309100000_AddJobRunsTable, 20260309110000_AddRetriedFromJobRunIdToBackgroundJob) that are in the 44 list. It was simply **omitted** from the documentation when the 44 list was compiled.
- **Not accidental loss:** There is no evidence that a Designer was removed; the migration is script-only by presence and was never documented as discoverable.

---

## 4. Whether the manifest already includes it

**No.** The manifest (`EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`) and the full audit §2.2 list do **not** include 20260309230000_AddJobExecutions. The manifest table jumps from 20260309220000_AddNotificationDispatches to 20260310120000_AddSnapshotProvenanceAndRepairRunHistory (and other 20260310* entries). So the manifest is **incomplete** by one migration.

---

## 5. Whether docs and validator baseline are inconsistent

**Yes.** The docs and validator assert 95 with Designer and 44 without. The **authoritative** count from `audit-migration-designers.ps1` is 94 with Designer and 45 without. So:

- **Validator:** Expects 44 missing → fails when 45 are missing.
- **Docs:** State 95 discoverable / 44 script-only → should state 94 / 45 to match the repo.

---

## 6. Recommended resolution

**Treat as documentation/validator baseline mismatch.**

- **Root cause:** One script-only migration (20260309230000_AddJobExecutions) was never added to the no-Designer list or to the expected counts.
- **Resolution:** Update the **authoritative baseline** to **94 with Designer, 45 without** (total 139 unchanged). Do **not** regenerate a Designer; do **not** modify the migration file. Add 20260309230000_AddJobExecutions to the no-Designer manifest and to the full audit §2.2 (as the 45th). Update the validator expected counts to 45 and 94. Update all docs that state 95/44 to 94/45.
