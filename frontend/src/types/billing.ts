/**
 * Billing Types - Shared type definitions for Billing module
 */

export interface CompanyLetterhead {
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  registrationNo?: string;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  partnerId: string;
  partnerName?: string;
  partnerAddress?: string;
  partnerContactName?: string;
  partnerContactEmail?: string;
  partnerContactPhone?: string;
  billToSubject?: string;
  invoiceDate: string;
  termsInDays?: number;
  dueDate?: string | null;
  subTotal: number;
  taxAmount?: number;
  totalAmount: number;
  taxRate?: number;
  doRefNo?: string | null;
  purchaseOrderNo?: string | null;
  status: 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Cancelled';
  /** Backend returns lineItems (camelCase) - use this, not items */
  lineItems?: InvoiceLineItem[];
  createdAt?: string;
  updatedAt?: string;
  companyLetterhead?: CompanyLetterhead | null;
}

/** Invoice line item from API - matches backend InvoiceLineItemDto */
export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
  orderId?: string | null;
  customerName?: string;
  serviceId?: string;
  orderType?: string;
  docketNo?: string;
}

/** @deprecated Use InvoiceLineItem - kept for backward compatibility */
export type InvoiceItem = InvoiceLineItem;

export interface CreateInvoiceLineItemRequest {
  description: string;
  quantity: number;
  unitPrice: number;
  orderId?: string | null;
}

export interface CreateInvoiceRequest {
  partnerId: string;
  invoiceDate: string;
  termsInDays?: number;
  dueDate?: string;
  lineItems: CreateInvoiceLineItemRequest[];
  notes?: string;
}

export interface UpdateInvoiceLineItemRequest {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  orderId?: string | null;
}

export interface UpdateInvoiceRequest {
  partnerId?: string;
  invoiceDate?: string;
  termsInDays?: number;
  dueDate?: string;
  lineItems?: UpdateInvoiceLineItemRequest[];
  notes?: string;
  status?: string;
}

export interface Payment {
  id: string;
  invoiceId: string;
  amount: number;
  paymentDate: string;
  method: 'Cash' | 'BankTransfer' | 'Cheque' | 'CreditCard' | 'Other';
  reference?: string;
  notes?: string;
}

export interface CreatePaymentRequest {
  invoiceId: string;
  amount: number;
  paymentDate: string;
  method: 'Cash' | 'BankTransfer' | 'Cheque' | 'CreditCard' | 'Other';
  reference?: string;
  notes?: string;
}

export interface CreditNote {
  id: string;
  creditNoteNumber: string;
  invoiceId: string;
  amount: number;
  reason: string;
  issueDate: string;
  createdAt?: string;
}

export interface CreateCreditNoteRequest {
  invoiceId: string;
  amount: number;
  reason: string;
  issueDate: string;
}

export interface Ratecard {
  id: string;
  partnerId: string;
  partnerName?: string;
  serviceType: string;
  rate: number;
  effectiveDate: string;
  expiryDate?: string;
  isActive: boolean;
}

export interface EInvoiceStatus {
  invoiceId: string;
  status: 'Pending' | 'Submitted' | 'Accepted' | 'Rejected';
  submissionDate?: string;
  responseDate?: string;
  errorMessage?: string;
}

export interface InvoiceFilters {
  status?: string;
  partnerId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface PaymentFilters {
  invoiceId?: string;
  dateRange?: string;
  method?: string;
}

export interface CreditNoteFilters {
  invoiceId?: string;
  dateRange?: string;
}

export interface RatecardFilters {
  partnerId?: string;
  serviceType?: string;
}

