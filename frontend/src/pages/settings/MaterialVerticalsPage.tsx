import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Network } from 'lucide-react';
import { 
  getMaterialVerticals, 
  getMaterialVertical, 
  createMaterialVertical, 
  updateMaterialVertical, 
  deleteMaterialVertical 
} from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';
import type { MaterialVertical } from '../../types/inventory';

interface MaterialVerticalFormData {
  code: string;
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
}

const MaterialVerticalsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const [verticals, setVerticals] = useState<MaterialVertical[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingVertical, setEditingVertical] = useState<MaterialVertical | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [formData, setFormData] = useState<MaterialVerticalFormData>({
    code: '',
    name: '',
    description: '',
    displayOrder: 0,
    isActive: true
  });
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof MaterialVerticalFormData, string>>>({});

  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  useEffect(() => {
    loadVerticals();
  }, []);

  const loadVerticals = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getMaterialVerticals({ isActive: undefined }); // Get all verticals
      setVerticals(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading verticals:', err);
      showError(err.message || 'Failed to load material verticals');
      setVerticals([]);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      code: '',
      name: '',
      description: '',
      displayOrder: 0,
      isActive: true
    });
    setFormErrors({});
  };

  const validateForm = (): boolean => {
    const errors: Partial<Record<keyof MaterialVerticalFormData, string>> = {};

    if (!formData.code.trim()) {
      errors.code = 'Vertical code is required';
    }
    if (!formData.name.trim()) {
      errors.name = 'Vertical name is required';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async (): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    try {
      // Check for duplicate code
      const codeTrimmed = formData.code.trim();
      const exists = verticals.some(
        v => v.code.toLowerCase() === codeTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A vertical with code "${codeTrimmed}" already exists.`);
        return;
      }

      await createMaterialVertical({
        code: codeTrimmed,
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });
      
      showSuccess('Material vertical created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadVerticals();
    } catch (err: any) {
      showError(err.message || 'Failed to create material vertical');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingVertical) return;
    if (!validateForm()) {
      return;
    }

    try {
      // Check for duplicate code (exclude current record)
      const codeTrimmed = formData.code.trim();
      const exists = verticals.some(
        v => v.id !== editingVertical.id && v.code.toLowerCase() === codeTrimmed.toLowerCase()
      );

      if (exists) {
        showError(`A vertical with code "${codeTrimmed}" already exists.`);
        return;
      }

      await updateMaterialVertical(editingVertical.id, {
        code: codeTrimmed,
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        displayOrder: formData.displayOrder,
        isActive: formData.isActive
      });
      
      showSuccess('Material vertical updated successfully!');
      setShowCreateModal(false);
      setEditingVertical(null);
      resetForm();
      await loadVerticals();
    } catch (err: any) {
      showError(err.message || 'Failed to update material vertical');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this material vertical?')) return;
    
    try {
      await deleteMaterialVertical(id);
      showSuccess('Material vertical deleted successfully!');
      await loadVerticals();
    } catch (err: any) {
      showError(err.message || 'Failed to delete material vertical');
    }
  };

  const handleToggleStatus = async (vertical: MaterialVertical): Promise<void> => {
    try {
      await updateMaterialVertical(vertical.id, {
        isActive: !vertical.isActive
      });
      showSuccess(`Material vertical ${!vertical.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadVerticals();
    } catch (err: any) {
      showError(err.message || 'Failed to update material vertical status');
    }
  };

  const openEditModal = (vertical: MaterialVertical): void => {
    setEditingVertical(vertical);
    setFormData({
      code: vertical.code || '',
      name: vertical.name || '',
      description: vertical.description || '',
      displayOrder: vertical.displayOrder || 0,
      isActive: vertical.isActive ?? true
    });
    setShowCreateModal(true);
  };

  if (loading) {
    return <LoadingSpinner message="Loading material verticals..." fullPage />;
  }

  return (
    <PageShell title="Material Verticals" breadcrumbs={[{ label: 'Settings' }, { label: 'Material Verticals' }]}>
    <div className="flex-1 p-4 md:p-6 max-w-7xl mx-auto">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-lg font-bold text-foreground">Material Verticals</h1>
        {canManage && (
          <Button onClick={() => {
            resetForm();
            setEditingVertical(null);
            setShowCreateModal(true);
          }} className="flex items-center gap-2">
            <Plus className="h-4 w-4" />
            Add Vertical
          </Button>
        )}
      </div>

      {verticals.length === 0 ? (
        <EmptyState
          icon={Network}
          title="No Material Verticals"
          description="Add your first material vertical to get started."
          action={
            canManage ? {
              label: 'Add Vertical',
              onClick: () => {
                resetForm();
                setEditingVertical(null);
                setShowCreateModal(true);
              }
            } : undefined
          }
        />
      ) : (
        <Card>
          <StandardListTable
            data={verticals}
            selectedRows={selectedRows}
            onSelectionChange={setSelectedRows}
            onRowClick={(row: MaterialVertical) => canManage && openEditModal(row)}
            columns={[
              { key: 'code', label: 'Code' },
              { key: 'name', label: 'Name' },
              { key: 'description', label: 'Description' },
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
            emptyMessage="Add your first material vertical to get started."
          />
        </Card>
      )}

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => {
          setShowCreateModal(false);
          setEditingVertical(null);
          resetForm();
        }}
        title={editingVertical ? 'Edit Material Vertical' : 'Create Material Vertical'}
        size="md"
      >
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Code *"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value })}
              required
              placeholder="e.g., ISP"
              error={formErrors.code}
            />
            <TextInput
              label="Name *"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
              placeholder="e.g., Internet Service Provider"
              error={formErrors.name}
            />
          </div>

          <TextInput
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Optional description"
            multiline
            rows={3}
          />

          <div className="grid grid-cols-2 gap-2">
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
                setEditingVertical(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingVertical ? handleUpdate : handleCreate}
              className="flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              {editingVertical ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default MaterialVerticalsPage;

