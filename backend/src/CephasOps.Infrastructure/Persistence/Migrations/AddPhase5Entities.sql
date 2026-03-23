-- Migration: Add Phase 5 Entities (Settings: Global Settings, Material Templates, Document Templates, KPI Profiles)
-- Description: Creates tables for Settings modules
-- Date: 2025-01-XX

-- ============================================
-- GLOBAL SETTINGS TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "GlobalSettings" (
    "Id" uuid NOT NULL,
    "Key" character varying(200) NOT NULL,
    "Value" character varying(5000) NOT NULL,
    "ValueType" character varying(50) NOT NULL,
    "Description" character varying(1000) NULL,
    "Module" character varying(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_GlobalSettings" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_GlobalSettings_Key" ON "GlobalSettings" ("Key");
CREATE INDEX IF NOT EXISTS "IX_GlobalSettings_Module" ON "GlobalSettings" ("Module");

-- ============================================
-- MATERIAL TEMPLATES TABLES
-- ============================================

CREATE TABLE IF NOT EXISTS "MaterialTemplates" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "OrderType" character varying(100) NOT NULL,
    "BuildingTypeId" uuid NULL,
    "PartnerId" uuid NULL,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_MaterialTemplates" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_CompanyId_OrderType_BuildingTypeId_PartnerId" 
    ON "MaterialTemplates" ("CompanyId", "OrderType", "BuildingTypeId", "PartnerId");
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_CompanyId_IsDefault_OrderType" 
    ON "MaterialTemplates" ("CompanyId", "IsDefault", "OrderType");
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_CompanyId_IsActive" 
    ON "MaterialTemplates" ("CompanyId", "IsActive");

CREATE TABLE IF NOT EXISTS "MaterialTemplateItems" (
    "Id" uuid NOT NULL,
    "MaterialTemplateId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "Quantity" numeric(18,2) NOT NULL,
    "UnitOfMeasure" character varying(50) NOT NULL,
    "IsSerialised" boolean NOT NULL,
    "Notes" character varying(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MaterialTemplateItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MaterialTemplateItems_MaterialTemplates_MaterialTemplateId" 
        FOREIGN KEY ("MaterialTemplateId") REFERENCES "MaterialTemplates" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_MaterialTemplateItems_MaterialTemplateId" 
    ON "MaterialTemplateItems" ("MaterialTemplateId");
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplateItems_MaterialId" 
    ON "MaterialTemplateItems" ("MaterialId");

-- ============================================
-- DOCUMENT TEMPLATES TABLES
-- ============================================

CREATE TABLE IF NOT EXISTS "DocumentTemplates" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "DocumentType" character varying(100) NOT NULL,
    "PartnerId" uuid NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "Engine" character varying(50) NOT NULL,
    "HtmlBody" text NOT NULL,
    "JsonSchema" text NULL,
    "Version" integer NOT NULL DEFAULT 1,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_DocumentTemplates" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_DocumentTemplates_CompanyId_DocumentType_PartnerId_IsActive" 
    ON "DocumentTemplates" ("CompanyId", "DocumentType", "PartnerId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_DocumentTemplates_CompanyId_IsActive" 
    ON "DocumentTemplates" ("CompanyId", "IsActive");

CREATE TABLE IF NOT EXISTS "GeneratedDocuments" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "DocumentType" character varying(100) NOT NULL,
    "ReferenceEntity" character varying(100) NOT NULL,
    "ReferenceId" uuid NOT NULL,
    "TemplateId" uuid NOT NULL,
    "FileId" uuid NOT NULL,
    "Format" character varying(50) NOT NULL,
    "GeneratedAt" timestamp with time zone NOT NULL,
    "GeneratedByUserId" uuid NULL,
    "MetadataJson" character varying(2000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_GeneratedDocuments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GeneratedDocuments_DocumentTemplates_TemplateId" 
        FOREIGN KEY ("TemplateId") REFERENCES "DocumentTemplates" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_GeneratedDocuments_Files_FileId" 
        FOREIGN KEY ("FileId") REFERENCES "Files" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_GeneratedDocuments_CompanyId_ReferenceEntity_ReferenceId" 
    ON "GeneratedDocuments" ("CompanyId", "ReferenceEntity", "ReferenceId");
CREATE INDEX IF NOT EXISTS "IX_GeneratedDocuments_CompanyId_DocumentType_GeneratedAt" 
    ON "GeneratedDocuments" ("CompanyId", "DocumentType", "GeneratedAt");
CREATE INDEX IF NOT EXISTS "IX_GeneratedDocuments_TemplateId" 
    ON "GeneratedDocuments" ("TemplateId");
CREATE INDEX IF NOT EXISTS "IX_GeneratedDocuments_FileId" 
    ON "GeneratedDocuments" ("FileId");

CREATE TABLE IF NOT EXISTS "DocumentPlaceholderDefinitions" (
    "Id" uuid NOT NULL,
    "DocumentType" character varying(100) NOT NULL,
    "Key" character varying(200) NOT NULL,
    "Description" character varying(500) NOT NULL,
    "ExampleValue" character varying(500) NULL,
    "IsRequired" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DocumentPlaceholderDefinitions" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentPlaceholderDefinitions_DocumentType_Key" 
    ON "DocumentPlaceholderDefinitions" ("DocumentType", "Key");
CREATE INDEX IF NOT EXISTS "IX_DocumentPlaceholderDefinitions_DocumentType" 
    ON "DocumentPlaceholderDefinitions" ("DocumentType");

-- ============================================
-- KPI PROFILES TABLE
-- ============================================

CREATE TABLE IF NOT EXISTS "KpiProfiles" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "PartnerId" uuid NULL,
    "OrderType" character varying(100) NOT NULL,
    "BuildingTypeId" uuid NULL,
    "MaxJobDurationMinutes" integer NOT NULL,
    "DocketKpiMinutes" integer NOT NULL,
    "MaxReschedulesAllowed" integer NULL,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "EffectiveFrom" timestamp with time zone NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CreatedByUserId" uuid NULL,
    "UpdatedByUserId" uuid NULL,
    CONSTRAINT "PK_KpiProfiles" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_KpiProfiles_CompanyId_PartnerId_OrderType_BuildingTypeId_EffectiveFrom" 
    ON "KpiProfiles" ("CompanyId", "PartnerId", "OrderType", "BuildingTypeId", "EffectiveFrom");
CREATE INDEX IF NOT EXISTS "IX_KpiProfiles_CompanyId_IsDefault_OrderType" 
    ON "KpiProfiles" ("CompanyId", "IsDefault", "OrderType");
CREATE INDEX IF NOT EXISTS "IX_KpiProfiles_CompanyId_IsActive" 
    ON "KpiProfiles" ("CompanyId", "IsDefault");

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE "GlobalSettings" IS 'System-wide configuration values';
COMMENT ON TABLE "MaterialTemplates" IS 'Material kit templates per order type/building/partner';
COMMENT ON TABLE "MaterialTemplateItems" IS 'Items within a material template';
COMMENT ON TABLE "DocumentTemplates" IS 'Reusable document templates for PDF generation';
COMMENT ON TABLE "GeneratedDocuments" IS 'Rendered document instances';
COMMENT ON TABLE "DocumentPlaceholderDefinitions" IS 'Placeholder variable catalog for templates';
COMMENT ON TABLE "KpiProfiles" IS 'Configurable KPI rules for scheduler and payroll';

