import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Eye, Filter, X, FileText, Download, Send, Receipt, Calendar, DollarSign, Building2, Trash2 } from 'lucide-react';
import { getInvoices, createInvoice, generateInvoicePdf, submitEInvoice } from '../../api/billing';
import { getPartners } from '../../api/partners';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable, StatusBadge, Modal, TextInput, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Invoice, CreateInvoiceRequest, InvoiceFilters } from '../../types/billing';
import type { Partner } from '../../types/partners';

const INVOICE_STATUSES = [
  { value: '', label: 'All Statuses' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Sent', label: 'Sent' },
  { value: 'Paid', label: 'Paid' },
  { value: 'Overdue', label: 'Overdue' },
  { value: 'Cancelled', label: 'Cancelled' }
];

interface Guide {
  number: number;
  title: string;
  content: string;
}

interface CollapsibleGuideProps {
  title: string;
  description: string;
  guides: Guide[];
}

const CollapsibleGuide: React.FC<CollapsibleGuideProps> = ({ title, description, guides }) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  
  return (
    <div className="mb-4 border border-slate-200 rounded-lg overflow-hidden bg-slate-50">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-slate-100 transition-colors"
      >
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-slate-700">{title}</span>
          <span className="text-xs text-slate-500">— {description}</span>
        </div>
        <span className="text-xs text-slate-400">{isOpen ? '▲ Hide' : '▼ Show Guide'}</span>
      </button>
      
      {isOpen && (
        <div className="px-3 py-2 border-t border-slate-200 bg-white">
          <div className="grid grid-cols-4 gap-2">
            {guides.map((guide, idx) => (
              <div key={idx} className="flex items-start gap-2">
                <div className="flex-shrink-0 w-4 h-4 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-bold">
                  {guide.number}
                </div>
                <div>
                  <h4 className="text-xs font-semibold text-slate-800">{guide.title}</h4>
                  <p className="text-xs text-slate-600 leading-tight">{guide.content}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

interface LineItem {
  description: string;
  quantity: number | string;
  unitPrice: number | string;
  orderId: string | null;
}

interface InvoiceFormData {
  partnerId: string;
  invoiceDate: string;
  dueDate: string;
  lineItems: LineItem[];
}

interface InvoiceFiltersState {
  status: string;
  partnerId: string;
  fromDate: string;
  toDate: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const InvoicesListPage: React.FC = () => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [filters, setFilters] = useState<InvoiceFiltersState>({
    status: '',
    partnerId: '',
    fromDate: '',
    toDate: ''
  });

  // Create Invoice Modal State
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [creating, setCreating] = useState<boolean>(false);
  const [formData, setFormData] = useState<InvoiceFormData>({
    partnerId: '',
    invoiceDate: new Date().toISOString().split('T')[0],
    dueDate: '',
    lineItems: [{ description: '', quantity: 1, unitPrice: 0, orderId: null }]
  });

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    loadInvoices();
  }, [filters]);

  const loadData = async (): Promise<void> => {
    try {
      const partnersData = await getPartners();
      setPartners(Array.isArray(partnersData) ? partnersData : []);
    } catch (err: any) {
      console.error('Error loading partners:', err);
    }
  };

  const loadInvoices = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const params: InvoiceFilters = {};
      if (filters.status) params.status = filters.status;
      if (filters.partnerId) params.partnerId = filters.partnerId;
      if (filters.fromDate) params.fromDate = filters.fromDate;
      if (filters.toDate) params.toDate = filters.toDate;
      
      const data = await getInvoices(params);
      setInvoices(Array.isArray(data) ? data : []);
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to load invoices';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Error loading invoices:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusVariant = (status?: string): 'success' | 'error' | 'info' | 'default' | 'warning' => {
    const statusLower = status?.toLowerCase() || 'draft';
    if (statusLower === 'paid') return 'success';
    if (statusLower === 'overdue') return 'error';
    if (statusLower === 'sent') return 'info';
    if (statusLower === 'cancelled') return 'default';
    return 'warning'; // draft
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY');
  };

  // Create Invoice Handlers
  const handleAddLineItem = (): void => {
    setFormData(prev => ({
      ...prev,
      lineItems: [...prev.lineItems, { description: '', quantity: 1, unitPrice: 0, orderId: null }]
    }));
  };

  const handleRemoveLineItem = (index: number): void => {
    if (formData.lineItems.length > 1) {
      setFormData(prev => ({
        ...prev,
        lineItems: prev.lineItems.filter((_, i) => i !== index)
      }));
    }
  };

  const handleLineItemChange = (index: number, field: keyof LineItem, value: string | number | null): void => {
    setFormData(prev => ({
      ...prev,
      lineItems: prev.lineItems.map((item, i) => 
        i === index ? { ...item, [field]: value } : item
      )
    }));
  };

  const calculateSubtotal = (): number => {
    return formData.lineItems.reduce((sum, item) => {
      const qty = typeof item.quantity === 'string' ? parseFloat(item.quantity) || 0 : item.quantity;
      const price = typeof item.unitPrice === 'string' ? parseFloat(item.unitPrice) || 0 : item.unitPrice;
      return sum + (qty * price);
    }, 0);
  };

  const handleCreateInvoice = async (): Promise<void> => {
    if (!formData.partnerId) {
      showError('Please select a partner');
      return;
    }
    if (!formData.invoiceDate) {
      showError('Please select an invoice date');
      return;
    }
    if (formData.lineItems.length === 0 || formData.lineItems.every(item => !item.description)) {
      showError('Please add at least one line item');
      return;
    }

    try {
      setCreating(true);
      const invoiceData: CreateInvoiceRequest = {
        partnerId: formData.partnerId,
        invoiceDate: formData.invoiceDate,
        termsInDays: 45,
        dueDate: formData.dueDate || undefined,
        lineItems: formData.lineItems.filter(item => item.description).map(item => ({
          description: item.description,
          quantity: typeof item.quantity === 'string' ? parseFloat(item.quantity) || 1 : item.quantity,
          unitPrice: typeof item.unitPrice === 'string' ? parseFloat(item.unitPrice) || 0 : item.unitPrice
        }))
      };

      const result = await createInvoice(invoiceData);
      showSuccess('Invoice created successfully');
      setShowCreateModal(false);
      resetForm();
      loadInvoices();
      
      // Navigate to the new invoice detail page
      if ((result as any)?.id) {
        navigate(`/billing/invoices/${(result as any).id}`);
      }
    } catch (err: any) {
      showError(err.message || 'Failed to create invoice');
    } finally {
      setCreating(false);
    }
  };

  const resetForm = (): void => {
    setFormData({
      partnerId: '',
      invoiceDate: new Date().toISOString().split('T')[0],
      dueDate: '',
      lineItems: [{ description: '', quantity: 1, unitPrice: 0, orderId: null }]
    });
  };

  const handleDownloadPdf = async (e: React.MouseEvent, invoiceId: string): Promise<void> => {
    e.stopPropagation();
    try {
      const blob = await generateInvoicePdf(invoiceId);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `invoice-${invoiceId}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      showSuccess('PDF downloaded successfully');
    } catch (err: any) {
      showError('Failed to download PDF');
    }
  };

  const handleSubmitToPortal = async (e: React.MouseEvent, invoiceId: string): Promise<void> => {
    e.stopPropagation();
    try {
      await submitEInvoice(invoiceId);
      showSuccess('Invoice submitted to portal successfully');
      loadInvoices();
    } catch (err: any) {
      showError(err.message || 'Failed to submit to portal');
    }
  };

  const clearFilters = (): void => {
    setFilters({ status: '', partnerId: '', fromDate: '', toDate: '' });
  };

  const partnerOptions = [
    { value: '', label: 'All Partners' },
    ...partners.map(p => ({ value: p.id, label: p.name }))
  ];

  const partnerSelectOptions = [
    { value: '', label: 'Select Partner...' },
    ...partners.map(p => ({ value: p.id, label: p.name }))
  ];

  const columns: TableColumn<Invoice>[] = [
    {
      key: 'invoiceNumber',
      label: 'Invoice #',
      render: (value: unknown, invoice: Invoice) => (
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-slate-400" />
          <span className="font-mono text-sm font-medium">{invoice.invoiceNumber || invoice.id?.slice(0, 8)}</span>
        </div>
      )
    },
    {
      key: 'partnerName',
      label: 'Partner',
      render: (value: unknown, invoice: Invoice) => (
        <div className="flex items-center gap-2">
          <Building2 className="h-4 w-4 text-slate-400" />
          <span>{invoice.partnerName || 'N/A'}</span>
        </div>
      )
    },
    {
      key: 'invoiceDate',
      label: 'Invoice Date',
      render: (value: unknown, invoice: Invoice) => (
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-slate-400" />
          <span>{formatDate(invoice.invoiceDate)}</span>
        </div>
      )
    },
    {
      key: 'dueDate',
      label: 'Due Date',
      render: (value: unknown, invoice: Invoice) => formatDate(invoice.dueDate)
    },
    {
      key: 'totalAmount',
      label: 'Amount',
      render: (value: unknown, invoice: Invoice) => (
        <span className="font-semibold text-emerald-600">{formatCurrency(invoice.totalAmount)}</span>
      )
    },
    {
      key: 'status',
      label: 'Status',
      render: (value: unknown, invoice: Invoice) => (
        <StatusBadge
          status={invoice.status || 'Draft'}
          variant={getStatusVariant(invoice.status)}
        />
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (value: unknown, invoice: Invoice) => (
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/billing/invoices/${invoice.id}`);
            }}
            title="View Details"
          >
            <Eye className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => handleDownloadPdf(e, invoice.id)}
            title="Download PDF"
          >
            <Download className="h-4 w-4" />
          </Button>
          {invoice.status === 'Sent' && (
            <Button
              variant="ghost"
              size="sm"
              onClick={(e) => handleSubmitToPortal(e, invoice.id)}
              title="Submit to Portal"
            >
              <Send className="h-4 w-4" />
            </Button>
          )}
        </div>
      )
    }
  ];

  // Stats
  const stats = {
    total: invoices.length,
    draft: invoices.filter(i => i.status?.toLowerCase() === 'draft').length,
    sent: invoices.filter(i => i.status?.toLowerCase() === 'sent').length,
    paid: invoices.filter(i => i.status?.toLowerCase() === 'paid').length,
    overdue: invoices.filter(i => i.status?.toLowerCase() === 'overdue').length,
    totalAmount: invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0),
    paidAmount: invoices.filter(i => i.status?.toLowerCase() === 'paid').reduce((sum, i) => sum + (i.totalAmount || 0), 0)
  };

  if (loading && invoices.length === 0) {
    return (
      <PageShell title="Billing & Invoices" breadcrumbs={[{ label: 'Billing', path: '/billing' }, { label: 'Invoices' }]}>
        <LoadingSpinner message="Loading invoices..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Billing & Invoices"
      breadcrumbs={[{ label: 'Billing', path: '/billing' }, { label: 'Invoices' }]}
      actions={
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => setShowFilters(!showFilters)}>
            <Filter className="h-4 w-4 mr-1" />
            Filters
          </Button>
          <Button onClick={() => setShowCreateModal(true)}>
            <Plus className="h-4 w-4 mr-1" />
            Create Invoice
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* How-to Guide */}
      <CollapsibleGuide
        title="How to Manage Invoices"
        description="Create, track, and submit invoices to partners and the e-Invoice portal."
        guides={[
          {
            number: 1,
            title: "Create Invoice",
            content: "Click 'Create Invoice', select a partner, add line items with descriptions and amounts, then save."
          },
          {
            number: 2,
            title: "Invoice Lifecycle",
            content: "Draft → Sent → Paid. Overdue status is auto-applied when past due date."
          },
          {
            number: 3,
            title: "Download PDF",
            content: "Use the download button to generate and save invoice PDFs for records or sending."
          },
          {
            number: 4,
            title: "Portal Submission",
            content: "For 'Sent' invoices, click 'Submit to Portal' to upload to TIME e-Invoice/MyInvois system."
          }
        ]}
      />

      {/* Stats Cards */}
      <div className="grid grid-cols-5 gap-3 mb-4">
        <Card className="p-3">
          <div className="text-xs text-slate-500">Total Invoices</div>
          <div className="text-xl font-bold text-slate-800">{stats.total}</div>
        </Card>
        <Card className="p-3">
          <div className="text-xs text-slate-500">Draft</div>
          <div className="text-xl font-bold text-amber-600">{stats.draft}</div>
        </Card>
        <Card className="p-3">
          <div className="text-xs text-slate-500">Sent</div>
          <div className="text-xl font-bold text-blue-600">{stats.sent}</div>
        </Card>
        <Card className="p-3">
          <div className="text-xs text-slate-500">Paid</div>
          <div className="text-xl font-bold text-emerald-600">{stats.paid}</div>
        </Card>
        <Card className="p-3">
          <div className="text-xs text-slate-500">Overdue</div>
          <div className="text-xl font-bold text-red-600">{stats.overdue}</div>
        </Card>
      </div>

      {/* Revenue Summary */}
      <div className="grid grid-cols-2 gap-3 mb-4">
        <Card className="p-3 bg-gradient-to-r from-blue-50 to-indigo-50 border-blue-200">
          <div className="flex items-center gap-2 text-xs text-blue-600 mb-1">
            <DollarSign className="h-4 w-4" />
            Total Invoice Value
          </div>
          <div className="text-2xl font-bold text-blue-800">{formatCurrency(stats.totalAmount)}</div>
        </Card>
        <Card className="p-3 bg-gradient-to-r from-emerald-50 to-green-50 border-emerald-200">
          <div className="flex items-center gap-2 text-xs text-emerald-600 mb-1">
            <DollarSign className="h-4 w-4" />
            Total Paid
          </div>
          <div className="text-2xl font-bold text-emerald-800">{formatCurrency(stats.paidAmount)}</div>
        </Card>
      </div>

      {/* Filters Panel */}
      {showFilters && (
        <Card className="p-4 mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-sm font-semibold">Filter Invoices</h3>
            <button onClick={clearFilters} className="text-xs text-blue-600 hover:text-blue-800">
              Clear All
            </button>
          </div>
          <div className="grid grid-cols-4 gap-4">
            <SelectInput
              label="Status"
              value={filters.status}
              onChange={(e) => setFilters(prev => ({ ...prev, status: e.target.value }))}
              options={INVOICE_STATUSES}
            />
            <SelectInput
              label="Partner"
              value={filters.partnerId}
              onChange={(e) => setFilters(prev => ({ ...prev, partnerId: e.target.value }))}
              options={partnerOptions}
            />
            <TextInput
              label="From Date"
              type="date"
              value={filters.fromDate}
              onChange={(e) => setFilters(prev => ({ ...prev, fromDate: e.target.value }))}
            />
            <TextInput
              label="To Date"
              type="date"
              value={filters.toDate}
              onChange={(e) => setFilters(prev => ({ ...prev, toDate: e.target.value }))}
            />
          </div>
        </Card>
      )}

      {/* Error Banner */}
      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-red-800 flex items-center gap-2 text-sm" role="alert">
          {error}
          <button className="ml-auto hover:opacity-70" onClick={() => setError(null)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Content */}
      <Card>
        {invoices.length > 0 ? (
          <DataTable
            data={invoices}
            columns={columns}
            pagination={true}
            pageSize={10}
            sortable={true}
            onRowClick={(invoice: Invoice) => navigate(`/billing/invoices/${invoice.id}`)}
          />
        ) : (
          <EmptyState
            icon={<Receipt className="h-12 w-12" />}
            title="No invoices found"
            description="Create your first invoice to start billing partners."
            action={
              <Button onClick={() => setShowCreateModal(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Create Invoice
              </Button>
            }
          />
        )}
      </Card>

      {/* Create Invoice Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => { setShowCreateModal(false); resetForm(); }}
        title="Create Invoice"
        size="lg"
      >
        <div className="space-y-4">
          {/* Partner & Dates */}
          <div className="grid grid-cols-3 gap-4">
            <SelectInput
              label="Partner *"
              value={formData.partnerId}
              onChange={(e) => setFormData(prev => ({ ...prev, partnerId: e.target.value }))}
              options={partnerSelectOptions}
              required
            />
            <TextInput
              label="Invoice Date *"
              type="date"
              value={formData.invoiceDate}
              onChange={(e) => setFormData(prev => ({ ...prev, invoiceDate: e.target.value }))}
              required
            />
            <TextInput
              label="Due Date"
              type="date"
              value={formData.dueDate}
              onChange={(e) => setFormData(prev => ({ ...prev, dueDate: e.target.value }))}
              helperText="Optional - 45 days from invoice date if empty"
            />
          </div>

          {/* Line Items */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="text-sm font-medium text-slate-700">Line Items</label>
              <Button variant="ghost" size="sm" onClick={handleAddLineItem}>
                <Plus className="h-4 w-4 mr-1" />
                Add Item
              </Button>
            </div>
            
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {formData.lineItems.map((item, index) => (
                <div key={index} className="grid grid-cols-12 gap-2 items-center p-2 bg-slate-50 rounded-lg">
                  <div className="col-span-5">
                    <input
                      type="text"
                      value={item.description}
                      onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                      placeholder="Description"
                      className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                  <div className="col-span-2">
                    <input
                      type="number"
                      value={item.quantity}
                      onChange={(e) => handleLineItemChange(index, 'quantity', e.target.value)}
                      placeholder="Qty"
                      min="1"
                      className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                  <div className="col-span-3">
                    <input
                      type="number"
                      value={item.unitPrice}
                      onChange={(e) => handleLineItemChange(index, 'unitPrice', e.target.value)}
                      placeholder="Unit Price (MYR)"
                      step="0.01"
                      min="0"
                      className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    />
                  </div>
                  <div className="col-span-1 text-right text-sm font-medium text-slate-600">
                    {formatCurrency(
                      (typeof item.quantity === 'string' ? parseFloat(item.quantity) || 0 : item.quantity) *
                      (typeof item.unitPrice === 'string' ? parseFloat(item.unitPrice) || 0 : item.unitPrice)
                    )}
                  </div>
                  <div className="col-span-1 text-right">
                    {formData.lineItems.length > 1 && (
                      <button
                        type="button"
                        onClick={() => handleRemoveLineItem(index)}
                        className="text-red-500 hover:text-red-700 p-1"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>

            {/* Totals */}
            <div className="mt-4 pt-4 border-t border-slate-200 space-y-2">
              <div className="flex justify-end items-center gap-4 text-sm">
                <span className="text-slate-600">Subtotal:</span>
                <span className="font-medium w-32 text-right">{formatCurrency(calculateSubtotal())}</span>
              </div>
              <div className="flex justify-end items-center gap-4 text-sm">
                <span className="text-slate-600">Tax (6%):</span>
                <span className="font-medium w-32 text-right">{formatCurrency(calculateSubtotal() * 0.06)}</span>
              </div>
              <div className="flex justify-end items-center gap-4 text-base font-bold border-t pt-2">
                <span>Total:</span>
                <span className="w-32 text-right text-emerald-600">{formatCurrency(calculateSubtotal() * 1.06)}</span>
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => { setShowCreateModal(false); resetForm(); }}>
              Cancel
            </Button>
            <Button onClick={handleCreateInvoice} disabled={creating}>
              {creating ? 'Creating...' : 'Create Invoice'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default InvoicesListPage;

