-- Fix Phase 6 Workflow Engine Tables - Drop and recreate with correct structure
-- This script fixes existing tables that have incorrect column names

-- Drop existing tables with wrong structure
DROP TABLE IF EXISTS "WorkflowTransitions" CASCADE;
DROP TABLE IF EXISTS "BackgroundJobs" CASCADE;
DROP TABLE IF EXISTS "SystemLogs" CASCADE;

-- Recreate WorkflowTransitions with correct structure
CREATE TABLE "WorkflowTransitions" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "WorkflowDefinitionId" uuid NOT NULL,
    "FromStatus" character varying(100) NULL,
    "ToStatus" character varying(100) NOT NULL,
    "AllowedRolesJson" jsonb NOT NULL DEFAULT '[]',
    "GuardConditionsJson" jsonb NULL,
    "SideEffectsConfigJson" jsonb NULL,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_WorkflowTransitions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkflowTransitions_WorkflowDefinitions_WorkflowDefinitionId" 
        FOREIGN KEY ("WorkflowDefinitionId") REFERENCES "WorkflowDefinitions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorkflowTransitions_Companies_CompanyId" 
        FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_FromStatus_ToStatus" 
    ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus");
CREATE INDEX "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_IsActive" 
    ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "IsActive");

-- Recreate BackgroundJobs with correct structure
CREATE TABLE "BackgroundJobs" (
    "Id" uuid NOT NULL,
    "JobType" character varying(100) NOT NULL,
    "PayloadJson" jsonb NOT NULL DEFAULT '{}',
    "State" character varying(50) NOT NULL DEFAULT 'Queued',
    "RetryCount" integer NOT NULL DEFAULT 0,
    "MaxRetries" integer NOT NULL DEFAULT 3,
    "LastError" character varying(2000) NULL,
    "Priority" integer NOT NULL DEFAULT 0,
    "ScheduledAt" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_BackgroundJobs" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_BackgroundJobs_State_Priority_CreatedAt" 
    ON "BackgroundJobs" ("State", "Priority", "CreatedAt");
CREATE INDEX "IX_BackgroundJobs_JobType_State" 
    ON "BackgroundJobs" ("JobType", "State");
CREATE INDEX "IX_BackgroundJobs_ScheduledAt" 
    ON "BackgroundJobs" ("ScheduledAt") 
    WHERE "ScheduledAt" IS NOT NULL;

-- Recreate SystemLogs with correct structure
CREATE TABLE "SystemLogs" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NULL,
    "Severity" character varying(50) NOT NULL DEFAULT 'Info',
    "Category" character varying(100) NOT NULL,
    "Message" character varying(1000) NOT NULL,
    "DetailsJson" jsonb NULL,
    "UserId" uuid NULL,
    "EntityType" character varying(100) NULL,
    "EntityId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SystemLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SystemLogs_Companies_CompanyId" 
        FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_SystemLogs_CompanyId_Category_CreatedAt" 
    ON "SystemLogs" ("CompanyId", "Category", "CreatedAt");
CREATE INDEX "IX_SystemLogs_Severity_CreatedAt" 
    ON "SystemLogs" ("Severity", "CreatedAt");
CREATE INDEX "IX_SystemLogs_EntityType_EntityId_CreatedAt" 
    ON "SystemLogs" ("EntityType", "EntityId", "CreatedAt");

COMMENT ON TABLE "WorkflowTransitions" IS 'Defines allowed status transitions within a workflow with guard conditions and side effects';
COMMENT ON TABLE "BackgroundJobs" IS 'Generic background job queue for email ingestion, P&L rebuild, etc.';
COMMENT ON TABLE "SystemLogs" IS 'Application-level structured logging';

