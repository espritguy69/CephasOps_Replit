# Service Installers Page Analysis & Fix Plan

**Date:** 2026-01-05  
**File:** `frontend/src/pages/settings/ServiceInstallersPage.tsx`

---

## Current Status Analysis

### ✅ What's Already Working

1. **Page Component Exists**
   - Location: `frontend/src/pages/settings/ServiceInstallersPage.tsx`
   - Uses React Query hooks (`useServiceInstallers`, `useCreateServiceInstaller`, etc.)
   - Has DataTable component imported and used

2. **Table Columns Defined**
   - ✅ Name
   - ✅ Employee ID (not "Code" - entity uses `employeeId`)
   - ✅ Department
   - ✅ Phone
   - ✅ Level (SiLevel)
   - ✅ Type (InstallerType)
   - ✅ Status (IsActive)
   - ✅ Actions column with Edit/Delete buttons

3. **CRUD Actions Implemented**
   - ✅ Edit button with `openEditModal(row)` handler
   - ✅ Delete button with `handleDelete(row.id)` handler
   - ✅ Toggle Status button with `handleToggleStatus(row)` handler
   - ✅ Create button opens modal

4. **API Integration**
   - ✅ Uses `useServiceInstallers()` hook
   - ✅ Uses `useDeleteServiceInstaller()` mutation
   - ✅ Uses `useUpdateServiceInstaller()` mutation
   - ✅ Uses `useCreateServiceInstaller()` mutation

---

## Issues Identified

### 🔴 Issue 1: Missing Email Column
**Current:** Email is not displayed in the table  
**Expected:** Email column should be visible

### 🔴 Issue 2: "Code" Column Request
**Current:** Table shows "Employee ID" (which is correct per entity)  
**Note:** Entity doesn't have a "Code" field - it uses `employeeId` instead  
**Decision Needed:** Should we add a "Code" field or keep "Employee ID"?

### 🟡 Issue 3: Delete Confirmation
**Current:** Uses `window.confirm()` (basic browser dialog)  
**Expected:** Should use a proper confirmation modal/dialog component

### 🟡 Issue 4: Column Order
**Current Order:**
1. Name
2. Employee ID
3. Department
4. Phone
5. Level
6. Type
7. Status
8. Actions

**Suggested Order:**
1. Name
2. Employee ID (or Code if added)
3. Department
4. Phone
5. Email (MISSING)
6. Level
7. Type
8. Status
9. Actions

---

## Comparison with Reference Pages

### Reference: `SiRatePlansPage.tsx`

**Similarities:**
- ✅ Uses DataTable component
- ✅ Has Actions column with Edit/Delete
- ✅ Uses React Query hooks
- ✅ Has proper error handling

**Differences:**
- ❌ SiRatePlansPage uses `setDeletingPlan(row)` for delete confirmation
- ❌ ServiceInstallersPage uses `window.confirm()` (less user-friendly)

### Reference: `BuildingsPage.tsx`

**Similarities:**
- ✅ Uses DataTable component
- ✅ Has Actions column
- ✅ Column rendering with badges

**Differences:**
- ✅ BuildingsPage has more columns displayed
- ✅ Better column organization

---

## Fix Plan

### Fix 1: Add Email Column

**Location:** `frontend/src/pages/settings/ServiceInstallersPage.tsx` (around line 349)

**Add after Phone column:**
```typescript
{ key: 'phone', label: 'Phone' },
{ 
  key: 'email', 
  label: 'Email',
  render: (value) => value ? (
    <span className="text-sm text-gray-700">{value as string}</span>
  ) : <span className="text-gray-400 text-xs">—</span>
},
```

### Fix 2: Improve Delete Confirmation

**Current Code (line 267-275):**
```typescript
const handleDelete = async (id: string): Promise<void> => {
  if (!window.confirm('Are you sure you want to delete this service installer?')) return;
  // ...
};
```

**Recommended:** Add a confirmation state and modal (similar to SiRatePlansPage)

### Fix 3: Verify Table Rendering

**Check:**
- Is `filteredServiceInstallers` populated correctly?
- Is DataTable receiving the data?
- Are there any console errors?

### Fix 4: Add "Code" Field (Optional)

**If "Code" is required:**
- Backend: Add `Code` property to `ServiceInstaller` entity
- Migration: Add `Code` column to database
- Frontend: Add `Code` column to table

**OR**

**If "Employee ID" is sufficient:**
- Keep current "Employee ID" column
- Update documentation to clarify

---

## Step-by-Step Fix Checklist

### Phase 1: Immediate Fixes

- [ ] **Add Email Column**
  - Add email column definition after Phone
  - Add render function to display email or "—" if empty

- [ ] **Improve Delete Confirmation**
  - Add `deletingSI` state
  - Create confirmation modal component
  - Update `handleDelete` to use modal instead of `window.confirm`

- [ ] **Verify Data Loading**
  - Check if `serviceInstallers` data is loading correctly
  - Verify `filteredServiceInstallers` is populated
  - Check for any API errors

### Phase 2: Enhancements

- [ ] **Column Order Optimization**
  - Reorder columns: Name, Employee ID, Department, Phone, Email, Level, Type, Status, Actions

- [ ] **Add Code Field (if needed)**
  - Backend: Add Code property
  - Migration: Add Code column
  - Frontend: Add Code column to table

- [ ] **Improve Empty State**
  - Verify EmptyState displays correctly when no data

### Phase 3: Testing

- [ ] **Test Table Display**
  - Verify all columns render correctly
  - Check one row per installer
  - Verify data is correct

- [ ] **Test Edit Action**
  - Click Edit icon
  - Verify modal opens with correct data
  - Test form submission

- [ ] **Test Delete Action**
  - Click Delete icon
  - Verify confirmation dialog
  - Test deletion

- [ ] **Test Create Action**
  - Click Add button
  - Verify modal opens
  - Test form submission

---

## Files to Modify

1. **`frontend/src/pages/settings/ServiceInstallersPage.tsx`**
   - Add Email column
   - Improve delete confirmation
   - Reorder columns (optional)

2. **Backend (if Code field needed):**
   - `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs`
   - `backend/src/CephasOps.Application/ServiceInstallers/DTOs/ServiceInstallerDto.cs`
   - Migration file

---

## Current Column Structure

```typescript
const columns: TableColumn<ServiceInstaller>[] = [
  { key: 'name', label: 'Name' },
  { key: 'employeeId', label: 'Employee ID' },
  { key: 'departmentName', label: 'Department', render: ... },
  { key: 'phone', label: 'Phone' },
  // ❌ MISSING: Email column
  { key: 'siLevel', label: 'Level', render: ... },
  { key: 'installerType', label: 'Type', render: ... },
  { key: 'isActive', label: 'Status', render: ... },
  { key: 'actions', label: 'Actions', render: ... } // ✅ Has Edit/Delete
];
```

---

## Expected Final Column Structure

```typescript
const columns: TableColumn<ServiceInstaller>[] = [
  { key: 'name', label: 'Name' },
  { key: 'employeeId', label: 'Employee ID' }, // or 'Code' if added
  { key: 'departmentName', label: 'Department', render: ... },
  { key: 'phone', label: 'Phone' },
  { key: 'email', label: 'Email', render: ... }, // ✅ TO BE ADDED
  { key: 'siLevel', label: 'Level', render: ... },
  { key: 'installerType', label: 'Type', render: ... },
  { key: 'isActive', label: 'Status', render: ... },
  { key: 'actions', label: 'Actions', render: ... } // ✅ Already has Edit/Delete
];
```

---

## Next Steps

1. **Immediate:** Add Email column and improve delete confirmation
2. **Verify:** Test that table displays correctly with one row per installer
3. **Enhance:** Add Code field if required (backend + frontend changes)

