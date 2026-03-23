import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/**
 * Get all building types for the current company
 * @param params - Query parameters (departmentId, isActive)
 * @returns List of building types
 */
export const getBuildingTypes = async (params: ReferenceDataFilters = {}): Promise<ReferenceDataItem[]> => {
  const response = await apiClient.get<ReferenceDataItem[]>('/building-types', { params });
  return response;
};

/**
 * Get a single building type by ID
 * @param id - Building type ID
 * @returns Building type
 */
export const getBuildingTypeById = async (id: string): Promise<ReferenceDataItem> => {
  const response = await apiClient.get<ReferenceDataItem>(`/building-types/${id}`);
  return response;
};

/**
 * Create a new building type
 * @param data - Building type data
 * @returns Created building type
 */
export const createBuildingType = async (data: CreateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.post<ReferenceDataItem>('/building-types', data);
  return response;
};

/**
 * Update an existing building type
 * @param id - Building type ID
 * @param data - Updated building type data
 * @returns Updated building type
 */
export const updateBuildingType = async (id: string, data: UpdateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.put<ReferenceDataItem>(`/building-types/${id}`, data);
  return response;
};

/**
 * Delete a building type
 * @param id - Building type ID
 * @returns Promise that resolves when building type is deleted
 */
export const deleteBuildingType = async (id: string): Promise<void> => {
  await apiClient.delete(`/building-types/${id}`);
};

