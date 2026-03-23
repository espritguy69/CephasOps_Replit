import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';
import { Save, Rocket, Copy, FlaskConical, History, AlertTriangle } from 'lucide-react';
import DOMPurify from 'dompurify';
import { marked } from 'marked';
import { PageShell } from '../../components/layout';
import { Badge, Button, Card, useToast } from '../../components/ui';
import { Separator } from '../../components/ui/separator';
import TemplateMetaForm, { TemplateFormValues } from '../../components/doc-templates/TemplateMetaForm';
import TemplateContentEditor from '../../components/doc-templates/TemplateContentEditor';
import TemplatePreviewPanel from '../../components/doc-templates/TemplatePreviewPanel';
import TestRenderDialog from '../../components/doc-templates/TestRenderDialog';
import {
  CONTENT_FORMATS,
  DEFAULT_ALLOWED_VARIABLES,
  DEFAULT_RECOMMENDED_VARIABLES,
  DEFAULT_TEMPLATE_CATEGORIES,
  OUTPUT_TYPES,
} from '../../lib/docTemplates/constants';
import {
  extractPlaceholders,
  findMissingRecommended,
  findUnknownPlaceholders,
} from '../../lib/docTemplates/placeholder';
import {
  createTemplate,
  duplicateTemplate,
  getTemplate,
  getTemplateCategories,
  getTemplateVariables,
  publishTemplate,
  updateTemplate,
} from '../../api/docTemplates';

type TemplateMetadata = {
  description?: string;
  tags?: string[];
  outputType?: string;
  contentFormat?: string;
};

const buildMetadataJson = (existingJsonSchema: string | undefined, metadata: TemplateMetadata) => {
  if (!metadata) return existingJsonSchema;
  try {
    const parsed = existingJsonSchema ? JSON.parse(existingJsonSchema) : null;
    const schema = parsed?.schema ?? (parsed && !parsed?.metadata ? parsed : null);
    const payload = {
      ...(schema ? { schema } : {}),
      metadata,
    };
    return JSON.stringify(payload);
  } catch {
    return JSON.stringify({ metadata });
  }
};

const parseMetadata = (jsonSchema?: string): TemplateMetadata => {
  if (!jsonSchema) return {};
  try {
    const parsed = JSON.parse(jsonSchema);
    if (parsed?.metadata && typeof parsed.metadata === 'object') {
      return parsed.metadata as TemplateMetadata;
    }
    if (typeof parsed === 'object') {
      return parsed as TemplateMetadata;
    }
    return {};
  } catch {
    return {};
  }
};

const createSchema = (allowedVariables: string[]) =>
  z
    .object({
      name: z.string().min(3, 'Template name must be at least 3 characters'),
      category: z.string().min(1, 'Category is required'),
      outputType: z.string().min(1, 'Output type is required'),
      status: z.enum(['Draft', 'Active']),
      tags: z.array(z.string()).default([]),
      description: z.string().optional(),
      content: z.string().min(1, 'Template content is required'),
      contentFormat: z.enum(['Markdown', 'HTML']),
    })
    .superRefine((data, ctx) => {
      if (data.status === 'Active') {
        const unknown = findUnknownPlaceholders(data.content, allowedVariables);
        if (unknown.length > 0) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ['content'],
            message: `Unknown placeholders: ${unknown.join(', ')}`,
          });
        }
      }
    });

const DocumentTemplateEditorPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams();
  const { showError, showSuccess } = useToast();
  const editorRef = useRef<HTMLTextAreaElement>(null);
  const [templateId, setTemplateId] = useState<string | null>(id || null);
  const [categories, setCategories] = useState<string[]>(DEFAULT_TEMPLATE_CATEGORIES);
  const [allowedVariables, setAllowedVariables] = useState<string[]>(DEFAULT_ALLOWED_VARIABLES);
  const [categoriesError, setCategoriesError] = useState<string | null>(null);
  const [variablesError, setVariablesError] = useState<string | null>(null);
  const [categoriesLoading, setCategoriesLoading] = useState<boolean>(false);
  const [variablesLoading, setVariablesLoading] = useState<boolean>(false);
  const [templateError, setTemplateError] = useState<string | null>(null);
  const [templateLoading, setTemplateLoading] = useState<boolean>(!!id);
  const [previewHtml, setPreviewHtml] = useState<string>('');
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [saveState, setSaveState] = useState<'saved' | 'saving' | 'unsaved' | 'error'>('saved');
  const [testRenderOpen, setTestRenderOpen] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const [existingJsonSchema, setExistingJsonSchema] = useState<string | undefined>(undefined);

  const schema = useMemo(() => createSchema(allowedVariables), [allowedVariables]);

  const { register, handleSubmit, reset, setValue, formState, control } = useForm<TemplateFormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      category: DEFAULT_TEMPLATE_CATEGORIES[0],
      outputType: OUTPUT_TYPES[0],
      status: 'Draft',
      tags: [],
      description: '',
      content: '',
      contentFormat: CONTENT_FORMATS[1],
    },
    mode: 'onChange',
  });

  useEffect(() => {
    register('category');
    register('outputType');
    register('status');
    register('content');
    register('contentFormat');
    register('tags');
  }, [register]);

  const [
    name,
    category,
    outputType,
    status,
    tags,
    description,
    content,
    contentFormat,
  ] = useWatch({
    control,
    name: ['name', 'category', 'outputType', 'status', 'tags', 'description', 'content', 'contentFormat'],
  });

  const unknownPlaceholders = useMemo(
    () => findUnknownPlaceholders(content || '', allowedVariables),
    [content, allowedVariables]
  );
  const missingRecommended = useMemo(
    () => findMissingRecommended(content || '', DEFAULT_RECOMMENDED_VARIABLES),
    [content]
  );

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        setCategoriesLoading(true);
        const list = await getTemplateCategories();
        if (Array.isArray(list) && list.length > 0) {
          setCategories(list);
        }
      } catch (err: any) {
        setCategoriesError(err?.message || 'Failed to load categories');
      } finally {
        setCategoriesLoading(false);
      }
    };
    const fetchVariables = async () => {
      try {
        setVariablesLoading(true);
        const list = await getTemplateVariables();
        if (Array.isArray(list) && list.length > 0) {
          setAllowedVariables(list);
        }
      } catch (err: any) {
        setVariablesError(err?.message || 'Failed to load variables');
      } finally {
        setVariablesLoading(false);
      }
    };
    fetchCategories();
    fetchVariables();
  }, []);

  useEffect(() => {
    if (!id) {
      setTemplateLoading(false);
      setIsInitialized(true);
      return;
    }
    const loadTemplate = async () => {
      try {
        setTemplateLoading(true);
        setTemplateError(null);
        const template = await getTemplate(id);
        const metadata = parseMetadata(template.jsonSchema);
        reset({
          name: template.name || '',
          category: template.documentType || DEFAULT_TEMPLATE_CATEGORIES[0],
          outputType: (metadata.outputType as any) || OUTPUT_TYPES[0],
          status: template.isActive ? 'Active' : 'Draft',
          tags: template.tags && template.tags.length > 0 ? template.tags : metadata.tags || [],
          description: template.description || metadata.description || '',
          content: template.htmlBody || '',
          contentFormat: (metadata.contentFormat as any) || CONTENT_FORMATS[1],
        });
        setTemplateId(template.id);
        setExistingJsonSchema(template.jsonSchema);
        setSaveState('saved');
      } catch (err: any) {
        setTemplateError(err?.message || 'Failed to load document template');
      } finally {
        setTemplateLoading(false);
        setIsInitialized(true);
      }
    };
    loadTemplate();
  }, [id, reset]);

  useEffect(() => {
    const timer = setTimeout(() => {
      if (!content) {
        setPreviewError(null);
        setPreviewHtml('<p class="text-muted-foreground">Start typing to preview your template.</p>');
        return;
      }
      try {
        const rawHtml = contentFormat === 'Markdown' ? marked.parse(content) : content;
        const sanitized = DOMPurify.sanitize(rawHtml);
        setPreviewError(null);
        setPreviewHtml(sanitized);
      } catch (err: any) {
        setPreviewError(err?.message || 'Preview failed to render. Please check your template formatting.');
        setPreviewHtml('');
      }
    }, 400);
    return () => clearTimeout(timer);
  }, [content, contentFormat]);

  useEffect(() => {
    if (!isInitialized) return;
    if (formState.isDirty) {
      setSaveState('unsaved');
    }
    if (!formState.isDirty || status !== 'Draft') {
      return;
    }
    const timer = setTimeout(() => {
      void handleSave('Draft', true);
    }, 2500);
    return () => clearTimeout(timer);
  }, [name, category, outputType, status, tags, description, content, contentFormat, formState.isDirty, isInitialized]);

  useEffect(() => {
    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      if (formState.isDirty) {
        event.preventDefault();
        event.returnValue = '';
      }
    };
    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [formState.isDirty]);


  const handleInsertVariable = (variableName: string) => {
    const placeholder = `{{${variableName}}}`;
    const textarea = editorRef.current;
    if (!textarea) {
      setValue('content', `${content || ''}${placeholder}`, { shouldDirty: true, shouldValidate: true });
      return;
    }
    const start = textarea.selectionStart ?? 0;
    const end = textarea.selectionEnd ?? 0;
    const nextValue = `${content?.slice(0, start) ?? ''}${placeholder}${content?.slice(end) ?? ''}`;
    setValue('content', nextValue, { shouldDirty: true, shouldValidate: true });
    requestAnimationFrame(() => {
      textarea.focus();
      const caret = start + placeholder.length;
      textarea.setSelectionRange(caret, caret);
    });
  };

  const handleSave = async (nextStatus?: 'Draft' | 'Active', silent = false): Promise<string | null> => {
    if (!isInitialized) return null;
    if (silent && !templateId && (!name?.trim() || !content?.trim())) {
      return null;
    }
    try {
      setSaveState('saving');
      const metadata: TemplateMetadata = {
        description,
        tags,
        outputType,
        contentFormat,
      };
      const jsonSchema = buildMetadataJson(existingJsonSchema, metadata);
      const payload = {
        name: name || '',
        documentType: category || DEFAULT_TEMPLATE_CATEGORIES[0],
        engine: 'Handlebars' as const,
        htmlBody: content || '',
        jsonSchema,
        isActive: nextStatus === 'Active' ? true : false,
        description: description || undefined,
        tags: tags || [],
      };
      if (templateId) {
        const updated = await updateTemplate(templateId, payload);
        const updatedMetadata = parseMetadata(updated.jsonSchema);
        setExistingJsonSchema(updated.jsonSchema);
        reset({
          name: updated.name,
          category: updated.documentType,
          outputType: (updatedMetadata.outputType as any) || outputType,
          status: updated.isActive ? 'Active' : 'Draft',
          tags: updated.tags && updated.tags.length > 0 ? updated.tags : updatedMetadata.tags || tags,
          description: updated.description || updatedMetadata.description || description,
          content: updated.htmlBody,
          contentFormat: (updatedMetadata.contentFormat as any) || contentFormat,
        });
        setSaveState('saved');
        if (!silent) {
          showSuccess('Template saved');
        }
        return updated.id;
      } else {
        const created = await createTemplate(payload);
        setTemplateId(created.id);
        setExistingJsonSchema(created.jsonSchema);
        navigate(`/doc-templates/${created.id}`, { replace: true });
        setSaveState('saved');
        if (!silent) {
          showSuccess('Template saved');
        }
        return created.id;
      }
    } catch (err: any) {
      setSaveState('error');
      if (!silent) {
        showError(err?.message || 'Failed to save template');
      }
      return null;
    }
  };

  const onSaveDraft = () => {
    setValue('status', 'Draft', { shouldDirty: true, shouldValidate: true });
    void handleSubmit(async () => {
      await handleSave('Draft');
    })();
  };

  const onPublish = handleSubmit(async () => {
    if (unknownPlaceholders.length > 0) {
      showError('Remove unknown placeholders before publishing.');
      return;
    }
    const savedId = await handleSave('Draft', true);
    if (!savedId) return;
    try {
      setSaveState('saving');
      const published = await publishTemplate(savedId);
      reset({
        name: published.name,
        category: published.documentType,
        outputType,
        status: 'Active',
        tags: published.tags && published.tags.length > 0 ? published.tags : tags,
        description: published.description || description,
        content: published.htmlBody,
        contentFormat,
      });
      setSaveState('saved');
      showSuccess('Template published');
    } catch (err: any) {
      setSaveState('error');
      showError(err?.message || 'Failed to publish template');
    }
  });

  const onDuplicate = async () => {
    if (!templateId) return;
    try {
      const duplicate = await duplicateTemplate(templateId);
      navigate(`/doc-templates/${duplicate.id}`);
      showSuccess('Template duplicated');
    } catch (err: any) {
      showError(err?.message || 'Failed to duplicate template');
    }
  };

  const statusBadge = () => {
    switch (saveState) {
      case 'saving':
        return <Badge variant="secondary">Saving...</Badge>;
      case 'saved':
        return <Badge variant="outline">Saved</Badge>;
      case 'unsaved':
        return <Badge variant="secondary">Unsaved changes</Badge>;
      default:
        return <Badge variant="destructive">Save error</Badge>;
    }
  };

  const actions = (
    <div className="flex flex-wrap items-center gap-2">
      {statusBadge()}
      <Button variant="outline" onClick={onSaveDraft}>
        <Save className="mr-2 h-4 w-4" />
        Save Draft
      </Button>
      <Button onClick={onPublish}>
        <Rocket className="mr-2 h-4 w-4" />
        Publish
      </Button>
      {templateId && (
        <Button variant="outline" onClick={onDuplicate}>
          <Copy className="mr-2 h-4 w-4" />
          Duplicate
        </Button>
      )}
      <Button variant="outline" onClick={() => setTestRenderOpen(true)}>
        <FlaskConical className="mr-2 h-4 w-4" />
        Test Render
      </Button>
      <Button variant="ghost" disabled>
        <History className="mr-2 h-4 w-4" />
        Version History
      </Button>
    </div>
  );

  return (
    <PageShell
      title={templateId ? 'Edit Document Template' : 'Create Document Template'}
      actions={actions}
      breadcrumbs={[
        { label: 'Settings', path: '/settings' },
        { label: 'Document Templates', path: '/document-templates' },
        { label: templateId ? 'Edit' : 'Create' },
      ]}
    >
      <div className="p-4 space-y-4">
        {templateError && (
          <Card className="p-4 flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-4 w-4" />
            {templateError}
          </Card>
        )}
        {(categoriesLoading || variablesLoading) && (
          <Card className="p-4 text-xs text-muted-foreground">
            {categoriesLoading && <div>Loading categories...</div>}
            {variablesLoading && <div>Loading variables...</div>}
          </Card>
        )}
        {(categoriesError || variablesError) && (
          <Card className="p-4 space-y-2 text-xs text-muted-foreground">
            {categoriesError && <div>Categories: {categoriesError}</div>}
            {variablesError && <div>Variables: {variablesError}</div>}
          </Card>
        )}
        {templateLoading ? (
          <Card className="p-6 flex items-center justify-center text-sm text-muted-foreground">
            Loading template...
          </Card>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
            <div className="space-y-4 lg:col-span-7">
              <TemplateMetaForm
                register={register}
                setValue={setValue}
                errors={formState.errors}
                categories={categories}
                outputTypes={[...OUTPUT_TYPES]}
                statusOptions={['Draft', 'Active']}
                tags={tags || []}
                category={category || DEFAULT_TEMPLATE_CATEGORIES[0]}
                outputType={(outputType || OUTPUT_TYPES[0]) as any}
                status={(status || 'Draft') as 'Draft' | 'Active'}
              />
              <Separator />
              <Card className="p-4">
                <TemplateContentEditor
                  content={content || ''}
                  contentFormat={contentFormat || 'HTML'}
                  onContentChange={(value) => setValue('content', value, { shouldDirty: true, shouldValidate: true })}
                  onFormatChange={(value) =>
                    setValue('contentFormat', value, { shouldDirty: true, shouldValidate: true })
                  }
                  editorRef={editorRef}
                  error={formState.errors.content?.message}
                />
              </Card>
            </div>
            <div className="lg:col-span-5 lg:sticky lg:top-4 lg:self-start">
              <Card className="p-4">
                <TemplatePreviewPanel
                  outputType={outputType || 'PDF'}
                  previewHtml={previewHtml}
                  previewError={previewError}
                  variables={allowedVariables}
                  onInsertVariable={handleInsertVariable}
                  variablesLoading={variablesLoading}
                  variablesError={variablesError}
                  unknownPlaceholders={unknownPlaceholders}
                  missingRecommended={missingRecommended}
                />
              </Card>
            </div>
          </div>
        )}
      </div>

      <TestRenderDialog
        open={testRenderOpen}
        onClose={() => setTestRenderOpen(false)}
        templateContent={content || ''}
        outputType={outputType || 'PDF'}
      />
    </PageShell>
  );
};

export default DocumentTemplateEditorPage;
