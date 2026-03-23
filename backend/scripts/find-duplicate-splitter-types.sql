-- Find duplicate SplitterTypes
-- This script identifies duplicate entries based on Code and CompanyId

-- 1. Find duplicates by Code (regardless of CompanyId)
SELECT 
    "Code",
    "Name",
    COUNT(*) as duplicate_count,
    array_agg("Id"::text) as ids,
    array_agg("CompanyId"::text) as company_ids,
    array_agg("DepartmentId"::text) as department_ids,
    array_agg("IsActive"::text) as is_active_flags,
    array_agg("IsDeleted"::text) as is_deleted_flags,
    array_agg("CreatedAt"::text) as created_dates
FROM "SplitterTypes"
WHERE "Code" = '1_8'  -- Focus on 1:8 duplicate
GROUP BY "Code", "Name"
HAVING COUNT(*) > 1;

-- 2. Show all 1:8 splitter types with full details
SELECT 
    "Id",
    "CompanyId",
    "DepartmentId",
    "Name",
    "Code",
    "TotalPorts",
    "StandbyPortNumber",
    "Description",
    "IsActive",
    "IsDeleted",
    "DisplayOrder",
    "CreatedAt",
    "UpdatedAt",
    "DeletedAt"
FROM "SplitterTypes"
WHERE "Code" = '1_8'
ORDER BY "CreatedAt" ASC;

-- 3. Check for duplicates by Code + CompanyId combination
SELECT 
    "Code",
    "CompanyId",
    COUNT(*) as duplicate_count,
    array_agg("Id"::text) as ids
FROM "SplitterTypes"
WHERE "IsDeleted" = false
GROUP BY "Code", "CompanyId"
HAVING COUNT(*) > 1;

