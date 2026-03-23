-- Migration: Add Phase 2 Settings Entities
-- Description: Creates database tables for Approval Workflows, Business Hours, Public Holidays, and Escalation Rules
-- Date: 2025-01-XX

-- ============================================
-- APPROVAL WORKFLOWS
-- ============================================

-- Approval Workflows table
CREATE TABLE IF NOT EXISTS "approval_workflows" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "WorkflowType" character varying(50) NOT NULL,
    "EntityType" character varying(50) NOT NULL DEFAULT 'Order',
    "PartnerId" uuid NULL,
    "DepartmentId" uuid NULL,
    "OrderType" character varying(100) NULL,
    "MinValueThreshold" numeric(18,2) NULL,
    "RequireAllSteps" boolean NOT NULL DEFAULT true,
    "AllowParallelApproval" boolean NOT NULL DEFAULT false,
    "TimeoutMinutes" integer NULL,
    "AutoApproveOnTimeout" boolean NOT NULL DEFAULT false,
    "EscalationRole" character varying(100) NULL,
    "EscalationUserId" uuid NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_approval_workflows" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_approval_workflows_CompanyId" ON "approval_workflows" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_approval_workflows_WorkflowType" ON "approval_workflows" ("WorkflowType");
CREATE INDEX IF NOT EXISTS "IX_approval_workflows_EntityType" ON "approval_workflows" ("EntityType");
CREATE INDEX IF NOT EXISTS "IX_approval_workflows_CompanyId_WorkflowType_IsDefault" ON "approval_workflows" ("CompanyId", "WorkflowType", "IsDefault");
CREATE INDEX IF NOT EXISTS "IX_approval_workflows_CompanyId_IsActive_EffectiveFrom_EffectiveTo" ON "approval_workflows" ("CompanyId", "IsActive", "EffectiveFrom", "EffectiveTo");

-- Approval Steps table
CREATE TABLE IF NOT EXISTS "approval_steps" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "ApprovalWorkflowId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "StepOrder" integer NOT NULL,
    "ApprovalType" character varying(50) NOT NULL,
    "TargetUserId" uuid NULL,
    "TargetRole" character varying(100) NULL,
    "TargetTeamId" uuid NULL,
    "ExternalSource" character varying(100) NULL,
    "IsRequired" boolean NOT NULL DEFAULT true,
    "CanSkipIfPreviousApproved" boolean NOT NULL DEFAULT false,
    "TimeoutMinutes" integer NULL,
    "AutoApproveOnTimeout" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_approval_steps" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_approval_steps_approval_workflows_ApprovalWorkflowId" FOREIGN KEY ("ApprovalWorkflowId") REFERENCES "approval_workflows" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_approval_steps_ApprovalWorkflowId" ON "approval_steps" ("ApprovalWorkflowId");
CREATE INDEX IF NOT EXISTS "IX_approval_steps_CompanyId" ON "approval_steps" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_approval_steps_ApprovalWorkflowId_StepOrder" ON "approval_steps" ("ApprovalWorkflowId", "StepOrder");

-- ============================================
-- BUSINESS HOURS & PUBLIC HOLIDAYS
-- ============================================

-- Business Hours table
CREATE TABLE IF NOT EXISTS "business_hours" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "DepartmentId" uuid NULL,
    "Timezone" character varying(100) NOT NULL DEFAULT 'Asia/Kuala_Lumpur',
    "MondayStart" character varying(5) NULL,
    "MondayEnd" character varying(5) NULL,
    "TuesdayStart" character varying(5) NULL,
    "TuesdayEnd" character varying(5) NULL,
    "WednesdayStart" character varying(5) NULL,
    "WednesdayEnd" character varying(5) NULL,
    "ThursdayStart" character varying(5) NULL,
    "ThursdayEnd" character varying(5) NULL,
    "FridayStart" character varying(5) NULL,
    "FridayEnd" character varying(5) NULL,
    "SaturdayStart" character varying(5) NULL,
    "SaturdayEnd" character varying(5) NULL,
    "SundayStart" character varying(5) NULL,
    "SundayEnd" character varying(5) NULL,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_business_hours" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_business_hours_CompanyId" ON "business_hours" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_business_hours_DepartmentId" ON "business_hours" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_business_hours_CompanyId_IsDefault" ON "business_hours" ("CompanyId", "IsDefault");
CREATE INDEX IF NOT EXISTS "IX_business_hours_CompanyId_IsActive_EffectiveFrom_EffectiveTo" ON "business_hours" ("CompanyId", "IsActive", "EffectiveFrom", "EffectiveTo");

-- Public Holidays table
CREATE TABLE IF NOT EXISTS "public_holidays" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "HolidayDate" timestamp with time zone NOT NULL,
    "HolidayType" character varying(50) NOT NULL DEFAULT 'National',
    "State" character varying(100) NULL,
    "IsRecurring" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "Description" character varying(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_public_holidays" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_public_holidays_CompanyId" ON "public_holidays" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_public_holidays_HolidayDate" ON "public_holidays" ("HolidayDate");
CREATE INDEX IF NOT EXISTS "IX_public_holidays_HolidayType" ON "public_holidays" ("HolidayType");
CREATE INDEX IF NOT EXISTS "IX_public_holidays_State" ON "public_holidays" ("State");
CREATE INDEX IF NOT EXISTS "IX_public_holidays_CompanyId_HolidayDate" ON "public_holidays" ("CompanyId", "HolidayDate");
CREATE INDEX IF NOT EXISTS "IX_public_holidays_CompanyId_IsActive_HolidayDate" ON "public_holidays" ("CompanyId", "IsActive", "HolidayDate");

-- ============================================
-- ESCALATION RULES
-- ============================================

-- Escalation Rules table
CREATE TABLE IF NOT EXISTS "escalation_rules" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "EntityType" character varying(50) NOT NULL DEFAULT 'Order',
    "PartnerId" uuid NULL,
    "DepartmentId" uuid NULL,
    "OrderType" character varying(100) NULL,
    "TriggerType" character varying(50) NOT NULL,
    "TriggerStatus" character varying(50) NULL,
    "TriggerDelayMinutes" integer NULL,
    "TriggerConditionsJson" character varying(4000) NULL,
    "EscalationType" character varying(50) NOT NULL,
    "TargetUserId" uuid NULL,
    "TargetRole" character varying(100) NULL,
    "TargetTeamId" uuid NULL,
    "TargetStatus" character varying(50) NULL,
    "NotificationTemplateId" uuid NULL,
    "EscalationMessage" character varying(1000) NULL,
    "ContinueEscalation" boolean NOT NULL DEFAULT false,
    "NextEscalationRuleId" uuid NULL,
    "NextEscalationDelayMinutes" integer NULL,
    "Priority" integer NOT NULL DEFAULT 100,
    "IsActive" boolean NOT NULL DEFAULT true,
    "StopOnMatch" boolean NOT NULL DEFAULT false,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_escalation_rules" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_escalation_rules_CompanyId" ON "escalation_rules" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_EntityType" ON "escalation_rules" ("EntityType");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_TriggerType" ON "escalation_rules" ("TriggerType");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_EscalationType" ON "escalation_rules" ("EscalationType");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_Priority" ON "escalation_rules" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_CompanyId_EntityType_TriggerType_Priority" ON "escalation_rules" ("CompanyId", "EntityType", "TriggerType", "Priority");
CREATE INDEX IF NOT EXISTS "IX_escalation_rules_CompanyId_IsActive_EffectiveFrom_EffectiveTo" ON "escalation_rules" ("CompanyId", "IsActive", "EffectiveFrom", "EffectiveTo");

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE "approval_workflows" IS 'Multi-step approval workflows for critical actions (RMA, reschedules, invoices)';
COMMENT ON TABLE "approval_steps" IS 'Individual steps within an approval workflow';
COMMENT ON TABLE "business_hours" IS 'Operating hours configuration for companies/departments';
COMMENT ON TABLE "public_holidays" IS 'Public holidays for SLA calculations';
COMMENT ON TABLE "escalation_rules" IS 'Auto-escalation rules based on time, status, or conditions';

