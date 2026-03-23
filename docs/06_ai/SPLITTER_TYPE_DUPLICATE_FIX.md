# Splitter Type Duplicate Fix

## Problem
Splitter Types table shows duplicate entry for 1:8 (appears twice in rows 1 and 2).

## Root Cause Analysis

### Investigation Results

1. **Seed Method Analysis**
   - Location: `DatabaseSeeder.cs` → `SeedDefaultSplitterTypesAsync()`
   - The seed method checks for duplicates using `CompanyId` and `Code`
   - **Issue**: In single-company mode, `CompanyId` can be `null`, causing the duplicate check to fail
   - The check `st.CompanyId == companyId` doesn't work correctly when both are `null`

2. **Database Constraints**
   - **Missing Unique Constraint**: There's only an index on `(CompanyId, Code)` but no unique constraint
   - The index `IX_SplitterTypes_CompanyId_Code` is non-unique
   - This allows duplicate records to be inserted

3. **Service Layer Validation**
   - `SplitterTypeService.CreateSplitterTypeAsync()` checks for duplicates
   - However, if `CompanyId` is `null`, the check may not catch duplicates properly
   - The validation is conditional: `if (companyId.HasValue)` - if null, it doesn't filter by company

4. **Likely Scenario**
   - Seed method ran when `CompanyId` was `null`, creating first 1:8 record
   - Seed method ran again (or user created via API), creating second 1:8 record
   - Since there's no unique constraint, both records were inserted
   - Both records have `Code = '1_8'` but potentially different `CompanyId` values (one null, one not null, or both null)

## Solution

### Step 1: Remove Existing Duplicates

Run the SQL script to identify and remove duplicates:
```sql
-- See: backend/scripts/fix-duplicate-splitter-types.sql
```

This script:
- Identifies all duplicate 1:8 splitter types
- Keeps the oldest record (by `CreatedAt`, then `Id`)
- Soft-deletes the duplicates (sets `IsDeleted = true`)

### Step 2: Add Unique Constraint

Run the SQL script to add a unique constraint:
```sql
-- See: backend/scripts/add-unique-constraint-splitter-types.sql
```

This script:
- Removes any remaining duplicates
- Creates a unique index on `(Code, COALESCE(CompanyId, '00000000-0000-0000-0000-000000000000'))`
- Prevents future duplicates at the database level

### Step 3: Fix Seed Method

**File**: `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**Change**: Updated `SeedDefaultSplitterTypesAsync()` to:
- Check for duplicates by `Code` only (not `CompanyId`)
- Filter out soft-deleted records explicitly
- Log when skipping existing records

**Before**:
```csharp
var exists = await _context.SplitterTypes
    .IgnoreQueryFilters()
    .AnyAsync(st => st.CompanyId == companyId && st.Code == splitterTypeData.Code);
```

**After**:
```csharp
var exists = await _context.SplitterTypes
    .IgnoreQueryFilters()
    .Where(st => st.Code == splitterTypeData.Code && st.IsDeleted == false)
    .AnyAsync();
```

## Prevention

### Database Level
- Unique constraint on `(Code, COALESCE(CompanyId, '00000000-0000-0000-0000-000000000000'))`
- Prevents duplicates even if application logic fails

### Application Level
- Seed method now checks by `Code` only (single-company mode)
- Service layer validation already checks for duplicates
- Both checks filter out soft-deleted records

## Files Modified

1. **backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs**
   - Updated `SeedDefaultSplitterTypesAsync()` duplicate check logic

2. **backend/scripts/find-duplicate-splitter-types.sql** (NEW)
   - SQL queries to identify duplicates

3. **backend/scripts/fix-duplicate-splitter-types.sql** (NEW)
   - SQL script to remove duplicates (keeps oldest)

4. **backend/scripts/add-unique-constraint-splitter-types.sql** (NEW)
   - SQL script to add unique constraint

## Execution Steps

1. **Identify Duplicates**:
   ```bash
   psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/find-duplicate-splitter-types.sql
   ```

2. **Remove Duplicates**:
   ```bash
   psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/fix-duplicate-splitter-types.sql
   ```

3. **Add Unique Constraint**:
   ```bash
   psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/add-unique-constraint-splitter-types.sql
   ```

4. **Verify Fix**:
   ```sql
   SELECT "Id", "Code", "Name", "CompanyId", "IsDeleted", "CreatedAt"
   FROM "SplitterTypes"
   WHERE "Code" = '1_8'
   ORDER BY "CreatedAt" ASC;
   ```
   Should return only 1 active record.

## Expected Outcome

- Only one 1:8 splitter type record remains (the oldest one)
- Duplicate records are soft-deleted
- Unique constraint prevents future duplicates
- Seed method won't create duplicates on subsequent runs

