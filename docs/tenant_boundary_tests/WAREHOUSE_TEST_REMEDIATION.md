# Warehouse Tenant-Isolation Test Remediation

## Root cause

Warehouse creation returned **500 Internal Server Error** in the integration test host because the **`Warehouse` entity was not included in the EF Core model** for `ApplicationDbContext`.

- `WarehouseService` uses `_context.Set<Warehouse>()` for all operations (GetAll, GetById, Create, Update, Delete).
- EF Core throws at runtime when `Set<T>()` is used for a type that is not part of the context model: the entity was never registered via a `DbSet<Warehouse>` and no `IEntityTypeConfiguration<Warehouse>` was applied.
- Migrations and the model snapshot reference `WarehouseId` / `WarehouseName` on related entities (e.g. `Bin`, `StockLocation`) but no migration ever created a `Warehouses` table and the `Warehouse` entity type was never added to the context. The service and API were written against an entity that was never wired into the persistence layer in this codebase.

## Fix

1. **Register the entity in the context**  
   In `ApplicationDbContext.cs`, added:
   - `public DbSet<Warehouse> Warehouses => Set<Warehouse>();`  
   so `Warehouse` is part of the model and `Set<Warehouse>()` in the service works.

2. **Map the entity to the database**  
   Added `WarehouseConfiguration` in `Infrastructure/Persistence/Configurations/Settings/WarehouseConfiguration.cs` so that:
   - The entity is mapped to table `Warehouses`.
   - Key and properties (Code, Name, CompanyId, etc.) are configured with appropriate constraints and indexes.
   - The configuration is applied via the existing `ApplyConfigurationsFromAssembly` in `OnModelCreating`.

With this, the in-memory database used by the integration tests creates the `Warehouses` table from the model, and warehouse create/list/get/update/delete succeed in the test host.

## Files changed

| File | Change |
|------|--------|
| `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` | Added `DbSet<Warehouse> Warehouses => Set<Warehouse>();` (after `Bins`). |
| `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Settings/WarehouseConfiguration.cs` | **New file.** `IEntityTypeConfiguration<Warehouse>` with table `Warehouses`, key, property constraints, and indexes. |
| `backend/tests/CephasOps.Api.Tests/Integration/TenantBoundaryTests.cs` | Removed `Skip` from: `Warehouses_List_ReturnsOnlySameTenantWarehouses`, `Warehouses_GetById_OtherTenant_Returns404`, `Warehouses_Update_OtherTenant_Returns404`, `Warehouses_Delete_OtherTenant_Returns404`. |
| `docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md` | Documented the remediation under “Critical gaps found and fixed” and reference to this file. |

## Result

- **Warehouse create in tests**: Works. POST `api/warehouses` with tenant-scoped client returns 201 and the created warehouse.
- **Tenant isolation tests**: All four previously skipped warehouse tests run and **pass**:
  - `Warehouses_List_ReturnsOnlySameTenantWarehouses`
  - `Warehouses_GetById_OtherTenant_Returns404`
  - `Warehouses_Update_OtherTenant_Returns404`
  - `Warehouses_Delete_OtherTenant_Returns404`
- Full tenant-boundary suite: **13 tests, 0 skipped, 0 failed.**

## Remaining warehouse risks

- **Production database**: If the production (or staging) database does not yet have a `Warehouses` table, a migration must be added and applied to create it. The new configuration defines the expected schema; add an EF Core migration from the API project when deploying to a database that does not have this table.
- **TenantSafetyGuard**: `Warehouse` is not in the list of tenant-scoped entity types in `TenantSafetyGuard.IsTenantScopedEntityType` (it inherits `BaseEntity`, not `CompanyScopedEntity`). Warehouse writes are still tenant-scoped at the API/controller level (companyId from `ITenantProvider` and cross-tenant checks). If you later want SaveChanges-time enforcement for Warehouse, you would add it to the guard’s tenant-scoped set and ensure all warehouse writes run under tenant scope or platform bypass.
