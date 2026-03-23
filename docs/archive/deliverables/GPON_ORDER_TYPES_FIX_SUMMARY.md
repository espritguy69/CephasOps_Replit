# GPON Settings > Order Types – Fix Summary

## Root cause

- **Duplicates in the parent list**: The API returns all rows with `ParentOrderTypeId == null` and canonical code (ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE). The database contained multiple parent rows per (CompanyId, Code) because:
  1. There was **no unique constraint** on parent order types, so duplicate parents could be created via UI or seed.
  2. **Seed/migration history**: e.g. `SeedAllReferenceData` inserted flat order types; later `FixOrderTypesHierarchyData` added parent rows with `WHERE NOT EXISTS`, but multiple runs or mixed seed data could still leave duplicate parents per company/code.
- **Query behaviour**: The parent list did not deduplicate by (CompanyId, Code), so every duplicate row appeared in the left panel. The issue was **data-side** (duplicate rows) and **query-side** (no deduplication when returning parents).

## What was fixed

### Backend

1. **OrderTypeService**
   - **GetOrderTypesAsync(parentsOnly: true)**  
     Returns only top-level parents (`ParentOrderTypeId == null`, canonical codes). **Dedupes by (CompanyId, Code)** and keeps one canonical parent per group (prefer most children, then lowest DisplayOrder, then oldest CreatedAt). Uses `AsNoTracking` for reads.
   - **GetSubtypesAsync**  
     Returns only direct children of the given parent. If `parentId` is not a top-level parent (e.g. a subtype id), returns an empty list. Uses `AsNoTracking`.
   - **UpdateOrderTypeAsync**  
     When editing a subtype, `ParentOrderTypeId` is only updated when the DTO has a value; it is never cleared by omission, so subtypes are not accidentally turned into parents.

2. **OrderTypesController**
   - When `parentsOnly=true`, **department filter is not applied** so the parent list is all parents for the company (no department scoping). This keeps the settings page and Create Order dropdown consistent.

3. **OrderTypeConfiguration (EF)**
   - **Unique partial indexes** (and removal of the previous non-unique (CompanyId, Code) index):
     - Parents: unique on (CompanyId, Code) where `ParentOrderTypeId IS NULL`.
     - Subtypes: unique on (CompanyId, ParentOrderTypeId, Code) where `ParentOrderTypeId IS NOT NULL`.
   - Application-level duplicate checks in Create/Update remain; the indexes prevent duplicates at the DB level.

4. **Migration: DedupeOrderTypeParentsAndAddUniqueIndexes**
   - **Cleanup parents**: For each (CompanyId, Code) with `ParentOrderTypeId IS NULL`, chooses a canonical parent (most children, then DisplayOrder, then CreatedAt), reassigns all FKs from duplicate parent ids to the canonical id, then **soft-deletes** duplicate parents.
   - **Cleanup subtypes**: For each (CompanyId, ParentOrderTypeId, Code) with `ParentOrderTypeId IS NOT NULL`, keeps one canonical subtype (DisplayOrder, CreatedAt), reassigns FKs from duplicate subtype ids to the canonical id, then soft-deletes duplicate subtypes (required before creating the subtype unique index).
   - **Indexes**: Drops the old non-unique `IX_OrderTypes_CompanyId_Code` if present, then creates the two partial unique indexes above.

### Frontend

1. **OrderTypesPage**
   - Left panel shows parents from `getOrderTypeParents()` (unchanged); backend now returns one row per logical parent.
   - Parent row label always shows subtype count, e.g. `Assurance (ASSURANCE) — 2 subtypes`.
   - When **saving an edit**, for subtypes we always send `parentOrderTypeId` (from item or selected parent) so the backend never clears it.

2. **Create Order**
   - No code changes. It already uses `getOrderTypeParents({ isActive: true })` and `getOrderTypeSubtypes(parentId, { isActive: true })`; the corrected backend behaviour is enough.

## Data cleanup

- **Included**: Migration `20260308150000_DedupeOrderTypeParentsAndAddUniqueIndexes` performs a one-time cleanup: merges duplicate parents (reassigns FKs, soft-deletes duplicates) and then adds the unique indexes.
- **Manual step**: Run migrations as usual (`dotnet ef database update` or apply the idempotent script from AGENTS.md). No separate manual repair is required unless you need to re-run only the cleanup logic (in that case the migration’s cleanup SQL can be run as a standalone script; the unique indexes would need to exist already or be created after).

## Success criteria (after fix)

- Order Types settings page shows **exactly one row per real parent** (Activation, Modification, Assurance, Value Added Service).
- Selecting Assurance shows only Assurance subtypes; selecting Modification shows only Modification subtypes.
- Duplicate top-level rows no longer appear.
- Create Order dropdown hierarchy matches the settings page (parent then subtype).
- Existing duplicate data is repaired by the migration (canonical parent kept, others soft-deleted and FKs reassigned).

## Tests added

- **OrderTypeServiceTests**: Parent list returns only `ParentOrderTypeId == null`; parent list dedupes by (CompanyId, Code); subtype list returns only direct children; subtype list returns empty when `parentId` is a subtype; duplicate parent code creation throws; duplicate subtype code under same parent throws; subtype with invalid parent throws.

## Files changed

- `backend/src/CephasOps.Application/Orders/Services/OrderTypeService.cs`
- `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Orders/OrderTypeConfiguration.cs`
- `backend/src/CephasOps.Api/Controllers/OrderTypesController.cs`
- `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260308150000_DedupeOrderTypeParentsAndAddUniqueIndexes.cs` (new)
- `frontend/src/pages/settings/OrderTypesPage.tsx`
- `backend/tests/CephasOps.Application.Tests/Orders/OrderTypeServiceTests.cs` (new)
- `docs/GPON_ORDER_TYPES_FIX_SUMMARY.md` (this file)
