import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power } from 'lucide-react';
import { getVerticals, createVertical, updateVertical, deleteVertical } from '../../api/verticals';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, StatusBadge } from '../../components/ui';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest } from '../../types/referenceData';

interface ExtendedReferenceDataItem extends ReferenceDataItem {
  displayOrder?: number;
}

interface VerticalFormData {
  name: string;
  code: string;
  description: string;
  displayOrder: number | string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const VerticalsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [verticals, setVerticals] = useState<ExtendedReferenceDataItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingVertical, setEditingVertical] = useState<ExtendedReferenceDataItem | null>(null);
  const [submitting, setSubmitting] = useState<boolean>(false);
  const [formData, setFormData] = useState<VerticalFormData>({
    name: '',
    code: '',
    description: '',
    displayOrder: 0,
    isActive: true
  });

  useEffect(() => {
    loadVerticals();
  }, []);

  const loadVerticals = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getVerticals();
      setVerticals(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load verticals');
      console.error('Error loading verticals:', err);
    } finally {
      setLoading(false);
    }
  };

  const closeModal = (): void => {
    setShowCreateModal(false);
    setEditingVertical(null);
    resetForm();
  };

  const handleCreate = async (): Promise<void> => {
    if (submitting) return;
    setSubmitting(true);

    try {
      const verticalData: CreateReferenceDataRequest & { displayOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim().toUpperCase(),
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      await createVertical(verticalData as any);
      showSuccess('Vertical created successfully!');
      closeModal();
      await loadVerticals();
    } catch (err) {
      showError((err as Error).message || 'Failed to create vertical');
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (submitting || !editingVertical) return;
    setSubmitting(true);

    try {
      const verticalData: UpdateReferenceDataRequest & { displayOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim().toUpperCase(),
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      await updateVertical(editingVertical.id, verticalData as any);
      showSuccess('Vertical updated successfully!');
      closeModal();
      await loadVerticals();
    } catch (err) {
      showError((err as Error).message || 'Failed to update vertical');
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleStatus = async (vertical: ExtendedReferenceDataItem): Promise<void> => {
    try {
      const verticalData: UpdateReferenceDataRequest & { displayOrder?: number } = {
        name: vertical.name,
        code: vertical.code,
        description: vertical.description || undefined,
        isActive: !vertical.isActive,
        displayOrder: vertical.displayOrder || 0
      };
      
      await updateVertical(vertical.id, verticalData as any);
      showSuccess(`Vertical ${!vertical.isActive ? 'activated' : 'deactivated'} successfully!`);
      loadVerticals();
    } catch (err) {
      showError((err as Error).message || 'Failed to update vertical status');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this vertical? This action cannot be undone if it is being used by companies.')) return;
    
    try {
      await deleteVertical(id);
      showSuccess('Vertical deleted successfully!');
      loadVerticals();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete vertical');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      code: '',
      description: '',
      displayOrder: 0,
      isActive: true
    });
  };

  const openEditModal = (vertical: ExtendedReferenceDataItem): void => {
    setEditingVertical(vertical);
    setFormData({
      name: vertical.name,
      code: vertical.code || '',
      description: vertical.description || '',
      displayOrder: vertical.displayOrder || 0,
      isActive: vertical.isActive ?? true
    });
    setShowCreateModal(true);
  };

  const columns: TableColumn<ExtendedReferenceDataItem>[] = [
    { 
      key: 'displayOrder', 
      label: 'Order',
      render: (value) => <span className="text-xs font-mono">{value}</span>
    },
    { 
      key: 'name', 
      label: 'Name',
      render: (value) => <span className="font-medium">{value}</span>
    },
    { 
      key: 'code', 
      label: 'Code',
      render: (value) => <span className="text-xs font-mono bg-gray-100 dark:bg-gray-700 px-2 py-0.5 rounded">{value}</span>
    },
    { 
      key: 'description', 
      label: 'Description',
      render: (value) => <span className="text-xs text-gray-600 dark:text-gray-400">{value || '-'}</span>
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value) => (
        <StatusBadge status={value ? 'success' : 'error'}>
          {value ? 'Active' : 'Inactive'}
        </StatusBadge>
      )
    },
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
  ];

  if (loading) {
    return (
      <PageShell title="Verticals" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Verticals' }]}>
        <LoadingSpinner message="Loading verticals..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Verticals"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Verticals' }]}
      actions={
        <Button
          onClick={() => {
            setEditingVertical(null);
            resetForm();
            setShowCreateModal(true);
          }}
          className="flex items-center gap-2"
        >
          <Plus className="h-4 w-4" />
          Add Vertical
        </Button>
      }
    >
      <div className="space-y-2 p-3">
      <Card>
        {verticals.length > 0 ? (
          <div className="[&_table_tbody_td]:h-8 [&_table_tbody_td]:py-1 [&_table_thead_th]:h-8 [&_table_thead_th]:py-1">
            <DataTable
              data={verticals}
              columns={columns}
            />
          </div>
        ) : (
          <EmptyState
            title="No verticals found"
            message="Add your first vertical to get started."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingVertical !== null}
        onClose={closeModal}
        title={editingVertical ? 'Edit Vertical' : 'Create New Vertical'}
        size="lg"
      >
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-2">
            <TextInput
              label="Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g., ISP, Retail, Travel"
              required
            />
            <TextInput
              label="Code *"
              name="code"
              value={formData.code}
              onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
              placeholder="e.g., ISP, RETAIL, TRAVEL"
              required
            />
          </div>

          <TextInput
            label="Display Order"
            name="displayOrder"
            type="number"
            value={formData.displayOrder}
            onChange={(e) => setFormData({ ...formData, displayOrder: e.target.value })}
          />

          <div className="space-y-0.5">
            <label className="text-xs font-medium">Description</label>
            <textarea
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-2 py-1 text-xs"
              placeholder="Brief description of this vertical"
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="rounded border-gray-300"
            />
            <label htmlFor="isActive" className="text-xs font-medium text-gray-700">
              Active
            </label>
          </div>
        </div>

        <div className="flex items-center justify-end gap-2 mt-2 pt-2 border-t">
          <Button
            variant="ghost"
            onClick={closeModal}
          >
            <X className="h-4 w-4 mr-2" />
            Cancel
          </Button>
          <Button
            onClick={editingVertical ? handleUpdate : handleCreate}
            className="flex items-center gap-2"
            disabled={submitting}
          >
            <Save className="h-4 w-4" />
            {submitting ? 'Saving...' : (editingVertical ? 'Update' : 'Create')}
          </Button>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default VerticalsPage;

