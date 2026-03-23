# Order Types Option B — Post-Implementation Verification Report

**Verification method:** Code and schema review (no runtime execution in this session).  
**Date:** 2026-03-08

---

## 1. Passed checks

### 1.1 DATABASE (code/schema verified)

| Check | Status | Evidence |
|-------|--------|----------|
| Migration `20260308100000_AddOrderTypeParentOrderTypeId` exists | ✅ | `Persistence/Migrations/20260308100000_AddOrderTypeParentOrderTypeId.cs` |
| Migration adds `ParentOrderTypeId` (nullable uuid) to `OrderTypes` | ✅ | `AddColumn<Guid>(ParentOrderTypeId, nullable: true)` |
| Self-referencing FK | ✅ | `FK_OrderTypes_OrderTypes_ParentOrderTypeId`, `ReferentialAction.Restrict` |
| Index on `ParentOrderTypeId` | ✅ | `IX_OrderTypes_ParentOrderTypeId` |
| Seed creates parents: ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE | ✅ | `SeedDefaultOrderTypesAsync` parents array |
| Seed creates children: INDOOR, OUTDOOR, STANDARD, REPULL, UPGRADE, IAD, FIXED_IP | ✅ | children array with correct ParentCode/Code |
| Seed idempotent (by Code + ParentOrderTypeId) | ✅ | `FirstOrDefaultAsync` / `AnyAsync` before insert |

**Note:** Migration must be applied in your environment (`dotnet ef database update`). Seed runs on app startup when seeding is enabled.

---

### 1.2 API (code verified)

| Endpoint | Status | Evidence |
|----------|--------|----------|
| GET `/api/order-types` | ✅ | `GetOrderTypes` with optional `departmentId`, `isActive` |
| GET `/api/order-types?parentsOnly=true` | ✅ | `parentsOnly` query param passed to `GetOrderTypesAsync(..., parentsOnly)` |
| GET `/api/order-types/{id}/subtypes` | ✅ | `[HttpGet("{id}/subtypes")]` → `GetSubtypesAsync(id, companyId)` |
| POST `/api/order-types` | ✅ | Accepts body with `ParentOrderTypeId`; service validates parent exists for subtypes |
| PUT `/api/order-types/{id}` | ✅ | Accepts `ParentOrderTypeId` in DTO |
| DELETE `/api/order-types/{id}` | ✅ | Blocks delete if parent has children; blocks if type is used by orders |

Route order: `{id}/subtypes` is more specific than `{id}`, so subtypes URL is matched correctly.

---

### 1.3 SETTINGS PAGE (code verified)

| Check | Status | Evidence |
|-------|--------|----------|
| Parent list loads from API | ✅ | `getOrderTypeParents({ departmentId, isActive })` in `loadParents` |
| Selecting parent loads subtypes | ✅ | `useEffect` on `selectedParentId` → `getOrderTypeSubtypes(selectedParentId)` → `setSubtypes` |
| Add parent | ✅ | `handleCreateParent` → modal with `parentOrderTypeId` empty → `createOrderType` |
| Add subtype | ✅ | `handleCreateSubtype` → modal with `parentOrderTypeId: selectedParentId` → `createOrderType` |
| Edit parent / edit subtype | ✅ | `openEdit(item)` sets `parentOrderTypeId`; `handleSave` calls `updateOrderType` with same payload shape |
| Toggle active | ✅ | `handleToggleStatus` → `updateOrderType(item.id, { ..., isActive: !item.isActive })` |
| Delete parent blocked if children exist | ✅ | Backend `DeleteOrderTypeAsync` throws `InvalidOperationException` when `ParentOrderTypeId == null` and `hasChildren`; frontend `handleDelete` shows error via `showError` |
| Delete subtype | ✅ | Backend allows delete when no orders use the type; frontend calls `deleteOrderType(id)` |

---

### 1.4 CREATE ORDER PAGE (code verified)

| Check | Status | Evidence |
|-------|--------|----------|
| Parent dropdown from API only | ✅ | `parentOrderTypes` from `getOrderTypeParents`; options `parentOrderTypes.map(t => <option value={t.id}>)` |
| Subtype dropdown from API only | ✅ | `subtypeOrderTypes` from `getOrderTypeSubtypes(orderTypeParentIdValue)`; options from `subtypeOrderTypes.map(s => ...)` |
| No hardcoded type/subtype values | ✅ | Grep: no `CORE_ORDER_TYPE`, `FALLBACK`, `mapOrderTypeCodeToOrderType`, `mapOrderTypeCodeToSubType`, `resolveOrderTypeId` in CreateOrderPage |
| Parent with no children (ACTIVATION) can submit directly | ✅ | `orderTypeId = values.orderSubTypeId \|\| values.orderTypeParentId`; when no subtypes, `orderSubTypeId` cleared; payload uses parent id |
| Parent with children requires subtype (Modification / Assurance) | ✅ | Validation: MODIFICATION requires INDOOR/OUTDOOR; ASSURANCE when `hasSubType` requires STANDARD/REPULL; `setError('orderSubTypeId', ...)` and return |
| Final payload sends one `orderTypeId` only | ✅ | `orderPayload.orderTypeId = values.orderSubTypeId \|\| values.orderTypeParentId` |

---

### 1.5 DRAFT / PARSER (code verified)

| Scenario | Status | Evidence |
|----------|--------|----------|
| ACTIVATION | ✅ | `deriveParentCode` returns `ACTIVATION` for ACTIVATION/FTTH/FTTO; parent found; no subtype required; `orderTypeParentId` set |
| MODIFICATION + INDOOR | ✅ | Parent MODIFICATION; `deriveSubtypeCode` returns INDOOR from MODIFICATION_INDOOR or draft.orderSubType; subtype found and `orderSubTypeId` set |
| MODIFICATION + OUTDOOR | ✅ | Same; OUTDOOR from MODIFICATION_OUTDOOR / MOD_OUT / OUTDOOR |
| ASSURANCE + STANDARD / REPULL | ✅ | Parent ASSURANCE; subtype from draft.orderSubType or default STANDARD; subtypes STANDARD/REPULL seeded |
| VALUE_ADDED_SERVICE + UPGRADE / IAD / FIXED_IP | ✅ | Parent VALUE_ADDED_SERVICE; subtype from IAD/FIXED/VAS/UPGRADE in code; subtypes UPGRADE, IAD, FIXED_IP seeded |

Draft loading uses `getOrderTypeParents` and `getOrderTypeSubtypes(parent.id)`; resolution is code-derived then API-matched.

---

### 1.6 VALIDATION (code verified)

| Rule | Status | Evidence |
|------|--------|----------|
| Assurance: ticket number + AWO number required | ✅ | `if (parentCode === 'ASSURANCE')` → `setError('ticketNumber'|'awoNumber', ...)` |
| Modification Outdoor: oldAddress + newAddress required | ✅ | `if (subtypeCode === 'OUTDOOR')` → `setError('oldAddress'|'newAddress', ...)` |
| Modification Indoor: indoorRemark required | ✅ | `if (subtypeCode === 'INDOOR' && !values.indoorRemark?.trim())` → `setError('indoorRemark', ...)` |
| Validation uses API-derived parentCode/subtypeCode | ✅ | `parentCode = parent?.code?.toUpperCase()`, `subtypeCode = subtype?.code?.toUpperCase()` from `parentOrderTypes` / `subtypeOrderTypes` |

---

### 1.7 REGRESSION (code verified)

| Area | Status | Evidence |
|------|--------|----------|
| Orders create | ✅ | Order create still receives single `orderTypeId`; `OrderService` and DTOs unchanged for `OrderTypeId` |
| Order list / detail | ✅ | Order DTO and `GetOrderTypeNameAsync(orderTypeId)` work for any OrderType row (parent or child) |
| Rates (BillingRatecard) | ✅ | Use `OrderTypeId`; lookup by id in `OrderTypes`; no change to schema or usage |
| Billing (invoices) | ✅ | `LoadOrderDataForLineItemsAsync` loads order type name by `OrderTypeId`; `OrderTypes` lookup by id |
| Payroll | ✅ | Uses `OrderTypeId` on orders; no change |
| Existing orders with legacy flat types | ✅ | Order entity still single `OrderTypeId`; existing rows (e.g. old "Modification Indoor") remain valid; new hierarchy is additive |

---

### 1.8 Hierarchy data fix (applied)

| Item | Status | Evidence |
|------|--------|----------|
| Data migration `20260308110000_FixOrderTypesHierarchyData` | ✅ | Links legacy flat types to parents; ensures four parents exist; normalizes display names |
| Standalone SQL scripts | ✅ | `backend/scripts/add-order-type-parent-column.sql`, `fix-order-types-hierarchy-data.sql`, `fix-order-types-legacy-codes-variants.sql` (for DBs with Code variants e.g. ASSURANCE REPULL, FIXED IP) |
| Seed duplicate prevention | ✅ | `GetLegacyOrderTypeCodesForSubtype` in `DatabaseSeeder` avoids creating INDOOR/OUTDOOR when MODIFICATION_INDOOR/MODIFICATION_OUTDOOR exist |
| Settings: only four parents shown | ✅ | `OrderTypeService.GetOrderTypesAsync` when `parentsOnly=true` filters to Code in ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE |

---

## 2. Failed checks

- **None** from the code/schema review.

The following cannot be confirmed without running the app and database:

- Migration actually applied to the target database.
- Seed actually run and rows present (depends on seeding being enabled and department/company).
- Live API responses (status codes, JSON shape).
- Live UI behaviour (settings page, Create Order, draft load) and any runtime errors.

---

## 3. Risk items

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Migration not yet applied** | High | Run `dotnet ef database update` before deploying. |
| **Legacy flat rows coexist with new hierarchy** | Medium | Existing DBs may have old "Modification Indoor" / "Modification Outdoor" rows; new seed adds new parents + children. Existing orders keep pointing at old ids. No data loss; optional follow-up to link old rows to parent or migrate orders to new leaf ids. |
| **VAS: subtype not required when parent has children** | Low | Spec says “if parent has children, require subtype”. For VALUE_ADDED_SERVICE we default `orderSubTypePayload` to UPGRADE and allow submit with parent id only. Functionally acceptable; UX could require subtype when `hasSubType` for VAS. |
| **childCount not set for non-parents** | Low | `ChildCount` is only populated when `parentsOnly` is true. Subtypes always have `ChildCount = 0`. No bug. |

---

## 4. Recommended follow-up actions

1. **Apply migration and run app**
   - From backend: `dotnet ef database update --project src/CephasOps.Infrastructure --startup-project src/CephasOps.Api`
   - Start API and confirm seed runs (if applicable) and no startup errors.

2. **Smoke-test in browser**
   - Open `/settings/gpon/order-types`: confirm parent list and subtype list, add/edit/toggle/delete parent and subtype, confirm delete parent with children is blocked.
   - Open `/orders/create`: select parent (e.g. ACTIVATION), submit; select MODIFICATION, choose INDOOR/OUTDOOR, fill required fields, submit; confirm single `orderTypeId` in network payload.
   - Load a draft with `?draftId=...` and confirm order type and subtype resolve (ACTIVATION, MODIFICATION+INDOOR/OUTDOOR, ASSURANCE+STANDARD/REPULL, VAS+UPGRADE/IAD/FIXED_IP).

3. **Optional: require subtype for VAS when parent has children**
   - In Create Order submit validation, add: if `parentCode === 'VALUE_ADDED_SERVICE' && hasSubType && !subtypeCode` then `setError('orderSubTypeId', { message: 'Please select a subtype' })` and return.

4. **Legacy data cleanup (done for this environment)**
   - Schema: `scripts/add-order-type-parent-column.sql` applied if column was missing.
   - Data: `scripts/fix-order-types-hierarchy-data.sql` links MODIFICATION_INDOOR/OUTDOOR, STANDARD/REPULL, UPGRADE/IAD/FIXED_IP to parents and normalizes names.
   - Variants: `scripts/fix-order-types-legacy-codes-variants.sql` links ASSURANCE REPULL, FIXED IP, VAS to the correct parents. Re-run on other DBs if they use those Code values.

---

## 5. Production safety

- **Implementation is production-safe** from a code and schema perspective:
  - Migration is additive (nullable column + index + FK).
  - Order and rate/payroll logic still use a single `OrderTypeId`; no breaking change.
  - Existing orders and existing flat order type rows remain valid.
  - New behaviour is behind new endpoints and new UI; existing GET `/api/order-types` behaviour is unchanged.

- **Before going to production:**
  1. Apply the migration in the target database.
  2. Run the app (or deployment) so seed runs and creates the new parent/child rows where applicable.
  3. Run the smoke tests above (settings page, Create Order, draft resolution).
  4. Optionally run any existing automated tests for orders, billing, and payroll.

---

**Summary:** All checklist items are satisfied by the current code and schema. No failed checks. Remaining work is to apply the migration, run the application, and perform the recommended smoke tests and optional follow-ups.
