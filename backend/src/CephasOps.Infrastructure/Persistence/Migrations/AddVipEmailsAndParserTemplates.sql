-- Migration: Add VipEmails, VipGroups, and ParserTemplates tables
-- Date: 2025-11-26
-- Description: Adds tables for VIP email management, VIP groups, and parser template configuration

-- VipGroups table (create first as VipEmails references it)
CREATE TABLE IF NOT EXISTS "VipGroups" (
    "Id" UUID NOT NULL,
    "CompanyId" UUID NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "NotifyDepartmentId" UUID NULL,
    "NotifyUserId" UUID NULL,
    "NotifyHodUserId" UUID NULL,
    "NotifyRole" VARCHAR(100) NULL,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedByUserId" UUID NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_VipGroups" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_VipGroups_CompanyId_Code" ON "VipGroups" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_VipGroups_CompanyId_IsActive" ON "VipGroups" ("CompanyId", "IsActive");

-- VipEmails table
CREATE TABLE IF NOT EXISTS "VipEmails" (
    "Id" UUID NOT NULL,
    "CompanyId" UUID NULL,
    "EmailAddress" VARCHAR(320) NOT NULL,
    "DisplayName" VARCHAR(200) NULL,
    "Description" VARCHAR(1000) NULL,
    "VipGroupId" UUID NULL,
    "NotifyUserId" UUID NULL,
    "NotifyRole" VARCHAR(100) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedByUserId" UUID NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_VipEmails" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VipEmails_VipGroups" FOREIGN KEY ("VipGroupId") REFERENCES "VipGroups" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_VipEmails_CompanyId_EmailAddress" ON "VipEmails" ("CompanyId", "EmailAddress");
CREATE INDEX IF NOT EXISTS "IX_VipEmails_CompanyId_IsActive" ON "VipEmails" ("CompanyId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_VipEmails_VipGroupId" ON "VipEmails" ("VipGroupId");

-- ParserTemplates table
CREATE TABLE IF NOT EXISTS "ParserTemplates" (
    "Id" UUID NOT NULL,
    "CompanyId" UUID NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(50) NOT NULL,
    "PartnerPattern" VARCHAR(500) NULL,
    "SubjectPattern" VARCHAR(500) NULL,
    "OrderTypeId" UUID NULL,
    "OrderTypeCode" VARCHAR(50) NULL,
    "AutoApprove" BOOLEAN NOT NULL DEFAULT FALSE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "Description" VARCHAR(1000) NULL,
    "PartnerId" UUID NULL,
    "DefaultDepartmentId" UUID NULL,
    "ExpectedAttachmentTypes" VARCHAR(200) NULL,
    "CreatedByUserId" UUID NOT NULL,
    "UpdatedByUserId" UUID NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ParserTemplates" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ParserTemplates_CompanyId_Code" ON "ParserTemplates" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_ParserTemplates_CompanyId_Priority_IsActive" ON "ParserTemplates" ("CompanyId", "Priority", "IsActive");

-- Seed default VIP Groups
INSERT INTO "VipGroups" ("Id", "Name", "Code", "Description", "Priority", "IsActive", "CreatedByUserId", "CreatedAt", "UpdatedAt")
VALUES 
    ('b1000000-0000-0000-0000-000000000001', 'Procurement VIP', 'PROCUREMENT_VIP', 'TIME Procurement team - high priority contacts', 100, TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('b1000000-0000-0000-0000-000000000002', 'GPON VIP', 'GPON_VIP', 'GPON department VIP contacts', 90, TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('b1000000-0000-0000-0000-000000000003', 'CWO VIP', 'CWO_VIP', 'CWO department VIP contacts', 90, TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('b1000000-0000-0000-0000-000000000004', 'NWO VIP', 'NWO_VIP', 'NWO department VIP contacts - most important', 95, TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Seed sample VIP emails for each group
INSERT INTO "VipEmails" ("Id", "EmailAddress", "DisplayName", "Description", "VipGroupId", "IsActive", "CreatedByUserId", "CreatedAt", "UpdatedAt")
VALUES 
    -- Procurement VIP emails
    ('c1000000-0000-0000-0000-000000000001', 'ian.lee@time.com.my', 'Ian Lee', 'Procurement team member', 'b1000000-0000-0000-0000-000000000001', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('c1000000-0000-0000-0000-000000000002', 'muslihah.muslimin@time.com.my', 'Muslihah Muslimin', 'Procurement team member', 'b1000000-0000-0000-0000-000000000001', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('c1000000-0000-0000-0000-000000000003', 'syaza.iffah@time.com.my', 'Syaza Iffah', 'Procurement team member', 'b1000000-0000-0000-0000-000000000001', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('c1000000-0000-0000-0000-000000000004', 'azizul.hakim@time.com.my', 'Azizul Hakim', 'Procurement team member', 'b1000000-0000-0000-0000-000000000001', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('c1000000-0000-0000-0000-000000000005', 'noor.fadhila@time.com.my', 'Noor Fadhila', 'Procurement team member', 'b1000000-0000-0000-0000-000000000001', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    -- GPON VIP emails
    ('c1000000-0000-0000-0000-000000000006', 'emmanuel.reuben@time.com.my', 'Emmanuel Reuben', 'GPON contact', 'b1000000-0000-0000-0000-000000000002', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    -- CWO VIP emails
    ('c1000000-0000-0000-0000-000000000007', 'michelle.lim@time.com.my', 'Michelle Lim', 'CWO contact', 'b1000000-0000-0000-0000-000000000003', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    -- NWO VIP emails
    ('c1000000-0000-0000-0000-000000000008', 'eagle@time.com.my', 'Eagle', 'NWO contact', 'b1000000-0000-0000-0000-000000000004', TRUE, '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Seed default parser templates (all with AutoApprove = false for initial testing)
-- Note: DefaultDepartmentId should be set to GPON department ID after departments are created
INSERT INTO "ParserTemplates" ("Id", "Name", "Code", "PartnerPattern", "SubjectPattern", "OrderTypeCode", "AutoApprove", "Priority", "IsActive", "Description", "CreatedByUserId", "CreatedAt", "UpdatedAt")
VALUES 
    ('a1000000-0000-0000-0000-000000000001', 'TIME FTTH Activation', 'TIME_FTTH', '*@time.com.my', 'FTTH', 'ACTIVATION', FALSE, 100, TRUE, 'Parse TIME FTTH activation orders from Excel attachments', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000002', 'TIME FTTO Activation', 'TIME_FTTO', '*@time.com.my', 'FTTO', 'ACTIVATION', FALSE, 99, TRUE, 'Parse TIME FTTO activation orders from Excel attachments', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000003', 'TIME-Digi HSBB', 'DIGI_HSBB', '*@digi.com.my', 'HSBB', 'ACTIVATION', FALSE, 98, TRUE, 'Parse Digi HSBB orders (via TIME partnership)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000004', 'TIME-Celcom HSBB', 'CELCOM_HSBB', '*@celcom.com.my', 'HSBB', 'ACTIVATION', FALSE, 97, TRUE, 'Parse Celcom HSBB orders (via TIME partnership)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000005', 'TIME Modification Indoor', 'TIME_MOD_INDOOR', '*@time.com.my', 'Modification Indoor', 'MODIFICATION_INDOOR', FALSE, 96, TRUE, 'Parse TIME indoor modification (relocation within same unit)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000006', 'TIME Modification Outdoor', 'TIME_MOD_OUTDOOR', '*@time.com.my', 'Modification Outdoor', 'MODIFICATION_OUTDOOR', FALSE, 95, TRUE, 'Parse TIME outdoor modification (relocation to different address)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000007', 'TIME Assurance (TTKT)', 'TIME_ASSURANCE', '*@time.com.my', 'TTKT', 'ASSURANCE', FALSE, 94, TRUE, 'Parse TIME assurance/fault repair orders (LOSi/LOBi issues)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    ('a1000000-0000-0000-0000-000000000008', 'Reschedule Approval', 'RESCHEDULE_APPROVAL', '*', 'Re:', 'RESCHEDULE', FALSE, 50, TRUE, 'Parse human reschedule approval emails (unstructured replies)', '00000000-0000-0000-0000-000000000001', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

-- Add VipGroupId column to VipEmails if it doesn't exist (for older schemas)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'VipEmails' AND column_name = 'VipGroupId') THEN
        ALTER TABLE "VipEmails" ADD COLUMN "VipGroupId" UUID NULL;
        ALTER TABLE "VipEmails" ADD CONSTRAINT "FK_VipEmails_VipGroups" 
            FOREIGN KEY ("VipGroupId") REFERENCES "VipGroups" ("Id") ON DELETE SET NULL;
        CREATE INDEX IF NOT EXISTS "IX_VipEmails_VipGroupId" ON "VipEmails" ("VipGroupId");
    END IF;
END $$;

-- Add Description column to VipEmails if it doesn't exist (for older schemas)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'VipEmails' AND column_name = 'Description') THEN
        ALTER TABLE "VipEmails" ADD COLUMN "Description" VARCHAR(1000) NULL;
    END IF;
END $$;
