-- ============================================
-- SERVICE INSTALLER DATA REMOVAL - CLEAN SLATE
-- ============================================
-- This script removes all Service Installer data from the database
-- Run this script to prepare for fresh installer data import
-- ============================================

-- Step 1: Set nullable foreign keys to NULL
UPDATE "Orders" 
SET "AssignedSiId" = NULL 
WHERE "AssignedSiId" IS NOT NULL;

UPDATE "StockMovements" 
SET "ServiceInstallerId" = NULL 
WHERE "ServiceInstallerId" IS NOT NULL;

UPDATE "StockLocations" 
SET "ServiceInstallerId" = NULL 
WHERE "ServiceInstallerId" IS NOT NULL;

-- Step 2: Delete dependent records that require ServiceInstaller
-- (These cannot have NULL ServiceInstallerId, so must be deleted)
DELETE FROM "ScheduledSlots";
DELETE FROM "SiAvailabilities";
DELETE FROM "SiLeaveRequests";
DELETE FROM "SiRatePlans";
DELETE FROM "GponSiCustomRate";
DELETE FROM "JobEarningRecord";
DELETE FROM "PayrollLine";
DELETE FROM "PnlDetailPerOrder";

-- Step 3: Delete ServiceInstallerContacts (will auto-delete via CASCADE, but explicit for clarity)
DELETE FROM "ServiceInstallerContacts";

-- Step 4: Delete all ServiceInstallers
DELETE FROM "ServiceInstallers";

-- Step 5: Verify
SELECT COUNT(*) as remaining_installers FROM "ServiceInstallers";
-- Expected: 0

SELECT COUNT(*) as remaining_contacts FROM "ServiceInstallerContacts";
-- Expected: 0

SELECT COUNT(*) as orders_with_installer 
FROM "Orders" 
WHERE "AssignedSiId" IS NOT NULL;
-- Expected: 0

-- ============================================
-- REMOVAL COMPLETE
-- ============================================

