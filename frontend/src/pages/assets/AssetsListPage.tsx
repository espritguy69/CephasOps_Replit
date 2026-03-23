import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit, Trash2, Eye, Boxes, Search } from 'lucide-react';
import { getAssets, createAsset, updateAsset, deleteAsset, AssetStatus, AssetStatusLabels } from '../../api/assets';
import { getAssetTypes, DepreciationMethodLabels } from '../../api/assetTypes';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Asset, AssetStatus as AssetStatusEnum, CreateAssetRequest, UpdateAssetRequest, AssetFilters } from '../../types/assets';
import type { AssetType } from '../../types/assetTypes';

interface ExtendedAsset extends Asset {
  assetTag?: string;
  assetTypeName?: string;
  purchaseCost?: number;
  currentBookValue?: number;
  serialNumber?: string;
  manufacturer?: string;
  location?: string;
  notes?: string;
}

interface AssetFormData {
  assetTypeId: string;
  assetTag: string;
  name: string;
  description: string;
  serialNumber: string;
  manufacturer: string;
  purchaseDate: string;
  purchaseCost: string;
  location: string;
  notes: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const AssetsListPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [assets, setAssets] = useState<ExtendedAsset[]>([]);
  const [assetTypes, setAssetTypes] = useState<AssetType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingAsset, setEditingAsset] = useState<ExtendedAsset | null>(null);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [typeFilter, setTypeFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [formData, setFormData] = useState<AssetFormData>({
    assetTypeId: '',
    assetTag: '',
    name: '',
    description: '',
    serialNumber: '',
    manufacturer: '',
    purchaseDate: new Date().toISOString().split('T')[0],
    purchaseCost: '',
    location: '',
    notes: ''
  });

  useEffect(() => {
    loadData();
  }, [searchTerm, typeFilter, statusFilter]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: AssetFilters & { search?: string } = {};
      if (searchTerm) params.search = searchTerm;
      if (typeFilter) params.assetTypeId = typeFilter;
      if (statusFilter) params.status = statusFilter as AssetStatusEnum;
      
      const [assetsData, typesData] = await Promise.all([
        getAssets(params),
        getAssetTypes({ isActive: true })
      ]);
      setAssets(Array.isArray(assetsData) ? assetsData : []);
      setAssetTypes(Array.isArray(typesData) ? typesData : []);
    } catch (err: any) {
      console.error('Error loading data:', err);
      showError('Failed to load assets');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.assetTypeId) {
        showError('Asset type is required');
        return;
      }
      if (!formData.assetTag.trim()) {
        showError('Asset tag is required');
        return;
      }
      if (!formData.name.trim()) {
        showError('Name is required');
        return;
      }
      const assetData: CreateAssetRequest = {
        assetTypeId: formData.assetTypeId,
        assetNumber: formData.assetTag,
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
        purchaseDate: formData.purchaseDate,
        purchasePrice: parseFloat(formData.purchaseCost) || 0,
        location: formData.location?.trim() || undefined,
        serialNumber: formData.serialNumber?.trim() || undefined,
        manufacturer: formData.manufacturer?.trim() || undefined
      };
      await createAsset(assetData);
      showSuccess('Asset created successfully');
      setShowCreateModal(false);
      resetForm();
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to create asset');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingAsset) return;
    try {
      const assetData: UpdateAssetRequest = {
        assetTypeId: formData.assetTypeId,
        assetNumber: formData.assetTag,
        name: formData.name.trim(),
        description: formData.description?.trim() || undefined,
        purchaseDate: formData.purchaseDate,
        purchasePrice: parseFloat(formData.purchaseCost) || 0,
        location: formData.location?.trim() || undefined,
        serialNumber: formData.serialNumber?.trim() || undefined,
        manufacturer: formData.manufacturer?.trim() || undefined
      };
      await updateAsset(editingAsset.id, assetData);
      showSuccess('Asset updated successfully');
      setShowCreateModal(false);
      setEditingAsset(null);
      resetForm();
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to update asset');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this asset?')) return;
    try {
      await deleteAsset(id);
      showSuccess('Asset deleted successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete asset');
    }
  };

  const resetForm = (): void => {
    setFormData({
      assetTypeId: '',
      assetTag: '',
      name: '',
      description: '',
      serialNumber: '',
      manufacturer: '',
      purchaseDate: new Date().toISOString().split('T')[0],
      purchaseCost: '',
      location: '',
      notes: ''
    });
  };

  const openEditModal = (asset: ExtendedAsset): void => {
    setEditingAsset(asset);
    setFormData({
      assetTypeId: asset.assetTypeId || '',
      assetTag: asset.assetTag || asset.assetNumber || '',
      name: asset.name || '',
      description: asset.description || '',
      serialNumber: asset.serialNumber || '',
      manufacturer: asset.manufacturer || '',
      purchaseDate: asset.purchaseDate ? asset.purchaseDate.split('T')[0] : new Date().toISOString().split('T')[0],
      purchaseCost: (asset.purchaseCost || asset.purchasePrice || '').toString(),
      location: asset.location || '',
      notes: asset.notes || ''
    });
    setShowCreateModal(true);
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY');
  };

  const getStatusBadge = (status: AssetStatusEnum | string): React.ReactNode => {
    const colors: Record<string, string> = {
      Active: 'bg-green-600',
      UnderMaintenance: 'bg-yellow-600',
      Reserved: 'bg-blue-600',
      OutOfService: 'bg-orange-600',
      Disposed: 'bg-red-600',
      WrittenOff: 'bg-slate-600',
      PendingDisposal: 'bg-purple-600'
    };
    return (
      <span className={`px-2 py-1 text-xs rounded ${colors[status] || 'bg-slate-600'} text-white`}>
        {AssetStatusLabels[status as AssetStatusEnum] || status}
      </span>
    );
  };

  const typeOptions = [
    { value: '', label: 'All Types' },
    ...assetTypes.map(t => ({ value: t.id, label: t.name }))
  ];

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    ...Object.entries(AssetStatusLabels).map(([value, label]) => ({ value, label }))
  ];

  if (loading && assets.length === 0) return <LoadingSpinner />;

  const columns: TableColumn<ExtendedAsset>[] = [
    { 
      key: 'assetTag', 
      label: 'Asset Tag',
      render: (v: unknown, row: ExtendedAsset) => (
        <div>
          <span className="font-medium text-brand-400">{v as string || row.assetNumber}</span>
          {row.serialNumber && <p className="text-xs text-slate-400">SN: {row.serialNumber}</p>}
        </div>
      )
    },
    { key: 'name', label: 'Name' },
    { key: 'assetTypeName', label: 'Type' },
    { key: 'location', label: 'Location' },
    { key: 'purchaseCost', label: 'Purchase Cost', render: (v: unknown) => formatCurrency(v as number) },
    { key: 'currentBookValue', label: 'Book Value', render: (v: unknown) => formatCurrency(v as number) },
    { key: 'status', label: 'Status', render: (v: unknown) => getStatusBadge(v as string) },
    {
      key: 'actions',
      label: 'Actions',
      sortable: false,
      render: (_: unknown, row: ExtendedAsset) => (
        <div className="flex items-center gap-2">
          <Link to={`/assets/${row.id}`} className="text-blue-500 hover:text-blue-400" title="View">
            <Eye className="h-4 w-4" />
          </Link>
          <button onClick={() => openEditModal(row)} className="text-yellow-500 hover:text-yellow-400" title="Edit">
            <Edit className="h-4 w-4" />
          </button>
          <button onClick={() => handleDelete(row.id)} className="text-red-500 hover:text-red-400" title="Delete">
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )
    }
  ];

  if (loading && assets.length === 0) {
    return (
      <PageShell title="Assets" breadcrumbs={[{ label: 'Assets' }]}>
        <LoadingSpinner message="Loading assets..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Assets"
      breadcrumbs={[{ label: 'Assets' }]}
      actions={
        <Button onClick={() => { resetForm(); setEditingAsset(null); setShowCreateModal(true); }}>
          <Plus className="h-4 w-4 mr-2" />
          Add Asset
        </Button>
      }
    >
      <div className="space-y-4">
      {/* Filters */}
      <div className="flex gap-4 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            type="text"
            placeholder="Search assets..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
        </div>
        <SelectInput value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)} options={typeOptions} className="w-48" />
        <SelectInput value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} options={statusOptions} className="w-48" />
      </div>

      <Card>
        {assets.length === 0 ? (
          <EmptyState message="No assets found" />
        ) : (
          <DataTable data={assets} columns={columns} />
        )}
      </Card>

      {/* Create/Edit Modal */}
      </div>
      <Modal
        isOpen={showCreateModal}
        onClose={() => { setShowCreateModal(false); resetForm(); setEditingAsset(null); }}
        title={editingAsset ? 'Edit Asset' : 'Add Asset'}
        size="lg"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <SelectInput 
              label="Asset Type" 
              value={formData.assetTypeId} 
              onChange={(e) => setFormData({ ...formData, assetTypeId: e.target.value })} 
              options={[{ value: '', label: 'Select type...' }, ...assetTypes.map(t => ({ value: t.id, label: t.name }))]}
              required 
            />
            <TextInput 
              label="Asset Tag" 
              value={formData.assetTag} 
              onChange={(e) => setFormData({ ...formData, assetTag: e.target.value.toUpperCase() })} 
              required 
              placeholder="e.g., VEH-001" 
            />
          </div>
          <TextInput 
            label="Name" 
            value={formData.name} 
            onChange={(e) => setFormData({ ...formData, name: e.target.value })} 
            required 
            placeholder="e.g., Toyota Hilux 2024" 
          />
          <div className="grid grid-cols-2 gap-4">
            <TextInput 
              label="Serial Number" 
              value={formData.serialNumber} 
              onChange={(e) => setFormData({ ...formData, serialNumber: e.target.value })} 
            />
            <TextInput 
              label="Manufacturer" 
              value={formData.manufacturer} 
              onChange={(e) => setFormData({ ...formData, manufacturer: e.target.value })} 
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <TextInput 
              label="Purchase Date" 
              type="date" 
              value={formData.purchaseDate} 
              onChange={(e) => setFormData({ ...formData, purchaseDate: e.target.value })} 
              required 
            />
            <TextInput 
              label="Purchase Cost (MYR)" 
              type="number" 
              value={formData.purchaseCost} 
              onChange={(e) => setFormData({ ...formData, purchaseCost: e.target.value })} 
              required 
            />
          </div>
          <TextInput 
            label="Location" 
            value={formData.location} 
            onChange={(e) => setFormData({ ...formData, location: e.target.value })} 
            placeholder="e.g., Head Office, Warehouse A" 
          />
          <TextInput 
            label="Notes" 
            value={formData.notes} 
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })} 
            multiline 
            rows={2} 
          />

          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => { setShowCreateModal(false); resetForm(); setEditingAsset(null); }}>Cancel</Button>
            <Button onClick={editingAsset ? handleUpdate : handleCreate}>{editingAsset ? 'Update' : 'Create'}</Button>
          </div>
        </div>
      </Modal>
    </PageShell>
  );
};

export default AssetsListPage;

