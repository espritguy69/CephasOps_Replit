# Remediation 1.1 — Critical Schema Drift (EventStore, OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions)

**Goal:** Resolve missing schema that breaks startup and background flows after company migration.

---

## 1. Root cause per missing schema object

| Schema object | Root cause | Confirmed/Inferred |
|---------------|------------|--------------------|
| **EventStore.RootEventId** (and Phase 8 columns) | Database was restored from a backup taken before Phase 8 migrations, or migrations were never applied. The EventStore table exists from an earlier migration but lacks columns added by `AddEventStorePhase8PlatformEnvelope` or `AddExternalIntegrationBus`. | **Confirmed** from audit: raw SQL in EventStoreRepository RETURNING clause references RootEventId; if column is missing, PostgreSQL throws. |
| **OrderPayoutSnapshots** table | Migration `AddOrderPayoutSnapshot` (20260309120000) was not applied. Later code and hosted services (MissingPayoutSnapshotRepairService, PayoutHealthDashboardService, OrderPayoutSnapshotService) expect the table. | **Confirmed** from code: DbSet and direct queries; MissingPayoutSnapshotSchedulerService fails on first run without table. |
| **InboundWebhookReceipts** table | Migration that creates this table lives inside `AddExternalIntegrationBus` (20260310031127). If that migration was not applied, the table is missing; EventPlatformRetentionService and InboundWebhookReceiptStore then fail. | **Confirmed** from code and migration Up(). |
| **JobExecutions** table / columns | Table is created by `AddJobExecutions` (20260309230000) or again inside `AddExternalIntegrationBus` (20260310031127). If neither was applied, JobExecutions is missing; JobExecutionWorkerHostedService and EmailIngestionSchedulerService fail. Current EF model matches: JobType, PayloadJson (jsonb), Status, CompanyId, NextRunAtUtc, etc. | **Confirmed** from configurations and migrations. |

---

## 2. Existing migrations (no new migrations required)

Migrations **already exist** for all four areas. No new migration needs to be generated.

| Schema need | Migration(s) that provide it | Expected objects |
|-------------|------------------------------|------------------|
| EventStore.RootEventId + Phase 8 | **20260309210000_AddEventStorePhase8PlatformEnvelope** | Adds columns: RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority; indexes on RootEventId, PartitionKey, ReplayId, PartitionKey+CreatedAtUtc+EventId. |
| EventStore (alternate path) | **20260310031127_AddExternalIntegrationBus** | Also adds RootEventId and other Phase 8–style columns to EventStore (overlap with above). **Warning:** If 20260309210000 was already applied, 20260310031127 will try to add the same columns and can fail with “column already exists”. Apply in timestamp order; if duplicates occur, use idempotent repair script below. |
| OrderPayoutSnapshots table | **20260309120000_AddOrderPayoutSnapshot** | Creates table OrderPayoutSnapshots with Id, OrderId, CompanyId, InstallerId, RateGroupId, BaseWorkRateId, ServiceProfileId, CustomRateId, LegacyRateId, BaseAmount, ModifierTraceJson, FinalPayout, Currency, ResolutionMatchLevel, PayoutPath, ResolutionTraceJson, CalculatedAt; unique index on OrderId. |
| OrderPayoutSnapshots.Provenance | **20260310120000_AddSnapshotProvenanceAndRepairRunHistory** | Adds column Provenance (varchar 32, default 'Unknown'); creates PayoutSnapshotRepairRuns table. |
| InboundWebhookReceipts table | **20260310031127_AddExternalIntegrationBus** | Creates table InboundWebhookReceipts with Id, ConnectorEndpointId, CompanyId, ExternalIdempotencyKey, ExternalEventId, ConnectorKey, MessageType, Status, PayloadJson (jsonb), CorrelationId, VerificationPassed, VerificationFailureReason, ReceivedAtUtc, ProcessedAtUtc, HandlerErrorMessage, HandlerAttemptCount, CreatedAtUtc, UpdatedAtUtc; indexes. |
| JobExecutions table | **20260309230000_AddJobExecutions** | Creates table JobExecutions with Id, JobType, PayloadJson (jsonb), Status, AttemptCount, MaxAttempts, NextRunAtUtc, CreatedAtUtc, UpdatedAtUtc, StartedAtUtc, CompletedAtUtc, LastError, LastErrorAtUtc, CompanyId, CorrelationId, CausationId, ProcessingNodeId, ProcessingLeaseExpiresAtUtc, ClaimedAtUtc, Priority; indexes IX_JobExecutions_Status_NextRunAtUtc, IX_JobExecutions_CompanyId_Status. |
| JobExecutions (alternate) | **20260310031127_AddExternalIntegrationBus** | Also creates JobExecutions with same shape. **Warning:** If 20260309230000 was already applied, 20260310031127 will try to create the table again and can fail. Apply in order; use idempotent script if needed. |

**Conclusion:** All required schema is covered by existing migrations. Apply them in chronological order. If the database already has some but not all (e.g. restored backup with partial history), use the **idempotent repair script** below to add only what is missing.

---

## 3. Exact commands to run

### Option A — Apply all pending migrations (recommended)

From repo root:

```bash
cd backend/src/CephasOps.Api
dotnet ef database update --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

Or with explicit connection string:

```bash
dotnet ef database update --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --connection "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=YOUR_PASSWORD;SslMode=Disable"
```

**If you see “column already exists” or “table already exists”:** Some migrations overlap (e.g. EventStore Phase 8 and AddExternalIntegrationBus both add RootEventId; AddJobExecutions and AddExternalIntegrationBus both create JobExecutions). In that case either:
- Run the **idempotent repair script** below (adds only missing objects), and/or  
- Manually insert into `__EFMigrationsHistory` the migration name that failed so EF skips it (only if you are sure the schema from that migration is already present).

### Option B — Generate and apply idempotent SQL script

Generate a single idempotent script (includes all migrations, with IF NOT EXISTS / ADD COLUMN IF NOT EXISTS where supported), then apply it:

```bash
cd backend/src/CephasOps.Api
dotnet ef migrations script --idempotent --output ../../scripts/apply-remediation-1.1-idempotent.sql --project ../CephasOps.Infrastructure/CephasOps.Infrastructure.csproj
```

Then apply (Windows PowerShell example):

```powershell
$env:PGPASSWORD = "YOUR_PASSWORD"
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/apply-remediation-1.1-idempotent.sql
```

---

## 4. Exact files changed (this remediation)

- **backend/scripts/apply-remediation-1.1-schema-repair.sql** (new): Idempotent SQL to add EventStore Phase 8 columns, OrderPayoutSnapshots (and Provenance), InboundWebhookReceipts, JobExecutions, PayoutSnapshotRepairRuns. Use when `dotnet ef database update` fails due to duplicate column/table.
- **backend/src/CephasOps.Application/Events/EventStoreDispatcherHostedService.cs**: Minimal runtime guard in `ProcessNextBatchAsync`: when claim throws and the exception message indicates missing column (e.g. RootEventId or "column ... does not exist"), log at **Critical** with instructions to apply migrations or the repair script, then return false (skip cycle) instead of logging Error every cycle.
- **docs/operations/REMEDIATION_1_1_SCHEMA_DRIFT.md** (this file): Deliverable document.

---

## 5. Schema verification checklist (SQL)

Run these against the target database. All should succeed (no “column/table does not exist” errors).

```sql
-- 1. EventStore has RootEventId (Phase 8)
SELECT "RootEventId" FROM "EventStore" LIMIT 0;

-- 2. OrderPayoutSnapshots table exists
SELECT 1 FROM "OrderPayoutSnapshots" LIMIT 0;

-- 3. OrderPayoutSnapshots has Provenance (if using repair/PayoutSnapshotRepairRuns)
SELECT "Provenance" FROM "OrderPayoutSnapshots" LIMIT 0;

-- 4. InboundWebhookReceipts table exists
SELECT 1 FROM "InboundWebhookReceipts" LIMIT 0;

-- 5. JobExecutions table exists and has required columns
SELECT "Id", "JobType", "PayloadJson", "Status", "CompanyId", "NextRunAtUtc" FROM "JobExecutions" LIMIT 0;

-- 6. (Optional) Migration history contains critical migrations
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20260309210000_AddEventStorePhase8PlatformEnvelope',
  '20260309120000_AddOrderPayoutSnapshot',
  '20260309230000_AddJobExecutions',
  '20260310031127_AddExternalIntegrationBus',
  '20260310120000_AddSnapshotProvenanceAndRepairRunHistory'
)
ORDER BY "MigrationId";
```

---

## 6. Startup / background services that depend on these objects

| Service | Depends on | Failure if missing |
|---------|------------|--------------------|
| **EventStoreDispatcherHostedService** | EventStore table with RootEventId (and Phase 8 columns) in RETURNING clause | Raw SQL fails (e.g. column "RootEventId" does not exist). Dispatcher throws every poll. |
| **MissingPayoutSnapshotSchedulerService** | OrderPayoutSnapshots, PayoutSnapshotRepairRuns | DetectMissingPayoutSnapshotsAsync / SaveChangesAsync fails; scheduler throws. |
| **EventPlatformRetentionWorkerHostedService** → EventPlatformRetentionService | EventStore, OutboundIntegrationDeliveries, **InboundWebhookReceipts**, ExternalIdempotencyRecords | Retention run fails when deleting from InboundWebhookReceipts (table or column missing). |
| **JobExecutionWorkerHostedService** | JobExecutions table | ClaimNextPendingBatchAsync or update fails; worker throws. |
| **EmailIngestionSchedulerService** | JobExecutions table | Queries JobExecutions; throws if table missing. |
| **PayoutHealthDashboardService** | OrderPayoutSnapshots | Queries OrderPayoutSnapshots; API or report fails. |
| **OrderPayoutSnapshotService** | OrderPayoutSnapshots | CreateSnapshotForOrderIfEligibleAsync fails. |
| **InboundWebhookReceiptStore** | InboundWebhookReceipts | All methods fail if table missing. |

---

## 7. Temporary runtime guard (if schema still missing)

If migrations cannot be applied immediately and you need the app to start without throwing repeatedly:

- **Recommendation:** In **EventStoreDispatcherHostedService**, before the first (or each) call to claim events, run a one-off check that the EventStore table has the RootEventId column. If not, log a critical message and skip the dispatch cycle (or disable the dispatcher until schema is fixed). Do not change EventStoreRepository’s raw SQL; the guard is only in the hosted service.

- **Implementation option:** In `EventStoreDispatcherHostedService.ExecuteAsync`, before the main loop (or inside the loop, once per N minutes), execute a trivial query that would fail if RootEventId is missing, e.g.  
  `SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EventStore' AND column_name = 'RootEventId'`  
  or  
  `SELECT "RootEventId" FROM "EventStore" LIMIT 0`.  
  If it throws or returns no row (for information_schema), set a “schema not ready” flag and skip calling `ClaimNextPendingBatchAsync` for that cycle; log critical so ops know to apply migrations.

- **Other services:** MissingPayoutSnapshotSchedulerService, EventPlatformRetentionService, and JobExecutionWorkerHostedService will still throw when they touch missing tables. A full “degrade safely” story would require similar schema checks and skip logic in each; the minimal recommendation is to **fix schema first** and use the guard only for the EventStore dispatcher to avoid repeated crashes while you apply the repair script.

---

## 8. Idempotent repair script (minimal add-only)

Use this when you cannot run `dotnet ef database update` without duplicate column/table errors, but you know the DB is missing only some objects. Run in a transaction and roll back if anything fails.

**File:** `backend/scripts/apply-remediation-1.1-schema-repair.sql` (created in repo).

It adds:

- EventStore: Phase 8 columns (RootEventId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, Priority) only if they do not exist.
- OrderPayoutSnapshots: CREATE TABLE IF NOT EXISTS with columns matching the current EF model; then ADD COLUMN IF NOT EXISTS for Provenance.
- InboundWebhookReceipts: CREATE TABLE IF NOT EXISTS with columns matching the current EF model.
- JobExecutions: CREATE TABLE IF NOT EXISTS with columns matching the current EF model.

After running the script, insert the corresponding migration names into `__EFMigrationsHistory` so that future `dotnet ef database update` runs do not try to re-apply those migrations.
