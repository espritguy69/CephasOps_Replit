# Seed Data Removal Plan - Move to PostgreSQL Direct Management

**Date:** 2025-01-05  
**Objective:** Remove all C# seeding code and SQL migration seeds, manage all data directly in PostgreSQL

---

## 1. Removal Checklist

### 1.1 Files to Delete

```
[ ] backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs
[ ] backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs
```

### 1.2 Code to Remove from Program.cs

**File:** `backend/src/CephasOps.Api/Program.cs`  
**Lines to Remove:** 638-666 (seeding invocation block)

**Current Code:**
```csharp
// Seed database with default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
        var programLogger = services.GetRequiredService<ILogger<Program>>();
        
        programLogger.LogInformation("Starting database seeding...");
        var seeder = new DatabaseSeeder(context, logger);
        await seeder.SeedAsync();
        programLogger.LogInformation("Database seeding completed successfully.");
        
        // Seed document placeholder definitions
        programLogger.LogInformation("Starting document placeholder seeding...");
        var placeholderLogger = services.GetRequiredService<ILogger<DocumentPlaceholderSeeder>>();
        var placeholderSeeder = new DocumentPlaceholderSeeder(context, placeholderLogger);
        await placeholderSeeder.SeedAsync();
        programLogger.LogInformation("Document placeholder seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database. Application will continue but login may not work.");
        // Don't throw - allow app to start even if seeding fails
    }
}
```

**Action:** Delete entire block (lines 638-666)

### 1.3 SQL Migration Files to Remove/Update

**Files with INSERT statements to remove:**

```
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/SeedMovementTypesAndLocationTypes.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/SeedGuardConditionsAndSideEffects.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/SeedGuardConditionsAndSideEffects_PostgreSQL.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216210000_AddEmailSendingTemplates.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216190000_AddPaymentAdviceParserTemplate.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216180000_AddRescheduleParserTemplate.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216170000_AddCustomerUncontactableParserTemplate.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216160000_AddRfbParserTemplate.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216150000_AddWithdrawalParserTemplate.sql
[ ] backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216240000_EnsureRescheduleEmailTemplatesExist.sql
```

**Note:** These files can be deleted entirely as seed data will be managed via PostgreSQL scripts.

### 1.4 Using Statements to Remove from Program.cs

**Remove these using statements if no longer needed:**
```csharp
using CephasOps.Infrastructure.Persistence.Seeders;  // Only if DocumentPlaceholderSeeder is removed
```

---

## 2. DatabaseSeeder Methods to Extract

### 2.1 Methods in DatabaseSeeder.cs

| Method | Table(s) | Records | Notes |
|--------|----------|---------|-------|
| `SeedDefaultCompanyAsync()` | `Companies` | 1 | Default company "Cephas" |
| `SeedSuperAdminRoleAsync()` | `Roles` | 1 | SuperAdmin role |
| `SeedDirectorRoleAsync()` | `Roles` | 1 | Director role |
| `SeedHeadOfDepartmentRoleAsync()` | `Roles` | 1 | HeadOfDepartment role |
| `SeedSupervisorRoleAsync()` | `Roles` | 1 | Supervisor role |
| `SeedDefaultAdminUserAsync()` | `Users`, `UserRoles` | 1 user, 1 role assignment | Admin user with SuperAdmin role |
| `SeedFinanceHodUserAsync()` | `Users`, `UserRoles`, `DepartmentMemberships` | 1 user, 1 role, 1 membership | Finance HOD user |
| `SeedGponDepartmentAsync()` | `Departments` | 1 | GPON department |
| `SeedDefaultOrderTypesAsync()` | `OrderTypes` | 5 | Activation, Modification Indoor/Outdoor, Assurance, VAS |
| `SeedDefaultOrderCategoriesAsync()` | `OrderCategories` | 4 | FTTH, FTTO, FTTR, FTTC |
| `SeedDefaultBuildingTypesAsync()` | `BuildingTypes` | 15 | Various building types |
| `SeedDefaultSplitterTypesAsync()` | `SplitterTypes` | 3 | 1:8, 1:12, 1:32 |
| `SeedDefaultMaterialsAsync()` | `Materials` | ~50+ | ONT, Router, IAD, etc. |
| `SeedMaterialCategoriesFromMaterialsAsync()` | `MaterialCategories` | Variable | Extracted from materials |
| `SeedDefaultMaterialCategoriesAsync()` | `MaterialCategories` | 8 | Default categories if none exist |
| `SeedDefaultParserTemplatesAsync()` | `ParserTemplates` | 9 | TIME order templates |
| `SeedDefaultGuardConditionsAsync()` | `GuardConditionDefinitions` | 10 | Workflow guard conditions |
| `SeedDefaultSideEffectsAsync()` | `SideEffectDefinitions` | 5 | Workflow side effects |
| `SeedSmsWhatsAppGlobalSettingsAsync()` | `GlobalSettings` | ~30+ | SMS/WhatsApp/E-Invoice settings |
| `SeedDefaultMovementTypesAsync()` | `MovementTypes` | 11 | Stock movement types |
| `SeedDefaultLocationTypesAsync()` | `LocationTypes` | 6 | Location types |

### 2.2 Helper Methods

| Method | Purpose | Notes |
|--------|---------|-------|
| `EnsureRoleAsync()` | Helper to create/find roles | Used by Finance HOD seeding |
| `HashPassword()` | Password hashing (SHA256) | **CRITICAL:** Must replicate in SQL or use pre-calculated hash |

---

## 3. DocumentPlaceholderSeeder Methods to Extract

### 3.1 Methods in DocumentPlaceholderSeeder.cs

| Method | Document Type | Placeholders |
|--------|---------------|--------------|
| `GetInvoicePlaceholders()` | Invoice | ~18 placeholders |
| `GetJobDocketPlaceholders()` | JobDocket | ~18 placeholders |
| `GetRmaFormPlaceholders()` | RmaForm | ~10 placeholders |
| `GetPurchaseOrderPlaceholders()` | PurchaseOrder | ~25 placeholders |
| `GetQuotationPlaceholders()` | Quotation | ~25 placeholders |
| `GetBoqPlaceholders()` | BOQ | ~30 placeholders |
| `GetDeliveryOrderPlaceholders()` | DeliveryOrder | ~20 placeholders |
| `GetPaymentReceiptPlaceholders()` | PaymentReceipt | ~12 placeholders |

**Total:** ~158 placeholders across 8 document types

---

## 4. Password Hash Calculation

**Algorithm:** SHA256 with salt "CephasOps_Salt_2024", Base64 encoded

**Passwords to Hash:**
- Admin: `J@saw007` → Hash: `[TO BE CALCULATED]`
- Finance HOD: `E5pr!tg@L` → Hash: `[TO BE CALCULATED]`

**SQL Function for Password Hashing:**
```sql
-- PostgreSQL function to hash password (matches C# implementation)
CREATE OR REPLACE FUNCTION hash_password(password_text TEXT)
RETURNS TEXT AS $$
DECLARE
    salt TEXT := 'CephasOps_Salt_2024';
    salted_password TEXT;
    hash_bytes BYTEA;
BEGIN
    salted_password := password_text || salt;
    hash_bytes := digest(salted_password, 'sha256');
    RETURN encode(hash_bytes, 'base64');
END;
$$ LANGUAGE plpgsql;

-- Requires pgcrypto extension
-- CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

---

## 5. PostgreSQL Script Organization

### 5.1 Script File Structure

```
backend/scripts/postgresql-seeds/
├── 01_system_data.sql          (Companies, Roles, Users, UserRoles, DepartmentMemberships)
├── 02_reference_data.sql       (OrderTypes, OrderCategories, BuildingTypes, SplitterTypes)
├── 03_master_data.sql          (Departments, Materials, MaterialCategories)
├── 04_configuration_data.sql   (ParserTemplates, GuardConditionDefinitions, SideEffectDefinitions, GlobalSettings)
├── 05_inventory_data.sql       (MovementTypes, LocationTypes)
├── 06_document_placeholders.sql (DocumentPlaceholderDefinitions)
└── README.md                    (Execution instructions)
```

### 5.2 Execution Order

**CRITICAL:** Scripts must be executed in this order due to foreign key dependencies:

1. `01_system_data.sql` - Creates company, roles, users (foundation)
2. `02_reference_data.sql` - Creates reference data (depends on company/department)
3. `03_master_data.sql` - Creates departments, materials (depends on company/department)
4. `04_configuration_data.sql` - Creates configuration (depends on company)
5. `05_inventory_data.sql` - Creates inventory types (depends on company)
6. `06_document_placeholders.sql` - Creates placeholders (no dependencies)

---

## 6. Dependency Mapping

### 6.1 Foreign Key Dependencies

| Table | Depends On | Script |
|-------|------------|--------|
| `UserRoles` | `Users`, `Roles`, `Companies` | 01_system_data.sql |
| `DepartmentMemberships` | `Users`, `Departments`, `Companies` | 01_system_data.sql |
| `OrderTypes` | `Companies`, `Departments` | 02_reference_data.sql |
| `OrderCategories` | `Companies`, `Departments` | 02_reference_data.sql |
| `BuildingTypes` | `Companies`, `Departments` | 02_reference_data.sql |
| `SplitterTypes` | `Companies`, `Departments` | 02_reference_data.sql |
| `Materials` | `Companies`, `Departments` | 03_master_data.sql |
| `MaterialCategories` | `Companies` | 03_master_data.sql |
| `ParserTemplates` | `Companies` (optional) | 04_configuration_data.sql |
| `GuardConditionDefinitions` | `Companies` | 04_configuration_data.sql |
| `SideEffectDefinitions` | `Companies` | 04_configuration_data.sql |
| `GlobalSettings` | None | 04_configuration_data.sql |
| `MovementTypes` | `Companies` | 05_inventory_data.sql |
| `LocationTypes` | `Companies` | 05_inventory_data.sql |
| `DocumentPlaceholderDefinitions` | None | 06_document_placeholders.sql |

---

## 7. Migration Steps

### Step 1: Backup Current Database
```sql
-- Create backup before migration
pg_dump -h localhost -U postgres -d cephasops -F c -f backup_before_seed_removal.dump
```

### Step 2: Extract Current Seed Data (Optional)
```sql
-- Export current seed data for reference
-- This helps verify the SQL scripts match current data
```

### Step 3: Remove C# Seeding Code
1. Delete `DatabaseSeeder.cs`
2. Delete `DocumentPlaceholderSeeder.cs`
3. Remove seeding invocation from `Program.cs`
4. Remove unused using statements

### Step 4: Remove SQL Migration Seeds
1. Delete or archive SQL migration files with INSERT statements
2. Keep only schema changes in migrations

### Step 5: Import PostgreSQL Scripts
1. Run scripts in order (01 → 06)
2. Verify data loaded correctly
3. Test application startup

### Step 6: Verification
1. Check all tables have expected row counts
2. Test login with seeded users
3. Verify application functionality

---

## 8. Verification Queries

### 8.1 System Data Verification
```sql
-- Companies
SELECT COUNT(*) FROM "Companies"; -- Expected: 1

-- Roles
SELECT COUNT(*) FROM "Roles"; -- Expected: 4 (SuperAdmin, Director, HeadOfDepartment, Supervisor)

-- Users
SELECT COUNT(*) FROM "Users"; -- Expected: 2 (admin, finance HOD)

-- UserRoles
SELECT COUNT(*) FROM "UserRoles"; -- Expected: 2

-- Departments
SELECT COUNT(*) FROM "Departments" WHERE "Code" = 'GPON'; -- Expected: 1
```

### 8.2 Reference Data Verification
```sql
-- OrderTypes
SELECT COUNT(*) FROM "OrderTypes"; -- Expected: 5

-- OrderCategories
SELECT COUNT(*) FROM "OrderCategories"; -- Expected: 4

-- BuildingTypes
SELECT COUNT(*) FROM "BuildingTypes"; -- Expected: 15

-- SplitterTypes
SELECT COUNT(*) FROM "SplitterTypes"; -- Expected: 3
```

### 8.3 Master Data Verification
```sql
-- Materials
SELECT COUNT(*) FROM "Materials"; -- Expected: ~50+

-- MaterialCategories
SELECT COUNT(*) FROM "MaterialCategories"; -- Expected: Variable (8+)
```

### 8.4 Configuration Data Verification
```sql
-- ParserTemplates
SELECT COUNT(*) FROM "ParserTemplates"; -- Expected: 9+

-- GuardConditionDefinitions
SELECT COUNT(*) FROM "GuardConditionDefinitions"; -- Expected: 10

-- SideEffectDefinitions
SELECT COUNT(*) FROM "SideEffectDefinitions"; -- Expected: 5

-- GlobalSettings
SELECT COUNT(*) FROM "GlobalSettings"; -- Expected: ~30+
```

### 8.5 Inventory Data Verification
```sql
-- MovementTypes
SELECT COUNT(*) FROM "MovementTypes"; -- Expected: 11

-- LocationTypes
SELECT COUNT(*) FROM "LocationTypes"; -- Expected: 6
```

### 8.6 Document Placeholders Verification
```sql
-- DocumentPlaceholderDefinitions
SELECT COUNT(*) FROM "DocumentPlaceholderDefinitions"; -- Expected: ~158

-- By document type
SELECT "DocumentType", COUNT(*) 
FROM "DocumentPlaceholderDefinitions" 
GROUP BY "DocumentType";
```

---

## 9. Future Update Process

### 9.1 Adding New Reference Data
1. Edit appropriate SQL script file
2. Add INSERT statements with `ON CONFLICT DO NOTHING`
3. Test in development
4. Commit to version control
5. Run script in production

### 9.2 Updating Existing Data
1. Create UPDATE statements in new SQL file
2. Use versioned filename: `YYYYMMDD_Update_Description.sql`
3. Test in development
4. Run in production

### 9.3 Managing Across Environments
- **Development:** Run scripts manually or via migration tool
- **Staging:** Run scripts as part of deployment
- **Production:** Run scripts manually with approval process

### 9.4 Version Control
- All SQL scripts in `backend/scripts/postgresql-seeds/`
- Track changes in Git
- Document changes in commit messages
- Maintain changelog in README.md

---

## 10. Rollback Plan

If issues occur after removal:

1. **Restore Database Backup:**
   ```sql
   pg_restore -h localhost -U postgres -d cephasops -c backup_before_seed_removal.dump
   ```

2. **Restore C# Seeding Code:**
   - Restore `DatabaseSeeder.cs` from Git
   - Restore `DocumentPlaceholderSeeder.cs` from Git
   - Restore seeding invocation in `Program.cs`

3. **Verify Application:**
   - Test login
   - Verify all functionality

---

## 11. Notes

- **Password Hashes:** Must match C# implementation exactly
- **UUIDs:** Use `gen_random_uuid()` in PostgreSQL
- **Timestamps:** Use `NOW()` or `CURRENT_TIMESTAMP`
- **Idempotency:** All scripts use `ON CONFLICT DO NOTHING` or existence checks
- **Company ID:** Scripts handle single-company mode (companyId can be NULL or first company)
- **Department ID:** Scripts reference GPON department by code/name

---

**Next Steps:**
1. Generate PostgreSQL SQL scripts (see separate files)
2. Test scripts in development environment
3. Execute removal plan
4. Verify application functionality

