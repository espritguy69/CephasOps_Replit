-- Migration: Add InstallerType to ServiceInstallers
-- Date: 2026-01-05
-- Description: Adds InstallerType enum field and migrates data from IsSubcontractor

-- Step 1: Add InstallerType column (nullable initially)
ALTER TABLE "ServiceInstallers" 
ADD COLUMN IF NOT EXISTS "InstallerType" VARCHAR(50) NULL;

-- Step 2: Migrate existing data from IsSubcontractor to InstallerType
UPDATE "ServiceInstallers" 
SET "InstallerType" = CASE 
    WHEN "IsSubcontractor" = true THEN 'Subcontractor' 
    ELSE 'InHouse' 
END
WHERE "InstallerType" IS NULL;

-- Step 3: Make InstallerType NOT NULL with default
ALTER TABLE "ServiceInstallers" 
ALTER COLUMN "InstallerType" SET NOT NULL,
ALTER COLUMN "InstallerType" SET DEFAULT 'InHouse';

-- Step 4: Add check constraint to ensure valid values
ALTER TABLE "ServiceInstallers"
ADD CONSTRAINT "CK_ServiceInstallers_InstallerType" 
CHECK ("InstallerType" IN ('InHouse', 'Subcontractor'));

-- Verification Query (run separately to verify)
-- SELECT 
--     "Name",
--     "IsSubcontractor",
--     "InstallerType",
--     CASE 
--         WHEN ("IsSubcontractor" = true AND "InstallerType" = 'Subcontractor') OR 
--              ("IsSubcontractor" = false AND "InstallerType" = 'InHouse') 
--         THEN 'OK' 
--         ELSE 'MISMATCH' 
--     END as "Status"
-- FROM "ServiceInstallers"
-- ORDER BY "Name";

