# Materials Import Guide

## Overview

Materials are no longer seeded automatically during database initialization. Instead, they should be imported via CSV file using one of the methods below.

## Default Materials CSV

The default materials data is available in:
- **File:** `backend/scripts/materials-default.csv`
- **Records:** 47 materials (ONT, Routers, IAD, Connectors, Patchcords, Drop Cables, Accessories, etc.)

## Import Methods

### Method 1: PowerShell Script (Recommended)

Use the automated PowerShell script to import via API:

```powershell
# Basic usage (uses default credentials)
.\backend\scripts\import-materials.ps1

# With custom parameters
.\backend\scripts\import-materials.ps1 `
    -ApiBaseUrl "http://localhost:5000" `
    -Email "simon@cephas.com.my" `
    -Password "J@saw007"

# Using environment variables
$env:CEPHASOPS_API_URL = "http://localhost:5000"
$env:CEPHASOPS_EMAIL = "simon@cephas.com.my"
$env:CEPHASOPS_PASSWORD = "J@saw007"
.\backend\scripts\import-materials.ps1
```

**Prerequisites:**
- Backend API must be running
- Valid user credentials (default: simon@cephas.com.my / J@saw007)
- CSV file must exist at `backend/scripts/materials-default.csv`

### Method 2: Web UI Import

1. Start the backend and frontend applications
2. Log in to the web application
3. Navigate to **Settings > Materials**
4. Click **Import** button
5. Select `backend/scripts/materials-default.csv`
6. Click **Upload** and review import results

### Method 3: Manual SQL Import (Advanced)

If API import is not available, you can import directly via SQL:

```sql
-- Example: Import a single material
INSERT INTO "Materials" (
    "Id", "CompanyId", "ItemCode", "Description", "Category", 
    "UnitOfMeasure", "IsSerialised", "DefaultCost", "IsActive",
    "CreatedAt", "UpdatedAt", "IsDeleted"
) VALUES (
    gen_random_uuid(),
    (SELECT "Id" FROM "Companies" LIMIT 1),
    'CAE-000-0820',
    'Huawei HG8145X6 - Dual-band WiFi 6 ONT',
    'ONT / Router',
    'Unit',
    true,
    0,
    true,
    NOW(),
    NOW(),
    false
) ON CONFLICT DO NOTHING;
```

**Note:** For bulk SQL import, you would need to convert the CSV to SQL INSERT statements.

## CSV Format

The CSV file must follow this format (matching `MaterialCsvDto`):

| Column | Type | Required | Description |
|--------|------|----------|-------------|
| `Code` | string | Yes | Material item code (unique identifier) |
| `Description` | string | Yes | Material description |
| `CategoryName` | string | Yes | Material category name |
| `UnitOfMeasure` | string | Yes | Unit of measure (Unit, Piece, Meter, etc.) |
| `UnitCost` | decimal | No | Default unit cost (default: 0) |
| `IsSerialised` | bool | Yes | Whether material requires serial numbers |
| `MinStockLevel` | int? | No | Minimum stock level (optional) |
| `ReorderPoint` | int? | No | Reorder point (optional) |
| `IsActive` | bool | Yes | Whether material is active (default: true) |

### Example CSV Row

```csv
Code,Description,CategoryName,UnitOfMeasure,UnitCost,IsSerialised,MinStockLevel,ReorderPoint,IsActive
CAE-000-0820,Huawei HG8145X6 - Dual-band WiFi 6 ONT,ONT / Router,Unit,0,True,,,True
```

## Material Categories

Material categories are automatically created from the `CategoryName` column during import. The following categories are included in the default CSV:

- ONT / Router
- Router / ONT
- ONT
- Router / AP
- Router
- Router / Mesh
- ONU
- Phone
- IAD
- Connector
- Patchcord
- Drop Cable
- Accessories
- Fiber Cable
- Distribution

## Department Assignment

Materials are assigned to the GPON department by default. If the GPON department doesn't exist, materials will be created without a department assignment.

## Troubleshooting

### Import Script Fails with "Authentication failed"

- Verify the backend API is running
- Check that the email/password are correct
- Ensure the API URL is correct (default: http://localhost:5000)

### Import Script Fails with "No file uploaded"

- Verify the CSV file exists at `backend/scripts/materials-default.csv`
- Check file permissions

### Materials Not Appearing After Import

- Check the import results for errors
- Verify materials are not filtered out (check IsActive flag)
- Check department assignment if filtering by department

### API Import Endpoint Returns "Not Implemented"

The API import endpoint may have a TODO comment. In this case:
1. Use the Web UI import method instead
2. Or wait for the API endpoint to be fully implemented
3. Or use manual SQL import for immediate needs

## Reverting to Seeded Materials

If you need to re-enable automatic material seeding (not recommended for production):

1. Edit `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`
2. Uncomment the line: `await SeedDefaultMaterialsAsync(null);`
3. Restart the backend application

**Note:** This is not recommended as it mixes seeding and import approaches.

## Related Files

- **CSV Data:** `backend/scripts/materials-default.csv`
- **Import Script:** `backend/scripts/import-materials.ps1`
- **Seeder Code:** `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs` (commented out)
- **API Endpoint:** `backend/src/CephasOps.Api/Controllers/InventoryController.cs` (ImportMaterials method)
- **DTO:** `backend/src/CephasOps.Application/Common/DTOs/ImportExportDtos.cs` (MaterialCsvDto)

## Migration from Seeded to Imported Materials

If you have an existing database with seeded materials:

1. Export existing materials via API: `GET /api/inventory/materials/export`
2. Review and merge with `materials-default.csv` if needed
3. Delete existing materials (if starting fresh) or update them via import
4. Import the consolidated CSV file

**Warning:** Deleting materials may affect existing orders and inventory records. Use caution in production environments.

