# Service Installers - Simplified Table Plan

**Date:** 2026-01-05  
**Objective:** Simplify table to show only essential columns with no scrolling

---

## Current Columns (8 columns)

1. ✅ **Name** - KEEP
2. ❌ **Employee ID** - REMOVE
3. ❌ **Department** - REMOVE
4. ✅ **Phone** - KEEP (rename to "Mobile")
5. ❌ **Level** (SiLevel) - REMOVE
6. ❌ **Type** (InstallerType) - REMOVE
7. ✅ **Status** (IsActive) - KEEP
8. ✅ **Actions** - KEEP (simplify to Edit + Delete only)

---

## New Simplified Columns (5 columns)

1. **Name** - Full name of installer
2. **Mobile** - Phone number (renamed from "Phone")
3. **Emergency Contact** - Emergency contact info (from `emergencyContact` field)
4. **Status** - Active/Inactive badge
5. **Actions** - Edit icon + Delete icon only (remove Toggle Status)

---

## Column Width Strategy

To prevent scrolling, use flexible widths:
- **Name**: `flex-1` or `min-w-[200px]` (takes available space)
- **Mobile**: `w-[140px]` (fixed width for phone numbers)
- **Emergency Contact**: `w-[180px]` (fixed width)
- **Status**: `w-[100px]` (fixed width for badge)
- **Actions**: `w-[100px]` (fixed width for 2 icons)

Total approximate width: ~720px (fits most viewports)

---

## Actions Column Changes

**Current Actions:**
- Power icon (Toggle Status) - ❌ REMOVE
- Edit icon - ✅ KEEP
- Delete icon - ✅ KEEP

**New Actions:**
- Edit icon only
- Delete icon only

---

## Implementation Steps

### Step 1: Update Columns Array

Remove these columns:
- `employeeId` (Employee ID)
- `departmentName` (Department)
- `siLevel` (Level)
- `installerType` (Type)

Keep and modify:
- `name` (Name) - no changes
- `phone` (Mobile) - rename label
- `emergencyContact` (Emergency Contact) - ADD NEW
- `isActive` (Status) - no changes
- `actions` (Actions) - remove Power button

### Step 2: Add Emergency Contact Column

```typescript
{ 
  key: 'emergencyContact', 
  label: 'Emergency Contact',
  render: (value) => value ? (
    <span className="text-sm text-gray-700">{value as string}</span>
  ) : <span className="text-gray-400 text-xs">—</span>
}
```

### Step 3: Simplify Actions Column

Remove Power button, keep only Edit and Delete:
```typescript
{
  key: 'actions',
  label: 'Actions',
  render: (_value, row) => (
    <div className="flex items-center gap-2">
      <button
        onClick={(e) => {
          e.stopPropagation();
          openEditModal(row);
        }}
        title="Edit"
        className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
      >
        <Edit className="h-4 w-4" />
      </button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          handleDelete(row.id);
        }}
        title="Delete"
        className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
      >
        <Trash2 className="h-4 w-4" />
      </button>
    </div>
  )
}
```

### Step 4: Ensure No Scrolling

Add to DataTable wrapper:
```tsx
<div className="overflow-x-auto">
  <DataTable
    data={filteredServiceInstallers}
    columns={columns}
    sortable={true}
    className="min-w-full"
  />
</div>
```

Or use responsive table classes:
```tsx
<Card className="overflow-x-auto">
  <DataTable ... />
</Card>
```

---

## Files to Modify

1. **`frontend/src/pages/settings/ServiceInstallersPage.tsx`**
   - Update `columns` array (lines 335-439)
   - Remove unnecessary columns
   - Add Emergency Contact column
   - Simplify Actions column
   - Rename "Phone" to "Mobile"

---

## Verification Checklist

- [ ] Table displays only 5 columns
- [ ] No horizontal scrolling on desktop
- [ ] No vertical scrolling (unless pagination needed)
- [ ] Name column is flexible width
- [ ] Mobile, Emergency Contact, Status, Actions have fixed widths
- [ ] Edit icon works (opens edit modal)
- [ ] Delete icon works (shows confirmation)
- [ ] Emergency Contact displays correctly or shows "—" if empty
- [ ] Table is responsive on mobile devices

---

## Expected Final Column Structure

```typescript
const columns: TableColumn<ServiceInstaller>[] = [
  { key: 'name', label: 'Name' },
  { key: 'phone', label: 'Mobile' },
  { 
    key: 'emergencyContact', 
    label: 'Emergency Contact',
    render: (value) => value ? (
      <span className="text-sm text-gray-700">{value as string}</span>
    ) : <span className="text-gray-400 text-xs">—</span>
  },
  { 
    key: 'isActive', 
    label: 'Status', 
    render: (value) => (
      <span className={`px-2 py-1 rounded text-xs font-medium border ${getBooleanStatusColor(value as boolean)}`}>
        {value ? 'Active' : 'Inactive'}
      </span>
    )
  },
  {
    key: 'actions',
    label: 'Actions',
    render: (_value, row) => (
      <div className="flex items-center gap-2">
        <button onClick={(e) => { e.stopPropagation(); openEditModal(row); }} title="Edit" className="...">
          <Edit className="h-4 w-4" />
        </button>
        <button onClick={(e) => { e.stopPropagation(); handleDelete(row.id); }} title="Delete" className="...">
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    )
  }
];
```

