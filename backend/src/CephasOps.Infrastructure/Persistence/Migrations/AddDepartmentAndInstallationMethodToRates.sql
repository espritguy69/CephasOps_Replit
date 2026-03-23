-- Migration: Add DepartmentId and InstallationMethodId to Rate Tables
-- Date: 2024-11-27
-- Description: Updates SiRatePlans and BillingRatecards with Department and InstallationMethod dimensions

-- ============================================
-- Update SiRatePlans table
-- ============================================
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "DepartmentId" UUID NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "InstallationMethodId" UUID NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "RateType" VARCHAR(50) NOT NULL DEFAULT 'Junior';
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "PrelaidRate" DECIMAL(18,2) NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "NonPrelaidRate" DECIMAL(18,2) NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "AssuranceRepullRate" DECIMAL(18,2) NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "EffectiveFrom" TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE "SiRatePlans" ADD COLUMN IF NOT EXISTS "EffectiveTo" TIMESTAMP WITH TIME ZONE NULL;

-- Create indexes for SiRatePlans
CREATE INDEX IF NOT EXISTS "IX_SiRatePlans_DepartmentId" ON "SiRatePlans" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_SiRatePlans_InstallationMethodId" ON "SiRatePlans" ("InstallationMethodId");
CREATE INDEX IF NOT EXISTS "IX_SiRatePlans_CompanyId_DepartmentId" ON "SiRatePlans" ("CompanyId", "DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_SiRatePlans_ServiceInstallerId_DepartmentId" ON "SiRatePlans" ("ServiceInstallerId", "DepartmentId");

-- ============================================
-- Update BillingRatecards table
-- ============================================
ALTER TABLE "BillingRatecards" ADD COLUMN IF NOT EXISTS "DepartmentId" UUID NULL;
ALTER TABLE "BillingRatecards" ADD COLUMN IF NOT EXISTS "ServiceCategory" VARCHAR(50) NULL;
ALTER TABLE "BillingRatecards" ADD COLUMN IF NOT EXISTS "InstallationMethodId" UUID NULL;

-- Make OrderTypeId nullable (it may not always be set)
ALTER TABLE "BillingRatecards" ALTER COLUMN "OrderTypeId" DROP NOT NULL;

-- Create indexes for BillingRatecards
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_DepartmentId" ON "BillingRatecards" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_ServiceCategory" ON "BillingRatecards" ("ServiceCategory");
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_InstallationMethodId" ON "BillingRatecards" ("InstallationMethodId");
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_CompanyId_DepartmentId" ON "BillingRatecards" ("CompanyId", "DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_BillingRatecards_PartnerId_ServiceCategory" ON "BillingRatecards" ("PartnerId", "ServiceCategory");

-- ============================================
-- Update MaterialTemplates table
-- ============================================
ALTER TABLE "MaterialTemplates" ADD COLUMN IF NOT EXISTS "DepartmentId" UUID NULL;
ALTER TABLE "MaterialTemplates" ADD COLUMN IF NOT EXISTS "InstallationMethodId" UUID NULL;

-- Create indexes for MaterialTemplates
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_DepartmentId" ON "MaterialTemplates" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_InstallationMethodId" ON "MaterialTemplates" ("InstallationMethodId");
CREATE INDEX IF NOT EXISTS "IX_MaterialTemplates_CompanyId_DepartmentId" ON "MaterialTemplates" ("CompanyId", "DepartmentId");

-- ============================================
-- Update InstallationMethods table
-- ============================================
ALTER TABLE "InstallationMethods" ADD COLUMN IF NOT EXISTS "DepartmentId" UUID NULL;

-- Create index
CREATE INDEX IF NOT EXISTS "IX_InstallationMethods_DepartmentId" ON "InstallationMethods" ("DepartmentId");

-- ============================================
-- Add unique constraint to prevent duplicate rates
-- ============================================
-- Prevent duplicate SI rates for same installer + department + installation method
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SiRatePlans_Unique_Context" 
ON "SiRatePlans" ("CompanyId", COALESCE("DepartmentId", '00000000-0000-0000-0000-000000000000'), "ServiceInstallerId", COALESCE("InstallationMethodId", '00000000-0000-0000-0000-000000000000'))
WHERE "IsActive" = TRUE;

-- Prevent duplicate billing rates for same context
CREATE UNIQUE INDEX IF NOT EXISTS "IX_BillingRatecards_Unique_Context" 
ON "BillingRatecards" ("CompanyId", COALESCE("DepartmentId", '00000000-0000-0000-0000-000000000000'), "PartnerId", COALESCE("ServiceCategory", ''), COALESCE("InstallationMethodId", '00000000-0000-0000-0000-000000000000'), COALESCE("OrderTypeId", '00000000-0000-0000-0000-000000000000'))
WHERE "IsActive" = TRUE;

-- Prevent duplicate material templates
CREATE UNIQUE INDEX IF NOT EXISTS "IX_MaterialTemplates_Unique_Context" 
ON "MaterialTemplates" ("CompanyId", COALESCE("DepartmentId", '00000000-0000-0000-0000-000000000000'), COALESCE("OrderType", ''), COALESCE("InstallationMethodId", '00000000-0000-0000-0000-000000000000'), COALESCE("PartnerId", '00000000-0000-0000-0000-000000000000'))
WHERE "IsActive" = TRUE;

