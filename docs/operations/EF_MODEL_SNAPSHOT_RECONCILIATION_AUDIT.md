# EF Model Snapshot — Reconciliation Audit

**Date:** Snapshot reconciliation pass (analysis + trial migration).  
**Purpose:** Compare the EF model snapshot with the current model, estimate drift severity, and recommend safe path (sync migration vs deferred re-baseline).

---

## 1. Snapshot status

| Item | Value |
|------|--------|
| **File** | `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` |
| **ProductVersion** | 10.0.3 (EF Core 10) |
| **Line count** | ~14,733 |
| **Entity count (ToTable)** | 100 tables in snapshot |
| **Verdict** | **Outdated.** The snapshot compiles and is structurally valid but is behind the current domain model and behind schema introduced by the 44 script-only migrations and later discoverable migrations. |

The snapshot reflects an older model state (roughly post–EventStore Phase 2, pre–Phase 7 and pre–many no-Designer migrations). It was not updated when many migrations were added (especially those without Designers).

---

## 2. Entities missing from snapshot (current model has them)

The current `ApplicationDbContext` and configurations define entities/tables that were missing or incomplete in the snapshot. A trial `dotnet ef migrations add SyncModelSnapshot_ReconciliationCheck` was run to capture the exact diff. The generated migration included:

**New tables (CreateTable in generated migration):**

- EventProcessingLog  
- EventStoreAttemptHistory  
- JobExecutions  
- JobDefinitions  
- JobRuns  
- LedgerEntries  
- NotificationDispatches  
- ParsedMaterialAliases  
- PayoutAnomalyAlerts  
- PayoutAnomalyAlertRuns  
- PayoutAnomalyReviews  
- RebuildExecutionLocks  
- RebuildOperations  
- ReplayExecutionLock  
- ReplayOperationEvents  
- ReplayOperations  
- (and related indexes/FKs)

**Columns added to existing tables (AddColumn in generated migration):**

- **TaskItems:** OrderId  
- **ParsedOrderDrafts:** OrderCategoryId, UnmatchedMaterialCount, UnmatchedMaterialNamesJson  
- **EventStore:** CapturedAtUtc, CausationId, IdempotencyKey, LastClaimedAtUtc, LastClaimedBy, LastErrorType, NextRetryAtUtc, PartitionKey, PayloadVersion, Priority, ProcessingLeaseExpiresAtUtc, ProcessingNodeId, ProcessingStartedAtUtc, ReplayId, RootEventId, SourceModule, SourceService, SpanId, TraceId  
- **BackgroundJobs:** ClaimedAtUtc, WorkerId  

Several of the “new” tables may already exist in real databases (created by script-only migrations); the generated migration does not use `IF NOT EXISTS` and would fail on those DBs if applied as-is.

---

## 3. Entities extra in snapshot

None identified. The snapshot does not contain tables that are no longer in the current model; the drift is primarily **missing** entities and columns in the snapshot.

---

## 4. Schema differences (summary)

- **Snapshot behind model:** Many EventStore columns (Phase 4–8), EventStoreAttemptHistory table, JobExecutions/JobDefinitions/JobRuns, LedgerEntries, EventProcessingLog, ReplayOperations and related tables, NotificationDispatches, ParsedMaterialAliases, PayoutAnomaly* tables, BackgroundJobs columns, TaskItems.OrderId, ParsedOrderDrafts columns.
- **Indexes / keys:** The generated migration added indexes and foreign keys for the new tables and relationships; no systematic comparison of index names or definitions was done beyond the generated migration.
- **Cause:** 44 script-only migrations were never reflected in the snapshot; later discoverable migrations may have updated the snapshot only partially.

---

## 5. Estimated migration size (from trial run)

A one-time sync migration was generated to measure drift:

| Metric | Value |
|--------|--------|
| **Migration name** | SyncModelSnapshot_ReconciliationCheck (trial; not kept) |
| **Main .cs line count** | **909** |
| **Approx. operations** | ~146 (AddColumn, CreateTable, CreateIndex, AddForeignKey) |
| **CreateTable count** | **15** new tables |
| **DropTable (in Down)** | 14 (mirrors new tables) |
| **AddColumn** | Many (EventStore, BackgroundJobs, TaskItems, ParsedOrderDrafts) |

The migration is **additive in Up()** (no DropTable in Up), but it is **suspiciously large** by the project’s hygiene threshold (e.g. > 500 lines). Several CreateTable operations would fail on databases that already have those tables (from script-only migrations) unless the migration were rewritten to use idempotent SQL (e.g. CREATE TABLE IF NOT EXISTS).

---

## 6. Recommendation: sync migration vs re-baseline later

| Option | Assessment |
|--------|------------|
| **Safe sync migration (keep as-is)** | **Not recommended.** The trial migration is 909 lines and includes 15 CreateTable calls. It exceeds the “suspiciously large” threshold and would not be idempotent for DBs that already have some of these tables. Keeping it would risk apply failures and difficult rollbacks. |
| **Idempotent sync migration (rewrite)** | Theoretically possible: rewrite Up() to use IF NOT EXISTS / ADD COLUMN IF NOT EXISTS and avoid duplicate index creation. Would require a dedicated, carefully tested pass and is **not** done in this analysis pass. |
| **Deferred re-baseline** | **Recommended.** Treat snapshot reconciliation as a separate, planned effort: either (a) a future re-baseline for new environments (see `EF_REBASELINE_PLAN.md`) or (b) a one-off, idempotent sync migration designed and reviewed in a dedicated sprint. |
| **Leave snapshot as-is** | Continue with PendingModelChangesWarning suppression; apply schema via discovered migrations and script-only migrations per manifest. **Current operational approach** until a dedicated reconciliation or re-baseline is done. |

**Conclusion:** Drift is **moderate to large** (909-line diff, 15 new tables, many new columns). A **sync migration is not** generated or kept in this pass. Snapshot reconciliation is **deferred** to a re-baseline or a dedicated idempotent sync migration pass.

---

## 7. Trial migration removal

A trial migration **SyncModelSnapshot_ReconciliationCheck** was generated for this audit. It was **removed** with `dotnet ef migrations remove`; the snapshot was reverted to its pre-audit state. No sync migration remains in the repository. If you ever regenerate a trial sync migration and need to remove it, run:

```bash
cd backend/src/CephasOps.Api
dotnet ef migrations remove --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --context ApplicationDbContext --force
```
