-- ============================================
-- Backend ingestion chain audit (last 14 days)
-- No code changes. Evidence only.
-- ============================================

-- 1) BackgroundJob: EmailIngest (last 14 days)
SELECT '=== 1) BACKGROUND JOBS (JobType = EmailIngest, last 14 days) ===' AS info;
SELECT "State", COUNT(*) AS cnt
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
  AND "CreatedAt" >= NOW() - INTERVAL '14 days'
GROUP BY "State"
ORDER BY "State";

SELECT 'Newest CreatedAt' AS metric, MAX("CreatedAt")::text AS value FROM "BackgroundJobs" WHERE "JobType" = 'EmailIngest' AND "CreatedAt" >= NOW() - INTERVAL '14 days'
UNION ALL
SELECT 'Newest CompletedAt', MAX("CompletedAt")::text FROM "BackgroundJobs" WHERE "JobType" = 'EmailIngest' AND "CreatedAt" >= NOW() - INTERVAL '14 days'
UNION ALL
SELECT 'Last successful (Succeeded) job', MAX("CompletedAt")::text FROM "BackgroundJobs" WHERE "JobType" = 'EmailIngest' AND "State" = 'Succeeded' AND "CreatedAt" >= NOW() - INTERVAL '14 days';

SELECT 'Last 5 Failed jobs (CreatedAt, LastError)' AS info;
SELECT "Id", "CreatedAt", "LastError"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' AND "State" = 'Failed' AND "CreatedAt" >= NOW() - INTERVAL '14 days'
ORDER BY "CreatedAt" DESC
LIMIT 5;

-- 1b) EmailIngest jobs all-time (when did they last exist?)
SELECT 'Last EmailIngest job ever (any state)' AS info;
SELECT "Id", "State", "CreatedAt", "CompletedAt", LEFT("LastError", 100) AS last_error_preview
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
ORDER BY "CreatedAt" DESC
LIMIT 3;

-- 2) EmailAccounts: admin@cephas.com.my
SELECT '=== 2) EMAIL ACCOUNTS (admin@cephas.com.my) ===' AS info;
SELECT "Id", "Name", "Username", "IsActive", "PollIntervalSec", "LastPolledAt",
       "Provider", "Host", "Port", "UseSsl",
       CASE WHEN "Username" IS NOT NULL AND TRIM("Username") <> '' THEN 'yes' ELSE 'no' END AS username_present,
       CASE WHEN "Password" IS NOT NULL AND TRIM("Password") <> '' THEN 'yes' ELSE 'no' END AS password_present,
       "DefaultParserTemplateId"
FROM "EmailAccounts"
WHERE "IsDeleted" = false
  AND ("Name" ILIKE '%admin@cephas.com.my%' OR "Username" ILIKE '%admin@cephas.com.my%');


-- 3) EmailMessage: for that EmailAccountId, last 14 days (use Id from step 2; run as two-step or use subquery)
SELECT '=== 3) EMAIL MESSAGES (last 14 days, account admin@cephas.com.my) ===' AS info;
WITH acc AS (
  SELECT "Id" FROM "EmailAccounts"
  WHERE "IsDeleted" = false AND ("Name" ILIKE '%admin@cephas.com.my%' OR "Username" ILIKE '%admin@cephas.com.my%')
  LIMIT 1
)
SELECT
  (SELECT COUNT(*) FROM "EmailMessages" m, acc WHERE m."EmailAccountId" = acc."Id" AND m."IsDeleted" = false AND m."CreatedAt" >= NOW() - INTERVAL '14 days') AS count_last_14d,
  (SELECT MAX(m."CreatedAt")::text FROM "EmailMessages" m, acc WHERE m."EmailAccountId" = acc."Id" AND m."IsDeleted" = false AND m."CreatedAt" >= NOW() - INTERVAL '14 days') AS newest_created_at,
  (SELECT m."Subject" FROM "EmailMessages" m, acc WHERE m."EmailAccountId" = acc."Id" AND m."IsDeleted" = false AND m."CreatedAt" >= NOW() - INTERVAL '14 days' ORDER BY m."CreatedAt" DESC LIMIT 1) AS newest_subject,
  (SELECT m."FromAddress" FROM "EmailMessages" m, acc WHERE m."EmailAccountId" = acc."Id" AND m."IsDeleted" = false AND m."CreatedAt" >= NOW() - INTERVAL '14 days' ORDER BY m."CreatedAt" DESC LIMIT 1) AS newest_from;

-- 4) ParseSession: last 14 days
SELECT '=== 4) PARSE SESSIONS (last 14 days) ===' AS info;
SELECT COUNT(*) AS count_last_14d,
       MAX("CreatedAt")::text AS newest_created_at,
       MAX("Status") AS sample_status
FROM "ParseSessions"
WHERE "IsDeleted" = false
  AND "CreatedAt" >= NOW() - INTERVAL '14 days';

SELECT "Id", "CreatedAt", "Status", "SourceType", "ParsedOrdersCount"
FROM "ParseSessions"
WHERE "IsDeleted" = false AND "CreatedAt" >= NOW() - INTERVAL '14 days'
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- 5) Drafts in last 14 days (for conclusion)
SELECT '=== 5) PARSED ORDER DRAFTS (last 14 days) ===' AS info;
SELECT COUNT(*) AS drafts_last_14d FROM "ParsedOrderDrafts" WHERE "IsDeleted" = false AND "CreatedAt" >= NOW() - INTERVAL '14 days';
