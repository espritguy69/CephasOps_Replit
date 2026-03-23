# Remediation 1.1 — Verification Report

**Context:** Migration `20260310065356_AddOperationalInsightsAndFeatureFlags` was applied successfully (idempotent). This report verifies whether all schema objects required by the running code are present and what to do next.

---

## 1. Confirmed present / missing status

| Object | Status | Notes |
|--------|--------|------|
| **EventStore.RootEventId** | **MISSING** | Column not in `information_schema.columns`; index `IX_EventStore_RootEventId` also missing. |
| **OrderPayoutSnapshots** | **MISSING** | Table does not exist (`to_regclass` returns NULL). |
| **InboundWebhookReceipts** | **MISSING** | Table does not exist. |
| **JobExecutions** | **PRESENT** | Table exists. |
| **PayoutSnapshotRepairRuns** | **MISSING** | Table does not exist. |

**JobExecutions** is the only one of the four tables present. The rest must be restored before the API can run without schema-related failures.

---

## 2. Migration vs repair script responsibility

| Object | Created by migration(s) | In __EFMigrationsHistory? | Created by repair script? |
|--------|-------------------------|---------------------------|----------------------------|
| EventStore.RootEventId (+ Phase 8 cols/indexes) | `20260309210000_AddEventStorePhase8PlatformEnvelope`; also in `20260310031127_AddExternalIntegrationBus` | 20260309210000 **no**; 20260310031127 **yes** | Yes (idempotent ADD COLUMN / CREATE INDEX) |
| OrderPayoutSnapshots | `20260309120000_AddOrderPayoutSnapshot` | **No** | Yes (CREATE TABLE IF NOT EXISTS + Provenance) |
| InboundWebhookReceipts | `20260310031127_AddExternalIntegrationBus` | **Yes** | Yes (CREATE TABLE IF NOT EXISTS) |
| JobExecutions | `20260309230000_AddJobExecutions`; also in `20260310031127_AddExternalIntegrationBus` | 20260309230000 **no**; 20260310031127 **yes** | Yes (no-op when table exists) |
| PayoutSnapshotRepairRuns | `20260310120000_AddSnapshotProvenanceAndRepairRunHistory` | **Yes** | Yes (CREATE TABLE IF NOT EXISTS) |

**Gap summary:**  
- `20260309120000_AddOrderPayoutSnapshot` and `20260309210000_AddEventStorePhase8PlatformEnvelope` are **not** in `__EFMigrationsHistory`, so OrderPayoutSnapshots and EventStore Phase 8 columns were never applied via EF.  
- `20260310031127_AddExternalIntegrationBus` and `20260310120000_AddSnapshotProvenanceAndRepairRunHistory` **are** in history, but the database is missing InboundWebhookReceipts, EventStore.RootEventId, and PayoutSnapshotRepairRuns. So either those migrations failed partway (e.g. 20260310120000 fails when adding column to non-existent OrderPayoutSnapshots) or schema was applied from a different path.  
- **Repair script** `backend/scripts/apply-remediation-1.1-schema-repair.sql` creates or completes all of the above idempotently and is the single way to restore missing objects without re-running failed migrations.

---

## 3. Migration gap / schema drift cause

- **OrderPayoutSnapshots:** Never created because `20260309120000_AddOrderPayoutSnapshot` is not in migration history.  
- **EventStore.RootEventId (and Phase 8):** Either never applied (20260309210000 not in history) or dropped/failed during 20260310031127.  
- **InboundWebhookReceipts:** Part of 20260310031127; migration is in history but table is missing — migration likely failed after creating JobExecutions or was never fully run.  
- **PayoutSnapshotRepairRuns:** Created in 20260310120000, which first adds `Provenance` to `OrderPayoutSnapshots`. Because OrderPayoutSnapshots did not exist, that migration’s `Up()` would fail on the first step, so PayoutSnapshotRepairRuns was never created even though the migration is recorded.

**Root cause:** Missing `OrderPayoutSnapshots` (and possibly other objects) plus partial application of later migrations led to recorded history not matching actual schema. Applying the idempotent repair script aligns the database with the EF model and existing history.

---

## 4. Repair script validation

- **Safe:** Uses `ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`. No drops.  
- **Minimal:** Only adds missing columns and tables; does not change existing data.  
- **Aligned with EF model:**  
  - EventStore Phase 8 columns and indexes match `EventStoreEntry` / snapshot.  
  - OrderPayoutSnapshots columns and unique index match `OrderPayoutSnapshot` and `OrderPayoutSnapshotConfiguration`; Provenance default `'Unknown'` matches domain.  
  - InboundWebhookReceipts matches `InboundWebhookReceipt` and `InboundWebhookReceiptConfiguration`.  
  - JobExecutions and PayoutSnapshotRepairRuns match current entities and configurations.  
- **Verdict:** Safe to run. No code changes required for alignment.

---

## 5. Exact SQL verification queries and expected results

**Verification script (already in repo):**  
`backend/scripts/verify-remediation-1.1.sql`

**Run:**

```powershell
$env:PGPASSWORD='<password>'; psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/verify-remediation-1.1.sql
```

**Before repair:**

- `EventStore.RootEventId exists` → **f**
- `OrderPayoutSnapshots` → **f**
- `InboundWebhookReceipts` → **f**
- `JobExecutions` → **t**
- `PayoutSnapshotRepairRuns` → **f**
- `IX_EventStore_RootEventId` → 0 rows

**After repair (expected):**

- `EventStore.RootEventId exists` → **t**
- `OrderPayoutSnapshots` → **t**
- `InboundWebhookReceipts` → **t**
- `JobExecutions` → **t**
- `PayoutSnapshotRepairRuns` → **t**
- `IX_EventStore_RootEventId` → 1 row (index definition shown)

**One-liner checks (optional):**

```sql
SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EventStore' AND column_name = 'RootEventId') AS "RootEventId";
SELECT to_regclass('public."OrderPayoutSnapshots"') IS NOT NULL AS "OrderPayoutSnapshots";
SELECT to_regclass('public."InboundWebhookReceipts"') IS NOT NULL AS "InboundWebhookReceipts";
SELECT to_regclass('public."JobExecutions"') IS NOT NULL AS "JobExecutions";
SELECT to_regclass('public."PayoutSnapshotRepairRuns"') IS NOT NULL AS "PayoutSnapshotRepairRuns";
```

---

## 6. Startup / background services that stop failing after each object is restored

| Object | Services that will stop failing once restored |
|--------|-----------------------------------------------|
| **EventStore.RootEventId** | **EventStoreDispatcherHostedService** — dispatcher claims/fetches events; missing column causes SQL errors and “EventStore schema is missing Phase 8 columns” critical log. |
| **OrderPayoutSnapshots** | **PayoutAnomalyService** (queries snapshots); **MissingPayoutSnapshotSchedulerService** (writes snapshots); **PayoutHealthDashboardService** (reads snapshots). |
| **InboundWebhookReceipts** | **InboundWebhookReceiptStore** (all operations); **EventPlatformRetentionService** (retention deletes); any inbound webhook API that persists receipts. |
| **JobExecutions** | Already present — **JobExecutionQueryService**, **EmailIngestionSchedulerService**, **LedgerReconciliationSchedulerService**, **StockSnapshotSchedulerService** already work. |
| **PayoutSnapshotRepairRuns** | **MissingPayoutSnapshotSchedulerService** (writes repair run records); **PayoutHealthDashboardService** (reads repair run history). |

---

## 7. Exact commands to run next

1. **Apply repair script (one-time):**

   ```powershell
   cd c:\Projects\CephasOps
   $env:PGPASSWORD='<your-postgres-password>'
   psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/apply-remediation-1.1-schema-repair.sql
   ```

2. **Re-run verification:**

   ```powershell
   psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/verify-remediation-1.1.sql
   ```

   Confirm all “exists”/table checks are **t** and `IX_EventStore_RootEventId` returns one row.

3. **Restart API** after verification passes.

---

## 8. Can the API be restarted safely now?

**Before running the repair script:** **No.** EventStoreDispatcherHostedService, MissingPayoutSnapshotSchedulerService, PayoutHealthDashboardService, InboundWebhookReceiptStore, and PayoutAnomalyService will hit missing column/table errors.

**After running** `apply-remediation-1.1-schema-repair.sql` **and confirming verification:** **Yes.** All schema objects required by the current code will be present; you can restart the API safely. No need to insert rows into `__EFMigrationsHistory` for this repair; existing history is left as-is.

---

## Summary

| Item | Result |
|------|--------|
| EventStore.RootEventId | Missing → repair script adds it |
| OrderPayoutSnapshots | Missing → repair script creates it |
| InboundWebhookReceipts | Missing → repair script creates it |
| JobExecutions | Present |
| PayoutSnapshotRepairRuns | Missing → repair script creates it |
| Responsible migration(s) | 20260309120000, 20260309210000, 20260310031127, 20260310120000 (partially applied or not in history) |
| Repair script | Safe, minimal, aligned with EF — run once then verify |
| Next steps | Run `apply-remediation-1.1-schema-repair.sql` → run `verify-remediation-1.1.sql` → restart API |
| Restart safe before repair? | No |
| Restart safe after repair? | Yes |
