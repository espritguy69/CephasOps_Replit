import apiClient from './client';
import type {
  PnlSummary,
  PnlOrderDetail,
  PnlDetailPerOrder,
  PnlPeriod,
  Overhead,
  CreateOverheadRequest,
  RebuildPnlRequest,
  RebuildPnlResponse,
  PnlSummaryFilters,
  PnlOrderDetailFilters,
  PnlDetailPerOrderFilters,
  PnlPeriodFilters,
  OverheadFilters
} from '../types/pnl';

/**
 * P&L API
 * Handles profit & loss summaries, order details, periods, rebuild, and overheads
 */

/**
 * Get P&L summary
 * @param filters - Optional filters (periodId, startDate, endDate)
 * @returns P&L summary data
 */
export const getPnlSummary = async (filters: PnlSummaryFilters = {}): Promise<PnlSummary> => {
  const response = await apiClient.get<PnlSummary>('/pnl/summary', { params: filters });
  return response;
};

/**
 * Get P&L order details
 * @param filters - Optional filters (orderId, periodId, startDate, endDate)
 * @returns Array of P&L order details
 */
export const getPnlOrderDetails = async (filters: PnlOrderDetailFilters = {}): Promise<PnlOrderDetail[]> => {
  const response = await apiClient.get<PnlOrderDetail[]>('/pnl/orders', { params: filters });
  return response;
};

/**
 * Rebuild P&L data
 * @param rebuildData - Rebuild parameters (period)
 * @returns Rebuild job status
 */
export const rebuildPnl = async (rebuildData: RebuildPnlRequest): Promise<RebuildPnlResponse> => {
  const response = await apiClient.post<RebuildPnlResponse>('/pnl/rebuild', rebuildData);
  return response;
};

/**
 * Get P&L periods list
 * @param filters - Optional filters (year)
 * @returns Array of P&L periods
 */
export const getPnlPeriods = async (filters: PnlPeriodFilters = {}): Promise<PnlPeriod[]> => {
  const response = await apiClient.get<PnlPeriod[]>('/pnl/periods', { params: filters });
  return response;
};

/**
 * Get overhead entries
 * @param filters - Optional filters (costCentreId, period)
 * @returns Array of overhead entries
 */
export const getOverheads = async (filters: OverheadFilters = {}): Promise<Overhead[]> => {
  const response = await apiClient.get<Overhead[]>('/pnl/overheads', { params: filters });
  return response;
};

/**
 * Create overhead entry
 * @param overheadData - Overhead entry data
 * @returns Created overhead entry
 */
export const createOverhead = async (overheadData: CreateOverheadRequest): Promise<Overhead> => {
  const response = await apiClient.post<Overhead>('/pnl/overheads', overheadData);
  return response;
};

/**
 * Delete overhead entry
 * @param overheadId - Overhead entry ID
 * @returns Promise that resolves when overhead is deleted
 */
export const deleteOverhead = async (overheadId: string): Promise<void> => {
  await apiClient.delete(`/pnl/overheads/${overheadId}`);
};

/**
 * Get P&L detail per order (enhanced per-order profitability)
 * @param filters - Optional filters (orderId, partnerId, departmentId, period, etc.)
 * @returns Array of P&L detail per order records
 */
export const getPnlDetailPerOrder = async (filters: PnlDetailPerOrderFilters = {}): Promise<PnlDetailPerOrder[]> => {
  const response = await apiClient.get<PnlDetailPerOrder[]>('/pnl/orders/detail', { params: filters });
  return response;
};

/**
 * Get P&L detail for a specific order
 * @param orderId - Order ID
 * @returns P&L detail for the order
 */
export const getPnlDetailForOrder = async (orderId: string): Promise<PnlDetailPerOrder | null> => {
  const response = await apiClient.get<PnlDetailPerOrder>(`/pnl/orders/${orderId}/detail`);
  return response;
};

/**
 * Export P&L detail per order to CSV
 * @param filters - Optional filters
 */
export const exportPnlDetailPerOrder = async (filters: PnlDetailPerOrderFilters = {}): Promise<void> => {
  const response = await apiClient.get<Blob>('/pnl/orders/detail/export', {
    params: filters,
    responseType: 'blob'
  });
  const url = window.URL.createObjectURL(response);
  const link = document.createElement('a');
  link.href = url;
  link.download = `pnl-orders-${filters.period || 'all'}.csv`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

