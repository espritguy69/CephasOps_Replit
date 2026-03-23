import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { getSplitterTypes, createSplitterType, updateSplitterType, deleteSplitterType } from '../../api/splitterTypes';
import { getDepartments } from '../../api/departments';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest } from '../../types/referenceData';
import type { Department } from '../../types/departments';

interface ExtendedReferenceDataItem extends ReferenceDataItem {
  displayOrder?: number;
  totalPorts?: number;
  standbyPortNumber?: number | null;
}

interface SplitterTypeFormData {
  departmentId: string;
  name: string;
  code: string;
  totalPorts: number | string;
  standbyPortNumber: number | string | null;
  description: string;
  displayOrder: number | string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const SplitterTypesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [splitterTypes, setSplitterTypes] = useState<ExtendedReferenceDataItem[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingSplitterType, setEditingSplitterType] = useState<ExtendedReferenceDataItem | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<SplitterTypeFormData>({
    departmentId: '',
    name: '',
    code: '',
    totalPorts: 8,
    standbyPortNumber: null,
    description: '',
    displayOrder: 0,
    isActive: true
  });

  useEffect(() => {
    loadSplitterTypes();
  }, []);

  useEffect(() => {
    loadDepartments();
  }, []);

  const loadSplitterTypes = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: { departmentId?: string } = {};
      if (formData.departmentId) params.departmentId = formData.departmentId;
      const data = await getSplitterTypes(params);
      setSplitterTypes(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load splitter types');
      console.error('Error loading splitter types:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async (): Promise<void> => {
    try {
      const data = await getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Error loading departments:', err);
      setDepartments([]);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      // Check for duplicate name
      const nameTrimmed = formData.name.trim();
      const duplicateName = splitterTypes.find(
        st => st.name.toLowerCase() === nameTrimmed.toLowerCase()
      );
      if (duplicateName) {
        showError(`A splitter type with the name "${nameTrimmed}" already exists.`);
        return;
      }

      // Check for duplicate code
      const codeTrimmed = formData.code.trim();
      if (codeTrimmed) {
        const duplicateCode = splitterTypes.find(
          st => st.code && st.code.toLowerCase() === codeTrimmed.toLowerCase()
        );
        if (duplicateCode) {
          showError(`A splitter type with the code "${codeTrimmed}" already exists.`);
          return;
        }
      }

      const splitterTypeData: CreateReferenceDataRequest & { displayOrder?: number; totalPorts?: number; standbyPortNumber?: number | null } = {
        name: nameTrimmed,
        code: codeTrimmed,
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0,
        totalPorts: typeof formData.totalPorts === 'number' ? formData.totalPorts : parseInt(String(formData.totalPorts)) || 8,
        standbyPortNumber: formData.standbyPortNumber ? (typeof formData.standbyPortNumber === 'number' ? formData.standbyPortNumber : parseInt(String(formData.standbyPortNumber))) : null
      };
      
      if (formData.departmentId) {
        splitterTypeData.departmentId = formData.departmentId;
      }
      
      await createSplitterType(splitterTypeData as any);
      showSuccess('Splitter Type created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadSplitterTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to create splitter type');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingSplitterType) return;
    
    try {
      // Check for duplicate name (exclude current record)
      const nameTrimmed = formData.name.trim();
      const duplicateName = splitterTypes.find(
        st => st.id !== editingSplitterType.id && st.name.toLowerCase() === nameTrimmed.toLowerCase()
      );
      if (duplicateName) {
        showError(`A splitter type with the name "${nameTrimmed}" already exists.`);
        return;
      }

      // Check for duplicate code (exclude current record)
      const codeTrimmed = formData.code.trim();
      if (codeTrimmed) {
        const duplicateCode = splitterTypes.find(
          st => st.id !== editingSplitterType.id && st.code && st.code.toLowerCase() === codeTrimmed.toLowerCase()
        );
        if (duplicateCode) {
          showError(`A splitter type with the code "${codeTrimmed}" already exists.`);
          return;
        }
      }

      const splitterTypeData: UpdateReferenceDataRequest & { displayOrder?: number; totalPorts?: number; standbyPortNumber?: number | null } = {
        name: nameTrimmed,
        code: codeTrimmed,
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0,
        totalPorts: typeof formData.totalPorts === 'number' ? formData.totalPorts : parseInt(String(formData.totalPorts)) || 8,
        standbyPortNumber: formData.standbyPortNumber ? (typeof formData.standbyPortNumber === 'number' ? formData.standbyPortNumber : parseInt(String(formData.standbyPortNumber))) : null
      };
      
      if (formData.departmentId) {
        splitterTypeData.departmentId = formData.departmentId;
      }
      
      await updateSplitterType(editingSplitterType.id, splitterTypeData as any);
      showSuccess('Splitter Type updated successfully!');
      setShowCreateModal(false);
      setEditingSplitterType(null);
      resetForm();
      await loadSplitterTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to update splitter type');
    }
  };

  const handleToggleStatus = async (splitterType: ExtendedReferenceDataItem): Promise<void> => {
    try {
      const splitterTypeData: UpdateReferenceDataRequest & { displayOrder?: number; totalPorts?: number; standbyPortNumber?: number | null } = {
        name: splitterType.name,
        code: splitterType.code,
        description: splitterType.description || undefined,
        isActive: !splitterType.isActive,
        displayOrder: splitterType.displayOrder || 0,
        totalPorts: splitterType.totalPorts || 8,
        standbyPortNumber: splitterType.standbyPortNumber || null
      };
      
      if (splitterType.departmentId) {
        splitterTypeData.departmentId = splitterType.departmentId;
      }
      
      await updateSplitterType(splitterType.id, splitterTypeData as any);
      showSuccess(`Splitter Type ${!splitterType.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadSplitterTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to update splitter type status');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this splitter type?')) return;
    
    try {
      await deleteSplitterType(id);
      showSuccess('Splitter Type deleted successfully!');
      await loadSplitterTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete splitter type');
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      name: '',
      code: '',
      totalPorts: 8,
      standbyPortNumber: null,
      description: '',
      displayOrder: 0,
      isActive: true
    });
  };

  const openEditModal = async (splitterType: ExtendedReferenceDataItem): Promise<void> => {
    setEditingSplitterType(splitterType);
    setFormData({
      departmentId: splitterType.departmentId || '',
      name: splitterType.name,
      code: splitterType.code || '',
      totalPorts: splitterType.totalPorts || 8,
      standbyPortNumber: splitterType.standbyPortNumber || null,
      description: splitterType.description || '',
      displayOrder: splitterType.displayOrder || 0,
      isActive: splitterType.isActive ?? true
    });
    // Ensure departments are loaded
    await loadDepartments();
  };

  const columns: TableColumn<ExtendedReferenceDataItem>[] = [
    { key: 'displayOrder', label: 'Order' },
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    { key: 'totalPorts', label: 'Total Ports' },
    { key: 'standbyPortNumber', label: 'Standby Port' },
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
      <PageShell title="Splitter Types" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitter Types' }]}>
        <LoadingSpinner message="Loading splitter types..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Splitter Types"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitter Types' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Splitter Type
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
            <span className="font-medium text-white text-sm">How Splitter Types Work</span>
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
                  <li>• Define splitter configs</li>
                  <li>• Set port counts</li>
                  <li>• Configure standby port</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Standard Types
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>1:8</strong> - Small bldg</li>
                  <li>• <strong>1:12</strong> - Medium bldg</li>
                  <li>• <strong>1:32</strong> - Large bldg</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Standby Port
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Emergency reserve</li>
                  <li>• Typically last port</li>
                  <li>• 1:32 uses port 32</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Auto Ports
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Ports auto-created</li>
                  <li>• Based on total count</li>
                  <li>• Standby marked special</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card>
        {splitterTypes.length > 0 ? (
          <DataTable
            data={splitterTypes}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No splitter types found"
            message="Add your first splitter type to get started."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingSplitterType !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingSplitterType(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingSplitterType ? 'Edit Splitter Type' : 'Create Splitter Type'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingSplitterType(null);
                resetForm();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="space-y-2">
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-0.5">
                <label className="text-xs font-medium">Department</label>
                <select
                  value={formData.departmentId}
                  onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                  className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <option value="">No Department</option>
                  {departments.map(dept => (
                    <option key={dept.id} value={dept.id}>{dept.name}</option>
                  ))}
                </select>
              </div>
              <TextInput
                label="Display Order (for dropdown placement)"
                type="number"
                value={formData.displayOrder}
                onChange={(e) => setFormData({ ...formData, displayOrder: e.target.value })}
                placeholder="0"
                title="Lower numbers appear first in dropdowns"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Name *"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                required
              />
              <TextInput
                label="Code *"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Total Ports *"
                type="number"
                value={formData.totalPorts}
                onChange={(e) => setFormData({ ...formData, totalPorts: e.target.value })}
                required
              />
              <TextInput
                label="Standby Port Number"
                type="number"
                value={formData.standbyPortNumber || ''}
                onChange={(e) => setFormData({ ...formData, standbyPortNumber: e.target.value || null })}
                placeholder="Optional"
              />
            </div>

            <div className="space-y-0.5">
              <label className="text-xs font-medium">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={3}
                className="flex w-full rounded-md border border-input bg-background px-2 py-1 text-xs"
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
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingSplitterType(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingSplitterType ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingSplitterType ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default SplitterTypesPage;

