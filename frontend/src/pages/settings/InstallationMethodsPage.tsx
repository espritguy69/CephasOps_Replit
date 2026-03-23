import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { 
  createInstallationMethod, 
  updateInstallationMethod, 
  deleteInstallationMethod,
  InstallationCategory,
  InstallationCategoryLabels 
} from '../../api/installationMethods';
import { getDepartments } from '../../api/departments';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { InstallationMethod, CreateInstallationMethodRequest, UpdateInstallationMethodRequest } from '../../types/installationMethods';
import type { Department } from '../../types/departments';
import { useInstallationMethods, useCreateInstallationMethod, useUpdateInstallationMethod, useDeleteInstallationMethod } from '../../hooks/useInstallationMethods';
import { useDepartment } from '../../contexts/DepartmentContext';

interface ExtendedInstallationMethod extends InstallationMethod {
  departmentId?: string;
  displayOrder?: number;
}

interface InstallationMethodFormData {
  departmentId: string;
  name: string;
  code: string;
  category: string;
  description: string;
  displayOrder: number | string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const InstallationMethodsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { departmentId, loading: departmentLoading } = useDepartment();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingMethod, setEditingMethod] = useState<ExtendedInstallationMethod | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<InstallationMethodFormData>({
    departmentId: '',
    name: '',
    code: '',
    category: '',
    description: '',
    displayOrder: 0,
    isActive: true
  });

  // Use React Query hooks for data fetching
  const { 
    data: installationMethodsData = [], 
    isLoading: methodsLoading, 
    refetch: refetchMethods 
  } = useInstallationMethods({ 
    departmentId: departmentId || undefined 
  });

  // Convert to ExtendedInstallationMethod format
  const installationMethods = installationMethodsData as ExtendedInstallationMethod[];

  // Load departments (this doesn't need React Query as it's rarely used)
  useEffect(() => {
    const loadDepartments = async () => {
      try {
        const departmentsData = await getDepartments();
        setDepartments(Array.isArray(departmentsData) ? departmentsData : []);
      } catch (err) {
        console.error('Error loading departments:', err);
      }
    };
    loadDepartments();
  }, []);

  // Mutation hooks
  const createMutation = useCreateInstallationMethod();
  const updateMutation = useUpdateInstallationMethod();
  const deleteMutation = useDeleteInstallationMethod();

  const loading = methodsLoading || departmentLoading;

  const handleCreate = async (): Promise<void> => {
    try {
      const nameTrimmed = formData.name.trim();
      const codeTrimmed = formData.code.trim();
      
      // Use departmentId from context if not specified in form
      const finalDepartmentId = formData.departmentId || departmentId || '';

      if (!nameTrimmed) {
        showError('Name is required');
        return;
      }
      if (!codeTrimmed) {
        showError('Code is required');
        return;
      }

      // Check for duplicates
      const duplicateName = installationMethods.find(
        im => im.name.toLowerCase() === nameTrimmed.toLowerCase()
      );
      if (duplicateName) {
        showError(`An installation method with the name "${nameTrimmed}" already exists.`);
        return;
      }

      const duplicateCode = installationMethods.find(
        im => im.code && im.code.toLowerCase() === codeTrimmed.toLowerCase()
      );
      if (duplicateCode) {
        showError(`An installation method with the code "${codeTrimmed}" already exists.`);
        return;
      }

      const methodData: CreateInstallationMethodRequest & { departmentId?: string | null; displayOrder?: number } = {
        name: nameTrimmed,
        code: codeTrimmed,
        category: formData.category ? (formData.category as InstallationCategory) : InstallationCategory.FTTH,
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      // Use departmentId from context if not specified in form
      if (finalDepartmentId) {
        methodData.departmentId = finalDepartmentId;
      }
      
      await createMutation.mutateAsync(methodData as any);
      setShowCreateModal(false);
      resetForm();
    } catch (err) {
      showError((err as Error).message || 'Failed to create installation method');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingMethod) return;
    
    try {
      const nameTrimmed = formData.name.trim();
      const codeTrimmed = formData.code.trim();

      if (!nameTrimmed) {
        showError('Name is required');
        return;
      }

      // Check for duplicate name (exclude current record)
      const duplicateName = installationMethods.find(
        im => im.id !== editingMethod.id && im.name.toLowerCase() === nameTrimmed.toLowerCase()
      );
      if (duplicateName) {
        showError(`An installation method with the name "${nameTrimmed}" already exists.`);
        return;
      }

      // Check for duplicate code (exclude current record)
      if (codeTrimmed) {
        const duplicateCode = installationMethods.find(
          im => im.id !== editingMethod.id && im.code && im.code.toLowerCase() === codeTrimmed.toLowerCase()
        );
        if (duplicateCode) {
          showError(`An installation method with the code "${codeTrimmed}" already exists.`);
          return;
        }
      }

      const methodData: UpdateInstallationMethodRequest & { departmentId?: string | null; displayOrder?: number } = {
        name: nameTrimmed,
        code: codeTrimmed,
        category: formData.category ? (formData.category as InstallationCategory) : undefined,
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      // Use departmentId from context if not specified in form
      const finalDepartmentId = formData.departmentId || departmentId || '';
      if (finalDepartmentId) {
        methodData.departmentId = finalDepartmentId;
      }
      
      await updateMutation.mutateAsync({ id: editingMethod.id, data: methodData as any });
      setShowCreateModal(false);
      setEditingMethod(null);
      resetForm();
    } catch (err) {
      showError((err as Error).message || 'Failed to update installation method');
    }
  };

  const handleToggleStatus = async (method: ExtendedInstallationMethod): Promise<void> => {
    try {
      const methodData: UpdateInstallationMethodRequest & { departmentId?: string | null; displayOrder?: number } = {
        name: method.name,
        code: method.code,
        category: method.category,
        description: method.description || undefined,
        isActive: !method.isActive,
        displayOrder: method.displayOrder || 0
      };
      
      // Preserve departmentId from method or use context
      const finalDepartmentId = method.departmentId || departmentId || '';
      if (finalDepartmentId) {
        methodData.departmentId = finalDepartmentId;
      }
      
      await updateMutation.mutateAsync({ id: method.id, data: methodData as any });
    } catch (err) {
      // Error is already handled by the mutation hook
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this installation method?')) return;
    
    try {
      await deleteMutation.mutateAsync(id);
    } catch (err) {
      // Error is already handled by the mutation hook
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      name: '',
      code: '',
      category: '',
      description: '',
      displayOrder: 0,
      isActive: true
    });
  };

  const openEditModal = (method: ExtendedInstallationMethod): void => {
    setEditingMethod(method);
    setFormData({
      departmentId: method.departmentId || '',
      name: method.name,
      code: method.code || '',
      category: method.category || '',
      description: method.description || '',
      displayOrder: method.displayOrder || 0,
      isActive: method.isActive ?? true
    });
    setShowCreateModal(true);
  };

  const categoryOptions = [
    { value: '', label: 'Select service type (e.g. FTTH)' },
    ...Object.entries(InstallationCategoryLabels).map(([value, label]) => ({
      value,
      label: `${value} - ${label}`
    }))
  ];

  const columns: TableColumn<ExtendedInstallationMethod>[] = [
    { key: 'displayOrder', label: 'Order' },
    { 
      key: 'name', 
      label: 'Name',
      render: (value, row) => (
        <div>
          <span className="font-medium">{value}</span>
          <span className="ml-2 text-xs px-1.5 py-0.5 bg-muted rounded text-muted-foreground">{row.code}</span>
        </div>
      )
    },
    { 
      key: 'category', 
      label: 'Applies To',
      render: (value) => value ? (
        <span className="text-xs px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 rounded text-blue-700 dark:text-blue-300">
          {value}
        </span>
      ) : '-'
    },
    { 
      key: 'description', 
      label: 'Description',
      render: (value) => (
        <span className="text-xs text-muted-foreground truncate max-w-[200px] block">
          {value || '-'}
        </span>
      )
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value) => (
        <span className={cn(
          "px-2 py-1 rounded-full text-xs font-medium",
          value 
            ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
            : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
        )}>
          {value ? 'Active' : 'Inactive'}
        </span>
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
      )
    }
  ];

  if (loading) {
    return (
      <PageShell title="Installation Methods" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Installation Methods' }]}>
        <LoadingSpinner message="Loading installation methods..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Installation Methods"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Installation Methods' }]}
      actions={
        <Button size="sm" onClick={() => { resetForm(); setEditingMethod(null); setShowCreateModal(true); }} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Installation Method
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto space-y-2">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-foreground text-sm">How Installation Methods Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-muted-foreground" /> : <ChevronDown className="h-4 w-4 text-muted-foreground" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
              <div className="bg-muted/50 rounded p-2">
                <h4 className="text-xs font-medium text-foreground mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px] text-white">1</span>
                  Prelaid
                </h4>
                <p className="text-[11px] text-muted-foreground">
                  Fibre already laid by building builder/management. We mainly tap into existing infrastructure.
                  Minimal materials needed (patch cord, indoor drop, faceplate).
                </p>
              </div>
              
              <div className="bg-muted/50 rounded p-2">
                <h4 className="text-xs font-medium text-foreground mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px] text-white">2</span>
                  Non-prelaid (MDU)
                </h4>
                <p className="text-[11px] text-muted-foreground">
                  Multi-dwelling units and old buildings. We must build the fibre infrastructure 
                  (riser, trunking, backbone). Full infra pack required.
                </p>
              </div>
              
              <div className="bg-muted/50 rounded p-2">
                <h4 className="text-xs font-medium text-foreground mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px] text-white">3</span>
                  SDU / RDF Pole
                </h4>
                <p className="text-[11px] text-muted-foreground">
                  Single dwelling units and pole-based installations.
                  Pole accessories, aerial cable, termination box, and basic house kit required.
                </p>
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card>
        {installationMethods.length > 0 ? (
          <DataTable
            data={installationMethods}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No installation methods found"
            message="Add your first installation method to get started."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingMethod !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingMethod(null);
          resetForm();
        }}
      >
        <div className="bg-card rounded-lg shadow-xl max-w-lg w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingMethod ? 'Edit Installation Method' : 'Create Installation Method'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingMethod(null);
                resetForm();
              }}
              className="text-muted-foreground hover:text-foreground"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="space-y-2">
            {/* Department Selection */}
            <SelectInput
              label="Department"
              value={formData.departmentId}
              onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
              options={[
                { value: '', label: 'All Departments (Global)' },
                ...departments.map(d => ({ value: d.id, label: d.name }))
              ]}
            />

            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Name *"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Prelaid"
                required
              />
              <TextInput
                label="Code *"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                placeholder="e.g., PRELAID"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <SelectInput
                label="Applies To"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                options={categoryOptions}
              />
              <TextInput
                label="Display Order"
                type="number"
                value={formData.displayOrder}
                onChange={(e) => setFormData({ ...formData, displayOrder: e.target.value })}
                placeholder="0"
              />
            </div>

            <div className="space-y-0.5">
              <label className="text-xs font-medium">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-2 py-1 text-xs"
                placeholder="Describe when this installation method is used and what materials are typically needed..."
              />
            </div>

            <div className="flex items-center gap-3 pt-2">
              <input
                type="checkbox"
                id="isActive"
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                className="h-3 w-3 rounded border-input"
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
                  setEditingMethod(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingMethod ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingMethod ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default InstallationMethodsPage;

