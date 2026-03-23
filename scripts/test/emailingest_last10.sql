SELECT "Id", "State", "CreatedAt", "StartedAt", "CompletedAt", "LastError"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
ORDER BY "CreatedAt" DESC
LIMIT 10;
