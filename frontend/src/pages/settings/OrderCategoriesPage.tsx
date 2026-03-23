import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import { getOrderCategories, createOrderCategory, updateOrderCategory, deleteOrderCategory } from '../../api/orderCategories';
import { getDepartments } from '../../api/departments';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest } from '../../types/referenceData';
import type { Department } from '../../types/departments';

interface ExtendedReferenceDataItem extends ReferenceDataItem {
  displayOrder?: number;
}

interface OrderCategoryFormData {
  departmentId: string;
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

const OrderCategoriesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [orderCategories, setOrderCategories] = useState<ExtendedReferenceDataItem[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingOrderCategory, setEditingOrderCategory] = useState<ExtendedReferenceDataItem | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<OrderCategoryFormData>({
    departmentId: '',
    name: '',
    code: '',
    description: '',
    displayOrder: 0,
    isActive: true
  });

  useEffect(() => {
    loadOrderCategories();
  }, []);

  useEffect(() => {
    loadDepartments();
  }, []);

  const loadOrderCategories = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: { departmentId?: string } = {};
      if (formData.departmentId) params.departmentId = formData.departmentId;
      const data = await getOrderCategories(params);
      setOrderCategories(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load order categories');
      console.error('Error loading order categories:', err);
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
      const orderCategoryData: CreateReferenceDataRequest & { displayOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      if (formData.departmentId) {
        orderCategoryData.departmentId = formData.departmentId;
      }
      
      await createOrderCategory(orderCategoryData as any);
      showSuccess('Order Category created successfully!');
      setShowCreateModal(false);
      resetForm();
      await loadOrderCategories();
    } catch (err) {
      showError((err as Error).message || 'Failed to create order category');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingOrderCategory) return;
    
    try {
      const orderCategoryData: UpdateReferenceDataRequest & { displayOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        isActive: formData.isActive ?? true,
        displayOrder: typeof formData.displayOrder === 'number' ? formData.displayOrder : parseInt(String(formData.displayOrder)) || 0
      };
      
      if (formData.departmentId) {
        orderCategoryData.departmentId = formData.departmentId;
      }
      
      await updateOrderCategory(editingOrderCategory.id, orderCategoryData as any);
      showSuccess('Order Category updated successfully!');
      setShowCreateModal(false);
      setEditingOrderCategory(null);
      resetForm();
      await loadOrderCategories();
    } catch (err) {
      showError((err as Error).message || 'Failed to update order category');
    }
  };

  const handleToggleStatus = async (orderCategory: ExtendedReferenceDataItem): Promise<void> => {
    try {
      const orderCategoryData: UpdateReferenceDataRequest & { displayOrder?: number } = {
        name: orderCategory.name,
        code: orderCategory.code,
        description: orderCategory.description || undefined,
        isActive: !orderCategory.isActive,
        displayOrder: orderCategory.displayOrder || 0
      };
      
      if (orderCategory.departmentId) {
        orderCategoryData.departmentId = orderCategory.departmentId;
      }
      
      await updateOrderCategory(orderCategory.id, orderCategoryData as any);
      showSuccess(`Order Category ${!orderCategory.isActive ? 'activated' : 'deactivated'} successfully!`);
      await loadOrderCategories();
    } catch (err) {
      showError((err as Error).message || 'Failed to update order category status');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this order category?')) return;
    
    try {
      await deleteOrderCategory(id);
      showSuccess('Order Category deleted successfully!');
      await loadOrderCategories();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete order category');
    }
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      name: '',
      code: '',
      description: '',
      displayOrder: 0,
      isActive: true
    });
  };

  const openEditModal = async (orderCategory: ExtendedReferenceDataItem): Promise<void> => {
    setEditingOrderCategory(orderCategory);
    setFormData({
      departmentId: orderCategory.departmentId || '',
      name: orderCategory.name,
      code: orderCategory.code || '',
      description: orderCategory.description || '',
      displayOrder: orderCategory.displayOrder || 0,
      isActive: orderCategory.isActive ?? true
    });
    // Ensure departments are loaded
    await loadDepartments();
  };

  const columns: TableColumn<ExtendedReferenceDataItem>[] = [
    { key: 'displayOrder', label: 'Order' },
    { key: 'name', label: 'Name' },
    { key: 'code', label: 'Code' },
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
    return <LoadingSpinner message="Loading order categories..." fullPage />;
  }

  return (
    <PageShell
      title="Order Categories"
      breadcrumbs={[{ label: 'Settings' }, { label: 'Order Categories' }]}
      actions={
        <Button
          onClick={() => {
            resetForm();
            setEditingOrderCategory(null);
            setShowCreateModal(true);
          }}
          className="flex items-center gap-2"
        >
          <Plus className="h-4 w-4" />
          Add category
        </Button>
      }
    >
    <div className="flex-1 p-2 max-w-7xl mx-auto space-y-2">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Order Categories Work</span>
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
                  <li>• Categorize services</li>
                  <li>• Group similar work</li>
                  <li>• Material templates</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Common Categories
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>TIME-FTTH</strong> - Home fibre</li>
                  <li>• <strong>TIME-FTTR</strong> - Room fibre</li>
                  <li>• <strong>TIME-FTTC</strong> - Fibre to the Charge</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Rate Impact
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Different pricing</li>
                  <li>• Affects SI rates</li>
                  <li>• Partner billing</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Material Link
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Links to templates</li>
                  <li>• Auto-apply materials</li>
                  <li>• Per department</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card>
        {orderCategories.length > 0 ? (
          <DataTable
            data={orderCategories}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No order categories found"
            message="Add your first order category to get started."
            action={{
              label: 'Add category',
              onClick: () => {
                resetForm();
                setEditingOrderCategory(null);
                setShowCreateModal(true);
              }
            }}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingOrderCategory !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingOrderCategory(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-2xl w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingOrderCategory ? 'Edit Order Category' : 'Create Order Category'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingOrderCategory(null);
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
                label="Display Order"
                type="number"
                value={formData.displayOrder}
                onChange={(e) => setFormData({ ...formData, displayOrder: e.target.value })}
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
                  setEditingOrderCategory(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingOrderCategory ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingOrderCategory ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default OrderCategoriesPage;

