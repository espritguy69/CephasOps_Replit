import apiClient from './client';
import type {
  Splitter,
  CreateSplitterRequest,
  UpdateSplitterRequest,
  UpdateSplitterPortRequest,
  SplitterFilters
} from '../types/splitters';

/**
 * Splitters API
 * Handles splitter and port management
 */

/**
 * Get all splitters
 * @param filters - Optional filters (buildingId, isActive)
 * @returns Array of splitters
 */
export const getSplitters = async (filters: SplitterFilters = {}): Promise<Splitter[]> => {
  const response = await apiClient.get<Splitter[] | { data: Splitter[] }>('/splitters', { params: filters });
  return Array.isArray(response) ? response : (response as { data: Splitter[] }).data || [];
};

/**
 * Get splitter by ID
 * @param splitterId - Splitter ID
 * @returns Splitter details with ports
 */
export const getSplitter = async (splitterId: string): Promise<Splitter> => {
  const response = await apiClient.get<Splitter>(`/splitters/${splitterId}`);
  return response;
};

/**
 * Create a new splitter
 * @param splitterData - Splitter creation data
 * @returns Created splitter
 */
export const createSplitter = async (splitterData: CreateSplitterRequest): Promise<Splitter> => {
  const response = await apiClient.post<Splitter>('/splitters', splitterData);
  return response;
};

/**
 * Update splitter
 * @param splitterId - Splitter ID
 * @param splitterData - Splitter update data
 * @returns Updated splitter
 */
export const updateSplitter = async (splitterId: string, splitterData: UpdateSplitterRequest): Promise<Splitter> => {
  const response = await apiClient.put<Splitter>(`/splitters/${splitterId}`, splitterData);
  return response;
};

/**
 * Delete splitter
 * @param splitterId - Splitter ID
 * @returns Promise that resolves when splitter is deleted
 */
export const deleteSplitter = async (splitterId: string): Promise<void> => {
  await apiClient.delete(`/splitters/${splitterId}`);
};

/**
 * Update splitter port
 * @param splitterId - Splitter ID
 * @param portId - Port ID
 * @param portData - Port update data (status, orderId)
 * @returns Updated port
 */
export const updateSplitterPort = async (
  splitterId: string,
  portId: string,
  portData: UpdateSplitterPortRequest
): Promise<any> => {
  const response = await apiClient.put(`/splitters/${splitterId}/ports/${portId}`, portData);
  return response;
};

