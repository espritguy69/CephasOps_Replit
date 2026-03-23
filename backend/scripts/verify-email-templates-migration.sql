-- Verification script for Email Templates migration
-- Run this to check if migration 20241201_AddEmailTemplates has been applied

-- Check EmailTemplates table exists
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'EmailTemplates'
    ) THEN
        RAISE NOTICE '✓ EmailTemplates table exists';
    ELSE
        RAISE WARNING '✗ EmailTemplates table missing';
    END IF;
END $$;

-- Check EmailMessages has Direction and SentAt columns
DO $$
DECLARE
    missing_columns TEXT[] := ARRAY[]::TEXT[];
    col TEXT;
    required_columns TEXT[] := ARRAY['Direction', 'SentAt'];
BEGIN
    FOREACH col IN ARRAY required_columns
    LOOP
        IF NOT EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'EmailMessages' 
            AND column_name = col
        ) THEN
            missing_columns := array_append(missing_columns, col);
        END IF;
    END LOOP;
    
    IF array_length(missing_columns, 1) IS NULL THEN
        RAISE NOTICE '✓ EmailMessages table: Direction and SentAt columns exist';
    ELSE
        RAISE WARNING '✗ EmailMessages table: Missing columns: %', array_to_string(missing_columns, ', ');
    END IF;
END $$;

-- Check 3 initial templates inserted
DO $$
DECLARE
    template_count INTEGER;
    required_templates TEXT[] := ARRAY['RESCHEDULE_TIME_ONLY', 'RESCHEDULE_DATE_TIME', 'ASSURANCE_CABLE_REPULL'];
    missing_templates TEXT[] := ARRAY[]::TEXT[];
    template_code TEXT;
BEGIN
    SELECT COUNT(*) INTO template_count FROM "EmailTemplates";
    
    IF template_count >= 3 THEN
        RAISE NOTICE '✓ EmailTemplates table: % templates found', template_count;
    ELSE
        RAISE WARNING '✗ EmailTemplates table: Only % templates found (expected at least 3)', template_count;
    END IF;
    
    -- Check each required template
    FOREACH template_code IN ARRAY required_templates
    LOOP
        IF NOT EXISTS (
            SELECT 1 FROM "EmailTemplates" WHERE "Code" = template_code
        ) THEN
            missing_templates := array_append(missing_templates, template_code);
        END IF;
    END LOOP;
    
    IF array_length(missing_templates, 1) IS NULL THEN
        RAISE NOTICE '✓ All 3 required templates found';
    ELSE
        RAISE WARNING '✗ Missing templates: %', array_to_string(missing_templates, ', ');
    END IF;
END $$;

-- Check indexes
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'EmailTemplates' 
        AND indexname = 'IX_EmailTemplates_CompanyId_Code'
    ) THEN
        RAISE NOTICE '✓ Index IX_EmailTemplates_CompanyId_Code exists';
    ELSE
        RAISE WARNING '✗ Index IX_EmailTemplates_CompanyId_Code missing';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE tablename = 'EmailMessages' 
        AND indexname = 'IX_EmailMessages_CompanyId_Direction_ReceivedAt'
    ) THEN
        RAISE NOTICE '✓ Index IX_EmailMessages_CompanyId_Direction_ReceivedAt exists';
    ELSE
        RAISE WARNING '✗ Index IX_EmailMessages_CompanyId_Direction_ReceivedAt missing';
    END IF;
END $$;

-- Display template details
SELECT 
    "Code",
    "Name",
    "IsActive",
    "AutoProcessReplies",
    "Priority",
    "RelatedEntityType"
FROM "EmailTemplates"
ORDER BY "Priority" DESC;

-- Summary
SELECT 
    'Email Templates Migration Verification Complete' AS status,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = 'EmailTemplates'
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'EmailMessages' AND column_name = 'Direction'
        ) AND (
            SELECT COUNT(*) FROM "EmailTemplates" WHERE "Code" IN ('RESCHEDULE_TIME_ONLY', 'RESCHEDULE_DATE_TIME', 'ASSURANCE_CABLE_REPULL')
        ) = 3 THEN 'READY'
        ELSE 'MIGRATION NEEDED'
    END AS result;

