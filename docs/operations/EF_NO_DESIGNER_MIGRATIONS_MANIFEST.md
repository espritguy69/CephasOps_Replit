# Migrations without Designer — Operational Manifest

Migrations that have a main `.cs` but **no** `.Designer.cs` are **not** discovered by `dotnet ef migrations list` or `dotnet ef database update`. They must be applied via idempotent SQL scripts or by adding a Designer (not recommended for already-applied/hand-written migrations). See `docs/MIGRATION_HYGIENE.md` and `backend/scripts/MIGRATION_RUNBOOK.md`.

**Source of list:** `backend/scripts/audit-migration-designers.ps1` (run from repo root or `backend/`). **Current count: 47.** Full inventory table (with script references) and duplicate-timestamp analysis: **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** §2.2. Recovery classification (script-only vs manual): **`docs/operations/EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`**. Baseline mismatch resolution: **`docs/operations/EF_MIGRATION_BASELINE_MISMATCH_DECISION.md`**. Post-baseline drift: **`docs/operations/EF_POST_BASELINE_DRIFT_DECISION.md`** (46th = AddCommandProcessingLogs, 47th = AddWorkflowInstancesAndSteps).

---

## Summary

| Classification | Count | How to apply |
|----------------|-------|----------------|
| **Apply via idempotent script when needed** | Most | Use repo script if exists (see table below); otherwise generate from migration .cs or hand-author idempotent SQL. Insert migration ID into `__EFMigrationsHistory` only if the script does not do it. |
| **Schema covered by discovered chain or repair** | Some | e.g. EventStore Phase 7 is in 20260309065620 (discovered) and idempotent; PasswordResetTokens has repair script. |
| **Order-dependent / risky** | Few | Apply in dependency order; backup first. |

---

## Manifest (no-Designer migrations; count from `audit-migration-designers.ps1`)

| Migration ID | Hand-written / no Designer | Discoverable by EF | Safe via `dotnet ef database update` | Recommended path | Notes / script |
|--------------|----------------------------|--------------------|---------------------------------------|------------------|----------------|
| 20251125175719_AddDepartmentIdToMaterial | Yes | No | N/A | Idempotent script or skip if already applied | Old; likely already in DB. |
| 20251202120000_RemoveDuplicateServiceInstallers | Yes | No | N/A | Idempotent script | Data cleanup + schema. |
| 20251210000000_AddOrderTypeToJobEarningRecord | Yes | No | N/A | Idempotent script | Add column(s) to JobEarningRecord. |
| 20251216120000_AddFullEmailBodyToEmailMessages | Yes | No | N/A | Idempotent script | Add column to EmailMessages. |
| 20251217100755_AddDeletedByUserIdToEmailMessages | Yes | No | N/A | Idempotent script | Add column; Designer exists for 20251217101555 (different name). |
| 20260203120000_AddTermsInDaysToInvoice | Yes | No | N/A | Idempotent script | Add column to Invoice. |
| 20260209120000_AddCodeToPartners | Yes | No | N/A | Idempotent script | Add column to Partners. |
| 20260209140000_AddParserReplayRuns | Yes | No | N/A | Idempotent script | New table(s). |
| 20260303120000_AddAdditionalInformationToParsedOrderDraft | Yes | No | N/A | Idempotent script | ParsedOrderDrafts columns. |
| 20260308100000_AddOrderTypeParentOrderTypeId | Yes | No | N/A | Idempotent script | OrderTypes column; order before 20260308110000. |
| 20260308110000_FixOrderTypesHierarchyData | Yes | No | N/A | Idempotent script | Data fix; run after 20260308100000. |
| 20260308120000_FixOrderTypesHierarchyDataAlternateCodes | Yes | No | N/A | Idempotent script | Data fix; order-dependent. |
| 20260308130000_AddOrderTypeCodeToWorkflowDefinition | Yes | No | N/A | Idempotent script | WorkflowDefinition column. |
| 20260308140000_AddRateGroupAndOrderTypeSubtypeRateGroup | Yes | No | N/A | Idempotent script | Tables/indexes; duplicate timestamp with next. |
| 20260308140000_AddUniqueActiveScopeIndexWorkflowDefinitions | Yes | No | N/A | Idempotent script | Index; duplicate timestamp. |
| 20260308150000_AddBaseWorkRates | Yes | No | N/A | Idempotent script | **Script:** `apply-AddBaseWorkRates.sql` |
| 20260308150000_DedupeOrderTypeParentsAndAddUniqueIndexes | Yes | No | N/A | Idempotent script | Data + indexes; duplicate timestamp. |
| 20260308160000_AddRateModifiers | Yes | No | N/A | Idempotent script | **Script:** `apply-AddRateModifiers.sql` |
| 20260308180000_AddLastLoginAndMustChangePasswordToUser | Yes | No | N/A | Idempotent script | Users columns. |
| 20260308190000_AddPasswordResetTokens | Yes | No | N/A | Idempotent script or **repair** | **Repair script:** `repair-password-reset-tokens-schema.sql` (if table missing). Do not insert into history from repair. |
| 20260308200000_AddLockoutFieldsToUser | Yes | No | N/A | Idempotent script | **Script:** `apply-lockout-fields.sql` |
| 20260309120000_AddOrderPayoutSnapshot | Yes | No | N/A | Idempotent script | OrderPayoutSnapshots table. |
| 20260309230000_AddJobExecutions | Yes | No | N/A | Idempotent script | JobExecutions table; apply when needed. (Added in baseline mismatch resolution.) |
| 20260310100000_AddCommandProcessingLogs | Yes | No | N/A | Idempotent script | CommandProcessingLogs table; workflow/command feature. (Added in post-baseline drift.) |
| 20260310110000_AddWorkflowInstancesAndSteps | Yes | No | N/A | Idempotent script | WorkflowInstances + WorkflowSteps tables; workflow engine. (Added in post-baseline drift.) |
| 20260310120000_AddSnapshotProvenanceAndRepairRunHistory | Yes | No | N/A | Idempotent script | Provenance/repair tables. |
| 20260311120000_AddPayoutAnomalyReview | Yes | No | N/A | Idempotent script | **Script:** `apply-payout-anomaly-review-migration.sql` (inserts into history). |
| 20260311120000_AddParsedMaterialAlias | Yes | No | N/A | Idempotent script | ParsedMaterialAliases table. No dedicated script in repo; use migration .cs or hand-author. |
| 20260312100000_AddEventStorePhase7LeaseAndAttemptHistory | Yes | No | N/A | Idempotent script | **Script:** `apply-EventStorePhase7LeaseAndAttemptHistory.sql`. Also covered by discovered migration 20260309065620_PendingModelCheck (idempotent). |

**Note:** 20260308163857_AddUserAgentToRefreshToken (discovered) runs after some of the above in calendar order; it was hardened to tolerate missing PasswordResetTokens index. 20260309053019 (discovered) was hardened to tolerate missing ParsedMaterialAliases table/indexes. The **authoritative full list of all 47** no-Designer migrations is in **`docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md`** §2.2 and **`docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`**. Do **not** delete or archive any of them; they are script history and may be needed for existing DB reconciliation.

---

## How to use this manifest

1. **Before applying discovered chain:** If the target DB was built from backup or scripts, check `__EFMigrationsHistory` and schema (e.g. `check-migration-state.sql`). For any no-Designer migration whose schema you need, apply the recommended script (or equivalent idempotent SQL) **before** or **after** the discovered chain as appropriate; document any manual insert into `__EFMigrationsHistory`.
2. **After bundle / `database update`:** If key tables (e.g. PasswordResetTokens, PayoutAnomalyReviews, ParsedMaterialAliases, EventStore Phase 7) are missing, use the repair or apply script from the table above; then verify with `check-migration-state.sql`.
3. **New migrations:** Always create with `dotnet ef migrations add` so a Designer is generated and the migration is discovered.
