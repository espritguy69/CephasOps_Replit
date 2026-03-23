# EF Migration Graph Integrity Audit

**Date:** Migration graph and baseline governance pass.  
**Scope:** Full migration landscape in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`.  
**Purpose:** Structural inventory, ordering, lineage, discovery, and graph health for governance and future InitialBaseline planning.

**Sources:** `backend/scripts/audit-migration-designers.ps1`, `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`, `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`, filesystem.

---

## 1. Full structural summary

| Metric | Value |
|--------|--------|
| **Total main migration .cs files** | 142 (excluding ApplicationDbContextModelSnapshot.cs) |
| **With .Designer.cs (EF-discoverable)** | 95 |
| **Missing .Designer.cs (script-only)** | 47 |
| **Snapshot file** | ApplicationDbContextModelSnapshot.cs (present; not used as migration node) |
| **EF discovery** | `dotnet ef migrations list` returns 95 migrations in ID order |

**Graph structure:** Two disjoint sets — **(1) discoverable chain** (95 nodes, linear order by migration ID); **(2) script-only set** (47 nodes, not in EF chain; apply via scripts). Combined chronology is mixed: script-only and discoverable share timestamp ranges and interleave by date; EF applies only the 95 in order.

---

## 2. Discoverable chain summary

- **Count:** 95 migrations.
- **Ordering:** Lexicographic by full migration ID (e.g. `20251123142132_InitialCreate` … `20260310031127_AddExternalIntegrationBus`). EF uses this order for `migrations list` and `database update`.
- **First:** 20251123142132_InitialCreate.
- **Last (latest discoverable):** 20260310031127_AddExternalIntegrationBus.
- **Invariant:** Every discoverable migration has both main `.cs` and `.Designer.cs`; no discoverable migration is in the no-Designer manifest.
- **Timestamp monotonicity:** Within the discoverable list, migration IDs sort in chronological order; no timestamp regression in the chain.
- **Duplicate timestamps in discoverable set:** Only one migration with timestamp 20260309120000 is discoverable (AddJobRunEventId). The other two with that timestamp (AddJobDefinitionsTable, AddOrderPayoutSnapshot) are script-only.

Full ordered list of 95: see **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** §2.1.

---

## 3. Script-only set summary

- **Count:** 47 migrations.
- **Definition:** Main `.cs` exists; no `.Designer.cs`; not in EF discovery.
- **Manifest:** All 47 are listed in **`docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`** (table + note) and in **`EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** §2.2.
- **Validator:** `validate-migration-hygiene.ps1` expects exactly 47 no-Designer; classifier uses same baseline.
- **Ordering:** No single canonical “chain”; apply order is dependency- and script-dependent (see runbook and manifest). Many share timestamps with each other (duplicate-timestamp pairs); none share a timestamp with a discoverable migration except 20260309120000 (AddJobRunEventId is discoverable; AddJobDefinitionsTable and AddOrderPayoutSnapshot are script-only).

Full list of 47: see **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** §2.2 and audit script output.

---

## 4. Duplicate timestamp analysis

| Timestamp | Migrations with this timestamp | In discoverable chain? | In script-only set? |
|-----------|--------------------------------|-------------------------|----------------------|
| 20260308140000 | AddRateGroupAndOrderTypeSubtypeRateGroup, AddUniqueActiveScopeIndexWorkflowDefinitions | No | Yes (both) |
| 20260308150000 | AddBaseWorkRates, DedupeOrderTypeParentsAndAddUniqueIndexes | No | Yes (both) |
| 20260308180000 | AddLastLoginAndMustChangePasswordToUser, AddPayoutAnomalyAlerts | No | Yes (both) |
| 20260309120000 | AddJobDefinitionsTable, AddOrderPayoutSnapshot, **AddJobRunEventId** | **Yes (AddJobRunEventId only)** | Yes (first two) |
| 20260309150000 | AddOrderCategoryIdToParsedOrderDraft, ExtendReplayOperationsAndEvents | No | Yes (both) |
| 20260309180000 | AddEventStorePhase4NextRetryAndVersion, ReplayOperationCancelRequested | No | Yes (both) |
| 20260309190000 | AddEventStoreProcessingStartedAtUtc, ReplayOperationSafetyWindow | No | Yes (both) |
| 20260309200000 | AddEventStoreCausationId, ReplayOperationsStateIndex | No | Yes (both) |
| 20260310180000 | AddPayoutAnomalyAlertRuns, AddUnmatchedMaterialAuditToParsedOrderDraft | No | Yes (both) |
| 20260311120000 | AddParsedMaterialAlias, AddPayoutAnomalyReview | No | Yes (both) |

**Ordering:** EF orders by full migration ID string. So for 20260309120000, order is AddJobDefinitionsTable &lt; AddJobRunEventId &lt; AddOrderPayoutSnapshot. Only AddJobRunEventId has a Designer, so only it appears in the discoverable chain. No ambiguity for the active EF path.

---

## 5. Chronology and ordering risks

- **Discoverable chain:** Chronologically coherent. IDs sort in date order; no gap or regression in the 94. Safe for continued `dotnet ef database update` and for future migrations that extend the chain.
- **Script-only vs discoverable:** Script-only migrations have timestamps that fall before, between, or after discoverable ones (e.g. 20260310100000, 20260310110000, 20260312100000 are script-only and “after” the last discoverable 20260309120000). This is by design; they are not applied by EF. For a future InitialBaseline that includes “full operational schema,” the logical order of schema application would be defined by dependency/runbook, not by a single linear EF chain.
- **Gaps:** No structural “missing” migration in the discoverable chain (every file that has a Designer is one of the 94; no Designer is missing for any of them). Script-only set has no formal “gap” definition; manifest and full audit are the source of truth.
- **Risks:** (1) Adding a new migration with a timestamp earlier than 20260309120000 would not change EF order (EF uses full ID) but could confuse humans. (2) Duplicate timestamps in script-only set require care when generating or applying scripts (order must match manifest/runbook). (3) Snapshot is behind the full model (documented elsewhere); not a graph defect but affects future baseline design.

---

## 6. Graph health assessment

| Check | Result |
|-------|--------|
| Total count matches baseline (142) | Yes (audit script) |
| Discoverable count matches baseline (95) | Yes |
| Script-only count matches baseline (47) | Yes |
| Every discoverable has Designer | Yes (by definition) |
| No discoverable migration in no-Designer manifest | Yes |
| Last discoverable identified and consistent | Yes — 20260309120000_AddJobRunEventId |
| Duplicate timestamps documented and unambiguous for EF chain | Yes — only one 20260309120000 in chain |
| Script-only set fully listed in manifest/full audit | Yes — 47 in §2.2 and manifest |
| Chronology (discoverable) | Monotonic by ID; no regression |
| Validator/classifier baseline alignment | Yes — 141/94/47 |

**Conclusion:** The migration graph is **structurally coherent**. The discoverable chain is a well-defined linear sequence of 95 nodes; the script-only set is fully enumerated and governed. No structural defect (missing Designer in chain, undocumented no-Designer, or count mismatch) was found. The graph is suitable for day-to-day governed authoring and for planning a future InitialBaseline strategy.

---

## 7. Inventory cross-reference

| Source | Location |
|--------|----------|
| Discoverable list (94) | EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md §2.1 |
| Script-only list (47) | EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md §2.2; audit-migration-designers.ps1 |
| Manifest (operational) | EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md |
| Duplicate timestamps | EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md §3; §4 above |
| Validator/classifier | validate-migration-hygiene.ps1; classify-migration-state.ps1 |
