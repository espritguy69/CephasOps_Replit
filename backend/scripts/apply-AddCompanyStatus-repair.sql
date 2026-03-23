-- Idempotent repair: add Companies.Status and IX_Companies_Status if missing.
-- Fixes login error: 42703: column c.Status does not exist
-- Run with: psql -h localhost -p 5432 -U postgres -d cephasops -f apply-AddCompanyStatus-repair.sql

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'Companies' AND column_name = 'Status'
    ) THEN
        ALTER TABLE "Companies"
        ADD COLUMN "Status" character varying(32) NOT NULL DEFAULT 'Active';
        RAISE NOTICE 'Added column Companies.Status';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'Companies' AND indexname = 'IX_Companies_Status'
    ) THEN
        CREATE INDEX "IX_Companies_Status" ON "Companies" ("Status");
        RAISE NOTICE 'Created index IX_Companies_Status';
    END IF;
END $$;
