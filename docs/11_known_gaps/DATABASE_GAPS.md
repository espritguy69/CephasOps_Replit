# Database Known Gaps

**Date:** April 2026  
**Status:** Active — these are unresolved database/schema risks

---

## DG-1: Missing DbSets — Runtime Crash Risk

**Severity:** Critical  
**Entities affected:**
- `NotificationTemplate` — Entity exists in `CephasOps.Domain/Settings/Entities/NotificationTemplate.cs`, has no DbSet in `ApplicationDbContext`, no migration, no table
- `ReportDefinition` — Entity exists in `CephasOps.Domain/Settings/Entities/ReportDefinition.cs`, has no DbSet, no migration, no table

**Impact:** `NotificationTemplateService` calls `_context.Set<NotificationTemplate>()` which will throw a "table not found" exception at runtime. `AutomationRule` and `EscalationRule` entities have `NotificationTemplateId` foreign keys pointing to a non-existent table.

**Fix Required:**
1. Add `public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();` to `ApplicationDbContext.cs`
2. Add `public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();` to `ApplicationDbContext.cs`
3. Create entity configurations in `Persistence/Configurations/`
4. Generate and apply a new EF Core migration
5. Consider whether `NotificationTemplate` should inherit from `CompanyScopedEntity` (it has `CompanyId` but inherits from `BaseEntity`, so it won't get automatic tenant query filters)

---

## DG-2: Dual Migration Strategy — Schema Drift Risk

**Severity:** High  
**Description:** The project uses both:
- **EF Core C# migrations** in `backend/src/CephasOps.Infrastructure/Persistence/Migrations/`
- **Manual SQL scripts** in `backend/scripts/` and `backend/src/CephasOps.Infrastructure/Persistence/Migrations/` (SQL files)

**Impact:** The C# model and actual database schema can drift apart. Tables created via SQL won't be tracked in EF Core's model snapshot, causing future migrations to attempt re-creating them or missing columns.

**Current Mitigation:** `migrations-idempotent-latest.sql` is generated as an idempotent SQL output of all C# migrations. But manual SQL fixes (13 files in `backend/`) may not be reflected in the C# model.

**Fix:** Standardize on one approach. If using C# migrations, all schema changes must go through `dotnet ef migrations add`. If using SQL scripts, remove C# migrations entirely.

---

## DG-3: Phantom Migration Records

**Severity:** High  
**Description:** Three migration transactions rolled back but their IDs were recorded in `__EFMigrationsHistory`:
- `20251202155910_AddPartnerGroupIdToBillingRatecard`
- `20251202174653_AddSoftDeleteToCompanyScopedEntities`
- `20260310031127_AddExternalIntegrationBus`

**Impact:** EF Core thinks these migrations were applied, so it won't re-run them. But the tables/columns they were supposed to create don't exist.

**Fix:** Run `backend/scripts/fix-missing-integration-tables.sql` to delete these phantom records, then re-run migrations.

---

## DG-4: Migration ON_ERROR_STOP=0

**Severity:** High  
**Location:** `deploy-vps-native.sh` line 233  
**Description:** The deployment script runs migrations with `ON_ERROR_STOP=0`, meaning psql continues executing even when a statement fails.

**Impact:** Database can end up in an inconsistent state with some tables created and others missing — which is exactly the bug that was debugged in the recent deployment.

**Fix:** Change to `ON_ERROR_STOP=1` or add per-statement error checking.

---

## DG-5: Tenant Isolation Gap on NotificationTemplate

**Severity:** Medium  
**Description:** `NotificationTemplate` has a `CompanyId` property but inherits from `BaseEntity` instead of `CompanyScopedEntity`.

**Impact:** If the table is ever created, it won't receive automatic tenant query filters. Queries could return templates from other tenants.

**Fix:** Change inheritance to `CompanyScopedEntity` or add a manual query filter in `ApplicationDbContext.OnModelCreating()`.

---

## DG-6: Inline Entity Configurations

**Severity:** Low  
**Description:** `StockAllocation`, `StockLedgerEntry`, `OrderType`, and several other entities have their EF Core configurations defined inline in `ApplicationDbContext.OnModelCreating()` instead of in dedicated files in `Persistence/Configurations/`.

**Impact:** Inconsistent with the project's pattern (188 other entities use separate configuration files). Harder to maintain and review.

**Fix:** Extract inline configurations to `Configurations/StockAllocationConfiguration.cs` etc.
