# Service Installer System Migration Guide

## Overview

This guide documents the migration process for updating existing Service Installers to use the new Type, Level, and Skills system.

## What Changed

### Before
- `SiLevel` could be: "Junior", "Senior", or "Subcon"
- `IsSubcontractor` boolean field (deprecated)
- No skills system

### After
- `SiLevel` (InstallerLevel enum): Only "Junior" or "Senior"
- `InstallerType` enum: "InHouse" or "Subcontractor"
- `IsSubcontractor` kept for backward compatibility (synced with InstallerType)
- Skills system with categories and assignments

## Migration Steps

### 1. Database Migration

The database schema has been updated via EF Core migrations. Run:

```bash
dotnet ef database update --project backend/src/CephasOps.Infrastructure --startup-project backend/src/CephasOps.Api
```

### 2. Data Migration

Run the data migration script to update existing Service Installers:

**Using PowerShell:**
```powershell
cd backend/scripts
.\migrate-service-installer-levels.ps1
```

**Or manually using psql:**
```bash
psql -h localhost -p 5432 -U postgres -d cephasops -f migrate-service-installer-levels.sql
```

### 3. What the Migration Does

1. **Converts "Subcon" level to Subcontractor type**
   - Installers with `SiLevel = 'Subcon'` are converted to:
     - `InstallerType = 'Subcontractor'`
     - `SiLevel = 'Junior'` (default)
     - `IsSubcontractor = true`

2. **Normalizes level values**
   - Any invalid `SiLevel` values are set to 'Junior'

3. **Syncs InstallerType with IsSubcontractor**
   - Ensures backward compatibility
   - Updates mismatched values

4. **Sets defaults**
   - NULL `InstallerType` → 'InHouse' (or 'Subcontractor' if `IsSubcontractor = true`)
   - NULL/empty `SiLevel` → 'Junior'

### 4. Skills Seeding

Default skills are automatically seeded when the database is initialized. The seeder includes:

- **FiberSkills** (9 skills): Cable installation, splicing, testing, etc.
- **NetworkEquipment** (7 skills): ONT, router, Wi-Fi, IPTV setup, etc.
- **InstallationMethods** (6 skills): Aerial, underground, indoor routing, etc.
- **SafetyCompliance** (6 skills): Heights certification, electrical safety, TNB clearance, etc.
- **CustomerService** (5 skills): Communication, service demo, professional conduct, etc.

Total: **33 default skills** across 5 categories.

## Verification

After migration, verify the results:

```sql
-- Check installer type distribution
SELECT 
    "InstallerType",
    "SiLevel",
    COUNT(*) as count
FROM "ServiceInstallers"
WHERE "IsDeleted" = false
GROUP BY "InstallerType", "SiLevel"
ORDER BY "InstallerType", "SiLevel";

-- Check for any remaining issues
SELECT 
    COUNT(*) as total,
    COUNT(CASE WHEN "InstallerType" IS NULL THEN 1 END) as null_type,
    COUNT(CASE WHEN "SiLevel" NOT IN ('Junior', 'Senior') THEN 1 END) as invalid_level,
    COUNT(CASE WHEN "InstallerType" = 'Subcontractor' AND "IsSubcontractor" = false THEN 1 END) as type_mismatch
FROM "ServiceInstallers"
WHERE "IsDeleted" = false;
```

## Manual Updates (If Needed)

If you need to manually update specific installers:

```sql
-- Convert an installer to Subcontractor
UPDATE "ServiceInstallers"
SET 
    "InstallerType" = 'Subcontractor',
    "IsSubcontractor" = true,
    "SiLevel" = 'Junior',  -- or 'Senior'
    "UpdatedAt" = NOW()
WHERE "Id" = 'installer-guid-here';

-- Promote an installer to Senior
UPDATE "ServiceInstallers"
SET 
    "SiLevel" = 'Senior',
    "UpdatedAt" = NOW()
WHERE "Id" = 'installer-guid-here';
```

## Rollback (If Needed)

If you need to rollback the migration:

```sql
-- Note: This is a simplified rollback - full rollback would require restoring from backup
-- Convert Subcontractors back to "Subcon" level (if needed)
UPDATE "ServiceInstallers"
SET 
    "SiLevel" = 'Subcon',
    "InstallerType" = 'InHouse',
    "IsSubcontractor" = false,
    "UpdatedAt" = NOW()
WHERE 
    "InstallerType" = 'Subcontractor'
    AND "IsDeleted" = false;
```

**Warning:** Rollback is not recommended as the new system is the standard going forward.

## Post-Migration Tasks

1. **Review Installer Classifications**
   - Verify that Subcontractors are correctly identified
   - Check that levels (Junior/Senior) are appropriate

2. **Assign Skills**
   - Use the UI to assign skills to installers
   - Skills can be assigned during creation or editing

3. **Update Conditional Fields**
   - For In-House installers: Ensure Employee ID is set
   - For Subcontractors: Ensure Contractor ID is set
   - Update email addresses for In-House installers (must be @cephas.com or @cephas.com.my)

4. **Test Filtering**
   - Test filtering by Type, Level, and Skills
   - Verify that job assignment filtering works correctly

## Troubleshooting

### Issue: Migration script fails
- Check database connection
- Verify PostgreSQL credentials
- Ensure you have write permissions

### Issue: Some installers still have invalid levels
- Run the normalization step manually:
  ```sql
  UPDATE "ServiceInstallers"
  SET "SiLevel" = 'Junior'
  WHERE "SiLevel" NOT IN ('Junior', 'Senior') AND "IsDeleted" = false;
  ```

### Issue: Skills not appearing
- Verify skills were seeded: `SELECT COUNT(*) FROM "Skills" WHERE "IsDeleted" = false;`
- Re-run seeder if needed: The seeder runs automatically on app startup

## Support

For issues or questions, refer to:
- `/docs/06_ai/SERVICE_INSTALLER_REVIEW_AND_UPDATE_ANALYSIS.md` - Full analysis document
- Backend code: `backend/src/CephasOps.Domain/ServiceInstallers/`
- Frontend code: `frontend/src/pages/settings/ServiceInstallersPage.tsx`

