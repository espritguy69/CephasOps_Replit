import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
import { getInvoice, updateInvoice } from '../../api/billing';
import { getPartners } from '../../api/partners';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Invoice, UpdateInvoiceRequest, UpdateInvoiceLineItemRequest } from '../../types/billing';
import type { Partner } from '../../types/partners';

const INVOICE_STATUSES = [
  { value: 'Draft', label: 'Draft' },
  { value: 'Sent', label: 'Sent' },
  { value: 'Paid', label: 'Paid' },
  { value: 'Overdue', label: 'Overdue' },
  { value: 'Cancelled', label: 'Cancelled' },
];

interface LineItemForm {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  orderId: string | null;
}

const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount);

const InvoiceEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const [invoice, setInvoice] = useState<Invoice | null>(null);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [partnerId, setPartnerId] = useState('');
  const [invoiceDate, setInvoiceDate] = useState('');
  const [termsInDays, setTermsInDays] = useState(45);
  const [status, setStatus] = useState('');
  const [lineItems, setLineItems] = useState<LineItemForm[]>([]);

  useEffect(() => {
    if (id) loadInvoice();
    loadPartners();
  }, [id]);

  const loadPartners = async () => {
    try {
      const data = await getPartners();
      setPartners(Array.isArray(data) ? data : []);
    } catch {
      // ignore
    }
  };

  const loadInvoice = async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const data = await getInvoice(id);
      setInvoice(data);
      setPartnerId(data.partnerId ?? '');
      setInvoiceDate(data.invoiceDate?.split('T')[0] ?? '');
      setTermsInDays(data.termsInDays ?? 45);
      setStatus(data.status ?? 'Draft');
      const items = (data.lineItems ?? []).map((li) => {
          let desc = li.description;
          if (!desc && (li.customerName ?? li.serviceId ?? li.orderType ?? li.docketNo)) {
            desc = [
              `CUSTOMER NAME: ${li.customerName ?? ''}`,
              `SERVICE ID: ${li.serviceId ?? ''}`,
              `ORDER TYPE: ${li.orderType ?? ''}`,
              `DOCKET NO: ${li.docketNo ?? ''}`,
            ].join('\n');
          }
          return {
            id: li.id,
            description: desc ?? '',
            quantity: li.quantity,
            unitPrice: li.unitPrice,
            orderId: li.orderId ?? null,
          };
        });
      setLineItems(items.length > 0 ? items : [{ id: '', description: '', quantity: 1, unitPrice: 0, orderId: null }]);
    } catch (err: any) {
      setError(err.message || 'Failed to load invoice');
      showError(err.message || 'Failed to load invoice');
    } finally {
      setLoading(false);
    }
  };

  const isSent = invoice?.status === 'Sent';
  const contentLocked = isSent;

  const handleAddLineItem = () => {
    setLineItems((prev) => [
      ...prev,
      {
        id: '',
        description: '',
        quantity: 1,
        unitPrice: 0,
        orderId: null,
      },
    ]);
  };

  const handleRemoveLineItem = (index: number) => {
    if (lineItems.length > 1) {
      setLineItems((prev) => prev.filter((_, i) => i !== index));
    }
  };

  const handleLineItemChange = (index: number, field: keyof LineItemForm, value: string | number | null) => {
    setLineItems((prev) =>
      prev.map((item, i) => (i === index ? { ...item, [field]: value } : item))
    );
  };


  const handleSave = async () => {
    if (!id) return;
    if (lineItems.length === 0 || lineItems.every((i) => !i.description.trim())) {
      showError('Please add at least one line item with a description');
      return;
    }
    try {
      setSaving(true);
      const payload: UpdateInvoiceRequest = {
        status,
        lineItems: lineItems
          .filter((i) => i.description.trim())
          .map((i) => ({
            id: i.id || '00000000-0000-0000-0000-000000000000',
            description: i.description.trim(),
            quantity: Number(i.quantity) || 0,
            unitPrice: Number(i.unitPrice) || 0,
            orderId: i.orderId || undefined,
          })),
      };
      if (!contentLocked) {
        payload.partnerId = partnerId || undefined;
        payload.invoiceDate = invoiceDate || undefined;
        payload.termsInDays = termsInDays;
      }
      await updateInvoice(id, payload);
      showSuccess('Invoice updated successfully');
      navigate(`/billing/invoices/${id}`);
    } catch (err: any) {
      showError(err.message || 'Failed to update invoice');
    } finally {
      setSaving(false);
    }
  };

  const calculatedSubtotal = lineItems.reduce(
    (sum, i) => sum + (Number(i.quantity) || 0) * (Number(i.unitPrice) || 0),
    0
  );
  const taxRate = invoice?.taxRate != null ? Number(invoice.taxRate) / 100 : 0.06;
  const calculatedTax = calculatedSubtotal * taxRate;
  const calculatedTotal = calculatedSubtotal + calculatedTax;

  if (loading) {
    return (
      <PageShell
        title="Edit Invoice"
        breadcrumbs={[
          { label: 'Billing', path: '/billing' },
          { label: 'Invoices', path: '/billing/invoices' },
          { label: 'Edit' },
        ]}
      >
        <LoadingSpinner message="Loading invoice..." fullPage />
      </PageShell>
    );
  }

  if (error || !invoice) {
    return (
      <PageShell
        title="Edit Invoice"
        breadcrumbs={[
          { label: 'Billing', path: '/billing' },
          { label: 'Invoices', path: '/billing/invoices' },
          { label: 'Edit' },
        ]}
      >
        <EmptyState
          title="Invoice Not Found"
          message={error || 'The invoice you are looking for does not exist.'}
          action={
            <Button onClick={() => navigate('/billing/invoices')} className="gap-1">
              <ArrowLeft className="h-4 w-4" />
              Back to Invoices
            </Button>
          }
        />
      </PageShell>
    );
  }

  const partnerOptions = partners.map((p) => ({ value: p.id, label: p.name }));

  return (
    <PageShell
      title={`Edit Invoice ${invoice.invoiceNumber}`}
      breadcrumbs={[
        { label: 'Billing', path: '/billing' },
        { label: 'Invoices', path: '/billing/invoices' },
        { label: invoice.invoiceNumber, path: `/billing/invoices/${id}` },
        { label: 'Edit' },
      ]}
      actions={
        <>
          <Button variant="outline" size="sm" onClick={() => navigate(`/billing/invoices/${id}`)} className="gap-1">
            <ArrowLeft className="h-4 w-4" />
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={saving} className="gap-1">
            <Save className="h-4 w-4" />
            {saving ? 'Saving...' : 'Save Changes'}
          </Button>
        </>
      }
    >
      <div className="max-w-5xl space-y-6">
        {contentLocked && (
          <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800">
            Bill To, dates, and line items are locked because this invoice has been marked as Sent. Only status can be changed.
          </div>
        )}

        {/* Bill To & Metadata */}
        <Card className="p-6">
          <h3 className="text-sm font-semibold text-slate-700 mb-4">Bill To & Invoice Details</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <SelectInput
                label="Bill To (Partner)"
                value={partnerId}
                onChange={(e) => setPartnerId(e.target.value)}
                options={partnerOptions}
                disabled={contentLocked}
              />
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Invoice Date</label>
                <TextInput
                  type="date"
                  value={invoiceDate}
                  onChange={(e) => setInvoiceDate(e.target.value)}
                  disabled={contentLocked}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Terms (days)</label>
                <TextInput
                  type="number"
                  value={String(termsInDays)}
                  onChange={(e) => setTermsInDays(parseInt(e.target.value, 10) || 45)}
                  disabled={contentLocked}
                  min={1}
                />
                <p className="text-xs text-slate-500 mt-1">Due date = Invoice date + {termsInDays} days</p>
              </div>
            </div>
            <div className="space-y-4">
              <SelectInput
                label="Status"
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                options={INVOICE_STATUSES}
              />
            </div>
          </div>
        </Card>

        {/* Line Items */}
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-sm font-semibold text-slate-700">Line Items</h3>
            {!contentLocked && (
              <Button variant="outline" size="sm" onClick={handleAddLineItem}>
                <Plus className="h-4 w-4 mr-1" />
                Add Item
              </Button>
            )}
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-200 text-left">
                  <th className="py-2 pr-2 text-xs font-semibold text-slate-500 uppercase">Description</th>
                  <th className="py-2 px-2 text-xs font-semibold text-slate-500 uppercase w-20 text-right">Qty</th>
                  <th className="py-2 px-2 text-xs font-semibold text-slate-500 uppercase w-28 text-right">Unit Price</th>
                  <th className="py-2 pl-2 text-xs font-semibold text-slate-500 uppercase w-24 text-right">Total</th>
                  {!contentLocked && <th className="w-10" />}
                </tr>
              </thead>
              <tbody>
                {lineItems.map((item, index) => (
                  <tr key={index} className="border-b border-slate-100">
                    <td className="py-2 pr-2">
                      <textarea
                        value={item.description}
                        onChange={(e) => handleLineItemChange(index, 'description', e.target.value)}
                        disabled={contentLocked}
                        placeholder="Description or CUSTOMER NAME / SERVICE ID / ORDER TYPE / DOCKET NO"
                        rows={3}
                        className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 disabled:bg-slate-50 disabled:cursor-not-allowed"
                      />
                    </td>
                    <td className="py-2 px-2 text-right">
                      <input
                        type="number"
                        value={item.quantity}
                        onChange={(e) => handleLineItemChange(index, 'quantity', parseFloat(e.target.value) || 0)}
                        disabled={contentLocked}
                        min={0}
                        step={0.01}
                        className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md text-right focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
                      />
                    </td>
                    <td className="py-2 px-2 text-right">
                      <input
                        type="number"
                        value={item.unitPrice}
                        onChange={(e) => handleLineItemChange(index, 'unitPrice', parseFloat(e.target.value) || 0)}
                        disabled={contentLocked}
                        min={0}
                        step={0.01}
                        className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded-md text-right focus:ring-2 focus:ring-blue-500 disabled:bg-slate-50"
                      />
                    </td>
                    <td className="py-2 pl-2 text-right text-sm font-medium">
                      {formatCurrency((Number(item.quantity) || 0) * (Number(item.unitPrice) || 0))}
                    </td>
                    {!contentLocked && (
                      <td className="py-2 pl-2">
                        <button
                          type="button"
                          onClick={() => handleRemoveLineItem(index)}
                          disabled={lineItems.length <= 1}
                          className="p-1 text-red-500 hover:text-red-700 disabled:opacity-40 disabled:cursor-not-allowed"
                          aria-label="Remove line"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="mt-4 pt-4 border-t border-slate-200 flex justify-end">
            <div className="text-right space-y-1">
              <div className="text-sm text-slate-600">
                Subtotal: {formatCurrency(calculatedSubtotal)}
              </div>
              <div className="text-sm text-slate-600">
                Tax ({(taxRate * 100).toFixed(0)}%): {formatCurrency(calculatedTax)}
              </div>
              <div className="text-base font-bold text-slate-800">
                Total: {formatCurrency(calculatedTotal)}
              </div>
            </div>
          </div>
        </Card>
      </div>
    </PageShell>
  );
};

export default InvoiceEditPage;
