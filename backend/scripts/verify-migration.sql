-- Verification script for Excel parser technical fields migration
-- Run this to check if migration 20241127_AddParserExcelFields has been applied

-- Check Orders table columns
DO $$
DECLARE
    missing_columns TEXT[] := ARRAY[]::TEXT[];
    col TEXT;
    required_columns TEXT[] := ARRAY['PackageName', 'Bandwidth', 'OnuSerialNumber', 'VoipServiceId', 'OldAddress'];
BEGIN
    FOREACH col IN ARRAY required_columns
    LOOP
        IF NOT EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'Orders' 
            AND column_name = col
        ) THEN
            missing_columns := array_append(missing_columns, col);
        END IF;
    END LOOP;
    
    IF array_length(missing_columns, 1) IS NULL THEN
        RAISE NOTICE '✓ Orders table: All required columns exist';
    ELSE
        RAISE WARNING '✗ Orders table: Missing columns: %', array_to_string(missing_columns, ', ');
    END IF;
END $$;

-- Check ParsedOrderDrafts table columns
DO $$
DECLARE
    missing_columns TEXT[] := ARRAY[]::TEXT[];
    col TEXT;
    required_columns TEXT[] := ARRAY['PackageName', 'Bandwidth', 'OnuSerialNumber', 'VoipServiceId', 'OldAddress', 'Remarks', 'CustomerEmail', 'OrderTypeCode', 'SourceFileName'];
BEGIN
    FOREACH col IN ARRAY required_columns
    LOOP
        IF NOT EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'ParsedOrderDrafts' 
            AND column_name = col
        ) THEN
            missing_columns := array_append(missing_columns, col);
        END IF;
    END LOOP;
    
    IF array_length(missing_columns, 1) IS NULL THEN
        RAISE NOTICE '✓ ParsedOrderDrafts table: All required columns exist';
    ELSE
        RAISE WARNING '✗ ParsedOrderDrafts table: Missing columns: %', array_to_string(missing_columns, ', ');
    END IF;
END $$;

-- Check indexes
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'ParsedOrderDrafts' 
        AND indexname = 'IX_ParsedOrderDrafts_OrderTypeCode'
    ) THEN
        RAISE NOTICE '✓ Index IX_ParsedOrderDrafts_OrderTypeCode exists';
    ELSE
        RAISE WARNING '✗ Index IX_ParsedOrderDrafts_OrderTypeCode missing';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'ParsedOrderDrafts' 
        AND indexname = 'IX_ParsedOrderDrafts_SourceFileName'
    ) THEN
        RAISE NOTICE '✓ Index IX_ParsedOrderDrafts_SourceFileName exists';
    ELSE
        RAISE WARNING '✗ Index IX_ParsedOrderDrafts_SourceFileName missing';
    END IF;
END $$;

-- Summary
SELECT 
    'Migration Verification Complete' AS status,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'Orders' AND column_name = 'PackageName'
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'PackageName'
        ) THEN 'READY'
        ELSE 'MIGRATION NEEDED'
    END AS result;

