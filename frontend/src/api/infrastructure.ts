import apiClient from './client';
import type {
  BuildingInfrastructure,
  BuildingBlock,
  BuildingSplitter,
  Street,
  HubBox,
  Pole,
  CreateBuildingBlockRequest,
  UpdateBuildingBlockRequest,
  CreateBuildingSplitterRequest,
  UpdateBuildingSplitterRequest,
  CreateStreetRequest,
  UpdateStreetRequest,
  CreateHubBoxRequest,
  UpdateHubBoxRequest,
  CreatePoleRequest,
  UpdatePoleRequest,
  SplitterFilters,
  SplitterStatus,
  HubBoxStatus,
  PoleStatus,
  PoleType,
  SplitterStatusLabels,
  PoleStatusLabels,
  PoleTypes
} from '../types/infrastructure';

/**
 * Building Infrastructure API
 * Handles blocks, splitters, streets, hub boxes, and poles
 */

// Export enums and constants (re-export from types)
export {
  SplitterStatus,
  HubBoxStatus,
  PoleStatus,
  SplitterStatusLabels,
  PoleStatusLabels,
  PoleTypes
} from '../types/infrastructure';

// Export aliases for backwards compatibility
export { SplitterStatus as SplitterStatuses, HubBoxStatus as HubBoxStatuses, PoleStatus as PoleStatuses } from '../types/infrastructure';

export type { PoleType } from '../types/infrastructure';

/**
 * Infrastructure Overview
 */

/**
 * Get building infrastructure overview
 * @param buildingId - Building ID
 * @returns Infrastructure overview
 */
export const getBuildingInfrastructure = async (buildingId: string): Promise<BuildingInfrastructure> => {
  const response = await apiClient.get<BuildingInfrastructure>(`/buildings/${buildingId}/infrastructure`);
  return response;
};

/**
 * Building Blocks
 */

/**
 * Get building blocks
 * @param buildingId - Building ID
 * @returns Array of blocks
 */
export const getBuildingBlocks = async (buildingId: string): Promise<BuildingBlock[]> => {
  const response = await apiClient.get<BuildingBlock[] | { data: BuildingBlock[] }>(
    `/buildings/${buildingId}/infrastructure/blocks`
  );
  return Array.isArray(response) ? response : (response as { data: BuildingBlock[] }).data || [];
};

/**
 * Create building block
 * @param buildingId - Building ID
 * @param data - Block data
 * @returns Created block
 */
export const createBuildingBlock = async (buildingId: string, data: CreateBuildingBlockRequest): Promise<BuildingBlock> => {
  const response = await apiClient.post<BuildingBlock>(`/buildings/${buildingId}/infrastructure/blocks`, data);
  return response;
};

/**
 * Update building block
 * @param buildingId - Building ID
 * @param blockId - Block ID
 * @param data - Block data
 * @returns Updated block
 */
export const updateBuildingBlock = async (
  buildingId: string,
  blockId: string,
  data: UpdateBuildingBlockRequest
): Promise<BuildingBlock> => {
  const response = await apiClient.put<BuildingBlock>(`/buildings/${buildingId}/infrastructure/blocks/${blockId}`, data);
  return response;
};

/**
 * Delete building block
 * @param buildingId - Building ID
 * @param blockId - Block ID
 * @returns Promise that resolves when block is deleted
 */
export const deleteBuildingBlock = async (buildingId: string, blockId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/infrastructure/blocks/${blockId}`);
};

/**
 * Building Splitters
 */

/**
 * Get building splitters
 * @param buildingId - Building ID
 * @param filters - Optional filters
 * @returns Array of splitters
 */
export const getBuildingSplitters = async (buildingId: string, filters: SplitterFilters = {}): Promise<BuildingSplitter[]> => {
  const response = await apiClient.get<BuildingSplitter[] | { data: BuildingSplitter[] }>(
    `/buildings/${buildingId}/infrastructure/splitters`,
    { params: filters }
  );
  return Array.isArray(response) ? response : (response as { data: BuildingSplitter[] }).data || [];
};

/**
 * Get all splitters across all buildings
 * @param filters - Optional filters
 * @returns Array of splitters
 */
export const getAllSplitters = async (filters: SplitterFilters = {}): Promise<BuildingSplitter[]> => {
  const response = await apiClient.get<BuildingSplitter[] | { data: BuildingSplitter[] }>('/splitters', { params: filters });
  return Array.isArray(response) ? response : (response as { data: BuildingSplitter[] }).data || [];
};

/**
 * Create building splitter
 * @param buildingId - Building ID
 * @param data - Splitter data
 * @returns Created splitter
 */
export const createBuildingSplitter = async (
  buildingId: string,
  data: CreateBuildingSplitterRequest
): Promise<BuildingSplitter> => {
  const response = await apiClient.post<BuildingSplitter>(`/buildings/${buildingId}/infrastructure/splitters`, data);
  return response;
};

/**
 * Update building splitter
 * @param buildingId - Building ID
 * @param splitterId - Splitter ID
 * @param data - Splitter data
 * @returns Updated splitter
 */
export const updateBuildingSplitter = async (
  buildingId: string,
  splitterId: string,
  data: UpdateBuildingSplitterRequest
): Promise<BuildingSplitter> => {
  const response = await apiClient.put<BuildingSplitter>(
    `/buildings/${buildingId}/infrastructure/splitters/${splitterId}`,
    data
  );
  return response;
};

/**
 * Delete building splitter
 * @param buildingId - Building ID
 * @param splitterId - Splitter ID
 * @returns Promise that resolves when splitter is deleted
 */
export const deleteBuildingSplitter = async (buildingId: string, splitterId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/infrastructure/splitters/${splitterId}`);
};

/**
 * Update splitter port usage
 * @param buildingId - Building ID
 * @param splitterId - Splitter ID
 * @param portsUsed - New ports used count
 * @returns Updated splitter
 */
export const updateSplitterPorts = async (buildingId: string, splitterId: string, portsUsed: number): Promise<BuildingSplitter> => {
  const response = await apiClient.patch<BuildingSplitter>(
    `/buildings/${buildingId}/infrastructure/splitters/${splitterId}/ports`,
    portsUsed
  );
  return response;
};

/**
 * Streets
 */

/**
 * Get streets
 * @param buildingId - Building ID
 * @returns Array of streets
 */
export const getStreets = async (buildingId: string): Promise<Street[]> => {
  const response = await apiClient.get<Street[] | { data: Street[] }>(`/buildings/${buildingId}/infrastructure/streets`);
  return Array.isArray(response) ? response : (response as { data: Street[] }).data || [];
};

/**
 * Create street
 * @param buildingId - Building ID
 * @param data - Street data
 * @returns Created street
 */
export const createStreet = async (buildingId: string, data: CreateStreetRequest): Promise<Street> => {
  const response = await apiClient.post<Street>(`/buildings/${buildingId}/infrastructure/streets`, data);
  return response;
};

/**
 * Update street
 * @param buildingId - Building ID
 * @param streetId - Street ID
 * @param data - Street data
 * @returns Updated street
 */
export const updateStreet = async (buildingId: string, streetId: string, data: UpdateStreetRequest): Promise<Street> => {
  const response = await apiClient.put<Street>(`/buildings/${buildingId}/infrastructure/streets/${streetId}`, data);
  return response;
};

/**
 * Delete street
 * @param buildingId - Building ID
 * @param streetId - Street ID
 * @returns Promise that resolves when street is deleted
 */
export const deleteStreet = async (buildingId: string, streetId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/infrastructure/streets/${streetId}`);
};

/**
 * Hub Boxes
 */

/**
 * Get hub boxes
 * @param buildingId - Building ID
 * @returns Array of hub boxes
 */
export const getHubBoxes = async (buildingId: string): Promise<HubBox[]> => {
  const response = await apiClient.get<HubBox[] | { data: HubBox[] }>(`/buildings/${buildingId}/infrastructure/hubboxes`);
  return Array.isArray(response) ? response : (response as { data: HubBox[] }).data || [];
};

/**
 * Create hub box
 * @param buildingId - Building ID
 * @param data - Hub box data
 * @returns Created hub box
 */
export const createHubBox = async (buildingId: string, data: CreateHubBoxRequest): Promise<HubBox> => {
  const response = await apiClient.post<HubBox>(`/buildings/${buildingId}/infrastructure/hubboxes`, data);
  return response;
};

/**
 * Update hub box
 * @param buildingId - Building ID
 * @param hubBoxId - Hub box ID
 * @param data - Hub box data
 * @returns Updated hub box
 */
export const updateHubBox = async (buildingId: string, hubBoxId: string, data: UpdateHubBoxRequest): Promise<HubBox> => {
  const response = await apiClient.put<HubBox>(`/buildings/${buildingId}/infrastructure/hubboxes/${hubBoxId}`, data);
  return response;
};

/**
 * Delete hub box
 * @param buildingId - Building ID
 * @param hubBoxId - Hub box ID
 * @returns Promise that resolves when hub box is deleted
 */
export const deleteHubBox = async (buildingId: string, hubBoxId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/infrastructure/hubboxes/${hubBoxId}`);
};

/**
 * Poles
 */

/**
 * Get poles
 * @param buildingId - Building ID
 * @returns Array of poles
 */
export const getPoles = async (buildingId: string): Promise<Pole[]> => {
  const response = await apiClient.get<Pole[] | { data: Pole[] }>(`/buildings/${buildingId}/infrastructure/poles`);
  return Array.isArray(response) ? response : (response as { data: Pole[] }).data || [];
};

/**
 * Create pole
 * @param buildingId - Building ID
 * @param data - Pole data
 * @returns Created pole
 */
export const createPole = async (buildingId: string, data: CreatePoleRequest): Promise<Pole> => {
  const response = await apiClient.post<Pole>(`/buildings/${buildingId}/infrastructure/poles`, data);
  return response;
};

/**
 * Update pole
 * @param buildingId - Building ID
 * @param poleId - Pole ID
 * @param data - Pole data
 * @returns Updated pole
 */
export const updatePole = async (buildingId: string, poleId: string, data: UpdatePoleRequest): Promise<Pole> => {
  const response = await apiClient.put<Pole>(`/buildings/${buildingId}/infrastructure/poles/${poleId}`, data);
  return response;
};

/**
 * Delete pole
 * @param buildingId - Building ID
 * @param poleId - Pole ID
 * @returns Promise that resolves when pole is deleted
 */
export const deletePole = async (buildingId: string, poleId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/infrastructure/poles/${poleId}`);
};

