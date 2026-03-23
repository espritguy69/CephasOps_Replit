-- Migration: Add Phase 6 Workflow Engine Entities
-- Description: Creates tables for Workflow Engine module (WorkflowDefinitions, WorkflowTransitions, WorkflowJobs, BackgroundJobs, SystemLogs)
-- Date: 2025-01-XX

-- ============================================
-- WORKFLOW DEFINITIONS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "WorkflowDefinitions" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(250) NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "Description" character varying(1000) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "PartnerId" uuid NULL,
    "CreatedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_WorkflowDefinitions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkflowDefinitions_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WorkflowDefinitions_Partners_PartnerId" FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_WorkflowDefinitions_CompanyId_EntityType_IsActive" 
    ON "WorkflowDefinitions" ("CompanyId", "EntityType", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_WorkflowDefinitions_CompanyId_Name" 
    ON "WorkflowDefinitions" ("CompanyId", "Name");

-- ============================================
-- WORKFLOW TRANSITIONS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "WorkflowTransitions" (
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

CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_FromStatus_ToStatus" 
    ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "FromStatus", "ToStatus");
CREATE INDEX IF NOT EXISTS "IX_WorkflowTransitions_CompanyId_WorkflowDefinitionId_IsActive" 
    ON "WorkflowTransitions" ("CompanyId", "WorkflowDefinitionId", "IsActive");

-- ============================================
-- WORKFLOW JOBS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "WorkflowJobs" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "WorkflowDefinitionId" uuid NOT NULL,
    "EntityType" character varying(100) NOT NULL,
    "EntityId" uuid NOT NULL,
    "CurrentStatus" character varying(100) NOT NULL,
    "TargetStatus" character varying(100) NOT NULL,
    "State" character varying(50) NOT NULL DEFAULT 'Pending',
    "LastError" character varying(2000) NULL,
    "PayloadJson" jsonb NULL,
    "InitiatedByUserId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "StartedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_WorkflowJobs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkflowJobs_WorkflowDefinitions_WorkflowDefinitionId" 
        FOREIGN KEY ("WorkflowDefinitionId") REFERENCES "WorkflowDefinitions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_WorkflowJobs_Companies_CompanyId" 
        FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_WorkflowJobs_CompanyId_EntityType_EntityId" 
    ON "WorkflowJobs" ("CompanyId", "EntityType", "EntityId");
CREATE INDEX IF NOT EXISTS "IX_WorkflowJobs_CompanyId_State_CreatedAt" 
    ON "WorkflowJobs" ("CompanyId", "State", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_WorkflowJobs_WorkflowDefinitionId_EntityId" 
    ON "WorkflowJobs" ("WorkflowDefinitionId", "EntityId");

-- ============================================
-- BACKGROUND JOBS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "BackgroundJobs" (
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

CREATE INDEX IF NOT EXISTS "IX_BackgroundJobs_State_Priority_CreatedAt" 
    ON "BackgroundJobs" ("State", "Priority", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_BackgroundJobs_JobType_State" 
    ON "BackgroundJobs" ("JobType", "State");
CREATE INDEX IF NOT EXISTS "IX_BackgroundJobs_ScheduledAt" 
    ON "BackgroundJobs" ("ScheduledAt") 
    WHERE "ScheduledAt" IS NOT NULL;

-- ============================================
-- SYSTEM LOGS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "SystemLogs" (
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

CREATE INDEX IF NOT EXISTS "IX_SystemLogs_CompanyId_Category_CreatedAt" 
    ON "SystemLogs" ("CompanyId", "Category", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_SystemLogs_Severity_CreatedAt" 
    ON "SystemLogs" ("Severity", "CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_SystemLogs_EntityType_EntityId_CreatedAt" 
    ON "SystemLogs" ("EntityType", "EntityId", "CreatedAt");

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE "WorkflowDefinitions" IS 'Defines workflow configurations for entity types (Order, Invoice, etc.)';
COMMENT ON TABLE "WorkflowTransitions" IS 'Defines allowed status transitions within a workflow with guard conditions and side effects';
COMMENT ON TABLE "WorkflowJobs" IS 'Tracks in-progress workflow executions and transitions';
COMMENT ON TABLE "BackgroundJobs" IS 'Generic background job queue for email ingestion, P&L rebuild, etc.';
COMMENT ON TABLE "SystemLogs" IS 'Application-level structured logging';

