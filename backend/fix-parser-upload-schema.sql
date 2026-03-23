-- =============================================
-- Fix ParseSessions table schema for file uploads
-- =============================================
-- This script ensures:
-- 1. EmailMessageId is nullable (required for file uploads)
-- 2. UpdatedAt has a default value
-- 3. RowVersion column exists with proper default
-- 4. SourceType and SourceDescription columns exist
--
-- Run this script in your PostgreSQL database before testing file uploads
-- =============================================

-- Make EmailMessageId nullable (if not already)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'ParseSessions' 
        AND column_name = 'EmailMessageId'
        AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "ParseSessions" ALTER COLUMN "EmailMessageId" DROP NOT NULL;
        RAISE NOTICE 'Made EmailMessageId nullable';
    ELSE
        RAISE NOTICE 'EmailMessageId is already nullable';
    END IF;
END $$;

-- Ensure UpdatedAt has a default value
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'ParseSessions' 
        AND column_name = 'UpdatedAt'
        AND column_default IS NOT NULL
    ) THEN
        ALTER TABLE "ParseSessions" 
        ALTER COLUMN "UpdatedAt" SET DEFAULT (now());
        RAISE NOTICE 'Added default value to UpdatedAt';
    ELSE
        RAISE NOTICE 'UpdatedAt already has a default value';
    END IF;
END $$;

-- Ensure RowVersion column exists and has proper default
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'ParseSessions' 
        AND column_name = 'RowVersion'
    ) THEN
        ALTER TABLE "ParseSessions" 
        ADD COLUMN "RowVersion" bytea DEFAULT pgcrypto.gen_random_bytes(8);
        RAISE NOTICE 'Added RowVersion column';
    ELSE
        -- Ensure it has a default value
        BEGIN
            ALTER TABLE "ParseSessions" 
            ALTER COLUMN "RowVersion" SET DEFAULT pgcrypto.gen_random_bytes(8);
            RAISE NOTICE 'Updated RowVersion default value';
        EXCEPTION WHEN OTHERS THEN
            -- If setting default fails, try without schema qualification
            ALTER TABLE "ParseSessions" 
            ALTER COLUMN "RowVersion" SET DEFAULT gen_random_bytes(8);
            RAISE NOTICE 'Updated RowVersion default value (fallback)';
        END;
    END IF;
END $$;

-- Ensure SourceType and SourceDescription columns exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'ParseSessions' 
        AND column_name = 'SourceType'
    ) THEN
        ALTER TABLE "ParseSessions" 
        ADD COLUMN "SourceType" VARCHAR(50);
        RAISE NOTICE 'Added SourceType column';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'ParseSessions' 
        AND column_name = 'SourceDescription'
    ) THEN
        ALTER TABLE "ParseSessions" 
        ADD COLUMN "SourceDescription" VARCHAR(1000);
        RAISE NOTICE 'Added SourceDescription column';
    END IF;
END $$;

DO $$
BEGIN
    RAISE NOTICE 'ParseSessions schema fix completed';
END $$;

