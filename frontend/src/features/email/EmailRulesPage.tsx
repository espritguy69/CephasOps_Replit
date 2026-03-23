import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Mail, Save, X, Lightbulb, ChevronDown, ChevronUp } from 'lucide-react';
import * as emailApi from '../../api/email';
import { LoadingSpinner, EmptyState, Button, Card, Modal, TextInput, Select, DataTable, StatusBadge, useToast } from '../../components/ui';
import type { EmailRule, EmailRuleFormData, TableColumn } from '../../types/email';

// ============================================================================
// Component
// ============================================================================

const EmailRulesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [rules, setRules] = useState<EmailRule[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingRule, setEditingRule] = useState<EmailRule | null>(null);
  const [showGuide, setShowGuide] = useState(true);
  const [formData, setFormData] = useState<EmailRuleFormData>({
    name: '',
    description: '',
    senderPattern: '',
    domainPattern: '',
    subjectPattern: '',
    bodyPattern: '',
    action: 'Process',
    targetDepartmentId: null,
    targetUserId: null,
    isVip: false,
    autoApprove: false,
    priority: 0,
    isActive: true
  });

  useEffect(() => {
    loadRules();
  }, []);

  const loadRules = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await emailApi.getEmailRules();
      const sorted = [...(data || [])].sort((a: EmailRule, b: EmailRule) => (b.priority || 0) - (a.priority || 0));
      setRules(sorted);
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error('Failed to load rules:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = () => {
    setEditingRule(null);
    setFormData({
      name: '',
      description: '',
      senderPattern: '',
      domainPattern: '',
      subjectPattern: '',
      bodyPattern: '',
      action: 'Process',
      targetDepartmentId: null,
      targetUserId: null,
      isVip: false,
      autoApprove: false,
      priority: 0,
      isActive: true
    });
    setShowModal(true);
  };

  const handleEdit = (rule: EmailRule) => {
    setEditingRule(rule);
    setFormData({
      name: rule.name || '',
      description: rule.description || '',
      senderPattern: rule.senderPattern || '',
      domainPattern: (rule as any).domainPattern || '',
      subjectPattern: rule.subjectPattern || '',
      bodyPattern: rule.bodyPattern || '',
      action: rule.action || 'Process',
      targetDepartmentId: rule.targetDepartmentId || null,
      targetUserId: rule.targetUserId || null,
      isVip: (rule as any).isVip || false,
      autoApprove: rule.autoApprove || false,
      priority: rule.priority || 0,
      isActive: rule.isActive !== false
    });
    setShowModal(true);
  };

  const handleDelete = async (ruleId: string) => {
    if (!window.confirm('Are you sure you want to delete this rule?')) {
      return;
    }

    try {
      setError(null);
      await emailApi.deleteEmailRule(ruleId);
      showSuccess('Email rule deleted successfully');
      await loadRules();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to delete rule';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to delete rule:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      showError('Rule name is required');
      return;
    }

    if (!formData.senderPattern && !formData.domainPattern && !formData.subjectPattern) {
      showError('At least one pattern (Sender, Domain, or Subject) must be filled');
      return;
    }

    try {
      setSaving(true);
      setError(null);
      if (editingRule) {
        await emailApi.updateEmailRule(editingRule.id, formData);
        showSuccess('Email rule updated successfully');
      } else {
        await emailApi.createEmailRule(formData);
        showSuccess('Email rule created successfully');
      }
      setShowModal(false);
      await loadRules();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to save rule';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to save rule:', err);
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
      [name]: type === 'checkbox' ? checked : (value === '' ? (type === 'number' ? 0 : '') : value)
    }));
  };

  const getActionLabel = (action: string) => {
    switch (action) {
      case 'Process': return 'Process (Create Order)';
      case 'Ignore': return 'Ignore (Skip)';
      case 'MarkVipOnly': return 'Mark VIP Only';
      case 'RouteToDepartment': return 'Route to Department';
      case 'RouteToUser': return 'Route to User';
      case 'MarkVipAndRouteToDepartment': return 'VIP + Route to Dept';
      case 'MarkVipAndRouteToUser': return 'VIP + Route to User';
      default: return action;
    }
  };

  const getActionBadgeColor = (action: string): 'success' | 'secondary' | 'warning' | 'info' => {
    switch (action) {
      case 'Process': return 'success';
      case 'Ignore': return 'secondary';
      case 'MarkVipOnly': return 'warning';
      case 'RouteToDepartment': return 'info';
      case 'RouteToUser': return 'info';
      case 'MarkVipAndRouteToDepartment': return 'warning';
      case 'MarkVipAndRouteToUser': return 'warning';
      default: return 'info';
    }
  };

  if (loading) {
    return (
      <div className="flex-1 p-6">
        <LoadingSpinner message="Loading email rules..." fullPage />
      </div>
    );
  }

  const columns: TableColumn<EmailRule>[] = [
    { key: 'priority', label: 'Priority', sortable: true, render: (value) => (
      <span className="font-mono bg-slate-700 px-2 py-1 rounded">{(value as number) || 0}</span>
    )},
    { key: 'name', label: 'Rule Name', render: (value, item) => (
      <div>
        <span className="font-medium text-white">{value as string}</span>
        {item.description && <p className="text-xs text-slate-400 mt-0.5">{item.description}</p>}
      </div>
    )},
    { key: 'senderPattern', label: 'Sender Pattern', render: (value) => value ? (
      <code className="text-xs bg-slate-700 px-2 py-1 rounded text-green-400">{value as string}</code>
    ) : <span className="text-slate-500">-</span> },
    { key: 'domainPattern', label: 'Domain Pattern', render: (value, item) => {
      const domain = (item as any).domainPattern;
      return domain ? (
        <code className="text-xs bg-slate-700 px-2 py-1 rounded text-purple-400">{domain}</code>
      ) : <span className="text-slate-500">-</span>;
    }},
    { key: 'subjectPattern', label: 'Subject Pattern', render: (value) => value ? (
      <code className="text-xs bg-slate-700 px-2 py-1 rounded text-blue-400">{value as string}</code>
    ) : <span className="text-slate-500">-</span> },
    {
      key: 'action',
      label: 'Action',
      render: (value) => (
        <StatusBadge status={getActionLabel(value as string)} variant={getActionBadgeColor(value as string)} size="sm" />
      )
    },
    {
      key: 'isVip',
      label: 'VIP',
      render: (value, item) => {
        const isVip = (item as any).isVip;
        return isVip ? (
          <StatusBadge status="VIP" variant="warning" size="sm" />
        ) : <span className="text-slate-500">-</span>;
      }
    },
    {
      key: 'isActive',
      label: 'Status',
      render: (value) => (
        <StatusBadge
          status={value ? 'Active' : 'Inactive'}
          variant={value ? 'success' : 'secondary'}
          size="sm"
        />
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, item) => (
        <div className="flex gap-1">
          <button onClick={() => handleEdit(item)} className="p-1.5 text-blue-400 hover:text-blue-300 hover:bg-slate-700 rounded" title="Edit">
            <Edit className="h-4 w-4" />
          </button>
          <button onClick={() => handleDelete(item.id)} className="p-1.5 text-red-400 hover:text-red-300 hover:bg-slate-700 rounded" title="Delete">
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      )
    }
  ];

  return (
    <div className="flex-1 p-6 space-y-6">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Email Rules Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Patterns
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Sender: <code className="bg-slate-700 px-0.5 rounded text-[10px]">*@time.com.my</code></li>
                  <li>• Subject: <code className="bg-slate-700 px-0.5 rounded text-[10px]">FTTH|FTTO</code></li>
                  <li>• <code className="bg-slate-700 px-0.5 rounded text-[10px]">*</code> wildcard, <code className="bg-slate-700 px-0.5 rounded text-[10px]">|</code> for OR</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Actions
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>Process:</strong> Create order</li>
                  <li>• <strong>Skip:</strong> Ignore email</li>
                  <li>• <strong>Flag:</strong> Manual review</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Priority
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Higher = evaluated first</li>
                  <li>• <code className="bg-slate-700 px-0.5 rounded text-[10px]">200+</code> spam filters</li>
                  <li>• <code className="bg-slate-700 px-0.5 rounded text-[10px]">100</code> main rules</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Examples
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• TIME: <code className="bg-slate-700 px-0.5 rounded text-[10px]">*@time.com.my</code></li>
                  <li>• Spam: Subject <code className="bg-slate-700 px-0.5 rounded text-[10px]">unsubscribe</code></li>
                  <li>• Auto-approve for VIPs</li>
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
          <h1 className="text-2xl font-bold text-white">Email Rules</h1>
          <span className="text-sm text-slate-400">({rules.length} rules)</span>
        </div>
        <Button onClick={handleAdd}>
          <Plus className="h-4 w-4 mr-2" />
          Add Rule
        </Button>
      </div>

      {error && (
        <div className="rounded-lg border border-red-500/50 bg-red-900/20 p-4 text-red-300" role="alert">
          {error}
        </div>
      )}

      {rules.length > 0 ? (
        <Card>
          <DataTable
            columns={columns}
            data={rules}
            emptyMessage="No rules configured"
          />
        </Card>
      ) : (
        <EmptyState
          title="No rules configured"
          description="Add email routing rules to manage how emails are processed."
        />
      )}

      <Modal
        isOpen={showModal}
        onClose={() => !saving && setShowModal(false)}
        title={editingRule ? 'Edit Email Rule' : 'Add Email Rule'}
        size="lg"
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <TextInput
            label="Rule Name"
            name="name"
            value={formData.name}
            onChange={handleInputChange}
            placeholder="e.g., TIME FTTH Orders"
            required
          />

          <TextInput
            label="Description (optional)"
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            placeholder="Brief description of what this rule does"
          />

          <div className="border-t border-slate-700 pt-4">
            <h4 className="text-sm font-medium text-slate-300 mb-3">Pattern Matching (at least one required)</h4>
            
            <div className="space-y-3">
              <TextInput
                label="Sender Pattern"
                name="senderPattern"
                value={formData.senderPattern}
                onChange={handleInputChange}
                placeholder="e.g., *@time.com.my or noreply@*"
              />
              <p className="text-xs text-slate-500 -mt-2">Use * as wildcard. Examples: *@time.com.my, john.doe@*, *procurement*</p>

              <TextInput
                label="Domain Pattern (optional)"
                name="domainPattern"
                value={(formData as any).domainPattern || ''}
                onChange={handleInputChange}
                placeholder="e.g., @time.com.my or @celcom.com.my"
              />
              <p className="text-xs text-slate-500 -mt-2">Match any email from this domain. Example: @time.com.my</p>

              <TextInput
                label="Subject Pattern"
                name="subjectPattern"
                value={formData.subjectPattern}
                onChange={handleInputChange}
                placeholder="e.g., FTTH|FTTO|Activation"
              />
              <p className="text-xs text-slate-500 -mt-2">Use | for OR. Case-insensitive.</p>
            </div>
          </div>

          <div className="border-t border-slate-700 pt-4 grid grid-cols-2 gap-4">
            <Select
              label="Action"
              name="action"
              value={formData.action}
              onChange={handleInputChange}
              options={[
                { value: 'Process', label: 'Process (Create Order)' },
                { value: 'Ignore', label: 'Ignore (Skip Email)' },
                { value: 'MarkVipOnly', label: 'Mark VIP Only' },
                { value: 'RouteToDepartment', label: 'Route to Department' },
                { value: 'RouteToUser', label: 'Route to User' },
                { value: 'MarkVipAndRouteToDepartment', label: 'VIP + Route to Department' },
                { value: 'MarkVipAndRouteToUser', label: 'VIP + Route to User' }
              ]}
            />

            <TextInput
              label="Priority"
              name="priority"
              type="number"
              value={formData.priority}
              onChange={handleInputChange}
              required
            />
          </div>

          {(formData.action === 'RouteToDepartment' || formData.action === 'MarkVipAndRouteToDepartment') && (
            <TextInput
              label="Target Department ID (optional)"
              name="targetDepartmentId"
              value={formData.targetDepartmentId || ''}
              onChange={handleInputChange}
              placeholder="Enter department ID"
            />
          )}

          {(formData.action === 'RouteToUser' || formData.action === 'MarkVipAndRouteToUser') && (
            <TextInput
              label="Target User ID (optional)"
              name="targetUserId"
              value={formData.targetUserId || ''}
              onChange={handleInputChange}
              placeholder="Enter user ID"
            />
          )}

          <div className="flex items-center gap-6 pt-2">
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                name="isVip"
                id="isVip"
                checked={(formData as any).isVip || false}
                onChange={handleInputChange}
                className="h-4 w-4 rounded border-slate-600 bg-slate-700"
              />
              <label htmlFor="isVip" className="text-sm text-slate-300">Mark as VIP</label>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                name="isActive"
                id="isActive"
                checked={formData.isActive}
                onChange={handleInputChange}
                className="h-4 w-4 rounded border-slate-600 bg-slate-700"
              />
              <label htmlFor="isActive" className="text-sm text-slate-300">Active</label>
            </div>
          </div>

          <div className="flex gap-3 justify-end pt-4 border-t border-slate-700">
            <Button
              type="button"
              variant="ghost"
              onClick={() => setShowModal(false)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  {editingRule ? 'Updating...' : 'Creating...'}
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  {editingRule ? 'Update Rule' : 'Create Rule'}
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default EmailRulesPage;

