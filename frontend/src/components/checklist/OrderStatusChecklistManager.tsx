import React, { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  Plus,
  Edit,
  Trash2,
  ChevronDown,
  ChevronRight,
  GripVertical,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import {
  Modal,
  Button,
  TextInput,
  useToast,
  LoadingSpinner,
} from '../ui';
import {
  useChecklistItemsByStatus,
  useCreateChecklistItem,
  useUpdateChecklistItem,
  useDeleteChecklistItem,
  useReorderChecklistItems,
  useBulkUpdateChecklistItems,
  useCopyChecklistFromStatus,
  type OrderStatusChecklistItem,
} from '../../hooks/useOrderStatusChecklists';
import { useOrderStatuses } from '../../hooks/useOrderStatuses';

interface OrderStatusChecklistManagerProps {
  statusCode: string;
  statusName: string;
  isOpen: boolean;
  onClose: () => void;
}

interface ChecklistItemFormData {
  name: string;
  description: string;
  orderIndex: number;
  isRequired: boolean;
  isActive: boolean;
}

interface SortableChecklistItemProps {
  item: OrderStatusChecklistItem;
  level: number;
  isExpanded: boolean;
  isEditing: boolean;
  isAddingSubStep: boolean;
  formData: ChecklistItemFormData;
  isSelected: boolean;
  onToggleExpanded: (id: string) => void;
  onEdit: (item: OrderStatusChecklistItem) => void;
  onDelete: (id: string) => void;
  onAddSubStep: (id: string) => void;
  onSave: () => void;
  onCancel: () => void;
  onFormDataChange: (data: ChecklistItemFormData) => void;
  onSelect: (id: string, selected: boolean) => void;
  renderSubSteps: (item: OrderStatusChecklistItem) => React.ReactNode;
}

const SortableChecklistItem: React.FC<SortableChecklistItemProps> = ({
  item,
  level,
  isExpanded,
  isEditing,
  isAddingSubStep,
  formData,
  isSelected,
  onToggleExpanded,
  onEdit,
  onDelete,
  onAddSubStep,
  onSave,
  onCancel,
  onFormDataChange,
  onSelect,
  renderSubSteps,
}) => {
  const isMainStep = !item.parentChecklistItemId;
  const hasSubSteps = item.subSteps && item.subSteps.length > 0;

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: item.id,
    disabled: !isMainStep, // Only main steps are draggable
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div ref={setNodeRef} style={style} className="border-b border-border last:border-b-0">
      {/* Main Step Row */}
      <div
        className={`flex items-center gap-2 p-2 hover:bg-muted/50 transition-colors ${
          level > 0 ? 'pl-6' : ''
        } ${isDragging ? 'bg-muted' : ''}`}
      >
        {/* Expand/Collapse Icon */}
        {hasSubSteps && (
          <button
            onClick={() => onToggleExpanded(item.id)}
            className="p-1 hover:bg-muted rounded transition-colors"
          >
            {isExpanded ? (
              <ChevronDown className="h-4 w-4" />
            ) : (
              <ChevronRight className="h-4 w-4" />
            )}
          </button>
        )}
        {!hasSubSteps && <div className="w-6" />}

        {/* Bulk Selection Checkbox */}
        <input
          type="checkbox"
          checked={isSelected}
          onChange={(e) => onSelect(item.id, e.target.checked)}
          className="rounded cursor-pointer"
          onClick={(e) => e.stopPropagation()}
        />

        {/* Drag Handle - only for main steps */}
        {isMainStep ? (
          <div
            {...attributes}
            {...listeners}
            className="cursor-grab active:cursor-grabbing p-1 hover:bg-muted rounded"
          >
            <GripVertical className="h-4 w-4 text-muted-foreground" />
          </div>
        ) : (
          <div className="w-6" />
        )}

        {/* Required Badge */}
        {item.isRequired && (
          <span className="px-1.5 py-0.5 text-xs font-medium bg-red-100 text-red-800 rounded">
            Required
          </span>
        )}

        {/* Item Name */}
        <div className="flex-1">
          <span className={`font-medium ${isMainStep ? 'text-base' : 'text-sm text-muted-foreground'}`}>
            {item.name}
          </span>
          {item.description && (
            <p className="text-xs text-muted-foreground mt-0.5">{item.description}</p>
          )}
        </div>

        {/* Status Badge */}
        {!item.isActive && (
          <span className="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded">Inactive</span>
        )}

        {/* Actions */}
        {isMainStep && (
          <button
            onClick={() => onAddSubStep(item.id)}
            className="p-1.5 text-blue-600 hover:bg-blue-50 rounded transition-colors"
            title="Add Sub-Step"
          >
            <Plus className="h-4 w-4" />
          </button>
        )}
        <button
          onClick={() => onEdit(item)}
          className="p-1.5 text-blue-600 hover:bg-blue-50 rounded transition-colors"
          title="Edit"
        >
          <Edit className="h-4 w-4" />
        </button>
        <button
          onClick={() => onDelete(item.id)}
          className="p-1.5 text-red-600 hover:bg-red-50 rounded transition-colors"
          title="Delete"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>

      {/* Edit Form */}
      {isEditing && (
        <div className="p-4 bg-muted/30 border-l-2 border-blue-500">
          <div className="space-y-2">
            <TextInput
              label="Name"
              value={formData.name}
              onChange={(e) => onFormDataChange({ ...formData, name: e.target.value })}
              required
            />
            <TextInput
              label="Description"
              value={formData.description}
              onChange={(e) => onFormDataChange({ ...formData, description: e.target.value })}
            />
            <div className="flex gap-4">
              <TextInput
                label="Order Index"
                type="number"
                value={formData.orderIndex}
                onChange={(e) =>
                  onFormDataChange({ ...formData, orderIndex: parseInt(e.target.value) || 0 })
                }
              />
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isRequired"
                  checked={formData.isRequired}
                  onChange={(e) => onFormDataChange({ ...formData, isRequired: e.target.checked })}
                  className="rounded"
                />
                <label htmlFor="isRequired" className="text-sm">
                  Required
                </label>
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => onFormDataChange({ ...formData, isActive: e.target.checked })}
                  className="rounded"
                />
                <label htmlFor="isActive" className="text-sm">
                  Active
                </label>
              </div>
            </div>
            <div className="flex gap-2">
              <Button onClick={onSave} size="sm">
                Save
              </Button>
              <Button onClick={onCancel} variant="outline" size="sm">
                Cancel
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Add Sub-Step Form */}
      {isAddingSubStep && (
        <div className="p-4 bg-muted/30 border-l-2 border-green-500 ml-6">
          <div className="space-y-2">
            <TextInput
              label="Sub-Step Name"
              value={formData.name}
              onChange={(e) => onFormDataChange({ ...formData, name: e.target.value })}
              required
            />
            <TextInput
              label="Description"
              value={formData.description}
              onChange={(e) => onFormDataChange({ ...formData, description: e.target.value })}
            />
            <div className="flex gap-4">
              <TextInput
                label="Order Index"
                type="number"
                value={formData.orderIndex}
                onChange={(e) =>
                  onFormDataChange({ ...formData, orderIndex: parseInt(e.target.value) || 0 })
                }
              />
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="subIsRequired"
                  checked={formData.isRequired}
                  onChange={(e) => onFormDataChange({ ...formData, isRequired: e.target.checked })}
                  className="rounded"
                />
                <label htmlFor="subIsRequired" className="text-sm">
                  Required
                </label>
              </div>
            </div>
            <div className="flex gap-2">
              <Button onClick={onSave} size="sm">
                Add Sub-Step
              </Button>
              <Button onClick={onCancel} variant="outline" size="sm">
                Cancel
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Sub-Steps */}
      {hasSubSteps && isExpanded && (
        <div className="ml-6">
          {renderSubSteps(item)}
        </div>
      )}
    </div>
  );
};

const OrderStatusChecklistManager: React.FC<OrderStatusChecklistManagerProps> = ({
  statusCode,
  statusName,
  isOpen,
  onClose,
}) => {
  const { showSuccess, showError } = useToast();
  const { data: checklistItems, isLoading, refetch } = useChecklistItemsByStatus(statusCode);
  const queryClient = useQueryClient();
  const createMutation = useCreateChecklistItem(statusCode);
  const updateMutation = useUpdateChecklistItem(statusCode);
  const deleteMutation = useDeleteChecklistItem(statusCode);
  const reorderMutation = useReorderChecklistItems(statusCode);

  // DnD sensors
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set());
  const [editingItem, setEditingItem] = useState<OrderStatusChecklistItem | null>(null);
  const [showAddMainStep, setShowAddMainStep] = useState(false);
  const [showAddSubStep, setShowAddSubStep] = useState<string | null>(null);
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
  const [showBulkActions, setShowBulkActions] = useState(false);
  const [showCopyDialog, setShowCopyDialog] = useState(false);
  const [formData, setFormData] = useState<ChecklistItemFormData>({
    name: '',
    description: '',
    orderIndex: 0,
    isRequired: false,
    isActive: true,
  });

  const toggleExpanded = (itemId: string) => {
    const newExpanded = new Set(expandedItems);
    if (newExpanded.has(itemId)) {
      newExpanded.delete(itemId);
    } else {
      newExpanded.add(itemId);
    }
    setExpandedItems(newExpanded);
  };

  const handleAddMainStep = () => {
    setEditingItem(null);
    setShowAddSubStep(null);
    setFormData({
      name: '',
      description: '',
      orderIndex: checklistItems?.length || 0,
      isRequired: false,
      isActive: true,
    });
    setShowAddMainStep(true);
  };

  const handleAddSubStep = (parentId: string) => {
    const parent = findItemById(parentId);
    if (!parent) return;

    setEditingItem(null);
    setShowAddMainStep(false);
    setFormData({
      name: '',
      description: '',
      orderIndex: parent.subSteps?.length || 0,
      isRequired: false,
      isActive: true,
    });
    setShowAddSubStep(parentId);
  };

  const handleEdit = (item: OrderStatusChecklistItem) => {
    setEditingItem(item);
    setShowAddMainStep(false);
    setShowAddSubStep(null);
    setFormData({
      name: item.name,
      description: item.description || '',
      orderIndex: item.orderIndex,
      isRequired: item.isRequired,
      isActive: item.isActive,
    });
  };

  const handleDelete = async (itemId: string) => {
    if (!confirm('Are you sure you want to delete this checklist item?')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(itemId);
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
    } catch (error) {
      showError('Failed to delete checklist item');
    }
  };

  const handleSave = async () => {
    try {
      if (editingItem) {
        // Update existing item
        await updateMutation.mutateAsync({
          itemId: editingItem.id,
          data: {
            name: formData.name,
            description: formData.description || undefined,
            orderIndex: formData.orderIndex,
            isRequired: formData.isRequired,
            isActive: formData.isActive,
          },
        });
      } else if (showAddSubStep) {
        // Add sub-step
        await createMutation.mutateAsync({
          statusCode,
          parentChecklistItemId: showAddSubStep,
          name: formData.name,
          description: formData.description || undefined,
          orderIndex: formData.orderIndex,
          isRequired: formData.isRequired,
          isActive: formData.isActive,
        });
      } else {
        // Add main step
        await createMutation.mutateAsync({
          statusCode,
          name: formData.name,
          description: formData.description || undefined,
          orderIndex: formData.orderIndex,
          isRequired: formData.isRequired,
          isActive: formData.isActive,
        });
      }

      // Reset form
      setEditingItem(null);
      setShowAddMainStep(false);
      setShowAddSubStep(null);
      setFormData({
        name: '',
        description: '',
        orderIndex: 0,
        isRequired: false,
        isActive: true,
      });
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
    } catch (error) {
      showError('Failed to save checklist item');
    }
  };

  const handleCancel = () => {
    setEditingItem(null);
    setShowAddMainStep(false);
    setShowAddSubStep(null);
    setFormData({
      name: '',
      description: '',
      orderIndex: 0,
      isRequired: false,
      isActive: true,
    });
  };

  const findItemById = (id: string): OrderStatusChecklistItem | null => {
    if (!checklistItems) return null;

    const findInItems = (items: OrderStatusChecklistItem[]): OrderStatusChecklistItem | null => {
      for (const item of items) {
        if (item.id === id) return item;
        if (item.subSteps) {
          const found = findInItems(item.subSteps);
          if (found) return found;
        }
      }
      return null;
    };

    return findInItems(checklistItems);
  };

  const handleSelectItem = (itemId: string, selected: boolean) => {
    const newSelected = new Set(selectedItems);
    if (selected) {
      newSelected.add(itemId);
    } else {
      newSelected.delete(itemId);
    }
    setSelectedItems(newSelected);
  };

  const handleSelectAll = (selected: boolean) => {
    if (!checklistItems) return;
    const allMainStepIds = checklistItems
      .filter((item) => !item.parentChecklistItemId)
      .map((item) => item.id);
    
    if (selected) {
      setSelectedItems(new Set(allMainStepIds));
    } else {
      setSelectedItems(new Set());
    }
  };

  const handleBulkAction = async () => {
    if (selectedItems.size === 0 || !bulkActionType) return;

    const itemIds = Array.from(selectedItems);
    let updateDto: any = {};

    switch (bulkActionType) {
      case 'activate':
        updateDto = { isActive: true };
        break;
      case 'deactivate':
        updateDto = { isActive: false };
        break;
      case 'required':
        updateDto = { isRequired: true };
        break;
      case 'optional':
        updateDto = { isRequired: false };
        break;
    }

    try {
      await bulkUpdateMutation.mutateAsync({
        itemIds,
        updateDto,
      });
      setSelectedItems(new Set());
      setShowBulkActions(false);
      setBulkActionType(null);
    } catch (error) {
      showError('Failed to perform bulk action');
    }
  };

  const handleCopyFromStatus = async (sourceStatusCode: string) => {
    try {
      await copyMutation.mutateAsync(sourceStatusCode);
      setShowCopyDialog(false);
    } catch (error) {
      showError('Failed to copy checklist');
    }
  };

  const handleExport = () => {
    if (!checklistItems) return;

    const exportData = {
      statusCode,
      statusName,
      items: checklistItems.map((item) => ({
        name: item.name,
        description: item.description,
        orderIndex: item.orderIndex,
        isRequired: item.isRequired,
        isActive: item.isActive,
        subSteps: item.subSteps?.map((sub) => ({
          name: sub.name,
          description: sub.description,
          orderIndex: sub.orderIndex,
          isRequired: sub.isRequired,
          isActive: sub.isActive,
        })),
      })),
      exportedAt: new Date().toISOString(),
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `checklist-${statusCode}-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    showSuccess('Checklist exported successfully');
  };

  const handleImport = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = async (e) => {
      try {
        const importData = JSON.parse(e.target?.result as string);
        
        // Validate import data structure
        if (!Array.isArray(importData)) {
          throw new Error('Import file must contain an array of checklist items');
        }

        // Create items from imported data
        let createdCount = 0;
        let errorCount = 0;
        
        for (const item of importData) {
          try {
            await createMutation.mutateAsync({
              name: item.name || item.Name,
              description: item.description || item.Description,
              orderIndex: item.orderIndex ?? item.OrderIndex ?? 0,
              isRequired: item.isRequired ?? item.IsRequired ?? false,
              isActive: item.isActive ?? item.IsActive ?? true,
              parentChecklistItemId: item.parentChecklistItemId || item.ParentChecklistItemId
            });
            createdCount++;
          } catch (err) {
            console.error('Failed to create checklist item:', err);
            errorCount++;
          }
        }

        if (createdCount > 0) {
          showSuccess(`Successfully imported ${createdCount} checklist item(s)${errorCount > 0 ? ` (${errorCount} failed)` : ''}`);
        } else {
          showError('Failed to import any checklist items');
        }
      } catch (error: any) {
        showError(error.message || 'Failed to import checklist. Invalid file format.');
      }
    };
    reader.readAsText(file);
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;

    if (!over || active.id === over.id || !checklistItems) {
      return;
    }

    // Only allow reordering of main steps (not sub-steps)
    const activeItem = findItemById(active.id as string);
    const overItem = findItemById(over.id as string);

    if (!activeItem || !overItem || activeItem.parentChecklistItemId || overItem.parentChecklistItemId) {
      return; // Can only reorder main steps
    }

    // Get all main steps
    const mainSteps = checklistItems.filter((item) => !item.parentChecklistItemId);
    const activeIndex = mainSteps.findIndex((item) => item.id === active.id);
    const overIndex = mainSteps.findIndex((item) => item.id === over.id);

    if (activeIndex === -1 || overIndex === -1) {
      return;
    }

    // Reorder in local array
    const reordered = [...mainSteps];
    const [removed] = reordered.splice(activeIndex, 1);
    reordered.splice(overIndex, 0, removed);

    // Create order map
    const itemOrderMap: Record<string, number> = {};
    reordered.forEach((item, index) => {
      itemOrderMap[item.id] = index;
    });

    try {
      await reorderMutation.mutateAsync(itemOrderMap);
    } catch (error) {
      showError('Failed to reorder checklist items');
    }
  };

  const renderSubSteps = (parentItem: OrderStatusChecklistItem) => {
    if (!parentItem.subSteps) return null;
    return parentItem.subSteps.map((subStep) => (
      <SortableChecklistItem
        key={subStep.id}
        item={subStep}
        level={1}
        isExpanded={expandedItems.has(subStep.id)}
        isEditing={editingItem?.id === subStep.id}
        isAddingSubStep={false}
        formData={formData}
        isSelected={selectedItems.has(subStep.id)}
        onToggleExpanded={toggleExpanded}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onAddSubStep={() => {}}
        onSave={handleSave}
        onCancel={handleCancel}
        onFormDataChange={setFormData}
        onSelect={handleSelectItem}
        renderSubSteps={() => null}
      />
    ));
  };

  const allMainStepsSelected = checklistItems
    ? checklistItems.filter((item) => !item.parentChecklistItemId).every((item) => selectedItems.has(item.id))
    : false;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Manage Process: ${statusName}`} size="xl">
      <div className="space-y-4">
        {/* Header Actions */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Define the process steps and sub-steps for this status. All required items must be
            completed before transitioning to the next status.
          </p>
          <div className="flex gap-2">
            {selectedItems.size > 0 && (
              <Button
                onClick={() => setShowBulkActions(true)}
                variant="outline"
                size="sm"
              >
                Bulk Actions ({selectedItems.size})
              </Button>
            )}
            <Button onClick={() => setShowCopyDialog(true)} variant="outline" size="sm">
              Copy from Status
            </Button>
            <Button onClick={handleExport} variant="outline" size="sm">
              Export
            </Button>
            <label className="cursor-pointer">
              <input
                type="file"
                accept=".json"
                onChange={handleImport}
                className="hidden"
              />
              <Button as="span" variant="outline" size="sm">
                Import
              </Button>
            </label>
            <Button onClick={handleAddMainStep} size="sm">
              <Plus className="h-4 w-4 mr-1" />
              Add Step
            </Button>
          </div>
        </div>

        {/* Add Main Step Form */}
        {showAddMainStep && (
          <div className="p-4 bg-muted/30 border border-blue-500 rounded-lg">
            <h3 className="text-sm font-semibold mb-2">Add Main Step</h3>
            <div className="space-y-2">
              <TextInput
                label="Step Name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                required
              />
              <TextInput
                label="Description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              />
              <div className="flex gap-4">
                <TextInput
                  label="Order Index"
                  type="number"
                  value={formData.orderIndex}
                  onChange={(e) =>
                    setFormData({ ...formData, orderIndex: parseInt(e.target.value) || 0 })
                  }
                />
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="mainIsRequired"
                    checked={formData.isRequired}
                    onChange={(e) => setFormData({ ...formData, isRequired: e.target.checked })}
                    className="rounded"
                  />
                  <label htmlFor="mainIsRequired" className="text-sm">
                    Required
                  </label>
                </div>
              </div>
              <div className="flex gap-2">
                <Button onClick={handleSave} size="sm">
                  Add Step
                </Button>
                <Button onClick={handleCancel} variant="outline" size="sm">
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Checklist Items List */}
        {isLoading ? (
          <LoadingSpinner />
        ) : !checklistItems || checklistItems.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            <p>No checklist items defined yet.</p>
            <p className="text-sm mt-1">Click "Add Step" to create the first step.</p>
          </div>
        ) : (
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={checklistItems.map((item) => item.id)}
              strategy={verticalListSortingStrategy}
            >
              <div className="border border-border rounded-lg divide-y divide-border">
                {/* Select All Header */}
                <div className="flex items-center gap-2 p-2 bg-muted/30 border-b border-border">
                  <input
                    type="checkbox"
                    checked={allMainStepsSelected}
                    onChange={(e) => handleSelectAll(e.target.checked)}
                    className="rounded cursor-pointer"
                  />
                  <span className="text-sm font-medium text-muted-foreground">
                    Select All Main Steps
                  </span>
                </div>
                {checklistItems.map((item) => (
                  <SortableChecklistItem
                    key={item.id}
                    item={item}
                    level={0}
                    isExpanded={expandedItems.has(item.id)}
                    isEditing={editingItem?.id === item.id}
                    isAddingSubStep={showAddSubStep === item.id}
                    formData={formData}
                    isSelected={selectedItems.has(item.id)}
                    onToggleExpanded={toggleExpanded}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                    onAddSubStep={handleAddSubStep}
                    onSave={handleSave}
                    onCancel={handleCancel}
                    onFormDataChange={setFormData}
                    onSelect={handleSelectItem}
                    renderSubSteps={renderSubSteps}
                  />
                ))}
              </div>
            </SortableContext>
          </DndContext>
        )}

        {/* Footer */}
        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button onClick={onClose} variant="outline">
            Close
          </Button>
        </div>
      </div>

      {/* Bulk Actions Modal */}
      <Modal
        isOpen={showBulkActions}
        onClose={() => {
          setShowBulkActions(false);
          setBulkActionType(null);
        }}
        title="Bulk Actions"
        size="md"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {selectedItems.size} item(s) selected. Choose an action to apply:
          </p>
          <div className="space-y-2">
            <Button
              variant="outline"
              className="w-full justify-start"
              onClick={() => {
                setBulkActionType('activate');
                handleBulkAction();
              }}
            >
              <CheckCircle className="h-4 w-4 mr-2" />
              Activate Selected
            </Button>
            <Button
              variant="outline"
              className="w-full justify-start"
              onClick={() => {
                setBulkActionType('deactivate');
                handleBulkAction();
              }}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Deactivate Selected
            </Button>
            <Button
              variant="outline"
              className="w-full justify-start"
              onClick={() => {
                setBulkActionType('required');
                handleBulkAction();
              }}
            >
              <CheckCircle className="h-4 w-4 mr-2" />
              Mark as Required
            </Button>
            <Button
              variant="outline"
              className="w-full justify-start"
              onClick={() => {
                setBulkActionType('optional');
                handleBulkAction();
              }}
            >
              <XCircle className="h-4 w-4 mr-2" />
              Mark as Optional
            </Button>
          </div>
        </div>
      </Modal>

      {/* Copy Dialog */}
      <CopyChecklistDialog
        isOpen={showCopyDialog}
        onClose={() => setShowCopyDialog(false)}
        onCopy={handleCopyFromStatus}
        currentStatusCode={statusCode}
      />
    </Modal>
  );
};

// Copy Checklist Dialog Component
interface CopyChecklistDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCopy: (sourceStatusCode: string) => void;
  currentStatusCode: string;
}

const CopyChecklistDialog: React.FC<CopyChecklistDialogProps> = ({
  isOpen,
  onClose,
  onCopy,
  currentStatusCode,
}) => {
  const { data: statuses, isLoading } = useOrderStatuses();
  const [selectedStatusCode, setSelectedStatusCode] = useState<string>('');

  const availableStatuses = statuses?.filter((s) => s.code !== currentStatusCode) || [];

  const handleCopy = () => {
    if (selectedStatusCode) {
      onCopy(selectedStatusCode);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Copy Checklist from Status" size="md">
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">
          Select a status to copy its checklist configuration to the current status.
        </p>
        {isLoading ? (
          <LoadingSpinner />
        ) : (
          <div className="space-y-2">
            <label className="text-sm font-medium">Source Status</label>
            <select
              value={selectedStatusCode}
              onChange={(e) => setSelectedStatusCode(e.target.value)}
              className="w-full p-2 border rounded-md"
            >
              <option value="">Select a status...</option>
              {availableStatuses.map((status) => (
                <option key={status.code} value={status.code}>
                  {status.name} ({status.code})
                </option>
              ))}
            </select>
          </div>
        )}
        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button onClick={onClose} variant="outline">
            Cancel
          </Button>
          <Button onClick={handleCopy} disabled={!selectedStatusCode}>
            Copy
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default OrderStatusChecklistManager;

