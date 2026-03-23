import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Power, ChevronRight, ChevronDown, Save, X } from 'lucide-react';
import { getPnlTypeTree, createPnlType, updatePnlType, deletePnlType } from '../../api/pnlTypes';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput } from '../../components/ui';
import { cn } from '@/lib/utils';
import type { PnlType, CreatePnlTypeRequest, UpdatePnlTypeRequest, PnlTypeCategory } from '../../types/pnlTypes';

interface PnlTypeFormData {
  name: string;
  code: string;
  description: string;
  category: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
  isTransactional: boolean;
}

const PnlTypesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [incomeTree, setIncomeTree] = useState<PnlType[]>([]);
  const [expenseTree, setExpenseTree] = useState<PnlType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [activeTab, setActiveTab] = useState<'Expense' | 'Income'>('Expense');
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingType, setEditingType] = useState<PnlType | null>(null);
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());
  const [formData, setFormData] = useState<PnlTypeFormData>({
    name: '',
    code: '',
    description: '',
    category: 'Expense',
    parentId: null,
    sortOrder: 0,
    isActive: true,
    isTransactional: true
  });

  useEffect(() => {
    loadPnlTypes();
  }, []);

  const loadPnlTypes = async (): Promise<void> => {
    try {
      setLoading(true);
      const [incomeData, expenseData] = await Promise.all([
        getPnlTypeTree({ category: 'Income' }),
        getPnlTypeTree({ category: 'Expense' })
      ]);
      setIncomeTree(Array.isArray(incomeData) ? incomeData : []);
      setExpenseTree(Array.isArray(expenseData) ? expenseData : []);
    } catch (err) {
      console.error('Error loading P&L types:', err);
      showError('Failed to load P&L types');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.name.trim()) {
        showError('Name is required');
        return;
      }
      if (!formData.code.trim()) {
        showError('Code is required');
        return;
      }
      const pnlTypeData: CreatePnlTypeRequest & { sortOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        category: formData.category as PnlTypeCategory,
        parentId: formData.parentId || undefined,
        isTransactional: formData.isTransactional,
        isActive: formData.isActive,
        sortOrder: formData.sortOrder
      };
      await createPnlType(pnlTypeData as any);
      showSuccess('P&L type created successfully');
      setShowCreateModal(false);
      resetForm();
      loadPnlTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to create P&L type');
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingType) return;
    
    try {
      if (!formData.name.trim()) {
        showError('Name is required');
        return;
      }
      const pnlTypeData: UpdatePnlTypeRequest & { sortOrder?: number } = {
        name: formData.name.trim(),
        code: formData.code.trim(),
        description: formData.description?.trim() || undefined,
        parentId: formData.parentId || undefined,
        isTransactional: formData.isTransactional,
        isActive: formData.isActive,
        sortOrder: formData.sortOrder
      };
      await updatePnlType(editingType.id, pnlTypeData as any);
      showSuccess('P&L type updated successfully');
      setShowCreateModal(false);
      setEditingType(null);
      resetForm();
      loadPnlTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to update P&L type');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this P&L type?')) return;
    
    try {
      await deletePnlType(id);
      showSuccess('P&L type deleted successfully');
      loadPnlTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to delete P&L type');
    }
  };

  const handleToggleStatus = async (pnlType: PnlType): Promise<void> => {
    try {
      await updatePnlType(pnlType.id, { isActive: !pnlType.isActive } as UpdatePnlTypeRequest);
      showSuccess(`P&L type ${!pnlType.isActive ? 'activated' : 'deactivated'} successfully!`);
      loadPnlTypes();
    } catch (err) {
      showError((err as Error).message || 'Failed to update status');
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: '',
      code: '',
      description: '',
      category: activeTab,
      parentId: null,
      sortOrder: 0,
      isActive: true,
      isTransactional: true
    });
  };

  const openCreateModal = (parentNode: PnlType | null = null): void => {
    resetForm();
    setEditingType(null);
    if (parentNode) {
      setFormData(prev => ({
        ...prev,
        category: (parentNode.category || activeTab) as string,
        parentId: parentNode.id
      }));
    } else {
      setFormData(prev => ({ ...prev, category: activeTab }));
    }
    setShowCreateModal(true);
  };

  const openEditModal = (pnlType: PnlType): void => {
    setEditingType(pnlType);
    setFormData({
      name: pnlType.name || '',
      code: pnlType.code || '',
      description: pnlType.description || '',
      category: pnlType.category,
      parentId: pnlType.parentId || null,
      sortOrder: pnlType.sortOrder || 0,
      isActive: pnlType.isActive !== false,
      isTransactional: pnlType.isTransactional !== false
    });
    setShowCreateModal(true);
  };

  const toggleNode = (nodeId: string): void => {
    setExpandedNodes(prev => {
      const newSet = new Set(prev);
      if (newSet.has(nodeId)) {
        newSet.delete(nodeId);
      } else {
        newSet.add(nodeId);
      }
      return newSet;
    });
  };

  const getParentOptions = (tree: PnlType[]): Array<{ value: string; label: string }> => {
    const options: Array<{ value: string; label: string }> = [{ value: '', label: '(Root Level)' }];
    
    const flatten = (nodes: PnlType[], level: number = 0): void => {
      nodes.forEach(node => {
        if (!node.isTransactional || !editingType || node.id !== editingType.id) {
          options.push({
            value: node.id,
            label: '  '.repeat(level) + node.name
          });
          if (node.children && node.children.length > 0) {
            flatten(node.children, level + 1);
          }
        }
      });
    };
    
    flatten(tree);
    return options;
  };

  const renderTreeNode = (node: PnlType, level: number = 0): React.ReactNode => {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = expandedNodes.has(node.id);

    return (
      <div key={node.id}>
        <div 
          className={cn(
            "flex items-center gap-2 py-2 px-3 hover:bg-accent rounded cursor-pointer transition-colors",
            !node.isActive && "opacity-50"
          )}
          style={{ paddingLeft: `${12 + level * 24}px` }}
        >
          {hasChildren ? (
            <button onClick={() => toggleNode(node.id)} className="p-0.5 hover:bg-muted rounded">
              {isExpanded ? (
                <ChevronDown className="h-3 w-3 text-muted-foreground" />
              ) : (
                <ChevronRight className="h-3 w-3 text-muted-foreground" />
              )}
            </button>
          ) : (
            <span className="w-4" />
          )}
          
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-medium text-foreground text-xs truncate">{node.name}</span>
              <span className="text-[10px] px-1.5 py-0.5 bg-muted rounded text-muted-foreground">{node.code}</span>
              {!node.isTransactional && (
                <span className="text-[10px] px-1.5 py-0.5 bg-purple-100 dark:bg-purple-900/30 rounded text-purple-700 dark:text-purple-300">Group</span>
              )}
            </div>
            {node.description && (
              <p className="text-[10px] text-muted-foreground truncate">{node.description}</p>
            )}
          </div>

          <div className="flex items-center gap-1 flex-shrink-0">
            <button
              onClick={(e) => { e.stopPropagation(); openCreateModal(node); }}
              className="p-1 text-green-600 hover:text-green-500 transition-colors"
              title="Add Child"
            >
              <Plus className="h-3 w-3" />
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); handleToggleStatus(node); }}
              className={cn(
                "p-1 transition-colors",
                node.isActive ? "text-yellow-600 hover:text-yellow-500" : "text-green-600 hover:text-green-500"
              )}
              title={node.isActive ? 'Deactivate' : 'Activate'}
            >
              <Power className="h-3 w-3" />
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); openEditModal(node); }}
              className="p-1 text-blue-600 hover:text-blue-500 transition-colors"
              title="Edit"
            >
              <Edit className="h-3 w-3" />
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); handleDelete(node.id); }}
              className="p-1 text-red-600 hover:text-red-500 transition-colors"
              title="Delete"
            >
              <Trash2 className="h-3 w-3" />
            </button>
          </div>
        </div>

        {hasChildren && isExpanded && (
          <div>
            {node.children!.map(child => renderTreeNode(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  const currentTree = activeTab === 'Income' ? incomeTree : expenseTree;

  if (loading) {
    return (
      <PageShell title="P&L Types" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'P&L Types' }]}>
        <LoadingSpinner message="Loading P&L types..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="P&L Types"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'P&L Types' }]}
      actions={
        <Button onClick={() => openCreateModal()} className="flex items-center gap-2">
          <Plus className="h-4 w-4" />
          Add P&L Type
        </Button>
      }
    >
      <div className="flex-1 p-2 max-w-7xl mx-auto">
      {/* Tabs */}
      <div className="flex gap-1 mb-2">
        <button
          onClick={() => setActiveTab('Expense')}
          className={cn(
            "px-3 py-1.5 rounded text-xs font-medium transition-colors",
            activeTab === 'Expense'
              ? "bg-red-600 text-white"
              : "bg-muted text-muted-foreground hover:bg-accent"
          )}
        >
          Expense Types
        </button>
        <button
          onClick={() => setActiveTab('Income')}
          className={cn(
            "px-3 py-1.5 rounded text-xs font-medium transition-colors",
            activeTab === 'Income'
              ? "bg-green-600 text-white"
              : "bg-muted text-muted-foreground hover:bg-accent"
          )}
        >
          Income Types
        </button>
      </div>

      <Card>
        <div className="p-2 border-b border-border flex items-center justify-between">
          <h3 className="font-medium text-foreground text-xs">
            {activeTab === 'Income' ? 'Income Categories' : 'Expense Categories'}
          </h3>
          <button
            onClick={() => setExpandedNodes(new Set(currentTree.map(n => n.id)))}
            className="text-[10px] text-muted-foreground hover:text-foreground transition-colors"
          >
            Expand All
          </button>
        </div>

        <div className="divide-y divide-border">
          {currentTree.length === 0 ? (
            <EmptyState 
              title="No P&L types found"
              message={`Click "Add P&L Type" to create your first ${activeTab.toLowerCase()} type.`} 
            />
          ) : (
            currentTree.map(node => renderTreeNode(node))
          )}
        </div>
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingType !== null}
        onClose={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}
      >
        <div className="bg-card rounded-lg shadow-xl max-w-lg w-full">
          <div className="flex items-center justify-between mb-2">
            <h2 className="text-xs font-bold">
              {editingType ? 'Edit P&L Type' : 'Create P&L Type'}
            </h2>
            <button
              onClick={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}
              className="text-muted-foreground hover:text-foreground"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="space-y-2">
            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Name *"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., Vehicle Expenses"
                required
              />
              <TextInput
                label="Code *"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                placeholder="e.g., EXP-VEH"
                required
                disabled={!!editingType}
              />
            </div>
            
            <div className="space-y-0.5">
              <label className="text-xs font-medium">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                rows={2}
                className="flex w-full rounded-md border border-input bg-background px-2 py-1 text-xs"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <SelectInput
                label="Category"
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value, parentId: null })}
                options={[
                  { value: 'Expense', label: 'Expense' },
                  { value: 'Income', label: 'Income' }
                ]}
                disabled={!!editingType}
              />
              <SelectInput
                label="Parent"
                value={formData.parentId || ''}
                onChange={(e) => setFormData({ ...formData, parentId: e.target.value || null })}
                options={getParentOptions(formData.category === 'Income' ? incomeTree : expenseTree)}
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <TextInput
                label="Sort Order"
                type="number"
                value={formData.sortOrder}
                onChange={(e) => setFormData({ ...formData, sortOrder: parseInt(e.target.value) || 0 })}
              />
              <div className="space-y-1.5 pt-4">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isTransactional"
                    checked={formData.isTransactional}
                    onChange={(e) => setFormData({ ...formData, isTransactional: e.target.checked })}
                    className="h-3 w-3 rounded border-input"
                  />
                  <label htmlFor="isTransactional" className="text-xs text-muted-foreground cursor-pointer">
                    Transactional
                  </label>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="h-3 w-3 rounded border-input"
                  />
                  <label htmlFor="isActive" className="text-xs text-muted-foreground cursor-pointer">
                    Active
                  </label>
                </div>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t">
              <Button variant="outline" onClick={() => { setShowCreateModal(false); resetForm(); setEditingType(null); }}>
                Cancel
              </Button>
              <Button onClick={editingType ? handleUpdate : handleCreate} className="flex items-center gap-2">
                <Save className="h-4 w-4" />
                {editingType ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PnlTypesPage;

