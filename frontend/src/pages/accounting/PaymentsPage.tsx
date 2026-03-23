import React, { useState, useEffect } from 'react';
import { Plus, Trash2, CreditCard, Check, X, Eye } from 'lucide-react';
import { 
  getPayments, createPayment, voidPayment, reconcilePayment,
  PaymentType, PaymentTypeLabels, PaymentMethod, PaymentMethodLabels 
} from '../../api/accounting';
import { getSupplierInvoices } from '../../api/accounting';
import { getTransactionalPnlTypes } from '../../api/pnlTypes';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, SelectInput, DataTable } from '../../components/ui';
import type { Payment, CreatePaymentRequest, PaymentFilters, PaymentType as PaymentTypeEnum, PaymentMethod as PaymentMethodEnum } from '../../types/accounting';
import type { PnlType } from '../../types/pnlTypes';
import type { SupplierInvoice } from '../../types/accounting';

interface PaymentFormData {
  paymentType: PaymentTypeEnum;
  paymentMethod: PaymentMethodEnum;
  paymentDate: string;
  amount: string;
  payerPayeeName: string;
  bankReference: string;
  supplierInvoiceId: string | null;
  pnlTypeId: string | null;
  description: string;
  notes: string;
}

interface TableColumn<T> {
  key: string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, row: T) => React.ReactNode;
}

interface ExtendedPayment extends Payment {
  payerPayeeName?: string;
  isVoided?: boolean;
}

const PaymentsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [payments, setPayments] = useState<ExtendedPayment[]>([]);
  const [pnlTypes, setPnlTypes] = useState<PnlType[]>([]);
  const [supplierInvoices, setSupplierInvoices] = useState<SupplierInvoice[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [showVoidModal, setShowVoidModal] = useState<boolean>(false);
  const [selectedPayment, setSelectedPayment] = useState<ExtendedPayment | null>(null);
  const [voidReason, setVoidReason] = useState<string>('');
  const [typeFilter, setTypeFilter] = useState<string>('');
  const [formData, setFormData] = useState<PaymentFormData>({
    paymentType: PaymentType.Expense,
    paymentMethod: PaymentMethod.BankTransfer,
    paymentDate: new Date().toISOString().split('T')[0],
    amount: '',
    payerPayeeName: '',
    bankReference: '',
    supplierInvoiceId: null,
    pnlTypeId: null,
    description: '',
    notes: ''
  });

  useEffect(() => {
    loadData();
  }, [typeFilter]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const params: PaymentFilters = typeFilter ? { paymentType: typeFilter as PaymentTypeEnum } : {};
      const [paymentsData, pnlData, invoicesData] = await Promise.all([
        getPayments(params),
        getTransactionalPnlTypes(),
        getSupplierInvoices({ status: 'Approved' as any })
      ]);
      setPayments(Array.isArray(paymentsData) ? paymentsData : []);
      setPnlTypes(Array.isArray(pnlData) ? pnlData : []);
      setSupplierInvoices(Array.isArray(invoicesData) ? invoicesData.filter((i: any) => (i as any).outstandingAmount > 0) : []);
    } catch (err: any) {
      console.error('Error loading data:', err);
      showError('Failed to load payments');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (): Promise<void> => {
    try {
      if (!formData.amount || parseFloat(formData.amount) <= 0) {
        showError('Amount is required');
        return;
      }
      if (!formData.payerPayeeName.trim()) {
        showError('Payer/Payee name is required');
        return;
      }
      const paymentData: CreatePaymentRequest = {
        paymentDate: formData.paymentDate,
        paymentType: formData.paymentType,
        paymentMethod: formData.paymentMethod,
        amount: parseFloat(formData.amount),
        reference: formData.bankReference || undefined,
        description: formData.description || undefined,
        supplierInvoiceId: formData.supplierInvoiceId || undefined
      };
      await createPayment(paymentData);
      showSuccess('Payment created successfully');
      setShowCreateModal(false);
      resetForm();
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to create payment');
    }
  };

  const handleVoid = async (): Promise<void> => {
    if (!selectedPayment || !voidReason.trim()) {
      showError('Void reason is required');
      return;
    }
    try {
      await voidPayment(selectedPayment.id, { reason: voidReason });
      showSuccess('Payment voided successfully');
      setShowVoidModal(false);
      setSelectedPayment(null);
      setVoidReason('');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to void payment');
    }
  };

  const handleReconcile = async (id: string): Promise<void> => {
    try {
      await reconcilePayment(id);
      showSuccess('Payment reconciled successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to reconcile payment');
    }
  };

  const resetForm = (): void => {
    setFormData({
      paymentType: PaymentType.Expense,
      paymentMethod: PaymentMethod.BankTransfer,
      paymentDate: new Date().toISOString().split('T')[0],
      amount: '',
      payerPayeeName: '',
      bankReference: '',
      supplierInvoiceId: null,
      pnlTypeId: null,
      description: '',
      notes: ''
    });
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY');
  };

  const typeOptions = [
    { value: '', label: 'All Types' },
    { value: 'Income', label: 'Income' },
    { value: 'Expense', label: 'Expense' }
  ];

  const paymentTypeOptions = Object.entries(PaymentTypeLabels).map(([value, label]) => ({ value, label }));
  const paymentMethodOptions = Object.entries(PaymentMethodLabels).map(([value, label]) => ({ value, label }));
  
  const pnlTypeOptions = [
    { value: '', label: '(None - Link to Invoice)' },
    ...pnlTypes.filter(pt => formData.paymentType === 'Income' ? pt.category === 'Income' : pt.category === 'Expense')
      .map(pt => ({ value: pt.id, label: `${pt.name} (${pt.code || ''})` }))
  ];

  const invoiceOptions = [
    { value: '', label: '(None)' },
    ...supplierInvoices.map(inv => ({ 
      value: inv.id, 
      label: `${inv.invoiceNumber} - ${inv.supplierName || 'N/A'} (${formatCurrency((inv as any).outstandingAmount || inv.totalAmount)})` 
    }))
  ];

  if (loading) {
    return (
      <PageShell title="Payments" breadcrumbs={[{ label: 'Accounting', path: '/accounting' }, { label: 'Payments' }]}>
        <LoadingSpinner />
      </PageShell>
    );
  }

  const columns: TableColumn<ExtendedPayment>[] = [
    { key: 'paymentNumber', label: 'Payment #' },
    { 
      key: 'paymentType', 
      label: 'Type',
      render: (v: unknown) => (
        <span className={`px-2 py-1 text-xs rounded ${v === 'Income' ? 'bg-green-600' : 'bg-red-600'} text-white`}>
          {v as string}
        </span>
      )
    },
    { key: 'payerPayeeName', label: 'Payer/Payee' },
    { key: 'paymentDate', label: 'Date', render: (v: unknown) => formatDate(v as string) },
    { key: 'paymentMethod', label: 'Method', render: (v: unknown) => PaymentMethodLabels[v as PaymentMethodEnum] || (v as string) },
    { 
      key: 'amount', 
      label: 'Amount',
      render: (v: unknown, row: ExtendedPayment) => (
        <span className={row.paymentType === 'Income' ? 'text-green-500' : 'text-red-500'}>
          {row.paymentType === 'Income' ? '+' : '-'}{formatCurrency(v as number)}
        </span>
      )
    },
    { 
      key: 'isReconciled', 
      label: 'Status',
      render: (v: unknown, row: ExtendedPayment) => {
        if (row.isVoided) return <span className="text-red-400">Voided</span>;
        return v ? <span className="text-green-500">Reconciled</span> : <span className="text-yellow-500">Pending</span>;
      }
    },
    {
      key: 'actions',
      label: 'Actions',
      sortable: false,
      render: (_: unknown, row: ExtendedPayment) => (
        <div className="flex items-center gap-2">
          {!row.isVoided && !row.isReconciled && (
            <button onClick={() => handleReconcile(row.id)} className="text-green-500 hover:text-green-400" title="Reconcile">
              <Check className="h-4 w-4" />
            </button>
          )}
          {!row.isVoided && (
            <button 
              onClick={() => { setSelectedPayment(row); setShowVoidModal(true); }} 
              className="text-red-500 hover:text-red-400" 
              title="Void"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
      )
    }
  ];

  return (
    <PageShell
      title="Payments"
      breadcrumbs={[{ label: 'Accounting', path: '/accounting' }, { label: 'Payments' }]}
      actions={
        <Button onClick={() => { resetForm(); setShowCreateModal(true); }}>
          <Plus className="h-4 w-4 mr-2" />
          Add Payment
        </Button>
      }
    >
      <div className="p-6">
      <div className="mb-4">
        <SelectInput
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
          options={typeOptions}
          className="w-48"
        />
      </div>

      <Card>
        {payments.length === 0 ? (
          <EmptyState message="No payments found" />
        ) : (
          <DataTable data={payments} columns={columns} />
        )}
      </Card>

      {/* Create Payment Modal */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => { setShowCreateModal(false); resetForm(); }}
        title="Add Payment"
        size="md"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <SelectInput 
              label="Payment Type" 
              value={formData.paymentType} 
              onChange={(e) => setFormData({ ...formData, paymentType: e.target.value as PaymentTypeEnum, pnlTypeId: null })} 
              options={paymentTypeOptions} 
            />
            <SelectInput 
              label="Payment Method" 
              value={formData.paymentMethod} 
              onChange={(e) => setFormData({ ...formData, paymentMethod: e.target.value as PaymentMethodEnum })} 
              options={paymentMethodOptions} 
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <TextInput 
              label="Payment Date" 
              type="date" 
              value={formData.paymentDate} 
              onChange={(e) => setFormData({ ...formData, paymentDate: e.target.value })} 
              required 
            />
            <TextInput 
              label="Amount (MYR)" 
              type="number" 
              value={formData.amount} 
              onChange={(e) => setFormData({ ...formData, amount: e.target.value })} 
              required 
            />
          </div>
          <TextInput 
            label="Payer/Payee Name" 
            value={formData.payerPayeeName} 
            onChange={(e) => setFormData({ ...formData, payerPayeeName: e.target.value })} 
            required 
          />
          <TextInput 
            label="Bank Reference" 
            value={formData.bankReference} 
            onChange={(e) => setFormData({ ...formData, bankReference: e.target.value })} 
          />
          
          {formData.paymentType === 'Expense' && (
            <SelectInput 
              label="Link to Supplier Invoice" 
              value={formData.supplierInvoiceId || ''} 
              onChange={(e) => setFormData({ ...formData, supplierInvoiceId: e.target.value || null })} 
              options={invoiceOptions} 
            />
          )}
          
          <SelectInput 
            label="P&L Type (if not linked to invoice)" 
            value={formData.pnlTypeId || ''} 
            onChange={(e) => setFormData({ ...formData, pnlTypeId: e.target.value || null })} 
            options={pnlTypeOptions} 
          />
          <TextInput 
            label="Description" 
            value={formData.description} 
            onChange={(e) => setFormData({ ...formData, description: e.target.value })} 
            multiline 
            rows={2} 
          />

          <div className="flex justify-end gap-2 pt-4">
            <Button variant="ghost" onClick={() => { setShowCreateModal(false); resetForm(); }}>Cancel</Button>
            <Button onClick={handleCreate}>Create Payment</Button>
          </div>
        </div>
      </Modal>

      {/* Void Modal */}
      <Modal
        isOpen={showVoidModal}
        onClose={() => { setShowVoidModal(false); setSelectedPayment(null); setVoidReason(''); }}
        title="Void Payment"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-slate-300">Are you sure you want to void payment <strong>{selectedPayment?.paymentNumber}</strong>?</p>
          <TextInput 
            label="Void Reason" 
            value={voidReason} 
            onChange={(e) => setVoidReason(e.target.value)} 
            multiline 
            rows={2} 
            required 
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => { setShowVoidModal(false); setSelectedPayment(null); setVoidReason(''); }}>Cancel</Button>
            <Button variant="danger" onClick={handleVoid}>Void Payment</Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default PaymentsPage;

