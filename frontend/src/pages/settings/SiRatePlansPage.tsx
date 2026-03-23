import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, DollarSign, Info, Filter, RefreshCcw, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { getSiRatePlans, createSiRatePlan, updateSiRatePlan, deleteSiRatePlan, exportSiRatePlans, importSiRatePlans, downloadSiRatePlansTemplate } from '../../api/payroll';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getDepartments } from '../../api/departments';
import { getInstallationMethods } from '../../api/installationMethods';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, Tabs, TabPanel, Select, StatusBadge, ImportExportButtons, ConfirmDialog } from '../../components/ui';
import { cn } from '@/lib/utils';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { Department } from '../../types/departments';
import type { InstallationMethod } from '../../types/installationMethods';

interface SiRatePlan {
  id: string;
  serviceInstallerId: string;
  serviceInstallerName?: string;
  departmentId?: string;
  departmentName?: string;
  installationMethodId?: string;
  installationMethodName?: string;
  rateType: string;
  level: string;
  prelaidRate?: number;
  nonPrelaidRate?: number;
  sduRate?: number;
  rdfPoleRate?: number;
  activationRate?: number;
  modificationRate?: number;
  assuranceRate?: number;
  assuranceRepullRate?: number;
  fttrRate?: number;
  fttcRate?: number;
  onTimeBonus?: number;
  latePenalty?: number;
  reworkRate?: number;
  effectiveFrom?: string;
  effectiveTo?: string;
  isActive: boolean;
}

interface RatePlanFormData {
  departmentId: string;
  serviceInstallerId: string;
  installationMethodId: string;
  rateType: string;
  level: string;
  prelaidRate: string;
  nonPrelaidRate: string;
  sduRate: string;
  rdfPoleRate: string;
  activationRate: string;
  modificationRate: string;
  assuranceRate: string;
  assuranceRepullRate: string;
  fttrRate: string;
  fttcRate: string;
  onTimeBonus: string;
  latePenalty: string;
  reworkRate: string;
  effectiveFrom: string;
  effectiveTo: string;
  isActive: boolean;
}

interface RatePlanFilters {
  departmentId: string;
  isActive: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const SiRatePlansPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [ratePlans, setRatePlans] = useState<SiRatePlan[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingPlan, setEditingPlan] = useState<SiRatePlan | null>(null);
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [deletingPlan, setDeletingPlan] = useState<SiRatePlan | null>(null);
  
  // Filters
  const [filters, setFilters] = useState<RatePlanFilters>({
    departmentId: '',
    isActive: 'true'
  });

  const [formData, setFormData] = useState<RatePlanFormData>({
    departmentId: '',
    serviceInstallerId: '',
    installationMethodId: '',
    rateType: 'Junior',
    level: 'Junior',
    prelaidRate: '',
    nonPrelaidRate: '',
    sduRate: '',
    rdfPoleRate: '',
    activationRate: '',
    modificationRate: '',
    assuranceRate: '',
    assuranceRepullRate: '',
    fttrRate: '',
    fttcRate: '',
    onTimeBonus: '',
    latePenalty: '',
    reworkRate: '',
    effectiveFrom: '',
    effectiveTo: '',
    isActive: true
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
      if (filters.departmentId) {
        apiFilters.departmentId = filters.departmentId;
      }

      const [ratePlansResponse, siResponse, deptResponse, methodsResponse] = await Promise.all([
        getSiRatePlans(apiFilters).catch(() => []),
        getServiceInstallers({ isActive: true }).catch(() => []),
        getDepartments().catch(() => []),
        getInstallationMethods({ isActive: true }).catch(() => [])
      ]);

      setRatePlans(Array.isArray(ratePlansResponse) ? ratePlansResponse : []);
      setServiceInstallers(Array.isArray(siResponse) ? siResponse : []);
      setDepartments(Array.isArray(deptResponse) ? deptResponse : []);
      setInstallationMethods(Array.isArray(methodsResponse) ? methodsResponse : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load data');
      console.error('Error loading data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    if (!formData.serviceInstallerId) {
      showError('Please select a Service Installer');
      return;
    }

    try {
      const ratePlanData: any = {
        departmentId: formData.departmentId || null,
        serviceInstallerId: formData.serviceInstallerId,
        installationMethodId: formData.installationMethodId || null,
        rateType: formData.rateType,
        level: formData.level,
        prelaidRate: formData.prelaidRate ? parseFloat(formData.prelaidRate) : null,
        nonPrelaidRate: formData.nonPrelaidRate ? parseFloat(formData.nonPrelaidRate) : null,
        sduRate: formData.sduRate ? parseFloat(formData.sduRate) : null,
        rdfPoleRate: formData.rdfPoleRate ? parseFloat(formData.rdfPoleRate) : null,
        activationRate: formData.activationRate ? parseFloat(formData.activationRate) : null,
        modificationRate: formData.modificationRate ? parseFloat(formData.modificationRate) : null,
        assuranceRate: formData.assuranceRate ? parseFloat(formData.assuranceRate) : null,
        assuranceRepullRate: formData.assuranceRepullRate ? parseFloat(formData.assuranceRepullRate) : null,
        fttrRate: formData.fttrRate ? parseFloat(formData.fttrRate) : null,
        fttcRate: formData.fttcRate ? parseFloat(formData.fttcRate) : null,
        onTimeBonus: formData.onTimeBonus ? parseFloat(formData.onTimeBonus) : null,
        latePenalty: formData.latePenalty ? parseFloat(formData.latePenalty) : null,
        reworkRate: formData.reworkRate ? parseFloat(formData.reworkRate) : null,
        effectiveFrom: formData.effectiveFrom || null,
        effectiveTo: formData.effectiveTo || null
      };

      await createSiRatePlan(ratePlanData);
      showSuccess('SI Rate Plan created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to create SI rate plan');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingPlan) return;
    try {
      const ratePlanData: any = {
        departmentId: formData.departmentId || null,
        serviceInstallerId: formData.serviceInstallerId,
        installationMethodId: formData.installationMethodId || null,
        rateType: formData.rateType,
        level: formData.level,
        prelaidRate: formData.prelaidRate ? parseFloat(formData.prelaidRate) : null,
        nonPrelaidRate: formData.nonPrelaidRate ? parseFloat(formData.nonPrelaidRate) : null,
        sduRate: formData.sduRate ? parseFloat(formData.sduRate) : null,
        rdfPoleRate: formData.rdfPoleRate ? parseFloat(formData.rdfPoleRate) : null,
        activationRate: formData.activationRate ? parseFloat(formData.activationRate) : null,
        modificationRate: formData.modificationRate ? parseFloat(formData.modificationRate) : null,
        assuranceRate: formData.assuranceRate ? parseFloat(formData.assuranceRate) : null,
        assuranceRepullRate: formData.assuranceRepullRate ? parseFloat(formData.assuranceRepullRate) : null,
        fttrRate: formData.fttrRate ? parseFloat(formData.fttrRate) : null,
        fttcRate: formData.fttcRate ? parseFloat(formData.fttcRate) : null,
        onTimeBonus: formData.onTimeBonus ? parseFloat(formData.onTimeBonus) : null,
        latePenalty: formData.latePenalty ? parseFloat(formData.latePenalty) : null,
        reworkRate: formData.reworkRate ? parseFloat(formData.reworkRate) : null,
        effectiveFrom: formData.effectiveFrom || null,
        effectiveTo: formData.effectiveTo || null
      };

      await updateSiRatePlan(editingPlan.id, ratePlanData);
      showSuccess('SI Rate Plan updated successfully!');
      setShowCreateModal(false);
      setEditingPlan(null);
      resetForm();
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to update SI rate plan');
    }
  };

  const handleToggleStatus = async (plan: SiRatePlan): Promise<void> => {
    try {
      const updatedStatus = !plan.isActive;
      await updateSiRatePlan(plan.id, { isActive: updatedStatus });
      showSuccess(`SI Rate Plan ${updatedStatus ? 'activated' : 'deactivated'} successfully!`);
      loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to toggle rate plan status');
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!deletingPlan) return;
    try {
      await deleteSiRatePlan(deletingPlan.id);
      showSuccess('SI Rate Plan deleted successfully!');
      setDeletingPlan(null);
      await loadAllData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete SI rate plan');
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      serviceInstallerId: '',
      installationMethodId: '',
      rateType: 'Junior',
      level: 'Junior',
      prelaidRate: '',
      nonPrelaidRate: '',
      sduRate: '',
      rdfPoleRate: '',
      activationRate: '',
      modificationRate: '',
      assuranceRate: '',
      assuranceRepullRate: '',
      fttrRate: '',
      fttcRate: '',
      onTimeBonus: '',
      latePenalty: '',
      reworkRate: '',
      effectiveFrom: '',
      effectiveTo: '',
      isActive: true
    });
  };

  const openEditModal = (plan: SiRatePlan): void => {
    setEditingPlan(plan);
    setFormData({
      departmentId: plan.departmentId || '',
      serviceInstallerId: plan.serviceInstallerId,
      installationMethodId: plan.installationMethodId || '',
      rateType: plan.rateType || 'Junior',
      level: plan.level || 'Junior',
      prelaidRate: plan.prelaidRate?.toString() || '',
      nonPrelaidRate: plan.nonPrelaidRate?.toString() || '',
      sduRate: plan.sduRate?.toString() || '',
      rdfPoleRate: plan.rdfPoleRate?.toString() || '',
      activationRate: plan.activationRate?.toString() || '',
      modificationRate: plan.modificationRate?.toString() || '',
      assuranceRate: plan.assuranceRate?.toString() || '',
      assuranceRepullRate: plan.assuranceRepullRate?.toString() || '',
      fttrRate: plan.fttrRate?.toString() || '',
      fttcRate: plan.fttcRate?.toString() || '',
      onTimeBonus: plan.onTimeBonus?.toString() || '',
      latePenalty: plan.latePenalty?.toString() || '',
      reworkRate: plan.reworkRate?.toString() || '',
      effectiveFrom: plan.effectiveFrom ? plan.effectiveFrom.split('T')[0] : '',
      effectiveTo: plan.effectiveTo ? plan.effectiveTo.split('T')[0] : '',
      isActive: plan.isActive
    });
    setShowCreateModal(true);
  };

  const columns: TableColumn<SiRatePlan>[] = [
    { 
      key: 'serviceInstallerName', 
      label: 'Service Installer',
      render: (value: unknown, row: SiRatePlan) => (
        <div>
          <div className="font-medium">{value as string || 'N/A'}</div>
          <div className="text-xs text-muted-foreground">{row.rateType} Rate</div>
        </div>
      )
    },
    { 
      key: 'departmentName', 
      label: 'Department',
      render: (value: unknown) => value || <span className="text-muted-foreground text-xs">All</span>
    },
    { 
      key: 'installationMethodName', 
      label: 'Site Condition',
      render: (value: unknown) => value || <span className="text-muted-foreground text-xs">All</span>
    },
    { key: 'level', label: 'Level' },
    { 
      key: 'activationRate', 
      label: 'Activation', 
      render: (value: unknown) => value ? `RM ${(value as number).toFixed(2)}` : '-' 
    },
    { 
      key: 'prelaidRate', 
      label: 'Prelaid', 
      render: (value: unknown) => value ? `RM ${(value as number).toFixed(2)}` : '-' 
    },
    { 
      key: 'nonPrelaidRate', 
      label: 'Non-Prelaid', 
      render: (value: unknown) => value ? `RM ${(value as number).toFixed(2)}` : '-' 
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value: unknown) => (
        <StatusBadge variant={value ? 'success' : 'default'}>
          {value ? 'Active' : 'Inactive'}
        </StatusBadge>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value: unknown, row: SiRatePlan) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleToggleStatus(row);
            }}
            title={row.isActive ? 'Deactivate' : 'Activate'}
            className={cn(
              "p-1 rounded hover:bg-muted transition-colors",
              row.isActive ? 'text-yellow-600 hover:text-yellow-700' : 'text-green-600 hover:text-green-700'
            )}
          >
            <Power className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditModal(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setDeletingPlan(row);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )
    }
  ];

  if (loading) {
    return (
      <PageShell title="SI Rate Plans" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'SI Rate Plans' }]}>
        <LoadingSpinner message="Loading SI rate plans..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="SI Rate Plans"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'SI Rate Plans' }]}
      actions={
        <div className="flex items-center gap-2">
          <ImportExportButtons
            entityName="SI Rate Plans"
            onExport={async () => {
              try {
                await exportSiRatePlans(filters);
                showSuccess('SI rate plans exported successfully');
              } catch (err: any) {
                showError(err.message || 'Failed to export SI rate plans');
              }
            }}
            onImport={async (file: File) => {
              const result = await importSiRatePlans(file);
              await loadAllData();
              return result;
            }}
            onDownloadTemplate={downloadSiRatePlansTemplate}
          />
          <Button variant="outline" size="sm" onClick={loadAllData}>
            <RefreshCcw className="h-3 w-3" />
          </Button>
          <Button onClick={() => setShowCreateModal(true)} size="sm" className="flex items-center gap-1.5">
            <Plus className="h-3 w-3" />
            Add Rate Plan
          </Button>
        </div>
      }
    >
      <div className="flex-1 p-3 max-w-7xl mx-auto h-full flex flex-col">
      {/* How-To Guide */}
      <Card className="mb-3 bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How SI Rate Plans Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Rate Type
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>Junior</strong> - Base rate</li>
                  <li>• <strong>Senior</strong> - Higher rate</li>
                  <li>• <strong>Custom</strong> - Per-SI</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Site Condition
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Prelaid / Non-Prelaid</li>
                  <li>• SDU / RDF Pole</li>
                  <li>• Determines base pay</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Order Type Rates
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Activation / Mod</li>
                  <li>• Assurance / Repull</li>
                  <li>• Order-type-specific add-on</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  KPI Adjustments
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• On-time bonus</li>
                  <li>• Late penalty</li>
                  <li>• Rework deduction</li>
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
        {ratePlans.length > 0 ? (
          <div className="flex-1 overflow-hidden">
            <DataTable
              data={ratePlans}
              columns={columns}
            />
          </div>
        ) : (
          <EmptyState
            title="No SI rate plans found"
            message="Add your first SI rate plan to get started, or adjust the filters."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingPlan !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingPlan(null);
          resetForm();
        }}
        title={editingPlan ? 'Edit SI Rate Plan' : 'Create SI Rate Plan'}
        size="lg"
      >
        <div className="bg-card rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] flex flex-col">
          <div className="flex-1 overflow-y-auto p-4">
            <Tabs defaultActiveTab={0}>
              <TabPanel label="Basic Info">
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
                      label="Service Installer *"
                      value={formData.serviceInstallerId}
                      onChange={(e) => setFormData({ ...formData, serviceInstallerId: e.target.value })}
                      options={[
                        { value: '', label: 'Select Service Installer' },
                        ...serviceInstallers.map(si => ({ value: si.id, label: si.name }))
                      ]}
                      required
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <Select
                      label="Site Condition (Installation Method)"
                      value={formData.installationMethodId}
                      onChange={(e) => setFormData({ ...formData, installationMethodId: e.target.value })}
                      options={[
                        { value: '', label: 'All Site Conditions' },
                        ...installationMethods.map(m => ({ value: m.id, label: m.name }))
                      ]}
                    />
                    <Select
                      label="Rate Type *"
                      value={formData.rateType}
                      onChange={(e) => setFormData({ ...formData, rateType: e.target.value, level: e.target.value })}
                      options={[
                        { value: 'Junior', label: 'Junior (Template Rate)' },
                        { value: 'Senior', label: 'Senior (Template Rate)' },
                        { value: 'Custom', label: 'Custom (Per-Installer Override)' }
                      ]}
                      required
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

              <TabPanel label="Site Condition Rates">
                <div className="space-y-3">
                  <p className="text-xs text-muted-foreground mb-3">
                    Base payment rates based on the site condition where installation is performed.
                  </p>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Prelaid Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.prelaidRate}
                      onChange={(e) => setFormData({ ...formData, prelaidRate: e.target.value })}
                      placeholder="e.g., 35.00"
                    />
                    <TextInput
                      label="Non-Prelaid (MDU) Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.nonPrelaidRate}
                      onChange={(e) => setFormData({ ...formData, nonPrelaidRate: e.target.value })}
                      placeholder="e.g., 55.00"
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="SDU (Landed) Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.sduRate}
                      onChange={(e) => setFormData({ ...formData, sduRate: e.target.value })}
                      placeholder="e.g., 45.00"
                    />
                    <TextInput
                      label="RDF Pole Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.rdfPoleRate}
                      onChange={(e) => setFormData({ ...formData, rdfPoleRate: e.target.value })}
                      placeholder="e.g., 50.00"
                    />
                  </div>
                </div>
              </TabPanel>

              <TabPanel label="Order Type Rates">
                <div className="space-y-3">
                  <p className="text-xs text-muted-foreground mb-3">
                    Payment rates based on order type.
                  </p>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Activation Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.activationRate}
                      onChange={(e) => setFormData({ ...formData, activationRate: e.target.value })}
                    />
                    <TextInput
                      label="Modification Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.modificationRate}
                      onChange={(e) => setFormData({ ...formData, modificationRate: e.target.value })}
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Assurance Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.assuranceRate}
                      onChange={(e) => setFormData({ ...formData, assuranceRate: e.target.value })}
                    />
                    <TextInput
                      label="Assurance Repull Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.assuranceRepullRate}
                      onChange={(e) => setFormData({ ...formData, assuranceRepullRate: e.target.value })}
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="FTTR Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.fttrRate}
                      onChange={(e) => setFormData({ ...formData, fttrRate: e.target.value })}
                    />
                    <TextInput
                      label="FTTC Rate (RM)"
                      type="number"
                      step="0.01"
                      value={formData.fttcRate}
                      onChange={(e) => setFormData({ ...formData, fttcRate: e.target.value })}
                    />
                  </div>
                  <TextInput
                    label="Rework Rate (RM)"
                    type="number"
                    step="0.01"
                    value={formData.reworkRate}
                    onChange={(e) => setFormData({ ...formData, reworkRate: e.target.value })}
                  />
                </div>
              </TabPanel>

              <TabPanel label="KPI Adjustments">
                <div className="space-y-3">
                  <p className="text-xs text-muted-foreground mb-3">
                    Bonus and penalty adjustments based on performance metrics.
                  </p>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="On-Time Bonus (RM)"
                      type="number"
                      step="0.01"
                      value={formData.onTimeBonus}
                      onChange={(e) => setFormData({ ...formData, onTimeBonus: e.target.value })}
                      placeholder="e.g., 5.00"
                    />
                    <TextInput
                      label="Late Penalty (RM)"
                      type="number"
                      step="0.01"
                      value={formData.latePenalty}
                      onChange={(e) => setFormData({ ...formData, latePenalty: e.target.value })}
                      placeholder="e.g., -10.00"
                    />
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
                setEditingPlan(null);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={editingPlan ? handleUpdate : handleCreate}
              className="flex items-center gap-1.5"
            >
              <Save className="h-3 w-3" />
              {editingPlan ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        isOpen={deletingPlan !== null}
        onClose={() => setDeletingPlan(null)}
        onConfirm={handleDelete}
        title="Delete SI Rate Plan"
        message={`Are you sure you want to delete the rate plan for "${deletingPlan?.serviceInstallerName || 'this installer'}"? This action cannot be undone.`}
        confirmLabel="Delete"
        confirmVariant="danger"
      />
      </div>
    </PageShell>
  );
};

export default SiRatePlansPage;

