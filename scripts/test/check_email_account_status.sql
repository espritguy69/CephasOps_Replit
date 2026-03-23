-- Quick Email Account Status Check
SELECT 
    'Email Accounts Status' as info;

SELECT 
    "Name",
    "IsActive",
    "PollIntervalSec",
    "LastPolledAt",
    CASE 
        WHEN "LastPolledAt" IS NULL THEN 'Never Polled'
        WHEN "LastPolledAt" < NOW() - INTERVAL '5 minutes' THEN 'Stale (>5 min)'
        ELSE 'Recent'
    END as "PollStatus",
    NOW() - "LastPolledAt" as "TimeSinceLastPoll"
FROM "EmailAccounts"
WHERE "IsActive" = true;

