import apiClient from './client';
import { getApiBaseUrl } from './config';
import type { 
  Order, 
  OrderFilters, 
  OrderStatusLog, 
  OrderReschedule, 
  OrderBlocker, 
  OrderDocket 
} from '../types/orders';
import type { GponRateResolutionResult } from '../types/rates';

/**
 * Orders API
 * Handles order management, status updates, reschedules, blockers, dockets, and materials
 */

/**
 * Get orders list with filters
 * @param filters - Optional filters (status, partnerId, assignedSiId, buildingId, fromDate, toDate)
 * @returns Array of orders
 */
export const getOrders = async (filters: OrderFilters = {}): Promise<Order[]> => {
  const response = await apiClient.get('/orders', { params: filters });
  // Backend returns array directly, so return it as-is
  return Array.isArray(response) ? response : (response?.data || []);
};

/** Paged orders request params (keyword + filters + pagination) */
export interface GetOrdersPagedParams {
  keyword?: string;
  page?: number;
  pageSize?: number;
  status?: string;
  partnerId?: string;
  assignedSiId?: string;
  buildingId?: string;
  fromDate?: string;
  toDate?: string;
  departmentId?: string;
}

/** Paged orders response */
export interface OrdersPagedResult {
  items: Order[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Get orders with keyword search, filters, and pagination (for Orders List and Reports Hub).
 * @param params - keyword, page, pageSize, status, fromDate, toDate, etc.
 * @returns Paged result with items, totalCount, page, pageSize
 */
export const getOrdersPaged = async (params: GetOrdersPagedParams = {}): Promise<OrdersPagedResult> => {
  const { page = 1, pageSize = 50, ...rest } = params;
  const response = await apiClient.get('/orders/paged', {
    params: { ...rest, page, pageSize }
  });
  const data = response?.data ?? response;
  return {
    items: Array.isArray(data?.items) ? data.items : [],
    totalCount: typeof data?.totalCount === 'number' ? data.totalCount : 0,
    page: typeof data?.page === 'number' ? data.page : 1,
    pageSize: typeof data?.pageSize === 'number' ? data.pageSize : pageSize
  };
};

/**
 * Get order by ID
 * @param orderId - Order ID
 * @param params - Optional query parameters
 * @returns Order details
 */
export const getOrder = async (orderId: string, params: Record<string, any> = {}): Promise<Order> => {
  const response = await apiClient.get(`/orders/${orderId}`, { params });
  return response as Order;
};

/** Params for order payout breakdown (reference date for rate validity) */
export interface GetOrderPayoutBreakdownParams {
  departmentId?: string;
  referenceDate?: string;
}

/**
 * Get installer payout breakdown for an order (base rate, modifiers, trace). Read-only.
 * @param orderId - Order ID (or Job ID; in this system order = job)
 * @param params - Optional departmentId, referenceDate
 * @returns Full rate resolution result for display
 */
export const getOrderPayoutBreakdown = async (
  orderId: string,
  params: GetOrderPayoutBreakdownParams = {}
): Promise<GponRateResolutionResult> => {
  const response = await apiClient.get<GponRateResolutionResult>(
    `/orders/${orderId}/payout-breakdown`,
    { params }
  );
  return response as GponRateResolutionResult;
};

/** Response from payout-snapshot: prefers stored snapshot, fallback to live resolution. */
export interface OrderPayoutSnapshotResponse {
  source: 'Snapshot' | 'Live';
  result: GponRateResolutionResult;
}

/**
 * Get payout for order: snapshot if exists (immutable), else resolve live. Use for Installer Payout Breakdown page.
 * @param orderId - Order ID
 * @param params - Optional departmentId, referenceDate
 * @returns { source, result } so UI can show "Snapshot" or "Live calculation" badge
 */
export const getOrderPayoutSnapshot = async (
  orderId: string,
  params: GetOrderPayoutBreakdownParams = {}
): Promise<OrderPayoutSnapshotResponse> => {
  const response = await apiClient.get<OrderPayoutSnapshotResponse>(
    `/orders/${orderId}/payout-snapshot`,
    { params }
  );
  return response as OrderPayoutSnapshotResponse;
};

/**
 * Create a new order
 * @param orderData - Order creation data
 * @returns Created order
 */
export const createOrder = async (orderData: Partial<Order>): Promise<Order> => {
  const response = await apiClient.post('/orders', orderData);
  return response as Order;
};

/**
 * Update order (for non-status fields)
 * @param orderId - Order ID
 * @param orderData - Order update data (status changes should use changeOrderStatus)
 * @returns Updated order
 */
export const updateOrder = async (orderId: string, orderData: Partial<Order>): Promise<Order> => {
  // If status is being changed, use the workflow engine endpoint instead
  if (orderData.status) {
    return changeOrderStatus(orderId, orderData.status, orderData.reason, orderData.metadata);
  }
  
  const response = await apiClient.put(`/orders/${orderId}`, orderData);
  return response as Order;
};

/**
 * Change order status via workflow engine
 * @param orderId - Order ID
 * @param status - New status
 * @param reason - Optional reason for status change
 * @param metadata - Optional metadata (e.g., blockerCategory, rescheduleDate, etc.)
 * @returns Updated order
 */
export const changeOrderStatus = async (
  orderId: string,
  status: string,
  reason?: string,
  metadata?: Record<string, any>
): Promise<Order> => {
  const response = await apiClient.post(`/orders/${orderId}/status`, {
    status,
    reason,
    metadata
  });
  return response as Order;
};

/**
 * Get order status logs
 * @param orderId - Order ID
 * @returns Array of status log entries
 */
export const getOrderStatusLogs = async (orderId: string): Promise<OrderStatusLog[]> => {
  const response = await apiClient.get(`/orders/${orderId}/status-logs`);
  return Array.isArray(response) ? response : [];
};

/**
 * Get ONU password for an order (decrypted, requires authorization)
 * @param orderId - Order ID
 * @returns Decrypted ONU password
 */
export const getOnuPassword = async (orderId: string): Promise<string> => {
  const response = await apiClient.get<string>(`/orders/${orderId}/onu-password`);
  return response as string;
};

/**
 * Get order reschedules
 * @param orderId - Order ID
 * @returns Array of reschedule records
 */
export const getOrderReschedules = async (orderId: string): Promise<OrderReschedule[]> => {
  const response = await apiClient.get(`/orders/${orderId}/reschedules`);
  return Array.isArray(response) ? response : [];
};

/**
 * Request order reschedule
 * @param orderId - Order ID
 * @param rescheduleData - Reschedule request data
 * @returns Reschedule record
 */
export const requestReschedule = async (
  orderId: string, 
  rescheduleData: Partial<OrderReschedule>
): Promise<OrderReschedule> => {
  const response = await apiClient.post(`/orders/${orderId}/reschedules`, rescheduleData);
  return response as OrderReschedule;
};

/**
 * Get order blockers
 * @param orderId - Order ID
 * @returns Array of blocker records
 */
export const getOrderBlockers = async (orderId: string): Promise<OrderBlocker[]> => {
  const response = await apiClient.get(`/orders/${orderId}/blockers`);
  return Array.isArray(response) ? response : [];
};

/**
 * Create order blocker
 * @param orderId - Order ID
 * @param blockerData - Blocker data
 * @returns Created blocker
 */
export const createBlocker = async (
  orderId: string, 
  blockerData: Partial<OrderBlocker>
): Promise<OrderBlocker> => {
  const response = await apiClient.post(`/orders/${orderId}/blockers`, blockerData);
  return response as OrderBlocker;
};

/**
 * Resolve order blocker
 * @param orderId - Order ID
 * @param blockerId - Blocker ID
 * @param resolutionData - Blocker resolution data
 * @returns Updated blocker
 */
export const resolveBlocker = async (
  orderId: string, 
  blockerId: string, 
  resolutionData: Partial<OrderBlocker>
): Promise<OrderBlocker> => {
  const response = await apiClient.put(`/orders/${orderId}/blockers/${blockerId}/resolve`, resolutionData);
  return response as OrderBlocker;
};

/**
 * Add a note to an order
 * @param orderId - Order ID
 * @param note - Note text
 * @returns Updated order
 */
export const addOrderNote = async (orderId: string, note: string): Promise<Order> => {
  const response = await apiClient.post<Order>(`/orders/${orderId}/notes`, { note });
  return response;
};

/**
 * Get order dockets
 * @param orderId - Order ID
 * @returns Array of docket records
 */
export const getOrderDockets = async (orderId: string): Promise<OrderDocket[]> => {
  const response = await apiClient.get(`/orders/${orderId}/dockets`);
  return Array.isArray(response) ? response : [];
};

/**
 * Create order docket
 * @param orderId - Order ID
 * @param docketData - Docket data
 * @returns Created docket
 */
export const createDocket = async (
  orderId: string, 
  docketData: Partial<OrderDocket>
): Promise<OrderDocket> => {
  const response = await apiClient.post(`/orders/${orderId}/dockets`, docketData);
  return response as OrderDocket;
};

/**
 * Get order material usage
 * @param orderId - Order ID
 * @returns Array of material usage records
 */
export const getOrderMaterials = async (orderId: string): Promise<any[]> => {
  const response = await apiClient.get(`/orders/${orderId}/materials`);
  return Array.isArray(response) ? response : [];
};

/**
 * Record material usage for order
 * @param orderId - Order ID
 * @param materialData - Material usage data
 * @returns Material usage record
 */
export const recordMaterialUsage = async (
  orderId: string, 
  materialData: Record<string, any>
): Promise<any> => {
  const response = await apiClient.post(`/orders/${orderId}/materials`, materialData);
  return response;
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

/**
 * Check material collection requirements for an order
 * @param orderId - Order ID
 * @returns Material collection check result
 */
export const checkMaterialCollection = async (orderId: string): Promise<MaterialCollectionCheckResult> => {
  const response = await apiClient.get(`/orders/${orderId}/materials/collection-check`);
  return response as MaterialCollectionCheckResult;
};

/**
 * Get required materials for an order
 * @param orderId - Order ID
 * @returns List of required materials
 */
export const getRequiredMaterials = async (orderId: string): Promise<RequiredMaterial[]> => {
  const response = await apiClient.get(`/orders/${orderId}/materials/required`);
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Get material usage for an order (includes serialised items)
 * @param orderId - Order ID
 * @returns List of material usage records
 */
export const getMaterialUsage = async (orderId: string): Promise<any[]> => {
  const response = await apiClient.get(`/orders/${orderId}/materials/usage`);
  return Array.isArray(response) ? response : (response?.data || []);
};

/**
 * Generate and download Order PDF (Job Docket)
 * @param orderId - Order ID
 * @returns PDF file blob
 */
export const generateOrderPdf = async (orderId: string): Promise<Blob> => {
  const token = localStorage.getItem('authToken') || '';
  const apiBaseUrl = getApiBaseUrl();
  const response = await fetch(`${apiBaseUrl}/documents/orders/${orderId}/docket`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  
  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }
  
  // Check if response is JSON (document metadata) or PDF blob
  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    const doc = await response.json() as { fileUrl?: string };
    // If document has fileUrl, fetch the actual PDF
    if (doc.fileUrl) {
      const pdfResponse = await fetch(doc.fileUrl, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!pdfResponse.ok) throw new Error('Failed to download PDF');
      return pdfResponse.blob();
    }
    throw new Error('PDF file not available');
  }
  
  return response.blob();
};

