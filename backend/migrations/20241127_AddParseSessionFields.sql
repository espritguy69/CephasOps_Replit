-- Migration: Add missing fields to ParseSessions table
-- Date: 2024-11-27
-- Description: Adds SourceType, SourceDescription, SnapshotFileId columns

-- =============================================
-- Add new columns to ParseSessions table
-- =============================================
ALTER TABLE "ParseSessions" ADD COLUMN IF NOT EXISTS "SourceType" VARCHAR(50);
ALTER TABLE "ParseSessions" ADD COLUMN IF NOT EXISTS "SourceDescription" VARCHAR(1000);
ALTER TABLE "ParseSessions" ADD COLUMN IF NOT EXISTS "SnapshotFileId" UUID;

-- Make EmailMessageId nullable (if not already)
ALTER TABLE "ParseSessions" ALTER COLUMN "EmailMessageId" DROP NOT NULL;

-- =============================================
-- Update any existing records
-- =============================================
UPDATE "ParseSessions" SET "SourceType" = 'Email' WHERE "SourceType" IS NULL AND "EmailMessageId" IS NOT NULL;
UPDATE "ParseSessions" SET "SourceType" = 'Unknown' WHERE "SourceType" IS NULL;

-- =============================================
-- Verify
-- =============================================
DO $$
BEGIN
    RAISE NOTICE 'Migration 20241127_AddParseSessionFields completed successfully';
END $$;

