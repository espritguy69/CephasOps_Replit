-- Remediation: Create missing integration tables that the startup schema guard requires.
-- These tables belong to migration '20260310031127_AddExternalIntegrationBus' which may
-- have been recorded in __EFMigrationsHistory without the tables actually being created.
-- All statements use IF NOT EXISTS so this script is safe to re-run.

BEGIN;

CREATE TABLE IF NOT EXISTS "ConnectorDefinitions" (
    "Id" uuid NOT NULL,
    "ConnectorKey" character varying(128) NOT NULL,
    "DisplayName" character varying(256) NOT NULL,
    "Description" character varying(1024),
    "ConnectorType" character varying(64) NOT NULL,
    "Direction" character varying(32) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ConnectorDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ExternalIdempotencyRecords" (
    "Id" uuid NOT NULL,
    "IdempotencyKey" character varying(512) NOT NULL,
    "ConnectorKey" character varying(128) NOT NULL,
    "CompanyId" uuid,
    "InboundWebhookReceiptId" uuid NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ExternalIdempotencyRecords" PRIMARY KEY ("Id")
);

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

CREATE TABLE IF NOT EXISTS "ConnectorEndpoints" (
    "Id" uuid NOT NULL,
    "ConnectorDefinitionId" uuid NOT NULL,
    "CompanyId" uuid,
    "EndpointUrl" character varying(2048) NOT NULL,
    "HttpMethod" character varying(16) NOT NULL,
    "AllowedEventTypes" character varying(2000),
    "SigningConfigJson" character varying(4000),
    "AuthConfigJson" character varying(4000),
    "RetryCount" integer NOT NULL,
    "TimeoutSeconds" integer NOT NULL,
    "IsPaused" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ConnectorEndpoints" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ConnectorEndpoints_ConnectorDefinitions_ConnectorDefinition~" FOREIGN KEY ("ConnectorDefinitionId") REFERENCES "ConnectorDefinitions" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "OutboundIntegrationAttempts" (
    "Id" uuid NOT NULL,
    "OutboundDeliveryId" uuid NOT NULL,
    "AttemptNumber" integer NOT NULL,
    "StartedAtUtc" timestamp with time zone NOT NULL,
    "CompletedAtUtc" timestamp with time zone,
    "Success" boolean NOT NULL,
    "HttpStatusCode" integer,
    "ResponseBodySnippet" character varying(2000),
    "ErrorMessage" character varying(2000),
    "DurationMs" integer,
    CONSTRAINT "PK_OutboundIntegrationAttempts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OutboundIntegrationAttempts_OutboundIntegrationDeliveries_O~" FOREIGN KEY ("OutboundDeliveryId") REFERENCES "OutboundIntegrationDeliveries" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConnectorDefinitions_ConnectorKey" ON "ConnectorDefinitions" ("ConnectorKey");
CREATE INDEX IF NOT EXISTS "IX_ConnectorEndpoints_CompanyId" ON "ConnectorEndpoints" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_ConnectorEndpoints_ConnectorDefinitionId_CompanyId" ON "ConnectorEndpoints" ("ConnectorDefinitionId", "CompanyId");
CREATE INDEX IF NOT EXISTS "IX_ExternalIdempotencyRecords_ConnectorKey_CompletedAtUtc" ON "ExternalIdempotencyRecords" ("ConnectorKey", "CompletedAtUtc");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ExternalIdempotencyRecords_IdempotencyKey" ON "ExternalIdempotencyRecords" ("IdempotencyKey");
CREATE INDEX IF NOT EXISTS "IX_OutboundIntegrationAttempts_OutboundDeliveryId" ON "OutboundIntegrationAttempts" ("OutboundDeliveryId");

COMMIT;
