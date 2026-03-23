import apiClient from './client';
import type { Order, ChecklistData, ChecklistAnswer } from '../types/api';

/**
 * Orders API
 * Handles order-related API calls for SI app
 */

export interface OrderFilters {
  status?: string;
  fromDate?: string;
  toDate?: string;
  partnerId?: string;
  assignedSiId?: string;
  buildingId?: string;
}

/**
 * Get orders assigned to current SI
 */
export const getAssignedOrders = async (filters: OrderFilters = {}): Promise<Order[]> => {
  const response = await apiClient.get<Order[] | { data: Order[] }>('/orders', {
    params: {
      ...filters,
      // assignedSiId will be set by backend based on current user
    }
  });
  
  // Handle response envelope
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Order[] }).data;
  }
  return [];
};

/**
 * Get order by ID
 */
export const getOrder = async (orderId: string): Promise<Order> => {
  const response = await apiClient.get<Order | { data: Order }>(`/orders/${orderId}`);
  
  // Handle response envelope
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Order }).data;
  }
  return response as Order;
};

/**
 * Get checklist for an order status
 */
export const getOrderChecklist = async (orderId: string, status: string): Promise<ChecklistData> => {
  const response = await apiClient.get<ChecklistData | { data: ChecklistData }>(`/orders/${orderId}/checklist`, {
    params: { status }
  });
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ChecklistData }).data;
  }
  return response as ChecklistData;
};

/**
 * Submit checklist answers
 */
export const submitChecklistAnswers = async (
  orderId: string, 
  status: string, 
  answers: ChecklistAnswer[]
): Promise<any> => {
  const response = await apiClient.post(`/orders/${orderId}/checklist/answers`, {
    status,
    answers
  });
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: any }).data;
  }
  return response;
};

/**
 * Get all orders (admin view)
 * For admin users, returns all orders with optional filters
 */
export const getAllOrders = async (filters: OrderFilters = {}): Promise<Order[]> => {
  const response = await apiClient.get<Order[] | { data: Order[] }>('/orders', {
    params: filters
  });
  
  // Handle response envelope
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: Order[] }).data;
  }
  return [];
};

/**
 * Material collection types
 */
export interface MissingMaterial {
  materialId: string;
  materialCode: string;
  materialName: string;
  requiredQuantity: number;
  availableQuantity: number;
  missingQuantity: number;
  unitOfMeasure: string;
}

export interface MaterialCollectionCheckResult {
  orderId: string;
  serviceInstallerId?: string;
  requiresCollection: boolean;
  missingMaterials: MissingMaterial[];
  message: string;
}

export interface RequiredMaterial {
  materialId: string;
  materialCode: string;
  materialName: string;
  quantity: number;
  unitOfMeasure: string;
  isSerialised: boolean;
}

export interface RecordMaterialUsageRequest {
  materialId: string;
  serialNumber?: string;
  serialisedItemId?: string;
  quantity: number;
  notes?: string;
}

export interface MaterialUsageRecorded {
  id: string;
  orderId: string;
  materialId: string;
  materialName: string;
  serialisedItemId?: string;
  serialNumber?: string;
  quantity: number;
  recordedAt: string;
}

/**
 * Check material collection requirements for an order
 */
export const checkMaterialCollection = async (orderId: string): Promise<MaterialCollectionCheckResult> => {
  const response = await apiClient.get<MaterialCollectionCheckResult | { data: MaterialCollectionCheckResult }>(
    `/orders/${orderId}/materials/collection-check`
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MaterialCollectionCheckResult }).data;
  }
  return response as MaterialCollectionCheckResult;
};

/**
 * Get required materials for an order
 */
export const getRequiredMaterials = async (orderId: string): Promise<RequiredMaterial[]> => {
  const response = await apiClient.get<RequiredMaterial[] | { data: RequiredMaterial[] }>(
    `/orders/${orderId}/materials/required`
  );
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: RequiredMaterial[] }).data;
  }
  return [];
};

/**
 * Record material usage for an order
 */
export const recordMaterialUsage = async (
  orderId: string,
  data: RecordMaterialUsageRequest
): Promise<MaterialUsageRecorded> => {
  const response = await apiClient.post<MaterialUsageRecorded | { data: MaterialUsageRecorded }>(
    `/orders/${orderId}/materials/usage`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MaterialUsageRecorded }).data;
  }
  return response as MaterialUsageRecorded;
};

/**
 * Get material usage for an order
 */
export const getMaterialUsage = async (orderId: string): Promise<MaterialUsageRecorded[]> => {
  const response = await apiClient.get<MaterialUsageRecorded[] | { data: MaterialUsageRecorded[] }>(
    `/orders/${orderId}/materials/usage`
  );
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MaterialUsageRecorded[] }).data;
  }
  return [];
};

