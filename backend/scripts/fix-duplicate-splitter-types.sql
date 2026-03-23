-- Fix Duplicate SplitterTypes
-- This script removes duplicate 1:8 splitter types, keeping the oldest one

-- STEP 1: Identify which record to keep
-- Keep the record with the earliest CreatedAt timestamp
-- If timestamps are the same, keep the one with the lower ID

WITH duplicates AS (
    SELECT 
        "Id",
        "Code",
        "Name",
        "CompanyId",
        "DepartmentId",
        "CreatedAt",
        ROW_NUMBER() OVER (
            PARTITION BY "Code", "CompanyId" 
            ORDER BY "CreatedAt" ASC, "Id" ASC
        ) as row_num
    FROM "SplitterTypes"
    WHERE "Code" = '1_8' 
      AND "IsDeleted" = false
)
SELECT 
    "Id",
    "Code",
    "Name",
    "CreatedAt",
    row_num,
    CASE 
        WHEN row_num = 1 THEN 'KEEP'
        ELSE 'DELETE'
    END as action
FROM duplicates
ORDER BY "CreatedAt" ASC, "Id" ASC;

-- STEP 2: Check if any duplicates are referenced by Splitters
-- Before deleting, check if any Splitters reference the duplicate records
SELECT 
    st."Id" as splitter_type_id,
    st."Code",
    st."Name",
    COUNT(s."Id") as splitter_count
FROM "SplitterTypes" st
LEFT JOIN "Splitters" s ON s."SplitterTypeId" = st."Id"
WHERE st."Code" = '1_8' 
  AND st."IsDeleted" = false
GROUP BY st."Id", st."Code", st."Name"
ORDER BY st."CreatedAt" ASC;

-- STEP 3: Delete duplicate records (keep the oldest one)
-- WARNING: Only run this after verifying which records to keep!
-- This will soft-delete duplicates (set IsDeleted = true)
-- Strategy: Keep the record with the earliest CreatedAt, or if same, keep the one with CompanyId (not NULL)

WITH duplicates AS (
    SELECT 
        "Id",
        "Code",
        "CompanyId",
        "CreatedAt",
        ROW_NUMBER() OVER (
            PARTITION BY "Code"
            ORDER BY 
                "CreatedAt" ASC,
                CASE WHEN "CompanyId" IS NOT NULL THEN 0 ELSE 1 END ASC,  -- Prefer non-NULL CompanyId
                "Id" ASC
        ) as row_num
    FROM "SplitterTypes"
    WHERE "Code" = '1_8' 
      AND "IsDeleted" = false
)
UPDATE "SplitterTypes"
SET 
    "IsDeleted" = true,
    "DeletedAt" = NOW(),
    "UpdatedAt" = NOW()
WHERE "Id" IN (
    SELECT "Id" 
    FROM duplicates 
    WHERE row_num > 1
)
RETURNING "Id", "Code", "Name", "CompanyId", "CreatedAt";

-- STEP 4: Verify fix (should return only 1 record)
SELECT 
    "Id",
    "Code",
    "Name",
    "CompanyId",
    "IsDeleted",
    "CreatedAt"
FROM "SplitterTypes"
WHERE "Code" = '1_8'
ORDER BY "CreatedAt" ASC;

