-- ============================================================================
-- Cleanup Script: Remove ALL Parser Data (Complete Clean Slate)
-- ============================================================================
-- This script removes ALL parsing data from the database:
-- - ALL ParsedOrderDrafts (from both emails and file uploads)
-- - ALL ParseSessions (from both emails and file uploads)
-- - EmailAttachments
-- - EmailMessages
-- 
-- WARNING: This will permanently delete ALL parser data!
-- Use with caution. This is for testing/development cleanup only.
-- ============================================================================

BEGIN;

-- Step 1: Delete ALL ParsedOrderDrafts
-- (Only delete drafts that haven't been converted to Orders yet)
DELETE FROM "ParsedOrderDrafts"
WHERE "CreatedOrderId" IS NULL; -- Don't delete drafts that created orders

-- Step 2: Delete ALL ParseSessions (both email and file upload)
DELETE FROM "ParseSessions";

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

