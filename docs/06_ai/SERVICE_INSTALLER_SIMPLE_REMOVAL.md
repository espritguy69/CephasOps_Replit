# Service Installer Data Removal - Simple Clean Slate

**Date:** 2026-01-05  
**Objective:** Remove Service Installer seed data and existing database records only

---

## SIMPLE REMOVAL PLAN

### Step 1: Remove Seed Data from DatabaseSeeder

**File:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**Change 1:** Comment out or remove the seed method call (Line 100):
```csharp
// Seed GPON service installers
// await SeedGponServiceInstallersAsync(null, gponDepartment?.Id);
```

**Change 2:** Disable the seed method (Lines 695-756):
```csharp
private async Task SeedGponServiceInstallersAsync(Guid? companyId, Guid? departmentId)
{
    // Seed data removed - installers will be imported separately
    _logger.LogInformation("Service installer seeding disabled - installers will be imported separately");
    return;
}
```

---

### Step 2: SQL Script to Remove Database Data

**Important:** This script handles foreign keys by:
- Setting nullable FKs to NULL (Orders, StockMovements, StockLocations)
- Deleting dependent records that require ServiceInstaller (ScheduledSlots, etc.)
- Deleting ServiceInstallerContacts (CASCADE)
- Finally deleting ServiceInstallers

```sql
-- ============================================
-- SERVICE INSTALLER DATA REMOVAL - CLEAN SLATE
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
```

---

### Step 3: Apply Code Changes

**File to modify:**
- `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**Changes:**
1. Comment out line 100: `await SeedGponServiceInstallersAsync(...)`
2. Add early return in `SeedGponServiceInstallersAsync` method (lines 695-756)

---

### Step 4: Run SQL Script

Execute the SQL script above against your database to remove all Service Installer data.

---

### Step 5: Verify

```sql
-- Check ServiceInstallers is empty
SELECT COUNT(*) FROM "ServiceInstallers";
-- Expected: 0

-- Check ServiceInstallerContacts is empty
SELECT COUNT(*) FROM "ServiceInstallerContacts";
-- Expected: 0

-- Check Orders no longer reference installers
SELECT COUNT(*) FROM "Orders" WHERE "AssignedSiId" IS NOT NULL;
-- Expected: 0
```

---

## SUMMARY

**What gets removed:**
- ✅ All ServiceInstaller records from database
- ✅ All ServiceInstallerContacts (CASCADE)
- ✅ Seed data from DatabaseSeeder.cs

**What gets cleared:**
- ✅ Orders.AssignedSiId → set to NULL
- ✅ StockMovements.ServiceInstallerId → set to NULL
- ✅ StockLocations.ServiceInstallerId → set to NULL

**What gets deleted (dependent records):**
- ⚠️ ScheduledSlots (all records)
- ⚠️ SiAvailabilities (all records)
- ⚠️ SiLeaveRequests (all records)
- ⚠️ SiRatePlans (all records)
- ⚠️ GponSiCustomRate (all records)
- ⚠️ JobEarningRecord (all records)
- ⚠️ PayrollLine (all records)
- ⚠️ PnlDetailPerOrder (all records)

**Note:** The dependent records (ScheduledSlots, etc.) must be deleted because they have REQUIRED foreign keys to ServiceInstallers. If you want to keep those records, you would need to modify the schema first (make FKs nullable), but that's a larger change.

---

## FILES TO MODIFY

1. **DatabaseSeeder.cs**
   - Line 100: Comment out seed call
   - Lines 695-756: Add early return in seed method

---

**Ready to proceed?** Apply the code changes and run the SQL script.

