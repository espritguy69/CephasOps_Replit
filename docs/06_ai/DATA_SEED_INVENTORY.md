# Data Seed Usage Inventory

**Date:** 2025-01-05  
**Purpose:** Complete inventory of all data seeding mechanisms in the CephasOps codebase

---

## Executive Summary

**PostgreSQL is now the single source of truth for all seed data.** All reference data is seeded via SQL migrations.

The CephasOps codebase uses **PostgreSQL SQL migrations** for seeding:
1. **PostgreSQL SQL Migration** (primary mechanism) - SQL embedded in `20260106014834_SeedAllReferenceData.cs`
2. **DatabaseSeeder class** - **DISABLED** (kept for reference only)
3. **DocumentPlaceholderSeeder class** - **DISABLED** (kept for reference only)
4. **No HasData() configurations** - Entity configurations do not use EF Core HasData()
5. **No InsertData in migrations** - C# migrations use raw SQL via `migrationBuilder.Sql()`

---

## 1. Migration Files with Seed Data (SQL INSERT INTO)

### 1.1 Primary Seed Migration (PostgreSQL)

**File:** `20260106014834_SeedAllReferenceData.cs` (SQL embedded directly in C# migration)

**Status:** ✅ **PRIMARY SEED MECHANISM** - All reference data is seeded via this migration

**Migration Applied:** 2026-01-06

**Tables Seeded:**
- Companies (1)
- Roles (5: SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager)
- Users (2: Admin, Finance HOD)
- UserRoles (2)
- Departments (1: GPON)
- DepartmentMemberships (1)
- OrderTypes (5)
- OrderCategories (4)
- BuildingTypes (19)
- SplitterTypes (3)
- Skills (33)
- ParserTemplates (14)
- GuardConditionDefinitions (10)
- SideEffectDefinitions (5)
- GlobalSettings (~30+)
- MovementTypes (11)
- LocationTypes (6)
- MaterialCategories (8, if none exist)

**Key Features:**
- Idempotent (uses `WHERE NOT EXISTS` and `ON CONFLICT DO NOTHING`)
- Runs automatically when EF Core migrations are applied
- PostgreSQL is the single source of truth

### 1.2 Other SQL Migration Files with Seed Data

**Note:** The following files contain additional seed data (not consolidated into the main seed migration):

| File | Tables Seeded | Description |
|------|---------------|-------------|
| `20251216210000_AddEmailSendingTemplates.sql` | `EmailTemplates` | Seeds 2 email templates (Customer Uncontactable, Reschedule) |
| `20251216240000_EnsureRescheduleEmailTemplatesExist.sql` | `EmailTemplates` | Ensures reschedule email templates exist |
| `20251207_MigrateMaterialPartnerIdToJoinTable.sql` | (Data migration only) | Migrates existing data, no new seed data |
| `AddVipEmailsAndParserTemplates.sql` | `VipGroups`, `VipEmails`, `ParserTemplates` | Table creation + seed data |
| `AddPnlTypesAssetsAndAccounting.sql` | (Table creation only) | Creates tables, no seed data |
| `AddInstallationMethodsTable.sql` | `InstallationMethods` | Table creation + possible seed data |
| `20241127_AddDepartmentIdToInstallationMethods.sql` | (Data migration only) | Updates existing data |

**Removed Files (consolidated into DatabaseSeeder):**
- ~~`SeedMovementTypesAndLocationTypes.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultMovementTypesAsync()`
- ~~`SeedGuardConditionsAndSideEffects.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultGuardConditionsAsync()`
- ~~`SeedGuardConditionsAndSideEffects_PostgreSQL.sql`~~ - Duplicate removed
- ~~`20251216150000_AddWithdrawalParserTemplate.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultParserTemplatesAsync()`
- ~~`20251216160000_AddRfbParserTemplate.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultParserTemplatesAsync()`
- ~~`20251216170000_AddCustomerUncontactableParserTemplate.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultParserTemplatesAsync()`
- ~~`20251216180000_AddRescheduleParserTemplate.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultParserTemplatesAsync()`
- ~~`20251216190000_AddPaymentAdviceParserTemplate.sql`~~ - Now seeded by `DatabaseSeeder.SeedDefaultParserTemplatesAsync()`

### 1.2 C# Migration Files with INSERT (via migrationBuilder.Sql)

| File | Tables Seeded | Description |
|------|---------------|-------------|
| `20251219020647_RenameInstallationTypeToOrderCategory.cs` | `OrderCategories` | Copies data from `InstallationTypes` to `OrderCategories` (data migration, not seed) |

**Note:** No C# migrations use `migrationBuilder.InsertData()` - all use raw SQL via `migrationBuilder.Sql()`.

---

## 2. Entity Configurations with HasData()

**Status:** ✅ **NONE FOUND**

- No entity configuration files use `.HasData()` method
- All seed data is handled via:
  - `DatabaseSeeder` class (runtime)
  - SQL migration files (static)

**Files Checked:**
- `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Billing/InvoiceSubmissionHistoryConfiguration.cs` - No HasData()
- All other configuration files - No HasData() usage found

---

## 3. Dedicated Seed Classes

### 3.1 DatabaseSeeder (DISABLED)

**Location:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`

**Status:** ⚠️ **DISABLED** - Kept for reference only. All seeding is now done via PostgreSQL migrations.

**Previous Invocation:** `backend/src/CephasOps.Api/Program.cs` (now commented out)

**Previous Tables Seeded (now in PostgreSQL migration):**

| Table | Method | Count | Category |
|-------|--------|-------|----------|
| `Companies` | `SeedDefaultCompanyAsync()` | 1 | System Data |
| `Roles` | `SeedSuperAdminRoleAsync()`, `SeedDirectorRoleAsync()`, `SeedHeadOfDepartmentRoleAsync()`, `SeedSupervisorRoleAsync()` | 4 | System Data |
| `Users` | `SeedDefaultAdminUserAsync()`, `SeedFinanceHodUserAsync()` | 2 | System Data |
| `UserRoles` | (via user seeding) | 2 | System Data |
| `Departments` | `SeedGponDepartmentAsync()` | 1 | Master Data |
| `DepartmentMemberships` | (via Finance HOD seeding) | 1 | System Data |
| `OrderTypes` | `SeedDefaultOrderTypesAsync()` | 5 | Reference Data |
| `OrderCategories` | `SeedDefaultOrderCategoriesAsync()` | 4 | Reference Data |
| `BuildingTypes` | `SeedDefaultBuildingTypesAsync()` | 15 | Reference Data |
| `SplitterTypes` | `SeedDefaultSplitterTypesAsync()` | 3 | Reference Data |
| `Materials` | `SeedDefaultMaterialsAsync()` | ~50+ | Master Data |
| `MaterialCategories` | `SeedMaterialCategoriesFromMaterialsAsync()`, `SeedDefaultMaterialCategoriesAsync()` | Variable | Reference Data |
| `ParserTemplates` | `SeedDefaultParserTemplatesAsync()` | 14 | Reference Data |
| `GuardConditionDefinitions` | `SeedDefaultGuardConditionsAsync()` | 10 | System Data |
| `SideEffectDefinitions` | `SeedDefaultSideEffectsAsync()` | 5 | System Data |
| `GlobalSettings` | `SeedSmsWhatsAppGlobalSettingsAsync()` | ~30+ | System Data |
| `MovementTypes` | `SeedDefaultMovementTypesAsync()` | 11 | Reference Data |
| `LocationTypes` | `SeedDefaultLocationTypesAsync()` | 6 | Reference Data |

**Total Tables Seeded:** 18+ tables  
**Total Records Seeded:** ~150+ records (varies by run)

**Migration Status:**
- ✅ All seed data migrated to PostgreSQL SQL migration `20250106_SeedAllReferenceData.sql`
- ✅ DatabaseSeeder disabled in `Program.cs`
- ✅ PostgreSQL is now the single source of truth for all reference data

### 3.2 DocumentPlaceholderSeeder (DISABLED)

**Location:** `backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs`

**Status:** ⚠️ **DISABLED** - Kept for reference only. Should be migrated to PostgreSQL if needed.

**Previous Invocation:** `backend/src/CephasOps.Api/Program.cs` (now commented out)

**Tables Seeded:**

| Table | Document Types | Count |
|-------|----------------|-------|
| `DocumentPlaceholderDefinitions` | Invoice, JobDocket, RmaForm, PurchaseOrder, Quotation, BOQ, DeliveryOrder, PaymentReceipt | ~100+ placeholders |

**Key Features:**
- Seeds placeholder definitions for document generation
- Idempotent (checks for existing placeholders)
- Batch inserts (50 records per batch)
- Handles duplicate key errors gracefully

---

## 4. DbContext OnModelCreating

**Status:** ✅ **NO INLINE SEEDING FOUND**

- `ApplicationDbContext.OnModelCreating()` does not contain seed data
- All seeding is delegated to seed classes

---

## 5. Categorized List of Tables with Seeded Data

### 5.1 Reference/Lookup Tables

| Table | Seeding Method | Approx. Records | Notes |
|-------|---------------|-----------------|-------|
| `OrderTypes` | PostgreSQL Migration | 5 | Activation, Modification Indoor/Outdoor, Assurance, VAS |
| `OrderCategories` | PostgreSQL Migration | 4 | FTTH, FTTO, FTTR, FTTC |
| `BuildingTypes` | PostgreSQL Migration | 19 | Condo, Apartment, Terrace, Office, etc. |
| `SplitterTypes` | PostgreSQL Migration | 3 | 1:8, 1:12, 1:32 |
| `MaterialCategories` | PostgreSQL Migration | 8 (if none exist) | Default categories |
| `MovementTypes` | PostgreSQL Migration | 11 | GRN, IssueToSI, Transfer, etc. |
| `LocationTypes` | PostgreSQL Migration | 6 | Warehouse, SI, CustomerSite, RMA, etc. |
| `Skills` | PostgreSQL Migration | 33 | Fiber skills, network equipment, installation methods, safety, customer service |

### 5.2 Master Data Tables

| Table | Seeding Method | Approx. Records | Notes |
|-------|---------------|-----------------|-------|
| `Materials` | CSV Import (not seeded) | 47 | ONT, Router, IAD, Connector, Patchcord, etc. (see `backend/scripts/materials-default.csv`) |
| `Departments` | PostgreSQL Migration | 1 | GPON department |
| `ParserTemplates` | PostgreSQL Migration | 14 | TIME activation, modification, assurance, withdrawal, RFB, customer uncontactable, reschedule, payment advice, etc. |
| `EmailTemplates` | SQL migrations | 2+ | Customer Uncontactable, Reschedule |

### 5.3 System Data Tables

| Table | Seeding Method | Approx. Records | Notes |
|-------|---------------|-----------------|-------|
| `Companies` | PostgreSQL Migration | 1 | Default company "Cephas" |
| `Roles` | PostgreSQL Migration | 5 | SuperAdmin, Director, HeadOfDepartment, Supervisor, FinanceManager |
| `Users` | PostgreSQL Migration | 2 | Default admin, Finance HOD |
| `UserRoles` | PostgreSQL Migration | 2 | User-role assignments |
| `DepartmentMemberships` | PostgreSQL Migration | 1 | Finance HOD membership |
| `GuardConditionDefinitions` | PostgreSQL Migration | 10 | Workflow guard conditions |
| `SideEffectDefinitions` | PostgreSQL Migration | 5 | Workflow side effects |
| `GlobalSettings` | PostgreSQL Migration | ~30+ | SMS/WhatsApp settings, E-Invoice settings |
| `DocumentPlaceholderDefinitions` | **DISABLED** | ~100+ | Document template placeholders (should be migrated to PostgreSQL if needed) |

### 5.4 Tables with Seed Data in SQL Migrations Only

| Table | SQL File | Approx. Records | Notes |
|-------|----------|-----------------|-------|
| `VipGroups` | `AddVipEmailsAndParserTemplates.sql` | Unknown | May contain seed data |
| `VipEmails` | `AddVipEmailsAndParserTemplates.sql` | Unknown | May contain seed data |
| `InstallationMethods` | `AddInstallationMethodsTable.sql` | Unknown | May contain seed data |

---

## 6. Recommendations

### 6.1 Keep (Essential Seeds)

✅ **KEEP - System Data:**
- `Companies` - Required for single-company mode
- `Roles` - Required for RBAC
- `Users` - Required for initial login (SuperAdmin)
- `Departments` - Required for department-based operations
- `GlobalSettings` - Required for system configuration

✅ **KEEP - Reference Data:**
- `OrderTypes` - Core business reference data
- `OrderCategories` - Core business reference data
- `BuildingTypes` - Core business reference data
- `SplitterTypes` - Core business reference data
- `MovementTypes` - Required for inventory operations
- `LocationTypes` - Required for inventory operations

✅ **KEEP - Workflow Configuration:**
- `GuardConditionDefinitions` - Required for workflow engine
- `SideEffectDefinitions` - Required for workflow engine

✅ **KEEP - Document System:**
- `DocumentPlaceholderDefinitions` - Required for document generation

### 6.2 Consider Removing/Moving

✅ **COMPLETED - Master Data:**
- `Materials` - Now imported via CSV instead of seeded
  - **Previous:** ~50+ materials seeded automatically
  - **Current:** Materials import disabled in seeder, CSV import script provided
  - **Status:** ✅ Complete - Materials can be imported using `backend/scripts/import-materials.ps1`
  - **CSV File:** `backend/scripts/materials-default.csv` (47 materials)
  - **Documentation:** `backend/scripts/MATERIALS_IMPORT_GUIDE.md`

✅ **COMPLETED - Parser Templates:**
- `ParserTemplates` - All templates now consolidated in DatabaseSeeder
  - **Previous:** 9+ templates in DatabaseSeeder + 5 additional in SQL migrations
  - **Current:** 14 templates in DatabaseSeeder (all parser templates consolidated)
  - **Status:** ✅ Complete - All parser templates now seeded via DatabaseSeeder

✅ **COMPLETED - SQL Migrations Consolidation:**
- Duplicate SQL seed files removed
  - **Removed:** `SeedGuardConditionsAndSideEffects.sql`, `SeedGuardConditionsAndSideEffects_PostgreSQL.sql`, `SeedMovementTypesAndLocationTypes.sql`
  - **Removed:** 5 parser template SQL migration files
  - **Status:** ✅ Complete - All duplicate seeds removed, DatabaseSeeder is now the single source of truth

### 6.3 Migration Strategy

**Phase 1: Remove Duplicate Seeds** ✅ **COMPLETED**
1. ✅ Removed `SeedGuardConditionsAndSideEffects.sql` (duplicate of DatabaseSeeder)
2. ✅ Removed `SeedGuardConditionsAndSideEffects_PostgreSQL.sql` (duplicate)
3. ✅ Removed `SeedMovementTypesAndLocationTypes.sql` (duplicate of DatabaseSeeder)

**Phase 2: Consolidate Parser Templates** ✅ **COMPLETED**
1. ✅ Moved all parser template seeds from SQL migrations to DatabaseSeeder
2. ✅ Removed 5 individual SQL parser template migration files
3. ✅ DatabaseSeeder now seeds 14 parser templates (up from 9)

**Phase 3: Remove Material Seeds** ✅ **COMPLETED**
1. ✅ Commented out `SeedDefaultMaterialsAsync()` call in DatabaseSeeder
2. ✅ Created CSV file with default materials (`backend/scripts/materials-default.csv`)
3. ✅ Created PowerShell import script (`backend/scripts/import-materials.ps1`)

**Phase 4: Migrate All Seed Data to PostgreSQL** ✅ **COMPLETED** (2026-01-06)

- ✅ All C# seed data migrated to PostgreSQL SQL migration `20260106014834_SeedAllReferenceData.cs`
- ✅ C# `DatabaseSeeder` and `DocumentPlaceholderSeeder` disabled in `Program.cs`
- ✅ Fixed column name issues (snake_case for guard_condition_definitions, side_effect_definitions)
- ✅ Fixed GlobalSettings (removed IsDeleted - not a CompanyScopedEntity)
- ✅ Fixed ParserTemplates (added required CreatedByUserId field)
- ✅ Added conditional checks for IsDeleted column existence (backward compatibility)
- ✅ Migration successfully applied to database

---

## 7. Summary Statistics

| Category | Count |
|----------|-------|
| **Total Tables with Seed Data** | 18+ |
| **Total Seed Classes** | 2 |
| **SQL Migration Files with Seeds** | 7 (reduced from 16 after consolidation) |
| **C# Migrations with Seeds** | 1 (data migration only) |
| **Entity Configurations with HasData()** | 0 |
| **Total Approximate Records Seeded** | ~200+ |

---

## 8. Files Reference

### Seed Classes
- `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`
- `backend/src/CephasOps.Infrastructure/Persistence/Seeders/DocumentPlaceholderSeeder.cs`

### Invocation Point
- `backend/src/CephasOps.Api/Program.cs` (lines 639-658)

### SQL Migration Files (with INSERT INTO)
**Remaining SQL seed files:**
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216210000_AddEmailSendingTemplates.sql`
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20251216240000_EnsureRescheduleEmailTemplatesExist.sql`
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/AddVipEmailsAndParserTemplates.sql`
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/AddInstallationMethodsTable.sql`

**Removed files (consolidated into DatabaseSeeder):**
- ~~`SeedMovementTypesAndLocationTypes.sql`~~ - Removed 2025-01-XX
- ~~`SeedGuardConditionsAndSideEffects.sql`~~ - Removed 2025-01-XX
- ~~`SeedGuardConditionsAndSideEffects_PostgreSQL.sql`~~ - Removed 2025-01-XX
- ~~`20251216150000_AddWithdrawalParserTemplate.sql`~~ - Removed 2025-01-XX
- ~~`20251216160000_AddRfbParserTemplate.sql`~~ - Removed 2025-01-XX
- ~~`20251216170000_AddCustomerUncontactableParserTemplate.sql`~~ - Removed 2025-01-XX
- ~~`20251216180000_AddRescheduleParserTemplate.sql`~~ - Removed 2025-01-XX
- ~~`20251216190000_AddPaymentAdviceParserTemplate.sql`~~ - Removed 2025-01-XX

### Documentation
- `backend/DATABASE_SEEDING.md` - General seeding documentation
- `backend/seed.ps1` - PowerShell script (informational only)

---

## 9. Next Steps

1. ✅ **Complete** - Inventory all seed data usage
2. ✅ **Complete** - Review and approve recommendations
3. ✅ **Complete** - Remove duplicate SQL seed files (3 files removed)
4. ✅ **Complete** - Consolidate parser template seeds (5 SQL files removed, 5 templates added to DatabaseSeeder)
5. ✅ **Complete** - Remove material seeds, create CSV import script and documentation

---

**Report Generated:** 2025-01-05  
**Last Updated:** 2025-01-XX (all phases completed)

## 10. Phase 3 Implementation Summary

**Materials Import Migration Completed:**

- ✅ Material seeding disabled in `DatabaseSeeder.cs`
- ✅ CSV file created: `backend/scripts/materials-default.csv` (47 materials)
- ✅ PowerShell import script: `backend/scripts/import-materials.ps1`
- ✅ Documentation: `backend/scripts/MATERIALS_IMPORT_GUIDE.md`

**Import Methods Available:**
1. PowerShell script (automated via API)
2. Web UI import (Settings > Materials > Import)
3. Manual SQL import (advanced)

**Impact:** Materials are now managed via import/export rather than automatic seeding, providing better control and flexibility for production deployments.

