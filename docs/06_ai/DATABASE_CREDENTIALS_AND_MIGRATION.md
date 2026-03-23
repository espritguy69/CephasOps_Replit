# PostgreSQL Database Credentials & InstallerType Migration

**Date:** 2026-01-05

---

## PostgreSQL Connection Details

### Connection Information
- **Host:** `localhost`
- **Port:** `5432`
- **Database:** `cephasops`
- **Username:** `postgres`
- **Password:** `J@saw007`
- **SSL Mode:** `Disable`

### Connection String
```
Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007;SslMode=Disable;Include Error Detail=true
```

### Connection Tools
- **pgAdmin:** Use the connection details above
- **psql Command Line:**
  ```bash
  psql -h localhost -p 5432 -U postgres -d cephasops
  ```
- **EF Core Migrations:**
  ```bash
  dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj --connection "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007;SslMode=Disable"
  ```

---

## InstallerType Migration SQL Script

### Manual Migration (Run in PostgreSQL)

**⚠️ IMPORTANT: Stop the API before running this migration!**

```sql
-- Step 1: Add InstallerType column (nullable initially)
ALTER TABLE "ServiceInstallers" 
ADD COLUMN "InstallerType" VARCHAR(50) NULL;

-- Step 2: Migrate existing data from IsSubcontractor to InstallerType
UPDATE "ServiceInstallers" 
SET "InstallerType" = CASE 
    WHEN "IsSubcontractor" = true THEN 'Subcontractor' 
    ELSE 'InHouse' 
END
WHERE "InstallerType" IS NULL;

-- Step 3: Make InstallerType NOT NULL with default
ALTER TABLE "ServiceInstallers" 
ALTER COLUMN "InstallerType" SET NOT NULL,
ALTER COLUMN "InstallerType" SET DEFAULT 'InHouse';

-- Step 4: Verify migration
SELECT 
    "Name",
    "IsSubcontractor",
    "InstallerType",
    CASE 
        WHEN ("IsSubcontractor" = true AND "InstallerType" = 'Subcontractor') OR 
             ("IsSubcontractor" = false AND "InstallerType" = 'InHouse') 
        THEN 'OK' 
        ELSE 'MISMATCH' 
    END as "Status"
FROM "ServiceInstallers"
ORDER BY "Name";

-- Expected: All rows should show "OK" status
```

---

## Migration Steps

### Option 1: Using EF Core Migration (Recommended)

1. **Stop the API** (if running)
   - Close any running `dotnet run` or `dotnet watch` processes
   - Process ID 19496 needs to be stopped

2. **Create Migration:**
   ```bash
   cd C:\Projects\CephasOps\backend
   dotnet ef migrations add AddInstallerTypeToServiceInstallers --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
   ```

3. **Edit Migration File:**
   - Location: `backend/src/CephasOps.Infrastructure/Persistence/Migrations/[timestamp]_AddInstallerTypeToServiceInstallers.cs`
   - Add data migration SQL in the `Up()` method (see SQL script above)

4. **Apply Migration:**
   ```bash
   dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj --connection "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007;SslMode=Disable"
   ```

### Option 2: Manual SQL Execution

1. **Stop the API** (if running)

2. **Connect to PostgreSQL:**
   - Use pgAdmin or psql
   - Connect using credentials above

3. **Run the SQL script** (provided above)

4. **Verify:**
   ```sql
   SELECT COUNT(*) FROM "ServiceInstallers" WHERE "InstallerType" IS NULL;
   -- Should return 0
   ```

---

## Verification Queries

### Check Current Data
```sql
SELECT 
    "Name",
    "IsSubcontractor",
    "InstallerType",
    "IsActive"
FROM "ServiceInstallers"
ORDER BY "Name";
```

### Count by Type
```sql
SELECT 
    "InstallerType",
    COUNT(*) as "Count"
FROM "ServiceInstallers"
GROUP BY "InstallerType";
```

### Check for Mismatches
```sql
SELECT 
    "Name",
    "IsSubcontractor",
    "InstallerType"
FROM "ServiceInstallers"
WHERE 
    ("IsSubcontractor" = true AND "InstallerType" != 'Subcontractor') OR
    ("IsSubcontractor" = false AND "InstallerType" != 'InHouse');
-- Should return 0 rows
```

---

## Rollback (If Needed)

```sql
-- Remove InstallerType column (only if migration fails)
ALTER TABLE "ServiceInstallers" 
DROP COLUMN IF EXISTS "InstallerType";
```

---

## Notes

- The `IsSubcontractor` field is kept for backward compatibility
- Both fields will be synced during create/update operations
- Future cleanup: Remove `IsSubcontractor` after migration period
- Default value: `InHouse` (matches current behavior where `IsSubcontractor` defaults to `false`)

