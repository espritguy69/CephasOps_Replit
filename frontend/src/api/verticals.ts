import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/**
 * Get all verticals for the current company
 * @param params - Query parameters (isActive)
 * @returns List of verticals
 */
export const getVerticals = async (params: Omit<ReferenceDataFilters, 'departmentId'> = {}): Promise<ReferenceDataItem[]> => {
  const response = await apiClient.get<ReferenceDataItem[]>('/verticals', { params });
  return response;
};

/**
 * Get a single vertical by ID
 * @param id - Vertical ID
 * @returns Vertical
 */
export const getVerticalById = async (id: string): Promise<ReferenceDataItem> => {
  const response = await apiClient.get<ReferenceDataItem>(`/verticals/${id}`);
  return response;
};

/**
 * Create a new vertical
 * @param data - Vertical data
 * @returns Created vertical
 */
export const createVertical = async (data: CreateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.post<ReferenceDataItem>('/verticals', data);
  return response;
};

/**
 * Update an existing vertical
 * @param id - Vertical ID
 * @param data - Updated vertical data
 * @returns Updated vertical
 */
export const updateVertical = async (id: string, data: UpdateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.put<ReferenceDataItem>(`/verticals/${id}`, data);
  return response;
};

/**
 * Delete a vertical
 * @param id - Vertical ID
 * @returns Promise that resolves when vertical is deleted
 */
export const deleteVertical = async (id: string): Promise<void> => {
  await apiClient.delete(`/verticals/${id}`);
};

