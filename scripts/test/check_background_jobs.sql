-- ============================================
-- Background Jobs Monitoring Script
-- Use this to monitor email ingestion jobs
-- ============================================

-- 1. Recent EmailIngest Jobs
SELECT 
    '=== RECENT EMAIL INGEST JOBS ===' as info;

SELECT 
    "Id",
    "JobType",
    "State",
    "Priority",
    "ScheduledAt",
    "CreatedAt",
    "StartedAt",
    "CompletedAt",
    "RetryCount",
    "MaxRetries",
    "LastError",
    "PayloadJson"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
ORDER BY "CreatedAt" DESC
LIMIT 20;

-- 2. Job Statistics
SELECT 
    '=== JOB STATISTICS ===' as info;

SELECT 
    "State",
    COUNT(*) as "Count",
    MIN("CreatedAt") as "Oldest",
    MAX("CreatedAt") as "Newest"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
GROUP BY "State"
ORDER BY "Count" DESC;

-- 3. Failed Jobs with Errors
SELECT 
    '=== FAILED JOBS ===' as info;

SELECT 
    "Id",
    "State",
    "LastError",
    "RetryCount",
    "MaxRetries",
    "CreatedAt",
    "ScheduledAt"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
AND "State" = 'Failed'
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- 4. Currently Running Jobs
SELECT 
    '=== RUNNING JOBS ===' as info;

SELECT 
    "Id",
    "JobType",
    "State",
    "StartedAt",
    "PayloadJson"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
AND "State" = 'Running'
ORDER BY "StartedAt" DESC;

-- 5. Queued Jobs
SELECT 
    '=== QUEUED JOBS ===' as info;

SELECT 
    "Id",
    "JobType",
    "State",
    "Priority",
    "ScheduledAt",
    "CreatedAt",
    "PayloadJson"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
AND "State" = 'Queued'
ORDER BY "Priority" DESC, "CreatedAt" ASC
LIMIT 10;

