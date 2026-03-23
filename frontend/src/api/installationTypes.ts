import apiClient from './client';
import type { ReferenceDataItem, CreateReferenceDataRequest, UpdateReferenceDataRequest, ReferenceDataFilters } from '../types/referenceData';

/**
 * Get all installation types for the current company
 * @param params - Query parameters (departmentId, isActive)
 * @returns List of installation types
 */
export const getInstallationTypes = async (params: ReferenceDataFilters = {}): Promise<ReferenceDataItem[]> => {
  const response = await apiClient.get<ReferenceDataItem[]>('/installation-types', { params });
  return response;
};

/**
 * Get a single installation type by ID
 * @param id - Installation type ID
 * @returns Installation type
 */
export const getInstallationTypeById = async (id: string): Promise<ReferenceDataItem> => {
  const response = await apiClient.get<ReferenceDataItem>(`/installation-types/${id}`);
  return response;
};

/**
 * Create a new installation type
 * @param data - Installation type data
 * @returns Created installation type
 */
export const createInstallationType = async (data: CreateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.post<ReferenceDataItem>('/installation-types', data);
  return response;
};

/**
 * Update an existing installation type
 * @param id - Installation type ID
 * @param data - Updated installation type data
 * @returns Updated installation type
 */
export const updateInstallationType = async (id: string, data: UpdateReferenceDataRequest): Promise<ReferenceDataItem> => {
  const response = await apiClient.put<ReferenceDataItem>(`/installation-types/${id}`, data);
  return response;
};

/**
 * Delete an installation type
 * @param id - Installation type ID
 * @returns Promise that resolves when installation type is deleted
 */
export const deleteInstallationType = async (id: string): Promise<void> => {
  await apiClient.delete(`/installation-types/${id}`);
};

