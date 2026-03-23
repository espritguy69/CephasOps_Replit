import React, { useState, useEffect, useRef } from 'react';
import { 
  Save, RefreshCw, TestTube, Eye, EyeOff, Plus, Trash2, Edit2, 
  MessageSquare, MessageCircle, FileText, Settings, AlertCircle, Mail
} from 'lucide-react';
import { 
  GridComponent, 
  ColumnsDirective, 
  ColumnDirective, 
  Page, 
  Sort, 
  Filter, 
  Toolbar, 
  ExcelExport,
  Inject
} from '@syncfusion/ej2-react-grids';
import { PageShell } from '../../components/layout';
import { 
  LoadingSpinner, Card, Button, useToast, TextInput, Select, Switch, EmptyState, Modal, Textarea, Label
} from '../../components/ui';
import { 
  getIntegrationSettings, 
  updateMyInvoisSettings, 
  updateSmsSettings, 
  updateWhatsAppSettings,
  testMyInvoisConnection,
  testSmsConnection,
  testWhatsAppConnection
} from '../../api/integrations';
import type { IntegrationSettings, MyInvoisSettings, SmsSettings, WhatsAppSettings } from '../../api/integrations';
import { 
  getSmsTemplates, createSmsTemplate, updateSmsTemplate, deleteSmsTemplate,
  type SmsTemplate, type CreateSmsTemplateDto
} from '../../api/smsTemplates';
import { 
  getWhatsAppTemplates, createWhatsAppTemplate, updateWhatsAppTemplate, deleteWhatsAppTemplate,
  type WhatsAppTemplate, type CreateWhatsAppTemplateDto
} from '../../api/whatsappTemplates';
import { useDepartment } from '../../contexts/DepartmentContext';
// Email imports
import EmailMailboxesPage from '../../features/email/EmailMailboxesPage';
import { getEmailTemplates, createEmailTemplate, updateEmailTemplate, deleteEmailTemplate } from '../../api/emailTemplates';
import type { EmailTemplate, CreateEmailTemplateDto } from '../../api/emailTemplates';
import { getParserTemplates, createParserTemplate, updateParserTemplate, deleteParserTemplate, getEmailSystemSettings, updateEmailSystemSettings, type EmailSystemSettings } from '../../api/email';
import type { ParserTemplate } from '../../types/email';

/**
 * Integrations Management Page - Modular Design
 * 
 * Tabs:
 * 1. Email - Email Accounts & Email Templates (configuration only)
 * 2. MyInvois (e-Invoice) - Settings & Configuration
 * 3. SMS - Settings & Templates
 * 4. WhatsApp - Settings & Templates
 * 
 * Note: Email Inbox/Sent/Compose is under Tools → Email (/email) for everyday users
 */

// ============================================================================
// Email Configuration Component (Accounts + Templates only)
// ============================================================================
interface EmailConfigTabProps {
  companyId: string;
}

const EmailConfigTab: React.FC<EmailConfigTabProps> = ({ companyId }) => {
  const [activeSubTab, setActiveSubTab] = useState<'accounts' | 'templates' | 'settings'>('accounts');

  return (
    <div className="space-y-6">
      {/* Sub-tabs */}
      <div className="flex gap-4 border-b">
        <button
          onClick={() => setActiveSubTab('accounts')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'accounts' ? 'border-orange-600 text-orange-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <Settings className="h-4 w-4 inline mr-2" />
          Email Accounts
        </button>
        <button
          onClick={() => setActiveSubTab('templates')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'templates' ? 'border-orange-600 text-orange-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <FileText className="h-4 w-4 inline mr-2" />
          Email Templates
        </button>
        <button
          onClick={() => setActiveSubTab('settings')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'settings' ? 'border-orange-600 text-orange-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <Settings className="h-4 w-4 inline mr-2" />
          System Settings
        </button>
      </div>

      {/* Email Accounts */}
      {activeSubTab === 'accounts' && <EmailMailboxesPage />}

      {/* Email Templates */}
      {activeSubTab === 'templates' && <EmailTemplatesSection companyId={companyId} />}

      {/* Email System Settings */}
      {activeSubTab === 'settings' && <EmailSystemSettingsSection />}
    </div>
  );
};

// Email Templates Section (embedded in Email tab) - Shows both Outgoing and Incoming (Parser) templates with CRUD
interface EmailTemplatesSectionProps {
  companyId: string;
}

const EmailTemplatesSection: React.FC<EmailTemplatesSectionProps> = ({ companyId }) => {
  const { showSuccess, showError } = useToast();
  const [activeTemplateTab, setActiveTemplateTab] = useState<'outgoing' | 'incoming'>('outgoing');
  const [outgoingTemplates, setOutgoingTemplates] = useState<EmailTemplate[]>([]);
  const [incomingTemplates, setIncomingTemplates] = useState<ParserTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  
  // Outgoing template modal state
  const [showOutgoingModal, setShowOutgoingModal] = useState(false);
  const [editingOutgoing, setEditingOutgoing] = useState<EmailTemplate | null>(null);
  const [outgoingForm, setOutgoingForm] = useState<CreateEmailTemplateDto>({
    code: '', name: '', description: '', subjectTemplate: '', bodyTemplate: '', isActive: true, direction: 'Outgoing'
  });

  // Incoming template modal state
  const [showIncomingModal, setShowIncomingModal] = useState(false);
  const [editingIncoming, setEditingIncoming] = useState<ParserTemplate | null>(null);
  const [incomingForm, setIncomingForm] = useState<Partial<ParserTemplate>>({
    code: '', name: '', description: '', partnerPattern: '', subjectPattern: '', priority: 100, autoApprove: false, isActive: true
  });

  const loadOutgoingTemplates = async () => {
    try {
      const data = await getEmailTemplates();
      setOutgoingTemplates(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load outgoing email templates');
    }
  };

  const loadIncomingTemplates = async () => {
    try {
      const data = await getParserTemplates();
      setIncomingTemplates(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load parser templates');
    }
  };

  const loadAllTemplates = async () => {
    setLoading(true);
    try {
      await Promise.all([loadOutgoingTemplates(), loadIncomingTemplates()]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAllTemplates();
  }, []);

  // Outgoing Template CRUD
  const handleSaveOutgoing = async () => {
    try {
      if (editingOutgoing) {
        await updateEmailTemplate(editingOutgoing.id, outgoingForm);
        showSuccess('Email template updated');
      } else {
        await createEmailTemplate(outgoingForm);
        showSuccess('Email template created');
      }
      setShowOutgoingModal(false);
      setEditingOutgoing(null);
      setOutgoingForm({ code: '', name: '', description: '', subjectTemplate: '', bodyTemplate: '', isActive: true, direction: 'Outgoing' });
      loadOutgoingTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to save template');
    }
  };

  const handleDeleteOutgoing = async (id: string) => {
    if (!confirm('Delete this email template?')) return;
    try {
      await deleteEmailTemplate(id);
      showSuccess('Email template deleted');
      loadOutgoingTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to delete template');
    }
  };

  const openEditOutgoing = (template: EmailTemplate) => {
    setEditingOutgoing(template);
    setOutgoingForm({
      code: template.code,
      name: template.name,
      description: template.description || '',
      subjectTemplate: template.subjectTemplate,
      bodyTemplate: template.bodyTemplate,
      isActive: template.isActive,
      direction: template.direction || 'Outgoing'
    });
    setShowOutgoingModal(true);
  };

  // Incoming Template CRUD
  const handleSaveIncoming = async () => {
    try {
      if (editingIncoming) {
        await updateParserTemplate(editingIncoming.id, incomingForm);
        showSuccess('Parser template updated');
      } else {
        await createParserTemplate(incomingForm);
        showSuccess('Parser template created');
      }
      setShowIncomingModal(false);
      setEditingIncoming(null);
      setIncomingForm({ code: '', name: '', description: '', partnerPattern: '', subjectPattern: '', priority: 100, autoApprove: false, isActive: true });
      loadIncomingTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to save template');
    }
  };

  const handleDeleteIncoming = async (id: string) => {
    if (!confirm('Delete this parser template?')) return;
    try {
      await deleteParserTemplate(id);
      showSuccess('Parser template deleted');
      loadIncomingTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to delete template');
    }
  };

  const openEditIncoming = (template: ParserTemplate) => {
    setEditingIncoming(template);
    setIncomingForm({
      code: template.code,
      name: template.name,
      description: template.description || '',
      partnerPattern: template.partnerPattern || '',
      subjectPattern: template.subjectPattern || '',
      priority: template.priority || 100,
      autoApprove: template.autoApprove || false,
      isActive: template.isActive
    });
    setShowIncomingModal(true);
  };

  // Grid templates
  const statusTemplate = (props: any) => (
    <span className={`px-2 py-0.5 rounded text-[11px] font-medium ${
      props.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'
    }`}>
      {props.isActive ? 'Active' : 'Inactive'}
    </span>
  );

  const autoApproveTemplate = (props: any) => (
    <span className={`px-2 py-0.5 rounded text-[11px] font-medium ${
      props.autoApprove ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'
    }`}>
      {props.autoApprove ? 'Auto' : 'Manual'}
    </span>
  );

  const outgoingActionsTemplate = (props: any) => (
    <div className="flex gap-1">
      <button onClick={() => openEditOutgoing(props)} className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded">
        <Edit2 className="h-3.5 w-3.5" />
      </button>
      <button onClick={() => handleDeleteOutgoing(props.id)} className="p-1 text-red-600 hover:text-red-800 hover:bg-red-50 rounded">
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  );

  const incomingActionsTemplate = (props: any) => (
    <div className="flex gap-1">
      <button onClick={() => openEditIncoming(props)} className="p-1 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded">
        <Edit2 className="h-3.5 w-3.5" />
      </button>
      <button onClick={() => handleDeleteIncoming(props.id)} className="p-1 text-red-600 hover:text-red-800 hover:bg-red-50 rounded">
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  );

  return (
    <div className="space-y-4">
      {/* Template Type Tabs */}
      <div className="flex gap-4 border-b">
        <button
          onClick={() => setActiveTemplateTab('outgoing')}
          className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTemplateTab === 'outgoing' ? 'border-purple-600 text-purple-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <Mail className="h-4 w-4 inline mr-2" />
          Outgoing Templates ({outgoingTemplates.length})
        </button>
        <button
          onClick={() => setActiveTemplateTab('incoming')}
          className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
            activeTemplateTab === 'incoming' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <FileText className="h-4 w-4 inline mr-2" />
          Incoming / Parser Templates ({incomingTemplates.length})
        </button>
      </div>

      {/* Outgoing Templates */}
      {activeTemplateTab === 'outgoing' && (
        <Card className="p-5">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-base font-semibold text-slate-900">Outgoing Email Templates</h3>
              <p className="text-xs text-slate-500 mt-0.5">Templates for sending emails to customers, partners, and internal teams</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={loadOutgoingTemplates}>
                <RefreshCw className="h-3.5 w-3.5 mr-1.5" />
                Refresh
              </Button>
              <Button size="sm" onClick={() => {
                setEditingOutgoing(null);
                setOutgoingForm({ code: '', name: '', description: '', subjectTemplate: '', bodyTemplate: '', isActive: true, direction: 'Outgoing' });
                setShowOutgoingModal(true);
              }}>
                <Plus className="h-3.5 w-3.5 mr-1.5" />
                Add Template
              </Button>
            </div>
          </div>

          {loading ? (
            <LoadingSpinner message="Loading templates..." />
          ) : outgoingTemplates.length === 0 ? (
            <EmptyState title="No Outgoing Templates" description="Create email templates for sending notifications" />
          ) : (
            <div className="text-sm [&_.e-grid]:text-sm [&_.e-headercell]:text-xs [&_.e-headercell]:font-semibold [&_.e-rowcell]:py-2">
              <GridComponent
                dataSource={outgoingTemplates}
                allowPaging
                allowSorting
                allowFiltering
                pageSettings={{ pageSize: 10 }}
                filterSettings={{ type: 'Menu' }}
                toolbar={['Search']}
                rowHeight={40}
                height="auto"
              >
                <ColumnsDirective>
                  <ColumnDirective field="code" headerText="Code" width="140" />
                  <ColumnDirective field="name" headerText="Name" width="180" />
                  <ColumnDirective field="relatedEntityType" headerText="Entity Type" width="100" />
                  <ColumnDirective field="subjectTemplate" headerText="Subject Template" width="280" />
                  <ColumnDirective field="isActive" headerText="Status" width="80" template={statusTemplate} />
                  <ColumnDirective headerText="Actions" width="80" template={outgoingActionsTemplate} textAlign="Center" />
                </ColumnsDirective>
                <Inject services={[Page, Sort, Filter, Toolbar]} />
              </GridComponent>
            </div>
          )}
        </Card>
      )}

      {/* Incoming / Parser Templates */}
      {activeTemplateTab === 'incoming' && (
        <Card className="p-5">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-base font-semibold text-slate-900">Incoming / Parser Templates</h3>
              <p className="text-xs text-slate-500 mt-0.5">Templates to parse and process incoming emails from TIME and other sources</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={loadIncomingTemplates}>
                <RefreshCw className="h-3.5 w-3.5 mr-1.5" />
                Refresh
              </Button>
              <Button size="sm" onClick={() => {
                setEditingIncoming(null);
                setIncomingForm({ code: '', name: '', description: '', partnerPattern: '', subjectPattern: '', priority: 100, autoApprove: false, isActive: true });
                setShowIncomingModal(true);
              }}>
                <Plus className="h-3.5 w-3.5 mr-1.5" />
                Add Template
              </Button>
            </div>
          </div>

          {loading ? (
            <LoadingSpinner message="Loading templates..." />
          ) : incomingTemplates.length === 0 ? (
            <EmptyState title="No Parser Templates" description="Parser templates are used to process incoming emails" />
          ) : (
            <div className="text-sm [&_.e-grid]:text-sm [&_.e-headercell]:text-xs [&_.e-headercell]:font-semibold [&_.e-rowcell]:py-2">
              <GridComponent
                dataSource={incomingTemplates}
                allowPaging
                allowSorting
                allowFiltering
                pageSettings={{ pageSize: 10 }}
                filterSettings={{ type: 'Menu' }}
                toolbar={['Search']}
                rowHeight={40}
                height="auto"
              >
                <ColumnsDirective>
                  <ColumnDirective field="code" headerText="Code" width="160" />
                  <ColumnDirective field="name" headerText="Name" width="180" />
                  <ColumnDirective field="partnerPattern" headerText="From/Partner Pattern" width="180" />
                  <ColumnDirective field="subjectPattern" headerText="Subject Pattern" width="220" />
                  <ColumnDirective field="priority" headerText="Priority" width="70" textAlign="Right" />
                  <ColumnDirective field="autoApprove" headerText="Approval" width="80" template={autoApproveTemplate} />
                  <ColumnDirective field="isActive" headerText="Status" width="70" template={statusTemplate} />
                  <ColumnDirective headerText="Actions" width="80" template={incomingActionsTemplate} textAlign="Center" />
                </ColumnsDirective>
                <Inject services={[Page, Sort, Filter, Toolbar]} />
              </GridComponent>
            </div>
          )}
        </Card>
      )}

      {/* Outgoing Template Modal */}
      <Modal isOpen={showOutgoingModal} onClose={() => setShowOutgoingModal(false)} title={editingOutgoing ? 'Edit Outgoing Template' : 'New Outgoing Template'} size="large">
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label className="text-sm">Code</Label>
              <TextInput
                value={outgoingForm.code}
                onChange={(e) => setOutgoingForm({ ...outgoingForm, code: e.target.value })}
                placeholder="TEMPLATE_CODE"
                disabled={!!editingOutgoing}
                className="text-sm"
              />
            </div>
            <div>
              <Label className="text-sm">Name</Label>
              <TextInput
                value={outgoingForm.name}
                onChange={(e) => setOutgoingForm({ ...outgoingForm, name: e.target.value })}
                placeholder="Template Name"
                className="text-sm"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label className="text-sm">Related Entity Type</Label>
              <Select
                value={outgoingForm.relatedEntityType || 'Order'}
                onChange={(e) => setOutgoingForm({ ...outgoingForm, relatedEntityType: e.target.value })}
                options={[
                  { value: 'Order', label: 'Order' },
                  { value: 'Appointment', label: 'Appointment' },
                  { value: 'Building', label: 'Building' },
                  { value: 'Customer', label: 'Customer' },
                  { value: 'General', label: 'General' }
                ]}
              />
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Switch
                checked={outgoingForm.isActive ?? true}
                onCheckedChange={(checked) => setOutgoingForm({ ...outgoingForm, isActive: checked })}
              />
              <span className="text-sm">Active</span>
            </div>
          </div>
          <div>
            <Label className="text-sm">Subject Template</Label>
            <TextInput
              value={outgoingForm.subjectTemplate}
              onChange={(e) => setOutgoingForm({ ...outgoingForm, subjectTemplate: e.target.value })}
              placeholder="Email subject with {placeholders}"
              className="text-sm"
            />
          </div>
          <div>
            <Label className="text-sm">Body Template (HTML)</Label>
            <Textarea
              value={outgoingForm.bodyTemplate}
              onChange={(e) => setOutgoingForm({ ...outgoingForm, bodyTemplate: e.target.value })}
              placeholder="Email body with {placeholders}..."
              rows={8}
              className="text-sm font-mono"
            />
          </div>
          <div>
            <Label className="text-sm">Description</Label>
            <TextInput
              value={outgoingForm.description || ''}
              onChange={(e) => setOutgoingForm({ ...outgoingForm, description: e.target.value })}
              placeholder="Optional description"
              className="text-sm"
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setShowOutgoingModal(false)}>Cancel</Button>
            <Button onClick={handleSaveOutgoing}>{editingOutgoing ? 'Update' : 'Create'} Template</Button>
          </div>
        </div>
      </Modal>

      {/* Incoming Template Modal */}
      <Modal isOpen={showIncomingModal} onClose={() => setShowIncomingModal(false)} title={editingIncoming ? 'Edit Parser Template' : 'New Parser Template'} size="large">
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label className="text-sm">Code</Label>
              <TextInput
                value={incomingForm.code || ''}
                onChange={(e) => setIncomingForm({ ...incomingForm, code: e.target.value })}
                placeholder="TIME_FTTH"
                disabled={!!editingIncoming}
                className="text-sm"
              />
            </div>
            <div>
              <Label className="text-sm">Name</Label>
              <TextInput
                value={incomingForm.name || ''}
                onChange={(e) => setIncomingForm({ ...incomingForm, name: e.target.value })}
                placeholder="TIME FTTH Orders"
                className="text-sm"
              />
            </div>
          </div>
          <div>
            <Label className="text-sm">From Pattern (regex or wildcard)</Label>
            <TextInput
              value={incomingForm.partnerPattern || ''}
              onChange={(e) => setIncomingForm({ ...incomingForm, partnerPattern: e.target.value })}
              placeholder="*@time.com.my|No-Reply@time.com.my"
              className="text-sm font-mono"
            />
            <p className="text-xs text-slate-500 mt-1">Use pipe (|) for OR patterns. Use * as wildcard.</p>
          </div>
          <div>
            <Label className="text-sm">Subject Pattern (regex or wildcard)</Label>
            <TextInput
              value={incomingForm.subjectPattern || ''}
              onChange={(e) => setIncomingForm({ ...incomingForm, subjectPattern: e.target.value })}
              placeholder="*FTTH*|New Activation*|Modification*"
              className="text-sm font-mono"
            />
            <p className="text-xs text-slate-500 mt-1">Use pipe (|) for OR patterns. Use * as wildcard.</p>
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <Label className="text-sm">Priority</Label>
              <TextInput
                type="number"
                value={incomingForm.priority?.toString() || '100'}
                onChange={(e) => setIncomingForm({ ...incomingForm, priority: parseInt(e.target.value) || 100 })}
                placeholder="100"
                className="text-sm"
              />
              <p className="text-xs text-slate-500 mt-1">Higher = matched first</p>
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Switch
                checked={incomingForm.autoApprove ?? false}
                onCheckedChange={(checked) => setIncomingForm({ ...incomingForm, autoApprove: checked })}
              />
              <span className="text-sm">Auto Approve</span>
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Switch
                checked={incomingForm.isActive ?? true}
                onCheckedChange={(checked) => setIncomingForm({ ...incomingForm, isActive: checked })}
              />
              <span className="text-sm">Active</span>
            </div>
          </div>
          <div>
            <Label className="text-sm">Description</Label>
            <TextInput
              value={incomingForm.description || ''}
              onChange={(e) => setIncomingForm({ ...incomingForm, description: e.target.value })}
              placeholder="Optional description"
              className="text-sm"
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setShowIncomingModal(false)}>Cancel</Button>
            <Button onClick={handleSaveIncoming}>{editingIncoming ? 'Update' : 'Create'} Template</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

// ============================================================================
// Email System Settings Component
// ============================================================================
const EmailSystemSettingsSection: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState<EmailSystemSettings>({
    pollIntervalMinutes: 15,
    retentionHours: 48
  });

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      const data = await getEmailSystemSettings();
      setSettings(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load email system settings');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      await updateEmailSystemSettings(settings);
      showSuccess('Email system settings saved successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to save email system settings');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading email system settings..." />;
  }

  return (
    <Card className="p-6">
      <div className="flex items-center justify-between border-b pb-4 mb-6">
        <div>
          <h3 className="text-lg font-semibold">Email System Settings</h3>
          <p className="text-sm text-muted-foreground mt-1">
            Configure global email polling interval and retention period
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button size="sm" variant="outline" onClick={loadSettings} disabled={loading}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button size="sm" onClick={handleSave} disabled={saving}>
            <Save className="h-4 w-4 mr-2" />
            {saving ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <Label>Poll Interval (minutes)</Label>
          <TextInput
            type="number"
            value={settings.pollIntervalMinutes.toString()}
            onChange={(e) => setSettings({ 
              ...settings, 
              pollIntervalMinutes: parseInt(e.target.value) || 15 
            })}
            placeholder="15"
            min="1"
          />
          <p className="text-xs text-slate-500 mt-1">
            How often the system polls email mailboxes for new messages. Default: 15 minutes.
          </p>
        </div>
        <div>
          <Label>Retention Period (hours)</Label>
          <TextInput
            type="number"
            value={settings.retentionHours.toString()}
            onChange={(e) => setSettings({ 
              ...settings, 
              retentionHours: parseInt(e.target.value) || 48 
            })}
            placeholder="48"
            min="1"
          />
          <p className="text-xs text-slate-500 mt-1">
            How long emails are kept before automatic cleanup. Default: 48 hours.
          </p>
        </div>
      </div>

      <div className="mt-6 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-2">
          <AlertCircle className="h-5 w-5 text-blue-600 mt-0.5" />
          <div className="text-sm text-blue-800 dark:text-blue-200">
            <p className="font-medium mb-1">Configuration Notes:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>Poll interval affects how frequently all email accounts are checked for new messages</li>
              <li>Retention period determines when emails are automatically deleted by the cleanup job</li>
              <li>Changes take effect immediately for new email operations</li>
              <li>These are global settings that apply to all email accounts</li>
            </ul>
          </div>
        </div>
      </div>
    </Card>
  );
};

// ============================================================================
// MyInvois Settings Component
// ============================================================================
interface MyInvoisSettingsTabProps {
  settings: MyInvoisSettings;
  onSettingsChange: (settings: MyInvoisSettings) => void;
  onSave: () => Promise<void>;
  onTest: () => Promise<void>;
  saving: boolean;
  testing: boolean;
}

const MyInvoisSettingsTab: React.FC<MyInvoisSettingsTabProps> = ({
  settings, onSettingsChange, onSave, onTest, saving, testing
}) => {
  const [showSecret, setShowSecret] = useState(false);

  return (
    <div className="space-y-6">
      <Card className="p-6">
        <div className="flex items-center justify-between border-b pb-4 mb-6">
          <div>
            <h3 className="text-lg font-semibold">MyInvois (e-Invoice) Integration</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Configure LHDN MyInvois API credentials for e-invoice submission
            </p>
          </div>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium">Enabled</span>
              <Switch
                checked={settings.isEnabled}
                onCheckedChange={(checked) => onSettingsChange({ ...settings, isEnabled: checked })}
              />
            </div>
            <Button size="sm" variant="outline" onClick={onTest} disabled={testing}>
              <TestTube className="h-4 w-4 mr-2" />
              {testing ? 'Testing...' : 'Test'}
            </Button>
            <Button size="sm" onClick={onSave} disabled={saving}>
              <Save className="h-4 w-4 mr-2" />
              {saving ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <Label>Environment</Label>
            <Select
              value={settings.environment || 'Sandbox'}
              onChange={(e) => onSettingsChange({ ...settings, environment: e.target.value })}
              options={[
                { value: 'Sandbox', label: 'Sandbox (Testing)' },
                { value: 'Production', label: 'Production' }
              ]}
            />
          </div>
          <div>
            <Label>Base URL</Label>
            <TextInput
              value={settings.baseUrl}
              onChange={(e) => onSettingsChange({ ...settings, baseUrl: e.target.value })}
              placeholder="https://api-sandbox.myinvois.hasil.gov.my"
            />
          </div>
          <div>
            <Label>Client ID</Label>
            <TextInput
              value={settings.clientId}
              onChange={(e) => onSettingsChange({ ...settings, clientId: e.target.value })}
              placeholder="Your MyInvois Client ID"
            />
          </div>
          <div>
            <Label>Client Secret</Label>
            <div className="relative">
              <TextInput
                type={showSecret ? 'text' : 'password'}
                value={settings.clientSecret}
                onChange={(e) => onSettingsChange({ ...settings, clientSecret: e.target.value })}
                placeholder="Your MyInvois Client Secret"
              />
              <button
                type="button"
                onClick={() => setShowSecret(!showSecret)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
              >
                {showSecret ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
          </div>
        </div>

        <div className="mt-6 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 rounded-lg p-4">
          <div className="flex items-start gap-2">
            <AlertCircle className="h-5 w-5 text-blue-600 mt-0.5" />
            <div className="text-sm text-blue-800 dark:text-blue-200">
              <p className="font-medium mb-1">Configuration Notes:</p>
              <ul className="list-disc list-inside space-y-1">
                <li>Get your Client ID and Client Secret from the MyInvois developer portal</li>
                <li>Use Sandbox environment for testing, Production for live invoices</li>
                <li>Base URL will auto-update based on environment selection</li>
              </ul>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
};

// ============================================================================
// SMS Settings & Templates Component
// ============================================================================
interface SmsSettingsTabProps {
  settings: SmsSettings;
  onSettingsChange: (settings: SmsSettings) => void;
  onSave: () => Promise<void>;
  onTest: () => Promise<void>;
  saving: boolean;
  testing: boolean;
  companyId: string;
}

const SmsSettingsTab: React.FC<SmsSettingsTabProps> = ({
  settings, onSettingsChange, onSave, onTest, saving, testing, companyId
}) => {
  const { showSuccess, showError } = useToast();
  const [showAuthToken, setShowAuthToken] = useState(false);
  const [showGatewayKey, setShowGatewayKey] = useState(false);
  const [activeSubTab, setActiveSubTab] = useState<'settings' | 'templates'>('settings');
  const [templates, setTemplates] = useState<SmsTemplate[]>([]);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [showTemplateModal, setShowTemplateModal] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<SmsTemplate | null>(null);
  const [templateForm, setTemplateForm] = useState<CreateSmsTemplateDto>({
    code: '', name: '', description: '', category: 'General', messageText: '', isActive: true, notes: ''
  });
  const gridRef = useRef<GridComponent>(null);

  const loadTemplates = async () => {
    if (!companyId) return;
    setTemplatesLoading(true);
    try {
      const data = await getSmsTemplates({ companyId });
      setTemplates(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load SMS templates');
    } finally {
      setTemplatesLoading(false);
    }
  };

  useEffect(() => {
    if (activeSubTab === 'templates' && companyId) {
      loadTemplates();
    }
  }, [activeSubTab, companyId]);

  const handleSaveTemplate = async () => {
    try {
      if (editingTemplate) {
        await updateSmsTemplate(editingTemplate.id, templateForm);
        showSuccess('SMS template updated');
      } else {
        await createSmsTemplate(companyId, templateForm);
        showSuccess('SMS template created');
      }
      setShowTemplateModal(false);
      setEditingTemplate(null);
      setTemplateForm({ code: '', name: '', description: '', category: 'General', messageText: '', isActive: true, notes: '' });
      loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to save template');
    }
  };

  const handleDeleteTemplate = async (id: string) => {
    if (!confirm('Delete this SMS template?')) return;
    try {
      await deleteSmsTemplate(id);
      showSuccess('SMS template deleted');
      loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to delete template');
    }
  };

  const openEditModal = (template: SmsTemplate) => {
    setEditingTemplate(template);
    setTemplateForm({
      code: template.code,
      name: template.name,
      description: template.description || '',
      category: template.category,
      messageText: template.messageText,
      isActive: template.isActive,
      notes: template.notes || ''
    });
    setShowTemplateModal(true);
  };

  const statusTemplate = (props: any) => (
    <span className={`px-2 py-1 rounded text-xs font-medium ${
      props.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'
    }`}>
      {props.isActive ? 'Active' : 'Inactive'}
    </span>
  );

  const actionsTemplate = (props: any) => (
    <div className="flex gap-2">
      <button onClick={() => openEditModal(props)} className="text-blue-600 hover:text-blue-800">
        <Edit2 className="h-4 w-4" />
      </button>
      <button onClick={() => handleDeleteTemplate(props.id)} className="text-red-600 hover:text-red-800">
        <Trash2 className="h-4 w-4" />
      </button>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Sub-tabs */}
      <div className="flex gap-4 border-b">
        <button
          onClick={() => setActiveSubTab('settings')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'settings' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <Settings className="h-4 w-4 inline mr-2" />
          Settings
        </button>
        <button
          onClick={() => setActiveSubTab('templates')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'templates' ? 'border-blue-600 text-blue-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <MessageSquare className="h-4 w-4 inline mr-2" />
          Templates ({templates.length})
        </button>
      </div>

      {/* Settings Sub-tab */}
      {activeSubTab === 'settings' && (
        <Card className="p-6">
          <div className="flex items-center justify-between border-b pb-4 mb-6">
            <div>
              <h3 className="text-lg font-semibold">SMS Integration</h3>
              <p className="text-sm text-muted-foreground mt-1">
                Configure SMS provider (Twilio or SMS Gateway) for sending notifications
              </p>
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium">Enabled</span>
                <Switch
                  checked={settings.isEnabled}
                  onCheckedChange={(checked) => onSettingsChange({ ...settings, isEnabled: checked })}
                />
              </div>
              <Button size="sm" variant="outline" onClick={onTest} disabled={testing}>
                <TestTube className="h-4 w-4 mr-2" />
                {testing ? 'Testing...' : 'Test'}
              </Button>
              <Button size="sm" onClick={onSave} disabled={saving}>
                <Save className="h-4 w-4 mr-2" />
                {saving ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <Label>Provider</Label>
              <Select
                value={settings.provider || 'None'}
                onChange={(e) => onSettingsChange({ ...settings, provider: e.target.value })}
                options={[
                  { value: 'None', label: 'None (Disabled)' },
                  { value: 'Twilio', label: 'Twilio' },
                  { value: 'SMS_Gateway', label: 'SMS Gateway' }
                ]}
              />
            </div>
          </div>

          {/* Twilio Settings */}
          {settings.provider === 'Twilio' && (
            <div className="border-t pt-6 mt-6 space-y-4">
              <h4 className="font-semibold">Twilio Configuration</h4>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label>Account SID</Label>
                  <TextInput
                    value={settings.twilioAccountSid || ''}
                    onChange={(e) => onSettingsChange({ ...settings, twilioAccountSid: e.target.value })}
                    placeholder="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
                  />
                </div>
                <div>
                  <Label>Auth Token</Label>
                  <div className="relative">
                    <TextInput
                      type={showAuthToken ? 'text' : 'password'}
                      value={settings.twilioAuthToken || ''}
                      onChange={(e) => onSettingsChange({ ...settings, twilioAuthToken: e.target.value })}
                      placeholder="Your Twilio Auth Token"
                    />
                    <button
                      type="button"
                      onClick={() => setShowAuthToken(!showAuthToken)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
                    >
                      {showAuthToken ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>
                <div>
                  <Label>From Number</Label>
                  <TextInput
                    value={settings.twilioFromNumber || ''}
                    onChange={(e) => onSettingsChange({ ...settings, twilioFromNumber: e.target.value })}
                    placeholder="+1234567890"
                  />
                </div>
              </div>
            </div>
          )}

          {/* SMS Gateway Settings */}
          {settings.provider === 'SMS_Gateway' && (
            <div className="border-t pt-6 mt-6 space-y-4">
              <h4 className="font-semibold">SMS Gateway Configuration</h4>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label>Gateway URL</Label>
                  <TextInput
                    value={settings.gatewayUrl || ''}
                    onChange={(e) => onSettingsChange({ ...settings, gatewayUrl: e.target.value })}
                    placeholder="https://api.smsgateway.com/send"
                  />
                </div>
                <div>
                  <Label>API Key</Label>
                  <div className="relative">
                    <TextInput
                      type={showGatewayKey ? 'text' : 'password'}
                      value={settings.gatewayApiKey || ''}
                      onChange={(e) => onSettingsChange({ ...settings, gatewayApiKey: e.target.value })}
                      placeholder="Your SMS Gateway API Key"
                    />
                    <button
                      type="button"
                      onClick={() => setShowGatewayKey(!showGatewayKey)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
                    >
                      {showGatewayKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>
                <div>
                  <Label>Sender ID</Label>
                  <TextInput
                    value={settings.gatewaySenderId || ''}
                    onChange={(e) => onSettingsChange({ ...settings, gatewaySenderId: e.target.value })}
                    placeholder="CEPHASOPS"
                  />
                </div>
              </div>
            </div>
          )}
        </Card>
      )}

      {/* Templates Sub-tab */}
      {activeSubTab === 'templates' && (
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold">SMS Templates</h3>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={loadTemplates}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
              <Button size="sm" onClick={() => {
                setEditingTemplate(null);
                setTemplateForm({ code: '', name: '', description: '', category: 'General', messageText: '', isActive: true, notes: '' });
                setShowTemplateModal(true);
              }}>
                <Plus className="h-4 w-4 mr-2" />
                Add Template
              </Button>
            </div>
          </div>

          {templatesLoading ? (
            <LoadingSpinner message="Loading templates..." />
          ) : templates.length === 0 ? (
            <EmptyState title="No SMS Templates" description="Create your first SMS template to get started" />
          ) : (
            <GridComponent
              ref={gridRef}
              dataSource={templates}
              allowPaging
              allowSorting
              allowFiltering
              pageSettings={{ pageSize: 10 }}
              filterSettings={{ type: 'Menu' }}
              toolbar={['Search', 'ExcelExport']}
              toolbarClick={(args: any) => {
                if (gridRef.current && args.item.id.includes('excelexport')) {
                  gridRef.current.excelExport({ fileName: 'SmsTemplates.xlsx' });
                }
              }}
            >
              <ColumnsDirective>
                <ColumnDirective field="code" headerText="Code" width="120" />
                <ColumnDirective field="name" headerText="Name" width="180" />
                <ColumnDirective field="category" headerText="Category" width="120" />
                <ColumnDirective field="messageText" headerText="Message" width="300" />
                <ColumnDirective field="charCount" headerText="Chars" width="80" textAlign="Right" />
                <ColumnDirective field="isActive" headerText="Status" width="100" template={statusTemplate} />
                <ColumnDirective headerText="Actions" width="100" template={actionsTemplate} />
              </ColumnsDirective>
              <Inject services={[Page, Sort, Filter, Toolbar, ExcelExport]} />
            </GridComponent>
          )}
        </Card>
      )}

      {/* Template Modal */}
      <Modal
        isOpen={showTemplateModal}
        onClose={() => setShowTemplateModal(false)}
        title={editingTemplate ? 'Edit SMS Template' : 'New SMS Template'}
        size="large"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Code</Label>
              <TextInput
                value={templateForm.code}
                onChange={(e) => setTemplateForm({ ...templateForm, code: e.target.value })}
                placeholder="TEMPLATE_CODE"
                disabled={!!editingTemplate}
              />
            </div>
            <div>
              <Label>Name</Label>
              <TextInput
                value={templateForm.name}
                onChange={(e) => setTemplateForm({ ...templateForm, name: e.target.value })}
                placeholder="Template Name"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Category</Label>
              <Select
                value={templateForm.category}
                onChange={(e) => setTemplateForm({ ...templateForm, category: e.target.value })}
                options={[
                  { value: 'General', label: 'General' },
                  { value: 'Order', label: 'Order' },
                  { value: 'Appointment', label: 'Appointment' },
                  { value: 'Reminder', label: 'Reminder' },
                  { value: 'Alert', label: 'Alert' }
                ]}
              />
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Switch
                checked={templateForm.isActive ?? true}
                onCheckedChange={(checked) => setTemplateForm({ ...templateForm, isActive: checked })}
              />
              <span className="text-sm">Active</span>
            </div>
          </div>
          <div>
            <Label>Message Text ({templateForm.messageText?.length || 0} chars)</Label>
            <Textarea
              value={templateForm.messageText}
              onChange={(e) => setTemplateForm({ ...templateForm, messageText: e.target.value })}
              placeholder="Enter SMS message. Use {placeholders} for dynamic content."
              rows={4}
            />
          </div>
          <div>
            <Label>Description</Label>
            <TextInput
              value={templateForm.description || ''}
              onChange={(e) => setTemplateForm({ ...templateForm, description: e.target.value })}
              placeholder="Optional description"
            />
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => setShowTemplateModal(false)}>Cancel</Button>
            <Button onClick={handleSaveTemplate}>
              {editingTemplate ? 'Update' : 'Create'} Template
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

// ============================================================================
// WhatsApp Settings & Templates Component
// ============================================================================
interface WhatsAppSettingsTabProps {
  settings: WhatsAppSettings;
  onSettingsChange: (settings: WhatsAppSettings) => void;
  onSave: () => Promise<void>;
  onTest: () => Promise<void>;
  saving: boolean;
  testing: boolean;
  companyId: string;
}

const WhatsAppSettingsTab: React.FC<WhatsAppSettingsTabProps> = ({
  settings, onSettingsChange, onSave, onTest, saving, testing, companyId
}) => {
  const { showSuccess, showError } = useToast();
  const [showToken, setShowToken] = useState(false);
  const [activeSubTab, setActiveSubTab] = useState<'settings' | 'templates'>('settings');
  const [templates, setTemplates] = useState<WhatsAppTemplate[]>([]);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [showTemplateModal, setShowTemplateModal] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<WhatsAppTemplate | null>(null);
  const [templateForm, setTemplateForm] = useState<CreateWhatsAppTemplateDto>({
    code: '', name: '', description: '', category: 'General', templateId: '', messageBody: '', language: 'en', isActive: true, notes: ''
  });
  const gridRef = useRef<GridComponent>(null);

  const loadTemplates = async () => {
    if (!companyId) return;
    setTemplatesLoading(true);
    try {
      const data = await getWhatsAppTemplates({ companyId });
      setTemplates(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load WhatsApp templates');
    } finally {
      setTemplatesLoading(false);
    }
  };

  useEffect(() => {
    if (activeSubTab === 'templates' && companyId) {
      loadTemplates();
    }
  }, [activeSubTab, companyId]);

  const handleSaveTemplate = async () => {
    try {
      if (editingTemplate) {
        await updateWhatsAppTemplate(editingTemplate.id, templateForm);
        showSuccess('WhatsApp template updated');
      } else {
        await createWhatsAppTemplate(companyId, templateForm);
        showSuccess('WhatsApp template created');
      }
      setShowTemplateModal(false);
      setEditingTemplate(null);
      setTemplateForm({ code: '', name: '', description: '', category: 'General', templateId: '', messageBody: '', language: 'en', isActive: true, notes: '' });
      loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to save template');
    }
  };

  const handleDeleteTemplate = async (id: string) => {
    if (!confirm('Delete this WhatsApp template?')) return;
    try {
      await deleteWhatsAppTemplate(id);
      showSuccess('WhatsApp template deleted');
      loadTemplates();
    } catch (err: any) {
      showError(err.message || 'Failed to delete template');
    }
  };

  const openEditModal = (template: WhatsAppTemplate) => {
    setEditingTemplate(template);
    setTemplateForm({
      code: template.code,
      name: template.name,
      description: template.description || '',
      category: template.category,
      templateId: template.templateId || '',
      messageBody: template.messageBody || '',
      language: template.language || 'en',
      isActive: template.isActive,
      notes: template.notes || ''
    });
    setShowTemplateModal(true);
  };

  const approvalTemplate = (props: any) => {
    const colors: Record<string, string> = {
      'Approved': 'bg-emerald-100 text-emerald-700',
      'Pending': 'bg-amber-100 text-amber-700',
      'Rejected': 'bg-red-100 text-red-700'
    };
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium ${colors[props.approvalStatus] || 'bg-gray-100 text-gray-600'}`}>
        {props.approvalStatus}
      </span>
    );
  };

  const statusTemplate = (props: any) => (
    <span className={`px-2 py-1 rounded text-xs font-medium ${
      props.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-gray-100 text-gray-600'
    }`}>
      {props.isActive ? 'Active' : 'Inactive'}
    </span>
  );

  const actionsTemplate = (props: any) => (
    <div className="flex gap-2">
      <button onClick={() => openEditModal(props)} className="text-blue-600 hover:text-blue-800">
        <Edit2 className="h-4 w-4" />
      </button>
      <button onClick={() => handleDeleteTemplate(props.id)} className="text-red-600 hover:text-red-800">
        <Trash2 className="h-4 w-4" />
      </button>
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Sub-tabs */}
      <div className="flex gap-4 border-b">
        <button
          onClick={() => setActiveSubTab('settings')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'settings' ? 'border-green-600 text-green-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <Settings className="h-4 w-4 inline mr-2" />
          Settings
        </button>
        <button
          onClick={() => setActiveSubTab('templates')}
          className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeSubTab === 'templates' ? 'border-green-600 text-green-600' : 'border-transparent text-slate-600 hover:text-slate-900'
          }`}
        >
          <MessageCircle className="h-4 w-4 inline mr-2" />
          Templates ({templates.length})
        </button>
      </div>

      {/* Settings Sub-tab */}
      {activeSubTab === 'settings' && (
        <Card className="p-6">
          <div className="flex items-center justify-between border-b pb-4 mb-6">
            <div>
              <h3 className="text-lg font-semibold">WhatsApp Integration</h3>
              <p className="text-sm text-muted-foreground mt-1">
                Configure WhatsApp Business API for sending template messages
              </p>
            </div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium">Enabled</span>
                <Switch
                  checked={settings.isEnabled}
                  onCheckedChange={(checked) => onSettingsChange({ ...settings, isEnabled: checked })}
                />
              </div>
              <Button size="sm" variant="outline" onClick={onTest} disabled={testing}>
                <TestTube className="h-4 w-4 mr-2" />
                {testing ? 'Testing...' : 'Test'}
              </Button>
              <Button size="sm" onClick={onSave} disabled={saving}>
                <Save className="h-4 w-4 mr-2" />
                {saving ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <Label>Provider</Label>
              <Select
                value={settings.provider || 'CloudApi'}
                onChange={(e) => onSettingsChange({ ...settings, provider: e.target.value })}
                options={[
                  { value: 'CloudApi', label: 'WhatsApp Cloud API' },
                  { value: 'Twilio', label: 'Twilio WhatsApp' },
                  { value: 'None', label: 'None (Disabled)' }
                ]}
              />
            </div>
          </div>

          {/* WhatsApp Cloud API Settings */}
          {settings.provider === 'CloudApi' && (
            <div className="border-t pt-6 mt-6 space-y-4">
              <h4 className="font-semibold">WhatsApp Cloud API Configuration</h4>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label>Phone Number ID</Label>
                  <TextInput
                    value={settings.phoneNumberId || ''}
                    onChange={(e) => onSettingsChange({ ...settings, phoneNumberId: e.target.value })}
                    placeholder="123456789012345"
                  />
                </div>
                <div>
                  <Label>Access Token</Label>
                  <div className="relative">
                    <TextInput
                      type={showToken ? 'text' : 'password'}
                      value={settings.accessToken || ''}
                      onChange={(e) => onSettingsChange({ ...settings, accessToken: e.target.value })}
                      placeholder="Your WhatsApp Access Token"
                    />
                    <button
                      type="button"
                      onClick={() => setShowToken(!showToken)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
                    >
                      {showToken ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                </div>
                <div>
                  <Label>Business Account ID</Label>
                  <TextInput
                    value={settings.businessAccountId || ''}
                    onChange={(e) => onSettingsChange({ ...settings, businessAccountId: e.target.value })}
                    placeholder="123456789012345"
                  />
                </div>
                <div>
                  <Label>API Version</Label>
                  <TextInput
                    value={settings.apiVersion || 'v18.0'}
                    onChange={(e) => onSettingsChange({ ...settings, apiVersion: e.target.value })}
                    placeholder="v18.0"
                  />
                </div>
              </div>
            </div>
          )}
        </Card>
      )}

      {/* Templates Sub-tab */}
      {activeSubTab === 'templates' && (
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold">WhatsApp Templates</h3>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={loadTemplates}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
              <Button size="sm" onClick={() => {
                setEditingTemplate(null);
                setTemplateForm({ code: '', name: '', description: '', category: 'General', templateId: '', messageBody: '', language: 'en', isActive: true, notes: '' });
                setShowTemplateModal(true);
              }}>
                <Plus className="h-4 w-4 mr-2" />
                Add Template
              </Button>
            </div>
          </div>

          {templatesLoading ? (
            <LoadingSpinner message="Loading templates..." />
          ) : templates.length === 0 ? (
            <EmptyState title="No WhatsApp Templates" description="Create your first WhatsApp template to get started" />
          ) : (
            <GridComponent
              ref={gridRef}
              dataSource={templates}
              allowPaging
              allowSorting
              allowFiltering
              pageSettings={{ pageSize: 10 }}
              filterSettings={{ type: 'Menu' }}
              toolbar={['Search', 'ExcelExport']}
              toolbarClick={(args: any) => {
                if (gridRef.current && args.item.id.includes('excelexport')) {
                  gridRef.current.excelExport({ fileName: 'WhatsAppTemplates.xlsx' });
                }
              }}
            >
              <ColumnsDirective>
                <ColumnDirective field="code" headerText="Code" width="120" />
                <ColumnDirective field="name" headerText="Name" width="180" />
                <ColumnDirective field="category" headerText="Category" width="120" />
                <ColumnDirective field="templateId" headerText="Template ID" width="150" />
                <ColumnDirective field="approvalStatus" headerText="Approval" width="100" template={approvalTemplate} />
                <ColumnDirective field="isActive" headerText="Status" width="100" template={statusTemplate} />
                <ColumnDirective headerText="Actions" width="100" template={actionsTemplate} />
              </ColumnsDirective>
              <Inject services={[Page, Sort, Filter, Toolbar, ExcelExport]} />
            </GridComponent>
          )}
        </Card>
      )}

      {/* Template Modal */}
      <Modal
        isOpen={showTemplateModal}
        onClose={() => setShowTemplateModal(false)}
        title={editingTemplate ? 'Edit WhatsApp Template' : 'New WhatsApp Template'}
        size="large"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Code</Label>
              <TextInput
                value={templateForm.code}
                onChange={(e) => setTemplateForm({ ...templateForm, code: e.target.value })}
                placeholder="TEMPLATE_CODE"
                disabled={!!editingTemplate}
              />
            </div>
            <div>
              <Label>Name</Label>
              <TextInput
                value={templateForm.name}
                onChange={(e) => setTemplateForm({ ...templateForm, name: e.target.value })}
                placeholder="Template Name"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Category</Label>
              <Select
                value={templateForm.category}
                onChange={(e) => setTemplateForm({ ...templateForm, category: e.target.value })}
                options={[
                  { value: 'General', label: 'General' },
                  { value: 'Order', label: 'Order' },
                  { value: 'Appointment', label: 'Appointment' },
                  { value: 'Marketing', label: 'Marketing' },
                  { value: 'Utility', label: 'Utility' }
                ]}
              />
            </div>
            <div>
              <Label>WhatsApp Template ID</Label>
              <TextInput
                value={templateForm.templateId || ''}
                onChange={(e) => setTemplateForm({ ...templateForm, templateId: e.target.value })}
                placeholder="Meta-approved template ID"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label>Language</Label>
              <Select
                value={templateForm.language || 'en'}
                onChange={(e) => setTemplateForm({ ...templateForm, language: e.target.value })}
                options={[
                  { value: 'en', label: 'English' },
                  { value: 'ms', label: 'Malay' },
                  { value: 'zh', label: 'Chinese' }
                ]}
              />
            </div>
            <div className="flex items-center gap-2 pt-6">
              <Switch
                checked={templateForm.isActive ?? true}
                onCheckedChange={(checked) => setTemplateForm({ ...templateForm, isActive: checked })}
              />
              <span className="text-sm">Active</span>
            </div>
          </div>
          <div>
            <Label>Message Body</Label>
            <Textarea
              value={templateForm.messageBody || ''}
              onChange={(e) => setTemplateForm({ ...templateForm, messageBody: e.target.value })}
              placeholder="Enter WhatsApp message body. Use {{1}}, {{2}} for placeholders."
              rows={4}
            />
          </div>
          <div>
            <Label>Description</Label>
            <TextInput
              value={templateForm.description || ''}
              onChange={(e) => setTemplateForm({ ...templateForm, description: e.target.value })}
              placeholder="Optional description"
            />
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => setShowTemplateModal(false)}>Cancel</Button>
            <Button onClick={handleSaveTemplate}>
              {editingTemplate ? 'Update' : 'Create'} Template
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

// ============================================================================
// Main Integrations Page
// ============================================================================
const IntegrationsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { activeDepartment } = useDepartment();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'email' | 'myinvois' | 'sms' | 'whatsapp'>('email');
  const [settings, setSettings] = useState<IntegrationSettings | null>(null);

  const companyId = activeDepartment?.companyId || '';

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      const data = await getIntegrationSettings();
      setSettings(data);
    } catch (err: any) {
      showError(err.message || 'Failed to load integration settings');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveMyInvois = async () => {
    if (!settings) return;
    try {
      setSaving(true);
      await updateMyInvoisSettings(settings.myInvois);
      showSuccess('MyInvois settings saved successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to save MyInvois settings');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveSms = async () => {
    if (!settings) return;
    try {
      setSaving(true);
      await updateSmsSettings(settings.sms);
      showSuccess('SMS settings saved successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to save SMS settings');
    } finally {
      setSaving(false);
    }
  };

  const handleSaveWhatsApp = async () => {
    if (!settings) return;
    try {
      setSaving(true);
      await updateWhatsAppSettings(settings.whatsApp);
      showSuccess('WhatsApp settings saved successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to save WhatsApp settings');
    } finally {
      setSaving(false);
    }
  };

  const handleTestMyInvois = async () => {
    try {
      setTesting('myinvois');
      const result = await testMyInvoisConnection();
      if (result.connected) {
        showSuccess('MyInvois connection test successful');
      } else {
        showError('MyInvois connection test failed');
      }
    } catch (err: any) {
      showError(err.message || 'Failed to test MyInvois connection');
    } finally {
      setTesting(null);
    }
  };

  const handleTestSms = async () => {
    try {
      setTesting('sms');
      const result = await testSmsConnection();
      if (result.connected) {
        showSuccess('SMS connection test successful');
      } else {
        showError('SMS connection test failed');
      }
    } catch (err: any) {
      showError(err.message || 'Failed to test SMS connection');
    } finally {
      setTesting(null);
    }
  };

  const handleTestWhatsApp = async () => {
    try {
      setTesting('whatsapp');
      const result = await testWhatsAppConnection();
      if (result.connected) {
        showSuccess('WhatsApp connection test successful');
      } else {
        showError('WhatsApp connection test failed');
      }
    } catch (err: any) {
      showError(err.message || 'Failed to test WhatsApp connection');
    } finally {
      setTesting(null);
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading integration settings..." fullPage />;
  }

  if (!settings) {
    return <EmptyState title="Error" description="Failed to load integration settings" />;
  }

  return (
    <PageShell
      title="Integrations"
      actions={
        <Button size="sm" variant="outline" onClick={loadSettings}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      }
    >
      <div className="space-y-6">
        {/* Main Tab Navigation */}
        <div className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
          <button
            onClick={() => setActiveTab('email')}
            className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'email'
                ? 'border-orange-600 text-orange-600'
                : 'border-transparent text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100'
            }`}
          >
            <Mail className="h-4 w-4" />
            Email
          </button>
          <button
            onClick={() => setActiveTab('myinvois')}
            className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'myinvois'
                ? 'border-purple-600 text-purple-600'
                : 'border-transparent text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100'
            }`}
          >
            <FileText className="h-4 w-4" />
            MyInvois (e-Invoice)
          </button>
          <button
            onClick={() => setActiveTab('sms')}
            className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'sms'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100'
            }`}
          >
            <MessageSquare className="h-4 w-4" />
            SMS
          </button>
          <button
            onClick={() => setActiveTab('whatsapp')}
            className={`px-6 py-3 font-medium text-sm border-b-2 transition-colors flex items-center gap-2 ${
              activeTab === 'whatsapp'
                ? 'border-green-600 text-green-600'
                : 'border-transparent text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100'
            }`}
          >
            <MessageCircle className="h-4 w-4" />
            WhatsApp
          </button>
        </div>

        {/* Tab Content */}
        {activeTab === 'email' && (
          <EmailConfigTab companyId={companyId} />
        )}

        {activeTab === 'myinvois' && (
          <MyInvoisSettingsTab
            settings={settings.myInvois}
            onSettingsChange={(myInvois) => setSettings({ ...settings, myInvois })}
            onSave={handleSaveMyInvois}
            onTest={handleTestMyInvois}
            saving={saving}
            testing={testing === 'myinvois'}
          />
        )}

        {activeTab === 'sms' && (
          <SmsSettingsTab
            settings={settings.sms}
            onSettingsChange={(sms) => setSettings({ ...settings, sms })}
            onSave={handleSaveSms}
            onTest={handleTestSms}
            saving={saving}
            testing={testing === 'sms'}
            companyId={companyId}
          />
        )}

        {activeTab === 'whatsapp' && (
          <WhatsAppSettingsTab
            settings={settings.whatsApp}
            onSettingsChange={(whatsApp) => setSettings({ ...settings, whatsApp })}
            onSave={handleSaveWhatsApp}
            onTest={handleTestWhatsApp}
            saving={saving}
            testing={testing === 'whatsapp'}
            companyId={companyId}
          />
        )}
      </div>
    </PageShell>
  );
};

export default IntegrationsPage;
