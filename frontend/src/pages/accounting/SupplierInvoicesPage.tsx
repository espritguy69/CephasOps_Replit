import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, FileText, Check, Eye } from 'lucide-react';
import { 
  getSupplierInvoices, createSupplierInvoice, updateSupplierInvoice, 
  deleteSupplierInvoice, approveSupplierInvoice,
  SupplierInvoiceStatus, SupplierInvoiceStatusLabels 
} from '../../api/accounting';
import { getTransactionalPnlTypes } from '../../api/pnlTypes';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import type { SupplierInvoice, SupplierInvoiceStatus as SupplierInvoiceStatusEnum, CreateSupplierInvoiceRequest } from '../../types/accounting';
import type { PnlType } from '../../types/pnlTypes';

interface LineItem {
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  pnlTypeId: string | null;
  totalWithTax?: number;
}

interface SupplierInvoiceFormData {
  invoiceNumber: string;
  supplierName: string;
  supplierTaxNumber: string;
  supplierAddress: string;
  invoiceDate: string;
  dueDate: string;
  description: string;
  notes: string;
  defaultPnlTypeId: string | null;
  lineItems: LineItem[];
}

interface ExtendedSupplierInvoice extends SupplierInvoice {
  supplierName?: string;
  outstandingAmount?: number;
  amountPaid?: number;
  lineItems?: LineItem[];
}

interface TableColumn<T> {
  key: string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const SupplierInvoicesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [invoices, setInvoices] = useState<ExtendedSupplierInvoice[]>([]);
  const [pnlTypes, setPnlTypes] = useState<PnlType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [showDetailModal, setShowDetailModal] = useState<boolean>(false);
  const [selectedInvoice, setSelectedInvoice] = useState<ExtendedSupplierInvoice | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [formData, setFormData] = useState<SupplierInvoiceFormData>({
    invoiceNumber: '',
    supplierName: '',
    supplierTaxNumber: '',
    supplierAddress: '',
    invoiceDate: new Date().toISOString().split('T')[0],
    dueDate: '',
    description: '',
    notes: '',
    defaultPnlTypeId: null,
    lineItems: [{ description: '', quantity: 1, unitPrice: 0, taxRate: 0, pnlTypeId: null }]
  });

  useEffect(() => {
    loadData();
  }, [statusFilter]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params = statusFilter ? { status: statusFilter as SupplierInvoiceStatusEnum } : {};
      const [invoicesData, pnlData] = await Promise.all([
        getSupplierInvoices(params),
        getTransactionalPnlTypes('Expense')
      ]);
      setInvoices(Array.isArray(invoicesData) ? invoicesData : []);
      setPnlTypes(Array.isArray(pnlData) ? pnlData : []);
    } catch (err: any) {
      console.error('Error loading data:', err);
      showError('Failed to load invoices');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.invoiceNumber.trim()) {
        showError('Invoice number is required');
        return;
      }
      if (!formData.supplierName.trim()) {
        showError('Supplier name is required');
        return;
      }
      if (formData.lineItems.length === 0 || !formData.lineItems[0].description) {
        showError('At least one line item is required');
        return;
      }
      const invoiceData: CreateSupplierInvoiceRequest = {
        invoiceNumber: formData.invoiceNumber.trim(),
        invoiceDate: formData.invoiceDate,
        dueDate: formData.dueDate,
        amount: calculateTotal(),
        taxAmount: formData.lineItems.reduce((sum, item) => {
          const lineTotal = (item.quantity || 0) * (item.unitPrice || 0);
          return sum + (lineTotal * ((item.taxRate || 0) / 100));
        }, 0)
      };
      await createSupplierInvoice(invoiceData);
      showSuccess('Invoice created successfully');
      setShowCreateModal(false);
      resetForm();
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to create invoice');
    }
  };

  const handleApprove = async (id: string): Promise<void> => {
    try {
      await approveSupplierInvoice(id);
      showSuccess('Invoice approved successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to approve invoice');
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this invoice?')) return;
    
    try {
      await deleteSupplierInvoice(id);
      showSuccess('Invoice deleted successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete invoice');
    }
  };

  const resetForm = (): void => {
    setFormData({
      invoiceNumber: '',
      supplierName: '',
      supplierTaxNumber: '',
      supplierAddress: '',
      invoiceDate: new Date().toISOString().split('T')[0],
      dueDate: '',
      description: '',
      notes: '',
      defaultPnlTypeId: null,
      lineItems: [{ description: '', quantity: 1, unitPrice: 0, taxRate: 0, pnlTypeId: null }]
    });
  };

  const addLineItem = (): void => {
    setFormData({
      ...formData,
      lineItems: [...formData.lineItems, { description: '', quantity: 1, unitPrice: 0, taxRate: 0, pnlTypeId: null }]
    });
  };

  const updateLineItem = (index: number, field: keyof LineItem, value: string | number | null): void => {
    const newLineItems = [...formData.lineItems];
    (newLineItems[index] as any)[field] = value;
    setFormData({ ...formData, lineItems: newLineItems });
  };

  const removeLineItem = (index: number): void => {
    if (formData.lineItems.length === 1) return;
    const newLineItems = formData.lineItems.filter((_, i) => i !== index);
    setFormData({ ...formData, lineItems: newLineItems });
  };

  const calculateTotal = (): number => {
    return formData.lineItems.reduce((sum, item) => {
      const lineTotal = (item.quantity || 0) * (item.unitPrice || 0);
      const tax = lineTotal * ((item.taxRate || 0) / 100);
      return sum + lineTotal + tax;
    }, 0);
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY');
  };

  const pnlTypeOptions = [
    { value: '', label: '(None)' },
    ...pnlTypes.map(pt => ({ value: pt.id, label: `${pt.name} (${pt.code || ''})` }))
  ];

  const statusOptions = [
    { value: '', label: 'All Statuses' },
    ...Object.entries(SupplierInvoiceStatusLabels).map(([value, label]) => ({ value, label }))
  ];

  const getStatusBadge = (status: SupplierInvoiceStatusEnum | string): React.ReactNode => {
    const colors: Record<string, string> = {
      Draft: 'bg-slate-600',
      PendingApproval: 'bg-yellow-600',
      Approved: 'bg-blue-600',
      PartiallyPaid: 'bg-purple-600',
      Paid: 'bg-green-600',
      Overdue: 'bg-red-600',
      Cancelled: 'bg-slate-500',
      Disputed: 'bg-orange-600'
    };
    return (
      <span className={`px-2 py-1 text-xs rounded ${colors[status] || 'bg-slate-600'} text-white`}>
        {SupplierInvoiceStatusLabels[status as SupplierInvoiceStatusEnum] || status}
      </span>
    );
  };

  if (loading) {
    return (
      <PageShell title="Supplier Invoices" breadcrumbs={[{ label: 'Accounting', path: '/accounting' }, { label: 'Supplier Invoices' }]}>
        <LoadingSpinner />
      </PageShell>
    );
  }

  const columns: TableColumn<ExtendedSupplierInvoice>[] = [
    { key: 'invoiceNumber', label: 'Invoice #' },
    { key: 'supplierName', label: 'Supplier' },
    { key: 'invoiceDate', label: 'Date', render: (v: unknown) => formatDate(v as string) },
    { key: 'dueDate', label: 'Due Date', render: (v: unknown) => formatDate(v as string) },
    { key: 'totalAmount', label: 'Total', render: (v: unknown) => formatCurrency(v as number) },
    { key: 'outstandingAmount', label: 'Outstanding', render: (v: unknown) => formatCurrency(v as number) },
    { key: 'status', label: 'Status', render: (v: unknown) => getStatusBadge(v as string) },
    {
      key: 'actions',
      label: 'Actions',
      sortable: false,
      render: (_: unknown, row: ExtendedSupplierInvoice) => (
        <div className="flex items-center gap-2">
          <button onClick={() => { setSelectedInvoice(row); setShowDetailModal(true); }} className="text-blue-500 hover:text-blue-400" title="View">
            <Eye className="h-4 w-4" />
          </button>
          {(row.status === 'Draft' || row.status === 'PendingApproval') && (
            <button onClick={() => handleApprove(row.id)} className="text-green-500 hover:text-green-400" title="Approve">
              <Check className="h-4 w-4" />
            </button>
          )}
          {(row.amountPaid === 0 || row.amountPaid === undefined) && (
            <button onClick={() => handleDelete(row.id)} className="text-red-500 hover:text-red-400" title="Delete">
              <Trash2 className="h-4 w-4" />
            </button>
          )}
        </div>
      )
    }
  ];

  return (
    <PageShell title="Supplier Invoices" breadcrumbs={[{ label: 'Accounting', path: '/accounting' }, { label: 'Supplier Invoices' }]}>
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <div className="flex items-center gap-3">
          <FileText className="h-6 w-6 text-brand-500" />
          <h1 className="text-2xl font-bold text-white">Supplier Invoices</h1>
        </div>
        <Button onClick={() => { resetForm(); setShowCreateModal(true); }}>
          <Plus className="h-4 w-4 mr-2" />
          Add Invoice
        </Button>
      </div>

      <div className="mb-4">
        <SelectInput
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          options={statusOptions}
          className="w-48"
        />
      </div>

      <Card>
        {invoices.length === 0 ? (
          <EmptyState message="No invoices found" />
        ) : (
          <DataTable data={invoices} columns={columns} />
        )}
      </Card>

      {/* Create Invoice Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => { setShowCreateModal(false); resetForm(); }}
        title="Add Supplier Invoice"
        size="lg"
      >
        <div className="space-y-4 max-h-[70vh] overflow-y-auto">
          <div className="grid grid-cols-2 gap-4">
            <TextInput 
              label="Invoice Number" 
              value={formData.invoiceNumber} 
              onChange={(e) => setFormData({ ...formData, invoiceNumber: e.target.value })} 
              required 
            />
            <TextInput 
              label="Supplier Name" 
              value={formData.supplierName} 
              onChange={(e) => setFormData({ ...formData, supplierName: e.target.value })} 
              required 
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <TextInput 
              label="Invoice Date" 
              type="date" 
              value={formData.invoiceDate} 
              onChange={(e) => setFormData({ ...formData, invoiceDate: e.target.value })} 
            />
            <TextInput 
              label="Due Date" 
              type="date" 
              value={formData.dueDate} 
              onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })} 
            />
          </div>
          <SelectInput 
            label="Default P&L Type" 
            value={formData.defaultPnlTypeId || ''} 
            onChange={(e) => setFormData({ ...formData, defaultPnlTypeId: e.target.value || null })} 
            options={pnlTypeOptions} 
          />
          <TextInput 
            label="Description" 
            value={formData.description} 
            onChange={(e) => setFormData({ ...formData, description: e.target.value })} 
            multiline 
            rows={2} 
          />

          <div className="border-t border-slate-700 pt-4">
            <div className="flex justify-between items-center mb-2">
              <h4 className="text-white font-medium">Line Items</h4>
              <Button size="sm" onClick={addLineItem}>+ Add Line</Button>
            </div>
            {formData.lineItems.map((item, idx) => (
              <div key={idx} className="grid grid-cols-12 gap-2 mb-2 items-end">
                <div className="col-span-4">
                  <TextInput 
                    label={idx === 0 ? "Description" : ""} 
                    value={item.description} 
                    onChange={(e) => updateLineItem(idx, 'description', e.target.value)} 
                    placeholder="Description" 
                  />
                </div>
                <div className="col-span-2">
                  <TextInput 
                    label={idx === 0 ? "Qty" : ""} 
                    type="number" 
                    value={item.quantity} 
                    onChange={(e) => updateLineItem(idx, 'quantity', parseFloat(e.target.value) || 0)} 
                  />
                </div>
                <div className="col-span-2">
                  <TextInput 
                    label={idx === 0 ? "Price" : ""} 
                    type="number" 
                    value={item.unitPrice} 
                    onChange={(e) => updateLineItem(idx, 'unitPrice', parseFloat(e.target.value) || 0)} 
                  />
                </div>
                <div className="col-span-2">
                  <TextInput 
                    label={idx === 0 ? "Tax %" : ""} 
                    type="number" 
                    value={item.taxRate} 
                    onChange={(e) => updateLineItem(idx, 'taxRate', parseFloat(e.target.value) || 0)} 
                  />
                </div>
                <div className="col-span-2">
                  <button 
                    onClick={() => removeLineItem(idx)} 
                    className="text-red-500 hover:text-red-400 p-2" 
                    disabled={formData.lineItems.length === 1}
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            ))}
            <div className="text-right text-lg font-bold text-white mt-4">
              Total: {formatCurrency(calculateTotal())}
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => { setShowCreateModal(false); resetForm(); }}>Cancel</Button>
            <Button onClick={handleCreate}>Create Invoice</Button>
          </div>
        </div>
      </Modal>

      {/* Detail Modal */}
      <Modal
        isOpen={showDetailModal}
        onClose={() => { setShowDetailModal(false); setSelectedInvoice(null); }}
        title={`Invoice: ${selectedInvoice?.invoiceNumber}`}
        size="lg"
      >
        {selectedInvoice && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div><span className="text-slate-400">Supplier:</span> <span className="text-white ml-2">{selectedInvoice.supplierName || 'N/A'}</span></div>
              <div><span className="text-slate-400">Status:</span> <span className="ml-2">{getStatusBadge(selectedInvoice.status)}</span></div>
              <div><span className="text-slate-400">Invoice Date:</span> <span className="text-white ml-2">{formatDate(selectedInvoice.invoiceDate)}</span></div>
              <div><span className="text-slate-400">Due Date:</span> <span className="text-white ml-2">{formatDate(selectedInvoice.dueDate)}</span></div>
              <div><span className="text-slate-400">Total:</span> <span className="text-white ml-2 font-bold">{formatCurrency(selectedInvoice.totalAmount)}</span></div>
              <div><span className="text-slate-400">Outstanding:</span> <span className="text-red-400 ml-2 font-bold">{formatCurrency(selectedInvoice.outstandingAmount)}</span></div>
            </div>
            {selectedInvoice.lineItems && selectedInvoice.lineItems.length > 0 && (
              <div className="border-t border-slate-700 pt-4">
                <h4 className="text-white font-medium mb-2">Line Items</h4>
                <table className="w-full text-sm">
                  <thead className="text-slate-400 border-b border-slate-700">
                    <tr>
                      <th className="text-left py-2">Description</th>
                      <th className="text-right">Qty</th>
                      <th className="text-right">Price</th>
                      <th className="text-right">Tax</th>
                      <th className="text-right">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedInvoice.lineItems.map((item, idx) => (
                      <tr key={idx} className="border-b border-slate-700/50 text-white">
                        <td className="py-2">{item.description}</td>
                        <td className="text-right">{item.quantity}</td>
                        <td className="text-right">{formatCurrency(item.unitPrice)}</td>
                        <td className="text-right">{item.taxRate}%</td>
                        <td className="text-right">{formatCurrency(item.totalWithTax || (item.quantity * item.unitPrice * (1 + item.taxRate / 100)))}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </Modal>
      </div>
    </PageShell>
  );
};

export default SupplierInvoicesPage;

