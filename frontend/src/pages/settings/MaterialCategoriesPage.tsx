import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power } from 'lucide-react';
import { 
  getMaterialCategories, 
  getMaterialCategory, 
  createMaterialCategory, 
  updateMaterialCategory, 
  deleteMaterialCategory 
} from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';

interface MaterialCategory {
  id: string;
  name: string;
  description?: string;
  displayOrder: number;
  isActive: boolean;
  createdAt?: string;
}

interface MaterialCategoryFormData {
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
}

const MaterialCategoriesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const [categories, setCategories] = useState<MaterialCategory[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingCategory, setEditingCategory] = useState<MaterialCategory | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [formData, setFormData] = useState<MaterialCategoryFormData>({
    name: '',
    description: '',
    displayOrder: 0,
    isActive: true
  });
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof MaterialCategoryFormData, string>>>({});

  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getMaterialCategories({ isActive: undefined }); // Get all categories
      setCategories(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading categories:', err);
      showError(err.message || 'Failed to load material categories');
      setCategories([]);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      description: '',
      displayOrder: 0,
      isActive: true
    });
    setFormErrors({});
  };

  const validateForm = (): boolean => {
    const errors: Partial<Record<keyof MaterialCategoryFormData, string>> = {};

    if (!formData.name.trim()) {
      errors.name = 'Category name is required';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async (): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    try {
      // Check for duplicate name
      const nameTrimmed = formData.name.trim();
      const exists = categories.some(
        c => c.name.toLowerCase() === nameTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A category with name "${nameTrimmed}" already exists.`);
        return;
      }

      await createMaterialCategory({
        name: nameTrimmed,
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });

      showSuccess('Material category created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadCategories();
    } catch (err: any) {
      console.error('Error creating category:', err);
      showError(err.message || 'Failed to create material category');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingCategory || !validateForm()) {
      return;
    }

    try {
      // Check for duplicate name (exclude current record)
      const nameTrimmed = formData.name.trim();
      const exists = categories.some(
        c => c.id !== editingCategory.id && c.name.toLowerCase() === nameTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A category with name "${nameTrimmed}" already exists.`);
        return;
      }

      await updateMaterialCategory(editingCategory.id, {
        name: nameTrimmed,
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });

      showSuccess('Material category updated successfully!');
      setEditingCategory(null);
      resetForm();
      await loadCategories();
    } catch (err: any) {
      console.error('Error updating category:', err);
      showError(err.message || 'Failed to update material category');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this category? This action cannot be undone.')) {
      return;
    }

    try {
      await deleteMaterialCategory(id);
      showSuccess('Material category deleted successfully!');
      await loadCategories();
    } catch (err: any) {
      console.error('Error deleting category:', err);
      showError(err.message || 'Failed to delete material category');
    }
  };

  const handleToggleStatus = async (category: MaterialCategory): Promise<void> => {
    try {
      await updateMaterialCategory(category.id, {
        isActive: !category.isActive
      });
      showSuccess(`Category ${!category.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadCategories();
    } catch (err: any) {
      console.error('Error toggling category status:', err);
      showError(err.message || 'Failed to update category status');
    }
  };

  const openEditModal = async (category: MaterialCategory): Promise<void> => {
    setEditingCategory(category);
    setFormData({
      name: category.name || '',
      description: category.description || '',
      displayOrder: category.displayOrder || 0,
      isActive: category.isActive ?? true
    });
  };

  if (loading) {
    return (
      <PageShell title="Material Categories" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Categories' }]}>
        <LoadingSpinner message="Loading material categories..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Material Categories"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Categories' }]}
      actions={
        canManage ? (
          <Button size="sm" onClick={() => { resetForm(); setShowCreateModal(true); }} className="gap-1">
            <Plus className="h-4 w-4" />
            Add Category
          </Button>
        ) : undefined
      }
    >
      <div className="space-y-4">
      <Card>
        <div className="p-4 border-b">
          <p className="text-sm text-muted-foreground">
            Manage material classification categories (e.g., Fiber Cables, ONUs, Splitters, Tools)
          </p>
        </div>

        {/* Categories Table */}
        <StandardListTable
          data={categories}
          selectedRows={selectedRows}
          onSelectionChange={setSelectedRows}
          onRowClick={(row: MaterialCategory) => canManage && openEditModal(row)}
          columns={[
            { key: 'name', label: 'Name' },
            { key: 'description', label: 'Description' },
            { 
              key: 'displayOrder', 
              label: 'Order',
              render: (value: unknown) => value ?? 0
            },
            { 
              key: 'isActive', 
              label: 'Status',
              render: (value: unknown) => (
                <span className={`px-2 py-0.5 rounded text-xs ${
                  value ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                }`}>
                  {value ? 'Active' : 'Inactive'}
                </span>
              )
            }
          ]}
          actions={{
            onEdit: canManage ? openEditModal : undefined,
            ...(canManage && {
              onDeactivate: handleToggleStatus,
              onDelete: (row: MaterialCategory) => handleDelete(row.id)
            })
          }}
          pageSize={20}
          loading={loading}
          emptyMessage="No material categories found. Add your first category to get started."
        />
      </Card>

      {/* Create/Edit Category Modal */}
      <Modal
        isOpen={showCreateModal || editingCategory !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingCategory(null);
          resetForm();
        }}
        title={editingCategory ? 'Edit Material Category' : 'Create Material Category'}
        size="md"
      >
        <div className="space-y-4">
          <TextInput
            label="Category Name *"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
            placeholder="e.g., Fiber Cables, ONUs, Splitters"
            error={formErrors.name}
          />

          <TextInput
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Optional description for this category"
            multiline
            rows={3}
          />

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Display Order"
              type="number"
              value={formData.displayOrder.toString()}
              onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
              placeholder="0"
            />

            <div className="space-y-2">
              <label className="text-sm font-medium">Status</label>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-gray-300"
                />
                <label htmlFor="isActive" className="text-sm text-muted-foreground">
                  Active
                </label>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingCategory(null);
                resetForm();
              }}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button
              onClick={editingCategory ? handleUpdate : handleCreate}
              className="gap-2"
            >
              <Save className="h-4 w-4" />
              {editingCategory ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default MaterialCategoriesPage;
