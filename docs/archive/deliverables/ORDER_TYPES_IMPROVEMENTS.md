# Order Types — Post-Implementation Improvements

**Date:** 2026-03-08

This document describes the safe, logic-only improvements applied to the Order Types system after the Option B hierarchy was in place. It also states rules for future developers so that flat/hardcoded logic is not reintroduced.

---

## Rules for Future Developers

- **Order Types are a parent/child hierarchy.** Parents (e.g. Activation, Modification, Assurance, Value Added Service) have optional subtypes (e.g. Indoor, Outdoor under Modification). The database stores `OrderTypes.ParentOrderTypeId`; parents have `ParentOrderTypeId = NULL`.

- **Orders store one final `OrderTypeId`.** An order references a single row in `OrderTypes` (either a parent used as a leaf, or a subtype). Create Order sends one `orderTypeId` in the payload.

- **Create Order must use the API only.** Parent and subtype lists must come from `GET /api/order-types?parentsOnly=true` and `GET /api/order-types/{id}/subtypes`. Do not hardcode parent or subtype codes or names in Create Order.

- **CreateOrderPage.tsx is the UI design source of truth.** When changing data logic (validation, API usage, dropdown behaviour), do **not** redesign the page. Do **not** change layout, spacing, cards, sections, or styling. Only make safe logic/usability changes.

- **Do not reintroduce legacy flat rows.** The system supports a single hierarchy (parent → subtypes). Do not add new top-level order types that duplicate the role of subtypes (e.g. avoid new flat codes like MODIFICATION_INDOOR as a parent).

---

## Order Type Hierarchy

Order types form a two-level hierarchy: **parents** (top-level) and **subtypes** (children of a parent). Parents have `ParentOrderTypeId = NULL`; subtypes have `ParentOrderTypeId` set to their parent’s id.

Canonical structure:

```
Order Type
    ├─ ACTIVATION
    ├─ MODIFICATION
    │      ├─ INDOOR
    │      └─ OUTDOOR
    ├─ ASSURANCE
    │      ├─ STANDARD
    │      └─ REPULL
    └─ VALUE_ADDED_SERVICE
           ├─ UPGRADE
           ├─ IAD
           └─ FIXED_IP
```

- **ACTIVATION** has no subtypes; the parent itself is used as the order type.
- **MODIFICATION**, **ASSURANCE**, and **VALUE_ADDED_SERVICE** have subtypes; Create Order requires the user to pick a subtype when one of these parents is selected.

---

## Create Order Behaviour

- **Parent dropdown:** Loaded from `GET /api/order-types?parentsOnly=true&isActive=true` — only active parents are shown.
- **Subtype dropdown:** Shown only when the selected parent has subtypes. Loaded from `GET /api/order-types/{parentId}/subtypes?isActive=true` — only active subtypes.
- **Submitted OrderTypeId:** If the parent has no subtypes, the order uses the parent’s id. If the parent has subtypes, the order uses the selected subtype’s id (subtype selection is required).
- **Parent change:** When the user changes the parent, the subtype selection is cleared and any draft-related warnings are cleared.
- **Validation:** On submit, the selected parent must be in the active parent list. If the parent has subtypes, a subtype must be selected and must be in the active subtype list.

---

## Active vs Inactive Rules

- **Create Order:** Only **active** parents and **active** subtypes appear in dropdowns. New orders cannot use inactive types.
- **Server:** Order create (and create-from-draft) validate that the chosen OrderType exists and is active; otherwise the request fails with a clear error.
- **Settings:** All types (active and inactive) are shown so admins can activate/deactivate and manage codes.
- **Existing orders:** Orders that reference inactive types still load and display; only **new** order creation is restricted to active types.

---

## Draft Handling

- **Inactive parent in draft:** Parent is not pre-filled; an amber message asks the user to select an active order type. Submission is blocked until an active parent is chosen.
- **Inactive or missing subtype in draft:** Subtype is not pre-filled; an amber message asks the user to select an active subtype. Submission is blocked until an active subtype is chosen when the parent has subtypes.
- **Parent change:** Clearing or changing the parent clears the subtype and clears draft warning state.

---

## Deletion Rules

- **Used by orders:** An order type **cannot be deleted** if any order has `OrderTypeId` equal to that type. Error: *"Order type cannot be deleted because it is used by existing orders."*
- **Parent with subtypes:** A parent **cannot be deleted** if it has any subtypes. Error: *"Cannot delete a parent that has subtypes. Delete or reassign the subtypes first."*
- **Safe approach:** Deactivate the type instead of deleting when it is in use; delete only when no orders reference it and (for parents) no subtypes exist.

---

## Active vs inactive behaviour

- **Create Order:** Only **active** parent order types and **active** subtypes are shown in the dropdowns. The API is called with `isActive: true` for parents and `isActive: true` for subtypes so users cannot select inactive types for new orders.
- **Drafts:** If a draft references an **inactive parent**, the parent is not pre-filled; an amber message asks the user to select an active order type. If a draft references an **inactive or missing subtype**, the subtype is cleared and an amber message asks the user to select an active subtype. Submission is blocked until valid active choices are made.
- **Settings (/settings/gpon/order-types):** Admins see **all** parents and **all** subtypes (active and inactive). The API is called without `isActive` so the full list is returned. Admins can toggle status (activate/deactivate) and manage inactive rows.
- **Existing orders:** Orders that reference inactive order types still load and display; no breaking changes to rates, payroll, or order display. Only **new** order creation is restricted to active types.

---

## Improvements Completed

### 1. Require subtype when parent has children

- **Problem:** Value Added Service (and any parent with children) could be submitted with only the parent id.
- **Change:** In Create Order submit validation, if the selected parent has one or more subtypes (`childCount > 0` or subtype list length > 0), subtype selection is required. Derived from API data; no hardcoded parent names.
- **Validation message:** "Please select a subtype for this order type."
- **Files:** `frontend/src/pages/orders/CreateOrderPage.tsx` (onSubmit validation, label shows `*` when `hasSubType`, error shown for any parent with children).

### 2. Settings page clarity (Order Types)

- **Change:** On `/settings/gpon/order-types`:
  - Short descriptions under section headers: "Top-level types; select one to manage its subtypes below." and "Child types under the selected parent; shown in Create Order when that parent is chosen."
  - Subtype count shown per parent (e.g. "— 3 subtypes").
  - Selected parent state made obvious with ring and "(selected)" label.
  - Empty state copy clarified when a parent has no subtypes.
- **Files:** `frontend/src/pages/settings/OrderTypesPage.tsx`. No layout or styling redesign.

### 3. Prevent duplicate / invalid data (backend)

- **Duplicate codes:** Create and Update validate that no other order type in the same scope has the same code (same company; for subtypes, same parent).
- **Duplicate subtype under same parent:** Create rejects if a subtype with the same code already exists under that parent.
- **Self-parent:** Update rejects if `ParentOrderTypeId` is set to the same entity id.
- **Circular parent:** Update rejects if the new parent is this type or any of its descendants (helper `GetDescendantIdsAsync`).
- **Delete when in use:** Delete already blocked when any order references the type; message: "Order type cannot be deleted because it is used by existing orders."
- **Delete parent with children:** Already blocked; message clarified to "Cannot delete a parent that has subtypes. Delete or reassign the subtypes first."
- **Files:** `backend/src/CephasOps.Application/Orders/Services/OrderTypeService.cs` (CreateOrderTypeAsync, UpdateOrderTypeAsync, DeleteOrderTypeAsync).

### 4. Order Types API safety

- **parentsOnly=true:** Returns only canonical parents (ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE); children are never returned as parents.
- **Subtypes:** `GetSubtypesAsync` orders by `DisplayOrder` then `Name` (already in place); documented in code.
- **Inactive rows:** Handled by existing `isActive` query parameter; no change.
- **Files:** `backend/src/CephasOps.Application/Orders/Services/OrderTypeService.cs` (comments only; behaviour already correct).

### 5. Create Order runtime safety

- **Parent change:** When the parent dropdown changes, subtype selection is cleared immediately so the old subtype does not apply to the new parent.
- **Draft subtype missing/inactive:** If a draft references a subtype that is not in the API list (e.g. removed or inactive), a clear message is shown: "Draft referenced a subtype that is no longer available. Please select a subtype." No auto-select in this case so the user must choose.
- **API loading errors:** Existing behaviour kept: `loadSettings` catches errors and sets `loadError`; subtype fetch `.catch` sets empty list so the form does not break.
- **No subtype options:** Parent is treated as leaf; `resolvedOrderTypeId = orderSubTypeIdValue || orderTypeParentIdValue` unchanged.
- **Files:** `frontend/src/pages/orders/CreateOrderPage.tsx`. Logic only; no design changes.

### 6. Documentation

- This file: rules for developers, list of improvements, validation rules, and confirmation that CreateOrderPage design was preserved.

### 7. Inactive Order Type / Subtype handling

- **Create Order active-only:** Parent dropdown loads with `isActive: true`; subtype dropdown loads with `isActive: true` so only active options are shown.
- **Draft safety:** If a draft references an inactive parent (not in active list), `draftInactiveParentWarning` is set and an amber message is shown; parent is not pre-filled. If a draft references an inactive or missing subtype, `draftSubtypeUnavailable` is set and the user must select an active subtype. Submission is blocked until valid active parent/subtype are selected.
- **Parent/subtype switching:** When the parent changes, subtype is cleared and both warning flags are cleared. When the subtype list (active only) loads and the current selection is not in the list (e.g. inactive), subtype is cleared and `draftSubtypeUnavailable` is set.
- **API:** `GET /api/order-types?parentsOnly=true&isActive=true` returns active parents (Create Order). `GET /api/order-types/{id}/subtypes?isActive=true` returns active subtypes (Create Order). Omit `isActive` for settings (all). No `includeInactive` flag added; behaviour is controlled by the existing `isActive` query parameter (parents) and new optional `isActive` on subtypes.
- **Validation:** On submit, the selected parent must be in the active parent list and the selected subtype (when required) must be in the active subtype list; otherwise a clear error is shown. Uses API-derived data (no hardcoded labels).
- **Settings:** No change; already loads all parents (`isActive` undefined) and all subtypes (no `isActive` passed).
- **Files:** `backend` (OrderTypeService.GetSubtypesAsync + controller: optional `isActive`), `frontend/api/orderTypes.ts` (getOrderTypeSubtypes optional params), `frontend/src/pages/orders/CreateOrderPage.tsx` (logic only; new state `draftInactiveParentWarning`, messages, validation).

---

## Validation Rules Added (summary)

| Rule | Location | Message / behaviour |
|------|----------|---------------------|
| Subtype required when parent has children | CreateOrderPage onSubmit | "Please select a subtype for this order type." |
| Duplicate parent code | OrderTypeService Create | "A parent order type with code \"{code}\" already exists." |
| Duplicate subtype code under same parent | OrderTypeService Create | "A subtype with code \"{code}\" already exists under this parent." |
| Duplicate code on update (same scope) | OrderTypeService Update | "Another order type with code \"{code}\" already exists in this scope." |
| Self-parent | OrderTypeService Update | "An order type cannot be its own parent." |
| Circular parent | OrderTypeService Update | "Cannot set parent: would create a circular relationship." |
| Delete when type used by orders | OrderTypeService Delete | "Order type cannot be deleted because it is used by existing orders." |
| Delete parent with children | OrderTypeService Delete | "Cannot delete a parent that has subtypes. Delete or reassign the subtypes first." |
| Inactive parent on submit | CreateOrderPage onSubmit | "Please select an active order type." (parent must be in active list) |
| Inactive subtype on submit | CreateOrderPage onSubmit | "Please select an active subtype for this order type." (subtype must be in active list when parent has children) |

---

## API behaviour (Order Types)

- **GET /api/order-types:** `isActive` (optional) filters by active status; `parentsOnly=true` returns only canonical parents. Create Order uses `isActive=true&parentsOnly=true`; Settings uses no `isActive` (or `isActive` undefined) to get all.
- **GET /api/order-types/{id}/subtypes:** `isActive` (optional, query). When `isActive=true`, only active subtypes are returned (Create Order). When omitted, all subtypes are returned (Settings). Ordering is always by DisplayOrder then Name.

---

## Files Changed

| File | Changes |
|------|---------|
| `frontend/src/pages/orders/CreateOrderPage.tsx` | Subtype required when parent has children; clear subtype on parent change; draft subtype unavailable state and message; draft inactive parent warning; active-only parents/subtypes; validation for active parent/subtype on submit; no design changes. |
| `frontend/src/pages/settings/OrderTypesPage.tsx` | Section descriptions; subtype count per parent; selected state (ring + "(selected)"); clearer empty states. |
| `frontend/src/api/orderTypes.ts` | `getOrderTypeSubtypes(parentId, params?)` with optional `params.isActive` for active-only subtypes. |
| `backend/src/CephasOps.Application/Orders/Services/OrderTypeService.cs` | Create: duplicate code validation (parent and subtype). Update: self-parent and circular parent checks; duplicate code on update. Delete: clearer error messages. Helper: `GetDescendantIdsAsync`. `GetSubtypesAsync`: optional `isActive` filter. |
| `backend/src/CephasOps.Application/Orders/Services/IOrderTypeService.cs` | `GetSubtypesAsync` signature: added optional `isActive` parameter. |
| `backend/src/CephasOps.Api/Controllers/OrderTypesController.cs` | GET `{id}/subtypes`: optional query `isActive`. |
| `docs/ORDER_TYPES_IMPROVEMENTS.md` | Rules, improvements list, validation summary, active vs inactive behaviour, API behaviour, files changed. |

---

## Confirmation: CreateOrderPage Design Preserved

- No layout, spacing, cards, or section structure changed.
- No styling or CSS class changes except where necessary for the new error/message (same pattern as existing destructive/amber text).
- Only additions: state variables (`draftSubtypeUnavailable`, `draftInactiveParentWarning`), short message lines under the order type and subtype dropdowns, and logic changes in validation and effects. Dropdowns, labels, and surrounding UI are unchanged.

---

## Optional Improvements for Later

- **Reassign orders before delete:** A dedicated "Reassign orders" flow before allowing delete of a type in use is out of scope; current guidance is to deactivate or reassign orders manually.
- **Bulk re-parent subtypes:** No admin UI for moving multiple subtypes to another parent; can be done via API or DB if needed.
