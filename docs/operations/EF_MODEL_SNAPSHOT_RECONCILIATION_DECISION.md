# EF Model Snapshot — Reconciliation Decision

**Date:** Snapshot reconciliation pass.  
**Outcome:** **DEFERRED REBASELINE.** No sync migration was kept. Snapshot remains as-is; reconciliation is deferred to a dedicated re-baseline or idempotent sync pass.

---

## 1. Decision: safe sync migration vs deferred re-baseline

**Chosen path: DEFERRED REBASELINE (no sync migration kept).**

A trial sync migration was generated to measure drift. It was **909 lines**, with **15 CreateTable** operations and many **AddColumn** operations. Per Phase 4 of the reconciliation instructions:

- The migration exceeds the project’s “suspiciously large” threshold (e.g. > 500 lines).
- It is **not** idempotent: CreateTable would fail on databases that already have those tables (from script-only migrations).
- Review and testing burden is high; risk of apply failures and rollback complexity is non-trivial.

Therefore the trial migration was **not** kept. Snapshot reconciliation is **deferred** to either:

1. **Re-baseline** for new environments (see `docs/operations/EF_REBASELINE_PLAN.md`), or  
2. A **dedicated pass** that designs and implements a one-off, idempotent sync migration (e.g. CREATE TABLE IF NOT EXISTS, ADD COLUMN IF NOT EXISTS) with full review and testing.

---

## 2. What was done in this pass

- **Phase 1:** Audited snapshot vs current model; created `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` with snapshot status, entities missing/extra, schema differences, estimated migration size, and recommendation.
- **Phase 2:** Decision documented in this file: DEFERRED REBASELINE.
- **Phase 3:** Skipped — no sync migration kept.
- **Phase 4:** Trial migration was measured and classified as suspiciously large; decision to stop and not keep it.
- **Phase 5:** Other docs updated to reference the audit and this decision (see below).
- **Phase 6:** Validation and final report (see final report section in operations docs).

---

## 3. Trial migration removal

A trial migration **20260310015501_SyncModelSnapshot_ReconciliationCheck** was generated and **removed** in this pass with `dotnet ef migrations remove`. The snapshot was reverted to its pre-audit state. No sync migration was committed. Migration counts and snapshot are back to the pre-audit state.

---

## 4. References

| Document | Purpose |
|----------|---------|
| `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_AUDIT.md` | Full audit: snapshot status, missing/extra entities, schema diff, estimated size, recommendation. |
| `docs/operations/EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md` | Earlier reconciliation report (pre-pass context). |
| `docs/operations/EF_REBASELINE_PLAN.md` | Re-baseline strategy for new environments. |
| `docs/operations/EF_SAFE_MIGRATION_WORKFLOW.md` | Safe migration workflow; snapshot drift early-warning. |
