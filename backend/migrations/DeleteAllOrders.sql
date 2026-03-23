-- ============================================================================
-- Delete All Orders Script
-- ============================================================================
-- WARNING: This will permanently delete ALL orders from the database
-- Use only for testing/development before testing manual parser
-- 
-- This script deletes all orders and related records to prepare for
-- testing the manual parser with Activation, Modification Outdoor, and Partners
-- ============================================================================

BEGIN;

-- Delete related records first (to avoid foreign key constraints)
DELETE FROM "OrderMaterialReplacements";
DELETE FROM "OrderNonSerialisedReplacements";
DELETE FROM "OrderMaterialUsage";
DELETE FROM "OrderStatusLogs";
DELETE FROM "OrderDockets";
DELETE FROM "OrderBlockers";
DELETE FROM "OrderReschedules";
DELETE FROM "PnlDetailPerOrders";
DELETE FROM "WorkflowJobs" WHERE "EntityType" = 'Order';

-- Delete all orders (including soft-deleted)
DELETE FROM "Orders";

COMMIT;

-- ============================================================================
-- Verification queries (run after deletion to confirm)
-- ============================================================================
-- SELECT COUNT(*) FROM "Orders"; -- Should return 0
-- SELECT COUNT(*) FROM "OrderMaterialUsage"; -- Should return 0
-- SELECT COUNT(*) FROM "OrderStatusLogs"; -- Should return 0
-- SELECT COUNT(*) FROM "OrderDockets"; -- Should return 0
-- ============================================================================

