import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Power, Save, X } from 'lucide-react';
import { getAssetTypes, createAssetType, updateAssetType, deleteAssetType, DepreciationMethod, DepreciationMethodLabels } from '../../api/assetTypes';
import { getTransactionalPnlTypes } from '../../api/pnlTypes';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { AssetType, CreateAssetTypeRequest, UpdateAssetTypeRequest } from '../../types/assetTypes';
import type { PnlType } from '../../types/pnlTypes';

interface ExtendedAssetType extends AssetType {
  defaultUsefulLifeMonths?: number;
  defaultSalvageValuePercent?: number;
  depreciationPnlTypeId?: string | null;
  assetCount?: number;
  sortOrder?: number;
}

interface AssetTypeFormData {
  name: string;
  code: string;
  description: string;
  defaultDepreciationMethod: string;
  defaultUsefulLifeMonths: number;
  defaultSalvageValuePercent: number;
  depreciationPnlTypeId: string | null;
  isActive: boolean;
  sortOrder: number;
}

interface TableColumn<T> {
  key: string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const AssetTypesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [assetTypes, setAssetTypes] = useState<ExtendedAssetType[]>([]);
  const [pnlTypes, setPnlTypes] = useState<PnlType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingType, setEditingType] = useState<ExtendedAssetType | null>(null);
  const [formData, setFormData] = useState<AssetTypeFormData>({
    name: '',
    code: '',
    description: '',
    defaultDepreciationMethod: 'StraightLine',
    defaultUsefulLifeMonths: 60,
    defaultSalvageValuePercent: 10,
    depreciationPnlTypeId: null,
    isActive: true,
    sortOrder: 0
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [typesData, pnlData] = await Promise.all([
        getAssetTypes(),
        getTransactionalPnlTypes('Expense')
      ]);
      setAssetTypes(Array.isArray(typesData) ? typesData : []);
      setPnlTypes(Array.isArray(pnlData) ? pnlData : []);
    } catch (err) {
      console.error('Error loading data:', err);
      showError('Failed to load asset types');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.name.trim()) {
        showError('Name is required');
        return;
      }
      if (!formData.code.trim()) {
        showError('Code is required');
        return;
      }
      const assetTypeData: CreateAssetTypeRequest & { defaultUsefulLifeMonths?: number; defaultSalvageValuePercent?: number; depreciationPnlTypeId?: string | null; sortOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        depreciationMethod: formData.defaultDepreciationMethod as DepreciationMethod,
        usefulLifeYears: formData.defaultUsefulLifeMonths ? formData.defaultUsefulLifeMonths / 12 : undefined,
        isActive: formData.isActive,
        defaultUsefulLifeMonths: formData.defaultUsefulLifeMonths,
        defaultSalvageValuePercent: formData.defaultSalvageValuePercent,
        depreciationPnlTypeId: formData.depreciationPnlTypeId,
        sortOrder: formData.sortOrder
      };
      await createAssetType(assetTypeData as any);
      showSuccess('Asset type created successfully');
      setShowCreateModal(false);
      resetForm();
      loadData();
    } catch (err) {
      showError((err as Error).message || 'Failed to create asset type');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingType) return;
    
    try {
      if (!formData.name.trim()) {
        showError('Name is required');
        return;
      }
      const assetTypeData: UpdateAssetTypeRequest & { defaultUsefulLifeMonths?: number; defaultSalvageValuePercent?: number; depreciationPnlTypeId?: string | null; sortOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        depreciationMethod: formData.defaultDepreciationMethod as DepreciationMethod,
        usefulLifeYears: formData.defaultUsefulLifeMonths ? formData.defaultUsefulLifeMonths / 12 : undefined,
        isActive: formData.isActive,
        defaultUsefulLifeMonths: formData.defaultUsefulLifeMonths,
        defaultSalvageValuePercent: formData.defaultSalvageValuePercent,
        depreciationPnlTypeId: formData.depreciationPnlTypeId,
        sortOrder: formData.sortOrder
      };
      await updateAssetType(editingType.id, assetTypeData as any);
      showSuccess('Asset type updated successfully');
      setShowCreateModal(false);
      setEditingType(null);
      resetForm();
      loadData();
    } catch (err) {
      showError((err as Error).message || 'Failed to update asset type');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this asset type?')) return;
    
    try {
      await deleteAssetType(id);
      showSuccess('Asset type deleted successfully');
      loadData();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete asset type');
    }
  };

  const handleToggleStatus = async (assetType: ExtendedAssetType): Promise<void> => {
    try {
      await updateAssetType(assetType.id, { isActive: !assetType.isActive } as UpdateAssetTypeRequest);
      showSuccess(`Asset type ${!assetType.isActive ? 'activated' : 'deactivated'} successfully!`);
      loadData();
    } catch (err) {
      showError((err as Error).message || 'Failed to update status');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      code: '',
      description: '',
      defaultDepreciationMethod: 'StraightLine',
      defaultUsefulLifeMonths: 60,
      defaultSalvageValuePercent: 10,
      depreciationPnlTypeId: null,
      isActive: true,
      sortOrder: 0
    });
  };

  const openEditModal = (assetType: ExtendedAssetType): void => {
    setEditingType(assetType);
    setFormData({
      name: assetType.name || '',
      code: assetType.code || '',
      description: assetType.description || '',
      defaultDepreciationMethod: assetType.depreciationMethod || 'StraightLine',
      defaultUsefulLifeMonths: assetType.defaultUsefulLifeMonths || (assetType.usefulLifeYears ? assetType.usefulLifeYears * 12 : 60),
      defaultSalvageValuePercent: assetType.defaultSalvageValuePercent || 10,
      depreciationPnlTypeId: assetType.depreciationPnlTypeId || null,
      isActive: assetType.isActive !== false,
      sortOrder: assetType.sortOrder || 0
    });
    setShowCreateModal(true);
  };

  const depreciationOptions = Object.entries(DepreciationMethodLabels).map(([value, label]) => ({
    value,
    label
  }));

  const pnlTypeOptions = [
    { value: '', label: '(None)' },
    ...pnlTypes.map(pt => ({ value: pt.id, label: `${pt.name} (${pt.code || ''})` }))
  ];

  if (loading) {
    return (
      <PageShell title="Asset Types" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Asset Types' }]}>
        <LoadingSpinner message="Loading asset types..." fullPage />
      </PageShell>
    );
  }

  const columns: TableColumn<ExtendedAssetType>[] = [
    { 
      key: 'name', 
      label: 'Name',
      render: (value, row) => (
        <div>
          <span className="font-medium">{value}</span>
          <span className="ml-2 text-xs px-2 py-0.5 bg-slate-600 rounded text-slate-300">{row.code}</span>
        </div>
      )
    },
    { key: 'description', label: 'Description' },
    { 
      key: 'defaultDepreciationMethod', 
      label: 'Depreciation Method',
      render: (value) => DepreciationMethodLabels[value as DepreciationMethod] || value
    },
    { 
      key: 'defaultUsefulLifeMonths', 
      label: 'Useful Life',
      render: (value) => `${value} months (${((value as number) / 12).toFixed(1)} years)`
    },
    { 
      key: 'defaultSalvageValuePercent', 
      label: 'Salvage %',
      render: (value) => `${value}%`
    },
    { 
      key: 'assetCount', 
      label: 'Assets',
      render: (value) => (value as number) || 0
    },
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
      sortable: false,
      render: (value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => { e.stopPropagation(); handleToggleStatus(row); }}
            title={row.isActive ? 'Deactivate' : 'Activate'}
            className={`${row.isActive ? 'text-yellow-600' : 'text-green-600'} hover:opacity-75 cursor-pointer transition-colors`}
          >
            <Power className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); openEditModal(row); }}
            title="Edit"
            className="text-blue-600 hover:opacity-75 cursor-pointer transition-colors"
          >
            <Edit className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); handleDelete(row.id); }}
            title="Delete"
            className="text-red-600 hover:opacity-75 cursor-pointer transition-colors"
            disabled={(row.assetCount || 0) > 0}
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      )
    }
  ];

  return (
    <PageShell
      title="Asset Types"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Asset Types' }]}
      actions={
        <Button size="sm" onClick={() => { resetForm(); setEditingType(null); setShowCreateModal(true); }} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Asset Type
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto">
      <Card>
        {assetTypes.length === 0 ? (
          <EmptyState
            title="No asset types found"
            message="Add your first asset type to get started."
          />
        ) : (
          <DataTable
            data={assetTypes}
            columns={columns}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingType !== null}
        onClose={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingType ? 'Edit Asset Type' : 'Create Asset Type'}
            </h2>
            <button
              onClick={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="space-y-2">
            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Name *"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Vehicles"
                required
              />
              <TextInput
                label="Code *"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                placeholder="e.g., VEH"
                required
              />
            </div>
            
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={2}
                className="flex w-full rounded-md border border-input bg-background px-2 py-1 text-xs"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <SelectInput
                label="Default Depreciation Method"
                value={formData.defaultDepreciationMethod}
                onChange={(e) => setFormData({ ...formData, defaultDepreciationMethod: e.target.value })}
                options={depreciationOptions}
              />
              <SelectInput
                label="Depreciation P&L Type"
                value={formData.depreciationPnlTypeId || ''}
                onChange={(e) => setFormData({ ...formData, depreciationPnlTypeId: e.target.value || null })}
                options={pnlTypeOptions}
              />
            </div>

            <div className="grid grid-cols-3 gap-2">
              <TextInput
                label="Useful Life (Months)"
                type="number"
                value={formData.defaultUsefulLifeMonths}
                onChange={(e) => setFormData({ ...formData, defaultUsefulLifeMonths: parseInt(e.target.value) || 60 })}
              />
              <TextInput
                label="Salvage Value %"
                type="number"
                value={formData.defaultSalvageValuePercent}
                onChange={(e) => setFormData({ ...formData, defaultSalvageValuePercent: parseFloat(e.target.value) || 10 })}
              />
              <TextInput
                label="Sort Order"
                type="number"
                value={formData.sortOrder}
                onChange={(e) => setFormData({ ...formData, sortOrder: parseInt(e.target.value) || 0 })}
              />
            </div>

            <div className="flex items-center gap-3 pt-2">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
              />
              <label htmlFor="isActive" className="text-xs font-medium cursor-pointer">
                Active Status
              </label>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button variant="outline" onClick={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}>
                Cancel
              </Button>
              <Button onClick={editingType ? handleUpdate : handleCreate} className="flex items-center gap-2">
                <Save className="h-4 w-4" />
                {editingType ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default AssetTypesPage;

