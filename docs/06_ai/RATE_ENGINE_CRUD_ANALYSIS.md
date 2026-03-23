# Rate Engine Page CRUD Analysis

**Date:** 2026-01-05  
**Page:** `/settings/gpon/rate-engine`  
**Component:** `frontend/src/pages/settings/RateEngineManagementPage.tsx`

---

## Executive Summary

✅ **CRUD Actions ARE Present** - The Rate Engine page already has Edit and Delete icons implemented using the `actions` prop on the `DataTable` component. However, the `DataTable` component may not support the `actions` prop, requiring a switch to an `actions` column instead.

---

## Backend Inventory

### ✅ RateCard Entity (Universal Rate Engine)

**Location:** `backend/src/CephasOps.Domain/Rates/Entities/RateCard.cs`

**Properties:**
- `Id` (Guid)
- `CompanyId` (Guid?)
- `VerticalId` (Guid?)
- `DepartmentId` (Guid?)
- `RateContext` (enum: GPON_JOB, NWO_SCOPE, etc.)
- `RateKind` (enum: REVENUE, PAYOUT, BONUS, COMMISSION)
- `Name` (string)
- `Description` (string?)
- `ValidFrom` (DateTime?)
- `ValidTo` (DateTime?)
- `IsActive` (bool)
- `Lines` (ICollection<RateCardLine>)

**Database Table:** `RateCards`

### ✅ RateCardLine Entity

**Location:** `backend/src/CephasOps.Domain/Rates/Entities/RateCardLine.cs`

**Properties:**
- `Id` (Guid)
- `RateCardId` (Guid)
- `Dimension1-4` (string?)
- `PartnerGroupId` (Guid?)
- `PartnerId` (Guid?)
- `RateAmount` (decimal)
- `UnitOfMeasure` (enum)
- `Currency` (string)
- `IsActive` (bool)

**Database Table:** `RateCardLines`

### ✅ RatesController

**Location:** `backend/src/CephasOps.Api/Controllers/RatesController.cs`

**Endpoints:**
- ✅ `GET /api/rates/ratecards` - Get all rate cards
- ✅ `GET /api/rates/ratecards/{id}` - Get single rate card
- ✅ `POST /api/rates/ratecards` - Create rate card
- ✅ `PUT /api/rates/ratecards/{id}` - Update rate card
- ✅ `DELETE /api/rates/ratecards/{id}` - Delete rate card
- ✅ `GET /api/rates/ratecards/{id}/lines` - Get rate card lines
- ✅ `POST /api/rates/ratecards/{id}/lines` - Create rate card line
- ✅ `PUT /api/rates/ratecardlines/{id}` - Update rate card line
- ✅ `DELETE /api/rates/ratecardlines/{id}` - Delete rate card line

**Status:** ✅ Full CRUD support exists

---

## Frontend Current State

### Page Component

**Location:** `frontend/src/pages/settings/RateEngineManagementPage.tsx`

**Current Implementation:**
- ✅ **3 Tabs:** Partner Revenue, SI Payouts, Custom Overrides
- ✅ **Create Buttons:** "Add Rate" / "Add Override" buttons present
- ✅ **Edit Handlers:** `openEditPartnerRate`, `openEditSiRate`, `openEditCustomRate`
- ✅ **Delete Handlers:** `handleDeletePartnerRate`, `handleDeleteSiRate`, `handleDeleteCustomRate`
- ✅ **Delete Confirmations:** `ConfirmDialog` components present
- ✅ **Modals:** Create/Edit modals for all 3 rate types

### ⚠️ Issue: Actions Column Implementation

**Current Code (Lines 751-760):**
```typescript
<DataTable
  data={partnerRates}
  columns={partnerRateColumns}
  actions={(row: GponPartnerJobRate) => (
    <div className="flex items-center gap-2">
      <button onClick={() => openEditPartnerRate(row)} className="text-blue-600 hover:opacity-75">
        <Edit className="h-3 w-3" />
      </button>
      <button onClick={() => setDeletingPartnerRate(row)} className="text-red-600 hover:opacity-75">
        <Trash2 className="h-3 w-3" />
      </button>
    </div>
  )}
/>
```

**Problem:** The `DataTable` component may not support the `actions` prop. Need to verify if it should be an `actions` column instead (like Service Installers page).

---

## Comparison with Reference Page

### Service Installers Page (Working Reference)

**Location:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

**Implementation:**
```typescript
const columns: TableColumn<ServiceInstaller>[] = [
  // ... other columns ...
  {
    key: 'actions',
    label: 'Actions',
    render: (_value, row) => (
      <div className="flex items-center gap-2">
        <button onClick={() => openEditModal(row)}>
          <Edit className="h-4 w-4" />
        </button>
        <button onClick={() => handleDelete(row.id)}>
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    ),
    width: '100px'
  }
];

<DataTable data={serviceInstallers} columns={columns} />
```

**Key Difference:** Uses an `actions` column instead of an `actions` prop.

---

## What's Missing

### ❌ Actions Column in Table Definitions

The Rate Engine page uses `actions` prop, but should use an `actions` column in the `columns` array for each table:

1. **Partner Rate Columns** - Missing `actions` column
2. **SI Rate Columns** - Missing `actions` column  
3. **Custom Rate Columns** - Missing `actions` column

---

## Step-by-Step Fix Checklist

### Step 1: Verify DataTable Component Support

- [ ] Check `frontend/src/components/ui/DataTable.tsx` to see if it supports `actions` prop
- [ ] If not supported, proceed to Step 2

### Step 2: Add Actions Column to Partner Rate Columns

**File:** `frontend/src/pages/settings/RateEngineManagementPage.tsx`

**Location:** Around line 532 (partnerRateColumns definition)

**Change:**
```typescript
const partnerRateColumns: TableColumn<GponPartnerJobRate>[] = [
  // ... existing columns ...
  {
    key: 'actions',
    label: 'Actions',
    render: (_value, row) => (
      <div className="flex items-center gap-2">
        <button
          onClick={(e) => {
            e.stopPropagation();
            openEditPartnerRate(row);
          }}
          title="Edit"
          className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
        >
          <Edit className="h-4 w-4" />
        </button>
        <button
          onClick={(e) => {
            e.stopPropagation();
            setDeletingPartnerRate(row);
          }}
          title="Delete"
          className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    ),
    width: '100px'
  }
];
```

### Step 3: Add Actions Column to SI Rate Columns

**Location:** Around line 554 (siRateColumns definition)

**Change:** Same pattern as Step 2, using `openEditSiRate` and `setDeletingSiRate`

### Step 4: Add Actions Column to Custom Rate Columns

**Location:** Around line 568 (customRateColumns definition)

**Change:** Same pattern as Step 2, using `openEditCustomRate` and `setDeletingCustomRate`

### Step 5: Remove `actions` Prop from DataTable Components

**Locations:**
- Line 751: Partner Rate DataTable
- Line 781: SI Rate DataTable
- Line 811: Custom Rate DataTable

**Change:** Remove the `actions={...}` prop from all three DataTable components

### Step 6: Test

- [ ] Verify Edit icons appear in all 3 tables
- [ ] Verify Delete icons appear in all 3 tables
- [ ] Test Edit functionality for each rate type
- [ ] Test Delete functionality for each rate type
- [ ] Verify delete confirmation dialogs work

---

## Files to Modify

1. ✅ `frontend/src/pages/settings/RateEngineManagementPage.tsx`
   - Add `actions` column to `partnerRateColumns` (around line 532)
   - Add `actions` column to `siRateColumns` (around line 554)
   - Add `actions` column to `customRateColumns` (around line 568)
   - Remove `actions` prop from DataTable components (lines 751, 781, 811)

---

## Summary

**Backend:** ✅ Full CRUD support exists  
**Frontend:** ⚠️ CRUD handlers exist but actions column missing from table definitions  
**Fix Required:** Add `actions` column to all 3 table column definitions and remove `actions` prop from DataTable components

**Estimated Time:** 15-20 minutes

---

## Notes

- The Rate Engine page manages GPON-specific rates (not universal RateCards)
- All CRUD handlers are already implemented
- Only the table column definitions need updating
- Follow the Service Installers page pattern for consistency

