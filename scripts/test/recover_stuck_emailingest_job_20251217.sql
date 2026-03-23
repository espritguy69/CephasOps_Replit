-- ============================================
-- Recover stuck EmailIngest BackgroundJob (production-safe)
-- Job Id: 31f4cdbd-6805-475c-89c7-1fd60d46b3d5 (Running since 2025-12-17)
-- Run during maintenance window after app is stopped.
-- ============================================
\set ON_ERROR_STOP on
-- Stops script immediately if DO guard raises (job missing or not Running); backup/update then do not run.

-- Target: only this job (stuck Running since 2025-12-17)
-- Id: 31f4cdbd-6805-475c-89c7-1fd60d46b3d5

-- ---------------------------------------------------------------------------
-- 1) SELECT BEFORE (confirm the row exists and is Running)
-- ---------------------------------------------------------------------------
SELECT '=== BEFORE (current row state) ===' AS step;
SELECT "Id", "JobType", "State", "CreatedAt", "StartedAt", "CompletedAt", "LastError", "UpdatedAt"
FROM "BackgroundJobs"
WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;

-- Safety: abort if row not found or not in Running state
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM "BackgroundJobs"
    WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid
  ) THEN
    RAISE EXCEPTION 'Recovery aborted: job 31f4cdbd-6805-475c-89c7-1fd60d46b3d5 not found';
  END IF;
  IF (SELECT "State" FROM "BackgroundJobs" WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid) != 'Running' THEN
    RAISE EXCEPTION 'Recovery aborted: job is not in Running state (already recovered or changed?)';
  END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 2) Backup row into a temp table (session-scoped, no schema change)
-- ---------------------------------------------------------------------------
SELECT '=== BACKUP (into temp table) ===' AS step;
CREATE TEMP TABLE IF NOT EXISTS backup_background_job_stuck_emailingest_20251217 (
  "Id" uuid NOT NULL,
  "JobType" character varying(100) NOT NULL,
  "PayloadJson" jsonb NOT NULL,
  "State" character varying(50) NOT NULL,
  "RetryCount" integer NOT NULL,
  "MaxRetries" integer NOT NULL,
  "LastError" character varying(2000),
  "Priority" integer NOT NULL,
  "ScheduledAt" timestamp with time zone,
  "CreatedAt" timestamp with time zone NOT NULL,
  "StartedAt" timestamp with time zone,
  "CompletedAt" timestamp with time zone,
  "UpdatedAt" timestamp with time zone NOT NULL,
  backup_at timestamp with time zone DEFAULT NOW()
);

-- Upsert backup (idempotent if script re-run)
DELETE FROM backup_background_job_stuck_emailingest_20251217 WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;
INSERT INTO backup_background_job_stuck_emailingest_20251217 (
  "Id", "JobType", "PayloadJson", "State", "RetryCount", "MaxRetries", "LastError",
  "Priority", "ScheduledAt", "CreatedAt", "StartedAt", "CompletedAt", "UpdatedAt"
)
SELECT "Id", "JobType", "PayloadJson", "State", "RetryCount", "MaxRetries", "LastError",
  "Priority", "ScheduledAt", "CreatedAt", "StartedAt", "CompletedAt", "UpdatedAt"
FROM "BackgroundJobs"
WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;

SELECT 'Backup row count:' AS info, COUNT(*) AS n FROM backup_background_job_stuck_emailingest_20251217 WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;

-- ---------------------------------------------------------------------------
-- 3) Set stuck job to Failed with recovery note
-- ---------------------------------------------------------------------------
SELECT '=== UPDATE (set to Failed) ===' AS step;
UPDATE "BackgroundJobs"
SET
  "State" = 'Failed',
  "CompletedAt" = NOW(),
  "LastError" = 'Recovered: stuck Running since 2025-12-17',
  "UpdatedAt" = NOW()
WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;

-- ---------------------------------------------------------------------------
-- 4) SELECT AFTER (verify)
-- ---------------------------------------------------------------------------
SELECT '=== AFTER (row state after recovery) ===' AS step;
SELECT "Id", "JobType", "State", "CreatedAt", "StartedAt", "CompletedAt", "LastError", "UpdatedAt"
FROM "BackgroundJobs"
WHERE "Id" = '31f4cdbd-6805-475c-89c7-1fd60d46b3d5'::uuid;

SELECT 'Recovery complete. Restart the application so the scheduler can create new EmailIngest jobs.' AS next_step;
