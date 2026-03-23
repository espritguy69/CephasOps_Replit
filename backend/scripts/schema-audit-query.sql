-- Schema verification audit: tables, columns, indexes (run in public schema).
-- Output: table name, then for each table columns and indexes.

\echo '=== ALL TABLES (public schema) ==='
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
ORDER BY table_name;

\echo ''
\echo '=== COLUMNS: EventStore ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'EventStore'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: OrderPayoutSnapshots ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'OrderPayoutSnapshots'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: PayoutSnapshotRepairRuns ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'PayoutSnapshotRepairRuns'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: InboundWebhookReceipts ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'InboundWebhookReceipts'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: JobExecutions ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'JobExecutions'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: OperationalInsights ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'OperationalInsights'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: BillingPlanFeatures ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'BillingPlanFeatures'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: TenantFeatureFlags ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'TenantFeatureFlags'
ORDER BY ordinal_position;

\echo ''
\echo '=== COLUMNS: EventStoreAttemptHistory ==='
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'EventStoreAttemptHistory'
ORDER BY ordinal_position;

\echo ''
\echo '=== INDEXES: EventStore ==='
SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'EventStore' ORDER BY indexname;

\echo ''
\echo '=== INDEXES: OrderPayoutSnapshots ==='
SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'OrderPayoutSnapshots' ORDER BY indexname;

\echo ''
\echo '=== INDEXES: JobExecutions ==='
SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'JobExecutions' ORDER BY indexname;

\echo ''
\echo '=== INDEXES: InboundWebhookReceipts ==='
SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'InboundWebhookReceipts' ORDER BY indexname;

\echo ''
\echo '=== __EFMigrationsHistory (last 5) ==='
SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
