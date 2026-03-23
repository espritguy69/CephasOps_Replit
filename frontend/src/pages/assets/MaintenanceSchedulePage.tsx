import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Wrench, Calendar, Check, Clock, AlertTriangle, Filter, Eye } from 'lucide-react';
import { 
  getMaintenanceRecords, getUpcomingMaintenance, createMaintenanceRecord, updateMaintenanceRecord,
  MaintenanceType, MaintenanceTypeLabels, getAssets
} from '../../api/assets';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { MaintenanceRecord, MaintenanceType as MaintenanceTypeEnum, CreateMaintenanceRecordRequest, UpdateMaintenanceRecordRequest, MaintenanceFilters } from '../../types/assets';
import type { Asset } from '../../types/assets';

interface ExtendedMaintenanceRecord extends MaintenanceRecord {
  assetName?: string;
  assetTag?: string;
  assetId: string;
}

interface MaintenanceFormData {
  assetId: string;
  maintenanceType: MaintenanceTypeEnum;
  scheduledDate: string;
  description: string;
  estimatedCost: string;
  vendor: string;
  notes: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const MaintenanceSchedulePage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [records, setRecords] = useState<ExtendedMaintenanceRecord[]>([]);
  const [upcomingRecords, setUpcomingRecords] = useState<ExtendedMaintenanceRecord[]>([]);
  const [assets, setAssets] = useState<Asset[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<'upcoming' | 'all'>('upcoming');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [typeFilter, setTypeFilter] = useState<string>('');
  
  const [formData, setFormData] = useState<MaintenanceFormData>({
    assetId: '',
    maintenanceType: MaintenanceType.Preventive,
    scheduledDate: new Date().toISOString().split('T')[0],
    description: '',
    estimatedCost: '',
    vendor: '',
    notes: ''
  });

  useEffect(() => {
    loadData();
  }, [statusFilter, typeFilter]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: MaintenanceFilters = {};
      if (statusFilter) params.status = statusFilter as any;
      if (typeFilter) params.maintenanceType = typeFilter as MaintenanceTypeEnum;
      
      const [recordsData, upcomingData, assetsData] = await Promise.all([
        getMaintenanceRecords(params),
        getUpcomingMaintenance(30),
        getAssets({ status: 'Active' as any })
      ]);
      setRecords(Array.isArray(recordsData) ? recordsData : []);
      setUpcomingRecords(Array.isArray(upcomingData) ? upcomingData : []);
      setAssets(Array.isArray(assetsData) ? assetsData : []);
    } catch (err: any) {
      console.error('Error loading data:', err);
      showError('Failed to load maintenance records');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.assetId) {
        showError('Asset is required');
        return;
      }
      if (!formData.description.trim()) {
        showError('Description is required');
        return;
      }
      const maintenanceData: CreateMaintenanceRecordRequest = {
        assetId: formData.assetId,
        maintenanceType: formData.maintenanceType,
        scheduledDate: formData.scheduledDate,
        cost: parseFloat(formData.estimatedCost) || undefined,
        description: formData.description.trim(),
        performedBy: formData.vendor?.trim() || undefined,
        notes: formData.notes?.trim() || undefined
      };
      await createMaintenanceRecord(maintenanceData);
      showSuccess('Maintenance scheduled successfully');
      setShowCreateModal(false);
      resetForm();
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to schedule maintenance');
    }
  };

  const handleMarkComplete = async (record: ExtendedMaintenanceRecord): Promise<void> => {
    try {
      const updateData: UpdateMaintenanceRecordRequest = {
        completedDate: new Date().toISOString().split('T')[0],
        status: 'Completed'
      };
      await updateMaintenanceRecord(record.id, updateData);
      showSuccess('Maintenance marked as complete');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to update maintenance');
    }
  };

  const resetForm = (): void => {
    setFormData({
      assetId: '',
      maintenanceType: MaintenanceType.Preventive,
      scheduledDate: new Date().toISOString().split('T')[0],
      description: '',
      estimatedCost: '',
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

  const getDaysUntil = (dateStr: string | null | undefined): number | null => {
    if (!dateStr) return null;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const targetDate = new Date(dateStr);
    targetDate.setHours(0, 0, 0, 0);
    const diffTime = targetDate.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  };

  const getUrgencyBadge = (scheduledDate: string | null | undefined): React.ReactNode | null => {
    const days = getDaysUntil(scheduledDate);
    if (days === null) return null;
    if (days < 0) return <span className="px-2 py-1 text-xs bg-red-600 rounded text-white">Overdue</span>;
    if (days === 0) return <span className="px-2 py-1 text-xs bg-orange-600 rounded text-white">Today</span>;
    if (days <= 7) return <span className="px-2 py-1 text-xs bg-yellow-600 rounded text-white">This Week</span>;
    return <span className="px-2 py-1 text-xs bg-green-600 rounded text-white">Scheduled</span>;
  };

  const getStatusBadge = (status: string): React.ReactNode => {
    const colors: Record<string, string> = {
      Scheduled: 'bg-blue-600',
      InProgress: 'bg-yellow-600',
      Completed: 'bg-green-600',
      Cancelled: 'bg-slate-600',
      Overdue: 'bg-red-600'
    };
    return (
      <span className={`px-2 py-1 text-xs rounded ${colors[status] || 'bg-slate-600'} text-white`}>
        {status}
      </span>
    );
  };

  const maintenanceTypeOptions = [
    { value: '', label: 'All Types' },
    ...Object.entries(MaintenanceTypeLabels).map(([value, label]) => ({ value, label }))
  ];

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'Scheduled', label: 'Scheduled' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  const assetOptions = [
    { value: '', label: 'Select asset...' },
    ...assets.map(a => ({ value: a.id, label: `${(a as any).assetTag || a.assetNumber} - ${a.name}` }))
  ];

  if (loading) {
    return (
      <PageShell title="Maintenance Schedule" breadcrumbs={[{ label: 'Assets', path: '/assets' }, { label: 'Maintenance' }]}>
        <LoadingSpinner fullPage />
      </PageShell>
    );
  }

  const columns: TableColumn<ExtendedMaintenanceRecord>[] = [
    {
      key: 'assetName',
      label: 'Asset',
      render: (v: unknown, row: ExtendedMaintenanceRecord) => (
        <div>
          <Link to={`/assets/${row.assetId}`} className="text-brand-400 hover:text-brand-300 font-medium">
            {v as string || 'Unknown'}
          </Link>
          <p className="text-xs text-slate-400">{row.assetTag || ''}</p>
        </div>
      )
    },
    {
      key: 'maintenanceType',
      label: 'Type',
      render: (v: unknown) => (
        <span className="px-2 py-1 text-xs bg-purple-600/30 rounded text-purple-300">
          {MaintenanceTypeLabels[v as MaintenanceTypeEnum] || (v as string)}
        </span>
      )
    },
    { key: 'description', label: 'Description' },
    {
      key: 'scheduledDate',
      label: 'Scheduled',
      render: (v: unknown, row: ExtendedMaintenanceRecord) => (
        <div className="flex items-center gap-2">
          <span className="text-white">{formatDate(v as string)}</span>
          {!row.completedDate && getUrgencyBadge(v as string)}
        </div>
      )
    },
    { key: 'completedDate', label: 'Completed', render: (v: unknown) => v ? formatDate(v as string) : <span className="text-slate-500">-</span> },
    { key: 'cost', label: 'Cost', render: (v: unknown) => v ? formatCurrency(v as number) : '-' },
    {
      key: 'status',
      label: 'Status',
      render: (v: unknown) => getStatusBadge(v as string)
    },
    {
      key: 'actions',
      label: 'Actions',
      sortable: false,
      render: (_: unknown, row: ExtendedMaintenanceRecord) => (
        <div className="flex items-center gap-2">
          <Link to={`/assets/${row.assetId}`} className="text-blue-500 hover:text-blue-400" title="View Asset">
            <Eye className="h-4 w-4" />
          </Link>
          {row.status === 'Scheduled' && (
            <button 
              onClick={() => handleMarkComplete(row)} 
              className="text-green-500 hover:text-green-400" 
              title="Mark Complete"
            >
              <Check className="h-4 w-4" />
            </button>
          )}
        </div>
      )
    }
  ];

  return (
    <PageShell
      title="Maintenance Schedule"
      breadcrumbs={[{ label: 'Assets', path: '/assets' }, { label: 'Maintenance' }]}
      actions={
        <Button size="sm" onClick={() => { resetForm(); setShowCreateModal(true); }} className="gap-1">
          <Plus className="h-4 w-4" />
          Schedule Maintenance
        </Button>
      }
    >
      <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="p-4 bg-gradient-to-br from-red-900/20 to-slate-800">
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-8 w-8 text-red-500" />
            <div>
              <p className="text-2xl font-bold text-red-500">{records.filter(r => r.status === 'Overdue' || (getDaysUntil(r.scheduledDate) || 0) < 0).length}</p>
              <p className="text-slate-400 text-sm">Overdue</p>
            </div>
          </div>
        </Card>
        <Card className="p-4 bg-gradient-to-br from-yellow-900/20 to-slate-800">
          <div className="flex items-center gap-3">
            <Clock className="h-8 w-8 text-yellow-500" />
            <div>
              <p className="text-2xl font-bold text-yellow-500">{upcomingRecords.filter(r => (getDaysUntil(r.scheduledDate) || 0) <= 7).length}</p>
              <p className="text-slate-400 text-sm">Due This Week</p>
            </div>
          </div>
        </Card>
        <Card className="p-4 bg-gradient-to-br from-blue-900/20 to-slate-800">
          <div className="flex items-center gap-3">
            <Calendar className="h-8 w-8 text-blue-500" />
            <div>
              <p className="text-2xl font-bold text-blue-500">{upcomingRecords.length}</p>
              <p className="text-slate-400 text-sm">Upcoming (30 days)</p>
            </div>
          </div>
        </Card>
        <Card className="p-4 bg-gradient-to-br from-green-900/20 to-slate-800">
          <div className="flex items-center gap-3">
            <Check className="h-8 w-8 text-green-500" />
            <div>
              <p className="text-2xl font-bold text-green-500">{records.filter(r => r.status === 'Completed').length}</p>
              <p className="text-slate-400 text-sm">Completed</p>
            </div>
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-slate-700 pb-2">
        <button
          onClick={() => setActiveTab('upcoming')}
          className={`px-4 py-2 rounded-t-lg font-medium transition-colors ${
            activeTab === 'upcoming' ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'
          }`}
        >
          Upcoming
        </button>
        <button
          onClick={() => setActiveTab('all')}
          className={`px-4 py-2 rounded-t-lg font-medium transition-colors ${
            activeTab === 'all' ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'
          }`}
        >
          All Records
        </button>
      </div>

      {activeTab === 'all' && (
        <div className="flex gap-4 mb-4">
          <SelectInput
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            options={maintenanceTypeOptions}
            className="w-48"
          />
          <SelectInput
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            options={statusOptions}
            className="w-48"
          />
        </div>
      )}

      <Card>
        {(activeTab === 'upcoming' ? upcomingRecords : records).length === 0 ? (
          <EmptyState 
            message={activeTab === 'upcoming' 
              ? "No upcoming maintenance scheduled in the next 30 days" 
              : "No maintenance records found"
            } 
          />
        ) : (
          <DataTable 
            data={activeTab === 'upcoming' ? upcomingRecords : records} 
            columns={columns} 
          />
        )}
      </Card>

      {/* Schedule Maintenance Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => { setShowCreateModal(false); resetForm(); }}
        title="Schedule Maintenance"
        size="md"
      >
        <div className="space-y-4">
          <SelectInput
            label="Asset"
            value={formData.assetId}
            onChange={(e) => setFormData({ ...formData, assetId: e.target.value })}
            options={assetOptions}
            required
          />
          <SelectInput
            label="Maintenance Type"
            value={formData.maintenanceType}
            onChange={(e) => setFormData({ ...formData, maintenanceType: e.target.value as MaintenanceTypeEnum })}
            options={Object.entries(MaintenanceTypeLabels).map(([value, label]) => ({ value, label }))}
          />
          <TextInput
            label="Scheduled Date"
            type="date"
            value={formData.scheduledDate}
            onChange={(e) => setFormData({ ...formData, scheduledDate: e.target.value })}
            required
          />
          <TextInput
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            required
            multiline
            rows={2}
          />
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Estimated Cost (MYR)"
              type="number"
              value={formData.estimatedCost}
              onChange={(e) => setFormData({ ...formData, estimatedCost: e.target.value })}
            />
            <TextInput
              label="Vendor"
              value={formData.vendor}
              onChange={(e) => setFormData({ ...formData, vendor: e.target.value })}
            />
          </div>
          <TextInput
            label="Notes"
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            multiline
            rows={2}
          />
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => { setShowCreateModal(false); resetForm(); }}>Cancel</Button>
            <Button onClick={handleCreate}>Schedule</Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default MaintenanceSchedulePage;

