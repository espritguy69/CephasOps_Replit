import apiClient from './client';
import type {
  SupplierInvoice,
  SupplierInvoiceSummary,
  Payment,
  PaymentSummary,
  AccountingDashboard,
  CreateSupplierInvoiceRequest,
  UpdateSupplierInvoiceRequest,
  CreatePaymentRequest,
  UpdatePaymentRequest,
  VoidPaymentRequest,
  SupplierInvoiceFilters,
  PaymentFilters,
  SupplierInvoiceStatus,
  PaymentType,
  PaymentMethod,
  SupplierInvoiceStatusLabels,
  PaymentTypeLabels,
  PaymentMethodLabels
} from '../types/accounting';

/**
 * Supplier Invoices API
 */

// Export enums and labels (re-export from types)
export {
  SupplierInvoiceStatus,
  PaymentType,
  PaymentMethod,
  SupplierInvoiceStatusLabels,
  PaymentTypeLabels,
  PaymentMethodLabels
} from '../types/accounting';

/**
 * Get all supplier invoices
 * @param params - Optional filters
 * @returns Array of supplier invoices
 */
export const getSupplierInvoices = async (params: SupplierInvoiceFilters = {}): Promise<SupplierInvoice[]> => {
  const response = await apiClient.get<SupplierInvoice[]>('/supplier-invoices', { params });
  return response;
};

/**
 * Get supplier invoice summary
 * @returns Supplier invoice summary
 */
export const getSupplierInvoiceSummary = async (): Promise<SupplierInvoiceSummary> => {
  const response = await apiClient.get<SupplierInvoiceSummary>('/supplier-invoices/summary');
  return response;
};

/**
 * Get overdue invoices
 * @returns Array of overdue supplier invoices
 */
export const getOverdueInvoices = async (): Promise<SupplierInvoice[]> => {
  const response = await apiClient.get<SupplierInvoice[]>('/supplier-invoices/overdue');
  return response;
};

/**
 * Get single supplier invoice by ID
 * @param id - Supplier invoice ID
 * @returns Supplier invoice details
 */
export const getSupplierInvoice = async (id: string): Promise<SupplierInvoice> => {
  const response = await apiClient.get<SupplierInvoice>(`/supplier-invoices/${id}`);
  return response;
};

/**
 * Create new supplier invoice
 * @param data - Supplier invoice creation data
 * @returns Created supplier invoice
 */
export const createSupplierInvoice = async (data: CreateSupplierInvoiceRequest): Promise<SupplierInvoice> => {
  const response = await apiClient.post<SupplierInvoice>('/supplier-invoices', data);
  return response;
};

/**
 * Update supplier invoice
 * @param id - Supplier invoice ID
 * @param data - Supplier invoice update data
 * @returns Updated supplier invoice
 */
export const updateSupplierInvoice = async (id: string, data: UpdateSupplierInvoiceRequest): Promise<SupplierInvoice> => {
  const response = await apiClient.put<SupplierInvoice>(`/supplier-invoices/${id}`, data);
  return response;
};

/**
 * Delete supplier invoice
 * @param id - Supplier invoice ID
 * @returns Promise that resolves when supplier invoice is deleted
 */
export const deleteSupplierInvoice = async (id: string): Promise<void> => {
  await apiClient.delete(`/supplier-invoices/${id}`);
};

/**
 * Approve supplier invoice
 * @param id - Supplier invoice ID
 * @returns Approved supplier invoice
 */
export const approveSupplierInvoice = async (id: string): Promise<SupplierInvoice> => {
  const response = await apiClient.post<SupplierInvoice>(`/supplier-invoices/${id}/approve`);
  return response;
};

/**
 * Payments API
 */

/**
 * Get all payments
 * @param params - Optional filters
 * @returns Array of payments
 */
export const getPayments = async (params: PaymentFilters = {}): Promise<Payment[]> => {
  const response = await apiClient.get<Payment[]>('/payments', { params });
  return response;
};

/**
 * Get payment summary
 * @param params - Optional filters
 * @returns Payment summary
 */
export const getPaymentSummary = async (params: PaymentFilters = {}): Promise<PaymentSummary> => {
  const response = await apiClient.get<PaymentSummary>('/payments/summary', { params });
  return response;
};

/**
 * Get accounting dashboard
 * @returns Accounting dashboard data
 */
export const getAccountingDashboard = async (): Promise<AccountingDashboard> => {
  const response = await apiClient.get<AccountingDashboard>('/payments/dashboard');
  return response;
};

/**
 * Get single payment by ID
 * @param id - Payment ID
 * @returns Payment details
 */
export const getPayment = async (id: string): Promise<Payment> => {
  const response = await apiClient.get<Payment>(`/payments/${id}`);
  return response;
};

/**
 * Create new payment
 * @param data - Payment creation data
 * @returns Created payment
 */
export const createPayment = async (data: CreatePaymentRequest): Promise<Payment> => {
  const response = await apiClient.post<Payment>('/payments', data);
  return response;
};

/**
 * Update payment
 * @param id - Payment ID
 * @param data - Payment update data
 * @returns Updated payment
 */
export const updatePayment = async (id: string, data: UpdatePaymentRequest): Promise<Payment> => {
  const response = await apiClient.put<Payment>(`/payments/${id}`, data);
  return response;
};

/**
 * Delete payment
 * @param id - Payment ID
 * @returns Promise that resolves when payment is deleted
 */
export const deletePayment = async (id: string): Promise<void> => {
  await apiClient.delete(`/payments/${id}`);
};

/**
 * Void payment
 * @param id - Payment ID
 * @param data - Void payment data
 * @returns Voided payment
 */
export const voidPayment = async (id: string, data: VoidPaymentRequest): Promise<Payment> => {
  const response = await apiClient.post<Payment>(`/payments/${id}/void`, data);
  return response;
};

/**
 * Reconcile payment
 * @param id - Payment ID
 * @returns Reconciled payment
 */
export const reconcilePayment = async (id: string): Promise<Payment> => {
  const response = await apiClient.post<Payment>(`/payments/${id}/reconcile`);
  return response;
};

