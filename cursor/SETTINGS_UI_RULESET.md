# SETTINGS UI RULESET – CephasOps Frontend (React + Tailwind + shadcn/ui)

All items created under **Settings pages** must follow this UI and UX standard.

## 1. CRUD Requirements
Every settings page MUST support the following:

- Create (using Modal + Form)
- Read (table listing with DataTable component)
- Update (Edit)
- Delete (soft delete or hard delete based on backend)
- Deactivate / Activate toggle

**Actions allowed:**  
- Toggle Status (Power icon)
- Edit  
- Delete  

No additional action types should be added without updating this ruleset.

## 2. Page Container
All settings pages must use:

```jsx
<div className="flex-1 p-2 max-w-7xl mx-auto">
```

- Use `flex-1` for proper flex layout
- Use `p-2` for padding
- Use `max-w-7xl mx-auto` for centered, max-width container

## 3. Page Header
```jsx
<div className="mb-2 flex items-center justify-between">
  <h1 className="text-sm font-bold text-foreground">Page Title</h1>
  <Button onClick={() => setShowCreateModal(true)} className="flex items-center gap-2">
    <Plus className="h-4 w-4" />
    Add Item
  </Button>
</div>
```

- Title: `text-sm font-bold text-foreground`
- Button icon: `h-4 w-4`
- Container: `flex items-center gap-2`

## 4. Table Layout Rules
All tables must follow these rules:

- Use **DataTable** component from `../../components/ui`
- Wrap in **Card** component
- Row height: `h-11` (configured in DataTable component)
- Text size: `text-sm`
- Padding: `px-3`

## 5. Action Icons (CRITICAL)
All action icons must use **lucide-react** and follow:

- Size: **`h-3 w-3`** (12px × 12px in Tailwind) - **MANDATORY**
- Container: `flex items-center gap-2`
- Colors:  
  - Toggle Status: `text-yellow-600` (active) / `text-green-600` (inactive)
  - Edit: `text-blue-600`  
  - Delete: `text-red-600`  
- Hover: `hover:opacity-75 cursor-pointer transition-colors`
- Always use `e.stopPropagation()` in handlers

Example:
```jsx
{
  key: 'actions',
  label: 'Actions',
  render: (value, row) => (
    <div className="flex items-center gap-2">
      <button
        onClick={(e) => {
          e.stopPropagation();
          handleToggleStatus(row);
        }}
        title={row.isActive ? 'Deactivate' : 'Activate'}
        className={`${row.isActive ? 'text-yellow-600' : 'text-green-600'} hover:opacity-75 cursor-pointer transition-colors`}
      >
        <Power className="h-3 w-3" />
      </button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          openEditModal(row);
        }}
        title="Edit"
        className="text-blue-600 hover:opacity-75 cursor-pointer transition-colors"
      >
        <Edit className="h-3 w-3" />
      </button>
      <button
        onClick={(e) => {
          e.stopPropagation();
          handleDelete(row.id);
        }}
        title="Delete"
        className="text-red-600 hover:opacity-75 cursor-pointer transition-colors"
      >
        <Trash2 className="h-3 w-3" />
      </button>
    </div>
  )
}
```

## 6. Status Badge
```jsx
<span className={`px-2 py-1 rounded-full text-xs font-medium ${
  value 
    ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
    : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
}`}>
  {value ? 'Active' : 'Inactive'}
</span>
```

## 7. Modal Structure
```jsx
<Modal
  isOpen={showCreateModal || editingItem !== null}
  onClose={() => {
    setShowCreateModal(false);
    setEditingItem(null);
    resetForm();
  }}
>
  <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
    <div className="flex items-center justify-between mb-2">
      <h2 className="text-xs font-bold">
        {editingItem ? 'Edit Item' : 'Create Item'}
      </h2>
      <button
        onClick={() => {
          setShowCreateModal(false);
          setEditingItem(null);
          resetForm();
        }}
        className="text-gray-400 hover:text-gray-600"
      >
        <X className="h-6 w-6" />
      </button>
    </div>

    <div className="space-y-2">
      {/* Form fields */}
    </div>

    <div className="flex justify-end gap-2 pt-2 border-t">
      <Button variant="outline" onClick={handleCancel}>
        Cancel
      </Button>
      <Button onClick={editingItem ? handleUpdate : handleCreate} className="flex items-center gap-2">
        <Save className="h-4 w-4" />
        {editingItem ? 'Update' : 'Create'}
      </Button>
    </div>
  </div>
</Modal>
```

## 8. Form Spacing
- Modal content: `space-y-2`
- Grid gaps: `gap-2`
- Labels: `text-xs font-medium`
- Textarea: `px-2 py-1 text-xs`

## 9. Data Handling
- Use React hooks (useState, useEffect) for state management
- Use async/await for API calls
- Always show toast notifications on success/error using `useToast()`
- Trim all string inputs
- Convert empty strings to null for nullable fields
- Handle loading state with `<LoadingSpinner message="Loading..." fullPage />`
- Handle empty state with `<EmptyState title="..." message="..." />`

## 10. Standard Imports
```jsx
import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power } from 'lucide-react';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Select } from '../../components/ui';
```

## 11. Allowed Settings Pages
All these pages MUST follow this ruleset:

- Partners
- Order Types
- Installation Types
- Building Types
- Buildings
- Splitter Types
- Splitters
- Departments
- Service Installers
- Cost Centres
- Asset Types
- Material Categories
- KPI Profiles
- Document Templates
- Order Statuses
- Partner Rates

## 12. Checklist
When creating/modifying a settings page, verify:

- [ ] Container uses `flex-1 p-2 max-w-7xl mx-auto`
- [ ] Header has `text-sm font-bold text-foreground` title
- [ ] DataTable is wrapped in Card component
- [ ] EmptyState is shown when no data
- [ ] Action icons are `h-3 w-3`
- [ ] Action buttons use correct colors
- [ ] All action buttons have `e.stopPropagation()`
- [ ] Modal follows standard structure
- [ ] Form spacing uses `space-y-2` and `gap-2`
- [ ] Loading state is handled
- [ ] Error handling with toast notifications
- [ ] Status badge uses standard styling
- [ ] CompanyId should not be user-selectable in UI. Tenant context is derived from authentication. The system remains multi-tenant but does not allow manual company switching in the UI.

## 13. Common Mistakes to Avoid

❌ Using `h-4 w-4` or `h-7 w-7` for action icons (must be `h-3 w-3`)
❌ Using `p-6` for container padding (must be `p-2`)
❌ Using `text-lg` or `text-2xl` for titles (must be `text-sm`)
❌ Using `space-y-4` or `gap-4` for spacing (must be `space-y-2` / `gap-2`)
❌ Forgetting `e.stopPropagation()` in action handlers
❌ Missing Power/toggle status button in actions
❌ Using custom status text instead of styled badge

---

**Last Updated**: 2024
**Maintained By**: CephasOps Development Team
