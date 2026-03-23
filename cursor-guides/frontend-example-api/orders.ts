import apiClient from './client';

/**
 * PATTERN: API Module
 * 
 * Key conventions:
 * - One file per domain (orders.ts, departments.ts, etc.)
 * - Export typed interfaces for requests/responses
 * - Use apiClient for all HTTP calls (handles auth + department injection)
 * - Handle envelope response format: { success, message, data, errors }
 * - Throw errors with meaningful messages
 */

// ==================== INTERFACES ====================

export interface Order {
  id: string;
  companyId?: string;
  departmentId?: string;
  partnerId?: string;

  status: string;
  priority?: string;

  serviceId?: string;
  ticketId?: string;
  orderTypeId?: string;

  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;

  addressLine1?: string;
  addressLine2?: string;
  unitNo?: string;
  city?: string;
  state?: string;
  postcode?: string;

  buildingId?: string;
  buildingName?: string;

  appointmentDate?: string | null;
  appointmentWindowFrom?: string | null;
  appointmentWindowTo?: string | null;

  rescheduleCount?: number;
  serialsValidated?: boolean;
  photosUploaded?: boolean;

  createdAt?: string;
  updatedAt?: string;
}

export interface OrdersListFilters {
  status?: string;
  partnerId?: string;
  assignedSiId?: string;
  buildingId?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  departmentId?: string; // IMPORTANT: Department filter for data isolation
}

export interface CreateOrderDto {
  orderTypeId: string;
  partnerId?: string;
  buildingId: string;
  serviceId?: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postcode: string;
  appointmentDate: string;
  appointmentWindowFrom: string;
  appointmentWindowTo: string;
  departmentId?: string; // Optional - will be resolved from OrderType settings
}

/**
 * PATTERN: Backend response envelope
 * All API responses follow this format
 */
interface ApiEnvelope<T> {
  success: boolean;
  message?: string;
  data: T;
  errors: string[];
}

// ==================== API FUNCTIONS ====================

/**
 * PATTERN: List endpoint
 * 
 * - Accept filters object
 * - departmentId is typically injected by apiClient automatically
 * - Return typed array
 */
export const getOrders = async (filters: OrdersListFilters = {}): Promise<Order[]> => {
  // apiClient automatically adds departmentId from DepartmentContext
  const response = await apiClient.get<ApiEnvelope<Order[]>>('/orders', {
    params: filters,
  });

  // Handle envelope response
  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to load orders.';
    throw new Error(message);
  }

  return response.data.data;
};

/**
 * PATTERN: Get single entity by ID
 * 
 * - Accept optional params for additional context
 * - Return single typed object
 */
export const getOrder = async (
  id: string, 
  params: { departmentId?: string } = {}
): Promise<Order> => {
  const response = await apiClient.get<ApiEnvelope<Order>>(`/orders/${id}`, { params });

  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to load order.';
    throw new Error(message);
  }

  return response.data.data;
};

/**
 * PATTERN: Create entity
 * 
 * - Accept typed DTO
 * - Return created entity
 */
export const createOrder = async (payload: CreateOrderDto): Promise<Order> => {
  const response = await apiClient.post<ApiEnvelope<Order>>('/orders', payload);

  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to create order.';
    throw new Error(message);
  }

  return response.data.data;
};

/**
 * PATTERN: Update entity
 * 
 * - Accept ID and partial payload
 * - Return updated entity
 */
export const updateOrder = async (
  id: string, 
  payload: Partial<Order>
): Promise<Order> => {
  const response = await apiClient.put<ApiEnvelope<Order>>(`/orders/${id}`, payload);

  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to update order.';
    throw new Error(message);
  }

  return response.data.data;
};

/**
 * PATTERN: Delete entity
 * 
 * - Accept ID
 * - Return void (no content)
 */
export const deleteOrder = async (id: string): Promise<void> => {
  const response = await apiClient.delete<ApiEnvelope<null>>(`/orders/${id}`);

  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to delete order.';
    throw new Error(message);
  }
};

/**
 * PATTERN: Custom action endpoint
 * 
 * - Use POST for actions that modify state
 * - Use descriptive endpoint names
 */
export const changeOrderStatus = async (
  id: string, 
  status: string, 
  reason?: string
): Promise<Order> => {
  const response = await apiClient.post<ApiEnvelope<Order>>(`/orders/${id}/status`, {
    status,
    reason,
  });

  if (!response.data.success) {
    const message = response.data.message || 
      response.data.errors?.join(', ') || 
      'Failed to change order status.';
    throw new Error(message);
  }

  return response.data.data;
};
