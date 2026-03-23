# Database Error Fix: Missing OrderCategories Table

**Date:** 2026-01-05  
**Status:** ✅ Migration Fixed - Ready to Apply  
**Priority:** Critical

---

## Problem Statement

**Error:** `Missing OrderCategories Table`  
**Root Cause:** Migration `20251219020647_RenameInstallationTypeToOrderCategory` is pending and hasn't been applied to the database.

---

## Root Cause Analysis

### Migration Status

The migration `20251219020647_RenameInstallationTypeToOrderCategory` exists but is **pending**:

```
20251219020647_RenameInstallationTypeToOrderCategory (Pending)
```

This migration:
1. Creates the `OrderCategories` table
2. Copies data from `InstallationTypes` table (if it exists)
3. Renames columns in related tables (`Orders`, `GponSiJobRates`, etc.)
4. Drops the old `InstallationTypes` table

### Why Migration Failed Initially

The migration was trying to create database objects that already exist:

1. **Columns** (added by manual SQL migration `20251219000000_AddIssueAndSolutionToOrders.sql`):
   - `AdditionalContactNumber` in `ParsedOrderDrafts`
   - `Issue` in `ParsedOrderDrafts` and `Orders`
   - `Solution` in `Orders`

2. **Index** (created by `AddInstallationMethodsTable.sql`):
   - `IX_Buildings_InstallationMethodId` on `Buildings` table

3. **Foreign Keys** (may already exist from previous migrations):
   - `FK_Buildings_InstallationMethods_InstallationMethodId`
   - `FK_Orders_OrderCategories_OrderCategoryId` (if migration was partially applied)

---

## Fix Applied

**File Modified:**
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251219020647_RenameInstallationTypeToOrderCategory.cs`

**Changes:**
1. **Up Migration:** 
   - Changed column additions to check if columns exist before adding
   - Changed index creation to check if index exists before creating
   - Changed foreign key creation to check if constraint exists before creating
2. **Down Migration:** 
   - Changed column drops to check if columns exist before dropping
   - Changed index drops to check if index exists before dropping
   - Changed foreign key drops to check if constraint exists before dropping

### Before (Would Fail)

```csharp
// Step 6: Add new columns for Issue/Solution
migrationBuilder.AddColumn<string>(
    name: "AdditionalContactNumber",
    table: "ParsedOrderDrafts",
    type: "character varying(100)",
    maxLength: 100,
    nullable: true);
// ❌ Fails if column already exists
```

### After (Safe)

```csharp
// Step 6: Add new columns for Issue/Solution (only if they don't exist)
migrationBuilder.Sql(@"
    DO $$
    BEGIN
        -- Add AdditionalContactNumber to ParsedOrderDrafts if it doesn't exist
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = 'ParsedOrderDrafts' AND column_name = 'AdditionalContactNumber'
        ) THEN
            ALTER TABLE ""ParsedOrderDrafts"" 
            ADD COLUMN ""AdditionalContactNumber"" character varying(100) NULL;
        END IF;
        -- ... (similar checks for other columns)
    END $$;
");
```

**✅ Status:** Migration now safely handles existing columns

---

## How to Apply the Fix

### Step 1: Stop the Running API

**If API is running (dotnet watch/run):**
- Press `Ctrl+C` in the terminal running the API
- Or close the terminal/process

**Verify API is stopped:**
```powershell
# Check if process is running
Get-Process -Name "CephasOps.Api" -ErrorAction SilentlyContinue
# If found, stop it:
Stop-Process -Name "CephasOps.Api" -Force
```

### Step 2: Apply the Migration

**Option A: Using EF Core CLI (Recommended)**
```powershell
cd C:\Projects\CephasOps\backend
dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

**Option B: Using SQL Script (If EF Core fails)**
```powershell
# Connect to PostgreSQL and run the migration SQL manually
psql -h localhost -d cephasops -U postgres -f "src/CephasOps.Infrastructure/Persistence/Migrations/20251219020647_RenameInstallationTypeToOrderCategory.sql"
```

**Option C: Manual SQL (If needed)**
```sql
-- Run this in PostgreSQL to create OrderCategories table if migration fails
-- (Only if OrderCategories table doesn't exist)
CREATE TABLE IF NOT EXISTS "OrderCategories" (
    "Id" uuid NOT NULL,
    "DepartmentId" uuid NULL,
    "Name" character varying(100) NOT NULL,
    "Code" character varying(50) NOT NULL,
    "Description" character varying(500) NULL,
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "CompanyId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone NULL,
    "DeletedByUserId" uuid NULL,
    "RowVersion" bytea NULL,
    CONSTRAINT "PK_OrderCategories" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrderCategories_Departments_DepartmentId" FOREIGN KEY ("DepartmentId") 
        REFERENCES "Departments" ("Id") ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_OrderCategories_CompanyId_Code" ON "OrderCategories" ("CompanyId", "Code");
CREATE INDEX IF NOT EXISTS "IX_OrderCategories_CompanyId_DepartmentId" ON "OrderCategories" ("CompanyId", "DepartmentId");
CREATE INDEX IF NOT EXISTS "IX_OrderCategories_CompanyId_IsActive" ON "OrderCategories" ("CompanyId", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_OrderCategories_DepartmentId" ON "OrderCategories" ("DepartmentId");
```

### Step 3: Verify Migration Applied

**Check migration status:**
```powershell
cd C:\Projects\CephasOps\backend
dotnet ef migrations list --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

**Expected output:**
```
...
20251219020647_RenameInstallationTypeToOrderCategory ✅ (No longer "Pending")
```

**Verify table exists:**
```sql
-- Run in PostgreSQL
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name = 'OrderCategories';
-- Should return 1 row
```

### Step 4: Restart API

```powershell
cd C:\Projects\CephasOps\backend
dotnet watch run --project src/CephasOps.Api/CephasOps.Api.csproj
```

---

## What the Migration Does

### 1. Creates OrderCategories Table
- New table to replace `InstallationTypes`
- Same structure as `InstallationTypes` (renamed for clarity)

### 2. Copies Data (If InstallationTypes Exists)
```sql
INSERT INTO "OrderCategories" (...)
SELECT ... FROM "InstallationTypes";
```

### 3. Renames Columns in Related Tables
- `Orders.InstallationTypeId` → `Orders.OrderCategoryId`
- `GponSiJobRates.InstallationTypeId` → `GponSiJobRates.OrderCategoryId`
- `GponSiCustomRates.InstallationTypeId` → `GponSiCustomRates.OrderCategoryId`
- `GponPartnerJobRates.InstallationTypeId` → `GponPartnerJobRates.OrderCategoryId`
- `PnlDetailPerOrders.InstallationType` → `PnlDetailPerOrders.OrderCategory`

### 4. Updates Foreign Keys
- Removes FK to `InstallationTypes`
- Adds FK to `OrderCategories`

### 5. Drops Old Table
- Drops `InstallationTypes` table (after data is copied)

---

## Verification Checklist

After applying the migration, verify:

- [ ] Migration shows as applied (not "Pending")
- [ ] `OrderCategories` table exists in database
- [ ] `InstallationTypes` table is dropped (or empty if kept for reference)
- [ ] `Orders.OrderCategoryId` column exists
- [ ] Foreign key `FK_Orders_OrderCategories_OrderCategoryId` exists
- [ ] API starts without errors
- [ ] No "Missing OrderCategories Table" errors in logs

---

## Troubleshooting

### Error: "Column already exists" or "Index already exists" or "Constraint already exists"
**Solution:** Migration now handles all of these automatically. If you still see these errors, the migration was partially applied. Check which step failed and manually complete it.

### Error: "relation IX_Buildings_InstallationMethodId already exists"
**Solution:** ✅ Fixed - Migration now checks if index exists before creating it.

### Error: "Table InstallationTypes does not exist"
**Solution:** This is OK if you're starting fresh. The migration will skip the data copy step.

### Error: "Foreign key constraint violation"
**Solution:** Check if there are orders referencing non-existent InstallationTypeIds. You may need to clean up orphaned references first.

### Error: "API process is locked"
**Solution:** Stop the API process before running migration:
```powershell
# Find and stop the process
Get-Process | Where-Object {$_.ProcessName -like "*CephasOps*"} | Stop-Process -Force
```

---

## Related Files

**Migration:**
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251219020647_RenameInstallationTypeToOrderCategory.cs`

**Entity:**
- `backend/src/CephasOps.Domain/Orders/Entities/OrderCategory.cs`

**Configuration:**
- `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Orders/OrderCategoryConfiguration.cs`

**Context:**
- `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` (line 63)

---

## Summary

✅ **Migration Fixed:** Now safely handles existing columns  
⏳ **Status:** Ready to apply (requires API to be stopped)  
📋 **Action Required:** Stop API → Apply migration → Restart API

The `OrderCategories` table will be created once the migration is applied successfully.

