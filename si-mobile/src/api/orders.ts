/**
 * Orders API – list assigned jobs, order detail.
 * Backend: GET /api/orders, GET /api/orders/:id.
 */
import { apiClient } from './client';
import type { Order } from '../types/api';

export interface OrderFilters {
  fromDate?: string;
  toDate?: string;
  status?: string;
  assignedSiId?: string;
}

export async function getAssignedOrders(filters: OrderFilters = {}): Promise<Order[]> {
  const res = await apiClient.get<Order[] | { data: Order[] }>('/orders', {
    params: filters as Record<string, string | number | undefined>,
  });
  if (Array.isArray(res)) return res;
  if (res && typeof res === 'object' && 'data' in res) return (res as { data: Order[] }).data;
  return [];
}

export async function getOrder(orderId: string): Promise<Order> {
  const res = await apiClient.get<Order | { data: Order }>(`/orders/${orderId}`);
  if (res && typeof res === 'object' && 'data' in res) return (res as { data: Order }).data;
  return res as Order;
}
