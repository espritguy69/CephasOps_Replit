# BillingRatecard Migration

This migration adds the `BillingRatecards` table for Partner Rates (PU Rates) management.

## Files

- `AddBillingRatecardTable.sql` - SQL migration script
- `ApplyBillingRatecardMigration.ps1` - PowerShell script to apply the migration

## How to Apply

### Option 1: Using PowerShell Script (Recommended)

1. Open PowerShell in the `Migrations` directory
2. Run the script:
   ```powershell
   .\ApplyBillingRatecardMigration.ps1
   ```
   
   Or provide connection string manually:
   ```powershell
   .\ApplyBillingRatecardMigration.ps1 -ConnectionString "Host=localhost;Port=5432;Database=cephasops;Username=your_user;Password=your_password"
   ```

### Option 2: Using EF Core Migration (When Backend is Stopped)

1. Stop the backend API if it's running
2. Navigate to Infrastructure project:
   ```powershell
   cd backend\src\CephasOps.Infrastructure
   ```
3. Create migration:
   ```powershell
   dotnet ef migrations add AddBillingRatecard --startup-project ../CephasOps.Api --context ApplicationDbContext
   ```
4. Apply migration:
   ```powershell
   dotnet ef database update --startup-project ../CephasOps.Api --context ApplicationDbContext
   ```

### Option 3: Manual SQL Execution

1. Open `AddBillingRatecardTable.sql` in a PostgreSQL client (pgAdmin, DBeaver, etc.)
2. Connect to your database
3. Execute the SQL script

## Verification

After applying the migration, verify the table was created:

```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_name = 'BillingRatecards';

-- Check table structure
\d "BillingRatecards"

-- Check indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'BillingRatecards';
```

## Rollback (if needed)

To drop the table:

```sql
DROP TABLE IF EXISTS "BillingRatecards" CASCADE;
```

## Notes

- The table includes indexes for efficient querying by Company/Partner/OrderType
- `TaxRate` uses precision (5,4) to support rates like 0.06 (6% SST)
- `Amount` uses precision (18,2) for currency values
- All columns from `CompanyScopedEntity` are included (Id, CompanyId, CreatedAt, UpdatedAt)

