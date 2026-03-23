# Service Installer Import Guide

**Date:** 2026-01-05  
**Source:** `backend/test-data/service-installers-template.csv`

---

## Overview

This guide explains how to import Service Installer data from the CSV template into PostgreSQL.

---

## Prerequisites

1. ✅ **Database migration applied** - `AddInstallerTypeToServiceInstallers` migration must be applied first
2. ✅ **GPON Department exists** - The department must be seeded before importing installers
3. ✅ **PostgreSQL client tools** - `psql` command must be available (or use pgAdmin)

---

## Import Methods

### Method 1: Direct SQL Script (Recommended)

**Two versions available:**

#### Option A: UPSERT Version (Updates existing, inserts new) - **RECOMMENDED**
**File:** `backend/scripts/import-service-installers-from-csv-upsert.sql`

This version will:
- Update existing Service Installers (matched by Name + DepartmentId)
- Insert new Service Installers if they don't exist

1. **Connect to PostgreSQL:**
   ```bash
   psql -h localhost -p 5432 -U postgres -d cephasops
   ```

2. **Run the SQL script:**
   ```sql
   \i backend/scripts/import-service-installers-from-csv-upsert.sql
   ```

   Or copy-paste the SQL content into pgAdmin and execute.

#### Option B: Insert Only Version (Skips duplicates)
**File:** `backend/scripts/import-service-installers-from-csv.sql`

This version will:
- Insert new Service Installers
- Skip existing ones (ON CONFLICT DO NOTHING)

Use this if you want to avoid updating existing records.

### Method 2: PowerShell Script

**File:** `backend/scripts/import-service-installers.ps1`

```powershell
cd C:\Projects\CephasOps\backend\scripts
.\import-service-installers.ps1
```

**With custom connection string:**
```powershell
.\import-service-installers.ps1 -ConnectionString "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=YourPassword;SslMode=Disable"
```

### Method 3: Manual SQL Execution

1. Open pgAdmin or psql
2. Connect to `cephasops` database
3. Copy the SQL from `backend/scripts/import-service-installers-from-csv.sql`
4. Execute the script

---

## Data Mapping

The CSV columns are mapped as follows:

| CSV Column | Database Column | Notes |
|-----------|----------------|-------|
| Name | Name | Direct mapping |
| Phone | Phone | Direct mapping |
| Email | Email | `-` becomes `NULL` |
| Level | SiLevel | Direct mapping (Junior/Senior) |
| Type | InstallerType | `In-house` → `InHouse`, `Subcontractor` → `Subcontractor` |
| IsActive | IsActive | `TRUE` → `true`, `FALSE` → `false` |
| DepartmentName | DepartmentId | Looked up by name "GPON" |
| Code | - | Not used (empty in CSV) |
| IcNumber | IcNumber | Empty in CSV → `NULL` |
| BankName | BankName | Empty in CSV → `NULL` |
| BankAccountNumber | BankAccountNumber | Empty in CSV → `NULL` |
| Address | Address | Empty in CSV → `NULL` |
| EmergencyContact | EmergencyContact | Empty in CSV → `NULL` |

---

## Imported Records

The script imports **21 Service Installers**:

- **In-House (11):**
  - CHANDRASEKARAN VEERIAH
  - ISHAAN
  - MOHAMMAD ALIYASMAAN
  - MUHAMAD QAIRUL HAIKAL BIN ABDULLAH
  - MUHAMMAD AMMAR BIN MOHD GHAZI
  - NORAFIZ HAFIZUL BIN ABDULLAH
  - SHAMALAN A/L JOSEPH
  - SIVA A/L THANGIAH (Inactive)
  - SYLVESTER ELGIVA A/L SIMON
  - Test Service Installer
  - (One more...)

- **Subcontractor (10):**
  - EDWIN DASS A/L YESU DAS
  - K. MARIAPPAN A/L KUPPATHAN
  - KLAVINN RAJ A/L AROKKIASAMY
  - MOHD TAKYIN BIN CHE ALI
  - MUNIANDY A/L SOORINARAYANAN
  - RAVEEN NAIR A/L K RAHMAN
  - SARAVANAN A/L I. CHINNIAH
  - SASIKUMAR A/L SEENIE
  - SATHISVARAN A/L S P GURUNATHAN
  - SIVANESVARAAN A/L S YANESAGAR
  - YELLESHUA JEEVAN A/L AROKKIASAMY

---

## Verification

After import, run this query to verify:

```sql
SELECT 
    "Name",
    "Phone",
    "Email",
    "SiLevel",
    "InstallerType",
    "IsSubcontractor",
    "IsActive",
    "CreatedAt"
FROM "ServiceInstallers"
WHERE "DepartmentId" = (SELECT "Id" FROM "Departments" WHERE "Name" = 'GPON' LIMIT 1)
ORDER BY "Name";
```

**Expected:** 21 rows returned.

---

## Troubleshooting

### Error: "GPON Department not found"

**Solution:** Run the database seeder first:
```bash
# The seeder should create the GPON department
# Check if it exists:
SELECT * FROM "Departments" WHERE "Name" = 'GPON';
```

### Error: "InstallerType column does not exist"

**Solution:** Apply the migration first:
```bash
dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

### Duplicate Key Errors

**Solution:** The script uses `ON CONFLICT DO NOTHING`, so duplicates are skipped. If you want to update existing records, modify the script to use `ON CONFLICT ... DO UPDATE`.

---

## Updating Existing Records

If you need to update existing Service Installers instead of inserting new ones, modify the SQL script to use:

```sql
INSERT INTO "ServiceInstallers" (...)
VALUES (...)
ON CONFLICT ("Name", "DepartmentId") 
DO UPDATE SET
    "Phone" = EXCLUDED."Phone",
    "Email" = EXCLUDED."Email",
    "SiLevel" = EXCLUDED."SiLevel",
    "InstallerType" = EXCLUDED."InstallerType",
    "IsSubcontractor" = EXCLUDED."IsSubcontractor",
    "IsActive" = EXCLUDED."IsActive",
    "UpdatedAt" = NOW();
```

**Note:** This requires a unique constraint on `(Name, DepartmentId)`.

---

## Files

- **CSV Source:** `backend/test-data/service-installers-template.csv`
- **SQL Script:** `backend/scripts/import-service-installers-from-csv.sql`
- **PowerShell Script:** `backend/scripts/import-service-installers.ps1`
- **This Guide:** `docs/06_ai/SERVICE_INSTALLER_IMPORT_GUIDE.md`

