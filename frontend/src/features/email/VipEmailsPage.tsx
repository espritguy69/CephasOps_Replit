import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Mail, Save, X, Users, Lightbulb, ChevronDown, ChevronUp, Building2 } from 'lucide-react';
import * as emailApi from '../../api/email';
import * as departmentApi from '../../api/departments';
import { getUsers } from '../../api/rbac';
import { LoadingSpinner, EmptyState, Button, Card, Modal, TextInput, Select, DataTable, StatusBadge, useToast } from '../../components/ui';
import type { VipEmail, VipEmailFormData, VipGroup, Department, User, TableColumn, SelectOption } from '../../types/email';

// ============================================================================
// Component
// ============================================================================

const VipEmailsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [vipEmails, setVipEmails] = useState<VipEmail[]>([]);
  const [vipGroups, setVipGroups] = useState<VipGroup[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingVipEmail, setEditingVipEmail] = useState<VipEmail | null>(null);
  const [showGuide, setShowGuide] = useState(true);
  const [viewMode, setViewMode] = useState<'grouped' | 'flat'>('grouped');
  const [formData, setFormData] = useState<VipEmailFormData>({
    emailAddress: '',
    displayName: '',
    vipGroupId: '',
    notifyUserId: '',
    notifyRole: '',
    departmentId: '',
    description: '',
    isActive: true
  });

  useEffect(() => {
    loadVipEmails();
    loadVipGroups();
    loadUsers();
    loadDepartments();
  }, []);

  const loadVipEmails = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await emailApi.getVipEmails();
      setVipEmails(data);
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error('Failed to load VIP emails:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadVipGroups = async () => {
    try {
      const data = await emailApi.getVipGroups();
      setVipGroups(data || []);
    } catch (err) {
      console.error('Failed to load VIP groups:', err);
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

  const loadDepartments = async () => {
    try {
      const data = await departmentApi.getDepartments({ isActive: true });
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Failed to load departments:', err);
    }
  };

  const handleAdd = () => {
    setEditingVipEmail(null);
    setFormData({
      emailAddress: '',
      displayName: '',
      vipGroupId: '',
      notifyUserId: '',
      notifyRole: '',
      departmentId: '',
      description: '',
      isActive: true
    });
    setShowModal(true);
  };

  const handleEdit = (vipEmail: VipEmail) => {
    setEditingVipEmail(vipEmail);
    setFormData({
      emailAddress: vipEmail.emailAddress || '',
      displayName: vipEmail.displayName || '',
      vipGroupId: vipEmail.vipGroupId || '',
      notifyUserId: vipEmail.notifyUserId || '',
      notifyRole: vipEmail.notifyRole || '',
      departmentId: vipEmail.departmentId || '',
      description: vipEmail.description || '',
      isActive: vipEmail.isActive !== false
    });
    setShowModal(true);
  };

  const handleDelete = async (vipEmailId: string) => {
    if (!window.confirm('Are you sure you want to delete this VIP email?')) {
      return;
    }

    try {
      setError(null);
      await emailApi.deleteVipEmail(vipEmailId);
      showSuccess('VIP email deleted successfully');
      await loadVipEmails();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to delete VIP email';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to delete VIP email:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.vipGroupId && !formData.notifyUserId && !formData.notifyRole) {
      showError('Either VIP Group, Notify User, or Notify Role must be selected');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      const submitData = {
        ...formData,
        vipGroupId: formData.vipGroupId || null,
        notifyUserId: formData.notifyUserId || null,
        notifyRole: formData.notifyRole || null,
        departmentId: formData.departmentId || null
      };

      if (editingVipEmail) {
        await emailApi.updateVipEmail(editingVipEmail.id, submitData);
        showSuccess('VIP email updated successfully');
      } else {
        await emailApi.createVipEmail(submitData);
        showSuccess('VIP email created successfully');
      }
      setShowModal(false);
      await loadVipEmails();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to save VIP email';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to save VIP email:', err);
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
        <LoadingSpinner message="Loading VIP emails..." fullPage />
      </div>
    );
  }

  const columns: TableColumn<VipEmail>[] = [
    { key: 'emailAddress', label: 'Email Address', sortable: true },
    { key: 'displayName', label: 'Display Name', sortable: true },
    { 
      key: 'vipGroupName', 
      label: 'VIP Group', 
      render: (value) => value ? (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-purple-500/20 text-purple-400">
          <Users className="h-3 w-3" />
          {value as string}
        </span>
      ) : '-'
    },
    { 
      key: 'notifyUserName', 
      label: 'Notify User', 
      render: (value) => (value as string) || '-'
    },
    { key: 'notifyRole', label: 'Notify Role', render: (value) => (value as string) || '-' },
    { 
      key: 'departmentName', 
      label: 'Department', 
      render: (value) => value ? (
        <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-blue-500/20 text-blue-400">
          <Building2 className="h-3 w-3" />
          {value as string}
        </span>
      ) : <span className="text-slate-400">-</span>
    },
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

  const roleOptions: SelectOption[] = [
    { value: '', label: 'Select Role' },
    { value: 'Admin', label: 'Admin' },
    { value: 'Manager', label: 'Manager' },
    { value: 'Supervisor', label: 'Supervisor' },
    { value: 'Technician', label: 'Technician' }
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
            <span className="font-medium text-white text-sm">How VIP Emails Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  What
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Exact email addresses</li>
                  <li>• From important contacts</li>
                  <li>• Get priority processing</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Groups
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Assign to VIP Groups</li>
                  <li>• Inherit notifications</li>
                  <li>• Or override per-email</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Notifications
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Notify specific user</li>
                  <li>• Or notify by role</li>
                  <li>• Alerts when VIP emails</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Example
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <code className="bg-slate-700 px-0.5 rounded text-[10px]">ceo@time.com.my</code></li>
                  <li>• Group: "Executive VIP"</li>
                  <li>• Notify: Manager role</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Header */}
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <Mail className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">VIP Emails</h1>
          <span className="text-sm text-slate-400">({vipEmails.length} contacts)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1 bg-slate-800 rounded-lg p-1">
            <button
              onClick={() => setViewMode('grouped')}
              className={`px-3 py-1.5 text-sm rounded transition-colors ${
                viewMode === 'grouped'
                  ? 'bg-brand-500 text-white'
                  : 'text-slate-400 hover:text-slate-300'
              }`}
            >
              Grouped
            </button>
            <button
              onClick={() => setViewMode('flat')}
              className={`px-3 py-1.5 text-sm rounded transition-colors ${
                viewMode === 'flat'
                  ? 'bg-brand-500 text-white'
                  : 'text-slate-400 hover:text-slate-300'
              }`}
            >
              Flat
            </button>
          </div>
          <Button onClick={handleAdd} variant="primary">
            <Plus className="h-4 w-4 mr-2" />
            Add VIP Email
          </Button>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <Card className="mb-4 p-4 bg-red-500/10 border-red-500/30">
          <p className="text-red-400">{error}</p>
        </Card>
      )}

      {/* VIP Emails Display */}
      {vipEmails.length === 0 ? (
        <EmptyState
          icon={<Mail className="h-12 w-12" />}
          title="No VIP Emails"
          description="Add VIP email addresses to receive notifications when important emails arrive."
          action={
            <Button onClick={handleAdd} variant="primary">
              <Plus className="h-4 w-4 mr-2" />
              Add VIP Email
            </Button>
          }
        />
      ) : viewMode === 'grouped' ? (
        <div className="space-y-4">
          {/* Group emails by VIP Group */}
          {(() => {
            const grouped = vipEmails.reduce((acc, email) => {
              const groupKey = email.vipGroupId || 'no-group';
              if (!acc[groupKey]) {
                acc[groupKey] = {
                  group: vipGroups.find(g => g.id === groupKey),
                  emails: []
                };
              }
              acc[groupKey].emails.push(email);
              return acc;
            }, {} as Record<string, { group?: VipGroup; emails: VipEmail[] }>);

            return Object.entries(grouped).map(([key, { group, emails }]) => (
              <Card key={key} className="bg-layout-card">
                <div className="p-4 border-b border-slate-700">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Users className="h-5 w-5 text-purple-400" />
                      <h3 className="text-lg font-semibold text-white">
                        {group ? group.name : 'No Group'}
                      </h3>
                      {group && (
                        <span className="text-xs text-slate-400">({group.code})</span>
                      )}
                    </div>
                    <span className="text-sm text-slate-400">
                      {emails.length} {emails.length === 1 ? 'email' : 'emails'}
                    </span>
                  </div>
                  {group?.description && (
                    <p className="text-sm text-slate-400 mt-1">{group.description}</p>
                  )}
                </div>
                <div className="p-4">
                  <DataTable
                    columns={columns.filter(c => c.key !== 'vipGroupName')}
                    data={emails}
                    className="bg-transparent"
                  />
                </div>
              </Card>
            ));
          })()}
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={vipEmails}
          className="bg-layout-card"
        />
      )}

      {/* Add/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => !saving && setShowModal(false)}
        title={editingVipEmail ? 'Edit VIP Email' : 'Add VIP Email'}
        size="lg"
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Email Address *"
              name="emailAddress"
              type="email"
              value={formData.emailAddress}
              onChange={handleInputChange}
              required
              placeholder="exact@email.com"
            />

            <TextInput
              label="Display Name"
              name="displayName"
              value={formData.displayName}
              onChange={handleInputChange}
              placeholder="e.g., John Doe, CEO"
            />
          </div>

          <Card className="p-4 bg-slate-800/50 border-slate-700">
            <h3 className="text-sm font-semibold text-slate-200 mb-3 flex items-center gap-2">
              <Users className="h-4 w-4" />
              Notification Settings
            </h3>
            <p className="text-xs text-slate-400 mb-3">
              Select a VIP Group to inherit its notification settings, or override with specific user/role.
            </p>
            
            <div className="grid grid-cols-2 gap-4">
              <Select
                label="VIP Group"
                name="vipGroupId"
                value={formData.vipGroupId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: 'Select VIP Group (optional)' },
                  ...vipGroups.map(g => ({ value: g.id, label: `${g.name} (${g.code})` }))
                ]}
              />

              <Select
                label="Department"
                name="departmentId"
                value={formData.departmentId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: '-- No Department --' },
                  ...departments.map(d => ({ value: d.id, label: d.name }))
                ]}
              />
            </div>

            <div className="grid grid-cols-2 gap-4 mt-4">
              <Select
                label="Notify User (override)"
                name="notifyUserId"
                value={formData.notifyUserId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: 'Select User' },
                  ...users.map(u => ({ value: u.id, label: u.fullName || u.email }))
                ]}
              />

              <Select
                label="Notify Role (override)"
                name="notifyRole"
                value={formData.notifyRole}
                onChange={handleInputChange}
                options={roleOptions}
              />
            </div>
          </Card>

          <TextInput
            label="Description"
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            as="textarea"
            rows={2}
            placeholder="Notes about this VIP contact..."
          />

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

          <div className="flex justify-end gap-2 pt-4 border-t border-slate-700">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setShowModal(false)}
              disabled={saving}
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
                  {editingVipEmail ? 'Update' : 'Create'}
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default VipEmailsPage;

