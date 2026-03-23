-- Check migration history and schema state for EF migration drift.
-- Use after deployment to verify expected tables and columns exist.
\echo '=== __EFMigrationsHistory (ordered) ==='
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";

\echo ''
\echo '=== Table PasswordResetTokens exists? ==='
SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PasswordResetTokens');

\echo ''
\echo '=== Indexes on PasswordResetTokens (if table exists) ==='
SELECT indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'PasswordResetTokens' ORDER BY indexname;

\echo ''
\echo '=== Table OrderPayoutSnapshots exists? ==='
SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'OrderPayoutSnapshots');

\echo ''
\echo '=== OrderPayoutSnapshots: Provenance column (snapshot provenance)? ==='
SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'OrderPayoutSnapshots' AND column_name = 'Provenance';

\echo ''
\echo '=== Table PayoutAnomalyReviews exists? ==='
SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PayoutAnomalyReviews');

\echo ''
\echo '=== Table PayoutSnapshotRepairRuns (repair run history) exists? ==='
SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'PayoutSnapshotRepairRuns');

\echo ''
\echo '=== RefreshTokens columns (UserAgent?) ==='
SELECT column_name, data_type FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'RefreshTokens' ORDER BY ordinal_position;

\echo ''
\echo '=== EventStore Phase 7 columns (lease/error)? ==='
SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'EventStore' AND column_name IN ('ProcessingNodeId', 'ProcessingLeaseExpiresAtUtc', 'LastClaimedAtUtc', 'LastClaimedBy', 'LastErrorType') ORDER BY column_name;

\echo ''
\echo '=== Table EventStoreAttemptHistory exists? ==='
SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'EventStoreAttemptHistory');

\echo ''
\echo '=== Phase 7 migration in history? ==='
SELECT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312100000_AddEventStorePhase7LeaseAndAttemptHistory');

\echo ''
\echo '=== Model-required integration-bus tables (AddExternalIntegrationBus)? ==='
SELECT table_name, EXISTS (SELECT 1 FROM information_schema.tables t WHERE t.table_schema = 'public' AND t.table_name = v.table_name) AS exists
FROM (VALUES
  ('ConnectorDefinitions'),
  ('ConnectorEndpoints'),
  ('ExternalIdempotencyRecords'),
  ('OutboundIntegrationAttempts')
) AS v(table_name)
ORDER BY v.table_name;
