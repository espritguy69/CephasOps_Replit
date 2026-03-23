# Order Types Option B — Implementation Summary

## 1. Files changed

### Backend
- **backend/src/CephasOps.Domain/Orders/Entities/OrderType.cs** — Added `ParentOrderTypeId`, `ParentOrderType`, `Children`.
- **backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs** — Configured self-reference and index on `ParentOrderTypeId`.
- **backend/src/CephasOps.Infrastructure/Persistence/Migrations/20260308100000_AddOrderTypeParentOrderTypeId.cs** — New migration (add column, index, FK).
- **backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs** — Updated for `ParentOrderTypeId` and relationship.
- **backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs** — `SeedDefaultOrderTypesAsync` rewritten: seed parents (ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE) then children (INDOOR, OUTDOOR, STANDARD, REPULL, UPGRADE, IAD, FIXED_IP).
- **backend/src/CephasOps.Application/Orders/DTOs/OrderTypeDto.cs** — Added `ParentOrderTypeId`, `ChildCount`; create/update DTOs include `ParentOrderTypeId`.
- **backend/src/CephasOps.Application/Orders/Services/IOrderTypeService.cs** — Added `parentsOnly` to `GetOrderTypesAsync`; added `GetSubtypesAsync`, `GetOrderTypeByCodeAsync`.
- **backend/src/CephasOps.Application/Orders/Services/OrderTypeService.cs** — Implemented parents-only filter, `GetSubtypesAsync`, `GetOrderTypeByCodeAsync`, create/update/delete with parent and child-count logic.
- **backend/src/CephasOps.Api/Controllers/OrderTypesController.cs** — Added `parentsOnly` query to `GetOrderTypes`; added `GET {id}/subtypes`; create handles `InvalidOperationException`.

### Frontend
- **frontend/src/api/orderTypes.ts** — Added `OrderTypeDto`, `getOrderTypeParents`, `getOrderTypeSubtypes`; `getOrderTypes` supports `parentsOnly`; create/update types include `parentOrderTypeId` and `displayOrder`.
- **frontend/src/pages/settings/OrderTypesPage.tsx** — Replaced with 2-panel layout: left = parent list (from API), right = subtypes for selected parent; CRUD for parents and subtypes; uses `getOrderTypeParents` and `getOrderTypeSubtypes`.
- **frontend/src/pages/orders/CreateOrderPage.tsx** — Removed all hardcoded order type/subtype logic; form uses `orderTypeParentId` and `orderSubTypeId`; loads parents via `getOrderTypeParents`, subtypes via `getOrderTypeSubtypes(parentId)`; draft resolution by code via API; validation by parent/subtype code; submit sends single `orderTypeId` (leaf = subtype id or parent id).

---

## 2. Migration name

- **20260308100000_AddOrderTypeParentOrderTypeId**

Adds nullable `ParentOrderTypeId` (uuid) to `OrderTypes`, index `IX_OrderTypes_ParentOrderTypeId`, and FK `FK_OrderTypes_OrderTypes_ParentOrderTypeId` (Restrict).

---

## 3. Seed strategy

- **Idempotent:** Upsert by `(CompanyId, Code)` for parents; for children by `(CompanyId, Code, ParentOrderTypeId)`.
- **Order:** Parents first (ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE); then children (MODIFICATION → INDOOR, OUTDOOR; ASSURANCE → STANDARD, REPULL; VALUE_ADDED_SERVICE → UPGRADE, IAD, FIXED_IP). ACTIVATION has no children.
- **Department/company:** Unchanged; seed still uses `companyId` and `departmentId` passed into `SeedDefaultOrderTypesAsync` (e.g. GPON department).
- **Existing DBs:** If the database already had flat order types (e.g. "Modification Indoor" with Code `MODIFICATION_INDOOR`), the new seed adds new parent rows (where no row with that Code and `ParentOrderTypeId = null` exists) and new child rows (Code INDOOR, OUTDOOR, etc.). Existing orders keep pointing at existing OrderTypeIds. For a fully consistent hierarchy on an existing DB, a one-off data migration could set `ParentOrderTypeId` on existing MODIFICATION_INDOOR/MODIFICATION_OUTDOOR rows to the new MODIFICATION parent id; not required for Create Order to work.

---

## 4. API endpoints added/updated

| Method | Endpoint | Change |
|--------|----------|--------|
| GET | `/api/order-types` | New query: `parentsOnly=true` returns only parent order types (e.g. for Create Order and settings). |
| GET | `/api/order-types/{id}/subtypes` | **New.** Returns subtypes of the given parent id. |
| GET | `/api/order-types` | Unchanged when `parentsOnly` is false; response includes `parentOrderTypeId` and `childCount` where applicable. |
| POST | `/api/order-types` | Body may include `parentOrderTypeId` to create a subtype. |
| PUT | `/api/order-types/{id}` | Body may include `parentOrderTypeId` to update parent link. |

---

## 5. Create Order behaviour summary

- **Load:** Parents from `GET /api/order-types?parentsOnly=true`. When user selects a parent, subtypes from `GET /api/order-types/{id}/subtypes`.
- **Form fields:** `orderTypeParentId`, `orderSubTypeId` (both stored as IDs). No hardcoded labels or codes in the UI.
- **Resolved leaf:** `orderTypeId = orderSubTypeId || orderTypeParentId`. Single `orderTypeId` is sent to the create-order API; orders still store one `OrderTypeId`.
- **Validation:** Assurance (parent code ASSURANCE): ticket number, AWO number, subtype Standard/Repull. Modification (parent code MODIFICATION): subtype Indoor/Outdoor; Outdoor → old/new address; Indoor → indoor remark. Done in submit handler using `parentOrderTypes` and `subtypeOrderTypes` (code-driven).
- **Draft/parser:** When loading a draft with `orderTypeCode` / `orderSubType`, parent and subtype are resolved by code: derive parent code from draft, find parent in `getOrderTypeParents()`, then load subtypes and find subtype by derived code; set `orderTypeParentId` and `orderSubTypeId`.

---

## 6. Dead code removed (CreateOrderPage)

- `CORE_ORDER_TYPE_LABELS`, `FALLBACK_ORDER_TYPES`
- `isAssuranceOrder`, `isModificationOrder`, `isIndoorModification`, `isOutdoorModification`, `isActivationOrder`, `isValueAddedServiceOrder`
- `mapOrderTypeCodeToOrderType`, `mapOrderTypeCodeToSubType`, `resolveOrderTypeId`
- `displayOrderTypes` memo
- Hardcoded subtype `<option>` blocks (Modification: Outdoor/Indoor; Assurance: Standard/Repull; VAS: Upgrade/IAD/Fixed IP)
- Label-based default subtype `useEffect` (replaced by defaulting to first subtype when parent has children)
- Schema `superRefine` for assurance/modification (replaced by submit-time validation using API data)

---

## 7. Compatibility and follow-up

- **Backward compatibility:** Existing `GET /api/order-types` (without `parentsOnly`) still returns all types; new fields `parentOrderTypeId` and `childCount` are additive. Create/update order payload still sends a single `orderTypeId`; no change to orders table or existing rates/payroll that use `OrderTypeId`.
- **Existing databases:** If you already have flat order types (e.g. "Modification Indoor"), the new seed does not remove them. New parent/child rows are added. For a single source of truth you may want a one-off script to (1) set `ParentOrderTypeId` on existing MODIFICATION_INDOOR/MODIFICATION_OUTDOOR (and optionally ASSURANCE/VAS) rows to the corresponding new parent, and/or (2) migrate existing orders to the new leaf ids. Not required for Create Order to function.
- **Rates/payroll:** Still keyed by `OrderTypeId` (leaf). New orders use the new leaf ids from the hierarchy; existing orders keep their current `OrderTypeId`. Any rate/payroll configuration that references order types by id continues to work; new configuration can use the new subtype (or parent) ids as needed.
