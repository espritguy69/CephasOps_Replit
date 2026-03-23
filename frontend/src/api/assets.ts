import apiClient from './client';
import type {
  Asset,
  AssetSummary,
  CreateAssetRequest,
  UpdateAssetRequest,
  MaintenanceRecord,
  CreateMaintenanceRecordRequest,
  UpdateMaintenanceRecordRequest,
  DepreciationEntry,
  DepreciationSchedule,
  RunDepreciationRequest,
  Disposal,
  CreateDisposalRequest,
  ApproveDisposalRequest,
  AssetFilters,
  MaintenanceFilters,
  DepreciationFilters,
  DisposalFilters,
  AssetStatus,
  MaintenanceType,
  DisposalMethod,
  AssetStatusLabels,
  MaintenanceTypeLabels,
  DisposalMethodLabels
} from '../types/assets';

/**
 * Assets API
 */

// Export enums and labels (re-export from types)
export {
  AssetStatus,
  MaintenanceType,
  DisposalMethod,
  AssetStatusLabels,
  MaintenanceTypeLabels,
  DisposalMethodLabels
} from '../types/assets';

/**
 * Get all assets
 * @param params - Optional filters
 * @returns Array of assets
 */
export const getAssets = async (params: AssetFilters = {}): Promise<Asset[]> => {
  const response = await apiClient.get<Asset[]>('/assets', { params });
  return response;
};

/**
 * Get asset summary for dashboard
 * @returns Asset summary data
 */
export const getAssetSummary = async (): Promise<AssetSummary> => {
  const response = await apiClient.get<AssetSummary>('/assets/summary');
  return response;
};

/**
 * Get single asset by ID
 * @param id - Asset ID
 * @returns Asset details
 */
export const getAsset = async (id: string): Promise<Asset> => {
  const response = await apiClient.get<Asset>(`/assets/${id}`);
  return response;
};

/**
 * Create new asset
 * @param data - Asset creation data
 * @returns Created asset
 */
export const createAsset = async (data: CreateAssetRequest): Promise<Asset> => {
  const response = await apiClient.post<Asset>('/assets', data);
  return response;
};

/**
 * Update asset
 * @param id - Asset ID
 * @param data - Asset update data
 * @returns Updated asset
 */
export const updateAsset = async (id: string, data: UpdateAssetRequest): Promise<Asset> => {
  const response = await apiClient.put<Asset>(`/assets/${id}`, data);
  return response;
};

/**
 * Delete asset
 * @param id - Asset ID
 * @returns Promise that resolves when asset is deleted
 */
export const deleteAsset = async (id: string): Promise<void> => {
  await apiClient.delete(`/assets/${id}`);
};

/**
 * Maintenance API
 */

/**
 * Get maintenance records
 * @param params - Optional filters
 * @returns Array of maintenance records
 */
export const getMaintenanceRecords = async (params: MaintenanceFilters = {}): Promise<MaintenanceRecord[]> => {
  const response = await apiClient.get<MaintenanceRecord[]>('/assets/maintenance', { params });
  return response;
};

/**
 * Get upcoming maintenance
 * @param daysAhead - Number of days ahead to look (default: 30)
 * @returns Array of upcoming maintenance records
 */
export const getUpcomingMaintenance = async (daysAhead: number = 30): Promise<MaintenanceRecord[]> => {
  const response = await apiClient.get<MaintenanceRecord[]>('/assets/maintenance/upcoming', { params: { daysAhead } });
  return response;
};

/**
 * Create maintenance record
 * @param data - Maintenance record data
 * @returns Created maintenance record
 */
export const createMaintenanceRecord = async (data: CreateMaintenanceRecordRequest): Promise<MaintenanceRecord> => {
  const response = await apiClient.post<MaintenanceRecord>('/assets/maintenance', data);
  return response;
};

/**
 * Update maintenance record
 * @param id - Maintenance record ID
 * @param data - Maintenance record update data
 * @returns Updated maintenance record
 */
export const updateMaintenanceRecord = async (
  id: string,
  data: UpdateMaintenanceRecordRequest
): Promise<MaintenanceRecord> => {
  const response = await apiClient.put<MaintenanceRecord>(`/assets/maintenance/${id}`, data);
  return response;
};

/**
 * Delete maintenance record
 * @param id - Maintenance record ID
 * @returns Promise that resolves when maintenance record is deleted
 */
export const deleteMaintenanceRecord = async (id: string): Promise<void> => {
  await apiClient.delete(`/assets/maintenance/${id}`);
};

/**
 * Depreciation API
 */

/**
 * Get depreciation entries
 * @param params - Optional filters
 * @returns Array of depreciation entries
 */
export const getDepreciationEntries = async (params: DepreciationFilters = {}): Promise<DepreciationEntry[]> => {
  const response = await apiClient.get<DepreciationEntry[]>('/assets/depreciation', { params });
  return response;
};

/**
 * Get depreciation schedule for an asset
 * @param assetId - Asset ID
 * @returns Depreciation schedule
 */
export const getDepreciationSchedule = async (assetId: string): Promise<DepreciationSchedule> => {
  const response = await apiClient.get<DepreciationSchedule>(`/assets/${assetId}/depreciation-schedule`);
  return response;
};

/**
 * Run depreciation for a period
 * @param data - Depreciation run data
 * @returns Depreciation entries created
 */
export const runDepreciation = async (data: RunDepreciationRequest): Promise<DepreciationEntry[]> => {
  const response = await apiClient.post<DepreciationEntry[]>('/assets/depreciation/run', data);
  return response;
};

/**
 * Post depreciation entries
 * @param period - Period to post
 * @returns Posted depreciation entries
 */
export const postDepreciation = async (period: string): Promise<DepreciationEntry[]> => {
  const response = await apiClient.post<DepreciationEntry[]>('/assets/depreciation/post', null, { params: { period } });
  return response;
};

/**
 * Disposal API
 */

/**
 * Get disposals
 * @param params - Optional filters
 * @returns Array of disposals
 */
export const getDisposals = async (params: DisposalFilters = {}): Promise<Disposal[]> => {
  const response = await apiClient.get<Disposal[]>('/assets/disposals', { params });
  return response;
};

/**
 * Create disposal
 * @param data - Disposal data
 * @returns Created disposal
 */
export const createDisposal = async (data: CreateDisposalRequest): Promise<Disposal> => {
  const response = await apiClient.post<Disposal>('/assets/disposals', data);
  return response;
};

/**
 * Approve/reject disposal
 * @param id - Disposal ID
 * @param data - Approval data
 * @returns Updated disposal
 */
export const approveDisposal = async (id: string, data: ApproveDisposalRequest): Promise<Disposal> => {
  const response = await apiClient.post<Disposal>(`/assets/disposals/${id}/approve`, data);
  return response;
};

