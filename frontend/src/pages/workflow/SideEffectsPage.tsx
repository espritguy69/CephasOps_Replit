import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Info } from 'lucide-react';
import { 
  getSideEffectDefinitions, 
  getSideEffectDefinition, 
  createSideEffectDefinition, 
  updateSideEffectDefinition, 
  deleteSideEffectDefinition,
  type SideEffectDefinition,
  type CreateSideEffectDefinition,
  type UpdateSideEffectDefinition
} from '../../api/sideEffects';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, StandardListTable, Select, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';

interface SideEffectFormData {
  key: string;
  name: string;
  description: string;
  entityType: string;
  executorType: string;
  executorConfigJson: string;
  isActive: boolean;
  displayOrder: number;
}

const EXECUTOR_TYPES = [
  'Notify',
  'CreateStockMovement',
  'CreateOrderStatusLog',
  'UpdateOrderFlags',
  'TriggerInvoiceEligibility'
];

const ENTITY_TYPES = ['Order', 'WorkOrder', 'Project'];

const SideEffectsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { user } = useAuth();
  const [definitions, setDefinitions] = useState<SideEffectDefinition[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingDefinition, setEditingDefinition] = useState<SideEffectDefinition | null>(null);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [filters, setFilters] = useState<{ entityType?: string; isActive?: boolean }>({});
  const [formData, setFormData] = useState<SideEffectFormData>({
    key: '',
    name: '',
    description: '',
    entityType: 'Order',
    executorType: '',
    executorConfigJson: '',
    isActive: true,
    displayOrder: 0
  });
  const [formErrors, setFormErrors] = useState<Partial<Record<keyof SideEffectFormData, string>>>({});

  const canManage = user?.roles?.some(r => r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor') ?? false;

  useEffect(() => {
    loadDefinitions();
  }, [filters]);

  const loadDefinitions = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getSideEffectDefinitions(filters);
      setDefinitions(Array.isArray(data) ? data : []);
    } catch (err: any) {
      console.error('Error loading side effects:', err);
      showError(err.message || 'Failed to load side effect definitions');
      setDefinitions([]);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      key: '',
      name: '',
      description: '',
      entityType: 'Order',
      executorType: '',
      executorConfigJson: '',
      isActive: true,
      displayOrder: 0
    });
    setFormErrors({});
  };

  const validateForm = (): boolean => {
    const errors: Partial<Record<keyof SideEffectFormData, string>> = {};

    if (!formData.key.trim()) {
      errors.key = 'Key is required';
    } else if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(formData.key)) {
      errors.key = 'Key must start with a letter and contain only letters, numbers, and underscores';
    }

    if (!formData.name.trim()) {
      errors.name = 'Name is required';
    }

    if (!formData.entityType) {
      errors.entityType = 'Entity type is required';
    }

    if (!formData.executorType) {
      errors.executorType = 'Executor type is required';
    }

    if (formData.executorConfigJson && formData.executorConfigJson.trim()) {
      try {
        JSON.parse(formData.executorConfigJson);
      } catch {
        errors.executorConfigJson = 'Invalid JSON format';
      }
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleCreate = async (): Promise<void> => {
    if (!validateForm()) {
      return;
    }

    try {
      const keyTrimmed = formData.key.trim();
      const exists = definitions.some(
        d => d.key.toLowerCase() === keyTrimmed.toLowerCase() && d.entityType === formData.entityType
      );

      if (exists) {
        showError(`A side effect with key "${keyTrimmed}" already exists for entity type "${formData.entityType}".`);
        return;
      }

      await createSideEffectDefinition({
        key: keyTrimmed,
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        entityType: formData.entityType,
        executorType: formData.executorType,
        executorConfigJson: formData.executorConfigJson.trim() || undefined,
        isActive: formData.isActive,
        displayOrder: formData.displayOrder
      });

      showSuccess('Side effect definition created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadDefinitions();
    } catch (err: any) {
      console.error('Error creating side effect:', err);
      showError(err.message || 'Failed to create side effect definition');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingDefinition || !validateForm()) {
      return;
    }

    try {
      await updateSideEffectDefinition(editingDefinition.id, {
        name: formData.name.trim(),
        description: formData.description.trim() || undefined,
        executorType: formData.executorType,
        executorConfigJson: formData.executorConfigJson.trim() || undefined,
        isActive: formData.isActive,
        displayOrder: formData.displayOrder
      });

      showSuccess('Side effect definition updated successfully!');
      setEditingDefinition(null);
      resetForm();
      await loadDefinitions();
    } catch (err: any) {
      console.error('Error updating side effect:', err);
      showError(err.message || 'Failed to update side effect definition');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this side effect definition? This action cannot be undone.')) {
      return;
    }

    try {
      await deleteSideEffectDefinition(id);
      showSuccess('Side effect definition deleted successfully!');
      await loadDefinitions();
    } catch (err: any) {
      console.error('Error deleting side effect:', err);
      showError(err.message || 'Failed to delete side effect definition');
    }
  };

  const handleToggleStatus = async (definition: SideEffectDefinition): Promise<void> => {
    try {
      await updateSideEffectDefinition(definition.id, {
        isActive: !definition.isActive
      });
      showSuccess(`Side effect ${!definition.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadDefinitions();
    } catch (err: any) {
      console.error('Error toggling side effect status:', err);
      showError(err.message || 'Failed to update side effect status');
    }
  };

  const openEditModal = async (definition: SideEffectDefinition): Promise<void> => {
    setEditingDefinition(definition);
    setFormData({
      key: definition.key,
      name: definition.name || '',
      description: definition.description || '',
      entityType: definition.entityType,
      executorType: definition.executorType,
      executorConfigJson: definition.executorConfigJson || '',
      isActive: definition.isActive ?? true,
      displayOrder: definition.displayOrder || 0
    });
  };

  if (loading) {
    return (
      <PageShell title="Side Effect Definitions" breadcrumbs={[{ label: 'Workflow', path: '/settings/workflow' }, { label: 'Side Effects' }]}>
        <LoadingSpinner message="Loading side effect definitions..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Side Effect Definitions"
      breadcrumbs={[{ label: 'Workflow', path: '/settings/workflow' }, { label: 'Side Effects' }]}
      actions={
        canManage ? (
          <Button size="sm" onClick={() => { resetForm(); setShowCreateModal(true); }} className="gap-1">
            <Plus className="h-4 w-4" />
            Create Side Effect
          </Button>
        ) : undefined
      }
    >
      <div className="space-y-4">
      <Card>
        <div className="p-4 border-b">
          <p className="text-sm text-muted-foreground">
            Manage side effects that execute after successful workflow transitions
          </p>
        </div>

        {/* Filters */}
        <div className="p-4 border-b bg-muted/30">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="text-xs font-medium mb-1 block">Entity Type</label>
              <Select
                value={filters.entityType || ''}
                onChange={(e) => setFilters({ ...filters, entityType: e.target.value || undefined })}
                className="w-full"
              >
                <option value="">All Types</option>
                {ENTITY_TYPES.map(type => (
                  <option key={type} value={type}>{type}</option>
                ))}
              </Select>
            </div>
            <div>
              <label className="text-xs font-medium mb-1 block">Status</label>
              <Select
                value={filters.isActive === undefined ? '' : filters.isActive ? 'true' : 'false'}
                onChange={(e) => setFilters({ ...filters, isActive: e.target.value === '' ? undefined : e.target.value === 'true' })}
                className="w-full"
              >
                <option value="">All</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </Select>
            </div>
          </div>
        </div>

        {/* Table */}
        {definitions.length === 0 ? (
          <EmptyState
            title="No side effect definitions found"
            description="Create your first side effect definition to get started."
            action={canManage ? {
              label: 'Create Side Effect',
              onClick: () => {
                resetForm();
                setShowCreateModal(true);
              }
            } : undefined}
          />
        ) : (
          <StandardListTable
            data={definitions}
            selectedRows={selectedRows}
            onSelectionChange={setSelectedRows}
            onRowClick={(row: SideEffectDefinition) => canManage && openEditModal(row)}
            columns={[
              { key: 'key', label: 'Key' },
              { key: 'name', label: 'Name' },
              { 
                key: 'entityType', 
                label: 'Entity Type',
                render: (value: unknown) => <span className="text-xs font-mono">{String(value)}</span>
              },
              { 
                key: 'executorType', 
                label: 'Executor Type',
                render: (value: unknown) => <span className="text-xs font-mono">{String(value)}</span>
              },
              { 
                key: 'isActive', 
                label: 'Status',
                render: (value: unknown) => (
                  <StatusBadge status={value ? 'active' : 'inactive'} />
                )
              },
              { key: 'displayOrder', label: 'Order' }
            ]}
            actions={canManage ? (row: SideEffectDefinition) => (
              <div className="flex items-center gap-1">
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    handleToggleStatus(row);
                  }}
                  className="p-1.5 hover:bg-accent rounded transition-colors"
                  title={row.isActive ? 'Deactivate' : 'Activate'}
                >
                  <Power className={`h-4 w-4 ${row.isActive ? 'text-green-600' : 'text-gray-400'}`} />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    openEditModal(row);
                  }}
                  className="p-1.5 hover:bg-accent rounded transition-colors"
                  title="Edit"
                >
                  <Edit className="h-4 w-4 text-blue-600" />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDelete(row.id);
                  }}
                  className="p-1.5 hover:bg-accent rounded transition-colors"
                  title="Delete"
                >
                  <Trash2 className="h-4 w-4 text-red-600" />
                </button>
              </div>
            ) : undefined}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingDefinition !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingDefinition(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
          <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white dark:bg-gray-800 z-10">
            <h2 className="text-lg font-bold">
              {editingDefinition ? 'Edit Side Effect' : 'Create Side Effect'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingDefinition(null);
                resetForm();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="p-4 space-y-4">
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded p-3 flex items-start gap-2">
              <Info className="h-5 w-5 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
              <div className="text-xs text-blue-800 dark:text-blue-200">
                <strong>Note:</strong> Side effects are executed after a successful workflow transition. 
                The executor type must match a registered executor implementation.
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Key *"
                name="key"
                value={formData.key}
                onChange={(e) => setFormData({ ...formData, key: e.target.value })}
                placeholder="e.g., Notify"
                required
                disabled={!!editingDefinition}
                error={formErrors.key}
                helperText="Unique identifier (letters, numbers, underscores only)"
              />
              <TextInput
                label="Name *"
                name="name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Send Notification"
                required
                error={formErrors.name}
              />
            </div>

            <TextInput
              label="Description"
              name="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Describe what this side effect does"
              multiline
              rows={2}
            />

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium mb-1">Entity Type *</label>
                <Select
                  value={formData.entityType}
                  onChange={(e) => setFormData({ ...formData, entityType: e.target.value })}
                  disabled={!!editingDefinition}
                  className="w-full"
                >
                  {ENTITY_TYPES.map(type => (
                    <option key={type} value={type}>{type}</option>
                  ))}
                </Select>
                {formErrors.entityType && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.entityType}</p>
                )}
              </div>
              <div>
                <label className="block text-xs font-medium mb-1">Executor Type *</label>
                <Select
                  value={formData.executorType}
                  onChange={(e) => setFormData({ ...formData, executorType: e.target.value })}
                  className="w-full"
                >
                  <option value="">Select Executor</option>
                  {EXECUTOR_TYPES.map(type => (
                    <option key={type} value={type}>{type}</option>
                  ))}
                </Select>
                {formErrors.executorType && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.executorType}</p>
                )}
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">Executor Config (JSON)</label>
              <textarea
                value={formData.executorConfigJson}
                onChange={(e) => setFormData({ ...formData, executorConfigJson: e.target.value })}
                className="w-full px-3 py-2 border border-input rounded-md text-xs font-mono"
                rows={4}
                placeholder='{"key": "value"}'
              />
              {formErrors.executorConfigJson && (
                <p className="text-xs text-red-600 mt-1">{formErrors.executorConfigJson}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium mb-1">Display Order</label>
                <input
                  type="number"
                  value={formData.displayOrder}
                  onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) || 0 })}
                  className="w-full px-3 py-2 border border-input rounded-md text-sm"
                  min="0"
                />
              </div>
              <div className="flex items-center gap-2 pt-6">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-input"
                />
                <label htmlFor="isActive" className="text-xs font-medium cursor-pointer">
                  Active
                </label>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingDefinition(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingDefinition ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingDefinition ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default SideEffectsPage;

