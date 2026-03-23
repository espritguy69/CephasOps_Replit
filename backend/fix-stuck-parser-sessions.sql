-- =============================================
-- Fix stuck parser sessions (status = 'Processing' with 0 drafts)
-- =============================================
-- This script updates sessions that are stuck in "Processing" state
-- with 0 parsed orders to "Completed" status with an error message
-- =============================================

UPDATE "ParseSessions"
SET 
    "Status" = 'Completed',
    "ErrorMessage" = 'Session was stuck in Processing state - no drafts were created. Please re-upload the file.',
    "UpdatedAt" = NOW(),
    "CompletedAt" = NOW()
WHERE 
    "Status" = 'Processing' 
    AND "ParsedOrdersCount" = 0
    AND "UpdatedAt" < NOW() - INTERVAL '5 minutes'; -- Only fix sessions older than 5 minutes

-- Show how many sessions were fixed
SELECT COUNT(*) as "FixedSessions"
FROM "ParseSessions"
WHERE 
    "Status" = 'Completed'
    AND "ErrorMessage" LIKE '%stuck in Processing%';

