import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Mail, Save, X, Server, Wifi, Lightbulb, ChevronDown, ChevronUp, RefreshCw } from 'lucide-react';
import * as emailApi from '../../api/email';
import * as departmentApi from '../../api/departments';
import { LoadingSpinner, EmptyState, Button, Card, Modal, TextInput, Select, DataTable, StatusBadge, useToast, Switch } from '../../components/ui';
import type {
  EmailMailbox,
  EmailMailboxFormData,
  ConnectionTestResult,
  PollResult,
  Department,
  TableColumn,
  ParserTemplate
} from '../../types/email';

// ============================================================================
// Component
// ============================================================================

const EmailMailboxesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [mailboxes, setMailboxes] = useState<EmailMailbox[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [polling, setPolling] = useState<string | null>(null);
  const [showTestModal, setShowTestModal] = useState(false);
  const [showPollModal, setShowPollModal] = useState(false);
  const [pollResult, setPollResult] = useState<PollResult | null>(null);
  const [pollAllResults, setPollAllResults] = useState<PollResult[]>([]);
  const [testResult, setTestResult] = useState<ConnectionTestResult | null>(null);
  const [testMailbox, setTestMailbox] = useState<EmailMailbox | null>(null);
  const [editingMailbox, setEditingMailbox] = useState<EmailMailbox | null>(null);
  const [useSameSmtpCredentials, setUseSameSmtpCredentials] = useState(true);
  const [showGuide, setShowGuide] = useState(true);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [pollingAll, setPollingAll] = useState(false);
  const [parserTemplates, setParserTemplates] = useState<ParserTemplate[]>([]);
  const [formData, setFormData] = useState<EmailMailboxFormData>({
    name: '',
    emailAddress: '',
    provider: 'POP3',
    host: '',
    port: 995,
    useSsl: true,
    username: '',
    password: '',
    pollIntervalMinutes: 15,
    isActive: true,
    defaultDepartmentId: '',
    defaultParserTemplateId: '',
    smtpHost: '',
    smtpPort: 587,
    smtpUsername: '',
    smtpPassword: '',
    smtpUseSsl: false,
    smtpUseTls: true,
    smtpFromAddress: '',
    smtpFromName: ''
  });

  useEffect(() => {
    loadMailboxes();
    loadDepartments();
    loadParserTemplates();
  }, []);

  const loadDepartments = async () => {
    try {
      const data = await departmentApi.getDepartments();
      setDepartments(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Failed to load departments:', err);
    }
  };

  const loadParserTemplates = async () => {
    try {
      const data = await emailApi.getParserTemplates();
      setParserTemplates(Array.isArray(data) ? data.filter(t => t.isActive) : []);
    } catch (err) {
      console.error('Failed to load parser templates:', err);
    }
  };

  const loadMailboxes = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await emailApi.getEmailAccounts();
      const normalised = Array.isArray(data)
        ? data.map((mb: EmailMailbox) => ({
            ...mb,
            emailAddress: mb.emailAddress || mb.username || ''
          }))
        : [];
      setMailboxes(normalised);
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error('Failed to load mailboxes:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = () => {
    setEditingMailbox(null);
    setUseSameSmtpCredentials(true);
    setFormData({
      name: '',
      emailAddress: '',
      provider: 'POP3',
      host: '',
      port: 995,
      useSsl: true,
      username: '',
      password: '',
      pollIntervalMinutes: 15,
      isActive: true,
      defaultDepartmentId: '',
      defaultParserTemplateId: '',
      smtpHost: '',
      smtpPort: 587,
      smtpUsername: '',
      smtpPassword: '',
      smtpUseSsl: false,
      smtpUseTls: true,
      smtpFromAddress: '',
      smtpFromName: ''
    });
    setShowModal(true);
  };

  const handleEdit = (mailbox: EmailMailbox) => {
    setEditingMailbox(mailbox);
    const sameCredentials =
      (!mailbox.smtpHost || mailbox.smtpHost === mailbox.host) &&
      (!mailbox.smtpUsername || mailbox.smtpUsername === mailbox.username);

    setUseSameSmtpCredentials(sameCredentials);

    setFormData({
      name: mailbox.name || '',
      emailAddress: mailbox.emailAddress || mailbox.username || '',
      provider: mailbox.provider || 'POP3',
      host: mailbox.host || '',
      port: mailbox.port || 995,
      useSsl: mailbox.useSsl !== false,
      username: mailbox.username || '',
      password: '',
      pollIntervalMinutes: mailbox.pollIntervalMinutes || 15,
      isActive: mailbox.isActive !== false,
      defaultDepartmentId: mailbox.defaultDepartmentId || '',
      defaultParserTemplateId: mailbox.defaultParserTemplateId || '',
      smtpHost: mailbox.smtpHost || mailbox.host || '',
      smtpPort: mailbox.smtpPort || 587,
      smtpUsername: mailbox.smtpUsername || mailbox.username || '',
      smtpPassword: '',
      smtpUseSsl: mailbox.smtpUseSsl ?? false,
      smtpUseTls: mailbox.smtpUseTls ?? true,
      smtpFromAddress: mailbox.smtpFromAddress || mailbox.emailAddress || '',
      smtpFromName: mailbox.smtpFromName || mailbox.name || ''
    });
    setShowModal(true);
  };

  const handleDelete = async (mailboxId: string) => {
    if (!window.confirm('Are you sure you want to delete this mailbox?')) {
      return;
    }

    try {
      setError(null);
      await emailApi.deleteEmailAccount(mailboxId);
      showSuccess('Mailbox deleted successfully');
      await loadMailboxes();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to delete mailbox';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to delete mailbox:', err);
    }
  };

  const handleToggleActive = async (mailbox: EmailMailbox) => {
    try {
      setError(null);
      await emailApi.updateEmailAccount(mailbox.id, {
        ...mailbox,
        isActive: !mailbox.isActive
      });
      showSuccess(`Mailbox ${!mailbox.isActive ? 'activated' : 'deactivated'} successfully`);
      await loadMailboxes();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to update mailbox status';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to toggle mailbox status:', err);
    }
  };

  const handlePollEmails = async (mailbox: EmailMailbox) => {
    try {
      setPolling(mailbox.id);
      setShowPollModal(true);
      setPollResult(null);
      setPollAllResults([]);
      setError(null);
      
      const response = await emailApi.pollEmailAccount(mailbox.id);
      
      setPollResult(response);
      
      if (response.success) {
        showSuccess(`Polled ${response.emailsFetched || 0} emails, created ${response.parseSessionsCreated || 0} parse sessions`);
      } else {
        showError(response.errorMessage || 'Email polling failed');
      }
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to poll emails';
      setError(errorMessage);
      setPollResult({ success: false, errorMessage });
      showError(errorMessage);
    } finally {
      setPolling(null);
    }
  };

  const handlePollAllEmails = async () => {
    try {
      setPollingAll(true);
      setShowPollModal(true);
      setPollResult(null);
      setPollAllResults([]);
      setError(null);
      
      const responses = await emailApi.pollAllEmailAccounts();
      
      setPollAllResults(responses);
      
      const totalFetched = responses.reduce((sum, r) => sum + (r.emailsFetched || 0), 0);
      const totalSessions = responses.reduce((sum, r) => sum + (r.parseSessionsCreated || 0), 0);
      const successCount = responses.filter(r => r.success).length;
      
      if (successCount > 0) {
        showSuccess(`Polled ${totalFetched} emails from ${successCount} account(s), created ${totalSessions} parse sessions`);
      } else {
        showError('Email polling failed for all accounts');
      }
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to poll all emails';
      setError(errorMessage);
      showError(errorMessage);
    } finally {
      setPollingAll(false);
    }
  };

  const handleTestConnection = async (mailbox: EmailMailbox) => {
    try {
      setTestMailbox(mailbox);
      setShowTestModal(true);
      setTestResult(null);
      setTesting(true);
      setError(null);
      const response = await emailApi.testEmailAccountConnection(mailbox.id);

      const hasDetailedResult =
        response &&
        (typeof response.success === 'boolean' ||
          typeof response.incomingSuccess === 'boolean' ||
          typeof response.smtpSuccess === 'boolean');

      if (hasDetailedResult) {
        const overallSuccess =
          typeof response.success === 'boolean'
            ? response.success
            : !!(response.incomingSuccess && response.smtpSuccess);

        setTestResult({
          status: overallSuccess ? 'success' : 'error',
          message: response?.message || '',
          incomingSuccess: response?.incomingSuccess,
          incomingProtocol: response?.incomingProtocol,
          incomingResponseTimeMs: response?.incomingResponseTimeMs,
          incomingError: response?.incomingError,
          smtpSuccess: response?.smtpSuccess,
          smtpResponseTimeMs: response?.smtpResponseTimeMs,
          smtpError: response?.smtpError
        });

        if (overallSuccess) {
          showSuccess('Connection test successful!');
        } else {
          showError('Connection test failed. See details in the dialog.');
        }
      } else if (response && response.status === 'ok') {
        setTestResult({
          status: 'success',
          message: 'Connection test successful!'
        });
        showSuccess('Connection test successful!');
      } else {
        setTestResult({
          status: 'error',
          message: 'Connection test failed.'
        });
        showError('Connection test failed.');
      }
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Connection test failed';
      setError(errorMessage);
      setTestResult({
        status: 'error',
        message: errorMessage
      });
      showError(errorMessage);
    } finally {
      setTesting(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setSaving(true);
      setError(null);
      if (editingMailbox) {
        await emailApi.updateEmailAccount(editingMailbox.id, formData);
        showSuccess('Mailbox updated successfully');
      } else {
        await emailApi.createEmailAccount(formData);
        showSuccess('Mailbox created successfully');
      }
      setShowModal(false);
      await loadMailboxes();
    } catch (err) {
      const error = err as Error;
      const errorMessage = error.message || 'Failed to save mailbox';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Failed to save mailbox:', err);
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
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  // When using the same credentials flag, keep SMTP fields in sync with incoming settings
  useEffect(() => {
    if (useSameSmtpCredentials) {
      setFormData(prev => ({
        ...prev,
        smtpHost: prev.host,
        smtpUsername: prev.username,
        smtpPassword: prev.password
      }));
    }
  }, [useSameSmtpCredentials, formData.host, formData.username, formData.password]);

  if (loading) {
    return (
      <div className="flex-1 p-6">
        <LoadingSpinner message="Loading email mailboxes..." fullPage />
      </div>
    );
  }

  const columns: TableColumn<EmailMailbox>[] = [
    { key: 'name', label: 'Name', sortable: true },
    { key: 'emailAddress', label: 'Email Address', sortable: true },
    { key: 'provider', label: 'Provider' },
    { key: 'host', label: 'Host', render: (value) => (value as string) || '-' },
    { 
      key: 'defaultDepartmentName', 
      label: 'Department', 
      render: (value) => (value as string) || <span className="text-slate-400">-</span>
    },
    {
      key: 'defaultParserTemplateName',
      label: 'Default Template',
      render: (value) =>
        (value as string) ? (
          value as string
        ) : (
          <span className="text-slate-400">Auto-match</span>
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
        <div className="flex gap-2 items-center">
          <Switch
            checked={item.isActive}
            onChange={() => handleToggleActive(item)}
            label={item.isActive ? 'Active' : 'Inactive'}
          />
          <Button size="sm" variant="ghost" onClick={() => handleEdit(item)}>
            <Edit className="h-4 w-4 mr-2" />
            Edit
          </Button>
          <Button 
            size="sm" 
            variant="outline" 
            onClick={() => handleTestConnection(item)}
            disabled={testing}
          >
            <Wifi className="h-4 w-4 mr-2" />
            Test
          </Button>
          <Button 
            size="sm" 
            variant="primary" 
            onClick={() => handlePollEmails(item)}
            disabled={polling === item.id}
          >
            {polling === item.id ? (
              <LoadingSpinner size="sm" className="mr-2" />
            ) : (
              <RefreshCw className="h-4 w-4 mr-2" />
            )}
            Poll
          </Button>
          <Button size="sm" variant="ghost" onClick={() => handleDelete(item.id)}>
            <Trash2 className="h-4 w-4 mr-2 text-destructive" />
            Delete
          </Button>
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
            <span className="font-medium text-white text-sm">How Email Mailboxes Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-blue-500 rounded-full flex items-center justify-center text-[10px]">1</span>
                  Providers
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• <strong>POP3/IMAP:</strong> Standard mail</li>
                  <li>• <strong>O365:</strong> Microsoft 365</li>
                  <li>• <strong>Gmail:</strong> Google Workspace</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-green-500 rounded-full flex items-center justify-center text-[10px]">2</span>
                  Incoming
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• POP3: Port <code className="bg-slate-700 px-0.5 rounded text-[10px]">995</code> (SSL)</li>
                  <li>• IMAP: Port <code className="bg-slate-700 px-0.5 rounded text-[10px]">993</code> (SSL)</li>
                  <li>• Poll interval: 5-30 mins</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-purple-500 rounded-full flex items-center justify-center text-[10px]">3</span>
                  Outgoing (SMTP)
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Port <code className="bg-slate-700 px-0.5 rounded text-[10px]">587</code> (TLS) or <code className="bg-slate-700 px-0.5 rounded text-[10px]">465</code> (SSL)</li>
                  <li>• Used for notifications</li>
                  <li>• Can share credentials</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <span className="w-4 h-4 bg-orange-500 rounded-full flex items-center justify-center text-[10px]">4</span>
                  Tips
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Test connection after setup</li>
                  <li>• Use app passwords for Gmail</li>
                  <li>• Enable 2FA where possible</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <Server className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">Email Mailboxes</h1>
          <span className="text-sm text-slate-400">({mailboxes.length} configured)</span>
        </div>
        <div className="flex gap-2">
          {mailboxes.length > 0 && (
            <Button 
              onClick={handlePollAllEmails}
              variant="outline"
              disabled={pollingAll}
            >
              {pollingAll ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Polling All...
                </>
              ) : (
                <>
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Poll All
                </>
              )}
            </Button>
          )}
          <Button onClick={handleAdd}>
            <Plus className="h-4 w-4 mr-2" />
            Add Mailbox
          </Button>
        </div>
      </div>

      {error && (
        <div className="mb-6 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800" role="alert">
          {error}
        </div>
      )}

      {mailboxes.length > 0 ? (
        <DataTable
          columns={columns}
          data={mailboxes}
          emptyMessage="No mailboxes configured"
          className="bg-white rounded-lg shadow overflow-hidden"
        />
      ) : (
        <EmptyState
          title="No mailboxes configured"
          description="Add email mailboxes to enable email parsing."
        />
      )}

      <Modal
        isOpen={showModal}
        onClose={() => !saving && setShowModal(false)}
        title={editingMailbox ? 'Edit Email Mailbox' : 'Add Email Mailbox'}
        size="medium"
      >
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Routing & Assignment</h3>
            
            <div className="space-y-4">
              <Select
                label="Default Department"
                name="defaultDepartmentId"
                value={formData.defaultDepartmentId}
                onChange={handleInputChange}
                options={[
                  { value: '', label: '-- No Department --' },
                  ...departments.map(d => ({ value: d.id, label: d.name }))
                ]}
              />
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                Department to assign orders parsed from this mailbox
              </p>

              <div>
                <Select
                  label="Default Parser Template"
                  name="defaultParserTemplateId"
                  value={formData.defaultParserTemplateId}
                  onChange={handleInputChange}
                  options={[
                    { value: '', label: '-- Auto-match using rules --' },
                    ...parserTemplates.map(template => ({
                      value: template.id,
                      label: `${template.name} (${template.code})`
                    }))
                  ]}
                />
                <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
                  Optional fallback template when no template matches incoming emails based on sender/subject patterns
                </p>
              </div>
            </div>
          </div>

          <div className="border-b border-slate-200 dark:border-slate-700 pb-4 mb-4">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300 mb-3">Basic Information</h3>
          </div>

          <TextInput
            label="Name"
            name="name"
            value={formData.name}
            onChange={handleInputChange}
            required
            placeholder="e.g., Main Mailbox"
          />

          <TextInput
            label="Email Address"
            name="emailAddress"
            type="email"
            value={formData.emailAddress}
            onChange={handleInputChange}
            required
            placeholder="mailbox@example.com"
          />

          <Select
            label="Provider"
            name="provider"
            value={formData.provider}
            onChange={handleInputChange}
            options={[
              { value: 'POP3', label: 'POP3' },
              { value: 'IMAP', label: 'IMAP' },
              { value: 'O365', label: 'Office 365' },
              { value: 'Gmail', label: 'Gmail' }
            ]}
          />

          {(formData.provider === 'POP3' || formData.provider === 'IMAP') && (
            <>
              <TextInput
                label="Host"
                name="host"
                value={formData.host}
                onChange={handleInputChange}
                required
                placeholder="mail.example.com"
              />

              <TextInput
                label="Port (Incoming)"
                name="port"
                type="number"
                value={formData.port}
                onChange={handleInputChange}
                required
                placeholder="995"
              />

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  name="useSsl"
                  checked={formData.useSsl}
                  onChange={handleInputChange}
                  className="h-4 w-4 rounded border-gray-300"
                />
                <label className="text-sm font-medium">Use SSL/TLS (Incoming)</label>
              </div>

              <TextInput
                label="Username"
                name="username"
                value={formData.username}
                onChange={handleInputChange}
                required
                placeholder="email@example.com"
              />

              <div>
                <TextInput
                  label="Password"
                  name="password"
                  type="password"
                  value={formData.password}
                  onChange={handleInputChange}
                  required={!editingMailbox}
                  placeholder="Enter password"
                />
                {editingMailbox && (
                  <p className="text-xs text-muted-foreground mt-1">
                    Leave blank to keep existing password
                  </p>
                )}
              </div>
            </>
          )}

          <TextInput
            label="Poll Interval (minutes)"
            name="pollIntervalMinutes"
            type="number"
            value={formData.pollIntervalMinutes}
            onChange={handleInputChange}
            required
            placeholder="15"
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

          {/* SMTP configuration */}
          <div className="pt-6 border-t">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-primary" />
                <h2 className="text-sm font-semibold">SMTP (Outgoing Mail)</h2>
              </div>
              <label className="flex items-center gap-2 text-xs text-gray-700">
                <input
                  type="checkbox"
                  checked={useSameSmtpCredentials}
                  onChange={(e) => setUseSameSmtpCredentials(e.target.checked)}
                  className="h-4 w-4 rounded border-gray-300"
                />
                Use same host/username/password as incoming
              </label>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="SMTP Host"
                name="smtpHost"
                value={formData.smtpHost}
                onChange={handleInputChange}
                disabled={useSameSmtpCredentials}
                placeholder="smtp.example.com"
              />
              <TextInput
                label="SMTP Port"
                name="smtpPort"
                type="number"
                value={formData.smtpPort}
                onChange={handleInputChange}
                placeholder="587"
              />
            </div>

            <div className="grid grid-cols-2 gap-4 mt-4">
              <TextInput
                label="SMTP Username"
                name="smtpUsername"
                value={formData.smtpUsername}
                onChange={handleInputChange}
                disabled={useSameSmtpCredentials}
              />
              <TextInput
                label="SMTP Password"
                name="smtpPassword"
                type="password"
                value={formData.smtpPassword}
                onChange={handleInputChange}
                disabled={useSameSmtpCredentials}
              />
            </div>

            <div className="flex items-center gap-6 mt-4">
              <label className="flex items-center gap-2 text-xs text-gray-700">
                <input
                  type="checkbox"
                  name="smtpUseSsl"
                  checked={formData.smtpUseSsl}
                  onChange={handleInputChange}
                  className="h-4 w-4 rounded border-gray-300"
                />
                Use SSL
              </label>
              <label className="flex items-center gap-2 text-xs text-gray-700">
                <input
                  type="checkbox"
                  name="smtpUseTls"
                  checked={formData.smtpUseTls}
                  onChange={handleInputChange}
                  className="h-4 w-4 rounded border-gray-300"
                />
                Use TLS
              </label>
            </div>

            <div className="grid grid-cols-2 gap-4 mt-4">
              <TextInput
                label="From Address (optional)"
                name="smtpFromAddress"
                value={formData.smtpFromAddress}
                onChange={handleInputChange}
                placeholder="orders@example.com"
              />
              <TextInput
                label="From Name (optional)"
                name="smtpFromName"
                value={formData.smtpFromName}
                onChange={handleInputChange}
                placeholder="Cephas Orders"
              />
            </div>
          </div>

          <div className="flex gap-4 justify-end pt-4 border-t mt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowModal(false)}
              disabled={saving}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  {editingMailbox ? 'Updating...' : 'Creating...'}
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  {editingMailbox ? 'Update' : 'Create'}
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Test Connection Modal */}
      <Modal
        isOpen={showTestModal}
        onClose={() => !testing && setShowTestModal(false)}
        title="Test Email Mailbox Connection"
        size="small"
      >
        <div className="space-y-4">
          {testMailbox && (
            <div className="rounded-md bg-slate-50 px-4 py-3 text-sm text-slate-800">
              <div className="font-semibold">{testMailbox.name || 'Mailbox'}</div>
              <div className="text-xs text-slate-600">
                {testMailbox.emailAddress || testMailbox.username || ''}
              </div>
            </div>
          )}

          {testing && (
            <div className="flex items-center gap-3">
              <LoadingSpinner size="sm" />
              <span className="text-sm text-slate-700">Testing connection...</span>
            </div>
          )}

          {!testing && testResult && (
            <div className="space-y-3">
              <div
                className={`rounded-md px-4 py-3 text-sm ${
                  testResult.status === 'success'
                    ? 'bg-emerald-50 text-emerald-800'
                    : 'bg-red-50 text-red-800'
                }`}
              >
                <div className="font-semibold">
                  {testResult.status === 'success' ? 'Success' : 'Connection test failed'}
                </div>
                {testResult.message && (
                  <div className="mt-1 text-xs leading-relaxed">{testResult.message}</div>
                )}
              </div>

              {(typeof testResult.incomingSuccess === 'boolean' ||
                typeof testResult.smtpSuccess === 'boolean') && (
                <div className="grid grid-cols-1 gap-3">
                  <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-800">
                    <div className="font-semibold mb-1">
                      Incoming ({testResult.incomingProtocol || 'POP3/IMAP'})
                    </div>
                    {testResult.incomingSuccess ? (
                      <>
                        <div>Connection successful</div>
                        <div>Authentication passed</div>
                        <div className="mt-1 text-[11px] text-slate-600">
                          Response time: {testResult.incomingResponseTimeMs ?? 0}ms
                        </div>
                      </>
                    ) : (
                      <>
                        <div className="text-red-700">Connection failed</div>
                        {testResult.incomingError && (
                          <div className="mt-1 text-[11px] text-red-700 break-words">
                            Error: {testResult.incomingError}
                          </div>
                        )}
                      </>
                    )}
                  </div>

                  <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-800">
                    <div className="font-semibold mb-1">SMTP (Outgoing)</div>
                    {testResult.smtpSuccess ? (
                      <>
                        <div>SMTP connection successful</div>
                        <div className="mt-1 text-[11px] text-slate-600">
                          Response time: {testResult.smtpResponseTimeMs ?? 0}ms
                        </div>
                      </>
                    ) : (
                      <>
                        <div className="text-red-700">SMTP connection failed</div>
                        {testResult.smtpError && (
                          <div className="mt-1 text-[11px] text-red-700 break-words">
                            Error: {testResult.smtpError}
                          </div>
                        )}
                      </>
                    )}
                  </div>
                </div>
              )}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowTestModal(false)}
              disabled={testing}
            >
              <X className="h-4 w-4 mr-2" />
              Close
            </Button>
          </div>
        </div>
      </Modal>

      {/* Poll Result Modal */}
      <Modal
        isOpen={showPollModal}
        onClose={() => !polling && !pollingAll && setShowPollModal(false)}
        title={pollAllResults.length > 0 ? "Email Polling Results (All Accounts)" : "Email Polling Result"}
        size="medium"
      >
        <div className="space-y-4">
          {(polling || pollingAll) && (
            <div className="flex items-center gap-3">
              <LoadingSpinner size="sm" />
              <span className="text-sm text-slate-700">Polling emails...</span>
            </div>
          )}

          {!polling && !pollingAll && pollAllResults.length > 0 && (
            <div className="space-y-3">
              <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-800">
                <div className="font-semibold mb-2">Summary</div>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <span className="font-semibold">Total Emails Fetched:</span>{' '}
                    {pollAllResults.reduce((sum, r) => sum + (r.emailsFetched || 0), 0)}
                  </div>
                  <div>
                    <span className="font-semibold">Total Sessions Created:</span>{' '}
                    {pollAllResults.reduce((sum, r) => sum + (r.parseSessionsCreated || 0), 0)}
                  </div>
                  <div>
                    <span className="font-semibold">Successful Accounts:</span>{' '}
                    {pollAllResults.filter(r => r.success).length} / {pollAllResults.length}
                  </div>
                  <div>
                    <span className="font-semibold">Total Errors:</span>{' '}
                    {pollAllResults.reduce((sum, r) => sum + (r.errors || 0), 0)}
                  </div>
                </div>
              </div>
              
              <div className="max-h-96 overflow-y-auto space-y-2">
                {pollAllResults.map((result, idx) => (
                  <div
                    key={idx}
                    className={`rounded-md border px-4 py-3 text-xs ${
                      result.success
                        ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
                        : 'border-red-200 bg-red-50 text-red-800'
                    }`}
                  >
                    <div className="font-semibold mb-1">
                      {result.emailAccountName || `Account ${idx + 1}`}
                    </div>
                    {result.success ? (
                      <div className="space-y-1">
                        <div>Emails: {result.emailsFetched || 0}</div>
                        <div>Sessions: {result.parseSessionsCreated || 0}</div>
                        {result.errors && result.errors > 0 && (
                          <div className="text-red-700">Errors: {result.errors}</div>
                        )}
                      </div>
                    ) : (
                      <div className="text-red-700">{result.errorMessage || 'Polling failed'}</div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {!polling && !pollingAll && pollResult && pollAllResults.length === 0 && (
            <div className="space-y-3">
              <div
                className={`rounded-md px-4 py-3 text-sm ${
                  pollResult.success
                    ? 'bg-emerald-50 text-emerald-800'
                    : 'bg-red-50 text-red-800'
                }`}
              >
                <div className="font-semibold">
                  {pollResult.success ? 'Polling Complete' : 'Polling Failed'}
                </div>
                {pollResult.errorMessage && (
                  <div className="mt-1 text-xs leading-relaxed">{pollResult.errorMessage}</div>
                )}
              </div>

              {pollResult.success && (
                <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-xs text-slate-800">
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <span className="font-semibold">Emails Fetched:</span> {pollResult.emailsFetched || 0}
                    </div>
                    <div>
                      <span className="font-semibold">Sessions Created:</span> {pollResult.parseSessionsCreated || 0}
                    </div>
                    <div>
                      <span className="font-semibold">Drafts Created:</span> {pollResult.draftsCreated || 0}
                    </div>
                    <div>
                      <span className="font-semibold">Errors:</span> {pollResult.errors || 0}
                    </div>
                  </div>
                  {pollResult.processedEmails && pollResult.processedEmails.length > 0 && (
                    <div className="mt-2 pt-2 border-t border-slate-200">
                      <div className="font-semibold mb-1">Processed Emails:</div>
                      <ul className="list-disc list-inside text-[11px] max-h-32 overflow-y-auto">
                        {pollResult.processedEmails.map((subject, idx) => (
                          <li key={idx} className="truncate">{subject}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setShowPollModal(false)}
              disabled={!!polling}
            >
              <X className="h-4 w-4 mr-2" />
              Close
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default EmailMailboxesPage;

