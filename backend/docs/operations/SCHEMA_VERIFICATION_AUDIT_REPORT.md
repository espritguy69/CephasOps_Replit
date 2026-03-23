# Schema Verification Audit Report

**Date:** 2026-03-13  
**Scope:** Development database (`cephasops` @ localhost) vs current EF Core model (ApplicationDbContextModelSnapshot).  
**Conclusion:** Development DB has **no pending EF Core migrations**. Schema drift was detected (4 tables missing due to a partial migration path). **Remediation has been executed successfully on Development** — see §6 Completed outcome.

---

## 1. Confirmed present objects (sensitive areas)

### EventStore / RootEventId
- **Table:** `EventStore` — present.
- **Columns (sample):** EventId, EventType, Payload, OccurredAtUtc, ProcessedAtUtc, RetryCount, Status, CorrelationId, CompanyId, CreatedAtUtc, EntityId, EntityType, LastError, LastErrorAtUtc, LastHandler, **RootEventId**, ParentEventId, NextRetryAtUtc, PayloadVersion, ProcessingStartedAtUtc, CausationId, PartitionKey, ReplayId, SourceService, SourceModule, CapturedAtUtc, IdempotencyKey, TraceId, SpanId, ProcessingLeaseExpiresAtUtc, LastClaimedAtUtc, LastClaimedBy.
- **Indexes:** PK_EventStore, IX_EventStore_Status, IX_EventStore_Status_NextRetryAtUtc, IX_EventStore_RootEventId (filtered), IX_EventStore_Status_ProcessingLeaseExpiresAtUtc, IX_EventStore_CorrelationId, IX_EventStore_OccurredAtUtc, IX_EventStore_PartitionKey, IX_EventStore_PartitionKey_CreatedAtUtc_EventId, IX_EventStore_ReplayId, IX_EventStore_CompanyId_EventType_OccurredAtUtc.
- **Verdict:** Aligned with model; RootEventId and all expected indexes present.

### EventStoreAttemptHistory
- **Table:** `EventStoreAttemptHistory` — present.
- **Columns:** Id, EventId, EventType, CompanyId, HandlerName, AttemptNumber, Status, StartedAtUtc, FinishedAtUtc, DurationMs, ProcessingNodeId, ErrorType, ErrorMessage, StackTraceSummary, WasRetried, WasDeadLettered.
- **Verdict:** Aligned with model.

### OrderPayoutSnapshots
- **Table:** `OrderPayoutSnapshots` — present.
- **Columns:** Id, OrderId, CompanyId, InstallerId, RateGroupId, BaseWorkRateId, ServiceProfileId, CustomRateId, LegacyRateId, BaseAmount, ModifierTraceJson, FinalPayout, Currency, ResolutionMatchLevel, PayoutPath, ResolutionTraceJson, CalculatedAt, **Provenance**.
- **Indexes:** PK_OrderPayoutSnapshots, IX_OrderPayoutSnapshots_OrderId (unique).
- **Verdict:** Aligned with model (including Provenance).

### PayoutSnapshotRepairRuns
- **Table:** `PayoutSnapshotRepairRuns` — present.
- **Columns:** Id, StartedAt, CompletedAt, TotalProcessed, CreatedCount, SkippedCount, ErrorCount, ErrorOrderIdsJson, TriggerSource, Notes.
- **Verdict:** Aligned with model.

### InboundWebhookReceipts
- **Table:** `InboundWebhookReceipts` — present.
- **Indexes:** PK_InboundWebhookReceipts, IX_InboundWebhookReceipts_CompanyId, IX_InboundWebhookReceipts_ConnectorKey_ExternalIdempotencyKey (unique), IX_InboundWebhookReceipts_ConnectorKey_Status_ReceivedAtUtc.
- **Verdict:** Table and indexes present. **Note:** `ConnectorEndpoints` table now exists (remediated); no FK from InboundWebhookReceipts to ConnectorEndpoints was added (by design until orphan ConnectorEndpointId values are resolved).

### JobExecutions
- **Table:** `JobExecutions` — present.
- **Columns (20):** Id, JobType, PayloadJson, Status, AttemptCount, MaxAttempts, NextRunAtUtc, CreatedAtUtc, UpdatedAtUtc, StartedAtUtc, CompletedAtUtc, LastError, LastErrorAtUtc, CompanyId, CorrelationId, CausationId, ProcessingNodeId, ProcessingLeaseExpiresAtUtc, ClaimedAtUtc, Priority.
- **Indexes:** PK_JobExecutions, IX_JobExecutions_CompanyId_Status, IX_JobExecutions_Status_NextRunAtUtc.
- **Verdict:** Aligned with model.

### Feature flags / operational insights
- **OperationalInsights** — present (columns: Id, CompanyId, Type, PayloadJson, OccurredAtUtc, EntityType, EntityId).
- **BillingPlanFeatures** — present (Id, BillingPlanId, FeatureKey, CreatedAtUtc).
- **TenantFeatureFlags** — present (Id, TenantId, FeatureKey, IsEnabled, UpdatedAtUtc).
- **Verdict:** All present and aligned.

### Other integration / event tables (present)
- **LedgerEntries**, **LedgerBalanceCaches**, **OutboundIntegrationDeliveries**, **EventProcessingLog**, **CommandProcessingLogs** (and remaining tables from the migration chain) — present where checked; public schema has 196 tables total.

---

## 2. Missing objects (remediated on Development)

The following **4 tables** were expected by the current EF Core model but were missing in the Development database (due to repair-script application). They have since been created by the schema drift remediation:

| Table | Purpose (from model) |
|-------|----------------------|
| **ConnectorDefinitions** | Integration connector metadata (ConnectorKey, DisplayName, Direction, etc.) |
| **ConnectorEndpoints** | Per-company/connector endpoints (FK to ConnectorDefinitions) |
| **ExternalIdempotencyRecords** | Idempotency for inbound webhooks (ConnectorKey, InboundWebhookReceiptId, etc.) |
| **OutboundIntegrationAttempts** | Per-attempt log for outbound deliveries (FK to OutboundIntegrationDeliveries) |

**Cause:** Migration `20260310031127_AddExternalIntegrationBus` was applied via the **repair script** (`apply-AddExternalIntegrationBus-repair.sql`), which only creates a subset of objects (EventStore columns, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, JobRuns.EventId) and then records the migration in `__EFMigrationsHistory`. The full migration also creates ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, and OutboundIntegrationAttempts; those steps were never run.

**Impact:** Any code path that uses `ConnectorDefinitions`, `ConnectorEndpoints`, `ExternalIdempotencyRecords`, or `OutboundIntegrationAttempts` (e.g. integration bus, connector management, idempotency checks) will fail at runtime with missing table errors.

---

## 3. Suspicious mismatches

- **InboundWebhookReceipts.ConnectorEndpointId:** Column exists and is non-nullable in the model, but the referenced table `ConnectorEndpoints` is missing. There is no FK in the DB from `InboundWebhookReceipts` to `ConnectorEndpoints`. If the application assumes a valid FK or joins to ConnectorEndpoints, behavior may be inconsistent or throw.
- **OutboundIntegrationDeliveries.ConnectorEndpointId:** Same situation: column references ConnectorEndpoints, which does not exist. No FK present in DB.
- **Applied migrations vs codebase:** DB has 99 applied migrations including `20260311120000_AddPayoutAnomalyReview` and `20260312100000_AddEventStorePhase7LeaseAndAttemptHistory`; the codebase migration chain may have a different length. This was not treated as schema drift for this audit but is worth keeping consistent.

---

## 4. Remediation (only because objects are missing)

**Do not create new migrations.** Apply the following SQL only if you need the four missing tables to align the Development database with the current model. Run in order.

```sql
-- 1. ConnectorDefinitions (no FK dependency)
CREATE TABLE IF NOT EXISTS "ConnectorDefinitions" (
    "Id" uuid NOT NULL,
    "ConnectorKey" character varying(128) NOT NULL,
    "DisplayName" character varying(256) NOT NULL,
    "Description" character varying(1024) NULL,
    "ConnectorType" character varying(64) NOT NULL,
    "Direction" character varying(32) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ConnectorDefinitions" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConnectorDefinitions_ConnectorKey" ON "ConnectorDefinitions" ("ConnectorKey");

-- 2. ConnectorEndpoints (FK to ConnectorDefinitions)
CREATE TABLE IF NOT EXISTS "ConnectorEndpoints" (
    "Id" uuid NOT NULL,
    "ConnectorDefinitionId" uuid NOT NULL,
    "CompanyId" uuid NULL,
    "EndpointUrl" character varying(2048) NOT NULL,
    "HttpMethod" character varying(16) NOT NULL,
    "AllowedEventTypes" character varying(2000) NULL,
    "SigningConfigJson" character varying(4000) NULL,
    "AuthConfigJson" character varying(4000) NULL,
    "RetryCount" integer NOT NULL,
    "TimeoutSeconds" integer NOT NULL,
    "IsPaused" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ConnectorEndpoints" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConnectorEndpoints_ConnectorDefinitions_ConnectorDefinitionId" 
        FOREIGN KEY ("ConnectorDefinitionId") REFERENCES "ConnectorDefinitions" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_ConnectorEndpoints_CompanyId" ON "ConnectorEndpoints" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_ConnectorEndpoints_ConnectorDefinitionId_CompanyId" ON "ConnectorEndpoints" ("ConnectorDefinitionId", "CompanyId");

-- 3. ExternalIdempotencyRecords (no FK to ConnectorEndpoints; references InboundWebhookReceipts which exists)
CREATE TABLE IF NOT EXISTS "ExternalIdempotencyRecords" (
    "Id" uuid NOT NULL,
    "IdempotencyKey" character varying(512) NOT NULL,
    "ConnectorKey" character varying(128) NOT NULL,
    "CompanyId" uuid NULL,
    "InboundWebhookReceiptId" uuid NOT NULL,
    "CompletedAtUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ExternalIdempotencyRecords" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ExternalIdempotencyRecords_IdempotencyKey" ON "ExternalIdempotencyRecords" ("IdempotencyKey");
CREATE INDEX IF NOT EXISTS "IX_ExternalIdempotencyRecords_ConnectorKey_CompletedAtUtc" ON "ExternalIdempotencyRecords" ("ConnectorKey", "CompletedAtUtc");

-- 4. OutboundIntegrationAttempts (FK to OutboundIntegrationDeliveries)
CREATE TABLE IF NOT EXISTS "OutboundIntegrationAttempts" (
    "Id" uuid NOT NULL,
    "OutboundDeliveryId" uuid NOT NULL,
    "AttemptNumber" integer NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone NULL,
    "Success" boolean NOT NULL,
    "HttpStatusCode" integer NULL,
    "ResponseBodySnippet" character varying(2000) NULL,
    "ErrorMessage" character varying(2000) NULL,
    "DurationMs" integer NULL,
    CONSTRAINT "PK_OutboundIntegrationAttempts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OutboundIntegrationAttempts_OutboundIntegrationDeliveries_OutboundDeliveryId" 
        FOREIGN KEY ("OutboundDeliveryId") REFERENCES "OutboundIntegrationDeliveries" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationAttempts_OutboundDeliveryId" ON "OutboundIntegrationAttempts" ("OutboundDeliveryId");
```

**Note:** This does **not** add FKs from `InboundWebhookReceipts` or `OutboundIntegrationDeliveries` to `ConnectorEndpoints`, so existing rows with `ConnectorEndpointId` values that are not in `ConnectorEndpoints` will not be validated. Add those FKs only after ensuring data consistency (e.g. backfilling or nulling invalid references).

---

## 5. Summary

| Category | Result |
|----------|--------|
| **EF migration status** | No pending migrations; database is up to date with applied migration history. |
| **EventStore / RootEventId** | Present and aligned. |
| **OrderPayoutSnapshots** | Present and aligned (including Provenance). |
| **PayoutSnapshotRepairRuns** | Present and aligned. |
| **InboundWebhookReceipts** | Present; ConnectorEndpoints now present (remediated). |
| **JobExecutions** | Present and aligned. |
| **OperationalInsights / BillingPlanFeatures / TenantFeatureFlags** | Present and aligned. |
| **Remediated tables (Development)** | ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts — created by remediation; indexes and expected FKs verified. |
| **Remediation for other envs** | Use `backend/scripts/apply-schema-drift-remediation.sql`; do not add new migrations. |

---

## 6. Completed outcome (Development)

- **Remediation executed successfully** on the Development database.
- **4 tables restored:** ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts.
- **Expected indexes and FKs verified** (index counts 2, 3, 3, 2; exactly 2 FKs: ConnectorEndpoints→ConnectorDefinitions, OutboundIntegrationAttempts→OutboundIntegrationDeliveries).
- **`__EFMigrationsHistory` unchanged** (count remained 117; no migration rows added or modified).
- **No application code or EF migration changes.**  
The Development database is now aligned with the current EF model for the remediated integration-bus objects.
