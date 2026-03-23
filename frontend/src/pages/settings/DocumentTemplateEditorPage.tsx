import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { AlertTriangle, ArrowLeft, Copy, File, FlaskConical, Rocket, Save, Search, Upload } from 'lucide-react';
import { uploadFile } from '../../api/files';
import {
  useActivateDocumentTemplate,
  useCarboneStatus,
  useCreateDocumentTemplate,
  useDocumentTemplate,
  usePlaceholderDefinitions,
  useUpdateDocumentTemplate
} from '../../hooks/useDocumentTemplates';
import { PageShell } from '../../components/layout';
import { Button, Card, TextInput, useToast } from '../../components/ui';
import type { DocumentEngineType, PlaceholderDefinition } from '../../types/documentTemplates';

interface TemplateFormData {
  name: string;
  documentType: string;
  engine: DocumentEngineType;
  partnerId: string | null;
  isActive: boolean;
  htmlBody: string;
  templateFileId: string | null;
  templateFileName: string | null;
}

const ENGINE_OPTIONS: { value: DocumentEngineType; label: string; description: string }[] = [
  { value: 'Handlebars', label: 'Handlebars', description: 'HTML template with Handlebars syntax (default)' },
  { value: 'CarboneHtml', label: 'Carbone HTML', description: 'HTML template rendered via Carbone' },
  { value: 'CarboneDocx', label: 'Carbone DOCX', description: 'DOCX/ODT template file rendered via Carbone' }
];

const DOCUMENT_TYPE_OPTIONS = [
  'Invoice',
  'JobDocket',
  'RmaForm',
  'PurchaseOrder',
  'Quotation',
  'BOQ',
  'DeliveryOrder',
  'PaymentReceipt'
];

const DocumentTemplateEditorPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEdit = Boolean(id);
  const { showError } = useToast();
  const editorRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [templateForm, setTemplateForm] = useState<TemplateFormData>({
    name: '',
    documentType: 'Invoice',
    engine: 'Handlebars',
    partnerId: null,
    isActive: false,
    htmlBody: '',
    templateFileId: null,
    templateFileName: null
  });
  const [placeholderQuery, setPlaceholderQuery] = useState('');
  const [previewMode, setPreviewMode] = useState(true);
  const [previewHtml, setPreviewHtml] = useState<string>('');
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [previewRetryKey, setPreviewRetryKey] = useState(0);
  const [isInitialized, setIsInitialized] = useState(false);
  const [templateError, setTemplateError] = useState<string | null>(null);

  const templateQuery = useDocumentTemplate(id || '');
  const placeholdersQuery = usePlaceholderDefinitions(templateForm.documentType);
  const carboneStatusQuery = useCarboneStatus();
  const createMutation = useCreateDocumentTemplate();
  const updateMutation = useUpdateDocumentTemplate();
  const activateMutation = useActivateDocumentTemplate();

  const placeholders = (placeholdersQuery.data || []) as PlaceholderDefinition[];
  const carboneStatus = carboneStatusQuery.data;
  const showHtmlEditor = templateForm.engine === 'Handlebars' || templateForm.engine === 'CarboneHtml';
  const showFileUpload = templateForm.engine === 'CarboneDocx';

  const placeholderKeys = useMemo(() => placeholders.map((placeholder) => placeholder.key), [placeholders]);
  const requiredKeys = useMemo(
    () => placeholders.filter((placeholder) => placeholder.isRequired || placeholder.required).map((placeholder) => placeholder.key),
    [placeholders]
  );

  const foundPlaceholders = useMemo(() => {
    if (!templateForm.htmlBody) return new Set<string>();
    const matches = Array.from(templateForm.htmlBody.matchAll(/\{\{\s*([^{}\s]+)\s*\}\}/g));
    return new Set(matches.map((match) => match[1]));
  }, [templateForm.htmlBody]);

  const unknownPlaceholders = useMemo(() => {
    if (!showHtmlEditor || !templateForm.htmlBody) return [];
    return Array.from(foundPlaceholders).filter((name) => !placeholderKeys.includes(name));
  }, [foundPlaceholders, placeholderKeys, showHtmlEditor, templateForm.htmlBody]);

  const missingRequired = useMemo(() => {
    if (!showHtmlEditor || requiredKeys.length === 0) return [];
    return requiredKeys.filter((key) => !foundPlaceholders.has(key));
  }, [foundPlaceholders, requiredKeys, showHtmlEditor]);

  const filteredPlaceholders = useMemo(() => {
    if (!placeholderQuery.trim()) return placeholders;
    const lower = placeholderQuery.toLowerCase();
    return placeholders.filter(
      (placeholder) =>
        placeholder.key.toLowerCase().includes(lower) ||
        placeholder.description?.toLowerCase().includes(lower)
    );
  }, [placeholderQuery, placeholders]);

  useEffect(() => {
    if (templateQuery.error) {
      setTemplateError((templateQuery.error as Error).message || 'Failed to load document template');
    }
  }, [templateQuery.error]);

  useEffect(() => {
    if (!isEdit) {
      setIsInitialized(true);
      return;
    }
    if (templateQuery.isLoading || !templateQuery.data) return;
    const template = templateQuery.data;
    setTemplateForm({
      name: template.name || '',
      documentType: template.documentType || 'Invoice',
      engine: template.engine || 'Handlebars',
      partnerId: template.partnerId || null,
      isActive: template.isActive ?? false,
      htmlBody: template.htmlBody || '',
      templateFileId: template.templateFileId || null,
      templateFileName: template.templateFileName || null
    });
    setIsInitialized(true);
  }, [isEdit, templateQuery.data, templateQuery.isLoading]);

  useEffect(() => {
    if (carboneStatusQuery.error) {
      showError((carboneStatusQuery.error as Error).message || 'Failed to load Carbone status');
    }
  }, [carboneStatusQuery.error, showError]);

  const handleInsertPlaceholder = (placeholderKey: string): void => {
    const placeholder = `{{${placeholderKey}}}`;
    const textarea = editorRef.current;
    if (!textarea) {
      setTemplateForm((prev) => ({ ...prev, htmlBody: `${prev.htmlBody}${placeholder}` }));
      return;
    }
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const text = textarea.value;
    const newText = text.substring(0, start) + placeholder + text.substring(end);
    setTemplateForm({ ...templateForm, htmlBody: newText });
    requestAnimationFrame(() => {
      textarea.focus();
      textarea.setSelectionRange(start + placeholder.length, start + placeholder.length);
    });
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>): Promise<void> => {
    const file = e.target.files?.[0];
    if (!file) return;

    const validTypes = [
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/vnd.oasis.opendocument.text'
    ];
    if (!validTypes.includes(file.type) && !file.name.endsWith('.docx') && !file.name.endsWith('.odt')) {
      showError('Please upload a DOCX or ODT file');
      return;
    }

    try {
      const result = await uploadFile(file, { module: 'DocumentTemplates', entityType: 'DocumentTemplate' });
      setTemplateForm((prev) => ({
        ...prev,
        templateFileId: result.id,
        templateFileName: result.fileName || file.name
      }));
    } catch (err: any) {
      showError(err.message || 'Failed to upload template file');
    } finally {
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleRemoveFile = (): void => {
    setTemplateForm((prev) => ({
      ...prev,
      templateFileId: null,
      templateFileName: null
    }));
  };

  const handleEngineChange = (newEngine: DocumentEngineType): void => {
    if (templateForm.engine === 'CarboneDocx' && newEngine !== 'CarboneDocx' && templateForm.templateFileId) {
      if (!window.confirm('Switching engine will remove the uploaded template file. Continue?')) {
        return;
      }
      setTemplateForm((prev) => ({
        ...prev,
        engine: newEngine,
        templateFileId: null,
        templateFileName: null
      }));
      return;
    }
    if (newEngine === 'CarboneDocx' && templateForm.htmlBody) {
      if (!window.confirm('Switching to DOCX engine will hide the HTML editor. Your HTML content will be preserved but not used. Continue?')) {
        return;
      }
    }
    setTemplateForm((prev) => ({ ...prev, engine: newEngine }));
  };

  const validateForm = (): boolean => {
    if (!templateForm.name.trim()) {
      showError('Please enter a template name');
      return false;
    }
    if (templateForm.engine === 'CarboneDocx') {
      if (!templateForm.templateFileId) {
        showError('Please upload a DOCX/ODT template file for CarboneDocx engine');
        return false;
      }
    } else if (!templateForm.htmlBody.trim()) {
      showError('Please enter HTML template content');
      return false;
    }
    if ((templateForm.engine === 'CarboneHtml' || templateForm.engine === 'CarboneDocx') && carboneStatus && !carboneStatus.enabled) {
      if (!window.confirm('Carbone engine is not enabled on the server. Templates using Carbone will fail to generate PDFs until Carbone is configured. Continue anyway?')) {
        return false;
      }
    }
    return true;
  };

  const saveTemplate = async (nextIsActive?: boolean): Promise<string | null> => {
    if (!isInitialized) return null;
    if (!validateForm()) return null;

    const payload = {
      name: templateForm.name.trim(),
      documentType: templateForm.documentType,
      engine: templateForm.engine,
      htmlBody: templateForm.htmlBody,
      templateFileId: templateForm.templateFileId || undefined,
      isActive: typeof nextIsActive === 'boolean' ? nextIsActive : templateForm.isActive
    };

    try {
      const saved = isEdit && id
        ? await updateMutation.mutateAsync({ id, data: payload })
        : await createMutation.mutateAsync(payload);
      setTemplateForm({
        name: saved.name || '',
        documentType: saved.documentType || templateForm.documentType,
        engine: saved.engine || templateForm.engine,
        partnerId: saved.partnerId || null,
        isActive: saved.isActive ?? false,
        htmlBody: saved.htmlBody || '',
        templateFileId: saved.templateFileId || null,
        templateFileName: saved.templateFileName || null
      });
      if (!isEdit) {
        navigate(`/settings/document-templates/${saved.id}`, { replace: true });
      }
      return saved.id;
    } catch {
      return null;
    }
  };

  const handleSaveDraft = async (): Promise<void> => {
    await saveTemplate(false);
  };

  const handlePublish = async (): Promise<void> => {
    const savedId = await saveTemplate(true);
    if (!savedId) return;
    try {
      await activateMutation.mutateAsync(savedId);
      setTemplateForm((prev) => ({ ...prev, isActive: true }));
    } catch {
      // Errors are handled in mutation hooks.
    }
  };

  const handleDuplicate = async (): Promise<void> => {
    if (!isEdit) return;
    const payload = {
      name: `${templateForm.name || 'Template'} Copy`,
      documentType: templateForm.documentType,
      engine: templateForm.engine,
      htmlBody: templateForm.htmlBody,
      templateFileId: templateForm.templateFileId || undefined,
      isActive: false
    };
    try {
      const duplicated = await createMutation.mutateAsync(payload);
      navigate(`/settings/document-templates/${duplicated.id}`);
    } catch {
      // Errors are handled in mutation hooks.
    }
  };

  const generatePreview = (): string => {
    if (!templateForm.htmlBody.trim()) {
      return '<p class="text-muted-foreground">Start typing to preview your template.</p>';
    }
    let preview = templateForm.htmlBody;
    const sampleData: Record<string, string> = {
      'invoice.number': 'INV-2024-001',
      'invoice.date': new Date().toLocaleDateString(),
      'invoice.dueDate': new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toLocaleDateString(),
      'invoice.totalAmount': '1,250.00',
      'invoice.subTotal': '1,150.00',
      'invoice.taxAmount': '100.00',
      'customer.name': 'John Doe',
      'customer.address': '123 Main Street',
      'customer.city': 'Kuala Lumpur',
      'customer.postcode': '50000',
      'customer.phone': '+60 12-345 6789',
      'customer.email': 'john.doe@example.com',
      'partner.name': 'CephasOps Sdn Bhd',
      'partner.code': 'CPH',
      'order.serviceId': 'TBBN-12345',
      'order.status': 'Completed'
    };

    Object.keys(sampleData).forEach((key) => {
      const regex = new RegExp(`\\{\\{${key}\\}\\}`, 'g');
      preview = preview.replace(regex, sampleData[key]);
    });

    preview = preview.replace(
      /\{\{([^}]+)\}\}/g,
      '<span style="background: #fff3cd; padding: 2px 4px; border-radius: 3px;">{{$1}}</span>'
    );
    return preview;
  };

  const runPreview = () => {
    try {
      const nextPreview = generatePreview();
      setPreviewError(null);
      setPreviewHtml(nextPreview);
    } catch (err: any) {
      setPreviewError(err?.message || 'Preview failed to render. Please check your template formatting.');
      setPreviewHtml('');
    }
  };

  useEffect(() => {
    if (!showHtmlEditor || !previewMode) {
      setPreviewError(null);
      setPreviewHtml('');
      return;
    }
    const timer = setTimeout(() => {
      runPreview();
    }, 400);
    return () => clearTimeout(timer);
  }, [showHtmlEditor, previewMode, templateForm.htmlBody, previewRetryKey]);

  if (templateQuery.isLoading && isEdit) {
    return (
      <PageShell
        title="Document Template"
        breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Document Templates', path: '/settings/document-templates' }, { label: '...' }]}
      >
        <Card className="p-6 flex items-center justify-center text-sm text-muted-foreground">
          Loading template...
        </Card>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={isEdit ? 'Edit Document Template' : 'Create Document Template'}
      breadcrumbs={[
        { label: 'Settings', path: '/settings' },
        { label: 'Document Templates', path: '/settings/document-templates' },
        { label: isEdit ? (templateForm.name || 'Template') : 'New' }
      ]}
      actions={
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="ghost" onClick={() => navigate('/settings/document-templates')}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
          <Button variant="outline" onClick={handleSaveDraft} disabled={createMutation.isPending || updateMutation.isPending}>
            <Save className="h-4 w-4 mr-2" />
            Save Draft
          </Button>
          <Button onClick={handlePublish} disabled={createMutation.isPending || updateMutation.isPending || activateMutation.isPending}>
            <Rocket className="h-4 w-4 mr-2" />
            Publish
          </Button>
          {isEdit && (
            <Button variant="outline" onClick={handleDuplicate} disabled={createMutation.isPending}>
              <Copy className="h-4 w-4 mr-2" />
              Duplicate
            </Button>
          )}
          <Button variant="outline" onClick={() => { setPreviewMode(true); setPreviewRetryKey((key) => key + 1); }}>
            <FlaskConical className="h-4 w-4 mr-2" />
            Test Render
          </Button>
        </div>
      }
    >
      <div className="p-4 space-y-4 max-w-7xl mx-auto">
        {templateError && (
          <Card className="p-4 flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {templateError}
          </Card>
        )}

        {carboneStatus && !carboneStatus.enabled && (
          <Card className="border-amber-200 bg-amber-50 text-amber-800 p-3">
            <div className="flex items-center gap-2 text-sm">
              <AlertTriangle className="h-4 w-4 flex-shrink-0" />
              <span>Carbone engine is disabled. Templates using CarboneHtml or CarboneDocx will not generate PDFs until Carbone is configured.</span>
            </div>
          </Card>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
          <div className="space-y-4 lg:col-span-8">
            <Card className="p-4 space-y-4">
              <div className="text-xs font-semibold text-muted-foreground">Template Details</div>
              <TextInput
                label="Template Name *"
                value={templateForm.name}
                onChange={(e) => setTemplateForm((prev) => ({ ...prev, name: e.target.value }))}
                required
              />
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="text-xs font-medium">Document Type *</label>
                  <select
                    value={templateForm.documentType}
                    onChange={(e) => setTemplateForm((prev) => ({ ...prev, documentType: e.target.value }))}
                    required
                    className="flex h-9 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  >
                    {DOCUMENT_TYPE_OPTIONS.map((type) => (
                      <option key={type} value={type}>{type}</option>
                    ))}
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-medium">Template Engine *</label>
                  <select
                    value={templateForm.engine}
                    onChange={(e) => handleEngineChange(e.target.value as DocumentEngineType)}
                    required
                    className="flex h-9 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  >
                    {ENGINE_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                  <p className="text-[11px] text-muted-foreground">
                    {ENGINE_OPTIONS.find((o) => o.value === templateForm.engine)?.description}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isActiveTemplate"
                  checked={templateForm.isActive}
                  onChange={(e) => setTemplateForm((prev) => ({ ...prev, isActive: e.target.checked }))}
                  className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                />
                <label htmlFor="isActiveTemplate" className="text-xs font-medium cursor-pointer">
                  Active template
                </label>
              </div>
            </Card>

            <Card className="p-4 space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-semibold">Template Body</h3>
                {showHtmlEditor && (
                  <label className="text-xs flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={previewMode}
                      onChange={(e) => setPreviewMode(e.target.checked)}
                      className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                    />
                    Show preview
                  </label>
                )}
              </div>

              {showFileUpload && (
                <div className="space-y-2">
                  <p className="text-[11px] text-muted-foreground">
                    Upload a DOCX or ODT template file with Carbone placeholders (e.g., {'{d.customer.name}'}).
                  </p>
                  {templateForm.templateFileId ? (
                    <div className="flex items-center gap-2 rounded border border-input bg-muted/30 p-2">
                      <File className="h-4 w-4 text-blue-600" />
                      <div className="flex-1">
                        <p className="text-xs font-medium">{templateForm.templateFileName || 'Template file'}</p>
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={handleRemoveFile}
                      >
                        Remove
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => fileInputRef.current?.click()}
                      >
                        Replace
                      </Button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center hover:border-blue-400 transition-colors w-full"
                      onClick={() => fileInputRef.current?.click()}
                    >
                      <Upload className="h-8 w-8 mx-auto text-gray-400 mb-2" />
                      <p className="text-sm font-medium">Click to upload template file</p>
                      <p className="text-xs text-muted-foreground">Supported formats: DOCX, ODT</p>
                    </button>
                  )}
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".docx,.odt,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.oasis.opendocument.text"
                    onChange={handleFileUpload}
                    className="hidden"
                  />
                </div>
              )}

              {showHtmlEditor && (
                <textarea
                  ref={editorRef}
                  className="min-h-[460px] w-full rounded border border-input bg-background px-2 py-2 text-xs font-mono"
                  value={templateForm.htmlBody}
                  onChange={(e) => setTemplateForm((prev) => ({ ...prev, htmlBody: e.target.value }))}
                  placeholder={templateForm.engine === 'CarboneHtml'
                    ? 'Enter HTML template with Carbone placeholders like {d.customer.name}...'
                    : 'Enter HTML template with Handlebars placeholders like {{customer.name}}...'}
                  spellCheck={false}
                />
              )}
            </Card>
          </div>

          <div className="space-y-4 lg:col-span-4 lg:sticky lg:top-4 lg:self-start">
            <Card className="p-4 space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-semibold">Placeholders</h3>
                {placeholdersQuery.isLoading && (
                  <span className="text-[11px] text-muted-foreground">Loading...</span>
                )}
              </div>
              <div className="relative">
                <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
                <input
                  type="text"
                  placeholder="Search placeholders"
                  value={placeholderQuery}
                  onChange={(e) => setPlaceholderQuery(e.target.value)}
                  className="flex h-8 w-full rounded border border-input bg-background pl-8 pr-2 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                />
              </div>
              {placeholdersQuery.error && (
                <div className="text-xs text-destructive">
                  {(placeholdersQuery.error as Error).message || 'Failed to load placeholders.'}
                </div>
              )}
              <div className="max-h-64 overflow-auto space-y-1 rounded border border-input bg-background p-2">
                {filteredPlaceholders.length === 0 ? (
                  <p className="text-xs text-muted-foreground">No placeholders found.</p>
                ) : (
                  filteredPlaceholders.map((placeholder) => (
                    <button
                      type="button"
                      key={placeholder.id}
                      className="w-full text-left rounded border border-transparent hover:border-border hover:bg-muted/40 px-2 py-1"
                      onClick={() => handleInsertPlaceholder(placeholder.key)}
                      title={placeholder.description}
                    >
                      <div className="text-xs font-mono text-primary">{placeholder.key}</div>
                      <div className="text-[11px] text-muted-foreground">{placeholder.description}</div>
                    </button>
                  ))
                )}
              </div>
            </Card>

            {showHtmlEditor && previewMode && (
              <Card className="p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-xs font-semibold">Live Preview</h3>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => runPreview()}
                  >
                    Retry
                  </Button>
                </div>
                {previewError ? (
                  <div className="text-xs text-destructive">{previewError}</div>
                ) : (
                  <div
                    className="max-h-80 overflow-auto rounded border border-input bg-white p-2 text-xs"
                    dangerouslySetInnerHTML={{ __html: previewHtml }}
                  />
                )}
              </Card>
            )}

            {(unknownPlaceholders.length > 0 || missingRequired.length > 0) && (
              <Card className="p-4 space-y-3 border-amber-200 bg-amber-50 text-amber-900">
                <h3 className="text-xs font-semibold">Validation Warnings</h3>
                {unknownPlaceholders.length > 0 && (
                  <div className="space-y-1">
                    <p className="text-[11px]">Unknown placeholders:</p>
                    <div className="flex flex-wrap gap-2">
                      {unknownPlaceholders.map((placeholder) => (
                        <span
                          key={placeholder}
                          className="rounded bg-amber-100 px-2 py-0.5 text-[11px] font-mono text-amber-900"
                        >
                          {placeholder}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
                {missingRequired.length > 0 && (
                  <div className="space-y-1">
                    <p className="text-[11px]">Missing required placeholders:</p>
                    <div className="flex flex-wrap gap-2">
                      {missingRequired.map((placeholder) => (
                        <span
                          key={placeholder}
                          className="rounded bg-amber-100 px-2 py-0.5 text-[11px] font-mono text-amber-900"
                        >
                          {placeholder}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </Card>
            )}
          </div>
        </div>
      </div>
    </PageShell>
  );
};

export default DocumentTemplateEditorPage;
