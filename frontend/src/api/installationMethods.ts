import apiClient from './client';
import type {
  InstallationMethod,
  CreateInstallationMethodRequest,
  UpdateInstallationMethodRequest,
  InstallationMethodFilters,
  InstallationCategory,
  InstallationCategoryLabels
} from '../types/installationMethods';

/**
 * Installation Methods API
 * Handles installation method management (Prelaid, Non-prelaid, SDU/RDF Pole, etc.)
 */

// Export enums and labels (re-export from types)
export { InstallationCategory, InstallationCategoryLabels } from '../types/installationMethods';

/**
 * Get installation methods list
 * @param params - Optional filters (category, isActive)
 * @returns Array of installation methods
 */
export const getInstallationMethods = async (params: InstallationMethodFilters = {}): Promise<InstallationMethod[]> => {
  const response = await apiClient.get<InstallationMethod[]>('/installation-methods', { params });
  return response;
};

/**
 * Get installation method by ID
 * @param id - Installation method ID
 * @returns Installation method details
 */
export const getInstallationMethod = async (id: string): Promise<InstallationMethod> => {
  const response = await apiClient.get<InstallationMethod>(`/installation-methods/${id}`);
  return response;
};

/**
 * Create a new installation method
 * @param data - Installation method data
 * @returns Created installation method
 */
export const createInstallationMethod = async (data: CreateInstallationMethodRequest): Promise<InstallationMethod> => {
  const response = await apiClient.post<InstallationMethod>('/installation-methods', data);
  return response;
};

/**
 * Update installation method
 * @param id - Installation method ID
 * @param data - Installation method update data
 * @returns Updated installation method
 */
export const updateInstallationMethod = async (id: string, data: UpdateInstallationMethodRequest): Promise<InstallationMethod> => {
  const response = await apiClient.put<InstallationMethod>(`/installation-methods/${id}`, data);
  return response;
};

/**
 * Delete installation method
 * @param id - Installation method ID
 * @returns Promise that resolves when installation method is deleted
 */
export const deleteInstallationMethod = async (id: string): Promise<void> => {
  await apiClient.delete(`/installation-methods/${id}`);
};

