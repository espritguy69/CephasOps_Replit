-- Remediation 1.1: Idempotent schema repair for critical objects
-- Use when "dotnet ef database update" fails due to duplicate column/table (e.g. partial migration history).
-- Run with: psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/apply-remediation-1.1-schema-repair.sql
-- PostgreSQL 9.5+ (ADD COLUMN IF NOT EXISTS, CREATE TABLE IF NOT EXISTS).

BEGIN;

-- ========== 1. EventStore: Phase 8 columns (RootEventId, etc.) ==========
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "RootEventId" uuid NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "PartitionKey" character varying(500) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "ReplayId" character varying(100) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "SourceService" character varying(100) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "SourceModule" character varying(100) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "CapturedAtUtc" timestamp with time zone NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "IdempotencyKey" character varying(500) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "TraceId" character varying(64) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "SpanId" character varying(64) NULL;
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "Priority" character varying(50) NULL;

CREATE INDEX IF NOT EXISTS "IX_EventStore_RootEventId" ON "EventStore" ("RootEventId") WHERE "RootEventId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_EventStore_PartitionKey" ON "EventStore" ("PartitionKey") WHERE "PartitionKey" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_EventStore_ReplayId" ON "EventStore" ("ReplayId") WHERE "ReplayId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_EventStore_PartitionKey_CreatedAtUtc_EventId" ON "EventStore" ("PartitionKey", "CreatedAtUtc", "EventId") WHERE "PartitionKey" IS NOT NULL;

-- ========== 2. OrderPayoutSnapshots table ==========
CREATE TABLE IF NOT EXISTS "OrderPayoutSnapshots" (
    "Id" uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "CompanyId" uuid NULL,
    "InstallerId" uuid NULL,
    "RateGroupId" uuid NULL,
    "BaseWorkRateId" uuid NULL,
    "ServiceProfileId" uuid NULL,
    "CustomRateId" uuid NULL,
    "LegacyRateId" uuid NULL,
    "BaseAmount" numeric(18,4) NULL,
    "ModifierTraceJson" text NULL,
    "FinalPayout" numeric(18,4) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "ResolutionMatchLevel" character varying(64) NULL,
    "PayoutPath" character varying(64) NULL,
    "ResolutionTraceJson" text NULL,
    "CalculatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OrderPayoutSnapshots" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_OrderPayoutSnapshots_OrderId" ON "OrderPayoutSnapshots" ("OrderId");

ALTER TABLE "OrderPayoutSnapshots" ADD COLUMN IF NOT EXISTS "Provenance" character varying(32) NOT NULL DEFAULT 'Unknown';

-- ========== 3. InboundWebhookReceipts table ==========
CREATE TABLE IF NOT EXISTS "InboundWebhookReceipts" (
    "Id" uuid NOT NULL,
    "ConnectorEndpointId" uuid NOT NULL,
    "CompanyId" uuid NULL,
    "ExternalIdempotencyKey" character varying(512) NOT NULL,
    "ExternalEventId" character varying(256) NULL,
    "ConnectorKey" character varying(128) NOT NULL,
    "MessageType" character varying(128) NULL,
    "Status" character varying(32) NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "CorrelationId" character varying(128) NULL,
    "VerificationPassed" boolean NOT NULL,
    "VerificationFailureReason" character varying(2000) NULL,
    "ReceivedAtUtc" timestamp with time zone NOT NULL,
    "ProcessedAtUtc" timestamp with time zone NULL,
    "HandlerErrorMessage" character varying(2000) NULL,
    "HandlerAttemptCount" integer NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_InboundWebhookReceipts" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_InboundWebhookReceipts_CompanyId" ON "InboundWebhookReceipts" ("CompanyId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_InboundWebhookReceipts_ConnectorKey_ExternalIdempotencyKey" ON "InboundWebhookReceipts" ("ConnectorKey", "ExternalIdempotencyKey");
CREATE INDEX IF NOT EXISTS "IX_InboundWebhookReceipts_ConnectorKey_Status_ReceivedAtUtc" ON "InboundWebhookReceipts" ("ConnectorKey", "Status", "ReceivedAtUtc");

-- ========== 4. JobExecutions table ==========
CREATE TABLE IF NOT EXISTS "JobExecutions" (
    "Id" uuid NOT NULL,
    "JobType" character varying(200) NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "Status" character varying(50) NOT NULL,
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "NextRunAtUtc" timestamp with time zone NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "StartedAtUtc" timestamp with time zone NULL,
    "CompletedAtUtc" timestamp with time zone NULL,
    "LastError" character varying(2000) NULL,
    "LastErrorAtUtc" timestamp with time zone NULL,
    "CompanyId" uuid NULL,
    "CorrelationId" character varying(200) NULL,
    "CausationId" uuid NULL,
    "ProcessingNodeId" character varying(200) NULL,
    "ProcessingLeaseExpiresAtUtc" timestamp with time zone NULL,
    "ClaimedAtUtc" timestamp with time zone NULL,
    "Priority" integer NOT NULL,
    CONSTRAINT "PK_JobExecutions" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_JobExecutions_Status_NextRunAtUtc" ON "JobExecutions" ("Status", "NextRunAtUtc");
CREATE INDEX IF NOT EXISTS "IX_JobExecutions_CompanyId_Status" ON "JobExecutions" ("CompanyId", "Status");

-- ========== 5. PayoutSnapshotRepairRuns (required by MissingPayoutSnapshotSchedulerService) ==========
CREATE TABLE IF NOT EXISTS "PayoutSnapshotRepairRuns" (
    "Id" uuid NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone NULL,
    "TotalProcessed" integer NOT NULL,
    "CreatedCount" integer NOT NULL,
    "SkippedCount" integer NOT NULL,
    "ErrorCount" integer NOT NULL,
    "ErrorOrderIdsJson" text NULL,
    "TriggerSource" character varying(32) NOT NULL,
    "Notes" character varying(500) NULL,
    CONSTRAINT "PK_PayoutSnapshotRepairRuns" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_PayoutSnapshotRepairRuns_StartedAt" ON "PayoutSnapshotRepairRuns" ("StartedAt" DESC);

COMMIT;

-- After running this script, you may need to insert into __EFMigrationsHistory so EF does not re-apply migrations:
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
--   ('20260309210000_AddEventStorePhase8PlatformEnvelope', '8.0.x'),
--   ('20260309120000_AddOrderPayoutSnapshot', '8.0.x'),
--   ('20260309230000_AddJobExecutions', '8.0.x'),
--   ('20260310031127_AddExternalIntegrationBus', '8.0.x'),
--   ('20260310120000_AddSnapshotProvenanceAndRepairRunHistory', '8.0.x')
-- ON CONFLICT ("MigrationId") DO NOTHING;
