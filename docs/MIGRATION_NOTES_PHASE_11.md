# Migration Notes — Phase 11 (Tenant Isolation)

## Summary

- Adds table **Tenants** and column **Companies.TenantId** (nullable FK to Tenants).
- No existing data is migrated; all current companies keep `TenantId = NULL`.

## Migration name

`Phase11_TenantIsolation` (file: `20260310033559_Phase11_TenantIsolation.cs`).

## Steps

1. **Backup** the database before applying.
2. **Apply migration** (choose one):
   - **EF CLI**: From `backend/src/CephasOps.Api`:  
     `dotnet ef database update --project ..\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj --context ApplicationDbContext`
   - **Idempotent script** (e.g. for production or shared DB):  
     `dotnet ef migrations script --idempotent --output migrations_phase11.sql --project ..\CephasOps.Infrastructure\CephasOps.Infrastructure.csproj`  
     Then run the generated SQL against the target database.
3. **Permissions**: Run the application so that the database seeder runs (or run your usual seed). New permissions `admin.tenants.view` and `admin.tenants.edit` will be created and assigned to SuperAdmin and Admin roles (admin.*).

## Rollback

To remove Phase 11 schema changes:

- **EF**: Add a new migration that drops `FK_Companies_Tenants_TenantId`, `IX_Companies_TenantId`, and table `Tenants`, and removes column `Companies.TenantId`. Or restore from backup and remove the Phase11 migration from the Migrations folder and revert the model snapshot.
- **Manual SQL**:  
  `ALTER TABLE "Companies" DROP CONSTRAINT "FK_Companies_Tenants_TenantId";`  
  `DROP INDEX "IX_Companies_TenantId";`  
  `ALTER TABLE "Companies" DROP COLUMN "TenantId";`  
  `DROP TABLE "Tenants";`

## Post-migration

- Create tenants via `POST /api/tenants` if needed.
- To attach companies to a tenant, update `Companies` directly:  
  `UPDATE "Companies" SET "TenantId" = '<tenant-guid>' WHERE "Id" = '<company-guid>';`  
  (Or extend the company API to accept TenantId on create/update.)
