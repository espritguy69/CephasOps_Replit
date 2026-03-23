import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Cog, Save, X, ToggleLeft, ToggleRight, Lightbulb, ChevronDown, ChevronUp, TestTube, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import * as emailApi from '../../api/email';
import * as departmentsApi from '../../api/departments';
import { LoadingSpinner, EmptyState, Button, Card, Modal, TextInput, Select, DataTable, StatusBadge, useToast } from '../../components/ui';
import type { ParserTemplate, ParserTemplateFormData, Department, EmailMailbox, TableColumn, SelectOption } from '../../types/email';
import type { ParserTemplateTestData, ParserTemplateTestResult } from '../../api/email';

// ============================================================================
// Component
// ============================================================================

const ParserTemplatesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [templates, setTemplates] = useState<ParserTemplate[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [mailboxes, setMailboxes] = useState<EmailMailbox[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<ParserTemplate | null>(null);
  const [showGuide, setShowGuide] = useState(true);
  const [showTestModal, setShowTestModal] = useState(false);
  const [testingTemplate, setTestingTemplate] = useState<ParserTemplate | null>(null);
  const [testData, setTestData] = useState<ParserTemplateTestData>({
    fromAddress: '',
    subject: '',
    body: '',
    hasAttachments: false,
    attachmentFileNames: []
  });
  const [testResult, setTestResult] = useState<ParserTemplateTestResult | null>(null);
  const [testing, setTesting] = useState(false);
  const [formData, setFormData] = useState<ParserTemplateFormData>({
    name: '',
    code: '',
    emailAccountId: '',
    partnerPattern: '',
    subjectPattern: '',
    orderTypeCode: '',
    defaultDepartmentId: '',
    autoApprove: false,
    priority: 0,
    isActive: true,
    description: ''
  });

  useEffect(() => {
    loadTemplates();
    loadDepartments();
    loadMailboxes();
  }, []);

  const loadDepartments = async () => {
    try {
      const data = await departmentsApi.getDepartments({ isActive: true });
      setDepartments(data || []);
    } catch (err) {
      console.error('Failed to load departments:', err);
    }
  };

  const loadMailboxes = async () => {
    try {
      const data = await emailApi.getEmailAccounts();
      setMailboxes(data || []);
    } catch (err) {
      console.error('Failed to load mailboxes:', err);
    }
  };

  const loadTemplates = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await emailApi.getParserTemplates();
      const sorted = [...data].sort((a: ParserTemplate, b: ParserTemplate) => (b.priority || 0) - (a.priority || 0));
      setTemplates(sorted);
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error('Failed to load parser templates:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = () => {
    setEditingTemplate(null);
    setFormData({
      name: '',
      code: '',
      emailAccountId: '',
      partnerPattern: '',
      subjectPattern: '',
      orderTypeCode: '',
      defaultDepartmentId: '',
      autoApprove: false,
      priority: 0,
      isActive: true,
      description: ''
    });
    setShowModal(true);
  };

  const handleEdit = (template: ParserTemplate) => {
    setEditingTemplate(template);
    setFormData({
      name: template.name || '',
      code: template.code || '',
      emailAccountId: template.emailAccountId || '',
      partnerPattern: template.partnerPattern || '',
      subjectPattern: template.subjectPattern || '',
      orderTypeCode: template.orderTypeCode || '',
      defaultDepartmentId: template.defaultDepartmentId || '',
      autoApprove: template.autoApprove || false,
      priority: template.priority || 0,
      isActive: template.isActive !== false,
      description: template.description || ''
    });
    setShowModal(true);
  };

  const handleDelete = async (templateId: string) => {
    if (!window.confirm('Are you sure you want to delete this parser template?')) {
      return;
    }

    try {
      setError(null);
      await emailApi.deleteParserTemplate(templateId);
      showSuccess('Parser template deleted successfully');
      await loadTemplates();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to delete parser template';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to delete parser template:', err);
    }
  };

  const handleTest = (template: ParserTemplate) => {
    setTestingTemplate(template);
    setTestData({
      fromAddress: template.partnerPattern || '',
      subject: template.subjectPattern || '',
      body: '',
      hasAttachments: false,
      attachmentFileNames: []
    });
    setTestResult(null);
    setShowTestModal(true);
  };

  const handleRunTest = async () => {
    if (!testingTemplate) return;

    try {
      setTesting(true);
      setTestResult(null);
      const result = await emailApi.testParserTemplate(testingTemplate.id, testData);
      setTestResult(result);
      if (result.matched) {
        showSuccess('Template matched successfully!');
      } else {
        showError(result.errorMessage || 'Template did not match');
      }
    } catch (err) {
      const error = err as Error;
      showError(error.message || 'Failed to test template');
      console.error('Failed to test template:', err);
    } finally {
      setTesting(false);
    }
  };

  const handleToggleAutoApprove = async (template: ParserTemplate) => {
    try {
      setError(null);
      await emailApi.toggleParserTemplateAutoApprove(template.id, !template.autoApprove);
      showSuccess(`Auto-approve ${!template.autoApprove ? 'enabled' : 'disabled'} for ${template.name}`);
      await loadTemplates();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to toggle auto-approve';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to toggle auto-approve:', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      showError('Template name is required');
      return;
    }
    if (!formData.code.trim()) {
      showError('Template code is required');
      return;
    }

    try {
      setSaving(true);
      setError(null);
      if (editingTemplate) {
        await emailApi.updateParserTemplate(editingTemplate.id, formData);
        showSuccess('Parser template updated successfully');
      } else {
        await emailApi.createParserTemplate(formData);
        showSuccess('Parser template created successfully');
      }
      setShowModal(false);
      await loadTemplates();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to save parser template';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to save parser template:', err);
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
        <LoadingSpinner message="Loading parser templates..." fullPage />
      </div>
    );
  }

  const getDepartmentName = (deptId: string | undefined): string => {
    if (!deptId) return '-';
    const dept = departments.find(d => d.id === deptId);
    return dept ? dept.name : '-';
  };

  const getMailboxName = (emailAccountId: string | undefined): React.ReactNode => {
    if (!emailAccountId) return <span className="text-slate-400 italic">All Mailboxes</span>;
    const mailbox = mailboxes.find(m => m.id === emailAccountId);
    return mailbox ? mailbox.name : '-';
  };

  const columns: TableColumn<ParserTemplate>[] = [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'code', label: 'Code', sortable: true },
    { 
      key: 'emailAccountId', 
      label: 'Mailbox', 
      render: (value, item) => item.emailAccountName || getMailboxName(value as string)
    },
    { key: 'partnerPattern', label: 'Partner Pattern', render: (value) => (value as string) || '-' },
    { key: 'subjectPattern', label: 'Subject Pattern', render: (value) => (value as string) || '-' },
    { key: 'orderTypeCode', label: 'Order Type', render: (value) => (value as string) || '-' },
    { key: 'defaultDepartmentId', label: 'Department', render: (value) => getDepartmentName(value as string) },
    { key: 'priority', label: 'Priority', sortable: true },
    {
      key: 'autoApprove',
      label: 'Auto-Approve',
      render: (value, item) => (
        <button
          onClick={() => handleToggleAutoApprove(item)}
          className={`flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-colors ${
            value 
              ? 'bg-green-500/20 text-green-400 hover:bg-green-500/30' 
              : 'bg-slate-500/20 text-slate-400 hover:bg-slate-500/30'
          }`}
          title={value ? 'Click to disable auto-approve' : 'Click to enable auto-approve'}
        >
          {value ? <ToggleRight className="h-4 w-4" /> : <ToggleLeft className="h-4 w-4" />}
          {value ? 'ON' : 'OFF'}
        </button>
      )
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
            onClick={() => handleTest(item)}
            className="p-1 hover:bg-blue-500/20 rounded"
            title="Test Template"
          >
            <TestTube className="h-4 w-4 text-blue-400" />
          </button>
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

  const orderTypeOptions: SelectOption[] = [
    { value: '', label: 'Select Order Type' },
    { value: 'ACTIVATION', label: 'Activation' },
    { value: 'MODIFICATION_INDOOR', label: 'Modification Indoor' },
    { value: 'MODIFICATION_OUTDOOR', label: 'Modification Outdoor' },
    { value: 'ASSURANCE', label: 'Assurance' },
    { value: 'RESCHEDULE', label: 'Reschedule' },
    { value: 'VALUE_ADDED_SERVICE', label: 'Value Added Service' }
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
            <span className="font-medium text-white text-sm">How Parser Templates Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-5 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-cyan-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Mailbox Routing
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>All Mailboxes:</strong> Any email</li>
                  <li>• <strong>Specific:</strong> Only that mailbox</li>
                  <li>• Route orders vs VIP emails</li>
                </ul>
              </div>

              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Matching
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Partner: <code className="bg-slate-700 px-0.5 rounded text-[10px]">*@time.com.my</code></li>
                  <li>• Subject: <code className="bg-slate-700 px-0.5 rounded text-[10px]">FTTH</code></li>
                  <li>• <code className="bg-slate-700 px-0.5 rounded text-[10px]">*</code> wildcard supported</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Order Type
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Activation, Modification</li>
                  <li>• Assurance, Reschedule</li>
                  <li>• Auto-assigns type</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Auto-Approve
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• OFF: Human review</li>
                  <li>• ON: Skip review</li>
                  <li>• Enable after testing</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">5</span>
                  Workflow
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Start with auto-approve OFF</li>
                  <li>• Test parser accuracy</li>
                  <li>• Enable when confident</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Header */}
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <Cog className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">Parser Templates</h1>
          <span className="text-sm text-slate-400">({templates.length} templates)</span>
        </div>
        <Button onClick={handleAdd} variant="primary">
          <Plus className="h-4 w-4 mr-2" />
          Add Parser Template
        </Button>
      </div>

      {/* Error Display */}
      {error && (
        <Card className="mb-4 p-4 bg-red-500/10 border-red-500/30">
          <p className="text-red-400">{error}</p>
        </Card>
      )}

      {/* Templates Table */}
      {templates.length === 0 ? (
        <EmptyState
          icon={<Cog className="h-12 w-12" />}
          title="No Parser Templates"
          description="Create parser templates to automatically process incoming emails from partners."
          action={
            <Button onClick={handleAdd} variant="primary">
              <Plus className="h-4 w-4 mr-2" />
              Add Parser Template
            </Button>
          }
        />
      ) : (
        <DataTable
          columns={columns}
          data={templates}
          className="bg-layout-card"
        />
      )}

      {/* Add/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title={editingTemplate ? 'Edit Parser Template' : 'Add Parser Template'}
        size="lg"
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Mailbox Routing</h3>
            <Select
              label="Restrict to Mailbox"
              name="emailAccountId"
              value={formData.emailAccountId}
              onChange={handleInputChange}
              options={[
                { value: '', label: '-- All Mailboxes (applies to any mailbox) --' },
                ...mailboxes.map(m => ({ value: m.id, label: `${m.name} (${m.username})` }))
              ]}
            />
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
              Select a specific mailbox to restrict this template, or leave as "All Mailboxes" to apply to any mailbox.
            </p>
          </div>

          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Template Information</h3>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Template Name *"
              name="name"
              value={formData.name}
              onChange={handleInputChange}
              placeholder="e.g., TIME FTTH Activation"
              required
            />

            <TextInput
              label="Template Code *"
              name="code"
              value={formData.code}
              onChange={handleInputChange}
              placeholder="e.g., TIME_FTTH"
              required
            />
          </div>

          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Matching Rules</h3>
            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Partner Pattern"
                name="partnerPattern"
                value={formData.partnerPattern}
                onChange={handleInputChange}
                placeholder="e.g., *@time.com.my or %@time.com.my"
              />

              <TextInput
                label="Subject Pattern"
                name="subjectPattern"
                value={formData.subjectPattern}
                onChange={handleInputChange}
                placeholder="e.g., FTTH|Activation|Service Order"
              />
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-2">
              Patterns use wildcards: <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">*</code> or <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">%</code> for any characters. 
              Subject patterns support multiple keywords separated by <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">|</code> (OR logic).
            </p>
          </div>

          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Order Assignment</h3>
            <div className="grid grid-cols-2 gap-4">
              <Select
                label="Order Type"
                name="orderTypeCode"
                value={formData.orderTypeCode}
                onChange={handleInputChange}
                options={orderTypeOptions}
              />

              <Select
                label="Default Department"
                name="defaultDepartmentId"
                value={formData.defaultDepartmentId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: '-- No Default Department --' },
                  ...departments.map(d => ({ value: d.id, label: d.name }))
                ]}
              />
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-2">
              Department to assign orders created from this template. Can be overridden by email routing rules.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <TextInput
              label="Priority"
              name="priority"
              type="number"
              value={formData.priority}
              onChange={handleInputChange}
            />
          </div>

          <TextInput
            label="Description"
            name="description"
            value={formData.description}
            onChange={handleInputChange}
            as="textarea"
            rows={2}
            placeholder="Describe what this parser handles..."
          />

          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                name="autoApprove"
                checked={formData.autoApprove}
                onChange={handleInputChange}
                className="h-4 w-4 rounded border-gray-300"
              />
              <label className="text-sm font-medium">
                Auto-Approve
                <span className="text-slate-400 font-normal ml-1">
                  (automatically create orders without human review)
                </span>
              </label>
            </div>

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

          <Card className="p-4 bg-amber-500/10 border-amber-500/30">
            <p className="text-sm text-amber-300">
              <strong>Tip:</strong> Start with Auto-Approve OFF while testing the parser. 
              Once you&apos;re confident the parser works correctly, enable Auto-Approve to reduce manual review.
            </p>
          </Card>

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
            <Button type="submit" variant="primary" disabled={saving}>
              {saving ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  {editingTemplate ? 'Update' : 'Create'}
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Test Template Modal */}
      <Modal
        isOpen={showTestModal}
        onClose={() => {
          setShowTestModal(false);
          setTestResult(null);
          setTestingTemplate(null);
        }}
        title={`Test Template: ${testingTemplate?.name || ''}`}
        size="lg"
      >
        <div className="space-y-4">
          <div className="bg-blue-500/10 border border-blue-500/30 rounded p-3">
            <p className="text-sm text-blue-300">
              Enter sample email data to test if this template will match. This helps verify your pattern rules before using them in production.
            </p>
          </div>

          <div className="space-y-4">
            <TextInput
              label="FROM Address *"
              name="fromAddress"
              value={testData.fromAddress}
              onChange={(e) => setTestData({ ...testData, fromAddress: e.target.value })}
              placeholder="e.g., noreply@time.com.my"
              required
            />

            <TextInput
              label="Subject *"
              name="subject"
              value={testData.subject}
              onChange={(e) => setTestData({ ...testData, subject: e.target.value })}
              placeholder="e.g., FTTH Activation Work Order"
              required
            />

            <div>
              <label className="block text-sm font-medium mb-2">Email Body (Optional)</label>
              <textarea
                name="body"
                value={testData.body}
                onChange={(e) => setTestData({ ...testData, body: e.target.value })}
                placeholder="Sample email body text..."
                rows={4}
                className="w-full px-3 py-2 bg-slate-800 border border-slate-700 rounded text-sm text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="hasAttachments"
                checked={testData.hasAttachments}
                onChange={(e) => setTestData({ ...testData, hasAttachments: e.target.checked })}
                className="h-4 w-4 rounded border-gray-300"
              />
              <label htmlFor="hasAttachments" className="text-sm font-medium">
                Email has attachments
              </label>
            </div>

            {testData.hasAttachments && (
              <TextInput
                label="Attachment File Names (comma-separated)"
                name="attachmentFileNames"
                value={testData.attachmentFileNames?.join(', ') || ''}
                onChange={(e) => setTestData({
                  ...testData,
                  attachmentFileNames: e.target.value.split(',').map(s => s.trim()).filter(s => s)
                })}
                placeholder="e.g., order.xlsx, invoice.pdf"
              />
            )}
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t border-slate-700">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setShowTestModal(false);
                setTestResult(null);
                setTestingTemplate(null);
              }}
              disabled={testing}
            >
              <X className="h-4 w-4 mr-2" />
              Close
            </Button>
            <Button
              type="button"
              variant="primary"
              onClick={handleRunTest}
              disabled={testing || !testData.fromAddress || !testData.subject}
            >
              {testing ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Testing...
                </>
              ) : (
                <>
                  <TestTube className="h-4 w-4 mr-2" />
                  Run Test
                </>
              )}
            </Button>
          </div>

          {/* Test Results */}
          {testResult && (
            <div className="mt-6 pt-4 border-t border-slate-700">
              <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                {testResult.matched ? (
                  <>
                    <CheckCircle className="h-5 w-5 text-green-400" />
                    <span className="text-green-400">Template Matched!</span>
                  </>
                ) : (
                  <>
                    <XCircle className="h-5 w-5 text-red-400" />
                    <span className="text-red-400">Template Did Not Match</span>
                  </>
                )}
              </h3>

              {testResult.matchDetails && (
                <div className="space-y-2 mb-4">
                  <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <span className="text-slate-400">FROM Pattern:</span>
                      <div className="mt-1">
                        {testResult.matchDetails.fromAddressMatched ? (
                          <span className="text-green-400 flex items-center gap-1">
                            <CheckCircle className="h-4 w-4" />
                            Matched
                          </span>
                        ) : (
                          <span className="text-red-400 flex items-center gap-1">
                            <XCircle className="h-4 w-4" />
                            Not Matched
                          </span>
                        )}
                        <div className="text-xs text-slate-500 mt-1">
                          Pattern: <code className="bg-slate-800 px-1 rounded">{testResult.matchDetails.fromAddressPattern || 'None'}</code>
                        </div>
                      </div>
                    </div>
                    <div>
                      <span className="text-slate-400">Subject Pattern:</span>
                      <div className="mt-1">
                        {testResult.matchDetails.subjectMatched ? (
                          <span className="text-green-400 flex items-center gap-1">
                            <CheckCircle className="h-4 w-4" />
                            Matched
                          </span>
                        ) : (
                          <span className="text-red-400 flex items-center gap-1">
                            <XCircle className="h-4 w-4" />
                            Not Matched
                          </span>
                        )}
                        <div className="text-xs text-slate-500 mt-1">
                          Pattern: <code className="bg-slate-800 px-1 rounded">{testResult.matchDetails.subjectPattern || 'None'}</code>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div className="text-sm">
                    <span className="text-slate-400">Priority:</span> <span className="text-white">{testResult.matchDetails.priority}</span>
                  </div>
                </div>
              )}

              {testResult.errorMessage && (
                <div className="bg-red-500/10 border border-red-500/30 rounded p-3 mb-4">
                  <div className="flex items-start gap-2">
                    <AlertCircle className="h-5 w-5 text-red-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-sm font-medium text-red-400 mb-1">Why it didn't match:</p>
                      <p className="text-sm text-red-300">{testResult.errorMessage}</p>
                    </div>
                  </div>
                </div>
              )}

              {testResult.extractedData && (
                <div className="bg-green-500/10 border border-green-500/30 rounded p-3">
                  <p className="text-sm font-medium text-green-400 mb-2">Sample Extracted Data:</p>
                  <pre className="text-xs bg-slate-900 p-2 rounded overflow-auto max-h-40">
                    {JSON.stringify(testResult.extractedData, null, 2)}
                  </pre>
                </div>
              )}
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};

export default ParserTemplatesPage;

