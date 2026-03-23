import apiClient from './client';
import type {
  GlobalSetting,
  CreateGlobalSettingRequest,
  UpdateGlobalSettingRequest,
  GlobalSettingFilters
} from '../types/globalSettings';

/**
 * Global Settings API
 * Handles system-wide configuration values
 */

/**
 * Get all global settings
 * @param filters - Optional filters (module)
 * @returns Array of global settings
 */
export const getGlobalSettings = async (filters: GlobalSettingFilters = {}): Promise<GlobalSetting[]> => {
  const response = await apiClient.get<GlobalSetting[]>('/global-settings', { params: filters });
  return response;
};

/**
 * Get global setting by key
 * @param key - Setting key
 * @returns Global setting
 */
export const getGlobalSetting = async (key: string): Promise<GlobalSetting> => {
  const response = await apiClient.get<GlobalSetting>(`/global-settings/${key}`);
  return response;
};

/**
 * Create global setting
 * @param settingData - Setting creation data
 * @returns Created setting
 */
export const createGlobalSetting = async (settingData: CreateGlobalSettingRequest): Promise<GlobalSetting> => {
  const response = await apiClient.post<GlobalSetting>('/global-settings', settingData);
  return response;
};

/**
 * Update global setting
 * @param key - Setting key
 * @param settingData - Setting update data
 * @returns Updated setting
 */
export const updateGlobalSetting = async (
  key: string,
  settingData: UpdateGlobalSettingRequest
): Promise<GlobalSetting> => {
  const response = await apiClient.put<GlobalSetting>(`/global-settings/${key}`, settingData);
  return response;
};

/**
 * Delete global setting
 * @param key - Setting key
 * @returns Promise that resolves when setting is deleted
 */
export const deleteGlobalSetting = async (key: string): Promise<void> => {
  await apiClient.delete(`/global-settings/${key}`);
};

