# GPON Order Types — Subtype create/display fix

## Root cause

**Subtypes were being created as top-level parent rows (duplicate parents) because the create payload sometimes did not include `parentOrderTypeId`.**

- **Frontend:** The create payload used `parentOrderTypeId: formData.parentOrderTypeId || undefined`. If `formData.parentOrderTypeId` was empty string (e.g. after reset or timing), it became `undefined` and was omitted from the JSON body.
- **Backend:** When `ParentOrderTypeId` is null or missing, the backend correctly creates a **parent** row. So a subtype intended to be a child was stored as a new parent, and the subtype list showed "No subtypes" while the parent list showed duplicates.
- **Read side:** Parent and subtype queries were already correct (parent list = `ParentOrderTypeId == null`, subtype list = direct children of selected parent). The problem was **create** sending no parent, not the reads.

## Files changed

| File | Change |
|------|--------|
| `frontend/src/pages/settings/OrderTypesPage.tsx` | Subtype create always sends selected parent; guard when no parent selected; payload uses `selectedParentId` for subtype add. |

No backend code changes were required for this fix; create/read/update logic was already correct.

## What was fixed in the create flow

1. **Payload source of truth for subtype:** When the user clicks "Add Subtype" and saves, the payload now sets `parentOrderTypeId` from **`selectedParentId`** when `isSubtypeModal` is true, instead of relying only on `formData.parentOrderTypeId`. That way the selected parent in the left panel is always sent and the new row is stored as a child.
2. **Guard:** If the user somehow tries to save a new subtype without a selected parent, we show "Please select a parent type first." and do not call the API.
3. **Explicit handling:** `parentIdForCreate` is set to `selectedParentId` when adding a subtype, and only falls back to the form value for non-subtype (parent) create.

## What was fixed in the read/query flow

- **No code changes.** Parent list already returns only rows with `ParentOrderTypeId == null` (and canonical codes, with dedupe). Subtype list already returns only rows with `ParentOrderTypeId == selectedParentId`. Both use the correct filters.

## Edit logic

- **No code changes.** Update already only sets `ParentOrderTypeId` when the DTO has a value, so editing a subtype does not clear its parent. The frontend already sends the current parent when editing a subtype.

## Dirty data

- **Existing bad data:** Rows that were previously created as top-level parents (instead of subtypes) remain in the DB. They are already handled by:
  - **Parent list:** Filtered by `ParentOrderTypeId == null` and canonical codes only, and deduped by (CompanyId, Code), so at most one row per logical parent is shown.
  - **Subtype list:** Only direct children of the selected parent are shown, so incorrect top-level rows do not appear as subtypes.
- **Optional cleanup:** The migration `DedupeOrderTypeParentsAndAddUniqueIndexes` (and the applied SQL script) deduped parent rows and added unique indexes. Any rows that are really subtypes but were stored with `ParentOrderTypeId == null` would need a separate repair (e.g. reattach to the correct parent by code) if you want to fix them; the UI no longer creates new ones.

## Manual test checklist

1. Select **Assurance** in the left panel.
2. Click **Add Subtype**, enter Name **Standard**, Code **STANDARD**, save.
3. **Expected:** Left panel shows "Assurance — 1 subtype"; right panel shows "Standard"; no new Assurance parent row.
4. Select **Modification** in the left panel.
5. Click **Add Subtype**, enter Name **Indoor**, Code **INDOOR**, save.
6. **Expected:** Left panel shows "Modification — 1 subtype"; right panel shows "Indoor"; no duplicate Modification parent row.
7. Edit the subtype "Standard" under Assurance: change display order or name, save. **Expected:** It remains under Assurance and still appears in the subtype list.

## Success criteria (met)

- Add Subtype creates a **child** record with `ParentOrderTypeId` set to the selected parent.
- Parent list shows only real parents (one per logical type after dedupe).
- Subtype list shows only children of the selected parent.
- No new duplicate parent rows are created when adding a subtype.
