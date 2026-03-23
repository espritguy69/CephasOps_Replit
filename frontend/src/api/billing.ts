import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  Invoice,
  CreateInvoiceRequest,
  UpdateInvoiceRequest,
  Payment,
  CreatePaymentRequest,
  CreditNote,
  CreateCreditNoteRequest,
  Ratecard,
  EInvoiceStatus,
  InvoiceFilters,
  PaymentFilters,
  CreditNoteFilters,
  RatecardFilters
} from '../types/billing';

/**
 * Billing API
 * Handles invoices, payments, credit notes, ratecards, and eInvoice submissions
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Get invoices list
 * @param filters - Optional filters (status, partnerId, fromDate, toDate)
 * @returns Array of invoices
 */
export const getInvoices = async (filters: InvoiceFilters = {}): Promise<Invoice[]> => {
  const response = await apiClient.get<Invoice[]>('/billing/invoices', { params: filters });
  return response;
};

/**
 * Get invoice by ID
 * @param invoiceId - Invoice ID
 * @returns Invoice details
 */
export const getInvoice = async (invoiceId: string): Promise<Invoice> => {
  const response = await apiClient.get<Invoice>(`/billing/invoices/${invoiceId}`);
  return response;
};

/**
 * Create invoice
 * @param invoiceData - Invoice creation data
 * @returns Created invoice
 */
export const createInvoice = async (invoiceData: CreateInvoiceRequest): Promise<Invoice> => {
  const response = await apiClient.post<Invoice>(`/billing/invoices`, invoiceData);
  return response;
};

/**
 * Update invoice
 * @param invoiceId - Invoice ID
 * @param invoiceData - Invoice update data
 * @returns Updated invoice
 */
export const updateInvoice = async (invoiceId: string, invoiceData: UpdateInvoiceRequest): Promise<Invoice> => {
  const response = await apiClient.put<Invoice>(`/billing/invoices/${invoiceId}`, invoiceData);
  return response;
};

/**
 * Delete invoice (Draft only in practice; backend may enforce)
 * @param invoiceId - Invoice ID
 */
export const deleteInvoice = async (invoiceId: string): Promise<void> => {
  await apiClient.delete(`/billing/invoices/${invoiceId}`);
};

/**
 * Generate invoice PDF (uses Document Templates)
 * @param invoiceId - Invoice ID
 * @returns PDF file blob
 */
export const generateInvoicePdf = async (invoiceId: string): Promise<Blob> => {
  const apiBaseUrl = getApiBaseUrl();
  const response = await fetch(`${apiBaseUrl}/billing/invoices/${invoiceId}/pdf`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${getAuthToken()}`
    }
  });
  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }
  return response.blob();
};

/**
 * Get invoice preview HTML (for print preview)
 * @param invoiceId - Invoice ID
 * @returns HTML string
 */
export const getInvoicePreviewHtml = async (invoiceId: string): Promise<string> => {
  const apiBaseUrl = getApiBaseUrl();
  const response = await fetch(`${apiBaseUrl}/billing/invoices/${invoiceId}/preview-html`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${getAuthToken()}`
    }
  });
  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }
  return response.text();
};

/**
 * Get payments list
 * @param filters - Optional filters (invoiceId, dateRange, method)
 * @returns Array of payments
 */
export const getPayments = async (filters: PaymentFilters = {}): Promise<Payment[]> => {
  const response = await apiClient.get<Payment[]>(`/billing/payments`, { params: filters });
  return response;
};

/**
 * Record payment
 * @param paymentData - Payment data
 * @returns Payment record
 */
export const recordPayment = async (paymentData: CreatePaymentRequest): Promise<Payment> => {
  const response = await apiClient.post<Payment>(`/billing/payments`, paymentData);
  return response;
};

/**
 * Get credit notes list
 * @param filters - Optional filters (invoiceId, dateRange)
 * @returns Array of credit notes
 */
export const getCreditNotes = async (filters: CreditNoteFilters = {}): Promise<CreditNote[]> => {
  const response = await apiClient.get<CreditNote[]>(`/billing/credit-notes`, { params: filters });
  return response;
};

/**
 * Create credit note
 * @param creditNoteData - Credit note data
 * @returns Created credit note
 */
export const createCreditNote = async (creditNoteData: CreateCreditNoteRequest): Promise<CreditNote> => {
  const response = await apiClient.post<CreditNote>(`/billing/credit-notes`, creditNoteData);
  return response;
};

/**
 * Get ratecards list
 * @param filters - Optional filters (partnerId, serviceType)
 * @returns Array of ratecards
 */
export const getRatecards = async (filters: RatecardFilters = {}): Promise<Ratecard[]> => {
  const response = await apiClient.get<Ratecard[]>(`/billing/ratecards`, { params: filters });
  return response;
};

/**
 * Get ratecard by ID
 * @param ratecardId - Ratecard ID
 * @returns Ratecard details
 */
export const getRatecard = async (ratecardId: string): Promise<Ratecard> => {
  const response = await apiClient.get<Ratecard>(`/billing/ratecards/${ratecardId}`);
  return response;
};

/**
 * Submit invoice to e-invoice portal (MyInvois)
 * @param invoiceId - Invoice ID
 * @returns Submission history
 */
export const submitEInvoice = async (invoiceId: string): Promise<any> => {
  const response = await apiClient.post(`/invoice-submissions/${invoiceId}/submit`);
  return response.data?.data || response.data || response;
};

/**
 * Get e-invoice status for an invoice
 * @param invoiceId - Invoice ID
 * @returns E-invoice status
 */
export const getEInvoiceStatus = async (invoiceId: string): Promise<EInvoiceStatus> => {
  const response = await apiClient.get(`/invoice-submissions/${invoiceId}/submission-history/active`);
  return response.data?.data || response.data || response;
};

