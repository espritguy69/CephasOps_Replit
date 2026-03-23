# EF Migration Final Cleanup — Audit

**Date:** Finalization and historical cleanup pass.  
**Purpose:** Classify the entire migration landscape, document status and recommended actions, and leave one authoritative decision set. **No migrations were deleted or archived in this pass.**

---

## 1. Inventory summary

| Category | Count | Description |
|---------|--------|-------------|
| **A. Active EF-discoverable** | 95 | Have `.Designer.cs`; appear in `dotnet ef migrations list`; are the operational EF path. |
| **B. Script-only / no-Designer** | 47 | No `.Designer.cs`; not applied by `dotnet ef database update`; apply via scripts when needed. |
| **C. Legacy / superseded / historical-only** | 0 | None classified as such; all 142 are either active chain or script history. |
| **D. Ambiguous (preserve)** | 18 | Subset of the 45: duplicate-timestamp or ordering-sensitive; do not touch. |
| **E. Duplicate timestamp sets** | 10 pairs | See §4. |
| **F. Repair/apply scripts** | See §5 | Scripts in `backend/scripts/` tied to migrations. |

**Total migration main .cs files:** 142 (excluding `ApplicationDbContextModelSnapshot.cs`).  
**Source of truth for lists:** `backend/scripts/audit-migration-designers.ps1` and `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`.

---

## 2. Classification of “old” or “completed” migrations

- **1. ACTIVE HISTORY (95 migrations)**
  Still part of the EF-discoverable chain. **Must remain** in `Migrations/`. Do not delete, rename, or archive. These are the only migrations applied by `dotnet ef database update` and the migration bundle.

- **2. SCRIPT HISTORY (47 migrations)**
  Not discoverable; schema may exist in real databases via SQL scripts or manual application. **Must remain** in `Migrations/` and be documented. Apply via idempotent/repair scripts per runbook and manifest. Do not delete; they explain schema drift and are referenced by runbooks/scripts.

- **3. LEGACY REFERENCE ONLY**  
  **None.** No migration was classified as “no longer part of the operational path.” All 142 are either active or script history.

- **4. SAFE ARCHIVE CANDIDATE**  
  **None.** Every migration is either in the active chain (94) or referenced by docs/scripts or required for existing DB reconciliation (47). Archiving any of them would be unsafe. **No archival was performed.**

- **5. DO NOT TOUCH**  
  All 47 no-Designer migrations, especially the 18 with duplicate timestamps or ordering dependencies. Do not rename timestamps, regenerate Designers, or move files. See `docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`.

---

## 3. Master table: status and recommended action

### 3.1 Active EF-discoverable migrations (95)

| Discoverable | Has Designer | Likely role | Status | Recommended action |
|--------------|---------------|-------------|--------|---------------------|
| Yes | Yes | EF chain | **active** | **keep** |

**Full list:** See `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.1 (95 migrations from 20251123142132_InitialCreate through 20260310031127_AddExternalIntegrationBus).

### 3.2 Script-only migrations (45)

| Discoverable | Has Designer | Likely role | Status | Recommended action |
|--------------|---------------|-------------|--------|---------------------|
| No | No | Script / repair | **script-only** | **preserve** (do-not-touch if duplicate timestamp) |

**Full list and script references:** See `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2. Duplicate-timestamp pairs: **do not touch** (ordering and lineage must be preserved).

---

## 4. Duplicate timestamp sets (E)

| Timestamp | Migration A | Migration B | Order (lexicographic) | Action |
|-----------|-------------|-------------|----------------------|--------|
| 20260308140000 | AddRateGroupAndOrderTypeSubtypeRateGroup | AddUniqueActiveScopeIndexWorkflowDefinitions | A then B | preserve both |
| 20260308150000 | AddBaseWorkRates | DedupeOrderTypeParentsAndAddUniqueIndexes | A then B | preserve both |
| 20260308180000 | AddLastLoginAndMustChangePasswordToUser | AddPayoutAnomalyAlerts | A then B | preserve both |
| 20260309120000 | AddJobDefinitionsTable | AddOrderPayoutSnapshot | (AddJobRunEventId is discoverable) | preserve all |
| 20260309150000 | AddOrderCategoryIdToParsedOrderDraft | ExtendReplayOperationsAndEvents | A then B | preserve both |
| 20260309180000 | AddEventStorePhase4NextRetryAndVersion | ReplayOperationCancelRequested | A then B | preserve both |
| 20260309190000 | AddEventStoreProcessingStartedAtUtc | ReplayOperationSafetyWindow | A then B | preserve both |
| 20260309200000 | AddEventStoreCausationId | ReplayOperationsStateIndex | A then B | preserve both |
| 20260310180000 | AddPayoutAnomalyAlertRuns | AddUnmatchedMaterialAuditToParsedOrderDraft | A then B | preserve both |
| 20260311120000 | AddParsedMaterialAlias | AddPayoutAnomalyReview | A then B | preserve both |

**Do not rename or reorder.** Ordering is by full migration ID string.

---

## 5. Repair / apply SQL scripts tied to migrations (F)

| Script | Related migration(s) | Use |
|--------|------------------------|-----|
| repair-password-reset-tokens-schema.sql | 20260308190000_AddPasswordResetTokens | When PasswordResetTokens table missing |
| apply-AddBaseWorkRates.sql | 20260308150000_AddBaseWorkRates | Apply base work rates schema |
| apply-AddRateModifiers.sql | 20260308160000_AddRateModifiers | Apply rate modifiers |
| apply-lockout-fields.sql | 20260308200000_AddLockoutFieldsToUser | Lockout fields on Users |
| add-partners-code-column.sql | 20260209120000_AddCodeToPartners | Partners.Code |
| add-order-type-parent-column.sql | 20260308100000_AddOrderTypeParentOrderTypeId | OrderTypes.ParentOrderTypeId |
| fix-order-types-hierarchy-data.sql | 20260308110000_FixOrderTypesHierarchyData | Data fix |
| apply-jobruns-migrations.sql | 20260309100000_AddJobRunsTable (and related) | JobRuns schema |
| add-JobDefinitions-table.sql | 20260309120000_AddJobDefinitionsTable | JobDefinitions table |
| add-payout-anomaly-alerts-table.sql | 20260308180000_AddPayoutAnomalyAlerts | Payout anomaly alerts |
| add-payout-anomaly-alert-runs-table.sql | 20260310180000_AddPayoutAnomalyAlertRuns | Payout anomaly alert runs |
| apply-payout-anomaly-review-migration.sql | 20260311120000_AddPayoutAnomalyReview | PayoutAnomalyReview; inserts into history |
| apply-EventStore-CausationId.sql | 20260309200000_AddEventStoreCausationId | EventStore CausationId |
| apply-EventStorePhase7LeaseAndAttemptHistory.sql | 20260312100000_AddEventStorePhase7LeaseAndAttemptHistory | EventStore Phase 7 + AttemptHistory |
| apply-AddServiceProfiles.sql | (ServiceProfiles discovered migration) | ServiceProfiles when needed |
| apply-background-job-worker-ownership.sql | 20260309053019_AddBackgroundJobWorkerOwnership | Idempotent apply of worker ownership (discovered) |

**Do not remove or archive** any migration that has a script above; they are part of the documented execution path.

---

## 6. Summary of recommended actions

| Action | Applies to |
|--------|------------|
| **keep** | All 95 active EF-discoverable migrations. Leave in place; they are the active path. |
| **preserve** | All 45 script-only migrations. Do not delete, rename, or archive. Document and apply via scripts. |
| **do-not-touch** | Duplicate-timestamp pairs; do not rename or reorder. |
| **archive** | None. No migration was moved or archived. Preservation is safer than false cleanup. |
