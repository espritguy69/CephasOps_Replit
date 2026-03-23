-- Migration: Add BodyText and BodyHtml columns to EmailMessages table
-- Date: 2025-12-16
-- Description: Adds full email body storage (text and HTML) to support complete email viewing and parsing

DO $$
BEGIN
    -- Add BodyText column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailMessages' AND column_name = 'BodyText'
    ) THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "BodyText" text NULL;
    END IF;

    -- Add BodyHtml column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'EmailMessages' AND column_name = 'BodyHtml'
    ) THEN
        ALTER TABLE "EmailMessages" ADD COLUMN "BodyHtml" text NULL;
    END IF;
END $$;

