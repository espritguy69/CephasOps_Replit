/**
 * Payroll Types - Shared type definitions for Payroll module
 */

export interface PayrollPeriod {
  id: string;
  periodName: string;
  startDate: string;
  endDate: string;
  status: 'Draft' | 'Open' | 'Closed';
  year: number;
  month?: number;
}

export interface CreatePayrollPeriodRequest {
  periodName: string;
  startDate: string;
  endDate: string;
  year: number;
  month?: number;
}

export interface PayrollRun {
  id: string;
  periodId: string;
  periodName?: string;
  runDate: string;
  status: 'Draft' | 'Finalized' | 'Paid';
  totalAmount: number;
  itemCount: number;
  finalizedAt?: string;
  paidAt?: string;
}

export interface CreatePayrollRunRequest {
  periodId: string;
  runDate: string;
  siIds?: string[];
}

export interface JobEarningRecord {
  id: string;
  siId: string;
  siName?: string;
  orderId: string;
  orderUniqueId?: string;
  orderTypeId: string;
  orderTypeCode: string;
  orderTypeName: string;
  /**
   * @deprecated This field is no longer populated by the backend. Use orderTypeId, orderTypeCode, and orderTypeName instead.
   * Kept in type definition for backward compatibility with existing API responses.
   */
  jobType?: string;
  period: string;
  amount: number;
  rate: number;
  quantity?: number;
  description?: string;
  kpiResult?: string;
  baseRate?: number;
  kpiAdjustment?: number;
  finalPay?: number;
  status?: string;
  confirmedAt?: string;
  paidAt?: string;
}

export interface SiRatePlan {
  id: string;
  siId: string;
  siName?: string;
  departmentId: string;
  departmentName?: string;
  rateType: 'Fixed' | 'PerOrder' | 'PerHour' | 'PerUnit';
  rate: number;
  effectiveDate: string;
  expiryDate?: string;
  isActive: boolean;
}

export interface CreateSiRatePlanRequest {
  siId: string;
  departmentId: string;
  rateType: 'Fixed' | 'PerOrder' | 'PerHour' | 'PerUnit';
  rate: number;
  effectiveDate: string;
  expiryDate?: string;
}

export interface UpdateSiRatePlanRequest {
  rateType?: 'Fixed' | 'PerOrder' | 'PerHour' | 'PerUnit';
  rate?: number;
  effectiveDate?: string;
  expiryDate?: string;
  isActive?: boolean;
}

export interface PayrollPeriodFilters {
  year?: number;
  status?: string;
}

export interface PayrollRunFilters {
  periodId?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface JobEarningFilters {
  siId?: string;
  period?: string;
  orderId?: string;
}

export interface SiRatePlanFilters {
  siId?: string;
  isActive?: boolean;
}

export interface ImportResult {
  success: boolean;
  imported: number;
  failed: number;
  errors?: string[];
}

