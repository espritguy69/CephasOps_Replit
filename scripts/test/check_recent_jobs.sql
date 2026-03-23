-- Check Recent EmailIngest Jobs
SELECT 
    "Id",
    "State",
    "CreatedAt",
    "ScheduledAt",
    "StartedAt",
    "CompletedAt"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
ORDER BY "CreatedAt" DESC
LIMIT 5;

