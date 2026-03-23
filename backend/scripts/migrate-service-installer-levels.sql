-- Migration Script: Update Service Installer Levels and Types
-- This script migrates existing Service Installers to use the new InstallerLevel enum
-- and ensures proper InstallerType classification

-- Step 1: Update installers with "Subcon" level
-- "Subcon" was a level, but now it's a Type (Subcontractor)
-- Convert "Subcon" level installers to Subcontractor type with Junior level
UPDATE "ServiceInstallers"
SET 
    "SiLevel" = 'Junior',
    "InstallerType" = 'Subcontractor',
    "IsSubcontractor" = true,
    "UpdatedAt" = NOW()
WHERE 
    "SiLevel" = 'Subcon' 
    AND "IsDeleted" = false;

-- Step 2: Normalize other level values
-- Ensure all levels are either 'Junior' or 'Senior'
-- Convert any invalid values to 'Junior' as default
UPDATE "ServiceInstallers"
SET 
    "SiLevel" = CASE 
        WHEN "SiLevel" IN ('Junior', 'Senior') THEN "SiLevel"
        ELSE 'Junior'
    END,
    "UpdatedAt" = NOW()
WHERE 
    "SiLevel" NOT IN ('Junior', 'Senior')
    AND "IsDeleted" = false;

-- Step 3: Sync InstallerType with IsSubcontractor for backward compatibility
-- If IsSubcontractor is true but InstallerType is not Subcontractor, update it
UPDATE "ServiceInstallers"
SET 
    "InstallerType" = 'Subcontractor',
    "UpdatedAt" = NOW()
WHERE 
    "IsSubcontractor" = true 
    AND "InstallerType" != 'Subcontractor'
    AND "IsDeleted" = false;

-- Step 4: Sync IsSubcontractor with InstallerType
-- If InstallerType is Subcontractor but IsSubcontractor is false, update it
UPDATE "ServiceInstallers"
SET 
    "IsSubcontractor" = true,
    "UpdatedAt" = NOW()
WHERE 
    "InstallerType" = 'Subcontractor' 
    AND "IsSubcontractor" = false
    AND "IsDeleted" = false;

-- Step 5: Ensure In-House installers have IsSubcontractor = false
UPDATE "ServiceInstallers"
SET 
    "IsSubcontractor" = false,
    "UpdatedAt" = NOW()
WHERE 
    ("InstallerType" = 'InHouse' OR "InstallerType" IS NULL)
    AND "IsSubcontractor" = true
    AND "IsDeleted" = false;

-- Step 6: Set default InstallerType for any NULL values
UPDATE "ServiceInstallers"
SET 
    "InstallerType" = CASE 
        WHEN "IsSubcontractor" = true THEN 'Subcontractor'
        ELSE 'InHouse'
    END,
    "UpdatedAt" = NOW()
WHERE 
    "InstallerType" IS NULL
    AND "IsDeleted" = false;

-- Step 7: Set default SiLevel for any NULL or empty values
UPDATE "ServiceInstallers"
SET 
    "SiLevel" = 'Junior',
    "UpdatedAt" = NOW()
WHERE 
    ("SiLevel" IS NULL OR "SiLevel" = '')
    AND "IsDeleted" = false;

-- Verification queries (run these to check results)
-- SELECT 
--     "InstallerType",
--     "SiLevel",
--     "IsSubcontractor",
--     COUNT(*) as count
-- FROM "ServiceInstallers"
-- WHERE "IsDeleted" = false
-- GROUP BY "InstallerType", "SiLevel", "IsSubcontractor"
-- ORDER BY "InstallerType", "SiLevel";

-- SELECT 
--     COUNT(*) as total_installers,
--     COUNT(CASE WHEN "InstallerType" = 'InHouse' THEN 1 END) as inhouse_count,
--     COUNT(CASE WHEN "InstallerType" = 'Subcontractor' THEN 1 END) as subcontractor_count,
--     COUNT(CASE WHEN "SiLevel" = 'Junior' THEN 1 END) as junior_count,
--     COUNT(CASE WHEN "SiLevel" = 'Senior' THEN 1 END) as senior_count
-- FROM "ServiceInstallers"
-- WHERE "IsDeleted" = false;

