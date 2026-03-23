-- Remediation 1.1 verification: exact checks for schema objects required by running code.
-- Run: psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/verify-remediation-1.1.sql

\echo '=== 1. EventStore.RootEventId column ==='
SELECT EXISTS (
  SELECT 1 FROM information_schema.columns
  WHERE table_schema = 'public' AND table_name = 'EventStore' AND column_name = 'RootEventId'
) AS "EventStore.RootEventId exists";

\echo ''
\echo '=== 2. Tables: OrderPayoutSnapshots, InboundWebhookReceipts, JobExecutions, PayoutSnapshotRepairRuns ==='
SELECT
  to_regclass('public."OrderPayoutSnapshots"') IS NOT NULL AS "OrderPayoutSnapshots",
  to_regclass('public."InboundWebhookReceipts"') IS NOT NULL AS "InboundWebhookReceipts",
  to_regclass('public."JobExecutions"') IS NOT NULL AS "JobExecutions",
  to_regclass('public."PayoutSnapshotRepairRuns"') IS NOT NULL AS "PayoutSnapshotRepairRuns";

\echo ''
\echo '=== 3. EventStore columns (all) ==='
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'EventStore'
ORDER BY ordinal_position;

\echo ''
\echo '=== 4. Index IX_EventStore_RootEventId ==='
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'EventStore' AND indexname = 'IX_EventStore_RootEventId';
