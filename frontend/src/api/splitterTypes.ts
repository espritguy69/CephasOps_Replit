import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/**
 * Get all splitter types for the current company
 * @param params - Query parameters (departmentId, isActive)
 * @returns List of splitter types
 */
export const getSplitterTypes = async (params: ReferenceDataFilters = {}): Promise<ReferenceDataItem[]> => {
  const response = await apiClient.get<ReferenceDataItem[]>('/splitter-types', { params });
  return response;
};

/**
 * Get a single splitter type by ID
 * @param id - Splitter type ID
 * @returns Splitter type
 */
export const getSplitterTypeById = async (id: string): Promise<ReferenceDataItem> => {
  const response = await apiClient.get<ReferenceDataItem>(`/splitter-types/${id}`);
  return response;
};

/**
 * Create a new splitter type
 * @param data - Splitter type data
 * @returns Created splitter type
 */
export const createSplitterType = async (data: CreateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.post<ReferenceDataItem>('/splitter-types', data);
  return response;
};

/**
 * Update an existing splitter type
 * @param id - Splitter type ID
 * @param data - Updated splitter type data
 * @returns Updated splitter type
 */
export const updateSplitterType = async (id: string, data: UpdateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.put<ReferenceDataItem>(`/splitter-types/${id}`, data);
  return response;
};

/**
 * Delete a splitter type
 * @param id - Splitter type ID
 * @returns Promise that resolves when splitter type is deleted
 */
export const deleteSplitterType = async (id: string): Promise<void> => {
  await apiClient.delete(`/splitter-types/${id}`);
};

