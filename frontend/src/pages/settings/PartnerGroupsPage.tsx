import React, { useState } from 'react';
import { Plus, Edit, Trash2, Save, X, Lightbulb, ChevronDown, ChevronUp, Users } from 'lucide-react';
import { usePartnerGroups, useCreatePartnerGroup, useUpdatePartnerGroup, useDeletePartnerGroup } from '../../hooks/usePartnerGroups';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { PartnerGroup, CreatePartnerGroupRequest, UpdatePartnerGroupRequest } from '../../types/partnerGroups';

interface PartnerGroupFormData {
  name: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const PartnerGroupsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { data: partnerGroups = [], isLoading: loading } = usePartnerGroups();
  const createMutation = useCreatePartnerGroup();
  const updateMutation = useUpdatePartnerGroup();
  const deleteMutation = useDeletePartnerGroup();

  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingGroup, setEditingGroup] = useState<PartnerGroup | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(true);
  const [formData, setFormData] = useState<PartnerGroupFormData>({
    name: ''
  });

  const handleCreate = async (): Promise<void> => {
    if (!formData.name.trim()) {
      showError('Partner group name is required');
      return;
    }

    try {
      const groupData: CreatePartnerGroupRequest = {
        name: formData.name.trim()
      };
      
      await createMutation.mutateAsync(groupData);
      setShowCreateModal(false);
      resetForm();
    } catch (err) {
      // Error already handled by mutation hook
      console.error('Error creating partner group:', err);
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingGroup) return;
    
    if (!formData.name.trim()) {
      showError('Partner group name is required');
      return;
    }

    try {
      const groupData: UpdatePartnerGroupRequest = {
        name: formData.name.trim()
      };
      
      await updateMutation.mutateAsync({ id: editingGroup.id, payload: groupData });
      setShowCreateModal(false);
      setEditingGroup(null);
      resetForm();
    } catch (err) {
      // Error already handled by mutation hook
      console.error('Error updating partner group:', err);
    }
  };

  const handleDelete = async (id: string, name: string): Promise<void> => {
    if (!window.confirm(`Are you sure you want to delete the partner group "${name}"?\n\nNote: You cannot delete a group if partners are assigned to it.`)) {
      return;
    }
    
    try {
      await deleteMutation.mutateAsync(id);
    } catch (err) {
      // Error already handled by mutation hook
      console.error('Error deleting partner group:', err);
    }
  };

  const resetForm = (): void => {
    setFormData({
      name: ''
    });
  };

  const openEditModal = (group: PartnerGroup): void => {
    setEditingGroup(group);
    setFormData({
      name: group.name
    });
    setShowCreateModal(true);
  };

  const columns: TableColumn<PartnerGroup>[] = [
    { 
      key: 'name', 
      label: 'Group Name',
      render: (value, row) => (
        <div className="flex items-center gap-2">
          <Users className="h-4 w-4 text-blue-500" />
          <span className="font-medium">{value as string}</span>
        </div>
      )
    },
    { 
      key: 'createdAt', 
      label: 'Created',
      render: (value) => new Date(value as string).toLocaleDateString()
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (value, row) => (
        <div className="flex items-center gap-2">
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
              handleDelete(row.id, row.name);
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
      <PageShell title="Partner Groups" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Partner Groups' }]}>
        <LoadingSpinner message="Loading partner groups..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Partner Groups"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Partner Groups' }]}
      actions={
        <Button size="sm" onClick={() => setShowCreateModal(true)} className="gap-1">
          <Plus className="h-4 w-4" />
          Add Group
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
            <span className="font-medium text-white text-sm">How Partner Groups Work</span>
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
                  <li>• Organize partners logically</li>
                  <li>• Group by relationship</li>
                  <li>• Simplify management</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Examples
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>TIME Group</strong></li>
                  <li>• <strong>Maxis Group</strong></li>
                  <li>• <strong>Direct Customers</strong></li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Benefits
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Filter orders by group</li>
                  <li>• Report by partner group</li>
                  <li>• Batch operations</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Usage
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Create groups first</li>
                  <li>• Assign partners to groups</li>
                  <li>• View in Partners page</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card>
        {partnerGroups.length > 0 ? (
          <DataTable
            data={partnerGroups}
            columns={columns}
          />
        ) : (
          <EmptyState
            title="No partner groups found"
            message="Create your first partner group to organize your partners."
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingGroup !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingGroup(null);
          resetForm();
        }}
      >
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold">
              {editingGroup ? 'Edit Partner Group' : 'Create Partner Group'}
            </h2>
            <button
              onClick={() => {
                setShowCreateModal(false);
                setEditingGroup(null);
                resetForm();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <X className="h-6 w-6" />
            </button>
          </div>

          <div className="space-y-4">
            <TextInput
              label="Group Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g., TIME Group, Maxis Group"
              required
            />

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingGroup(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingGroup ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                <Save className="h-4 w-4" />
                {editingGroup ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PartnerGroupsPage;

