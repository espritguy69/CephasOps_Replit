-- 1) DB time + environment
SELECT '=== 1) DB time + environment ===' AS step;
SELECT now() AS db_now;
SELECT current_database() AS db_name;
SELECT inet_server_addr() AS host_ip, inet_server_port() AS port;
-- Schema: default public
SELECT current_schema() AS current_schema;

-- 2) BackgroundJob activity (all types, last 24h)
SELECT '=== 2) BackgroundJobs created last 24h by JobType, State ===' AS step;
SELECT "JobType", "State", COUNT(*) AS cnt
FROM "BackgroundJobs"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
GROUP BY "JobType", "State"
ORDER BY "JobType", "State";

SELECT 'Newest 20 BackgroundJobs (all time)' AS step;
SELECT "JobType", "State", "CreatedAt", "StartedAt", "CompletedAt", LEFT("LastError", 80) AS last_error
FROM "BackgroundJobs"
ORDER BY "CreatedAt" DESC
LIMIT 20;

-- 4) EmailAccounts sanity (admin@cephas.com.my)
SELECT '=== 4) EmailAccounts admin@cephas.com.my ===' AS step;
SELECT "Id", "Name", "Username", "IsActive", "PollIntervalSec", "LastPolledAt",
       "LastPolledAt"::date = CURRENT_DATE AS last_polled_is_today
FROM "EmailAccounts"
WHERE "IsDeleted" = false AND "Username" ILIKE '%admin@cephas.com.my%';

-- EmailIngest jobs referenced in logs (93840d17, 31f4cdbd) - current state
SELECT 'EmailIngest jobs from logs (current state)' AS step;
SELECT "Id", "State", "CreatedAt", "CompletedAt", LEFT("LastError", 60) AS last_error
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' AND "Id" IN ('93840d17-21e0-44f6-8225-bb85b8f4a8f8'::uuid, '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid);
