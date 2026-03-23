import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/** Order type (parent or subtype) from API */
export interface OrderTypeDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
  departmentId?: string;
  parentOrderTypeId?: string | null;
  displayOrder: number;
  childCount?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateOrderTypeRequest extends CreateReferenceDataRequest {
  parentOrderTypeId?: string | null;
  displayOrder?: number;
}

export interface UpdateOrderTypeRequest extends UpdateReferenceDataRequest {
  parentOrderTypeId?: string | null;
  displayOrder?: number;
}

export type OrderTypeFilters = ReferenceDataFilters & { parentsOnly?: boolean };

/**
 * Get order types (all, or parents only when parentsOnly=true)
 */
export const getOrderTypes = async (params: OrderTypeFilters = {}): Promise<OrderTypeDto[]> => {
  const { parentsOnly, ...rest } = params;
  const response = await apiClient.get<OrderTypeDto[]>('/order-types', {
    params: { ...rest, ...(parentsOnly !== undefined && { parentsOnly }) },
  });
  return response;
};

/**
 * Get parent order types only (for Create Order and settings left panel)
 */
export const getOrderTypeParents = async (params: Omit<OrderTypeFilters, 'parentsOnly'> = {}): Promise<OrderTypeDto[]> => {
  return getOrderTypes({ ...params, parentsOnly: true });
};

/**
 * Get subtypes of a parent order type.
 * Pass isActive: true for Create Order (active only); omit for settings (all).
 */
export const getOrderTypeSubtypes = async (
  parentId: string,
  params?: { isActive?: boolean }
): Promise<OrderTypeDto[]> => {
  const response = await apiClient.get<OrderTypeDto[]>(`/order-types/${parentId}/subtypes`, {
    params: params?.isActive !== undefined ? { isActive: params.isActive } : undefined,
  });
  return response;
};

/**
 * Get a single order type by ID
 */
export const getOrderType = async (id: string): Promise<OrderTypeDto> => {
  const response = await apiClient.get<OrderTypeDto>(`/order-types/${id}`);
  return response;
};

export const getOrderTypeById = getOrderType;

/**
 * Create order type (parent or subtype when parentOrderTypeId is set)
 */
export const createOrderType = async (data: CreateOrderTypeRequest): Promise<OrderTypeDto> => {
  const response = await apiClient.post<OrderTypeDto>('/order-types', data);
  return response;
};

/**
 * Update order type (supports parentOrderTypeId)
 */
export const updateOrderType = async (id: string, data: UpdateOrderTypeRequest): Promise<OrderTypeDto> => {
  const response = await apiClient.put<OrderTypeDto>(`/order-types/${id}`, data);
  return response;
};

export const deleteOrderType = async (id: string): Promise<void> => {
  await apiClient.delete(`/order-types/${id}`);
};

