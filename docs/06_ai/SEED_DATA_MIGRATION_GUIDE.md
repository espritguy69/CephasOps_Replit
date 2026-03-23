# Seed Data Migration Guide - Move to PostgreSQL Direct Management

**Date:** 2025-01-05  
**Purpose:** Step-by-step guide to migrate from C# seeding to PostgreSQL direct management

---

## Overview

This guide walks you through:
1. Removing all C# seeding code
2. Removing SQL migration seeds
3. Importing PostgreSQL seed scripts
4. Verifying the migration
5. Testing the application

---

## Prerequisites

- ✅ Database migrations are up-to-date
- ✅ PostgreSQL client tools installed (psql, pgAdmin, or DBeaver)
- ✅ Database backup created
- ✅ Access to database connection string

---

## Step 1: Backup Database

**CRITICAL:** Always backup before making changes.

```bash
# Using pg_dump
pg_dump -h localhost -U postgres -d cephasops -F c -f backup_before_seed_removal_$(date +%Y%m%d_%H%M%S).dump

# Or using PowerShell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
pg_dump -h localhost -U postgres -d cephasops -F c -f "backup_before_seed_removal_$timestamp.dump"
```

**Verify backup:**
```bash
pg_restore --list backup_before_seed_removal_*.dump | head -20
```

---

## Step 2: Extract Current Seed Data (Optional)

**Purpose:** Verify SQL scripts match current data

```sql
-- Export current seed data for reference
\copy (SELECT * FROM "Companies" WHERE "ShortName" = 'Cephas') TO 'companies_export.csv' CSV HEADER;
\copy (SELECT * FROM "Roles") TO 'roles_export.csv' CSV HEADER;
\copy (SELECT * FROM "Users" WHERE "Email" IN ('simon@cephas.com.my', 'finance@cephas.com.my')) TO 'users_export.csv' CSV HEADER;
\copy (SELECT * FROM "OrderTypes") TO 'order_types_export.csv' CSV HEADER;
\copy (SELECT * FROM "Materials") TO 'materials_export.csv' CSV HEADER;
```

---

## Step 3: Remove C# Seeding Code

### 3.1 Delete Seed Classes

```bash
# Delete DatabaseSeeder.cs
rm backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs

# Delete DocumentPlaceholderSeeder.cs
rm backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs
```

**Or manually delete:**
- `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`
- `backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs`

### 3.2 Remove Seeding Invocation from Program.cs

**File:** `backend/src/CephasOps.Api/Program.cs`

**Remove lines 638-666:**
```csharp
// Seed database with default data
using (var scope = app.Services.CreateScope())
{
    // ... entire seeding block ...
}
```

**After removal, Program.cs should end with:**
```csharp
app.MapControllers();

app.Run();
```

### 3.3 Remove Unused Using Statements

**File:** `backend/src/CephasOps.Api/Program.cs`

**Remove if present:**
```csharp
using CephasOps.Infrastructure.Persistence.Seeders;
```

---

## Step 4: Remove SQL Migration Seeds

**Note:** These files can be deleted entirely as seed data is now managed via PostgreSQL scripts.

### 4.1 Files to Delete

```bash
# Navigate to migrations directory
cd backend/src/CephasOps.Infrastructure/Persistence/Migrations

# Delete seed SQL files
rm SeedMovementTypesAndLocationTypes.sql
rm SeedGuardConditionsAndSideEffects.sql
rm SeedGuardConditionsAndSideEffects_PostgreSQL.sql
rm 20251216210000_AddEmailSendingTemplates.sql
rm 20251216190000_AddPaymentAdviceParserTemplate.sql
rm 20251216180000_AddRescheduleParserTemplate.sql
rm 20251216170000_AddCustomerUncontactableParserTemplate.sql
rm 20251216160000_AddRfbParserTemplate.sql
rm 20251216150000_AddWithdrawalParserTemplate.sql
rm 20251216240000_EnsureRescheduleEmailTemplatesExist.sql
```

**Or archive them:**
```bash
mkdir -p backend/scripts/archived-migrations
mv backend/src/CephasOps.Infrastructure/Persistence/Migrations/Seed*.sql backend/scripts/archived-migrations/
mv backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216*.sql backend/scripts/archived-migrations/
```

---

## Step 5: Import PostgreSQL Seed Scripts

### 5.1 Verify Scripts Exist

```bash
# Check all scripts are present
ls -la backend/scripts/postgresql-seeds/*.sql

# Expected files:
# 01_system_data.sql
# 02_reference_data.sql
# 03_master_data.sql
# 04_configuration_data.sql
# 05_inventory_data.sql
# 06_document_placeholders.sql
```

### 5.2 Set Connection Details

**Option 1: Environment Variables**
```bash
export PGHOST=localhost
export PGPORT=5432
export PGDATABASE=cephasops
export PGUSER=postgres
export PGPASSWORD=J@saw007
```

**Option 2: Connection String**
```bash
export DATABASE_URL="Host=localhost;Port=5432;Database=cephasops;Username=cephasops_app;Password=YOUR_PASSWORD"
```

### 5.3 Execute Scripts in Order

**CRITICAL:** Execute in exact order due to foreign key dependencies.

```bash
cd backend/scripts/postgresql-seeds

# Execute in order
psql -f 01_system_data.sql
psql -f 02_reference_data.sql
psql -f 03_master_data.sql
psql -f 04_configuration_data.sql
psql -f 05_inventory_data.sql
psql -f 06_document_placeholders.sql
```

**Or using connection string:**
```bash
psql "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007" -f 01_system_data.sql
psql "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007" -f 02_reference_data.sql
# ... continue for all scripts
```

**Or using PowerShell:**
```powershell
$env:PGPASSWORD = "J@saw007"
psql -h localhost -U postgres -d cephasops -f backend/scripts/postgresql-seeds/01_system_data.sql
psql -h localhost -U postgres -d cephasops -f backend/scripts/postgresql-seeds/02_reference_data.sql
# ... continue for all scripts
```

### 5.4 Using pgAdmin or DBeaver

1. Open SQL editor
2. Load `01_system_data.sql`
3. Execute (F5 or Execute button)
4. Verify success message
5. Repeat for scripts 02-06 in order

---

## Step 6: Verify Data Loaded

### 6.1 Quick Verification Query

```sql
-- Run this query to verify all data loaded
SELECT 
    (SELECT COUNT(*) FROM "Companies") as companies,
    (SELECT COUNT(*) FROM "Roles") as roles,
    (SELECT COUNT(*) FROM "Users") as users,
    (SELECT COUNT(*) FROM "UserRoles") as user_roles,
    (SELECT COUNT(*) FROM "Departments" WHERE "Code" = 'GPON') as gpon_departments,
    (SELECT COUNT(*) FROM "OrderTypes") as order_types,
    (SELECT COUNT(*) FROM "OrderCategories") as order_categories,
    (SELECT COUNT(*) FROM "BuildingTypes") as building_types,
    (SELECT COUNT(*) FROM "SplitterTypes") as splitter_types,
    (SELECT COUNT(*) FROM "Materials") as materials,
    (SELECT COUNT(*) FROM "MaterialCategories") as material_categories,
    (SELECT COUNT(*) FROM "ParserTemplates") as parser_templates,
    (SELECT COUNT(*) FROM "GuardConditionDefinitions") as guard_conditions,
    (SELECT COUNT(*) FROM "SideEffectDefinitions") as side_effects,
    (SELECT COUNT(*) FROM "GlobalSettings") as global_settings,
    (SELECT COUNT(*) FROM "MovementTypes") as movement_types,
    (SELECT COUNT(*) FROM "LocationTypes") as location_types,
    (SELECT COUNT(*) FROM "DocumentPlaceholderDefinitions") as document_placeholders;
```

**Expected Results:**
- companies: 1
- roles: 4-5 (SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager)
- users: 2
- user_roles: 2
- gpon_departments: 1
- order_types: 5
- order_categories: 4
- building_types: 15
- splitter_types: 3
- materials: ~50+
- material_categories: Variable (8+)
- parser_templates: 9+
- guard_conditions: 10
- side_effects: 5
- global_settings: ~30+
- movement_types: 11
- location_types: 6
- document_placeholders: ~158

### 6.2 Verify User Login

```sql
-- Check admin user exists and is active
SELECT "Id", "Name", "Email", "IsActive" 
FROM "Users" 
WHERE "Email" = 'simon@cephas.com.my';

-- Check user has SuperAdmin role
SELECT u."Email", r."Name" as "RoleName"
FROM "Users" u
JOIN "UserRoles" ur ON u."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'simon@cephas.com.my';
```

### 6.3 Test Application Login

1. Start backend application
2. Navigate to login page
3. Login with:
   - Email: `simon@cephas.com.my`
   - Password: `J@saw007`
4. Verify login succeeds
5. Verify user has SuperAdmin access

---

## Step 7: Test Application Functionality

### 7.1 Basic Functionality Tests

- ✅ Login works
- ✅ Dashboard loads
- ✅ Orders page loads
- ✅ Materials page loads
- ✅ Settings page loads
- ✅ Document generation works (if applicable)

### 7.2 Data Integrity Tests

```sql
-- Verify foreign key relationships
SELECT COUNT(*) FROM "OrderTypes" WHERE "DepartmentId" IS NOT NULL;
SELECT COUNT(*) FROM "Materials" WHERE "DepartmentId" IS NOT NULL;
SELECT COUNT(*) FROM "UserRoles" WHERE "RoleId" IS NOT NULL;
```

---

## Step 8: Handle Existing Data (If Applicable)

### 8.1 If Data Already Exists

**Scripts are idempotent** - they use `ON CONFLICT DO NOTHING` or existence checks.

**If you want to refresh data:**
```sql
-- WARNING: This will delete existing seed data
-- Only run if you want to start fresh

-- Delete in reverse dependency order
DELETE FROM "DocumentPlaceholderDefinitions";
DELETE FROM "LocationTypes";
DELETE FROM "MovementTypes";
DELETE FROM "GlobalSettings";
DELETE FROM "SideEffectDefinitions";
DELETE FROM "GuardConditionDefinitions";
DELETE FROM "ParserTemplates";
DELETE FROM "MaterialCategories";
DELETE FROM "Materials";
DELETE FROM "SplitterTypes";
DELETE FROM "BuildingTypes";
DELETE FROM "OrderCategories";
DELETE FROM "OrderTypes";
DELETE FROM "DepartmentMemberships";
DELETE FROM "UserRoles";
DELETE FROM "Users" WHERE "Email" IN ('simon@cephas.com.my', 'finance@cephas.com.my');
DELETE FROM "Departments" WHERE "Code" = 'GPON';
DELETE FROM "Roles" WHERE "Name" IN ('SuperAdmin', 'Director', 'HeadOfDepartment', 'Supervisor', 'FinanceManager');
DELETE FROM "Companies" WHERE "ShortName" = 'Cephas';

-- Then re-run all seed scripts
```

---

## Step 9: Update Documentation

### 9.1 Update README Files

Update any documentation that references:
- DatabaseSeeder
- Automatic seeding on startup
- Seed data location

### 9.2 Update Developer Onboarding

Document that seed data is now managed via PostgreSQL scripts in:
- `backend/scripts/postgresql-seeds/README.md`

---

## Step 10: Commit Changes

```bash
# Stage all changes
git add backend/scripts/postgresql-seeds/
git add docs/06_ai/SEED_DATA_REMOVAL_PLAN.md
git add docs/06_ai/SEED_DATA_MIGRATION_GUIDE.md

# Remove deleted files
git rm backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs
git rm backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs

# Commit
git commit -m "feat: Move seed data from C# to PostgreSQL direct management

- Remove DatabaseSeeder.cs and DocumentPlaceholderSeeder.cs
- Remove seeding invocation from Program.cs
- Create PostgreSQL seed scripts organized by category
- Update documentation for manual seed data management
- All seed data now managed via SQL scripts in backend/scripts/postgresql-seeds/"
```

---

## Rollback Plan

If issues occur after migration:

### Option 1: Restore Database Backup

```bash
# Restore backup
pg_restore -h localhost -U postgres -d cephasops -c backup_before_seed_removal_*.dump

# Verify restore
psql -h localhost -U postgres -d cephasops -c "SELECT COUNT(*) FROM \"Users\";"
```

### Option 2: Restore C# Seeding Code

```bash
# Restore from Git
git checkout HEAD -- backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs
git checkout HEAD -- backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs
git checkout HEAD -- backend/src/CephasOps.Api/Program.cs

# Rebuild and restart application
cd backend/src/CephasOps.Api
dotnet build
dotnet run
```

---

## Troubleshooting

### Issue: Script fails with "relation does not exist"

**Solution:** Ensure database migrations are up-to-date
```bash
cd backend/src/CephasOps.Api
dotnet ef database update
```

### Issue: Duplicate key violation

**Solution:** Scripts use `ON CONFLICT DO NOTHING` - this should not occur. If it does:
1. Check for existing data
2. Remove conflicting data if needed
3. Re-run script

### Issue: Foreign key constraint violation

**Solution:** Ensure scripts are run in correct order (01 → 06)

### Issue: Password hash mismatch

**Solution:** Verify password hash calculation matches C# implementation
```powershell
# Run hash calculation script
powershell -File backend/scripts/calculate-password-hash.ps1
```

### Issue: Application won't start after removing seeding

**Solution:** 
1. Check Program.cs - ensure seeding block is completely removed
2. Check for compilation errors
3. Verify database connection works
4. Check application logs

---

## Post-Migration Checklist

- [ ] Database backup created
- [ ] C# seeding code removed
- [ ] Program.cs seeding invocation removed
- [ ] SQL migration seed files removed/archived
- [ ] PostgreSQL seed scripts executed in order
- [ ] Data verification queries pass
- [ ] Application starts successfully
- [ ] Login works with seeded users
- [ ] Basic functionality tests pass
- [ ] Documentation updated
- [ ] Changes committed to Git

---

## Future Updates

### Adding New Seed Data

1. Edit appropriate SQL script file
2. Add INSERT statements with `ON CONFLICT DO NOTHING`
3. Test in development
4. Commit to version control
5. Run in production

### Updating Existing Data

1. Create new SQL file: `YYYYMMDD_Update_Description.sql`
2. Use UPDATE statements
3. Test in development
4. Run in production

### Managing Across Environments

- **Development:** Run scripts manually or via migration tool
- **Staging:** Run scripts as part of deployment
- **Production:** Run scripts manually with approval process

---

## Support

If you encounter issues:
1. Check this guide's troubleshooting section
2. Review PostgreSQL seed script logs
3. Check application logs
4. Verify database state with verification queries
5. Restore from backup if needed

---

**Last Updated:** 2025-01-05

