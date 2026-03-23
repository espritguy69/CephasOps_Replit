# EF Migration Full Audit — Recovery Pass

**Date:** Migration recovery pass (full audit).  
**Scope:** All migration artifacts in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`.  
**DbContext:** `ApplicationDbContext` (Infrastructure/Persistence). **Design-time factory:** `ApplicationDbContextFactory`. **Startup project:** `backend/src/CephasOps.Api`.

---

## 1. Inventory summary

| Metric | Value |
|--------|--------|
| **Total migration main .cs files** | 142 (excluding ApplicationDbContextModelSnapshot.cs) |
| **With .Designer.cs** | 95 |
| **Missing .Designer.cs** | 47 |
| **EF-discovered (via migrations list)** | 95 (same as With Designer) |
| **Last discoverable migration** | 20260310031127_AddExternalIntegrationBus |
| **Pending (not yet applied in typical DB)** | 20260309065620_PendingModelCheck, 20260309065950_VerifyNoPending, 20260309070232_SyncSnapshotCheck, 20260309120000_AddJobRunEventId |

**Source:** `backend/scripts/audit-migration-designers.ps1` (run from `backend/` or repo root).

---

## 2. Full migration inventory (discoverable vs non-discoverable)

### 2.1 Discoverable migrations (95)

These have a `.Designer.cs` with `[Migration("...")]` and appear in `dotnet ef migrations list`. Order below is the EF discovery order (by migration ID).

- 20251123142132_InitialCreate  
- 20251123185309_AddCompanyDocuments  
- 20251123193706_AddDepartmentIdToPartnersBuildingsSplitters  
- 20251124035749_AddOrderTypeInstallationTypeBuildingTypeSplitterType  
- 20251124051255_AddVerticalsTable  
- 20251125070624_AddDepartmentIdToServiceInstaller  
- 20251125100326_AddDepartmentIdToMaterial_Update  
- 20251125101723_AddMaterialCategory  
- 20251125110553_AddPartnerForeignKeyToMaterial_Update  
- 20251126150839_SyncModelChanges  
- 20251126174910_AddPnlTypesAssetsAndAccounting  
- 20251127033039_SyncModelSnapshot  
- 20251129161619_AddParsedMaterialsJson  
- 20251129163409_AddDepartmentMemberships  
- 20251129164118_AddOrderDepartment  
- 20251129191720_AddTimeSlots  
- 20251129193519_UpdateTimeModificationTemplates  
- 20251201043956_AddDefaultParserTemplateToEmailAccounts  
- 20251202155247_AddRateEngineAndInstallationMethod  
- 20251202155910_AddPartnerGroupIdToBillingRatecard  
- 20251202160130_AddRateAuditFieldsToJobEarningRecord  
- 20251202162647_EnhancePnlDetailPerOrder  
- 20251202164938_AddOrderRelocationFields  
- 20251202165427_AddSplitterPortStandbyApprovalFields  
- 20251202173338_AddSameDayRescheduleEvidence  
- 20251202173502_AddBlockerEvidenceFields  
- 20251202173612_AddDocketNumberToOrder  
- 20251202174507_AddAuditOverrideEntity  
- 20251202174653_AddSoftDeleteToCompanyScopedEntities  
- 20251202174940_AddRowVersionConcurrencyTokens  
- 20251203001308_AddProcurementSalesProjectEntities  
- 20251203004645_AddTemplateFileIdToDocumentTemplate  
- 20251203082425_AddBuildingNameAndStatusToOrderDraft  
- 20251204111940_AddSmsAndWhatsAppTemplates  
- 20251204122813_AddAllSettingsTables  
- 20251204124148_FixBuildingRelationshipsQueryFilter  
- 20251205020231_IncreaseParsedOrderDraftFieldLengths  
- 20251205023922_AddCompanyLocaleSettings  
- 20251206140750_AddNetworkInfoAndVoipFields  
- 20251206141109_AddServiceIdTypeAndNetworkInfoFields  
- 20251207095918_AddMaterialPartnersTable  
- 20251207114421_AddGuardConditionAndSideEffectDefinitions  
- 20251207154424_AddOrderStatusChecklist  
- 20251207221618_AddMaterialUsageAndRMAFeatures  
- 20251208072755_AddScheduledSlotWorkflowFields  
- 20251208075939_AddBarcodeToMaterial  
- 20251208083105_AddMovementTypeAndLocationType  
- 20251208084246_AddMaterialTaggingSystem  
- 20251209071111_AddCustomerPreferenceTable  
- 20251209080853_AddOneDriveFieldsToFile  
- 20251209090436_CheckPendingChanges  
- 20251210234157_FixModelSnapshot  
- 20251212031140_FixBuildErrors  
- 20251212033704_AddUpdatedAtToCostCentres  
- 20251217101555_AddDeletedByUserIdToEmailMessages  
- 20251218055129_AddAwoNumberToParsedOrderDraft  
- 20251218074652_AddNetworkFieldsToParsedOrderDraft  
- 20251219020647_RenameInstallationTypeToOrderCategory  
- 20260105101702_AddBuildingTypeIdToBuildings  
- 20260105104100_RemoveServiceInstallerData  
- 20260105111631_AddAdditionalFieldsToServiceInstallers  
- 20260105122021_AddInstallerTypeToServiceInstallers  
- 20260106014539_CapturePendingModelChanges  
- 20260106014834_SeedAllReferenceData  
- 20260106072239_AddDepartmentIdToSkills  
- 20260127073842_CapturePendingModelChanges_20260127  
- 20260127143150_AddDocumentTemplateTagsDescription  
- 20260203043142_AddAuditLogTable  
- 20260203063309_AddStockLedgerAndAllocation  
- 20260203082321_AddLedgerReadPerformanceIndexes  
- 20260203082947_AddLedgerBalanceCache  
- 20260203093328_AddStockByLocationSnapshots  
- 20260308102000_AddOrderFinancialAlerts  
- 20260308133701_AddServiceProfiles  
- 20260308134742_AddServiceProfileIdToBaseWorkRate  
- 20260308163857_AddUserAgentToRefreshToken  
- 20260308193638_AddEventBusCorrelationAndEventStore  
- 20260308194528_ExtendEventStorePhase2  
- 20260308204047_AddReplayOperations  
- 20260308204750_AddSlaRulesTable  
- 20260308204838_AddSlaBreachesTable  
- 20260308214936_AddWorkflowTransitionHistory  
- 20260309013846_AddEventLedgerEntries  
- 20260309025207_AddEventProcessingLog  
- 20260309030727_AddReplayExecutionLock  
- 20260309044417_AddRebuildOperations  
- 20260309050021_RebuildPhase2CheckpointAndLock  
- 20260309051147_AddWorkerInstancesAndJobOwnership  
- 20260309053019_AddBackgroundJobWorkerOwnership  
- 20260309060310_AddOrderIdToTaskItem  
- 20260309065620_PendingModelCheck  
- 20260309065950_VerifyNoPending  
- 20260309070232_SyncSnapshotCheck  
- 20260309120000_AddJobRunEventId  
- 20260310031127_AddExternalIntegrationBus  

### 2.2 Non-discoverable migrations (47 — missing .Designer.cs)

| # | Migration ID | Duplicate timestamp? | Repo apply/repair script |
|---|--------------|----------------------|---------------------------|
| 1 | 20251125175719_AddDepartmentIdToMaterial | No | — |
| 2 | 20251202120000_RemoveDuplicateServiceInstallers | No | — |
| 3 | 20251210000000_AddOrderTypeToJobEarningRecord | No | — |
| 4 | 20251216120000_AddFullEmailBodyToEmailMessages | No | — |
| 5 | 20251217100755_AddDeletedByUserIdToEmailMessages | No | (Designer exists for 20251217101555, different) |
| 6 | 20260203120000_AddTermsInDaysToInvoice | No | — |
| 7 | 20260209120000_AddCodeToPartners | No | add-partners-code-column.sql |
| 8 | 20260209140000_AddParserReplayRuns | No | — |
| 9 | 20260303120000_AddAdditionalInformationToParsedOrderDraft | No | — |
| 10 | 20260308100000_AddOrderTypeParentOrderTypeId | No | add-order-type-parent-column.sql |
| 11 | 20260308110000_FixOrderTypesHierarchyData | No | fix-order-types-hierarchy-data.sql |
| 12 | 20260308120000_FixOrderTypesHierarchyDataAlternateCodes | No | — |
| 13 | 20260308130000_AddOrderTypeCodeToWorkflowDefinition | No | — |
| 14 | 20260308140000_AddRateGroupAndOrderTypeSubtypeRateGroup | Yes (2) | — |
| 15 | 20260308140000_AddUniqueActiveScopeIndexWorkflowDefinitions | Yes (2) | — |
| 16 | 20260308150000_AddBaseWorkRates | Yes (2) | apply-AddBaseWorkRates.sql |
| 17 | 20260308150000_DedupeOrderTypeParentsAndAddUniqueIndexes | Yes (2) | — |
| 18 | 20260308160000_AddRateModifiers | No | apply-AddRateModifiers.sql |
| 19 | 20260308180000_AddLastLoginAndMustChangePasswordToUser | Yes (2) | — |
| 20 | 20260308180000_AddPayoutAnomalyAlerts | Yes (2) | add-payout-anomaly-alerts-table.sql |
| 21 | 20260308190000_AddPasswordResetTokens | No | repair-password-reset-tokens-schema.sql |
| 22 | 20260308200000_AddLockoutFieldsToUser | No | apply-lockout-fields.sql |
| 23 | 20260309100000_AddJobRunsTable | No | apply-jobruns-migrations.sql |
| 24 | 20260309110000_AddRetriedFromJobRunIdToBackgroundJob | No | — |
| 25 | 20260309110100_AddJobRunsParentJobRunIdIndex | No | — |
| 26 | 20260309120000_AddJobDefinitionsTable | Yes (2) | add-JobDefinitions-table.sql |
| 27 | 20260309120000_AddOrderPayoutSnapshot | Yes (2) | — |
| 28 | 20260309150000_AddOrderCategoryIdToParsedOrderDraft | Yes (2) | — |
| 29 | 20260309150000_ExtendReplayOperationsAndEvents | Yes (2) | — |
| 30 | 20260309160000_ReplayOperationPhase2CheckpointResumeRerun | No | — |
| 31 | 20260309180000_AddEventStorePhase4NextRetryAndVersion | Yes (2) | — |
| 32 | 20260309180000_ReplayOperationCancelRequested | Yes (2) | — |
| 33 | 20260309190000_AddEventStoreProcessingStartedAtUtc | Yes (2) | — |
| 34 | 20260309190000_ReplayOperationSafetyWindow | Yes (2) | — |
| 35 | 20260309200000_AddEventStoreCausationId | Yes (2) | apply-EventStore-CausationId.sql |
| 36 | 20260309200000_ReplayOperationsStateIndex | Yes (2) | — |
| 37 | 20260309210000_AddEventStorePhase8PlatformEnvelope | No | — |
| 38 | 20260309220000_AddNotificationDispatches | No | — |
| 39 | 20260309230000_AddJobExecutions | No | — (added in baseline mismatch resolution) |
| 40 | 20260310100000_AddCommandProcessingLogs | No | — (added in post-baseline drift) |
| 41 | 20260310110000_AddWorkflowInstancesAndSteps | No | — (added in post-baseline drift) |
| 42 | 20260310120000_AddSnapshotProvenanceAndRepairRunHistory | No | — |
| 43 | 20260310180000_AddPayoutAnomalyAlertRuns | Yes (2) | add-payout-anomaly-alert-runs-table.sql |
| 44 | 20260310180000_AddUnmatchedMaterialAuditToParsedOrderDraft | Yes (2) | — |
| 45 | 20260311120000_AddParsedMaterialAlias | Yes (2) | — |
| 46 | 20260311120000_AddPayoutAnomalyReview | Yes (2) | apply-payout-anomaly-review-migration.sql |
| 47 | 20260312100000_AddEventStorePhase7LeaseAndAttemptHistory | No | apply-EventStorePhase7LeaseAndAttemptHistory.sql |

---

## 3. Duplicate timestamp analysis

EF orders migrations by full migration ID string. The following timestamps have **two** migrations each; ordering is lexicographic by full name:

| Timestamp | Migration A | Migration B | Order (A then B) |
|-----------|-------------|-------------|-------------------|
| 20260308140000 | AddRateGroupAndOrderTypeSubtypeRateGroup | AddUniqueActiveScopeIndexWorkflowDefinitions | A < B |
| 20260308150000 | AddBaseWorkRates | DedupeOrderTypeParentsAndAddUniqueIndexes | A < B |
| 20260308180000 | AddLastLoginAndMustChangePasswordToUser | AddPayoutAnomalyAlerts | A < B |
| 20260309120000 | AddJobDefinitionsTable | AddOrderPayoutSnapshot | A < B (AddJobRunEventId has Designer; different) |
| 20260309150000 | AddOrderCategoryIdToParsedOrderDraft | ExtendReplayOperationsAndEvents | A < B |
| 20260309180000 | AddEventStorePhase4NextRetryAndVersion | ReplayOperationCancelRequested | A < B |
| 20260309190000 | AddEventStoreProcessingStartedAtUtc | ReplayOperationSafetyWindow | A < B |
| 20260309200000 | AddEventStoreCausationId | ReplayOperationsStateIndex | A < B |
| 20260310180000 | AddPayoutAnomalyAlertRuns | AddUnmatchedMaterialAuditToParsedOrderDraft | A < B |
| 20260311120000 | AddParsedMaterialAlias | AddPayoutAnomalyReview | A < B |

**Note:** 20260309120000_AddJobRunEventId has a Designer and is discoverable; 20260309120000_AddJobDefinitionsTable and 20260309120000_AddOrderPayoutSnapshot do not. So the discovered chain has only one 20260309120000 (AddJobRunEventId). 20260309230000_AddJobExecutions is script-only (no Designer) and is the 45th in the no-Designer list (see baseline mismatch resolution).

---

## 4. Snapshot consistency notes

- **ApplicationDbContextModelSnapshot.cs** lives in the same Migrations folder. Its `BuildModel` reflects the model state after the **last** migration that was used to update it (typically the last migration in the chain when the snapshot was last regenerated).
- **Known drift:** The snapshot has been documented as "far behind" the current domain model (see `EF_PENDING_MODEL_CHANGES_REPAIR.md`, `EF_SCHEMA_RECONCILIATION_PASS.md`). Many tables/columns added in no-Designer or later migrations are not in the snapshot. PendingModelChangesWarning is suppressed at design-time and runtime.
- **Snapshot lineage:** The snapshot is not automatically updated when migrations are hand-written; it is updated only when `dotnet ef migrations add` is run. So the snapshot's "effective" ancestor is an older migration in the discovered chain; exact point is not re-audited here (see Phase 4).

---

## 5. Suspected drift causes

1. **Hand-written migrations without `migrations add`** — Many migrations were added as .cs only (no Designer), so EF never updated the snapshot for them.
2. **Script-based application** — Some schema was applied via idempotent SQL scripts and manual `__EFMigrationsHistory` inserts, so DB state can be ahead of the discovered chain.
3. **Duplicate timestamps** — Multiple migrations share the same timestamp; ordering is by full ID. Regenerating Designers for these would require a stable ordering convention and no renames.
4. **Repaired migrations** — 20260309053019 (ParsedMaterialAliases conditional) and 20260309065620 (EventStore Phase 7 idempotent) were changed after generation; their Designers still match the intended post-migration model for the discovered chain.

---

## 6. Cross-check: filesystem vs EF vs docs

| Source | Count / note |
|--------|----------------|
| Filesystem (main .cs, excl. Snapshot) | 142 |
| With .Designer.cs | 95 |
| EF `migrations list` | 95 migrations (all discoverable) |
| docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md | Lists 47 no-Designer (see baseline mismatch + post-baseline drift). |
| backend/scripts apply/repair SQL | Multiple; see table in §2.2. |

---

## 7. Next steps (recovery pass)

- **Phase 2:** Classify each of the 45 missing-Designer migrations as SAFE TO REGENERATE / SAFE TO LEAVE SCRIPT-ONLY / NEEDS MANUAL INTERVENTION.
- **Phase 3:** Regenerate .Designer.cs only where SAFE TO REGENERATE (with correct historical model state).
- **Phase 4:** Snapshot consistency check and reconciliation report.
- **Phase 5:** Snapshot recovery path or re-baseline plan.
- **Phase 6–7:** Validation and final decision.

No recovery file changes have been made in this phase; audit only.
