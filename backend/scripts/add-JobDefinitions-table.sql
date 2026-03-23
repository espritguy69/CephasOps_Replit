-- Idempotent: create JobDefinitions table if missing (fixes 42P01: relation "JobDefinitions" does not exist).
-- Run against your cephasops database, e.g.:
--   psql -h localhost -p 5432 -U postgres -d cephasops -f add-JobDefinitions-table.sql
-- Or from backend/scripts:  $env:PGPASSWORD='yourpassword'; psql -h localhost -p 5432 -U postgres -d cephasops -f add-JobDefinitions-table.sql

CREATE TABLE IF NOT EXISTS "JobDefinitions" (
    "Id" uuid NOT NULL,
    "JobType" character varying(100) NOT NULL,
    "DisplayName" character varying(200) NOT NULL,
    "RetryAllowed" boolean NOT NULL,
    "MaxRetries" integer NOT NULL,
    "DefaultStuckThresholdSeconds" integer NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_JobDefinitions" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_JobDefinitions_JobType"
    ON "JobDefinitions" ("JobType");

COMMENT ON TABLE "JobDefinitions" IS 'Metadata per job type: display name, retry policy, stuck threshold. Empty table is valid; app falls back to built-in defaults.';
