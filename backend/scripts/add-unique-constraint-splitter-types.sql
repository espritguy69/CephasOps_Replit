-- Add Unique Constraint to Prevent Duplicate SplitterTypes
-- This ensures that Code + CompanyId combination is unique

-- STEP 1: First, remove any existing duplicates (keep the oldest one)
-- This handles the case where CompanyId might be NULL

-- For records with NULL CompanyId, treat them as the same company
WITH duplicates AS (
    SELECT 
        "Id",
        "Code",
        COALESCE("CompanyId", '00000000-0000-0000-0000-000000000000'::uuid) as normalized_company_id,
        ROW_NUMBER() OVER (
            PARTITION BY "Code", COALESCE("CompanyId", '00000000-0000-0000-0000-000000000000'::uuid)
            ORDER BY "CreatedAt" ASC, "Id" ASC
        ) as row_num
    FROM "SplitterTypes"
    WHERE "IsDeleted" = false
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

-- STEP 2: Create unique index on Code + CompanyId
-- This will prevent future duplicates at the database level

-- First, drop the existing non-unique index if it exists
DROP INDEX IF EXISTS "IX_SplitterTypes_CompanyId_Code";

-- Create unique index (handles NULL CompanyId by using COALESCE)
CREATE UNIQUE INDEX "IX_SplitterTypes_CompanyId_Code_Unique" 
ON "SplitterTypes" ("Code", COALESCE("CompanyId", '00000000-0000-0000-0000-000000000000'::uuid))
WHERE "IsDeleted" = false;

-- STEP 3: Verify the constraint works
-- Try to insert a duplicate (should fail)
-- This is just for testing - comment out in production
/*
INSERT INTO "SplitterTypes" (
    "Id", "CompanyId", "DepartmentId", "Name", "Code", "TotalPorts", 
    "StandbyPortNumber", "Description", "IsActive", "DisplayOrder", 
    "CreatedAt", "UpdatedAt", "IsDeleted"
) VALUES (
    gen_random_uuid(), NULL, NULL, '1:8', '1_8', 8, NULL, 
    'Test duplicate', true, 1, NOW(), NOW(), false
);
-- Should fail with: duplicate key value violates unique constraint
*/

