import apiClient from './client';

/**
 * Order Statuses API
 * Handles order workflow status definitions
 */

export interface OrderStatus {
  code: string;
  name: string;
  description: string;
  order: number;
  color: string;
  icon: string;
  triggeredBy: string;
  workflowType: string;
  phase: string;
  kpiCategory?: string;
}

/**
 * Get all order workflow statuses
 */
export const getOrderStatuses = async (): Promise<OrderStatus[]> => {
  const response = await apiClient.get<OrderStatus[]>('/order-statuses');
  return response;
};

/**
 * Get statuses by workflow type
 */
export const getStatusesByWorkflow = async (workflowType: string): Promise<OrderStatus[]> => {
  const response = await apiClient.get<OrderStatus[]>(`/order-statuses/workflow/${workflowType}`);
  return response;
};

/**
 * Get all RMA workflow statuses
 */
export const getRmaStatuses = async (): Promise<OrderStatus[]> => {
  const response = await apiClient.get<OrderStatus[]>('/order-statuses/rma');
  return response;
};

/**
 * Get all KPI workflow statuses
 */
export const getKpiStatuses = async (): Promise<OrderStatus[]> => {
  const response = await apiClient.get<OrderStatus[]>('/order-statuses/kpi');
  return response;
};

