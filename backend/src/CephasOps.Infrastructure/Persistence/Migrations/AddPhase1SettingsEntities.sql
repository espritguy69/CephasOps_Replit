-- Migration: Add Phase 1 Settings Entities
-- Description: Creates database tables for SLA Profiles and Automation Rules
-- Date: 2025-01-XX

-- ============================================
-- SLA PROFILES
-- ============================================

-- SLA Profiles table
CREATE TABLE IF NOT EXISTS "SlaProfiles" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "PartnerId" uuid NULL,
    "OrderType" character varying(100) NULL,
    "BuildingTypeId" uuid NULL,
    "IsVip" boolean NOT NULL DEFAULT false,
    "TargetResponseTimeMinutes" integer NOT NULL,
    "TargetResolutionTimeMinutes" integer NOT NULL,
    "EscalationThresholdPercent" integer NOT NULL DEFAULT 80,
    "EscalationNotifyRolesJson" jsonb NOT NULL DEFAULT '[]',
    "EscalationNotifyUsersJson" jsonb NOT NULL DEFAULT '[]',
    "ExcludeNonBusinessHours" boolean NOT NULL DEFAULT true,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_SlaProfiles" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_SlaProfiles_CompanyId" ON "SlaProfiles" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_SlaProfiles_CompanyId_PartnerId_OrderType_BuildingTypeId_IsVip_IsActive_EffectiveFrom_EffectiveTo" ON "SlaProfiles" ("CompanyId", "PartnerId", "OrderType", "BuildingTypeId", "IsVip", "IsActive", "EffectiveFrom", "EffectiveTo");
CREATE INDEX IF NOT EXISTS "IX_SlaProfiles_CompanyId_IsDefault_OrderType" ON "SlaProfiles" ("CompanyId", "IsDefault", "OrderType");

-- ============================================
-- AUTOMATION RULES
-- ============================================

-- Automation Rules table
CREATE TABLE IF NOT EXISTS "automation_rules" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "RuleType" character varying(50) NOT NULL,
    "EntityType" character varying(50) NOT NULL DEFAULT 'Order',
    "PartnerId" uuid NULL,
    "DepartmentId" uuid NULL,
    "OrderType" character varying(100) NULL,
    "TriggerType" character varying(50) NOT NULL,
    "TriggerConditionsJson" character varying(4000) NULL,
    "TriggerStatus" character varying(50) NULL,
    "TriggerDelayMinutes" integer NULL,
    "ActionType" character varying(50) NOT NULL,
    "ActionConfigJson" character varying(4000) NULL,
    "TargetUserId" uuid NULL,
    "TargetRole" character varying(100) NULL,
    "TargetTeamId" uuid NULL,
    "TargetStatus" character varying(50) NULL,
    "NotificationTemplateId" uuid NULL,
    "ConditionsJson" character varying(4000) NULL,
    "Priority" integer NOT NULL DEFAULT 100,
    "IsActive" boolean NOT NULL DEFAULT true,
    "StopOnMatch" boolean NOT NULL DEFAULT false,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_automation_rules" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_automation_rules_CompanyId" ON "automation_rules" ("CompanyId");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_RuleType" ON "automation_rules" ("RuleType");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_EntityType" ON "automation_rules" ("EntityType");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_TriggerType" ON "automation_rules" ("TriggerType");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_ActionType" ON "automation_rules" ("ActionType");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_Priority" ON "automation_rules" ("Priority");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_CompanyId_RuleType_IsActive" ON "automation_rules" ("CompanyId", "RuleType", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_automation_rules_CompanyId_EntityType_TriggerType_Priority" ON "automation_rules" ("CompanyId", "EntityType", "TriggerType", "Priority");

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE "SlaProfiles" IS 'SLA profiles for response and resolution time targets';
COMMENT ON TABLE "automation_rules" IS 'Automation rules for auto-assignment, escalation, and notifications';

