import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Package, Power, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { getDepartments, createDepartment, updateDepartment, deleteDepartment } from '../../api/departments';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Department, CreateDepartmentRequest, UpdateDepartmentRequest } from '../../types/departments';

interface DepartmentFormData {
  name: string;
  code: string;
  description: string;
  costCentreId: string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const DepartmentsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<DepartmentFormData>({
    name: '',
    code: '',
    description: '',
    costCentreId: '',
    isActive: true
  });

  useEffect(() => {
    loadDepartments();
  }, []);

  const loadDepartments = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load departments');
      console.error('Error loading departments:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      // Prepare data: convert empty strings to null for nullable Guid fields
      const departmentData: CreateDepartmentRequest = {
        name: formData.name.trim(),
        code: formData.code?.trim() || undefined,
        description: formData.description?.trim() || undefined
      };
      
      // Handle costCentreId: only include if it's a valid non-empty GUID string
      const costCentreIdTrimmed = formData.costCentreId?.trim();
      if (costCentreIdTrimmed && costCentreIdTrimmed.length > 0) {
        // Validate it looks like a GUID before including
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        if (guidRegex.test(costCentreIdTrimmed)) {
          (departmentData as any).costCentreId = costCentreIdTrimmed;
        }
      }
      
      await createDepartment(departmentData);
      showSuccess('Department created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadDepartments();
    } catch (err) {
      showError((err as Error).message || 'Failed to create department');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingDepartment) return;
    
    try {
      // Prepare data: convert empty strings to null for nullable Guid fields
      const departmentData: UpdateDepartmentRequest = {
        name: formData.name.trim(),
        code: formData.code?.trim() || undefined,
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true
      };
      
      // Handle costCentreId: only include if it's a valid non-empty GUID string
      const costCentreIdTrimmed = formData.costCentreId?.trim();
      if (costCentreIdTrimmed && costCentreIdTrimmed.length > 0) {
        // Validate it looks like a GUID before including
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        if (guidRegex.test(costCentreIdTrimmed)) {
          (departmentData as any).costCentreId = costCentreIdTrimmed;
        }
      }
      
      await updateDepartment(editingDepartment.id, departmentData);
      showSuccess('Department updated successfully!');
      setShowCreateModal(false);
      setEditingDepartment(null);
      resetForm();
      await loadDepartments();
    } catch (err) {
      showError((err as Error).message || 'Failed to update department');
    }
  };

  const handleToggleStatus = async (department: Department): Promise<void> => {
    try {
      const departmentData: UpdateDepartmentRequest = {
        name: department.name,
        code: department.code || undefined,
        description: department.description || undefined,
        isActive: !department.isActive
      };
      
      if (department.costCentreId) {
        (departmentData as any).costCentreId = department.costCentreId;
      }
      
      await updateDepartment(department.id, departmentData);
      showSuccess(`Department ${!department.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadDepartments();
    } catch (err) {
      showError((err as Error).message || 'Failed to update department status');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this department?')) return;
    
    try {
      await deleteDepartment(id);
      showSuccess('Department deleted successfully!');
      await loadDepartments();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete department');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      code: '',
      description: '',
      costCentreId: '',
      isActive: true
    });
  };

  const openEditModal = (department: Department): void => {
    setEditingDepartment(department);
    setFormData({
      name: department.name,
      code: department.code || '',
      description: department.description || '',
      costCentreId: department.costCentreId || '',
      isActive: department.isActive ?? true
    });
  };

  const columns: TableColumn<Department>[] = [
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    { key: 'costCentreName', label: 'Cost Centre' },
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
      <PageShell title="Departments" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Departments' }]}>
        <LoadingSpinner message="Loading departments..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Departments"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Departments' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Department
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
            <span className="font-medium text-white text-sm">How Departments Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Purpose
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Organize operations</li>
                  <li>• Separate workflows</li>
                  <li>• Track costs</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Common Depts
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>NWO</strong> - New Orders</li>
                  <li>• <strong>CWO</strong> - Current Work</li>
                  <li>• <strong>GPON</strong> - Fibre Infra</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Rate Linking
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• SI rates per dept</li>
                  <li>• Partner rates</li>
                  <li>• Material templates</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Cost Centre
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Link to accounting</li>
                  <li>• P&L tracking</li>
                  <li>• Budget allocation</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card>
        {departments.length > 0 ? (
          <DataTable
            data={departments}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No departments found"
            message="Add your first department to get started."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingDepartment !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingDepartment(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingDepartment ? 'Edit Department' : 'Create Department'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingDepartment(null);
                resetForm();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="space-y-2">
            <TextInput
              label="Department Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />

            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Code"
                name="code"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value })}
              />
              <TextInput
                label="Cost Centre ID"
                name="costCentreId"
                value={formData.costCentreId}
                onChange={(e) => setFormData({ ...formData, costCentreId: e.target.value })}
              />
            </div>

            <div className="space-y-0.5">
              <label className="text-xs font-medium">Description</label>
              <textarea
                name="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
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
              <span className={`ml-2 px-2 py-1 rounded-full text-xs font-medium ${
                formData.isActive 
                  ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
                  : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
              }`}>
                {formData.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingDepartment(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingDepartment ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingDepartment ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default DepartmentsPage;

