-- Remediation: Multiple migrations were recorded in __EFMigrationsHistory but their
-- schema changes were rolled back (transaction failures during idempotent migration).
-- This removes ALL phantom migration records so re-running 'migrate' will properly
-- create all missing tables and apply all schema changes.
--
-- After running this script, re-run: deploy-vps-native.sh migrate
--
-- Safe to re-run: only deletes records for migrations dated after the last known-good one.

BEGIN;

-- Remove migration records from 2025-12-02 onward where transactions failed.
-- The three known failures:
--   1. 20251202155910_AddPartnerGroupIdToBillingRatecard (ServiceCategory column error)
--   2. 20251202174653_AddSoftDeleteToCompanyScopedEntities (DeletedAt column error)
--   3. 20260310031127_AddExternalIntegrationBus (all integration tables)
-- Plus any migrations after these that may also be phantom.
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" >= '20251202155910';

COMMIT;
