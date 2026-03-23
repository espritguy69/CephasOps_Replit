# EF Post-Baseline Drift — Classification and Decision

**Date:** Post-baseline drift classification pass.  
**Input:** `docs/operations/EF_POST_BASELINE_DRIFT_AUDIT.md` (identification of the two extra no-Designer migrations).

---

## Classification key

- **A.** Accidental incomplete migration  
- **B.** Intentional new script-only migration  
- **C.** Local temporary artifact not meant for commit  
- **D.** Ambiguous; preserve and escalate  

---

## Migration 1: 20260310100000_AddCommandProcessingLogs

| Criterion | Finding |
|-----------|--------|
| Classification | **B. Intentional new script-only migration** |
| Evidence | Full Up/Down; creates `CommandProcessingLogs` table; Domain entity `CommandProcessingLog` and Infrastructure configuration and DbSet exist; schema matches model. No Designer by design. |
| Recommended action | **Keep.** Add to no-Designer manifest and full audit §2.2 as item 46. Update authoritative counts to include this migration. Do not delete; do not add a Designer to historical migration. |

---

## Migration 2: 20260310110000_AddWorkflowInstancesAndSteps

| Criterion | Finding |
|-----------|--------|
| Classification | **B. Intentional new script-only migration** |
| Evidence | Full Up/Down; creates `WorkflowInstances` and `WorkflowSteps`; Domain entities and Infrastructure configurations and DbSets exist; schema matches workflow engine model. No Designer by design. |
| Recommended action | **Keep.** Add to no-Designer manifest and full audit §2.2 as item 47. Update authoritative counts. Do not delete; do not add a Designer to historical migration. |

---

## Resolution summary

| Migration | Classification | Stay or go? | Action |
|-----------|----------------|-------------|--------|
| 20260310100000_AddCommandProcessingLogs | B | **Stay** | Add to manifest; update counts and validator. |
| 20260310110000_AddWorkflowInstancesAndSteps | B | **Stay** | Add to manifest; update counts and validator. |

**Resulting authoritative state:** 141 total main migrations, 94 with Designer, 47 without Designer. No deletions; no deferred items.
