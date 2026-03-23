# Remediation 1.1 — Execution Support

**Purpose:** Script review against current EF model, execution checklist, and go/no-go for Section 2.

---

## 1. Script review result

**Script:** `backend/scripts/apply-remediation-1.1-schema-repair.sql`  
**Reference:** `EventStoreEntry`, `OrderPayoutSnapshot`, `InboundWebhookReceipt`, `JobExecution`, `PayoutSnapshotRepairRun` entities; their EF configurations; migrations `20260309210000`, `20260309120000`, `20260310031127`, `20260310120000`.

### 1.1 EventStore (Phase 8 columns and indexes)

| Script column/index | EF / config | Type / nullability | Match |
|--------------------|-------------|--------------------|--------|
| RootEventId | EventStoreEntry.RootEventId, EventStoreEntryConfiguration | uuid, nullable | Yes |
| PartitionKey | .PartitionKey, HasMaxLength(500) | character varying(500) NULL | Yes |
| ReplayId | .ReplayId, HasMaxLength(100) | character varying(100) NULL | Yes |
| SourceService | .SourceService, HasMaxLength(100) | character varying(100) NULL | Yes |
| SourceModule | .SourceModule, HasMaxLength(100) | character varying(100) NULL | Yes |
| CapturedAtUtc | .CapturedAtUtc | timestamp with time zone NULL | Yes |
| IdempotencyKey | .IdempotencyKey, HasMaxLength(500) | character varying(500) NULL | Yes |
| TraceId | .TraceId, HasMaxLength(64) | character varying(64) NULL | Yes |
| SpanId | .SpanId, HasMaxLength(64) | character varying(64) NULL | Yes |
| Priority | .Priority, HasMaxLength(50) | character varying(50) NULL | Yes |
| IX_EventStore_RootEventId | HasIndex(RootEventId), filter "RootEventId IS NOT NULL" | partial index | Yes |
| IX_EventStore_PartitionKey | HasIndex(PartitionKey), filter | partial index | Yes |
| IX_EventStore_ReplayId | HasIndex(ReplayId), filter | partial index | Yes |
| IX_EventStore_PartitionKey_CreatedAtUtc_EventId | HasIndex(PartitionKey, CreatedAtUtc, EventId), filter | partial index | Yes |

**Note:** If `PartitionKey` (or any Phase 8 column) already exists, `ADD COLUMN IF NOT EXISTS` is a no-op. No drift.

---

### 1.2 OrderPayoutSnapshots

| Script column | Entity / migration | Type / nullability | Match |
|--------------|--------------------|--------------------|--------|
| Id | OrderPayoutSnapshot.Id | uuid NOT NULL | Yes |
| OrderId | .OrderId | uuid NOT NULL | Yes |
| CompanyId, InstallerId, RateGroupId, BaseWorkRateId, ServiceProfileId, CustomRateId, LegacyRateId | entity | uuid NULL | Yes |
| BaseAmount | .BaseAmount, Precision(18,4) | numeric(18,4) NULL | Yes |
| ModifierTraceJson, ResolutionTraceJson | .ModifierTraceJson, .ResolutionTraceJson, text | text NULL | Yes |
| FinalPayout | .FinalPayout, Precision(18,4) | numeric(18,4) NOT NULL | Yes |
| Currency | .Currency, MaxLength(3) | character varying(3) NOT NULL | Yes |
| ResolutionMatchLevel, PayoutPath | entity, MaxLength(64) | character varying(64) NULL | Yes |
| CalculatedAt | .CalculatedAt | timestamp with time zone NOT NULL | Yes |
| Provenance | .Provenance, SnapshotProvenance.Unknown | ADD COLUMN IF NOT EXISTS, DEFAULT 'Unknown', varchar(32) NOT NULL | Yes |
| PK_OrderPayoutSnapshots | Primary key Id | Yes |
| IX_OrderPayoutSnapshots_OrderId | HasIndex(OrderId).IsUnique() | UNIQUE INDEX | Yes |

---

### 1.3 InboundWebhookReceipts

| Script column | Entity / migration 20260310031127 | Type / nullability | Match |
|---------------|-----------------------------------|--------------------|--------|
| Id, ConnectorEndpointId, CompanyId | entity | uuid NOT NULL / NOT NULL / NULL | Yes |
| ExternalIdempotencyKey, ExternalEventId, ConnectorKey, MessageType | entity, max lengths 512, 256, 128, 128 | varchar lengths as script | Yes |
| Status | entity | character varying(32) NOT NULL | Yes |
| PayloadJson | entity, jsonb | jsonb NOT NULL | Yes |
| CorrelationId, VerificationFailureReason, HandlerErrorMessage | entity | varchar 128 / 2000 / 2000, nullability as script | Yes |
| VerificationPassed | entity | boolean NOT NULL | Yes |
| ReceivedAtUtc, ProcessedAtUtc, CreatedAtUtc, UpdatedAtUtc | entity | timestamp with time zone as script | Yes |
| HandlerAttemptCount | entity | integer NULL | Yes |
| PK, IX_CompanyId, IX_ConnectorKey_ExternalIdempotencyKey (unique), IX_ConnectorKey_Status_ReceivedAtUtc | InboundWebhookReceiptConfiguration | Yes |

---

### 1.4 JobExecutions

| Script column | Entity / migration | Type / nullability | Match |
|---------------|--------------------|--------------------|--------|
| Id, JobType, PayloadJson, Status, AttemptCount, MaxAttempts | JobExecution | uuid, varchar(200), jsonb, varchar(50), int NOT NULL | Yes |
| NextRunAtUtc, CreatedAtUtc, UpdatedAtUtc, StartedAtUtc, CompletedAtUtc, LastErrorAtUtc | entity | timestamp with time zone, nullability as migration | Yes |
| LastError, CompanyId, CorrelationId, CausationId, ProcessingNodeId, ProcessingLeaseExpiresAtUtc, ClaimedAtUtc | entity | types and nullability as 20260310031127 | Yes |
| Priority | entity | integer NOT NULL | Yes |
| IX_JobExecutions_Status_NextRunAtUtc, IX_JobExecutions_CompanyId_Status | JobExecutionConfiguration | Yes |

**Note:** Table already exists in DB. Script uses `CREATE TABLE IF NOT EXISTS` → no-op; no duplicate table or drift.

---

### 1.5 PayoutSnapshotRepairRuns

| Script column | Entity / migration 20260310120000 | Type / nullability | Match |
|---------------|-----------------------------------|--------------------|--------|
| Id | PayoutSnapshotRepairRun.Id | uuid NOT NULL | Yes |
| StartedAt | .StartedAt | timestamp with time zone NOT NULL | Yes |
| CompletedAt | .CompletedAt | timestamp with time zone NULL | Yes |
| TotalProcessed, CreatedCount, SkippedCount, ErrorCount | entity | integer NOT NULL | Yes |
| ErrorOrderIdsJson | .ErrorOrderIdsJson, text | text NULL | Yes |
| TriggerSource | .TriggerSource, MaxLength(32) | character varying(32) NOT NULL | Yes |
| Notes | .Notes, MaxLength(500) | character varying(500) NULL | Yes |
| IX_PayoutSnapshotRepairRuns_StartedAt | HasIndex(StartedAt).IsDescending() | INDEX (StartedAt DESC) | Yes |

---

## 2. Corrections needed before execution

**None.** The script matches the current EF model, entity configurations, and migrations. All operations are idempotent (`ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`). No edits to the script are required before running it.

---

## 3. Risk of drift from current model

- **No intentional drift:** Column names, types, lengths, and nullability align with entities and configurations.
- **Existing columns/tables:** For EventStore, any Phase 8 column that already exists is skipped. For JobExecutions, the table already exists so creation is skipped. No duplicate objects.
- **History:** The script does not insert into `__EFMigrationsHistory` (that block is commented out). Existing history is left unchanged; the script only repairs physical schema.
- **Conclusion:** Safe to run; no model drift introduced.

---

## 4. Execution checklist

### Pre-flight

- [ ] Database backup or restore point taken (recommended).
- [ ] Connection details confirmed: Host, Port, Database, User (e.g. `appsettings.Development.json` or env).
- [ ] No other process is applying migrations or altering the same tables during the run.

### Apply repair script

```powershell
cd c:\Projects\CephasOps
$env:PGPASSWORD='<your-postgres-password>'
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/apply-remediation-1.1-schema-repair.sql
```

- [ ] Command completed with exit code 0.
- [ ] No ERROR lines in psql output (warnings about “already exists” are acceptable).

### Verify schema

```powershell
psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/verify-remediation-1.1.sql
```

- [ ] `EventStore.RootEventId exists` = **t**
- [ ] `OrderPayoutSnapshots` = **t**
- [ ] `InboundWebhookReceipts` = **t**
- [ ] `JobExecutions` = **t**
- [ ] `PayoutSnapshotRepairRuns` = **t**
- [ ] `IX_EventStore_RootEventId` returns 1 row.

### Restart API

- [ ] Stop the running API (if any).
- [ ] Start the API (e.g. `cd backend/src/CephasOps.Api && dotnet run` or your usual command).
- [ ] API starts without migration or schema-related exceptions.

### Post-restart log check

- [ ] No critical log: “EventStore schema is missing Phase 8 columns (e.g. RootEventId)”.
- [ ] EventStoreDispatcherHostedService runs without column/table errors.
- [ ] MissingPayoutSnapshotSchedulerService (if enabled) runs without table errors.
- [ ] No errors referencing `InboundWebhookReceipts` or `OrderPayoutSnapshots` or `PayoutSnapshotRepairRuns` from normal request or background paths.

### Sign-off

- [ ] Remediation 1.1 confirmed complete (schema applied, verification passed, API restarted, logs clean).

---

## 5. Post-restart verification checklist (summary)

| Check | How |
|-------|-----|
| Schema | Run `verify-remediation-1.1.sql`; all five object checks true, RootEventId index present. |
| API startup | No exception on startup; no “missing column” or “relation does not exist” in stack traces. |
| Dispatcher | Logs show normal claim/process cycles; no Critical “Phase 8 columns” message. |
| Payout / webhook | Optional: trigger or wait for MissingPayoutSnapshotSchedulerService and a request that uses InboundWebhookReceipts; no SQL errors. |

---

## 6. Section 2 scope (do not implement until 1.1 complete)

From `POST_COMPANY_MIGRATION_REMEDIATION_PLAN.md`:

- **2.1 DefaultCompanyId:** Set `Tenant:DefaultCompanyId` in appsettings (and/or production config / env) to the default company GUID so legacy users (User.CompanyId and first-department company null) get a non-null tenant and scoped data.
- **2.2 Company-scoped controller guard:** In company-scoped controllers, replace `CompanyId ?? Guid.Empty` with an explicit check: if the operation is company-scoped and `(!companyId.HasValue || companyId.Value == Guid.Empty)` then return 403 (or 400) with a clear message; otherwise use `companyId.Value`. Target controllers include (per plan): InventoryController, PnlController, ApprovalWorkflowsController, AssetTypesController, EmailAccountsController, SupplierInvoicesController, BuildingsController (where effectiveCompanyId == Guid.Empty).
- **2.3 Backfill User.CompanyId (optional):** One-time data fix for users with null CompanyId; can be done after 2.1/2.2 if desired.

**Do not implement Section 2** until Remediation 1.1 is confirmed complete (repair script applied, verification passed, API restarted, logs checked).

---

## 7. Go/no-go for moving to Section 2

| Criterion | Go | No-go |
|-----------|----|--------|
| Repair script applied successfully | Yes | Script failed or not run |
| Verification script shows all objects present | All five true, index present | Any missing object or index |
| API restarts without schema errors | Yes | Startup exception or “missing column/table” |
| Dispatcher and payout/webhook-related logs clean | No Phase 8 / table errors | Critical or repeated errors |

**Recommendation:**

- **Go for Section 2:** Only after every item in the execution checklist (apply script, verify schema, restart API, check logs) is done and the post-restart verification checklist passes. Then proceed with 2.1 (DefaultCompanyId) and 2.2 (company-scoped controller guard) per the remediation plan.
- **No-go:** If verification fails, API fails to start, or logs show schema-related errors, resolve 1.1 fully (re-run or fix script, re-verify, restart) before starting Section 2.

---

## 8. Deliverables summary

| Deliverable | Location |
|-------------|----------|
| Script review result | This document §1; script matches EF model, no changes needed. |
| Corrections before execution | None (§2). |
| Execution checklist | §4 (pre-flight, apply, verify, restart, logs, sign-off). |
| Post-restart verification | §5. |
| Section 2 scope | §6 (DefaultCompanyId, controller guard, optional backfill). |
| Go/no-go for Section 2 | §7 (go only after 1.1 complete and verified). |
