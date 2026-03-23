import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  ArrowLeft, Download, Send, Edit, Trash2, Check, X, 
  FileText, Building2, Calendar, DollarSign, Clock, 
  CheckCircle, AlertTriangle, Printer, Mail, Globe,
  RefreshCw, FileSearch
} from 'lucide-react';
import { getInvoice, updateInvoice, deleteInvoice, generateInvoicePdf, getInvoicePreviewHtml, submitEInvoice, getEInvoiceStatus } from '../../api/billing';
import { LoadingSpinner, EmptyState, useToast, Button, Card, StatusBadge, Modal, TextInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Invoice, UpdateInvoiceRequest, EInvoiceStatus } from '../../types/billing';

const INVOICE_STATUSES = [
  { value: 'Draft', label: 'Draft', color: 'bg-amber-100 text-amber-800' },
  { value: 'Sent', label: 'Sent', color: 'bg-blue-100 text-blue-800' },
  { value: 'Paid', label: 'Paid', color: 'bg-emerald-100 text-emerald-800' },
  { value: 'Overdue', label: 'Overdue', color: 'bg-red-100 text-red-800' },
  { value: 'Cancelled', label: 'Cancelled', color: 'bg-slate-100 text-slate-800' }
];

/** Invoice from API - uses backend snapshot (lineItems, subTotal, totalAmount, dueDate) */
type InvoiceData = Invoice;

const InvoiceDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const previewIframeRef = useRef<HTMLIFrameElement>(null);

  const [invoice, setInvoice] = useState<InvoiceData | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  
  // Portal submission state
  const [submittingToPortal, setSubmittingToPortal] = useState<boolean>(false);
  const [eInvoiceStatus, setEInvoiceStatus] = useState<EInvoiceStatus | null>(null);
  const [loadingEInvoiceStatus, setLoadingEInvoiceStatus] = useState<boolean>(false);
  
  // Status change modal
  const [showStatusModal, setShowStatusModal] = useState<boolean>(false);
  const [newStatus, setNewStatus] = useState<string>('');
  const [updating, setUpdating] = useState<boolean>(false);
  // Print preview modal (uses server-rendered HTML from Document Templates)
  const [showPrintPreview, setShowPrintPreview] = useState<boolean>(false);
  const [previewHtml, setPreviewHtml] = useState<string>('');
  const [previewLoading, setPreviewLoading] = useState<boolean>(false);
  // Delete confirmation
  const [showDeleteModal, setShowDeleteModal] = useState<boolean>(false);
  const [deleting, setDeleting] = useState<boolean>(false);

  useEffect(() => {
    if (id) {
      loadInvoice();
      loadEInvoiceStatus();
    }
  }, [id]);

  const loadInvoice = async (): Promise<void> => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const data = await getInvoice(id);
      setInvoice(data as InvoiceData);
    } catch (err: any) {
      setError(err.message || 'Failed to load invoice');
      showError(err.message || 'Failed to load invoice');
    } finally {
      setLoading(false);
    }
  };

  const loadEInvoiceStatus = async (): Promise<void> => {
    if (!id) return;
    try {
      setLoadingEInvoiceStatus(true);
      const status = await getEInvoiceStatus(id);
      setEInvoiceStatus(status);
    } catch {
      // e-Invoice might not exist yet, that's okay - silently ignore
    } finally {
      setLoadingEInvoiceStatus(false);
    }
  };

  const formatCurrency = (amount: number | null | undefined): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-MY', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  };

  const getStatusVariant = (status?: string): 'success' | 'error' | 'info' | 'default' | 'warning' => {
    const statusLower = status?.toLowerCase() || 'draft';
    if (statusLower === 'paid') return 'success';
    if (statusLower === 'overdue') return 'error';
    if (statusLower === 'sent') return 'info';
    if (statusLower === 'cancelled') return 'default';
    return 'warning';
  };

  const handleDownloadPdf = async (): Promise<void> => {
    if (!id) return;
    try {
      const blob = await generateInvoicePdf(id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `invoice-${invoice?.invoiceNumber || id}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      showSuccess('PDF downloaded successfully');
    } catch (err: any) {
      showError('Failed to download PDF');
    }
  };

  const loadPreviewHtml = async (): Promise<string> => {
    if (!id) throw new Error('No invoice ID');
    const html = await getInvoicePreviewHtml(id);
    return html;
  };

  const handlePrint = async (): Promise<void> => {
    if (!id) return;
    try {
      const html = await loadPreviewHtml();
      const printWindow = document.createElement('iframe');
      printWindow.style.position = 'absolute';
      printWindow.style.width = '0';
      printWindow.style.height = '0';
      printWindow.style.border = 'none';
      document.body.appendChild(printWindow);
      const doc = printWindow.contentDocument || printWindow.contentWindow?.document;
      if (doc) {
        doc.open();
        doc.write(html);
        doc.close();
        printWindow.contentWindow?.focus();
        printWindow.contentWindow?.print();
      }
      setTimeout(() => document.body.removeChild(printWindow), 1000);
      showSuccess('Print completed');
    } catch (err: any) {
      showError(err?.message || 'Failed to print invoice');
    }
  };

  const handleOpenPrintPreview = async (): Promise<void> => {
    if (!id) return;
    setShowPrintPreview(true);
    setPreviewLoading(true);
    setPreviewHtml('');
    try {
      const html = await loadPreviewHtml();
      setPreviewHtml(html);
    } catch (err: any) {
      showError(err?.message || 'Failed to load preview');
      setShowPrintPreview(false);
    } finally {
      setPreviewLoading(false);
    }
  };

  const handlePrintFromPreview = (): void => {
    if (previewIframeRef.current?.contentWindow) {
      previewIframeRef.current.contentWindow.print();
      showSuccess('Print completed');
    }
  };

  const handleSubmitToPortal = async (): Promise<void> => {
    if (!id) return;
    try {
      setSubmittingToPortal(true);
      await submitEInvoice(id);
      showSuccess('Invoice submitted to e-Invoice portal successfully');
      await loadEInvoiceStatus();
      await loadInvoice();
    } catch (err: any) {
      showError(err.message || 'Failed to submit to portal');
    } finally {
      setSubmittingToPortal(false);
    }
  };

  const handleStatusChange = async (): Promise<void> => {
    if (!id || !newStatus) return;
    
    try {
      setUpdating(true);
      const updateData: UpdateInvoiceRequest = { status: newStatus };
      await updateInvoice(id, updateData);
      showSuccess(`Invoice status updated to ${newStatus}`);
      setShowStatusModal(false);
      await loadInvoice();
    } catch (err: any) {
      showError(err.message || 'Failed to update status');
    } finally {
      setUpdating(false);
    }
  };

  const handleMarkAsSent = async (): Promise<void> => {
    if (!id) return;
    try {
      setUpdating(true);
      const updateData: UpdateInvoiceRequest = { status: 'Sent' };
      await updateInvoice(id, updateData);
      showSuccess('Invoice marked as sent');
      await loadInvoice();
    } catch (err: any) {
      showError(err.message || 'Failed to mark as sent');
    } finally {
      setUpdating(false);
    }
  };

  const handleMarkAsPaid = async (): Promise<void> => {
    if (!id) return;
    try {
      setUpdating(true);
      const updateData: UpdateInvoiceRequest = { status: 'Paid' };
      await updateInvoice(id, updateData);
      showSuccess('Invoice marked as paid');
      await loadInvoice();
    } catch (err: any) {
      showError(err.message || 'Failed to mark as paid');
    } finally {
      setUpdating(false);
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!id) return;
    try {
      setDeleting(true);
      await deleteInvoice(id);
      showSuccess('Invoice deleted');
      setShowDeleteModal(false);
      navigate('/billing/invoices');
    } catch (err: any) {
      showError(err?.message || 'Failed to delete invoice');
    } finally {
      setDeleting(false);
    }
  };

  if (loading) {
    return (
      <PageShell title="Invoice" breadcrumbs={[{ label: 'Billing', path: '/billing' }, { label: 'Invoices', path: '/billing/invoices' }, { label: 'Details' }]}>
        <LoadingSpinner message="Loading invoice..." fullPage />
      </PageShell>
    );
  }

  if (error || !invoice) {
    return (
      <PageShell title="Invoice" breadcrumbs={[{ label: 'Billing', path: '/billing' }, { label: 'Invoices', path: '/billing/invoices' }, { label: 'Details' }]}>
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

  const canSubmitToPortal = invoice.status === 'Sent' && !eInvoiceStatus;
  const isOverdue = invoice.dueDate && new Date(invoice.dueDate) < new Date() && invoice.status !== 'Paid';

  const pageTitle = `Invoice ${invoice.invoiceNumber}`;
  const breadcrumbLabel = invoice.invoiceNumber || 'Detail';

  return (
    <PageShell
      title={pageTitle}
      breadcrumbs={[
        { label: 'Billing', path: '/billing' },
        { label: 'Invoices', path: '/billing/invoices' },
        { label: breadcrumbLabel }
      ]}
      actions={
        <>
          <Button variant="outline" size="sm" onClick={() => navigate('/billing/invoices')} className="gap-1">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>
          <StatusBadge status={invoice.status} variant={getStatusVariant(invoice.status)} />
          {isOverdue && invoice.status !== 'Paid' && (
            <span className="px-2 py-1 text-xs font-medium bg-red-100 text-red-800 rounded-full">
              OVERDUE
            </span>
          )}
        </>
      }
    >
      <div className="max-w-5xl mx-auto space-y-6">
      {/* MyInvois Status Card */}
      {eInvoiceStatus && (
        <Card className="mb-6 p-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-semibold mb-1">MyInvois Submission Status</h3>
              <div className="flex items-center gap-2">
                <StatusBadge 
                  status={eInvoiceStatus.status || 'Unknown'} 
                  variant={eInvoiceStatus.status === 'Approved' ? 'success' : eInvoiceStatus.status === 'Rejected' ? 'error' : 'info'}
                />
                {eInvoiceStatus.submissionId && (
                  <span className="text-sm text-muted-foreground">
                    ID: {eInvoiceStatus.submissionId}
                  </span>
                )}
              </div>
              {eInvoiceStatus.rejectionReason && (
                <p className="text-sm text-red-600 mt-2">
                  Rejection Reason: {eInvoiceStatus.rejectionReason}
                </p>
              )}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={loadEInvoiceStatus}
              disabled={loadingEInvoiceStatus}
            >
              <RefreshCw className={`h-4 w-4 mr-2 ${loadingEInvoiceStatus ? 'animate-spin' : ''}`} />
              Refresh Status
            </Button>
          </div>
        </Card>
      )}

      {/* Print Preview Modal - uses server-rendered HTML from Document Templates */}
      <Modal
        isOpen={showPrintPreview}
        onClose={() => setShowPrintPreview(false)}
        title={`Print Preview - Invoice ${invoice?.invoiceNumber ?? ''}`}
        size="lg"
      >
        <div className="space-y-4">
          <div className="max-h-[70vh] overflow-y-auto bg-white rounded-lg border border-slate-200">
            {previewLoading ? (
              <div className="flex items-center justify-center py-16">
                <LoadingSpinner message="Loading preview..." />
              </div>
            ) : previewHtml ? (
              <iframe
                ref={previewIframeRef}
                srcDoc={previewHtml}
                title="Invoice Preview"
                className="w-full min-h-[500px] border-0"
                sandbox="allow-same-origin"
              />
            ) : null}
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setShowPrintPreview(false)}>
              Close
            </Button>
            <Button onClick={handlePrintFromPreview} disabled={!previewHtml}>
              <Printer className="h-4 w-4 mr-2" />
              Print
            </Button>
          </div>
        </div>
      </Modal>

      {/* Action Buttons */}
      <div className="flex flex-wrap gap-2 mb-6">
        <Button variant="outline" onClick={handleDownloadPdf}>
          <Download className="h-4 w-4 mr-2" />
          Download PDF
        </Button>
        <Button variant="outline" onClick={handleOpenPrintPreview}>
          <FileSearch className="h-4 w-4 mr-2" />
          Print Preview
        </Button>
        <Button variant="outline" onClick={handlePrint}>
          <Printer className="h-4 w-4 mr-2" />
          Print
        </Button>
        
        {invoice.status === 'Draft' && (
          <Button onClick={handleMarkAsSent} disabled={updating}>
            <Mail className="h-4 w-4 mr-2" />
            Mark as Sent
          </Button>
        )}
        
        {invoice.status === 'Sent' && (
          <Button variant="success" onClick={handleMarkAsPaid} disabled={updating}>
            <Check className="h-4 w-4 mr-2" />
            Mark as Paid
          </Button>
        )}
        
        <Button 
          variant="outline" 
          onClick={() => { setNewStatus(invoice.status); setShowStatusModal(true); }}
        >
          <Edit className="h-4 w-4 mr-2" />
          Change Status
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Main Invoice Details */}
        <div className="col-span-2 space-y-6">
          {/* Invoice Info Card */}
          <Card className="p-6">
            <div className="grid grid-cols-2 gap-6">
              {/* Bill To - accounting style */}
              <div>
                <h3 className="text-xs font-semibold text-slate-500 uppercase mb-2">Bill To</h3>
                <div className="flex items-start gap-2">
                  <Building2 className="h-5 w-5 text-slate-400 mt-0.5" />
                  <div>
                    <p className="font-semibold text-slate-800">{invoice.partnerName || 'N/A'}</p>
                    {invoice.partnerAddress &&
                      invoice.partnerAddress.split('\n').map((line, i) => (
                        <p key={i} className="text-sm text-slate-600">
                          {line.trim()}
                        </p>
                      ))}
                    <p className="text-sm text-slate-600 mt-1">
                      Person in charge: {invoice.partnerContactName || 'Finance Department'}
                    </p>
                    {invoice.partnerContactPhone && (
                      <p className="text-sm text-slate-600">TEL: {invoice.partnerContactPhone}</p>
                    )}
                    <p className="text-sm text-slate-600">
                      Subject: {invoice.billToSubject || 'Non Prelaid Activation'}
                    </p>
                  </div>
                </div>
              </div>
              
              {/* Invoice Details */}
              <div className="space-y-3">
                <div className="flex items-center gap-2">
                  <Calendar className="h-4 w-4 text-slate-400" />
                  <span className="text-sm text-slate-600">Invoice Date:</span>
                  <span className="text-sm font-medium">{formatDate(invoice.invoiceDate)}</span>
                </div>
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-slate-400" />
                  <span className="text-sm text-slate-600">Due Date:</span>
                  <span className={`text-sm font-medium ${isOverdue ? 'text-red-600' : ''}`}>
                    {formatDate(invoice.dueDate ?? null)}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-sm text-slate-600">Terms:</span>
                  <span className="text-sm font-medium">Net {invoice.termsInDays ?? 45} days</span>
                </div>
              </div>
            </div>
          </Card>

          {/* Line Items */}
          <Card className="overflow-hidden">
            <div className="px-6 py-3 bg-slate-50 border-b border-slate-200">
              <h3 className="text-sm font-semibold text-slate-700">Line Items</h3>
            </div>
            <table className="w-full">
              <thead>
                <tr className="border-b border-slate-200 text-left">
                  <th className="px-6 py-3 text-xs font-semibold text-slate-500 uppercase w-12">No</th>
                  <th className="px-6 py-3 text-xs font-semibold text-slate-500 uppercase">Description</th>
                  <th className="px-6 py-3 text-xs font-semibold text-slate-500 uppercase text-right">Qty/Unit</th>
                  <th className="px-6 py-3 text-xs font-semibold text-slate-500 uppercase text-right">Unit Price</th>
                  <th className="px-6 py-3 text-xs font-semibold text-slate-500 uppercase text-right">Total</th>
                </tr>
              </thead>
              <tbody>
                {invoice.lineItems && invoice.lineItems.length > 0 ? (
                  invoice.lineItems.map((item, index) => {
                    const hasOrderData =
                      item.customerName ?? item.serviceId ?? item.orderType ?? item.docketNo;
                    const desc = hasOrderData
                      ? [
                          `CUSTOMER NAME: ${item.customerName ?? ''}`,
                          `SERVICE ID: ${item.serviceId ?? ''}`,
                          `ORDER TYPE: ${item.orderType ?? ''}`,
                          `DOCKET NO: ${item.docketNo ?? ''}`,
                        ].join('\n')
                      : item.description;
                    return (
                      <tr key={item.id || index} className="border-b border-slate-100">
                        <td className="px-6 py-4 text-sm">{index + 1}</td>
                        <td className="px-6 py-4 text-sm">
                          <pre className="whitespace-pre-wrap font-sans text-sm">{desc}</pre>
                        </td>
                        <td className="px-6 py-4 text-sm text-right">{item.quantity}</td>
                        <td className="px-6 py-4 text-sm text-right">{formatCurrency(item.unitPrice)}</td>
                        <td className="px-6 py-4 text-sm text-right font-medium">
                          {formatCurrency(item.total ?? item.quantity * item.unitPrice)}
                        </td>
                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td colSpan={5} className="px-6 py-8 text-center text-slate-500">
                      No line items
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
            
            {/* Totals - from backend snapshot, no frontend recomputation */}
            <div className="px-6 py-4 bg-slate-50 border-t border-slate-200">
              <div className="max-w-xs ml-auto space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Subtotal:</span>
                  <span className="font-medium">{formatCurrency(invoice.subTotal)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-slate-600">Tax (GST):</span>
                  <span className="font-medium">{formatCurrency(invoice.taxAmount)}</span>
                </div>
                <div className="flex justify-between text-base font-bold border-t pt-2">
                  <span>Total:</span>
                  <span className="text-emerald-600">{formatCurrency(invoice.totalAmount)}</span>
                </div>
              </div>
            </div>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Amount Summary */}
          <Card className="p-6 bg-gradient-to-br from-blue-50 to-indigo-50 border-blue-200">
            <div className="text-center">
              <div className="text-xs text-blue-600 font-semibold uppercase mb-1">Total Amount</div>
              <div className="text-3xl font-bold text-blue-800">{formatCurrency(invoice.totalAmount)}</div>
              <StatusBadge status={invoice.status} variant={getStatusVariant(invoice.status)} className="mt-3" />
            </div>
          </Card>

          {/* Portal Submission Card */}
          <Card className="p-6">
            <h3 className="text-sm font-semibold text-slate-700 mb-4 flex items-center gap-2">
              <Globe className="h-4 w-4 text-purple-600" />
              e-Invoice Portal
            </h3>
            
            {loadingEInvoiceStatus ? (
              <div className="text-center py-4">
                <RefreshCw className="h-6 w-6 text-slate-400 animate-spin mx-auto" />
                <p className="text-xs text-slate-500 mt-2">Loading status...</p>
              </div>
            ) : eInvoiceStatus ? (
              <div className="space-y-3">
                <div className="flex items-center gap-2 text-emerald-600">
                  <CheckCircle className="h-5 w-5" />
                  <span className="text-sm font-medium">Submitted to Portal</span>
                </div>
                <div className="text-xs space-y-1 text-slate-600">
                  <p><strong>Status:</strong> {eInvoiceStatus.status || 'Pending'}</p>
                  <p><strong>Submitted:</strong> {formatDate(eInvoiceStatus.submissionDate)}</p>
                  {eInvoiceStatus.errorMessage && (
                    <p className="text-red-600"><strong>Error:</strong> {eInvoiceStatus.errorMessage}</p>
                  )}
                </div>
                <Button variant="outline" size="sm" className="w-full" onClick={loadEInvoiceStatus}>
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Refresh Status
                </Button>
              </div>
            ) : (
              <div className="space-y-3">
                <p className="text-sm text-slate-600">
                  Submit this invoice to the TIME e-Invoice/MyInvois portal for official processing.
                </p>
                {invoice.status === 'Draft' && (
                  <div className="flex items-start gap-2 p-2 bg-amber-50 rounded-lg text-xs text-amber-700">
                    <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
                    <span>Mark invoice as "Sent" before submitting to portal.</span>
                  </div>
                )}
                <Button 
                  className="w-full" 
                  onClick={handleSubmitToPortal}
                  disabled={!canSubmitToPortal || submittingToPortal}
                >
                  {submittingToPortal ? (
                    <>
                      <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                      Submitting...
                    </>
                  ) : (
                    <>
                      <Send className="h-4 w-4 mr-2" />
                      Submit to Portal
                    </>
                  )}
                </Button>
              </div>
            )}
          </Card>

          {/* Quick Actions */}
          <Card className="p-6">
            <h3 className="text-sm font-semibold text-slate-700 mb-4">Quick Actions</h3>
            <div className="space-y-2">
              <Button variant="outline" size="sm" className="w-full justify-start" onClick={handleDownloadPdf}>
                <Download className="h-4 w-4 mr-2" />
                Download PDF
              </Button>
              <Button variant="outline" size="sm" className="w-full justify-start" onClick={handlePrint}>
                <Printer className="h-4 w-4 mr-2" />
                Print Invoice
              </Button>
              <Button 
                variant="outline" 
                size="sm" 
                className="w-full justify-start"
                onClick={() => navigate(`/billing/invoices/${id}/edit`)}
              >
                <Edit className="h-4 w-4 mr-2" />
                Edit Invoice
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="w-full justify-start text-red-600 hover:text-red-700 hover:bg-red-50"
                onClick={() => setShowDeleteModal(true)}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                Delete Invoice
              </Button>
            </div>
          </Card>

          {/* Timeline / Activity */}
          <Card className="p-6">
            <h3 className="text-sm font-semibold text-slate-700 mb-4">Activity</h3>
            <div className="space-y-3">
              <div className="flex items-start gap-3">
                <div className="w-2 h-2 rounded-full bg-blue-500 mt-2"></div>
                <div>
                  <p className="text-sm text-slate-800">Invoice created</p>
                  <p className="text-xs text-slate-500">{formatDate(invoice.createdAt)}</p>
                </div>
              </div>
              {invoice.status !== 'Draft' && (
                <div className="flex items-start gap-3">
                  <div className="w-2 h-2 rounded-full bg-indigo-500 mt-2"></div>
                  <div>
                    <p className="text-sm text-slate-800">Invoice sent</p>
                    <p className="text-xs text-slate-500">{formatDate((invoice as any).sentAt || invoice.updatedAt)}</p>
                  </div>
                </div>
              )}
              {invoice.status === 'Paid' && (
                <div className="flex items-start gap-3">
                  <div className="w-2 h-2 rounded-full bg-emerald-500 mt-2"></div>
                  <div>
                    <p className="text-sm text-slate-800">Payment received</p>
                    <p className="text-xs text-slate-500">{formatDate((invoice as any).paidAt || invoice.updatedAt)}</p>
                  </div>
                </div>
              )}
              {eInvoiceStatus && (
                <div className="flex items-start gap-3">
                  <div className="w-2 h-2 rounded-full bg-purple-500 mt-2"></div>
                  <div>
                    <p className="text-sm text-slate-800">Submitted to e-Invoice</p>
                    <p className="text-xs text-slate-500">{formatDate(eInvoiceStatus.submissionDate)}</p>
                  </div>
                </div>
              )}
            </div>
          </Card>
        </div>
      </div>

      {/* Delete confirmation modal */}
      <Modal
        isOpen={showDeleteModal}
        onClose={() => !deleting && setShowDeleteModal(false)}
        title="Delete Invoice"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Are you sure you want to delete invoice <strong>{invoice?.invoiceNumber}</strong>? This action cannot be undone.
          </p>
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => setShowDeleteModal(false)} disabled={deleting}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={deleting}>
              {deleting ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Status Change Modal */}
      <Modal
        isOpen={showStatusModal}
        onClose={() => setShowStatusModal(false)}
        title="Change Invoice Status"
        size="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Current status: <strong>{invoice.status}</strong>
          </p>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">New Status</label>
            <div className="space-y-2">
              {INVOICE_STATUSES.map(status => (
                <label 
                  key={status.value} 
                  className={`flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                    newStatus === status.value 
                      ? 'border-blue-500 bg-blue-50' 
                      : 'border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  <input
                    type="radio"
                    name="status"
                    value={status.value}
                    checked={newStatus === status.value}
                    onChange={(e) => setNewStatus(e.target.value)}
                    className="h-4 w-4 text-blue-600"
                  />
                  <span className={`px-2 py-1 text-xs font-medium rounded-full ${status.color}`}>
                    {status.label}
                  </span>
                </label>
              ))}
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => setShowStatusModal(false)}>
              Cancel
            </Button>
            <Button onClick={handleStatusChange} disabled={updating || newStatus === invoice.status}>
              {updating ? 'Updating...' : 'Update Status'}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default InvoiceDetailPage;

