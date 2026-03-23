-- EmailIngest jobs in Queued state older than 10 minutes (last 30 days)
-- Evidence: can they block scheduling indefinitely?

-- Count and oldest queued age
SELECT
  COUNT(*) AS queued_older_than_10m_count,
  MIN("CreatedAt") AS oldest_created_at,
  EXTRACT(EPOCH FROM (NOW() - MIN("CreatedAt")))/60.0 AS oldest_queued_age_minutes
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
  AND "State" = 'Queued'
  AND "CreatedAt" >= NOW() - INTERVAL '30 days'
  AND "CreatedAt" < NOW() - INTERVAL '10 minutes';

-- Sample of such jobs (up to 5)
SELECT "Id", "State", "CreatedAt", "ScheduledAt", "UpdatedAt",
       EXTRACT(EPOCH FROM (NOW() - "CreatedAt"))/60.0 AS queued_age_minutes
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
  AND "State" = 'Queued'
  AND "CreatedAt" >= NOW() - INTERVAL '30 days'
  AND "CreatedAt" < NOW() - INTERVAL '10 minutes'
ORDER BY "CreatedAt" ASC
LIMIT 5;

-- All EmailIngest by State in last 30 days (context)
SELECT 'EmailIngest by State (last 30 days)' AS info;
SELECT "State", COUNT(*) AS cnt
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' AND "CreatedAt" >= NOW() - INTERVAL '30 days'
GROUP BY "State";
