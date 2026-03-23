-- Migration: Add Direction field to EmailTemplates table
-- Date: 2025-12-16
-- Description: Separates email templates into Incoming (for parsing) and Outgoing (for sending)

DO $$
BEGIN
    -- Add Direction column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailTemplates' AND column_name = 'Direction'
    ) THEN
        ALTER TABLE "EmailTemplates" 
        ADD COLUMN "Direction" character varying(20) NOT NULL DEFAULT 'Outgoing';
        
        -- Set all existing templates to "Outgoing" (since EmailTemplate is for sending)
        UPDATE "EmailTemplates" SET "Direction" = 'Outgoing' WHERE "Direction" IS NULL;
        
        RAISE NOTICE 'Added Direction column to EmailTemplates table';
    ELSE
        RAISE NOTICE 'Direction column already exists in EmailTemplates table';
    END IF;

    -- Create index for filtering by direction
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'IX_EmailTemplates_CompanyId_Direction_IsActive'
    ) THEN
        CREATE INDEX "IX_EmailTemplates_CompanyId_Direction_IsActive" 
        ON "EmailTemplates" ("CompanyId", "Direction", "IsActive");
        
        RAISE NOTICE 'Created index IX_EmailTemplates_CompanyId_Direction_IsActive';
    ELSE
        RAISE NOTICE 'Index IX_EmailTemplates_CompanyId_Direction_IsActive already exists';
    END IF;
END $$;

