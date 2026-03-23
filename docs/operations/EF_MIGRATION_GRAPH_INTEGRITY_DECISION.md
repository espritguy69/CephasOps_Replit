# EF Migration Graph Integrity — Decision

**Purpose:** Record the result of the migration graph integrity check (discoverable chain and script-only set) and whether the current graph is safe for continued authoring and for future InitialBaseline planning.

**Input:** `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_AUDIT.md`.

---

## 1. Discoverable chain integrity (95 migrations)

### 1.1 Checks performed

| Check | Result |
|-------|--------|
| Missing .Designer.cs where one should exist | **None.** All 94 discoverable migrations have both main .cs and .Designer.cs. |
| Broken ordering | **None.** Ordering is lexicographic by migration ID; EF list is consistent. |
| Timestamp regressions | **None.** Discoverable IDs sort in chronological order. |
| Duplicate timestamp ambiguity | **Resolved.** Only one migration with timestamp 20260309120000 is in the chain (AddJobRunEventId). The other two (AddJobDefinitionsTable, AddOrderPayoutSnapshot) are script-only. |
| Discoverability inconsistency | **None.** Count 95 matches validator and audit; every file with a Designer is discoverable. |
| Latest discoverable identity | **20260310031127_AddExternalIntegrationBus** — confirmed and consistent across audit, full recovery pass, and runbook. |

### 1.2 Assessment

The active EF chain is **structurally coherent** and **coherent but historically mixed** (in the sense that the repo also contains 47 script-only migrations that are not in the chain; the chain itself is clean).

**Classification:** **B. Coherent but historically mixed.**

- **Structurally:** The 95-migration discoverable chain is internally consistent: no missing Designers, no ordering or timestamp defects, no ambiguity for EF. Safe for `dotnet ef database update` and for adding new migrations via `create-migration.ps1`.
- **Historically mixed:** The overall repo has two tracks (discoverable vs script-only). New authors must follow governance (official script, validator, classifier) so new migrations get a Designer and stay in the chain. Current guardrails (validator, classifier, PR checklist, runbook) are required to keep it that way.

**Not C (fragile)** because the chain itself is not brittle — it is well-defined and ordered. **Not D (not safe for continued authoring)** because with current governance, continued authoring is safe. **Not A (structurally coherent only)** because we explicitly acknowledge the dual-track nature.

**Conclusion:** The discoverable chain is **safe for day-to-day authoring** provided current governance (create-migration.ps1, validate-migration-hygiene.ps1, classify-migration-state.ps1, docs, CI) remains in place. No further intervention is required for integrity of the chain itself.

---

## 2. Script-only set integrity (47 migrations)

### 2.1 Checks performed

| Check | Result |
|-------|--------|
| Every one in the manifest | **Yes.** All 47 are listed in EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md (table or authoritative note) and in EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md §2.2. |
| Count correct | **Yes.** Validator and classifier expect 47; audit script reports 47. |
| Undocumented no-Designer migrations | **None.** Every main .cs without a Designer is one of the 47 and is documented. |
| Duplicate timestamp interaction with discoverable | **Documented.** Only 20260309120000 has both discoverable (AddJobRunEventId) and script-only (AddJobDefinitionsTable, AddOrderPayoutSnapshot) peers; ordering and roles are clear. |
| Script-only that should be in discoverable chain | **None.** No evidence that any of the 47 were intended to be EF-discoverable and mistakenly left without a Designer; they are classified as intentional script-only or legacy script-only and are governed as such. |
| Long-term risk for future InitialBaseline | **Low.** For an InitialBaseline that represents “full operational schema,” the 47 will need to be reflected either (a) in the baseline schema itself, or (b) as a documented set of “already applied” before baseline. They do not block defining the strategy; they are part of the design. |

### 2.2 Assessment

**Script-only set is fully governed.** All 47 are documented, counted, and covered by the manifest and runbook. No migration needs immediate reconsideration; any future “reconsideration” (e.g. whether to reflect them in a baseline snapshot) belongs in the InitialBaseline strategy, not in graph integrity.

---

## 3. Summary

| Area | Status | Safe for continued use? |
|------|--------|-------------------------|
| Discoverable chain (94) | Coherent, ordered, no defects | Yes, with current governance |
| Script-only set (47) | Fully in manifest and docs | Yes |
| Graph overall | Structurally healthy | Yes |
| Future InitialBaseline | Not blocked by graph integrity | Strategy can be defined |

**Recommendation:** Continue with the current governed model. Use the graph integrity audit and this decision as the basis for InitialBaseline strategy (when to stop extending legacy chain, how to represent full schema, how to handle existing vs new environments) without changing the migration graph in this pass.
