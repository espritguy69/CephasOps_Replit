/**
 * Accounting Types - Shared type definitions for Accounting module
 */

export enum SupplierInvoiceStatus {
  Draft = 'Draft',
  PendingApproval = 'PendingApproval',
  Approved = 'Approved',
  PartiallyPaid = 'PartiallyPaid',
  Paid = 'Paid',
  Overdue = 'Overdue',
  Cancelled = 'Cancelled',
  Disputed = 'Disputed'
}

export enum PaymentType {
  Income = 'Income',
  Expense = 'Expense'
}

export enum PaymentMethod {
  Cash = 'Cash',
  BankTransfer = 'BankTransfer',
  Cheque = 'Cheque',
  CreditCard = 'CreditCard',
  DebitCard = 'DebitCard',
  OnlineBanking = 'OnlineBanking',
  EWallet = 'EWallet',
  DirectDebit = 'DirectDebit',
  Other = 'Other'
}

export interface SupplierInvoice {
  id: string;
  invoiceNumber: string;
  supplierId?: string;
  supplierName?: string;
  invoiceDate: string;
  dueDate: string;
  amount: number;
  taxAmount?: number;
  totalAmount: number;
  status: SupplierInvoiceStatus;
  approvedBy?: string;
  approvedAt?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface SupplierInvoiceSummary {
  total: number;
  pending: number;
  approved: number;
  paid: number;
  overdue: number;
  totalAmount: number;
  unpaidAmount: number;
}

export interface Payment {
  id: string;
  paymentNumber?: string;
  paymentDate: string;
  paymentType: PaymentType;
  paymentMethod: PaymentMethod;
  amount: number;
  reference?: string;
  description?: string;
  supplierInvoiceId?: string;
  isReconciled: boolean;
  reconciledAt?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface PaymentSummary {
  totalIncome: number;
  totalExpense: number;
  netAmount: number;
  byMethod: Record<PaymentMethod, number>;
}

export interface AccountingDashboard {
  supplierInvoices: SupplierInvoiceSummary;
  payments: PaymentSummary;
  overdueInvoices: SupplierInvoice[];
  recentPayments: Payment[];
}

export interface CreateSupplierInvoiceRequest {
  invoiceNumber: string;
  supplierId?: string;
  invoiceDate: string;
  dueDate: string;
  amount: number;
  taxAmount?: number;
}

export interface UpdateSupplierInvoiceRequest {
  invoiceNumber?: string;
  supplierId?: string;
  invoiceDate?: string;
  dueDate?: string;
  amount?: number;
  taxAmount?: number;
  status?: SupplierInvoiceStatus;
}

export interface CreatePaymentRequest {
  paymentDate: string;
  paymentType: PaymentType;
  paymentMethod: PaymentMethod;
  amount: number;
  reference?: string;
  description?: string;
  supplierInvoiceId?: string;
}

export interface UpdatePaymentRequest {
  paymentDate?: string;
  paymentType?: PaymentType;
  paymentMethod?: PaymentMethod;
  amount?: number;
  reference?: string;
  description?: string;
}

export interface VoidPaymentRequest {
  reason: string;
}

export interface SupplierInvoiceFilters {
  status?: SupplierInvoiceStatus;
  supplierId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface PaymentFilters {
  paymentType?: PaymentType;
  paymentMethod?: PaymentMethod;
  fromDate?: string;
  toDate?: string;
  isReconciled?: boolean;
}

export const SupplierInvoiceStatusLabels: Record<SupplierInvoiceStatus, string> = {
  [SupplierInvoiceStatus.Draft]: 'Draft',
  [SupplierInvoiceStatus.PendingApproval]: 'Pending Approval',
  [SupplierInvoiceStatus.Approved]: 'Approved',
  [SupplierInvoiceStatus.PartiallyPaid]: 'Partially Paid',
  [SupplierInvoiceStatus.Paid]: 'Paid',
  [SupplierInvoiceStatus.Overdue]: 'Overdue',
  [SupplierInvoiceStatus.Cancelled]: 'Cancelled',
  [SupplierInvoiceStatus.Disputed]: 'Disputed'
};

export const PaymentTypeLabels: Record<PaymentType, string> = {
  [PaymentType.Income]: 'Income (Receipt)',
  [PaymentType.Expense]: 'Expense (Payment)'
};

export const PaymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]: 'Cash',
  [PaymentMethod.BankTransfer]: 'Bank Transfer',
  [PaymentMethod.Cheque]: 'Cheque',
  [PaymentMethod.CreditCard]: 'Credit Card',
  [PaymentMethod.DebitCard]: 'Debit Card',
  [PaymentMethod.OnlineBanking]: 'Online Banking (FPX)',
  [PaymentMethod.EWallet]: 'E-Wallet',
  [PaymentMethod.DirectDebit]: 'Direct Debit',
  [PaymentMethod.Other]: 'Other'
};

