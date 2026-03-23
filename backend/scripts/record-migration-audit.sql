-- Record a migration deployment in MigrationAudit (operational audit only).
-- Run this AFTER: migration applied, verification script passed, smoke test passed.
-- Replace the placeholder values below with actual values for your environment.
--
-- Prerequisites:
--   1. Migration must be present in __EFMigrationsHistory.
--   2. Post-apply verification (e.g. verify-20260313025530-email-messages-text.sql or check-migration-state.sql) has passed.
--   3. Smoke test (if required for this migration) has passed.
--
-- Usage: Edit the VALUES below, then run with psql or your SQL client:
--   psql -h localhost -p 5432 -U postgres -d cephasops -f record-migration-audit.sql

INSERT INTO "MigrationAudit" (
    "Id",
    "Environment",
    "MigrationId",
    "AppliedAtUtc",
    "AppliedBy",
    "MethodUsed",
    "VerificationStatus",
    "SmokeTestStatus",
    "Notes"
) VALUES (
    gen_random_uuid(),
    'Development',           -- Environment: Development | Staging | Production
    '20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText',  -- MigrationId (EF migration ID)
    NOW() AT TIME ZONE 'UTC',
    current_user,            -- AppliedBy: e.g. current_user, 'deploy-pipeline', 'ops@company.com'
    'EF database update',    -- MethodUsed: 'EF database update' | 'Idempotent script' | 'Migration bundle'
    'Pass',                  -- VerificationStatus: Pass | Fail | Skipped
    'Pass',                  -- SmokeTestStatus: Pass | Fail | Skipped | N/A
    NULL                     -- Notes: optional ticket ref or comments
);

-- Verify the row was inserted:
SELECT "Id", "Environment", "MigrationId", "AppliedAtUtc", "AppliedBy", "VerificationStatus", "SmokeTestStatus"
FROM "MigrationAudit"
ORDER BY "AppliedAtUtc" DESC
LIMIT 5;
