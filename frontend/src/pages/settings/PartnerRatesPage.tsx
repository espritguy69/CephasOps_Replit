import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, DollarSign, Info, Filter, RefreshCcw, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { getBillingRatecards, createBillingRatecard, updateBillingRatecard, deleteBillingRatecard, exportPartnerRates, importPartnerRates, downloadPartnerRatesTemplate } from '../../api/billingRatecards';
import { getPartners } from '../../api/partners';
import { getDepartments } from '../../api/departments';
import { getInstallationMethods } from '../../api/installationMethods';
import { getOrderTypes } from '../../api/orderTypes';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Tabs, TabPanel, Select, StatusBadge, ImportExportButtons } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { Partner } from '../../types/partners';
import type { Department } from '../../types/departments';
import type { InstallationMethod } from '../../types/installationMethods';
import type { OrderType } from '../../types/orderTypes';

const SERVICE_CATEGORIES = [
  { value: '', label: 'All Service Categories' },
  { value: 'FTTH', label: 'FTTH (Fibre to the Home)' },
  { value: 'FTTO', label: 'FTTO (Fibre to the Office)' },
  { value: 'FTTR', label: 'FTTR (Fibre to the Room)' },
  { value: 'FTTC', label: 'FTTC (Fibre to the Curb)' }
];

interface BillingRatecard {
  id: string;
  partnerId: string;
  partnerName?: string;
  departmentId?: string;
  departmentName?: string;
  orderTypeId?: string;
  orderTypeName?: string;
  serviceCategory?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  buildingType?: string;
  description?: string;
  amount: number;
  taxRate?: number;
  isActive: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

interface RatecardFormData {
  departmentId: string;
  partnerId: string;
  orderTypeId: string;
  serviceCategory: string;
  installationMethodId: string;
  buildingType: string;
  description: string;
  amount: string;
  taxRate: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo: string;
}

interface RatecardFilters {
  departmentId: string;
  partnerId: string;
  isActive: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PartnerRatesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [ratecards, setRatecards] = useState<BillingRatecard[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [orderTypes, setOrderTypes] = useState<OrderType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingRatecard, setEditingRatecard] = useState<BillingRatecard | null>(null);
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [showGuide, setShowGuide] = useState<boolean>(true);

  // Filters
  const [filters, setFilters] = useState<RatecardFilters>({
    departmentId: '',
    partnerId: '',
    isActive: 'true'
  });

  const [formData, setFormData] = useState<RatecardFormData>({
    departmentId: '',
    partnerId: '',
    orderTypeId: '',
    serviceCategory: '',
    installationMethodId: '',
    buildingType: '',
    description: '',
    amount: '',
    taxRate: '0',
    isActive: true,
    effectiveFrom: '',
    effectiveTo: ''
  });

  useEffect(() => {
    loadAllData();
  }, [filters]);

  const loadAllData = async (): Promise<void> => {
    try {
      setLoading(true);
      
      const apiFilters: any = { 
        isActive: filters.isActive === '' ? undefined : filters.isActive === 'true'
      };
      if (filters.partnerId) apiFilters.partnerId = filters.partnerId;
      if (filters.departmentId) apiFilters.departmentId = filters.departmentId;

      const [ratecardsResponse, partnersResponse, deptResponse, methodsResponse, orderTypesResponse] = await Promise.all([
        getBillingRatecards(apiFilters).catch(() => []),
        getPartners({ isActive: true }).catch(() => []),
        getDepartments().catch(() => []),
        getInstallationMethods({ isActive: true }).catch(() => []),
        getOrderTypes({ isActive: true }).catch(() => [])
      ]);

      setRatecards(Array.isArray(ratecardsResponse) ? ratecardsResponse : []);
      setPartners(Array.isArray(partnersResponse) ? partnersResponse : []);
      setDepartments(Array.isArray(deptResponse) ? deptResponse : []);
      setInstallationMethods(Array.isArray(methodsResponse) ? methodsResponse : []);
      setOrderTypes(Array.isArray(orderTypesResponse) ? orderTypesResponse : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load data');
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    if (!formData.partnerId) {
      showError('Please select a Partner');
      return;
    }
    if (!formData.amount) {
      showError('Please enter a rate amount');
      return;
    }

    try {
      const ratecardData: any = {
        departmentId: formData.departmentId || null,
        partnerId: formData.partnerId,
        orderTypeId: formData.orderTypeId || null,
        serviceCategory: formData.serviceCategory || null,
        installationMethodId: formData.installationMethodId || null,
        buildingType: formData.buildingType?.trim() || null,
        description: formData.description?.trim() || null,
        amount: parseFloat(formData.amount),
        taxRate: parseFloat(formData.taxRate) || 0,
        isActive: formData.isActive,
        effectiveFrom: formData.effectiveFrom || null,
        effectiveTo: formData.effectiveTo || null
      };

      await createBillingRatecard(ratecardData);
      showSuccess('Partner rate created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to create partner rate');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingRatecard) return;
    try {
      const ratecardData: any = {
        departmentId: formData.departmentId || null,
        partnerId: formData.partnerId || undefined,
        orderTypeId: formData.orderTypeId || null,
        serviceCategory: formData.serviceCategory || null,
        installationMethodId: formData.installationMethodId || null,
        buildingType: formData.buildingType?.trim() || null,
        description: formData.description?.trim() || null,
        amount: formData.amount ? parseFloat(formData.amount) : undefined,
        taxRate: formData.taxRate !== undefined ? parseFloat(formData.taxRate) : undefined,
        isActive: formData.isActive,
        effectiveFrom: formData.effectiveFrom || null,
        effectiveTo: formData.effectiveTo || null
      };

      // Remove undefined values
      Object.keys(ratecardData).forEach(key => {
        if (ratecardData[key] === undefined) {
          delete ratecardData[key];
        }
      });

      await updateBillingRatecard(editingRatecard.id, ratecardData);
      showSuccess('Partner rate updated successfully!');
      setShowCreateModal(false);
      setEditingRatecard(null);
      resetForm();
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to update partner rate');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this partner rate?')) return;
    
    try {
      await deleteBillingRatecard(id);
      showSuccess('Partner rate deleted successfully!');
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete partner rate');
    }
  };

  const handleToggleStatus = async (ratecard: BillingRatecard): Promise<void> => {
    try {
      const updatedStatus = !ratecard.isActive;
      await updateBillingRatecard(ratecard.id, { isActive: updatedStatus });
      showSuccess(`Partner rate ${updatedStatus ? 'activated' : 'deactivated'} successfully!`);
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to toggle rate status');
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      partnerId: '',
      orderTypeId: '',
      serviceCategory: '',
      installationMethodId: '',
      buildingType: '',
      description: '',
      amount: '',
      taxRate: '0',
      isActive: true,
      effectiveFrom: '',
      effectiveTo: ''
    });
  };

  const openEditModal = (ratecard: BillingRatecard): void => {
    setEditingRatecard(ratecard);
    setFormData({
      departmentId: ratecard.departmentId || '',
      partnerId: ratecard.partnerId,
      orderTypeId: ratecard.orderTypeId || '',
      serviceCategory: ratecard.serviceCategory || '',
      installationMethodId: ratecard.installationMethodId || '',
      buildingType: ratecard.buildingType || '',
      description: ratecard.description || '',
      amount: ratecard.amount?.toString() || '',
      taxRate: ratecard.taxRate?.toString() || '0',
      isActive: ratecard.isActive,
      effectiveFrom: ratecard.effectiveFrom ? ratecard.effectiveFrom.split('T')[0] : '',
      effectiveTo: ratecard.effectiveTo ? ratecard.effectiveTo.split('T')[0] : ''
    });
    setShowCreateModal(true);
  };

  const columns: TableColumn<BillingRatecard>[] = [
    { 
      key: 'partnerName', 
      label: 'Partner',
      render: (value: unknown, row: BillingRatecard) => (
        <div>
          <div className="font-medium">{value as string || 'N/A'}</div>
          {row.departmentName && <div className="text-xs text-muted-foreground">{row.departmentName}</div>}
        </div>
      )
    },
    { 
      key: 'orderTypeName', 
      label: 'Order Type',
      render: (value: unknown) => value || <span className="text-muted-foreground text-xs">All</span>
    },
    { 
      key: 'serviceCategory', 
      label: 'Service Category',
      render: (value: unknown) => value || <span className="text-muted-foreground text-xs">All</span>
    },
    { 
      key: 'installationMethodName', 
      label: 'Site Condition',
      render: (value: unknown) => value || <span className="text-muted-foreground text-xs">All</span>
    },
    { 
      key: 'amount', 
      label: 'Rate', 
      render: (value: unknown) => value ? `RM ${(value as number).toFixed(2)}` : '-' 
    },
    { 
      key: 'taxRate', 
      label: 'Tax', 
      render: (value: unknown) => value ? `${((value as number) * 100).toFixed(0)}%` : '0%' 
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value: unknown) => (
        <StatusBadge variant={value ? 'success' : 'default'}>
          {value ? 'Active' : 'Inactive'}
        </StatusBadge>
      )
    }
  ];

  if (loading) {
    return (
      <PageShell title="Partner Rates" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Partner Rates' }]}>
        <LoadingSpinner message="Loading partner rates..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Partner Rates (PU Rates)"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Partner Rates' }]}
      actions={
        <div className="flex items-center gap-2">
          <ImportExportButtons
            entityName="Partner Rates"
            onExport={async () => {
              try {
                await exportPartnerRates(filters);
                showSuccess('Partner rates exported successfully');
              } catch (err: any) {
                showError(err.message || 'Failed to export partner rates');
              }
            }}
            onImport={async (file: File) => {
              const result = await importPartnerRates(file);
              await loadAllData();
              return result;
            }}
            onDownloadTemplate={downloadPartnerRatesTemplate}
          />
          <Button variant="outline" size="sm" onClick={loadAllData}>
            <RefreshCcw className="h-3 w-3" />
          </Button>
          <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
            <Plus className="h-3 w-3" />
            Add Rate
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto h-full flex flex-col space-y-3">
      <p className="text-xs text-muted-foreground">
        Define billing rates from partners by department, service category, and site condition
      </p>
      {/* How-To Guide */}
      <Card className="mb-3 bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Partner Rates (PU Rates) Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Partner
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• TM, Maxis, etc.</li>
                  <li>• Who pays us</li>
                  <li>• Invoice billing</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Service Category
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• FTTH / FTTO</li>
                  <li>• FTTR / FTTC</li>
                  <li>• Product tier pricing</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Site & Order Type
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Prelaid / Non-Prelaid</li>
                  <li>• Activation / Mod</li>
                  <li>• Complexity pay</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Tax & Validity
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• 0.06 = 6% SST</li>
                  <li>• Effective dates</li>
                  <li>• Rate versioning</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Filters */}
      <Card className="mb-3">
        <div className="p-3">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium">Filters</span>
            <button 
              onClick={() => setShowFilters(!showFilters)}
              className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
            >
              <Filter className="h-3 w-3" />
              {showFilters ? 'Hide' : 'Show'}
            </button>
          </div>
          {showFilters && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2 border-t border-border">
              <Select
                label="Department"
                value={filters.departmentId}
                onChange={(e) => setFilters({ ...filters, departmentId: e.target.value })}
                options={[
                  { value: '', label: 'All Departments' },
                  ...departments.map(dept => ({ value: dept.id, label: dept.name }))
                ]}
              />
              <Select
                label="Partner"
                value={filters.partnerId}
                onChange={(e) => setFilters({ ...filters, partnerId: e.target.value })}
                options={[
                  { value: '', label: 'All Partners' },
                  ...partners.map(p => ({ value: p.id, label: p.name }))
                ]}
              />
              <Select
                label="Status"
                value={filters.isActive}
                onChange={(e) => setFilters({ ...filters, isActive: e.target.value })}
                options={[
                  { value: '', label: 'All' },
                  { value: 'true', label: 'Active Only' },
                  { value: 'false', label: 'Inactive Only' }
                ]}
              />
            </div>
          )}
        </div>
      </Card>

      {/* Data Table */}
      <Card className="flex-1 flex flex-col min-h-0">
        {ratecards.length > 0 ? (
          <div className="flex-1 overflow-hidden">
            <DataTable
              data={ratecards}
              columns={columns}
              actions={(row: BillingRatecard) => (
                <div className="flex items-center gap-2">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleToggleStatus(row);
                    }}
                    title={row.isActive ? 'Deactivate' : 'Activate'}
                    className={cn(
                      "hover:opacity-75 cursor-pointer transition-colors",
                      row.isActive ? 'text-yellow-600' : 'text-green-600'
                    )}
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
              )}
            />
          </div>
        ) : (
          <EmptyState
            title="No partner rates found"
            message="Add your first partner rate to get started, or adjust the filters."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingRatecard !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingRatecard(null);
          resetForm();
        }}
        title={editingRatecard ? 'Edit Partner Rate' : 'Create Partner Rate'}
        size="lg"
      >
        <div className="bg-card rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] flex flex-col">
          <div className="flex-1 overflow-y-auto p-4">
            <Tabs defaultActiveTab={0}>
              <TabPanel label="Rate Context">
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <Select
                      label="Department"
                      value={formData.departmentId}
                      onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                      options={[
                        { value: '', label: 'All Departments (Company-wide)' },
                        ...departments.map(dept => ({ value: dept.id, label: dept.name }))
                      ]}
                    />
                    <Select
                      label="Partner *"
                      value={formData.partnerId}
                      onChange={(e) => setFormData({ ...formData, partnerId: e.target.value })}
                      options={[
                        { value: '', label: 'Select Partner' },
                        ...partners.map(p => ({ value: p.id, label: p.name }))
                      ]}
                      required
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <Select
                      label="Order Type"
                      value={formData.orderTypeId}
                      onChange={(e) => setFormData({ ...formData, orderTypeId: e.target.value })}
                      options={[
                        { value: '', label: 'All Order Types' },
                        ...orderTypes.map(ot => ({ value: ot.id, label: ot.name }))
                      ]}
                    />
                    <Select
                      label="Service Category"
                      value={formData.serviceCategory}
                      onChange={(e) => setFormData({ ...formData, serviceCategory: e.target.value })}
                      options={SERVICE_CATEGORIES}
                    />
                  </div>

                  <Select
                    label="Site Condition (Installation Method)"
                    value={formData.installationMethodId}
                    onChange={(e) => setFormData({ ...formData, installationMethodId: e.target.value })}
                    options={[
                      { value: '', label: 'All Site Conditions' },
                      ...installationMethods.map(m => ({ value: m.id, label: m.name }))
                    ]}
                  />

                  <div className="space-y-0.5">
                    <label className="text-xs font-medium">Description / Notes</label>
                    <textarea
                      value={formData.description}
                      onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                      rows={2}
                      className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                      placeholder="Optional notes for this rate"
                    />
                  </div>
                </div>
              </TabPanel>

              <TabPanel label="Rate & Validity">
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Rate Amount (RM) *"
                      type="number"
                      step="0.01"
                      value={formData.amount}
                      onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
                      placeholder="e.g., 85.00"
                      required
                    />
                    <TextInput
                      label="Tax Rate (0 = No Tax, 0.06 = 6% SST)"
                      type="number"
                      step="0.01"
                      min="0"
                      max="1"
                      value={formData.taxRate}
                      onChange={(e) => setFormData({ ...formData, taxRate: e.target.value })}
                      placeholder="e.g., 0.06"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Effective From"
                      type="date"
                      value={formData.effectiveFrom}
                      onChange={(e) => setFormData({ ...formData, effectiveFrom: e.target.value })}
                    />
                    <TextInput
                      label="Effective To"
                      type="date"
                      value={formData.effectiveTo}
                      onChange={(e) => setFormData({ ...formData, effectiveTo: e.target.value })}
                    />
                  </div>

                  <div className="flex items-center gap-2 pt-2">
                    <input
                      type="checkbox"
                      id="isActive"
                      checked={formData.isActive}
                      onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                      className="h-4 w-4 rounded border-input"
                    />
                    <label htmlFor="isActive" className="text-xs font-medium">
                      Active
                    </label>
                  </div>
                </div>
              </TabPanel>
            </Tabs>
          </div>

          <div className="flex justify-end gap-2 p-4 border-t flex-shrink-0">
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setShowCreateModal(false);
                setEditingRatecard(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={editingRatecard ? handleUpdate : handleCreate}
              className="flex items-center gap-1.5"
            >
              <Save className="h-3 w-3" />
              {editingRatecard ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PartnerRatesPage;

