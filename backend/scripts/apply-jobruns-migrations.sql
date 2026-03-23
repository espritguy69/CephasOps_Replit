-- Idempotent apply of JobRuns + RetriedFromJobRunId migrations (run once; safe to re-run).
-- Use when dotnet ef database update fails with PendingModelChangesWarning.

-- 1. Create JobRuns table if not exists (20260309100000_AddJobRunsTable)
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'JobRuns') THEN
    CREATE TABLE "JobRuns" (
      "Id" uuid NOT NULL,
      "CompanyId" uuid NULL,
      "JobName" character varying(200) NOT NULL,
      "JobType" character varying(100) NOT NULL,
      "TriggerSource" character varying(50) NOT NULL,
      "CorrelationId" character varying(100) NULL,
      "QueueOrChannel" character varying(100) NULL,
      "PayloadSummary" character varying(1000) NULL,
      "Status" character varying(50) NOT NULL,
      "StartedAtUtc" timestamp with time zone NOT NULL,
      "CompletedAtUtc" timestamp with time zone NULL,
      "DurationMs" bigint NULL,
      "RetryCount" integer NOT NULL,
      "WorkerNode" character varying(256) NULL,
      "ErrorCode" character varying(100) NULL,
      "ErrorMessage" character varying(500) NULL,
      "ErrorDetails" character varying(2000) NULL,
      "InitiatedByUserId" uuid NULL,
      "ParentJobRunId" uuid NULL,
      "RelatedEntityType" character varying(100) NULL,
      "RelatedEntityId" character varying(50) NULL,
      "BackgroundJobId" uuid NULL,
      "CreatedAtUtc" timestamp with time zone NOT NULL,
      "UpdatedAtUtc" timestamp with time zone NOT NULL,
      CONSTRAINT "PK_JobRuns" PRIMARY KEY ("Id")
    );
    CREATE INDEX "IX_JobRuns_StartedAtUtc" ON "JobRuns" ("StartedAtUtc");
    CREATE INDEX "IX_JobRuns_Status_StartedAtUtc" ON "JobRuns" ("Status", "StartedAtUtc");
    CREATE INDEX "IX_JobRuns_JobType_StartedAtUtc" ON "JobRuns" ("JobType", "StartedAtUtc");
    CREATE INDEX "IX_JobRuns_CompanyId_StartedAtUtc" ON "JobRuns" ("CompanyId", "StartedAtUtc");
    CREATE INDEX "IX_JobRuns_BackgroundJobId" ON "JobRuns" ("BackgroundJobId") WHERE "BackgroundJobId" IS NOT NULL;
    CREATE INDEX "IX_JobRuns_CorrelationId" ON "JobRuns" ("CorrelationId") WHERE "CorrelationId" IS NOT NULL;
  END IF;
END $$;

-- 2. Add RetriedFromJobRunId to BackgroundJobs if not exists (20260309110000_AddRetriedFromJobRunIdToBackgroundJob)
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'BackgroundJobs' AND column_name = 'RetriedFromJobRunId') THEN
    ALTER TABLE "BackgroundJobs" ADD COLUMN "RetriedFromJobRunId" uuid NULL;
  END IF;
END $$;

-- 3. Add ParentJobRunId index if not exists (20260309110100_AddJobRunsParentJobRunIdIndex)
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'JobRuns')
     AND NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND tablename = 'JobRuns' AND indexname = 'IX_JobRuns_ParentJobRunId') THEN
    CREATE INDEX "IX_JobRuns_ParentJobRunId" ON "JobRuns" ("ParentJobRunId") WHERE "ParentJobRunId" IS NOT NULL;
  END IF;
END $$;

-- 4. Add EventId to JobRuns if not exists (20260309120000_AddJobRunEventId)
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'JobRuns') THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'JobRuns' AND column_name = 'EventId') THEN
      ALTER TABLE "JobRuns" ADD COLUMN "EventId" uuid NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND tablename = 'JobRuns' AND indexname = 'IX_JobRuns_EventId') THEN
      CREATE INDEX "IX_JobRuns_EventId" ON "JobRuns" ("EventId") WHERE "EventId" IS NOT NULL;
    END IF;
  END IF;
END $$;

-- 5. Record migrations as applied (skip if already present)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309100000_AddJobRunsTable', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309100000_AddJobRunsTable');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309110000_AddRetriedFromJobRunIdToBackgroundJob', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309110000_AddRetriedFromJobRunIdToBackgroundJob');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309110100_AddJobRunsParentJobRunIdIndex', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309110100_AddJobRunsParentJobRunIdIndex');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260309120000_AddJobRunEventId', '10.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309120000_AddJobRunEventId');
