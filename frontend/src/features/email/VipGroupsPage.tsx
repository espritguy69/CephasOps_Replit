import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Users, Save, X, Building2, Lightbulb, ChevronDown, ChevronUp, Mail } from 'lucide-react';
import * as emailApi from '../../api/email';
import * as departmentsApi from '../../api/departments';
import { getUsers } from '../../api/rbac';
import { LoadingSpinner, EmptyState, Button, Card, Modal, TextInput, Select, DataTable, StatusBadge, useToast } from '../../components/ui';
import type { VipGroup, VipGroupFormData, Department, User, TableColumn } from '../../types/email';

// ============================================================================
// Component
// ============================================================================

const VipGroupsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [groups, setGroups] = useState<VipGroup[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingGroup, setEditingGroup] = useState<VipGroup | null>(null);
  const [showGuide, setShowGuide] = useState(true);
  const [formData, setFormData] = useState<VipGroupFormData>({
    name: '',
    code: '',
    description: '',
    notifyDepartmentId: '',
    notifyUserId: '',
    notifyHodUserId: '',
    notifyRole: '',
    priority: 0,
    isActive: true,
    emailAddresses: []
  });

  useEffect(() => {
    loadGroups();
    loadDepartments();
    loadUsers();
  }, []);

  const loadGroups = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await emailApi.getVipGroups();
      const sorted = [...data].sort((a: VipGroup, b: VipGroup) => (b.priority || 0) - (a.priority || 0));
      setGroups(sorted);
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error('Failed to load VIP groups:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDepartments = async () => {
    try {
      const data = await departmentsApi.getDepartments({ isActive: true });
      setDepartments(data || []);
    } catch (err) {
      console.error('Failed to load departments:', err);
    }
  };

  const loadUsers = async () => {
    try {
      const data = await getUsers();
      setUsers(data || []);
    } catch (err) {
      console.error('Failed to load users:', err);
    }
  };

  const handleAdd = () => {
    setEditingGroup(null);
    setFormData({
      name: '',
      code: '',
      description: '',
      notifyDepartmentId: '',
      notifyUserId: '',
      notifyHodUserId: '',
      notifyRole: '',
      priority: 0,
      isActive: true,
      emailAddresses: []
    });
    setShowModal(true);
  };

  const handleEdit = (group: VipGroup) => {
    setEditingGroup(group);
    setFormData({
      name: group.name || '',
      code: group.code || '',
      description: group.description || '',
      notifyDepartmentId: group.notifyDepartmentId || '',
      notifyUserId: group.notifyUserId || '',
      notifyHodUserId: group.notifyHodUserId || '',
      notifyRole: group.notifyRole || '',
      priority: group.priority || 0,
      isActive: group.isActive !== false,
      emailAddresses: group.emailAddresses?.map(e => e.emailAddress) || []
    });
    setShowModal(true);
  };

  const handleDelete = async (groupId: string) => {
    if (!window.confirm('Are you sure you want to delete this VIP group?')) {
      return;
    }

    try {
      setError(null);
      await emailApi.deleteVipGroup(groupId);
      showSuccess('VIP group deleted successfully');
      await loadGroups();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to delete VIP group';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to delete VIP group:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.name?.trim()) {
      showError('Group name is required');
      return;
    }
    if (!formData.code?.trim()) {
      showError('Group code is required');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      const submitData = {
        ...formData,
        code: formData.code.toUpperCase().replace(/\s+/g, '_'),
        notifyDepartmentId: formData.notifyDepartmentId || null,
        notifyUserId: formData.notifyUserId || null,
        notifyHodUserId: formData.notifyHodUserId || null,
        notifyRole: formData.notifyRole || null,
        priority: parseInt(String(formData.priority), 10) || 0,
        emailAddresses: formData.emailAddresses.filter(e => e.trim() !== '')
      };

      if (editingGroup) {
        await emailApi.updateVipGroup(editingGroup.id, submitData);
        showSuccess('VIP group updated successfully');
      } else {
        await emailApi.createVipGroup(submitData);
        showSuccess('VIP group created successfully');
      }

      setShowModal(false);
      await loadGroups();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to save VIP group';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to save VIP group:', err);
    } finally {
      setSaving(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const target = e.target as HTMLInputElement;
    const { name, value, type } = target;
    const checked = target.checked;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : (value === '' ? '' : value)
    }));
  };

  if (loading) {
    return (
      <div className="flex-1 p-6">
        <LoadingSpinner message="Loading VIP groups..." fullPage />
      </div>
    );
  }

  const addEmailAddress = () => {
    setFormData(prev => ({
      ...prev,
      emailAddresses: [...prev.emailAddresses, '']
    }));
  };

  const removeEmailAddress = (index: number) => {
    setFormData(prev => ({
      ...prev,
      emailAddresses: prev.emailAddresses.filter((_, i) => i !== index)
    }));
  };

  const updateEmailAddress = (index: number, value: string) => {
    setFormData(prev => ({
      ...prev,
      emailAddresses: prev.emailAddresses.map((email, i) => i === index ? value : email)
    }));
  };

  const columns: TableColumn<VipGroup>[] = [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'code', label: 'Code', sortable: true },
    { 
      key: 'emailAddresses', 
      label: 'Emails', 
      render: (value) => {
        const emails = value as VipGroupEmail[] | undefined;
        const count = emails?.length || 0;
        return count > 0 ? (
          <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-blue-500/20 text-blue-400">
            {count} {count === 1 ? 'email' : 'emails'}
          </span>
        ) : <span className="text-slate-400">-</span>;
      }
    },
    { 
      key: 'departmentName', 
      label: 'Department',
      render: (value) => (value as string) || '-'
    },
    { 
      key: 'notifyUserName', 
      label: 'Notify User',
      render: (value) => (value as string) || '-'
    },
    { 
      key: 'hodUserName', 
      label: 'HOD/Supervisor',
      render: (value) => (value as string) || '-'
    },
    { key: 'priority', label: 'Priority', sortable: true },
    {
      key: 'isActive',
      label: 'Status',
      render: (_, item) => (
        <StatusBadge
          status={item.isActive ? 'Active' : 'Inactive'}
          variant={item.isActive ? 'success' : 'secondary'}
          size="sm"
        />
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, item) => (
        <div className="flex gap-2">
          <button
            onClick={() => handleEdit(item)}
            className="p-1 hover:bg-slate-700 rounded"
            title="Edit"
          >
            <Edit className="h-4 w-4 text-slate-400" />
          </button>
          <button
            onClick={() => handleDelete(item.id)}
            className="p-1 hover:bg-red-500/20 rounded"
            title="Delete"
          >
            <Trash2 className="h-4 w-4 text-red-400" />
          </button>
        </div>
      )
    }
  ];

  return (
    <div className="flex-1 p-6 space-y-4">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How VIP Groups Work</span>
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
                  <li>• Reusable templates</li>
                  <li>• Group VIP emails together</li>
                  <li>• Shared notification rules</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Routing
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• To department</li>
                  <li>• To specific user</li>
                  <li>• To HOD/supervisor</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Priority
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Higher = evaluated first</li>
                  <li>• Assign urgency levels</li>
                  <li>• Sort notification order</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Example
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• "Procurement VIP"</li>
                  <li>• Dept: Operations</li>
                  <li>• HOD: John Smith</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Header */}
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <Users className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">VIP Groups</h1>
          <span className="text-sm text-slate-400">({groups.length} groups)</span>
        </div>
        <Button onClick={handleAdd} variant="primary">
          <Plus className="h-4 w-4 mr-2" />
          Add VIP Group
        </Button>
      </div>

      {/* Error Display */}
      {error && (
        <Card className="mb-4 p-4 bg-red-500/10 border-red-500/30">
          <p className="text-red-400">{error}</p>
        </Card>
      )}

      {/* Groups Table */}
      {groups.length === 0 ? (
        <EmptyState
          icon={<Users className="h-12 w-12" />}
          title="No VIP Groups"
          description="Create VIP groups to define notification routing templates for important contacts."
          action={
            <Button onClick={handleAdd} variant="primary">
              <Plus className="h-4 w-4 mr-2" />
              Add VIP Group
            </Button>
          }
        />
      ) : (
        <DataTable
          columns={columns}
          data={groups}
          className="bg-layout-card"
        />
      )}

      {/* Add/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title={editingGroup ? 'Edit VIP Group' : 'Add VIP Group'}
        size="lg"
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Group Name *"
              name="name"
              value={formData.name}
              onChange={handleInputChange}
              placeholder="e.g., Procurement VIP"
              required
            />

            <TextInput
              label="Group Code *"
              name="code"
              value={formData.code}
              onChange={handleInputChange}
              placeholder="e.g., PROCUREMENT_VIP"
              required
            />
          </div>

          <TextInput
            label="Description"
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            as="textarea"
            rows={2}
            placeholder="Describe this VIP group..."
          />

          <Card className="p-4 bg-slate-800/50 border-slate-700">
            <h3 className="text-sm font-semibold text-slate-200 mb-3 flex items-center gap-2">
              <Building2 className="h-4 w-4" />
              Notification Routing
            </h3>
            
            <div className="grid grid-cols-2 gap-4">
              <Select
                label="Department"
                name="notifyDepartmentId"
                value={formData.notifyDepartmentId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: 'Select Department' },
                  ...departments.map(d => ({ value: d.id, label: d.name }))
                ]}
              />

              <Select
                label="Notify User"
                name="notifyUserId"
                value={formData.notifyUserId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: 'Select User' },
                  ...users.map(u => ({ value: u.id, label: u.fullName || u.email }))
                ]}
              />
            </div>

            <div className="grid grid-cols-2 gap-4 mt-4">
              <Select
                label="HOD/Supervisor"
                name="notifyHodUserId"
                value={formData.notifyHodUserId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: 'Select HOD/Supervisor' },
                  ...users.map(u => ({ value: u.id, label: u.fullName || u.email }))
                ]}
              />

              <TextInput
                label="Notify Role"
                name="notifyRole"
                value={formData.notifyRole}
                onChange={handleInputChange}
                placeholder="e.g., Admin, Manager"
              />
            </div>
          </Card>

          {/* Email Addresses Section */}
          <Card className="p-4 bg-slate-800/50 border-slate-700">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-slate-200 flex items-center gap-2">
                <Mail className="h-4 w-4" />
                Email Addresses
              </h3>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={addEmailAddress}
              >
                <Plus className="h-3 w-3 mr-1" />
                Add Email
              </Button>
            </div>
            <p className="text-xs text-slate-400 mb-3">
              Add email addresses to this group. These will automatically inherit the group&apos;s notification settings.
            </p>
            
            {formData.emailAddresses.length === 0 ? (
              <div className="text-sm text-slate-400 italic py-2">
                No email addresses added yet. Click &quot;Add Email&quot; to add one.
              </div>
            ) : (
              <div className="space-y-2">
                {formData.emailAddresses.map((email, index) => (
                  <div key={index} className="flex gap-2 items-center">
                    <TextInput
                      type="email"
                      value={email}
                      onChange={(e) => updateEmailAddress(index, e.target.value)}
                      placeholder="email@example.com"
                      className="flex-1"
                    />
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() => removeEmailAddress(index)}
                      className="text-red-400 hover:text-red-300"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </Card>

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Priority"
              name="priority"
              type="number"
              value={formData.priority}
              onChange={handleInputChange}
            />

            <div className="flex items-end pb-2">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleInputChange}
                  className="h-4 w-4 rounded border-gray-300"
                />
                <label className="text-sm font-medium">Active</label>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t border-slate-700">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setShowModal(false)}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={saving}
            >
              {saving ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  {editingGroup ? 'Update' : 'Create'} Group
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default VipGroupsPage;

