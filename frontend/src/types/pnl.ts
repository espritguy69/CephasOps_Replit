/**
 * P&L Types - Shared type definitions for P&L module
 * Per PNL_MODULE.md specification
 */

export interface PnlSummary {
  periodId?: string;
  startDate?: string;
  endDate?: string;
  totalRevenue: number;
  totalCost: number;
  totalCosts?: number; // Alias for totalCost
  totalOverhead: number;
  grossProfit: number;
  netProfit: number;
  margin: number;
  orderCount: number;
}

/**
 * P&L Order Detail - legacy format for backward compatibility
 */
export interface PnlOrderDetail {
  id: string;
  orderId: string;
  orderNumber?: string;
  periodId?: string;
  revenue: number;
  materialCost: number;
  laborCost: number;
  overheadAllocation: number;
  totalCost: number;
  profit: number;
  margin: number;
}

/**
 * P&L Detail Per Order - enhanced per-order profitability tracking
 * Per PNL_MODULE.md section 6.2
 */
export interface PnlDetailPerOrder {
  id: string;
  companyId: string;
  orderId: string;
  partnerId: string;
  partnerName?: string;
  /** Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). */
  derivedPartnerCategoryLabel?: string;
  departmentId?: string;
  departmentName?: string;
  period: string;
  orderType: string;
  orderTypeName?: string;
  orderCategory?: string;
  installationMethod?: string;
  revenueAmount: number;
  materialCost: number;
  labourCost: number;
  overheadAllocated: number;
  grossProfit: number;
  profitForOrder: number;
  kpiResult?: string;
  rescheduleCount: number;
  serviceInstallerId?: string;
  serviceInstallerName?: string;
  revenueRateSource?: string;
  labourRateSource?: string;
  completedAt?: string;
  calculatedAt: string;
  dataQualityNotes?: string;
  // Computed fields
  marginPercentage?: number;
  // Related order info
  orderNumber?: string;
  customerName?: string;
  buildingName?: string;
  addressLine1?: string;
}

export interface PnlPeriod {
  id: string;
  periodName: string;
  startDate: string;
  endDate: string;
  year: number;
  month?: number;
  isClosed: boolean;
}

export interface Overhead {
  id: string;
  costCentreId: string;
  costCentreName?: string;
  period: string;
  amount: number;
  description?: string;
  category?: string;
}

export interface CreateOverheadRequest {
  costCentreId: string;
  period: string;
  amount: number;
  description?: string;
  category?: string;
}

export interface RebuildPnlRequest {
  period?: string;
  startDate?: string;
  endDate?: string;
}

export interface RebuildPnlResponse {
  jobId?: string;
  status: 'Started' | 'InProgress' | 'Completed' | 'Failed';
  message?: string;
}

export interface PnlSummaryFilters {
  periodId?: string;
  startDate?: string;
  endDate?: string;
}

export interface PnlOrderDetailFilters {
  orderId?: string;
  periodId?: string;
  startDate?: string;
  endDate?: string;
}

export interface PnlDetailPerOrderFilters {
  orderId?: string;
  partnerId?: string;
  departmentId?: string;
  period?: string;
  orderType?: string;
  serviceInstallerId?: string;
  kpiResult?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

// For type compatibility with existing PnlOrdersPage
export type PnlOrderFilters = PnlOrderDetailFilters;

export interface PnlPeriodFilters {
  year?: number;
}

export interface OverheadFilters {
  costCentreId?: string;
  period?: string;
}

