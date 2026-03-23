import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/**
 * Get all order categories for the current company
 * Previously known as InstallationTypes but renamed for clarity.
 * Order categories represent service/technology categories (FTTH, FTTO, FTTR, FTTC).
 * @param params - Query parameters (departmentId, isActive)
 * @returns List of order categories
 */
export const getOrderCategories = async (params: ReferenceDataFilters = {}): Promise<ReferenceDataItem[]> => {
  const response = await apiClient.get<ReferenceDataItem[]>('/order-categories', { params });
  return response;
};

/**
 * Get a single order category by ID
 * @param id - Order category ID
 * @returns Order category
 */
export const getOrderCategoryById = async (id: string): Promise<ReferenceDataItem> => {
  const response = await apiClient.get<ReferenceDataItem>(`/order-categories/${id}`);
  return response;
};

/**
 * Create a new order category
 * @param data - Order category data
 * @returns Created order category
 */
export const createOrderCategory = async (data: CreateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.post<ReferenceDataItem>('/order-categories', data);
  return response;
};

/**
 * Update an existing order category
 * @param id - Order category ID
 * @param data - Updated order category data
 * @returns Updated order category
 */
export const updateOrderCategory = async (id: string, data: UpdateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.put<ReferenceDataItem>(`/order-categories/${id}`, data);
  return response;
};

/**
 * Delete an order category
 * @param id - Order category ID
 * @returns Promise that resolves when order category is deleted
 */
export const deleteOrderCategory = async (id: string): Promise<void> => {
  await apiClient.delete(`/order-categories/${id}`);
};

