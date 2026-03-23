-- Fix Phase 5 Tables - Drop and recreate with correct structure
-- This script fixes MaterialTemplates and KpiProfiles tables that have incorrect column names

-- Drop existing tables with wrong structure
DROP TABLE IF EXISTS "MaterialTemplateItems" CASCADE;
DROP TABLE IF EXISTS "MaterialTemplates" CASCADE;
DROP TABLE IF EXISTS "KpiProfiles" CASCADE;

-- Recreate MaterialTemplates with correct structure
CREATE TABLE "MaterialTemplates" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(250) NOT NULL,
    "OrderType" character varying(100) NOT NULL,
    "BuildingTypeId" uuid NULL,
    "PartnerId" uuid NULL,
    "IsDefault" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MaterialTemplates" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_MaterialTemplates_CompanyId_OrderType_BuildingTypeId_PartnerId" 
    ON "MaterialTemplates" ("CompanyId", "OrderType", "BuildingTypeId", "PartnerId");
CREATE INDEX "IX_MaterialTemplates_CompanyId_IsDefault_OrderType" 
    ON "MaterialTemplates" ("CompanyId", "IsDefault", "OrderType");
CREATE INDEX "IX_MaterialTemplates_CompanyId_IsActive" 
    ON "MaterialTemplates" ("CompanyId", "IsActive");

-- Recreate MaterialTemplateItems
CREATE TABLE "MaterialTemplateItems" (
    "Id" uuid NOT NULL,
    "MaterialTemplateId" uuid NOT NULL,
    "MaterialId" uuid NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "UnitOfMeasure" character varying(50) NOT NULL,
    "IsSerialised" boolean NOT NULL,
    "Notes" character varying(500) NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_MaterialTemplateItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_MaterialTemplateItems_MaterialTemplates_MaterialTemplateId" 
        FOREIGN KEY ("MaterialTemplateId") REFERENCES "MaterialTemplates" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_MaterialTemplateItems_Materials_MaterialId" 
        FOREIGN KEY ("MaterialId") REFERENCES "Materials" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_MaterialTemplateItems_MaterialTemplateId" 
    ON "MaterialTemplateItems" ("MaterialTemplateId");
CREATE INDEX "IX_MaterialTemplateItems_MaterialId" 
    ON "MaterialTemplateItems" ("MaterialId");

-- Recreate KpiProfiles with correct structure
CREATE TABLE "KpiProfiles" (
    "Id" uuid NOT NULL,
    "CompanyId" uuid NOT NULL,
    "Name" character varying(250) NOT NULL,
    "PartnerId" uuid NULL,
    "OrderType" character varying(100) NOT NULL,
    "BuildingTypeId" uuid NULL,
    "MaxJobDurationMinutes" integer NOT NULL,
    "DocketKpiMinutes" integer NOT NULL,
    "MaxReschedulesAllowed" integer NULL,
    "IsDefault" boolean NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveTo" timestamp with time zone NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedByUserId" uuid NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_KpiProfiles" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_KpiProfiles_CompanyId_PartnerId_OrderType_BuildingTypeId_EffectiveFrom" 
    ON "KpiProfiles" ("CompanyId", "PartnerId", "OrderType", "BuildingTypeId", "EffectiveFrom");
CREATE INDEX "IX_KpiProfiles_CompanyId_IsDefault_OrderType" 
    ON "KpiProfiles" ("CompanyId", "IsDefault", "OrderType") WHERE "IsDefault" = TRUE;
-- Note: KpiProfile doesn't have IsActive column, using EffectiveFrom/EffectiveTo for active status

