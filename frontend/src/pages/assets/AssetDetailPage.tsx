import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { 
  Boxes, ArrowLeft, Edit, Trash2, Wrench, TrendingDown, 
  AlertTriangle, Calendar, DollarSign, MapPin, Tag, Plus, Clock
} from 'lucide-react';
import { 
  getAsset, deleteAsset, getDepreciationSchedule,
  createMaintenanceRecord, createDisposal,
  AssetStatusLabels, MaintenanceType, MaintenanceTypeLabels,
  DisposalMethod, DisposalMethodLabels
} from '../../api/assets';
import { getAssetTypes, DepreciationMethodLabels } from '../../api/assetTypes';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, TextInput, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Asset, MaintenanceType as MaintenanceTypeEnum, DisposalMethod as DisposalMethodEnum, CreateMaintenanceRecordRequest, CreateDisposalRequest } from '../../types/assets';
import type { DepreciationMethod } from '../../types/assetTypes';

interface ExtendedAsset extends Asset {
  assetTag?: string;
  assetTypeName?: string;
  purchaseCost?: number;
  purchasePrice?: number;
  currentBookValue?: number;
  accumulatedDepreciation?: number;
  depreciationMethod?: DepreciationMethod;
  usefulLifeMonths?: number;
  salvageValue?: number;
  monthlyDepreciation?: number;
  remainingLifeMonths?: number;
  maintenanceRecords?: ExtendedMaintenanceRecord[];
  serialNumber?: string;
  manufacturer?: string;
  location?: string;
  description?: string;
}

interface ExtendedMaintenanceRecord {
  id: string;
  maintenanceType: MaintenanceTypeEnum;
  scheduledDate: string;
  completedDate?: string;
  description?: string;
  cost?: number;
  vendor?: string;
}

interface DepreciationScheduleEntry {
  period: string;
  depreciationAmount: number;
  accumulatedDepreciation: number;
  bookValue: number;
}

interface MaintenanceFormData {
  maintenanceType: MaintenanceTypeEnum;
  scheduledDate: string;
  completedDate: string;
  description: string;
  cost: string;
  vendor: string;
  notes: string;
}

interface DisposalFormData {
  disposalMethod: DisposalMethodEnum;
  disposalDate: string;
  salePrice: string;
  buyer: string;
  reason: string;
  notes: string;
}

const AssetDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [asset, setAsset] = useState<ExtendedAsset | null>(null);
  const [depreciationSchedule, setDepreciationSchedule] = useState<DepreciationScheduleEntry[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showMaintenanceModal, setShowMaintenanceModal] = useState<boolean>(false);
  const [showDisposalModal, setShowDisposalModal] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<'overview' | 'maintenance' | 'depreciation'>('overview');

  const [maintenanceForm, setMaintenanceForm] = useState<MaintenanceFormData>({
    maintenanceType: MaintenanceType.Preventive,
    scheduledDate: new Date().toISOString().split('T')[0],
    completedDate: '',
    description: '',
    cost: '',
    vendor: '',
    notes: ''
  });

  const [disposalForm, setDisposalForm] = useState<DisposalFormData>({
    disposalMethod: DisposalMethod.Sale,
    disposalDate: new Date().toISOString().split('T')[0],
    salePrice: '',
    buyer: '',
    reason: '',
    notes: ''
  });

  useEffect(() => {
    if (id) {
      loadAsset();
    }
  }, [id]);

  const loadAsset = async (): Promise<void> => {
    if (!id) return;
    try {
      setLoading(true);
      const [assetData, scheduleData] = await Promise.all([
        getAsset(id),
        getDepreciationSchedule(id).catch(() => [])
      ]);
      setAsset(assetData as ExtendedAsset);
      setDepreciationSchedule(Array.isArray(scheduleData) ? scheduleData : []);
    } catch (err: any) {
      console.error('Error loading asset:', err);
      showError('Failed to load asset');
      navigate('/assets/list');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!id) return;
    if (!window.confirm('Are you sure you want to delete this asset? This action cannot be undone.')) return;
    try {
      await deleteAsset(id);
      showSuccess('Asset deleted successfully');
      navigate('/assets/list');
    } catch (err: any) {
      showError(err.message || 'Failed to delete asset');
    }
  };

  const handleAddMaintenance = async (): Promise<void> => {
    if (!id) return;
    try {
      if (!maintenanceForm.description.trim()) {
        showError('Description is required');
        return;
      }
      const maintenanceData: CreateMaintenanceRecordRequest = {
        assetId: id,
        maintenanceType: maintenanceForm.maintenanceType,
        scheduledDate: maintenanceForm.scheduledDate,
        cost: parseFloat(maintenanceForm.cost) || undefined,
        description: maintenanceForm.description.trim(),
        performedBy: maintenanceForm.vendor?.trim() || undefined,
        notes: maintenanceForm.notes?.trim() || undefined
      };
      await createMaintenanceRecord(maintenanceData);
      showSuccess('Maintenance record added successfully');
      setShowMaintenanceModal(false);
      resetMaintenanceForm();
      loadAsset();
    } catch (err: any) {
      showError(err.message || 'Failed to add maintenance record');
    }
  };

  const handleDispose = async (): Promise<void> => {
    if (!id) return;
    try {
      if (!disposalForm.reason.trim()) {
        showError('Disposal reason is required');
        return;
      }
      const disposalData: CreateDisposalRequest = {
        assetId: id,
        disposalDate: disposalForm.disposalDate,
        disposalMethod: disposalForm.disposalMethod,
        disposalAmount: parseFloat(disposalForm.salePrice) || undefined,
        reason: disposalForm.reason.trim()
      };
      await createDisposal(disposalData);
      showSuccess('Disposal initiated successfully');
      setShowDisposalModal(false);
      loadAsset();
    } catch (err: any) {
      showError(err.message || 'Failed to initiate disposal');
    }
  };

  const resetMaintenanceForm = (): void => {
    setMaintenanceForm({
      maintenanceType: MaintenanceType.Preventive,
      scheduledDate: new Date().toISOString().split('T')[0],
      completedDate: '',
      description: '',
      cost: '',
      vendor: '',
      notes: ''
    });
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY', { year: 'numeric', month: 'short', day: 'numeric' });
  };

  const getStatusColor = (status: string): string => {
    const colors: Record<string, string> = {
      Active: 'bg-green-600',
      UnderMaintenance: 'bg-yellow-600',
      Reserved: 'bg-blue-600',
      OutOfService: 'bg-orange-600',
      Disposed: 'bg-red-600',
      WrittenOff: 'bg-slate-600',
      PendingDisposal: 'bg-purple-600'
    };
    return colors[status] || 'bg-slate-600';
  };

  const maintenanceTypeOptions = Object.entries(MaintenanceTypeLabels).map(([value, label]) => ({ value, label }));
  const disposalMethodOptions = Object.entries(DisposalMethodLabels).map(([value, label]) => ({ value, label }));

  if (loading) {
    return (
      <PageShell title="Asset" breadcrumbs={[{ label: 'Assets', path: '/assets/list' }, { label: 'Details' }]}>
        <LoadingSpinner fullPage />
      </PageShell>
    );
  }
  if (!asset) {
    return (
      <PageShell title="Asset" breadcrumbs={[{ label: 'Assets', path: '/assets/list' }, { label: 'Details' }]}>
        <EmptyState title="Asset not found" message="The requested asset could not be loaded." />
      </PageShell>
    );
  }

  const depreciationPercent = (asset.purchaseCost || asset.purchasePrice || 0) > 0 
    ? (((asset.accumulatedDepreciation || 0) / (asset.purchaseCost || asset.purchasePrice || 1)) * 100).toFixed(1)
    : 0;

  return (
    <PageShell title="Asset" breadcrumbs={[{ label: 'Assets', path: '/assets/list' }, { label: 'Details' }]}>
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link to="/assets/list" className="p-2 hover:bg-slate-700 rounded-lg transition-colors">
            <ArrowLeft className="h-5 w-5 text-slate-400" />
          </Link>
          <div>
            <div className="flex items-center gap-3">
              <Boxes className="h-6 w-6 text-brand-500" />
              <h1 className="text-2xl font-bold text-white">{asset.name}</h1>
              <span className={`px-3 py-1 text-sm rounded ${getStatusColor(asset.status)} text-white`}>
                {AssetStatusLabels[asset.status] || asset.status}
              </span>
            </div>
            <p className="text-slate-400 mt-1">{asset.assetTag || asset.assetNumber} • {asset.assetTypeName}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {asset.status === 'Active' && (
            <>
              <Button variant="secondary" onClick={() => setShowMaintenanceModal(true)}>
                <Wrench className="h-4 w-4 mr-2" />
                Add Maintenance
              </Button>
              <Button variant="warning" onClick={() => setShowDisposalModal(true)}>
                <AlertTriangle className="h-4 w-4 mr-2" />
                Dispose
              </Button>
            </>
          )}
          <Button variant="danger" onClick={handleDelete}>
            <Trash2 className="h-4 w-4 mr-2" />
            Delete
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-500/20 rounded">
              <DollarSign className="h-5 w-5 text-blue-500" />
            </div>
            <div>
              <p className="text-slate-400 text-sm">Purchase Cost</p>
              <p className="text-xl font-bold text-white">{formatCurrency(asset.purchaseCost || asset.purchasePrice)}</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-green-500/20 rounded">
              <TrendingDown className="h-5 w-5 text-green-500" />
            </div>
            <div>
              <p className="text-slate-400 text-sm">Current Book Value</p>
              <p className="text-xl font-bold text-green-500">{formatCurrency(asset.currentBookValue || asset.currentValue)}</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-orange-500/20 rounded">
              <TrendingDown className="h-5 w-5 text-orange-500" />
            </div>
            <div>
              <p className="text-slate-400 text-sm">Depreciated</p>
              <p className="text-xl font-bold text-orange-500">{depreciationPercent}%</p>
              <p className="text-xs text-slate-500">{formatCurrency(asset.accumulatedDepreciation)}</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-purple-500/20 rounded">
              <Wrench className="h-5 w-5 text-purple-500" />
            </div>
            <div>
              <p className="text-slate-400 text-sm">Maintenance Records</p>
              <p className="text-xl font-bold text-white">{asset.maintenanceRecords?.length || 0}</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-slate-700 pb-2">
        {(['overview', 'maintenance', 'depreciation'] as const).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 rounded-t-lg font-medium transition-colors ${
              activeTab === tab
                ? 'bg-slate-700 text-white'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            {tab.charAt(0).toUpperCase() + tab.slice(1)}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card className="p-4">
            <h3 className="text-lg font-semibold text-white mb-4">Asset Details</h3>
            <div className="space-y-3">
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Asset Tag</span>
                <span className="text-white font-medium">{asset.assetTag || asset.assetNumber}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Serial Number</span>
                <span className="text-white">{asset.serialNumber || '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Manufacturer</span>
                <span className="text-white">{asset.manufacturer || '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Asset Type</span>
                <span className="text-white">{asset.assetTypeName || '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Location</span>
                <span className="text-white">{asset.location || '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Purchase Date</span>
                <span className="text-white">{formatDate(asset.purchaseDate)}</span>
              </div>
              {asset.description && (
                <div className="py-2">
                  <span className="text-slate-400 block mb-1">Description</span>
                  <span className="text-white">{asset.description}</span>
                </div>
              )}
            </div>
          </Card>

          <Card className="p-4">
            <h3 className="text-lg font-semibold text-white mb-4">Depreciation Settings</h3>
            <div className="space-y-3">
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Depreciation Method</span>
                <span className="text-white">{asset.depreciationMethod ? DepreciationMethodLabels[asset.depreciationMethod] : '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Useful Life</span>
                <span className="text-white">{asset.usefulLifeMonths ? `${asset.usefulLifeMonths} months (${(asset.usefulLifeMonths / 12).toFixed(1)} years)` : '-'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Salvage Value</span>
                <span className="text-white">{formatCurrency(asset.salvageValue)}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-slate-700">
                <span className="text-slate-400">Monthly Depreciation</span>
                <span className="text-white">{formatCurrency(asset.monthlyDepreciation)}</span>
              </div>
              <div className="flex justify-between py-2">
                <span className="text-slate-400">Remaining Life</span>
                <span className="text-white">{asset.remainingLifeMonths || 0} months</span>
              </div>
            </div>
          </Card>
        </div>
      )}

      {activeTab === 'maintenance' && (
        <Card className="p-4">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-white">Maintenance History</h3>
            {asset.status === 'Active' && (
              <Button size="sm" onClick={() => setShowMaintenanceModal(true)}>
                <Plus className="h-4 w-4 mr-1" />
                Add Record
              </Button>
            )}
          </div>
          {(!asset.maintenanceRecords || asset.maintenanceRecords.length === 0) ? (
            <p className="text-slate-400 text-center py-8">No maintenance records found</p>
          ) : (
            <div className="space-y-3">
              {asset.maintenanceRecords.map((record) => (
                <div key={record.id} className="flex justify-between items-start p-3 bg-slate-700/50 rounded-lg">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="px-2 py-0.5 text-xs bg-blue-600 rounded text-white">
                        {MaintenanceTypeLabels[record.maintenanceType] || record.maintenanceType}
                      </span>
                      <span className="text-slate-400 text-sm">{formatDate(record.completedDate || record.scheduledDate)}</span>
                    </div>
                    <p className="text-white mt-1">{record.description}</p>
                    {record.vendor && <p className="text-slate-400 text-sm">Vendor: {record.vendor}</p>}
                  </div>
                  <span className="text-brand-400 font-semibold">{formatCurrency(record.cost)}</span>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {activeTab === 'depreciation' && (
        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white mb-4">Depreciation Schedule</h3>
          {depreciationSchedule.length === 0 ? (
            <p className="text-slate-400 text-center py-8">No depreciation entries recorded yet</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-slate-400 border-b border-slate-700">
                  <tr>
                    <th className="text-left py-2">Period</th>
                    <th className="text-right py-2">Depreciation</th>
                    <th className="text-right py-2">Accumulated</th>
                    <th className="text-right py-2">Book Value</th>
                  </tr>
                </thead>
                <tbody>
                  {depreciationSchedule.map((entry, idx) => (
                    <tr key={idx} className="border-b border-slate-700/50 text-white">
                      <td className="py-2">{entry.period}</td>
                      <td className="text-right text-red-400">{formatCurrency(entry.depreciationAmount)}</td>
                      <td className="text-right text-orange-400">{formatCurrency(entry.accumulatedDepreciation)}</td>
                      <td className="text-right text-green-400">{formatCurrency(entry.bookValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      )}

      {/* Add Maintenance Modal */}
      <Modal
        isOpen={showMaintenanceModal}
        onClose={() => { setShowMaintenanceModal(false); resetMaintenanceForm(); }}
        title="Add Maintenance Record"
        size="md"
      >
        <div className="space-y-4">
          <SelectInput
            label="Maintenance Type"
            value={maintenanceForm.maintenanceType}
            onChange={(e) => setMaintenanceForm({ ...maintenanceForm, maintenanceType: e.target.value as MaintenanceTypeEnum })}
            options={maintenanceTypeOptions}
          />
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Scheduled Date"
              type="date"
              value={maintenanceForm.scheduledDate}
              onChange={(e) => setMaintenanceForm({ ...maintenanceForm, scheduledDate: e.target.value })}
            />
            <TextInput
              label="Completed Date"
              type="date"
              value={maintenanceForm.completedDate}
              onChange={(e) => setMaintenanceForm({ ...maintenanceForm, completedDate: e.target.value })}
            />
          </div>
          <TextInput
            label="Description"
            value={maintenanceForm.description}
            onChange={(e) => setMaintenanceForm({ ...maintenanceForm, description: e.target.value })}
            required
            multiline
            rows={2}
          />
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Cost (MYR)"
              type="number"
              value={maintenanceForm.cost}
              onChange={(e) => setMaintenanceForm({ ...maintenanceForm, cost: e.target.value })}
            />
            <TextInput
              label="Vendor"
              value={maintenanceForm.vendor}
              onChange={(e) => setMaintenanceForm({ ...maintenanceForm, vendor: e.target.value })}
            />
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => { setShowMaintenanceModal(false); resetMaintenanceForm(); }}>Cancel</Button>
            <Button onClick={handleAddMaintenance}>Add Record</Button>
          </div>
        </div>
      </Modal>

      {/* Disposal Modal */}
      <Modal
        isOpen={showDisposalModal}
        onClose={() => setShowDisposalModal(false)}
        title="Dispose Asset"
        size="md"
      >
        <div className="space-y-4">
          <div className="p-3 bg-yellow-600/20 border border-yellow-600 rounded-lg text-yellow-300 text-sm">
            <AlertTriangle className="h-4 w-4 inline mr-2" />
            This will mark the asset as disposed. This action requires approval.
          </div>
          <SelectInput
            label="Disposal Method"
            value={disposalForm.disposalMethod}
            onChange={(e) => setDisposalForm({ ...disposalForm, disposalMethod: e.target.value as DisposalMethodEnum })}
            options={disposalMethodOptions}
          />
          <TextInput
            label="Disposal Date"
            type="date"
            value={disposalForm.disposalDate}
            onChange={(e) => setDisposalForm({ ...disposalForm, disposalDate: e.target.value })}
          />
          {disposalForm.disposalMethod === 'Sale' && (
            <>
              <TextInput
                label="Sale Price (MYR)"
                type="number"
                value={disposalForm.salePrice}
                onChange={(e) => setDisposalForm({ ...disposalForm, salePrice: e.target.value })}
              />
              <TextInput
                label="Buyer"
                value={disposalForm.buyer}
                onChange={(e) => setDisposalForm({ ...disposalForm, buyer: e.target.value })}
              />
            </>
          )}
          <TextInput
            label="Reason for Disposal"
            value={disposalForm.reason}
            onChange={(e) => setDisposalForm({ ...disposalForm, reason: e.target.value })}
            required
            multiline
            rows={2}
          />
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => setShowDisposalModal(false)}>Cancel</Button>
            <Button variant="warning" onClick={handleDispose}>Initiate Disposal</Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default AssetDetailPage;

