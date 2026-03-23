-- Idempotent application of BackgroundJob worker ownership columns and indexes
-- (scheduler Phase 1). Use this if the full EF migration is blocked by other migrations.
-- Run against the cephasops database.

-- Columns
ALTER TABLE "BackgroundJobs" ADD COLUMN IF NOT EXISTS "ClaimedAtUtc" timestamp with time zone NULL;
ALTER TABLE "BackgroundJobs" ADD COLUMN IF NOT EXISTS "WorkerId" uuid NULL;

-- Indexes
CREATE INDEX IF NOT EXISTS "IX_BackgroundJobs_State_WorkerId" ON "BackgroundJobs" ("State", "WorkerId");
CREATE INDEX IF NOT EXISTS "IX_BackgroundJobs_WorkerId" ON "BackgroundJobs" ("WorkerId");
