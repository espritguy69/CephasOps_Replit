import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import {
  getOrderTypeParents,
  getOrderTypeSubtypes,
  createOrderType,
  updateOrderType,
  deleteOrderType,
  type OrderTypeDto,
  type CreateOrderTypeRequest,
  type UpdateOrderTypeRequest,
} from '../../api/orderTypes';
import { getDepartments } from '../../api/departments';
import { useDepartment } from '../../contexts/DepartmentContext';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Department } from '../../types/departments';

interface OrderTypeFormData {
  departmentId: string;
  parentOrderTypeId: string;
  name: string;
  code: string;
  description: string;
  displayOrder: number | string;
  isActive: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const OrderTypesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { departmentId: contextDepartmentId, activeDepartment } = useDepartment();
  const [parents, setParents] = useState<OrderTypeDto[]>([]);
  const [subtypes, setSubtypes] = useState<OrderTypeDto[]>([]);
  const [selectedParentId, setSelectedParentId] = useState<string | null>(null);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loadingParents, setLoadingParents] = useState(true);
  const [loadingSubtypes, setLoadingSubtypes] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isSubtypeModal, setIsSubtypeModal] = useState(false);
  const [editingItem, setEditingItem] = useState<OrderTypeDto | null>(null);
  const [showGuide, setShowGuide] = useState(true);
  const [formData, setFormData] = useState<OrderTypeFormData>({
    departmentId: '',
    parentOrderTypeId: '',
    name: '',
    code: '',
    description: '',
    displayOrder: 0,
    isActive: true,
  });

  const departmentId = activeDepartment?.id || contextDepartmentId || undefined;

  const loadParents = useCallback(async () => {
    try {
      setLoadingParents(true);
      const data = await getOrderTypeParents({
        departmentId: departmentId || undefined,
        isActive: undefined,
      });
      const list = Array.isArray(data) ? data : [];
      setParents(list);
      const ids = list.map((p: OrderTypeDto) => p.id);
      if (!selectedParentId && list.length) {
        setSelectedParentId(list[0]?.id ?? null);
      } else if (selectedParentId && !ids.includes(selectedParentId)) {
        setSelectedParentId(list.length ? list[0].id : null);
      }
    } catch (err) {
      showError((err as Error).message || 'Failed to load order types');
      console.error('Error loading order types:', err);
    } finally {
      setLoadingParents(false);
    }
  }, [departmentId, selectedParentId, showError]);

  const loadSubtypes = useCallback(async () => {
    if (!selectedParentId) {
      setSubtypes([]);
      return;
    }
    try {
      setLoadingSubtypes(true);
      const data = await getOrderTypeSubtypes(selectedParentId);
      setSubtypes(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load subtypes');
      setSubtypes([]);
    } finally {
      setLoadingSubtypes(false);
    }
  }, [selectedParentId, showError]);

  useEffect(() => {
    loadParents();
  }, [loadParents]);

  useEffect(() => {
    loadSubtypes();
  }, [loadSubtypes]);

  useEffect(() => {
    const loadDepts = async () => {
      try {
        const data = await getDepartments();
        setDepartments(Array.isArray(data) ? data : []);
      } catch {
        setDepartments([]);
      }
    };
    loadDepts();
  }, []);

  const resetForm = () => {
    setFormData({
      departmentId: departmentId || '',
      parentOrderTypeId: selectedParentId || '',
      name: '',
      code: '',
      description: '',
      displayOrder: 0,
      isActive: true,
    });
    setEditingItem(null);
    setIsSubtypeModal(false);
  };

  const handleCreateParent = () => {
    setIsSubtypeModal(false);
    setFormData({
      departmentId: departmentId || '',
      parentOrderTypeId: '',
      name: '',
      code: '',
      description: '',
      displayOrder: parents.length + 1,
      isActive: true,
    });
    setShowCreateModal(true);
  };

  const handleCreateSubtype = () => {
    if (!selectedParentId) return;
    setIsSubtypeModal(true);
    setFormData({
      departmentId: departmentId || '',
      parentOrderTypeId: selectedParentId,
      name: '',
      code: '',
      description: '',
      displayOrder: subtypes.length + 1,
      isActive: true,
    });
    setShowCreateModal(true);
  };

  const handleSave = async () => {
    try {
      if (!editingItem && isSubtypeModal && !selectedParentId) {
        showError('Please select a parent type first.');
        return;
      }
      const displayOrder = typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder), 10) || 0;
      if (editingItem) {
        // When editing a subtype, always send parentOrderTypeId so the backend does not detach it.
        const parentId = editingItem.parentOrderTypeId || formData.parentOrderTypeId || selectedParentId || undefined;
        const payload: UpdateOrderTypeRequest = {
          name: formData.name.trim(),
          code: formData.code.trim(),
          description: formData.description?.trim() || undefined,
          isActive: formData.isActive,
          displayOrder,
          departmentId: formData.departmentId || undefined,
          parentOrderTypeId: parentId || undefined,
        };
        await updateOrderType(editingItem.id, payload);
        showSuccess('Order type updated successfully!');
      } else {
        // Subtype create: always send the selected parent so the record is stored as a child (ParentOrderTypeId set).
        // Do not rely only on form state — use selectedParentId when adding a subtype so we never create a top-level row by mistake.
        const parentIdForCreate = isSubtypeModal && selectedParentId
          ? selectedParentId
          : (formData.parentOrderTypeId && formData.parentOrderTypeId.trim() ? formData.parentOrderTypeId.trim() : undefined);
        const payload: CreateOrderTypeRequest = {
          name: formData.name.trim(),
          code: formData.code.trim(),
          description: formData.description?.trim() || undefined,
          isActive: formData.isActive,
          displayOrder,
          departmentId: formData.departmentId || undefined,
          parentOrderTypeId: parentIdForCreate ?? undefined,
        };
        await createOrderType(payload);
        showSuccess(isSubtypeModal ? 'Subtype created successfully!' : 'Order type created successfully!');
      }
      setShowCreateModal(false);
      resetForm();
      await loadParents();
      await loadSubtypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to save');
    }
  };

  const handleToggleStatus = async (item: OrderTypeDto) => {
    try {
      await updateOrderType(item.id, {
        name: item.name,
        code: item.code,
        description: item.description ?? undefined,
        isActive: !item.isActive,
        displayOrder: item.displayOrder ?? 0,
        parentOrderTypeId: item.parentOrderTypeId ?? undefined,
      });
      showSuccess(item.isActive ? 'Deactivated' : 'Activated');
      await loadParents();
      await loadSubtypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to update status');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this order type?')) return;
    try {
      await deleteOrderType(id);
      showSuccess('Deleted successfully');
      setShowCreateModal(false);
      resetForm();
      await loadParents();
      await loadSubtypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete');
    }
  };

  const openEdit = (item: OrderTypeDto) => {
    setEditingItem(item);
    setIsSubtypeModal(!!item.parentOrderTypeId);
    setFormData({
      departmentId: item.departmentId || departmentId || '',
      parentOrderTypeId: item.parentOrderTypeId || selectedParentId || '',
      name: item.name,
      code: item.code,
      description: item.description || '',
      displayOrder: item.displayOrder ?? 0,
      isActive: item.isActive ?? true,
    });
    setShowCreateModal(true);
  };

  const subtypeColumns: TableColumn<OrderTypeDto>[] = [
    { key: 'displayOrder', label: 'Order' },
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
    {
      key: 'isActive',
      label: 'Status',
      render: (value) => (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${value ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'}`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, row) => (
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => handleToggleStatus(row)} title={row.isActive ? 'Deactivate' : 'Activate'} className="text-yellow-600 hover:opacity-75">
            <Power className="h-3 w-3" />
          </button>
          <button type="button" onClick={() => openEdit(row)} title="Edit" className="text-blue-600 hover:opacity-75">
            <Edit className="h-3 w-3" />
          </button>
          <button type="button" onClick={() => handleDelete(row.id)} title="Delete" className="text-red-600 hover:opacity-75">
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      ),
    },
  ];

  const selectedParent = parents.find((p) => p.id === selectedParentId);

  if (loadingParents) {
    return <LoadingSpinner message="Loading order types..." fullPage />;
  }

  return (
    <PageShell
      title="Order Types"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Order Types' }]}
      actions={
        <div className="flex gap-2">
          <Button size="sm" onClick={handleCreateParent} className="gap-1">
            <Plus className="h-4 w-4" />
            Add Parent Type
          </Button>
          {selectedParentId && (
            <Button size="sm" variant="outline" onClick={handleCreateSubtype} className="gap-1">
              <Plus className="h-4 w-4" />
              Add Subtype
            </Button>
          )}
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-2">
        <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
          <button type="button" onClick={() => setShowGuide(!showGuide)} className="w-full flex items-center justify-between px-3 py-2">
            <div className="flex items-center gap-2">
              <Lightbulb className="h-4 w-4 text-blue-400" />
              <span className="font-medium text-white text-sm">How Order Types Work</span>
            </div>
            {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
          </button>
          {showGuide && (
            <div className="px-3 pb-3">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                <div className="bg-slate-800/50 rounded p-2">
                  <h4 className="text-xs font-medium text-white mb-1">Parent types</h4>
                  <p className="text-[11px] text-slate-300">Activation, Modification, Assurance, Value Added Service.</p>
                </div>
                <div className="bg-slate-800/50 rounded p-2">
                  <h4 className="text-xs font-medium text-white mb-1">Subtypes</h4>
                  <p className="text-[11px] text-slate-300">Modification → Indoor/Outdoor; Assurance → Standard/Repull; VAS → Upgrade/IAD/Fixed IP.</p>
                </div>
                <div className="bg-slate-800/50 rounded p-2">
                  <h4 className="text-xs font-medium text-white mb-1">Create Order</h4>
                  <p className="text-[11px] text-slate-300">Parent and subtype dropdowns on Create Order load from here.</p>
                </div>
                <div className="bg-slate-800/50 rounded p-2">
                  <h4 className="text-xs font-medium text-white mb-1">Display order</h4>
                  <p className="text-[11px] text-slate-300">Lower number appears first in dropdowns.</p>
                </div>
              </div>
            </div>
          )}
        </Card>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <Card>
            <h3 className="text-sm font-semibold mb-2">Parent Order Types</h3>
            <p className="text-xs text-muted-foreground mb-2">Top-level types; select one to manage its subtypes below.</p>
            {parents.length === 0 ? (
              <EmptyState title="No parent types" message="Add a parent order type to get started." />
            ) : (
              <div className="space-y-1">
                {parents.map((p) => (
                  <div
                    key={p.id}
                    className={`p-2 rounded border flex items-center justify-between gap-2 ${selectedParentId === p.id ? 'border-primary bg-primary/5 ring-1 ring-primary/20' : 'border-border hover:bg-muted/50'}`}
                  >
                    <button
                      type="button"
                      className="flex-1 text-left"
                      onClick={() => setSelectedParentId(p.id)}
                    >
                      <span className="font-medium">{p.name}</span>
                      <span className="text-xs text-muted-foreground ml-1">({p.code})</span>
                      <span className="text-xs text-muted-foreground ml-1">— {typeof p.childCount === 'number' ? p.childCount : 0} subtype{(typeof p.childCount === 'number' ? p.childCount : 0) !== 1 ? 's' : ''}</span>
                      {selectedParentId === p.id && <span className="ml-1 text-xs text-primary font-medium">(selected)</span>}
                    </button>
                    <div className="flex items-center gap-1 shrink-0">
                      <button type="button" onClick={() => handleToggleStatus(p)} title={p.isActive ? 'Deactivate' : 'Activate'} className="text-yellow-600 hover:opacity-75">
                        <Power className="h-3 w-3" />
                      </button>
                      <button type="button" onClick={() => openEdit(p)} title="Edit" className="text-blue-600 hover:opacity-75">
                        <Edit className="h-3 w-3" />
                      </button>
                      <button type="button" onClick={() => handleDelete(p.id)} title="Delete" className="text-red-600 hover:opacity-75">
                        <Trash2 className="h-3 w-3" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>

          <Card>
            <h3 className="text-sm font-semibold mb-2">
              Subtypes {selectedParent ? `— ${selectedParent.name}` : ''}
            </h3>
            <p className="text-xs text-muted-foreground mb-2">Child types under the selected parent; shown in Create Order when that parent is chosen.</p>
            {!selectedParentId ? (
              <EmptyState title="Select a parent" message="Select a parent order type above to view or add subtypes." />
            ) : loadingSubtypes ? (
              <LoadingSpinner message="Loading subtypes..." />
            ) : subtypes.length === 0 ? (
              <EmptyState title="No subtypes" message="This parent has no subtypes yet. Add one with the button above, or use the parent as a leaf type (e.g. Activation)." />
            ) : (
              <DataTable data={subtypes} columns={subtypeColumns} />
            )}
          </Card>
        </div>

        <Modal
          isOpen={showCreateModal}
          onClose={() => {
            setShowCreateModal(false);
            resetForm();
          }}
        >
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
            <div className="flex items-center justify-between mb-2">
              <h2 className="text-xs font-bold">
                {editingItem ? 'Edit Order Type' : isSubtypeModal ? 'Add Subtype' : 'Add Parent Order Type'}
              </h2>
              <button type="button" onClick={() => { setShowCreateModal(false); resetForm(); }} className="text-gray-400 hover:text-gray-600">
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
                    className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs"
                  >
                    <option value="">No Department</option>
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>{d.name}</option>
                    ))}
                  </select>
                </div>
                <TextInput label="Display Order" type="number" value={formData.displayOrder} onChange={(e) => setFormData({ ...formData, displayOrder: e.target.value })} />
              </div>
              {isSubtypeModal && (
                <div className="space-y-0.5">
                  <label className="text-xs font-medium">Parent</label>
                  <select
                    value={formData.parentOrderTypeId}
                    onChange={(e) => setFormData({ ...formData, parentOrderTypeId: e.target.value })}
                    className="flex h-8 w-full rounded border border-input bg-background px-2 py-1 text-xs"
                  >
                    {parents.map((p) => (
                      <option key={p.id} value={p.id}>{p.name} ({p.code})</option>
                    ))}
                  </select>
                </div>
              )}
              <div className="grid grid-cols-2 gap-2">
                <TextInput label="Name *" value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} required />
                <TextInput label="Code *" value={formData.code} onChange={(e) => setFormData({ ...formData, code: e.target.value })} required />
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
                <input type="checkbox" id="isActive" checked={formData.isActive} onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })} className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary" />
                <label htmlFor="isActive" className="text-xs font-medium cursor-pointer">Active</label>
              </div>
              <div className="flex justify-end gap-2 pt-2 border-t">
                <Button variant="outline" onClick={() => { setShowCreateModal(false); resetForm(); }}>Cancel</Button>
                <Button onClick={handleSave} className="flex items-center gap-2">
                  <Save className="h-4 w-4" />
                  {editingItem ? 'Update' : 'Create'}
                </Button>
              </div>
            </div>
          </div>
        </Modal>
      </div>
    </PageShell>
  );
};

export default OrderTypesPage;
