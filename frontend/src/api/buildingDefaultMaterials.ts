import apiClient from './client';
import type {
  BuildingDefaultMaterial,
  CreateBuildingDefaultMaterialRequest,
  UpdateBuildingDefaultMaterialRequest,
  BuildingDefaultMaterialFilters,
  DefaultMaterialsSummary
} from '../types/buildingDefaultMaterials';

/**
 * Building Default Materials API
 * Manages default materials per building + job type
 */

/**
 * Get all default materials for a building
 * @param buildingId - Building ID
 * @param params - Optional filters (orderTypeId, isActive)
 * @returns Array of default materials
 */
export const getBuildingDefaultMaterials = async (
  buildingId: string,
  params: BuildingDefaultMaterialFilters = {}
): Promise<BuildingDefaultMaterial[]> => {
  const response = await apiClient.get<BuildingDefaultMaterial[]>(`/buildings/${buildingId}/default-materials`, { params });
  return response;
};

/**
 * Get a specific default material by ID
 * @param buildingId - Building ID
 * @param id - Default material ID
 * @returns Default material
 */
export const getBuildingDefaultMaterial = async (buildingId: string, id: string): Promise<BuildingDefaultMaterial> => {
  const response = await apiClient.get<BuildingDefaultMaterial>(`/buildings/${buildingId}/default-materials/${id}`);
  return response;
};

/**
 * Create a new default material for a building
 * @param buildingId - Building ID
 * @param data - Material data (orderTypeId, materialId, defaultQuantity, notes)
 * @returns Created default material
 */
export const createBuildingDefaultMaterial = async (
  buildingId: string,
  data: CreateBuildingDefaultMaterialRequest
): Promise<BuildingDefaultMaterial> => {
  const response = await apiClient.post<BuildingDefaultMaterial>(`/buildings/${buildingId}/default-materials`, data);
  return response;
};

/**
 * Update an existing default material
 * @param buildingId - Building ID
 * @param id - Default material ID
 * @param data - Update data (defaultQuantity, notes, isActive)
 * @returns Updated default material
 */
export const updateBuildingDefaultMaterial = async (
  buildingId: string,
  id: string,
  data: UpdateBuildingDefaultMaterialRequest
): Promise<BuildingDefaultMaterial> => {
  const response = await apiClient.put<BuildingDefaultMaterial>(`/buildings/${buildingId}/default-materials/${id}`, data);
  return response;
};

/**
 * Delete a default material
 * @param buildingId - Building ID
 * @param id - Default material ID
 * @returns Promise that resolves when default material is deleted
 */
export const deleteBuildingDefaultMaterial = async (buildingId: string, id: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/default-materials/${id}`);
};

/**
 * Get summary of default materials for dashboard
 * @returns Summary data
 */
export const getDefaultMaterialsSummary = async (): Promise<DefaultMaterialsSummary> => {
  const response = await apiClient.get<DefaultMaterialsSummary>('/buildings/default-materials/summary');
  return response;
};

