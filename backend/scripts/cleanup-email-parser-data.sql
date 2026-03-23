-- ============================================================================
-- Cleanup Script: Remove All Email Parser Data
-- ============================================================================
-- This script removes all email-related parsing data from the database:
-- - ParsedOrderDrafts (from email parse sessions)
-- - ParseSessions (from emails)
-- - EmailAttachments
-- - EmailMessages
-- 
-- WARNING: This will permanently delete all email parsing data!
-- Use with caution. This is for testing/development cleanup only.
-- ============================================================================

BEGIN;

-- Step 1: Delete ParsedOrderDrafts that came from email ParseSessions
-- (Only delete drafts that haven't been converted to Orders yet)
DELETE FROM "ParsedOrderDrafts"
WHERE "ParseSessionId" IN (
    SELECT "Id" 
    FROM "ParseSessions" 
    WHERE "EmailMessageId" IS NOT NULL
)
AND "CreatedOrderId" IS NULL; -- Don't delete drafts that created orders

-- Step 2: Delete ParseSessions that came from emails
DELETE FROM "ParseSessions"
WHERE "EmailMessageId" IS NOT NULL;

-- Step 3: Delete EmailAttachments
DELETE FROM "EmailAttachments";

-- Step 4: Delete EmailMessages
DELETE FROM "EmailMessages";

-- Step 5: Reset EmailAccount LastPolledAt (optional - uncomment if needed)
-- UPDATE "EmailAccounts" SET "LastPolledAt" = NULL;

COMMIT;

-- ============================================================================
-- Verification Queries (run after cleanup to verify)
-- ============================================================================

-- Check remaining ParsedOrderDrafts
SELECT COUNT(*) as remaining_drafts FROM "ParsedOrderDrafts";

-- Check remaining ParseSessions
SELECT COUNT(*) as remaining_sessions FROM "ParseSessions";

-- Check remaining EmailMessages
SELECT COUNT(*) as remaining_emails FROM "EmailMessages";

-- Check remaining EmailAttachments
SELECT COUNT(*) as remaining_attachments FROM "EmailAttachments";

