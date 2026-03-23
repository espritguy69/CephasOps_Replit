/**
 * Billing Ratecards Types - Shared type definitions for Billing Ratecards module
 */

export interface BillingRatecard {
  id: string;
  partnerId: string;
  partnerName?: string;
  orderTypeId: string;
  orderTypeName?: string;
  departmentId?: string;
  departmentName?: string;
  rate: number;
  effectiveDate: string;
  expiryDate?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateBillingRatecardRequest {
  partnerId: string;
  orderTypeId: string;
  departmentId?: string;
  rate: number;
  effectiveDate: string;
  expiryDate?: string;
  isActive?: boolean;
}

export interface UpdateBillingRatecardRequest {
  partnerId?: string;
  orderTypeId?: string;
  departmentId?: string;
  rate?: number;
  effectiveDate?: string;
  expiryDate?: string;
  isActive?: boolean;
}

export interface BillingRatecardFilters {
  partnerId?: string;
  orderTypeId?: string;
  departmentId?: string;
  isActive?: boolean;
}

export interface ImportResult {
  success: boolean;
  imported: number;
  failed: number;
  errors?: string[];
}

