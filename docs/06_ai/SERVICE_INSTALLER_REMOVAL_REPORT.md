# Service Installer Data Removal - Investigation Report

**Date:** 2026-01-05  
**Status:** Investigation Complete  
**Objective:** Remove ALL Service Installer data from database and seed data

---

## SERVICE INSTALLER DATA REPORT
==============================

### ENTITY INFORMATION

**Entity Name:** `ServiceInstaller`  
**Entity Location:** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`  
**Table Name:** `ServiceInstallers`  
**Primary Key:** `Id` (Guid)

**Properties:**
- `Id` (Guid) - Primary key
- `CompanyId` (Guid) - Company scope
- `DepartmentId` (Guid?) - Optional department assignment
- `Name` (string, required, max 200) - Installer name
- `EmployeeId` (string?, max 50) - Employee ID
- `Phone` (string?, max 50) - Phone number
- `Email` (string?, max 255) - Email address
- `SiLevel` (string, required, max 50) - Level (Junior, Senior, Subcon)
- `IsSubcontractor` (bool) - Whether subcontractor
- `IsActive` (bool) - Active status
- `UserId` (Guid?) - Link to User if SI has login access
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime?) - Update timestamp
- `Contacts` (ICollection<ServiceInstallerContact>) - Related contacts

---

### RELATIONSHIPS

**Referenced By (Foreign Keys):**

1. **ServiceInstallerContacts** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **CASCADE** (child records deleted when parent deleted)
   - Impact: All contacts will be deleted when installers are deleted

2. **Orders** (Nullable FK)
   - Field: `AssignedSiId`
   - Delete Behavior: **SET NULL** (nullable, will be set to null)
   - Impact: Orders will have `AssignedSiId = null` after deletion

3. **ScheduledSlots** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **RESTRICT** (prevents deletion if slots exist)
   - Impact: **CRITICAL** - Must delete ScheduledSlots first

4. **SiAvailabilities** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **RESTRICT** (prevents deletion if availabilities exist)
   - Impact: **CRITICAL** - Must delete SiAvailabilities first

5. **SiLeaveRequests** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **RESTRICT** (prevents deletion if leave requests exist)
   - Impact: **CRITICAL** - Must delete SiLeaveRequests first

6. **StockMovements** (Nullable FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **SET NULL** (nullable, will be set to null)
   - Impact: Stock movements will have `ServiceInstallerId = null`

7. **StockLocations** (Nullable FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **SET NULL** (nullable, will be set to null)
   - Impact: Stock locations will have `ServiceInstallerId = null`

8. **SiRatePlans** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **RESTRICT** (prevents deletion if rate plans exist)
   - Impact: **CRITICAL** - Must delete SiRatePlans first

9. **GponSiCustomRate** (Required FK)
   - Field: `ServiceInstallerId`
   - Delete Behavior: **RESTRICT** (prevents deletion if custom rates exist)
   - Impact: **CRITICAL** - Must delete GponSiCustomRate first

10. **JobEarningRecord** (Required FK)
    - Field: `ServiceInstallerId`
    - Delete Behavior: **RESTRICT** (prevents deletion if earning records exist)
    - Impact: **CRITICAL** - Must delete JobEarningRecord first

11. **PayrollLine** (Required FK)
    - Field: `ServiceInstallerId`
    - Delete Behavior: **RESTRICT** (prevents deletion if payroll lines exist)
    - Impact: **CRITICAL** - Must delete PayrollLine first

12. **PnlDetailPerOrder** (Required FK)
    - Field: `ServiceInstallerId`
    - Delete Behavior: **RESTRICT** (prevents deletion if PNL records exist)
    - Impact: **CRITICAL** - Must delete PnlDetailPerOrder first

**References:**
- `User` (via `UserId`) - Optional link to user account
- `Department` (via `DepartmentId`) - Optional department assignment

---

### CURRENT DATA

**Row Count:** Unknown (requires database query)  
**Seed Data Count:** 19 installers (in DatabaseSeeder)

**Sample Seed Data:**
1. K. MARIAPPAN A/L KUPPATHAN (KM Siva) - +60 17-676 7625
2. SARAVANAN A/L I. CHINNIAH (Solo) - +60 16-392 3026
3. MUNIANDY A/L SOORINARAYANAN (Mani) - +60 16-319 8867
4. YELLESHUA JEEVAN A/L AROKKIASAMY (Jeevan) - +60 16-453 2305
5. RAVEEN NAIR A/L K RAHMAN (Raveen) - +60 11-1081 8049
... (14 more installers)

**Seed Data Location:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`  
**Method:** `SeedGponServiceInstallersAsync` (lines 695-756)

---

### SEED DATA LOCATIONS

**Migration Files:**
- ❌ **No seed data in migration files** - All seed data is in `DatabaseSeeder.cs`

**HasData() Configuration:**
- ❌ **No HasData() configuration** - Entity configuration does not use `HasData()`

**Seed Classes:**
- ✅ **DatabaseSeeder.cs** - Method: `SeedGponServiceInstallersAsync`
  - Location: `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`
  - Lines: 695-756
  - Called from: `SeedAsync()` method (line 100)
  - Condition: Only seeds if no installers exist (checks with `IgnoreQueryFilters()`)

---

### DEPENDENCIES

**Buildings:**
- ❌ **No reference** - Buildings do not reference ServiceInstallers

**Orders:**
- ✅ **Yes** - Field: `AssignedSiId` (nullable)
  - Delete Behavior: SET NULL
  - Impact: Orders will have `AssignedSiId = null` after deletion

**Other Critical Dependencies:**
- ✅ **ScheduledSlots** - Required FK (RESTRICT)
- ✅ **SiAvailabilities** - Required FK (RESTRICT)
- ✅ **SiLeaveRequests** - Required FK (RESTRICT)
- ✅ **SiRatePlans** - Required FK (RESTRICT)
- ✅ **GponSiCustomRate** - Required FK (RESTRICT)
- ✅ **JobEarningRecord** - Required FK (RESTRICT)
- ✅ **PayrollLine** - Required FK (RESTRICT)
- ✅ **PnlDetailPerOrder** - Required FK (RESTRICT)
- ✅ **ServiceInstallerContacts** - Required FK (CASCADE - will auto-delete)

---

### PARSER USAGE

**Excel Parser:**
- ❌ **Not Used** - No references to ServiceInstaller in Excel parser

**Email Parser:**
- ❌ **Not Used** - No references to ServiceInstaller in Email parser

**Impact:** ✅ **SAFE** - Removing installer data will NOT break parsers

---

## SERVICE INSTALLER REMOVAL PLAN
==============================

### PRE-REMOVAL CHECKS

- [ ] 1. Verify backup exists
- [ ] 2. Document current installer count: `SELECT COUNT(*) FROM "ServiceInstallers";`
- [ ] 3. Check if any critical orders reference installers: `SELECT COUNT(*) FROM "Orders" WHERE "AssignedSiId" IS NOT NULL;`
- [ ] 4. Check dependent records:
  ```sql
  SELECT COUNT(*) FROM "ScheduledSlots";
  SELECT COUNT(*) FROM "SiAvailabilities";
  SELECT COUNT(*) FROM "SiLeaveRequests";
  SELECT COUNT(*) FROM "SiRatePlans";
  SELECT COUNT(*) FROM "GponSiCustomRate";
  SELECT COUNT(*) FROM "JobEarningRecord";
  SELECT COUNT(*) FROM "PayrollLine";
  SELECT COUNT(*) FROM "PnlDetailPerOrder";
  SELECT COUNT(*) FROM "ServiceInstallerContacts";
  ```
- [ ] 5. Confirm no parsers will break (✅ Already verified - no parser usage)

---

### REMOVAL STEPS

#### Step 1: Handle Dependent Data (CRITICAL - Must be done first)

**⚠️ WARNING:** The following tables have **REQUIRED** foreign keys with **RESTRICT** delete behavior. These MUST be deleted first, otherwise ServiceInstaller deletion will fail.

**1.1 Delete ScheduledSlots:**
```sql
DELETE FROM "ScheduledSlots";
```

**1.2 Delete SiAvailabilities:**
```sql
DELETE FROM "SiAvailabilities";
```

**1.3 Delete SiLeaveRequests:**
```sql
DELETE FROM "SiLeaveRequests";
```

**1.4 Delete SiRatePlans:**
```sql
DELETE FROM "SiRatePlans";
```

**1.5 Delete GponSiCustomRate:**
```sql
DELETE FROM "GponSiCustomRate";
```

**1.6 Delete JobEarningRecord:**
```sql
DELETE FROM "JobEarningRecord";
```

**1.7 Delete PayrollLine:**
```sql
DELETE FROM "PayrollLine";
```

**1.8 Delete PnlDetailPerOrder:**
```sql
DELETE FROM "PnlDetailPerOrder";
```

**1.9 Handle Orders (SET NULL - automatic):**
- No action needed - `AssignedSiId` is nullable and will be set to null automatically
- Optional verification: `UPDATE "Orders" SET "AssignedSiId" = NULL WHERE "AssignedSiId" IS NOT NULL;`

**1.10 Handle StockMovements (SET NULL - automatic):**
- No action needed - `ServiceInstallerId` is nullable and will be set to null automatically

**1.11 Handle StockLocations (SET NULL - automatic):**
- No action needed - `ServiceInstallerId` is nullable and will be set to null automatically

**1.12 ServiceInstallerContacts (CASCADE - automatic):**
- No action needed - Will be automatically deleted when ServiceInstaller is deleted

---

#### Step 2: Remove Database Data

**2.1 Delete all ServiceInstaller records:**
```sql
DELETE FROM "ServiceInstallers";
```

**2.2 Verify deletion:**
```sql
SELECT COUNT(*) as remaining_installers FROM "ServiceInstallers";
-- Expected: 0
```

---

#### Step 3: Remove Seed Data from DatabaseSeeder

**File:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**3.1 Remove method call:**
- **Line 100:** Remove or comment out:
  ```csharp
  await SeedGponServiceInstallersAsync(null, gponDepartment?.Id);
  ```

**3.2 Remove entire seed method (optional - can keep for future use):**
- **Lines 695-756:** Remove or comment out the entire `SeedGponServiceInstallersAsync` method
- **Alternative:** Keep method but add early return to prevent seeding:
  ```csharp
  private async Task SeedGponServiceInstallersAsync(Guid? companyId, Guid? departmentId)
  {
      // Seed data removed - installers will be imported separately
      return;
  }
  ```

---

#### Step 4: Remove HasData() Configuration

**Status:** ✅ **Not Applicable** - No `HasData()` configuration exists for ServiceInstaller

---

#### Step 5: Create Cleanup Migration (Optional but Recommended)

**Purpose:** Create a migration that documents the data removal and ensures clean state

**Command:**
```bash
dotnet ef migrations add RemoveServiceInstallerSeedData --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

**Note:** This migration will be empty (no schema changes), but you can add SQL comments documenting the data removal.

---

#### Step 6: Apply Changes

**6.1 Run migration (if Step 5 was done):**
```bash
dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

**6.2 Verify database state:**
```sql
-- Check ServiceInstallers is empty
SELECT COUNT(*) FROM "ServiceInstallers";
-- Expected: 0

-- Check ServiceInstallerContacts is empty (CASCADE)
SELECT COUNT(*) FROM "ServiceInstallerContacts";
-- Expected: 0

-- Check Orders have null AssignedSiId
SELECT COUNT(*) FROM "Orders" WHERE "AssignedSiId" IS NOT NULL;
-- Expected: 0

-- Check dependent tables are empty
SELECT COUNT(*) FROM "ScheduledSlots";
SELECT COUNT(*) FROM "SiAvailabilities";
SELECT COUNT(*) FROM "SiLeaveRequests";
SELECT COUNT(*) FROM "SiRatePlans";
SELECT COUNT(*) FROM "GponSiCustomRate";
SELECT COUNT(*) FROM "JobEarningRecord";
SELECT COUNT(*) FROM "PayrollLine";
SELECT COUNT(*) FROM "PnlDetailPerOrder";
-- All expected: 0
```

---

#### Step 7: Verify Application Still Works

- [ ] Check Buildings still load (no dependency)
- [ ] Check Orders still load (AssignedSiId will be null)
- [ ] Test Excel Parser (no dependency)
- [ ] Test Email Parser (no dependency)
- [ ] Verify ServiceInstaller API endpoints return empty lists

---

## FILES TO MODIFY
===============

### 1. DatabaseSeeder.cs

**File:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**Changes:**
- **Line 100:** Remove or comment out:
  ```csharp
  await SeedGponServiceInstallersAsync(null, gponDepartment?.Id);
  ```

- **Lines 695-756:** Remove or comment out entire `SeedGponServiceInstallersAsync` method

**Alternative (Keep method but disable):**
```csharp
private async Task SeedGponServiceInstallersAsync(Guid? companyId, Guid? departmentId)
{
    // Seed data removed - installers will be imported separately
    _logger.LogInformation("Service installer seeding disabled - installers will be imported separately");
    return;
}
```

---

### 2. (Optional) Create Cleanup Migration

**File:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/YYYYMMDDHHMMSS_RemoveServiceInstallerSeedData.cs`

**Purpose:** Document the data removal (no schema changes needed)

---

## SQL CLEANUP SCRIPT
====================

```sql
-- ============================================
-- SERVICE INSTALLER DATA REMOVAL SCRIPT
-- ============================================
-- WARNING: This script will DELETE all Service Installer data
-- and all dependent records. Run with caution!
-- ============================================

-- Step 1: Backup current data (OPTIONAL but RECOMMENDED)
CREATE TABLE IF NOT EXISTS "ServiceInstallers_Backup" AS 
SELECT * FROM "ServiceInstallers";

CREATE TABLE IF NOT EXISTS "ServiceInstallerContacts_Backup" AS 
SELECT * FROM "ServiceInstallerContacts";

CREATE TABLE IF NOT EXISTS "ScheduledSlots_Backup" AS 
SELECT * FROM "ScheduledSlots";

CREATE TABLE IF NOT EXISTS "SiAvailabilities_Backup" AS 
SELECT * FROM "SiAvailabilities";

CREATE TABLE IF NOT EXISTS "SiLeaveRequests_Backup" AS 
SELECT * FROM "SiLeaveRequests";

CREATE TABLE IF NOT EXISTS "SiRatePlans_Backup" AS 
SELECT * FROM "SiRatePlans";

CREATE TABLE IF NOT EXISTS "GponSiCustomRate_Backup" AS 
SELECT * FROM "GponSiCustomRate";

CREATE TABLE IF NOT EXISTS "JobEarningRecord_Backup" AS 
SELECT * FROM "JobEarningRecord";

CREATE TABLE IF NOT EXISTS "PayrollLine_Backup" AS 
SELECT * FROM "PayrollLine";

CREATE TABLE IF NOT EXISTS "PnlDetailPerOrder_Backup" AS 
SELECT * FROM "PnlDetailPerOrder";

-- Step 2: Handle foreign key dependencies (REQUIRED - Must delete first)
-- These tables have RESTRICT delete behavior, so they must be deleted before ServiceInstallers

DELETE FROM "ScheduledSlots";
DELETE FROM "SiAvailabilities";
DELETE FROM "SiLeaveRequests";
DELETE FROM "SiRatePlans";
DELETE FROM "GponSiCustomRate";
DELETE FROM "JobEarningRecord";
DELETE FROM "PayrollLine";
DELETE FROM "PnlDetailPerOrder";

-- Step 3: Handle nullable foreign keys (SET NULL - optional explicit update)
UPDATE "Orders" 
SET "AssignedSiId" = NULL 
WHERE "AssignedSiId" IS NOT NULL;

UPDATE "StockMovements" 
SET "ServiceInstallerId" = NULL 
WHERE "ServiceInstallerId" IS NOT NULL;

UPDATE "StockLocations" 
SET "ServiceInstallerId" = NULL 
WHERE "ServiceInstallerId" IS NOT NULL;

-- Step 4: Delete ServiceInstallerContacts (CASCADE - will auto-delete, but explicit for clarity)
DELETE FROM "ServiceInstallerContacts";

-- Step 5: Delete all ServiceInstaller data
DELETE FROM "ServiceInstallers";

-- Step 6: Verify deletion
SELECT COUNT(*) as remaining_installers FROM "ServiceInstallers";
-- Expected: 0

SELECT COUNT(*) as remaining_contacts FROM "ServiceInstallerContacts";
-- Expected: 0

SELECT COUNT(*) as orders_with_installer 
FROM "Orders" 
WHERE "AssignedSiId" IS NOT NULL;
-- Expected: 0

SELECT COUNT(*) as dependent_records FROM "ScheduledSlots";
SELECT COUNT(*) as dependent_records FROM "SiAvailabilities";
SELECT COUNT(*) as dependent_records FROM "SiLeaveRequests";
SELECT COUNT(*) as dependent_records FROM "SiRatePlans";
SELECT COUNT(*) as dependent_records FROM "GponSiCustomRate";
SELECT COUNT(*) as dependent_records FROM "JobEarningRecord";
SELECT COUNT(*) as dependent_records FROM "PayrollLine";
SELECT COUNT(*) as dependent_records FROM "PnlDetailPerOrder";
-- All expected: 0

-- ============================================
-- REMOVAL COMPLETE
-- ============================================
```

---

## ROLLBACK PROCEDURE
====================

If you need to restore Service Installer data:

### Option 1: Restore from backup tables
```sql
-- Restore ServiceInstallers
INSERT INTO "ServiceInstallers" 
SELECT * FROM "ServiceInstallers_Backup";

-- Restore ServiceInstallerContacts
INSERT INTO "ServiceInstallerContacts" 
SELECT * FROM "ServiceInstallerContacts_Backup";

-- Restore dependent tables (if needed)
INSERT INTO "ScheduledSlots" 
SELECT * FROM "ScheduledSlots_Backup";

-- ... (repeat for other backup tables)
```

### Option 2: Restore from database backup
```bash
# PostgreSQL restore command (adjust path and database name)
pg_restore -d cephasops -U postgres backup_file.dump
```

### Option 3: Re-run DatabaseSeeder
- Re-enable `SeedGponServiceInstallersAsync` in DatabaseSeeder.cs
- Run application - seeder will populate installers if table is empty

---

## IMPACT ASSESSMENT
==================

### ✅ SAFE TO REMOVE

1. **Parsers:** ✅ No impact - Excel and Email parsers do not use ServiceInstallers
2. **Buildings:** ✅ No impact - Buildings do not reference ServiceInstallers
3. **Orders:** ✅ Safe - `AssignedSiId` is nullable, will be set to null
4. **StockMovements:** ✅ Safe - `ServiceInstallerId` is nullable, will be set to null
5. **StockLocations:** ✅ Safe - `ServiceInstallerId` is nullable, will be set to null

### ⚠️ CRITICAL DEPENDENCIES (Must delete first)

1. **ScheduledSlots:** ⚠️ Must delete first (RESTRICT FK)
2. **SiAvailabilities:** ⚠️ Must delete first (RESTRICT FK)
3. **SiLeaveRequests:** ⚠️ Must delete first (RESTRICT FK)
4. **SiRatePlans:** ⚠️ Must delete first (RESTRICT FK)
5. **GponSiCustomRate:** ⚠️ Must delete first (RESTRICT FK)
6. **JobEarningRecord:** ⚠️ Must delete first (RESTRICT FK)
7. **PayrollLine:** ⚠️ Must delete first (RESTRICT FK)
8. **PnlDetailPerOrder:** ⚠️ Must delete first (RESTRICT FK)

### ✅ AUTO-DELETED (CASCADE)

1. **ServiceInstallerContacts:** ✅ Will be automatically deleted (CASCADE)

---

## SUMMARY
=========

### Entity Status
- ✅ ServiceInstaller entity exists
- ✅ Table: `ServiceInstallers`
- ✅ Seed data: 19 installers in DatabaseSeeder.cs

### Dependencies
- ⚠️ **8 Critical** (RESTRICT - must delete first)
- ✅ **3 Safe** (SET NULL - automatic)
- ✅ **1 Auto-delete** (CASCADE)

### Parser Impact
- ✅ **No impact** - Parsers do not use ServiceInstallers

### Removal Strategy
1. Delete dependent records first (8 tables with RESTRICT FKs)
2. Delete ServiceInstallerContacts (CASCADE)
3. Delete ServiceInstallers
4. Update nullable FKs (automatic, but can be explicit)
5. Remove seed data from DatabaseSeeder.cs
6. Verify all tables are empty

### Files to Modify
- `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs` (remove seed method/call)

---

**Status:** ✅ Investigation Complete - Ready for Removal  
**Risk Level:** ⚠️ Medium (due to 8 critical dependencies that must be deleted first)  
**Parser Impact:** ✅ None

