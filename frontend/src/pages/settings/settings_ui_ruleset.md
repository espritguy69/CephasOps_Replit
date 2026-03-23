# Settings Page UI Ruleset

This document outlines the standard rules and patterns that **MUST** be followed when creating or updating any page under `/settings`.

## 📋 Layout Structure

### Container
```jsx
<div className="flex-1 p-2 max-w-7xl mx-auto">
  {/* or p-3 for pages with more content */}
```
- Use `flex-1` for proper flex layout
- Use `p-2` or `p-3` for padding
- Use `max-w-7xl mx-auto` for centered, max-width container

### Header Section
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
- Button: `flex items-center gap-2` with Plus icon (`h-4 w-4`)

## 📊 DataTable

### Wrapper
```jsx
<Card>
  {items.length > 0 ? (
    <DataTable data={items} columns={columns} />
  ) : (
    <EmptyState title="No items found" message="Add your first item to get started." />
  )}
</Card>
```

### Row Height
- **MUST**: Row height is set to `h-11` (44px in Tailwind) in the DataTable component
- Each table row must use: `h-11 items-center border-b`
- This is already configured in `DataTable.jsx` - do not override

### Columns Structure
```jsx
const columns = [
  { key: 'displayOrder', label: 'Order' }, // If applicable
  { key: 'name', label: 'Name' },
  { key: 'code', label: 'Code' }, // If applicable
  { 
    key: 'isActive', 
    label: 'Status', 
    render: (value) => (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
        value 
          ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
          : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
      }`}>
        {value ? 'Active' : 'Inactive'}
      </span>
    )
  },
  {
    key: 'actions',
    label: 'Actions',
    render: (value, row) => (
      // See Action Buttons section below
    )
  }
];
```

## 🎯 Action Buttons

### **CRITICAL**: Icon-Only Buttons
All action buttons **MUST** be icon-only with the following specifications:

```jsx
{
  key: 'actions',
  label: 'Actions',
  render: (value, row) => (
    <div className="flex items-center gap-2">
      {/* Deactivate/Activate Button */}
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
      
      {/* Edit Button */}
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
      
      {/* Delete Button */}
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

### Rules:
- ✅ **Icon size**: `h-3 w-3` (12px × 12px in Tailwind) - **MANDATORY**
- ✅ **Container**: `flex gap-3`
- ✅ **Always use**: `e.stopPropagation()` to prevent row click events
- ✅ **Icons**: Power (deactivate), Edit (edit), Trash2 (delete)
- ✅ **Colors**: 
  - Deactivate: Yellow (`text-yellow-600`)
  - Activate: Green (`text-green-600`)
  - Edit: Blue (`text-blue-600`)
  - Delete: Red (`text-red-600`)
- ✅ **Hover**: `hover:opacity-75 cursor-pointer`
- ❌ **DO NOT**: Use Button component for actions
- ❌ **DO NOT**: Add text labels to action buttons
- ❌ **DO NOT**: Use different icon sizes

## 🔔 Modal Structure

### Standard Modal
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
      <h2 className="text-sm font-bold">
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

    <div className="flex justify-end gap-3 pt-4 border-t">
      <Button variant="outline" onClick={handleCancel}>
        Cancel
      </Button>
      <Button onClick={editingItem ? handleUpdate : handleCreate}>
        <Save className="h-4 w-4 mr-2" />
        {editingItem ? 'Update' : 'Create'}
      </Button>
    </div>
  </div>
</Modal>
```

## 📝 Form Handling

### State Management
```jsx
const [items, setItems] = useState([]);
const [loading, setLoading] = useState(true);
const [showCreateModal, setShowCreateModal] = useState(false);
const [editingItem, setEditingItem] = useState(null);
const [formData, setFormData] = useState({
  name: '',
  code: '',
  description: '',
  displayOrder: 0,
  isActive: true
});
```

### Loading State
```jsx
if (loading) {
  return <LoadingSpinner message="Loading items..." fullPage />;
}
```

### Error Handling
- Always use `useToast()` for success/error messages
- Wrap API calls in try-catch blocks
- Show user-friendly error messages

### Data Validation
- Trim all string inputs: `formData.name.trim()`
- Convert empty strings to `null` for optional fields
- Validate GUIDs before sending (if applicable)
- Check for duplicates (if applicable)

## 🎨 Status Badge

### Standard Status Badge
```jsx
<span className={`px-2 py-1 rounded-full text-xs font-medium ${
  value 
    ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
    : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
}`}>
  {value ? 'Active' : 'Inactive'}
</span>
```

## 📦 Imports

### Standard Imports
```jsx
import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power } from 'lucide-react';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
```

## ✅ Checklist

When creating a new settings page, verify:

- [ ] Container uses `flex-1 p-2` (or `p-3`) and `max-w-7xl mx-auto`
- [ ] Header has `text-sm font-bold` title
- [ ] DataTable is wrapped in Card component
- [ ] EmptyState is shown when no data
- [ ] Action buttons are icon-only (`h-3 w-3`)
- [ ] Action buttons use correct colors (yellow/green/blue/red)
- [ ] All action buttons have `e.stopPropagation()`
- [ ] Modal follows standard structure
- [ ] Form fields are properly labeled
- [ ] Loading state is handled
- [ ] Error handling with toast notifications
- [ ] Status badge uses standard styling
- [ ] No `companyId` references (multi-company feature removed)
- [ ] Nullable GUID fields handle empty strings correctly

## 🚫 Common Mistakes to Avoid

1. ❌ Using Button component for action buttons (use `<button>` instead)
2. ❌ Adding text labels to action buttons
3. ❌ Using wrong icon sizes (must be h-3 w-3, which is 12px × 12px)
4. ❌ Forgetting `e.stopPropagation()` in action handlers
5. ❌ Not handling nullable GUID fields properly
6. ❌ Using different row heights or overriding DataTable styles
7. ❌ Missing loading states
8. ❌ Not trimming form inputs
9. ❌ Using `companyId` in any form (multi-company removed)

## 📚 Reference Examples

- `BuildingTypesPage.jsx` - Complete example with duplicate validation
- `DepartmentsPage.jsx` - Example with nullable GUID handling
- `OrderTypesPage.jsx` - Standard CRUD pattern
- `SplitterTypesPage.jsx` - Example with display order

---

**Last Updated**: 2024
**Maintained By**: Development Team

