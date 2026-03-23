-- Idempotent repair: add only the objects missing for API startup (EventStore.PartitionKey, JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, JobRuns.EventId).
-- Use when full idempotent script fails due to schema drift (e.g. columns already exist from script-only migrations).
-- Safe to run multiple times.

-- 1. EventStore.PartitionKey
ALTER TABLE "EventStore" ADD COLUMN IF NOT EXISTS "PartitionKey" character varying(500);
CREATE INDEX IF NOT EXISTS "IX_EventStore_PartitionKey" ON "EventStore" ("PartitionKey") WHERE "PartitionKey" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_EventStore_PartitionKey_CreatedAtUtc_EventId" ON "EventStore" ("PartitionKey", "CreatedAtUtc", "EventId") WHERE "PartitionKey" IS NOT NULL;

-- 2. JobExecutions
CREATE TABLE IF NOT EXISTS "JobExecutions" (
    "Id" uuid NOT NULL,
    "JobType" character varying(200) NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "Status" character varying(50) NOT NULL,
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "NextRunAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "StartedAtUtc" timestamp with time zone,
    "CompletedAtUtc" timestamp with time zone,
    "LastError" character varying(2000),
    "LastErrorAtUtc" timestamp with time zone,
    "CompanyId" uuid,
    "CorrelationId" character varying(200),
    "CausationId" uuid,
    "ProcessingNodeId" character varying(200),
    "ProcessingLeaseExpiresAtUtc" timestamp with time zone,
    "ClaimedAtUtc" timestamp with time zone,
    "Priority" integer NOT NULL,
    CONSTRAINT "PK_JobExecutions" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_JobExecutions_CompanyId_Status" ON "JobExecutions" ("CompanyId", "Status");
CREATE INDEX IF NOT EXISTS "IX_JobExecutions_Status_NextRunAtUtc" ON "JobExecutions" ("Status", "NextRunAtUtc");

-- 3. NotificationDispatches
CREATE TABLE IF NOT EXISTS "NotificationDispatches" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid,
    "Channel" character varying(50) NOT NULL,
    "Target" character varying(500) NOT NULL,
    "TemplateKey" character varying(200),
    "PayloadJson" jsonb,
    "Status" character varying(50) NOT NULL,
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "NextRetryAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone,
    "LastError" character varying(2000),
    "LastErrorAtUtc" timestamp with time zone,
    "CorrelationId" character varying(200),
    "CausationId" uuid,
    "SourceEventId" uuid,
    "IdempotencyKey" character varying(500),
    "ProcessingNodeId" character varying(200),
    "ProcessingLeaseExpiresAtUtc" timestamp with time zone,
    CONSTRAINT "PK_NotificationDispatches" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_NotificationDispatches_CompanyId_Status" ON "NotificationDispatches" ("CompanyId", "Status");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_NotificationDispatches_IdempotencyKey" ON "NotificationDispatches" ("IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_NotificationDispatches_Status_NextRetryAtUtc" ON "NotificationDispatches" ("Status", "NextRetryAtUtc");

-- 4. OutboundIntegrationDeliveries
CREATE TABLE IF NOT EXISTS "OutboundIntegrationDeliveries" (
    "Id" uuid NOT NULL,
    "ConnectorEndpointId" uuid NOT NULL,
    "CompanyId" uuid,
    "SourceEventId" uuid NOT NULL,
    "EventType" character varying(256) NOT NULL,
    "CorrelationId" character varying(128),
    "RootEventId" uuid,
    "WorkflowInstanceId" uuid,
    "CommandId" uuid,
    "IdempotencyKey" character varying(512) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "SignatureHeaderValue" character varying(512),
    "AttemptCount" integer NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "NextRetryAtUtc" timestamp with time zone,
    "DeliveredAtUtc" timestamp with time zone,
    "LastErrorMessage" character varying(2000),
    "LastHttpStatusCode" integer,
    "IsReplay" boolean NOT NULL,
    "ReplayOperationId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_OutboundIntegrationDeliveries" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationDeliveries_CompanyId_Status_CreatedAtUtc" ON "OutboundIntegrationDeliveries" ("CompanyId", "Status", "CreatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationDeliveries_ConnectorEndpointId_Status_CreatedAtUtc" ON "OutboundIntegrationDeliveries" ("ConnectorEndpointId", "Status", "CreatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationDeliveries_EventType_Status" ON "OutboundIntegrationDeliveries" ("EventType", "Status");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_OutboundIntegrationDeliveries_IdempotencyKey" ON "OutboundIntegrationDeliveries" ("IdempotencyKey");
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationDeliveries_NextRetryAtUtc" ON "OutboundIntegrationDeliveries" ("NextRetryAtUtc") WHERE "NextRetryAtUtc" IS NOT NULL;

-- 5. JobRuns.EventId (if JobRuns exists)
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'JobRuns') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'JobRuns' AND column_name = 'EventId') THEN
      ALTER TABLE "JobRuns" ADD COLUMN "EventId" uuid NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND tablename = 'JobRuns' AND indexname = 'IX_JobRuns_EventId') THEN
      CREATE INDEX "IX_JobRuns_EventId" ON "JobRuns" ("EventId") WHERE "EventId" IS NOT NULL;
    END IF;
  END IF;
END $$;

-- 6. Record migration so full idempotent script skips AddExternalIntegrationBus next time
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260310031127_AddExternalIntegrationBus', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310031127_AddExternalIntegrationBus');
