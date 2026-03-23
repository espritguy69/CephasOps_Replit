# No-Designer Migrations — Recovery Classification

Each migration that is missing a `.Designer.cs` is classified for recovery feasibility. **No .Designer.cs files were regenerated in this pass** for the reasons below.

---

## Classification buckets

- **A. SAFE TO REGENERATE** — Can reconstruct Designer metadata with high confidence (correct historical model state); no timestamp/ordering ambiguity.
- **B. SAFE TO LEAVE SCRIPT-ONLY** — Leave non-discoverable; apply via idempotent scripts; track in manifest. Regenerating would be misleading or risky.
- **C. NEEDS MANUAL INTERVENTION** — Conflicting timestamps, unclear snapshot ancestry, or content inconsistent with chain; regeneration could corrupt lineage.

---

## Why no migrations are classified A (SAFE TO REGENERATE)

Reconstructing a `.Designer.cs` requires the **exact model state after that migration** (the `BuildTargetModel` content). We do not have that stored for any of the 44 migrations. To derive it we would need to:

1. Start from a known snapshot (the next *discoverable* migration’s Designer, or the current ModelSnapshot), and
2. Reverse all migrations between that and the target migration,

which implies reversing many operations (AddColumn, CreateTable, etc.) and is error-prone. For migrations with **duplicate timestamps**, ordering is only defined by full migration ID string; adding Designers would require both to have correct preceding state and a stable order. **EF does not expose tooling to “replay” or “reverse” migrations into a model builder for historical Designer generation.** Therefore we do not classify any migration as A in this pass.

---

## Classification table (all 44)

| # | Migration ID | Bucket | Reason |
|---|--------------|--------|--------|
| 1 | 20251125175719_AddDepartmentIdToMaterial | B | Old hand-written; already in chain before many Designers; script-only is safe. |
| 2 | 20251202120000_RemoveDuplicateServiceInstallers | B | Data cleanup + schema; script-only. |
| 3 | 20251210000000_AddOrderTypeToJobEarningRecord | B | Add column(s); script-only. |
| 4 | 20251216120000_AddFullEmailBodyToEmailMessages | B | Add column; script-only. |
| 5 | 20251217100755_AddDeletedByUserIdToEmailMessages | B | Designer exists for 20251217101555 (different name); leave as script-only to avoid confusion. |
| 6 | 20260203120000_AddTermsInDaysToInvoice | B | Add column; script-only. |
| 7 | 20260209120000_AddCodeToPartners | B | Add column; script available; script-only. |
| 8 | 20260209140000_AddParserReplayRuns | B | New table(s); script-only. |
| 9 | 20260303120000_AddAdditionalInformationToParsedOrderDraft | B | ParsedOrderDrafts columns; script-only. |
| 10 | 20260308100000_AddOrderTypeParentOrderTypeId | B | OrderTypes column; script available; script-only. |
| 11 | 20260308110000_FixOrderTypesHierarchyData | B | Data fix; order-dependent; script-only. |
| 12 | 20260308120000_FixOrderTypesHierarchyDataAlternateCodes | B | Data fix; script-only. |
| 13 | 20260308130000_AddOrderTypeCodeToWorkflowDefinition | B | WorkflowDefinition column; script-only. |
| 14 | 20260308140000_AddRateGroupAndOrderTypeSubtypeRateGroup | C | Duplicate timestamp; ordering with 08140000_AddUniqueActiveScopeIndex; manual if ever regenerated. |
| 15 | 20260308140000_AddUniqueActiveScopeIndexWorkflowDefinitions | C | Duplicate timestamp; see above. |
| 16 | 20260308150000_AddBaseWorkRates | C | Duplicate timestamp; script exists; leave script-only. |
| 17 | 20260308150000_DedupeOrderTypeParentsAndAddUniqueIndexes | C | Duplicate timestamp; data + indexes. |
| 18 | 20260308160000_AddRateModifiers | B | Script exists; script-only. |
| 19 | 20260308180000_AddLastLoginAndMustChangePasswordToUser | C | Duplicate timestamp with AddPayoutAnomalyAlerts. |
| 20 | 20260308180000_AddPayoutAnomalyAlerts | C | Duplicate timestamp; script exists. |
| 21 | 20260308190000_AddPasswordResetTokens | B | Repair script exists; script-only. |
| 22 | 20260308200000_AddLockoutFieldsToUser | B | Script exists; script-only. |
| 23 | 20260309100000_AddJobRunsTable | B | Table creation; script exists; script-only. |
| 24 | 20260309110000_AddRetriedFromJobRunIdToBackgroundJob | B | Add column; script-only. |
| 25 | 20260309110100_AddJobRunsParentJobRunIdIndex | B | Index; script-only. |
| 26 | 20260309120000_AddJobDefinitionsTable | C | Duplicate timestamp; 20260309120000_AddJobRunEventId has Designer; ordering ambiguity. |
| 27 | 20260309120000_AddOrderPayoutSnapshot | C | Duplicate timestamp; see above. |
| 28 | 20260309150000_AddOrderCategoryIdToParsedOrderDraft | C | Duplicate timestamp. |
| 29 | 20260309150000_ExtendReplayOperationsAndEvents | C | Duplicate timestamp. |
| 30 | 20260309160000_ReplayOperationPhase2CheckpointResumeRerun | B | ReplayOperations changes; script-only. |
| 31 | 20260309180000_AddEventStorePhase4NextRetryAndVersion | C | Duplicate timestamp. |
| 32 | 20260309180000_ReplayOperationCancelRequested | C | Duplicate timestamp. |
| 33 | 20260309190000_AddEventStoreProcessingStartedAtUtc | C | Duplicate timestamp. |
| 34 | 20260309190000_ReplayOperationSafetyWindow | C | Duplicate timestamp. |
| 35 | 20260309200000_AddEventStoreCausationId | C | Duplicate timestamp; script exists. |
| 36 | 20260309200000_ReplayOperationsStateIndex | C | Duplicate timestamp. |
| 37 | 20260309210000_AddEventStorePhase8PlatformEnvelope | B | EventStore columns; script-only. |
| 38 | 20260309220000_AddNotificationDispatches | B | New table(s); script-only. |
| 39 | 20260310120000_AddSnapshotProvenanceAndRepairRunHistory | B | Tables; script-only. |
| 40 | 20260310180000_AddPayoutAnomalyAlertRuns | C | Duplicate timestamp; script exists. |
| 41 | 20260310180000_AddUnmatchedMaterialAuditToParsedOrderDraft | C | Duplicate timestamp. |
| 42 | 20260311120000_AddParsedMaterialAlias | C | Duplicate timestamp. |
| 43 | 20260311120000_AddPayoutAnomalyReview | C | Duplicate timestamp; script exists. |
| 44 | 20260312100000_AddEventStorePhase7LeaseAndAttemptHistory | B | Script exists; also covered by discovered 20260309065620 (idempotent). |

---

## Summary

| Bucket | Count | Action |
|--------|-------|--------|
| **A. SAFE TO REGENERATE** | 0 | None. |
| **B. SAFE TO LEAVE SCRIPT-ONLY** | 26 | Leave as-is; apply via scripts; manifest is authoritative. |
| **C. NEEDS MANUAL INTERVENTION** | 18 | Do not regenerate; leave script-only. If Designer is ever required, manual reconstruction and ordering decision needed. |

**Operational recommendation:** Treat all 44 as **script-only**. Use `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` (updated to reflect 44 and repo scripts) and `backend/scripts/MIGRATION_RUNBOOK.md` for when and how to apply them. No Designer files are generated in Phase 3 for this pass.
