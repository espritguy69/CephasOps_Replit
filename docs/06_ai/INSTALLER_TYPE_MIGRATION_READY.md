# InstallerType Migration - Ready to Apply

**Date:** 2026-01-05  
**Status:** ✅ Migration Created | ⏳ Ready to Apply

---

## ✅ Migration File Created

**Location:** `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260105122021_AddInstallerTypeToServiceInstallers.cs`

**What it does:**
1. Adds `InstallerType` column as nullable initially
2. Migrates existing data: `IsSubcontractor = true` → `'Subcontractor'`, `false` → `'InHouse'`
3. Makes column NOT NULL with default `'InHouse'`
4. Adds check constraint to ensure valid values (`'InHouse'` or `'Subcontractor'`)

---

## ⏳ Apply Migration

### Step 1: Stop the API

**⚠️ IMPORTANT:** The API must be stopped before applying the migration!

- Close any running `dotnet run` or `dotnet watch` processes
- Check Task Manager for `CephasOps.Api.exe` or `dotnet.exe` processes

### Step 2: Apply Migration

Run this command:

```bash
cd C:\Projects\CephasOps\backend
dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj --connection "Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=J@saw007;SslMode=Disable"
```

**Or** if you have the connection string in `appsettings.json`, you can omit the `--connection` parameter:

```bash
dotnet ef database update --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

### Step 3: Verify Migration

After applying, verify the migration with this SQL query:

```sql
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
```

**Expected:** All rows should show "OK" status.

### Step 4: Restart API

After successful migration, restart the API:

```bash
cd C:\Projects\CephasOps\backend
dotnet watch run --project src/CephasOps.Api/CephasOps.Api.csproj
```

---

## 🔄 Rollback (If Needed)

If you need to rollback the migration:

```bash
dotnet ef database update <PreviousMigrationName> --project src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj --startup-project src/CephasOps.Api/CephasOps.Api.csproj
```

Or manually:

```sql
-- Drop constraint
ALTER TABLE "ServiceInstallers"
DROP CONSTRAINT IF EXISTS "CK_ServiceInstallers_InstallerType";

-- Drop column
ALTER TABLE "ServiceInstallers"
DROP COLUMN IF EXISTS "InstallerType";
```

---

## ✅ Implementation Status

- ✅ Backend enum created
- ✅ Entity updated
- ✅ EF Core configuration fixed
- ✅ DTOs updated
- ✅ Service layer updated
- ✅ Frontend types updated
- ✅ Frontend forms updated
- ✅ Migration file created
- ⏳ **Database migration pending** (waiting for API to stop)

---

## 📋 Next Steps

1. **Stop the API**
2. **Apply the migration** (command above)
3. **Verify migration** (SQL query above)
4. **Restart the API**
5. **Test the frontend** - Create/Edit Service Installers with InstallerType dropdown

---

## 🎯 Testing Checklist

After migration:
- [ ] Verify all existing Service Installers have correct `InstallerType` values
- [ ] Create new Service Installer with "In-House" type
- [ ] Create new Service Installer with "Subcontractor" type
- [ ] Edit existing Service Installer and change type
- [ ] Verify table displays correct type badges
- [ ] Test filter buttons (All / In-House / Subcontractor)
- [ ] Verify API endpoints return `installerType` field
- [ ] Check that `IsSubcontractor` is still synced for backward compatibility

