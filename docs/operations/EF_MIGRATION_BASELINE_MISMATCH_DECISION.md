# EF Migration Baseline Mismatch — Decision

**Outcome:** **D. One migration is intentionally script-only but not yet reflected everywhere.**

The migration **20260309230000_AddJobExecutions** is script-only (no Designer in repo) but was never added to the no-Designer manifest or to the documented 44 list. The true baseline is **94 with Designer, 45 without** (total 139).

---

## Classification

| Option | Chosen | Notes |
|--------|--------|--------|
| A. Documentation mismatch only | — | Docs were wrong; validator was also wrong (expected 44). |
| B. Validator baseline mismatch only | — | Validator and docs both used 95/44. |
| C. One migration accidentally lost its Designer | No | No evidence of a removed Designer; migration is script-only by history. |
| **D. One migration intentionally script-only but not yet reflected everywhere** | **Yes** | 20260309230000_AddJobExecutions is script-only and was omitted from the 44 list and from counts. |

---

## Resolution

- Update the **authoritative baseline** to **94** with Designer and **45** without.
- Add **20260309230000_AddJobExecutions** to the no-Designer manifest and to the full audit script-only list.
- Update **validate-migration-hygiene.ps1** expected counts to `ExpectedNoDesignerCount = 45`, `ExpectedWithDesignerCount = 94`, `ExpectedTotalMainCount = 139`.
- Update all docs that state 95/44 to **94/45**.
- Do **not** regenerate a Designer; do **not** modify any historical migration file or snapshot.

---

**Future repair:** No technical repair (no Designer regeneration) was performed. 20260309230000_AddJobExecutions remains script-only. Adding a Designer is deferred; apply schema via idempotent script when needed. Only documentation and validator baseline were updated.

---

## References

- **Audit:** `docs/operations/EF_MIGRATION_BASELINE_MISMATCH_AUDIT.md`
- **Manifest:** `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`
- **Full audit list:** `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2
