-- Schema drift remediation: create the 4 missing tables (and indexes only) for Development DB.
-- Source: backend/docs/operations/SCHEMA_VERIFICATION_AUDIT_REPORT.md §4.
-- Scope: ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts.
-- Does NOT add FKs from InboundWebhookReceipts or OutboundIntegrationDeliveries to ConnectorEndpoints.
-- Idempotent: safe to run multiple times. Run in order.

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
