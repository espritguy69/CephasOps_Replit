-- ============================================
-- Fix: Remove duplicate index on BuildingRules.BuildingId
-- ============================================
-- Issue: Two identical unique indexes exist:
--   1. BuildingRules_BuildingId_key (from UNIQUE constraint on column)
--   2. IX_BuildingRules_BuildingId (explicitly created / EF Core managed)
-- 
-- Solution: Drop the UNIQUE constraint (which removes its index) and keep 
--           the EF Core-managed index (IX_BuildingRules_BuildingId)
-- ============================================

-- Step 1: Verify the indexes exist and identify which one backs a constraint
SELECT
    i.indexname,
    i.indexdef,
    CASE 
        WHEN c.conname IS NOT NULL THEN 'CONSTRAINT INDEX (' || c.conname || ')'
        ELSE 'STANDALONE INDEX'
    END AS index_type
FROM pg_indexes i
LEFT JOIN pg_constraint c ON c.conindid = (
    SELECT oid FROM pg_class WHERE relname = i.indexname
)
WHERE i.schemaname = 'public' 
  AND i.tablename = 'buildingrules'
  AND i.indexname IN ('BuildingRules_BuildingId_key', 'IX_BuildingRules_BuildingId')
ORDER BY i.indexname;

-- Step 2: Find the constraint name that uses BuildingRules_BuildingId_key
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    conrelid::regclass AS table_name
FROM pg_constraint
WHERE conrelid = 'public.buildingrules'::regclass
  AND contype = 'u'
  AND conindid = (
      SELECT oid FROM pg_class WHERE relname = 'BuildingRules_BuildingId_key'
  );

-- Step 3: Drop the UNIQUE constraint (this will automatically drop BuildingRules_BuildingId_key index)
-- Replace 'BuildingRules_BuildingId_key' with the actual constraint name from Step 2 if different
DO $$
DECLARE
    constraint_name text;
BEGIN
    -- Find the unique constraint on BuildingId
    SELECT conname INTO constraint_name
    FROM pg_constraint
    WHERE conrelid = 'public.buildingrules'::regclass
      AND contype = 'u'
      AND conindid = (
          SELECT oid FROM pg_class WHERE relname = 'BuildingRules_BuildingId_key'
      );
    
    IF constraint_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE public."BuildingRules" DROP CONSTRAINT IF EXISTS %I', constraint_name);
        RAISE NOTICE 'Dropped constraint: %', constraint_name;
    ELSE
        -- If no constraint found, try dropping the index directly
        DROP INDEX IF EXISTS public."BuildingRules_BuildingId_key";
        RAISE NOTICE 'Dropped index: BuildingRules_BuildingId_key';
    END IF;
END $$;

-- Step 4: Verify only one index remains (IX_BuildingRules_BuildingId)
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public' 
  AND tablename = 'buildingrules'
  AND indexname LIKE '%BuildingId%';

-- Step 5: Reanalyze the table for query planner
ANALYZE public."BuildingRules";

-- Expected result: Only IX_BuildingRules_BuildingId should remain

