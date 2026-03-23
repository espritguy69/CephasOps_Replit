import React, { useEffect, useMemo, useState } from 'react';
import {
  Plus,
  Trash2,
  Edit,
  Search,
  AlertCircle,
  Power,
  Copy,
  LayoutGrid,
  List,
  FileText,
  Receipt,
  FileSpreadsheet,
  Truck,
  ShoppingCart,
  ClipboardList,
  Package
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import {
  useActivateDocumentTemplate,
  useCarboneStatus,
  useDeleteDocumentTemplate,
  useDocumentTemplates,
  useDuplicateDocumentTemplate,
  useUpdateDocumentTemplate
} from '../../hooks/useDocumentTemplates';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type {
  DocumentTemplate,
  DocumentTemplateFilters
} from '../../types/documentTemplates';

interface DocumentTemplateRow extends DocumentTemplate {
  displayName?: string;
  templateEngine?: string;
  isDefault?: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

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

const DOCUMENT_TYPE_CONFIG: Record<string, { label: string; icon: React.ElementType; color: string }> = {
  Invoice: { label: 'Invoice', icon: FileText, color: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
  JobDocket: { label: 'Job Docket', icon: ClipboardList, color: 'bg-slate-100 text-slate-800 dark:bg-slate-700 dark:text-slate-200' },
  RmaForm: { label: 'RMA Form', icon: FileText, color: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300' },
  PurchaseOrder: { label: 'Purchase Order', icon: ShoppingCart, color: 'bg-violet-100 text-violet-800 dark:bg-violet-900/40 dark:text-violet-300' },
  Quotation: { label: 'Quotation', icon: FileText, color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300' },
  BOQ: { label: 'BOQ', icon: FileSpreadsheet, color: 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300' },
  DeliveryOrder: { label: 'Delivery Order', icon: Truck, color: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300' },
  PaymentReceipt: { label: 'Receipt', icon: Receipt, color: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' }
};

function getTypeConfig(documentType: string) {
  return DOCUMENT_TYPE_CONFIG[documentType] ?? {
    label: documentType || 'Document',
    icon: Package,
    color: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
  };
}

type ViewMode = 'cards' | 'table';

const DocumentTemplatesPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useToast();
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [viewMode, setViewMode] = useState<ViewMode>('cards');
  const [filters, setFilters] = useState<DocumentTemplateFilters>({ documentType: '' });

  const templatesQuery = useDocumentTemplates(filters);
  const carboneStatusQuery = useCarboneStatus();
  const updateMutation = useUpdateDocumentTemplate();
  const deleteMutation = useDeleteDocumentTemplate();
  const activateMutation = useActivateDocumentTemplate();
  const duplicateMutation = useDuplicateDocumentTemplate();

  const templates = (templatesQuery.data || []) as DocumentTemplateRow[];
  const carboneStatus = carboneStatusQuery.data;

  useEffect(() => {
    if (templatesQuery.error) {
      showError((templatesQuery.error as Error).message || 'Failed to load document templates');
    }
  }, [templatesQuery.error, showError]);

  useEffect(() => {
    if (carboneStatusQuery.error) {
      showError((carboneStatusQuery.error as Error).message || 'Failed to load Carbone status');
    }
  }, [carboneStatusQuery.error, showError]);

  const handleDelete = async (templateId: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this template? This action cannot be undone.')) return;
    try {
      await deleteMutation.mutateAsync(templateId);
    } catch {
      // Errors are handled in mutation hooks.
    }
  };

  const handleToggleStatus = async (template: DocumentTemplateRow): Promise<void> => {
    try {
      if (template.isActive) {
        await updateMutation.mutateAsync({ id: template.id, data: { isActive: false } });
      } else {
        await activateMutation.mutateAsync(template.id);
      }
    } catch {
      // Errors are handled in mutation hooks.
    }
  };

  const handleDuplicate = async (template: DocumentTemplateRow, e: React.MouseEvent): Promise<void> => {
    e.stopPropagation();
    try {
      const duplicated = await duplicateMutation.mutateAsync(template.id);
      navigate(`/settings/document-templates/${duplicated.id}`);
    } catch {
      // Errors are handled in mutation hooks.
    }
  };

  const filteredTemplates = useMemo(() => {
    return templates.filter(template => {
      const displayName = template.name || template.displayName || '';
      const matchesSearch = !searchQuery ||
        displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (template.documentType && template.documentType.toLowerCase().includes(searchQuery.toLowerCase()));
      return matchesSearch;
    });
  }, [templates, searchQuery]);

  const columns: TableColumn<DocumentTemplateRow>[] = [
    {
      key: 'name',
      label: 'Template Name',
      render: (value, row) => row.name || row.displayName || 'Unnamed Template'
    },
    { key: 'documentType', label: 'Type' },
    {
      key: 'partnerName',
      label: 'Partner',
      render: (value, row) => row.partnerName || '-'
    },
    {
      key: 'engine',
      label: 'Engine',
      render: (value, row) => row.engine || row.templateEngine || 'Handlebars'
    },
    {
      key: 'version',
      label: 'Version',
      render: (value, row) => row.version ?? '-'
    },
    {
      key: 'isActive',
      label: 'Status',
      render: (value) => (
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${
          value
            ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
            : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
        }`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      )
    },
    {
      key: 'updatedAt',
      label: 'Updated',
      render: (value, row) => row.updatedAt ? new Date(row.updatedAt).toLocaleDateString() : '-'
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleToggleStatus(row);
            }}
            title={row.isActive ? 'Deactivate' : 'Activate'}
            className={`${row.isActive ? 'text-yellow-600' : 'text-green-600'} hover:opacity-75 cursor-pointer transition-colors`}
          >
            <Power className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDuplicate(row, e);
            }}
            title="Duplicate"
            className="text-indigo-600 hover:opacity-75 cursor-pointer transition-colors"
          >
            <Copy className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/settings/document-templates/${row.id}`);
            }}
            title="Edit"
            className="text-blue-600 hover:opacity-75 cursor-pointer transition-colors"
          >
            <Edit className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDelete(row.id);
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

  if (templatesQuery.isLoading) {
    return (
      <PageShell title="Document Templates" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Document Templates' }]}>
        <LoadingSpinner message="Loading document templates..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Document Templates"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Document Templates' }]}
      actions={
        <div className="flex items-center gap-2">
          <div className="flex rounded-lg border border-input bg-muted/30 p-0.5">
            <button
              type="button"
              onClick={() => setViewMode('cards')}
              className={`flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors ${
                viewMode === 'cards'
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
              title="Card view"
            >
              <LayoutGrid className="h-3.5 w-3.5" />
              Cards
            </button>
            <button
              type="button"
              onClick={() => setViewMode('table')}
              className={`flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium transition-colors ${
                viewMode === 'table'
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
              title="Table view"
            >
              <List className="h-3.5 w-3.5" />
              Table
            </button>
          </div>
          <Button size="sm" onClick={() => navigate('/settings/document-templates/new')} className="gap-1">
            <Plus className="h-4 w-4" />
            New Template
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {carboneStatus && !carboneStatus.enabled && (
        <Card className="border-amber-200 bg-amber-50 dark:bg-amber-950/30 dark:border-amber-800 text-amber-800 dark:text-amber-200 p-3">
          <div className="flex items-center gap-2 text-sm">
            <AlertCircle className="h-4 w-4 flex-shrink-0" />
            <span>Carbone engine is disabled. Templates using CarboneHtml or CarboneDocx will not generate PDFs until Carbone is configured.</span>
          </div>
        </Card>
      )}

      {/* Filters: type chips (receipt-management style) + search */}
      <Card className="p-3 space-y-3">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs font-medium text-muted-foreground mr-1">Type:</span>
          <button
            type="button"
            onClick={() => setFilters({ ...filters, documentType: undefined })}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
              !filters.documentType
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
            }`}
          >
            All
          </button>
          {DOCUMENT_TYPE_OPTIONS.map((type) => {
            const config = getTypeConfig(type);
            const Icon = config.icon;
            return (
              <button
                key={type}
                type="button"
                onClick={() => setFilters({ ...filters, documentType: filters.documentType === type ? undefined : type })}
                className={`flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                  filters.documentType === type
                    ? config.color + ' ring-1 ring-offset-1 ring-offset-background ring-current'
                    : 'bg-muted/60 text-muted-foreground hover:bg-muted hover:text-foreground'
                }`}
              >
                <Icon className="h-3 w-3" />
                {config.label}
              </button>
            );
          })}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="relative flex-1 min-w-[200px] max-w-sm">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
            <input
              type="text"
              placeholder="Search templates..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background pl-8 pr-3 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
          </div>
        </div>
      </Card>

      {/* Content: cards grid or table */}
      {filteredTemplates.length > 0 ? (
        viewMode === 'cards' ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filteredTemplates.map((template) => {
              const config = getTypeConfig(template.documentType || '');
              const Icon = config.icon;
              return (
                <Card
                  key={template.id}
                  className="overflow-hidden hover:shadow-md transition-shadow cursor-pointer border-border"
                  onClick={() => navigate(`/settings/document-templates/${template.id}`)}
                  hoverable
                >
                  <div className="p-4 space-y-3">
                    <div className="flex items-start justify-between gap-2">
                      <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${config.color}`}>
                        <Icon className="h-5 w-5" />
                      </div>
                      <span
                        className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${config.color}`}
                      >
                        {config.label}
                      </span>
                    </div>
                    <div>
                      <h3 className="font-medium text-sm text-foreground truncate" title={template.name || ''}>
                        {template.name || template.displayName || 'Unnamed Template'}
                      </h3>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {template.engine || template.templateEngine || 'Handlebars'}
                        {template.partnerName ? ` · ${template.partnerName}` : ''}
                      </p>
                    </div>
                    <div className="flex items-center justify-between pt-2 border-t border-border">
                      <span
                        className={`px-2 py-0.5 rounded-full text-[10px] font-medium ${
                          template.isActive
                            ? 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300'
                            : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                        }`}
                      >
                        {template.isActive ? 'Active' : 'Inactive'}
                      </span>
                      <span className="text-[10px] text-muted-foreground">
                        {template.updatedAt ? new Date(template.updatedAt).toLocaleDateString() : '—'}
                      </span>
                    </div>
                    <div
                      className="flex items-center gap-1 pt-2 border-t border-border"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <button
                        type="button"
                        onClick={() => handleToggleStatus(template)}
                        title={template.isActive ? 'Deactivate' : 'Activate'}
                        className={`p-1.5 rounded hover:bg-muted ${template.isActive ? 'text-amber-600' : 'text-green-600'}`}
                      >
                        <Power className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={(e) => handleDuplicate(template, e)}
                        title="Duplicate"
                        className="p-1.5 rounded hover:bg-muted text-indigo-600"
                      >
                        <Copy className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/settings/document-templates/${template.id}`);
                        }}
                        title="Edit"
                        className="p-1.5 rounded hover:bg-muted text-blue-600"
                      >
                        <Edit className="h-3.5 w-3.5" />
                      </button>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDelete(template.id);
                        }}
                        title="Delete"
                        className="p-1.5 rounded hover:bg-muted text-red-600"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  </div>
                </Card>
              );
            })}
          </div>
        ) : (
          <Card className="p-2">
            <DataTable
              data={filteredTemplates}
              columns={columns}
              onRowClick={(row) => navigate(`/settings/document-templates/${row.id}`)}
            />
          </Card>
        )
      ) : (
        <Card className="p-8">
          <EmptyState
            title="No templates found"
            message={searchQuery || filters.documentType ? 'Try adjusting your filters or search.' : 'Create a new document template to get started (Invoice, BOQ, Receipt, etc.).'}
          />
        </Card>
      )}
      </div>
    </PageShell>
  );
};

export default DocumentTemplatesPage;
