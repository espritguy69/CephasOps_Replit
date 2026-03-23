import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Tag } from 'lucide-react';
import { 
  getMaterialTags, 
  getMaterialTag, 
  createMaterialTag, 
  updateMaterialTag, 
  deleteMaterialTag 
} from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';
import type { MaterialTag } from '../../types/inventory';

interface MaterialTagFormData {
  name: string;
  description: string;
  color: string;
  displayOrder: number;
  isActive: boolean;
}

const MaterialTagsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const [tags, setTags] = useState<MaterialTag[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingTag, setEditingTag] = useState<MaterialTag | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [formData, setFormData] = useState<MaterialTagFormData>({
    name: '',
    description: '',
    color: '#3B82F6', // Default blue color
    displayOrder: 0,
    isActive: true
  });
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof MaterialTagFormData, string>>>({});

  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  useEffect(() => {
    loadTags();
  }, []);

  const loadTags = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getMaterialTags({ isActive: undefined }); // Get all tags
      setTags(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading tags:', err);
      showError(err.message || 'Failed to load material tags');
      setTags([]);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      description: '',
      color: '#3B82F6',
      displayOrder: 0,
      isActive: true
    });
    setFormErrors({});
  };

  const validateForm = (): boolean => {
    const errors: Partial<Record<keyof MaterialTagFormData, string>> = {};

    if (!formData.name.trim()) {
      errors.name = 'Tag name is required';
    }
    if (formData.color && !/^#[0-9A-F]{6}$/i.test(formData.color)) {
      errors.color = 'Color must be a valid hex code (e.g., #3B82F6)';
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
      const exists = tags.some(
        t => t.name.toLowerCase() === nameTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A tag with name "${nameTrimmed}" already exists.`);
        return;
      }

      await createMaterialTag({
        name: nameTrimmed,
        description: formData.description.trim() || undefined,
        color: formData.color || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });
      
      showSuccess('Material tag created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadTags();
    } catch (err: any) {
      showError(err.message || 'Failed to create material tag');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingTag) return;
    if (!validateForm()) {
      return;
    }

    try {
      // Check for duplicate name (exclude current record)
      const nameTrimmed = formData.name.trim();
      const exists = tags.some(
        t => t.id !== editingTag.id && t.name.toLowerCase() === nameTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A tag with name "${nameTrimmed}" already exists.`);
        return;
      }

      await updateMaterialTag(editingTag.id, {
        name: nameTrimmed,
        description: formData.description.trim() || undefined,
        color: formData.color || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });
      
      showSuccess('Material tag updated successfully!');
      setShowCreateModal(false);
      setEditingTag(null);
      resetForm();
      await loadTags();
    } catch (err: any) {
      showError(err.message || 'Failed to update material tag');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this material tag?')) return;
    
    try {
      await deleteMaterialTag(id);
      showSuccess('Material tag deleted successfully!');
      await loadTags();
    } catch (err: any) {
      showError(err.message || 'Failed to delete material tag');
    }
  };

  const handleToggleStatus = async (tag: MaterialTag): Promise<void> => {
    try {
      await updateMaterialTag(tag.id, {
        isActive: !tag.isActive
      });
      showSuccess(`Material tag ${!tag.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadTags();
    } catch (err: any) {
      showError(err.message || 'Failed to update material tag status');
    }
  };

  const openEditModal = (tag: MaterialTag): void => {
    setEditingTag(tag);
    setFormData({
      name: tag.name || '',
      description: tag.description || '',
      color: tag.color || '#3B82F6',
      displayOrder: tag.displayOrder || 0,
      isActive: tag.isActive ?? true
    });
    setShowCreateModal(true);
  };

  if (loading) {
    return (
      <PageShell title="Material Tags" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Tags' }]}>
        <LoadingSpinner message="Loading material tags..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Material Tags"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Material Tags' }]}
      actions={
        canManage ? (
          <Button size="sm" onClick={() => { resetForm(); setEditingTag(null); setShowCreateModal(true); }} className="gap-1">
            <Plus className="h-4 w-4" />
            Add Tag
          </Button>
        ) : undefined
      }
    >
      <div className="max-w-7xl mx-auto">
        <>
      {tags.length === 0 ? (
        <EmptyState
          icon={Tag}
          title="No Material Tags"
          description="Add your first material tag to get started."
          action={
            canManage ? {
              label: 'Add Tag',
              onClick: () => {
                resetForm();
                setEditingTag(null);
                setShowCreateModal(true);
              }
            } : undefined
          }
        />
      ) : (
        <Card>
          <StandardListTable
            data={tags}
            selectedRows={selectedRows}
            onSelectionChange={setSelectedRows}
            onRowClick={(row: MaterialTag) => canManage && openEditModal(row)}
            columns={[
              { 
                key: 'name', 
                label: 'Name',
                render: (value: unknown, row: MaterialTag) => (
                  <div className="flex items-center gap-2">
                    <span
                      className="w-3 h-3 rounded-full"
                      style={{ backgroundColor: row.color || '#3B82F6' }}
                    />
                    <span>{value as string}</span>
                  </div>
                )
              },
              { key: 'description', label: 'Description' },
              { 
                key: 'color', 
                label: 'Color',
                render: (value: unknown) => (
                  <div className="flex items-center gap-2">
                    <span
                      className="w-4 h-4 rounded border border-gray-300"
                      style={{ backgroundColor: value as string || '#3B82F6' }}
                    />
                    <span className="text-xs font-mono">{value as string || '#3B82F6'}</span>
                  </div>
                )
              },
              { 
                key: 'displayOrder', 
                label: 'Order',
                render: (value: unknown) => value || 0
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
              onDeactivate: canManage ? handleToggleStatus : undefined,
              onDelete: canManage ? handleDelete : undefined
            }}
            pageSize={20}
            loading={loading}
            emptyMessage="Add your first material tag to get started."
          />
        </Card>
      )}

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => {
          setShowCreateModal(false);
          setEditingTag(null);
          resetForm();
        }}
        title={editingTag ? 'Edit Material Tag' : 'Create Material Tag'}
        size="md"
      >
        <div className="space-y-2">
          <TextInput
            label="Name *"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
            placeholder="e.g., High Priority"
            error={formErrors.name}
          />

          <TextInput
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Optional description"
            multiline
            rows={3}
          />

          <div className="grid grid-cols-2 gap-2">
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Color *</label>
              <div className="flex gap-2 items-center">
                <input
                  type="color"
                  value={formData.color}
                  onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                  className="h-8 w-16 rounded border border-input cursor-pointer"
                />
                <TextInput
                  value={formData.color}
                  onChange={(e) => setFormData({ ...formData, color: e.target.value })}
                  placeholder="#3B82F6"
                  error={formErrors.color}
                  className="flex-1"
                />
              </div>
            </div>
            <TextInput
              label="Display Order"
              type="number"
              value={formData.displayOrder.toString()}
              onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
              placeholder="0"
            />
          </div>

          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="h-3 w-3 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <label htmlFor="isActive" className="text-xs font-medium cursor-pointer">
              Active Status
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button
              variant="outline"
              onClick={() => {
                setShowCreateModal(false);
                setEditingTag(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingTag ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingTag ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
        </>
      </div>
    </PageShell>
  );
};

export default MaterialTagsPage;

