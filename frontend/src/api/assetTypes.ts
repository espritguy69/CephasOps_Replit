import apiClient from './client';
import type {
  AssetType,
  CreateAssetTypeRequest,
  UpdateAssetTypeRequest,
  AssetTypeFilters,
  DepreciationMethod,
  DepreciationMethodLabels
} from '../types/assetTypes';

/**
 * Asset Types API
 */

// Export enums and labels (re-export from types)
export { DepreciationMethod, DepreciationMethodLabels } from '../types/assetTypes';

/**
 * Get all asset types
 * @param params - Optional filters
 * @returns Array of asset types
 */
export const getAssetTypes = async (params: AssetTypeFilters = {}): Promise<AssetType[]> => {
  const response = await apiClient.get<AssetType[]>('/asset-types', { params });
  return response;
};

/**
 * Get single asset type by ID
 * @param id - Asset type ID
 * @returns Asset type details
 */
export const getAssetType = async (id: string): Promise<AssetType> => {
  const response = await apiClient.get<AssetType>(`/asset-types/${id}`);
  return response;
};

/**
 * Create new asset type
 * @param data - Asset type creation data
 * @returns Created asset type
 */
export const createAssetType = async (data: CreateAssetTypeRequest): Promise<AssetType> => {
  const response = await apiClient.post<AssetType>('/asset-types', data);
  return response;
};

/**
 * Update asset type
 * @param id - Asset type ID
 * @param data - Asset type update data
 * @returns Updated asset type
 */
export const updateAssetType = async (id: string, data: UpdateAssetTypeRequest): Promise<AssetType> => {
  const response = await apiClient.put<AssetType>(`/asset-types/${id}`, data);
  return response;
};

/**
 * Delete asset type
 * @param id - Asset type ID
 * @returns Promise that resolves when asset type is deleted
 */
export const deleteAssetType = async (id: string): Promise<void> => {
  await apiClient.delete(`/asset-types/${id}`);
};

