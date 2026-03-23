import apiClient from './client';
import type {
  KpiProfile,
  CreateKpiProfileRequest,
  UpdateKpiProfileRequest,
  KpiProfileFilters,
  EffectiveKpiProfileParams,
  KpiEvaluationResult
} from '../types/kpiProfiles';

/**
 * KPI Profiles API
 * Handles configurable KPI rules for scheduler and payroll
 */

/**
 * Get KPI profiles
 * @param filters - Optional filters (orderType, partnerId, buildingTypeId, isActive)
 * @returns Array of KPI profiles
 */
export const getKpiProfiles = async (filters: KpiProfileFilters = {}): Promise<KpiProfile[]> => {
  const response = await apiClient.get<KpiProfile[]>('/kpi-profiles', { params: filters });
  return response;
};

/**
 * Get KPI profile by ID
 * @param profileId - Profile ID
 * @returns KPI profile details
 */
export const getKpiProfile = async (profileId: string): Promise<KpiProfile> => {
  const response = await apiClient.get<KpiProfile>(`/kpi-profiles/${profileId}`);
  return response;
};

/**
 * Get effective KPI profile for order context
 * @param params - Parameters (orderType, partnerId, buildingTypeId, jobDate)
 * @returns Effective KPI profile
 */
export const getEffectiveKpiProfile = async (params: EffectiveKpiProfileParams): Promise<KpiProfile> => {
  const response = await apiClient.get<KpiProfile>('/kpi-profiles/effective', { params });
  return response;
};

/**
 * Create KPI profile
 * @param profileData - Profile creation data
 * @returns Created profile
 */
export const createKpiProfile = async (profileData: CreateKpiProfileRequest): Promise<KpiProfile> => {
  const response = await apiClient.post<KpiProfile>('/kpi-profiles', profileData);
  return response;
};

/**
 * Update KPI profile
 * @param profileId - Profile ID
 * @param profileData - Profile update data
 * @returns Updated profile
 */
export const updateKpiProfile = async (profileId: string, profileData: UpdateKpiProfileRequest): Promise<KpiProfile> => {
  const response = await apiClient.put<KpiProfile>(`/kpi-profiles/${profileId}`, profileData);
  return response;
};

/**
 * Delete KPI profile
 * @param profileId - Profile ID
 * @returns Promise that resolves when profile is deleted
 */
export const deleteKpiProfile = async (profileId: string): Promise<void> => {
  await apiClient.delete(`/kpi-profiles/${profileId}`);
};

/**
 * Set KPI profile as default
 * @param profileId - Profile ID
 * @returns Updated profile
 */
export const setKpiProfileAsDefault = async (profileId: string): Promise<KpiProfile> => {
  const response = await apiClient.post<KpiProfile>(`/kpi-profiles/${profileId}/set-default`);
  return response;
};

// Alias for backward compatibility
export const setDefaultKpiProfile = setKpiProfileAsDefault;

/**
 * Evaluate KPI for an order
 * @param orderId - Order ID
 * @returns KPI evaluation result
 */
export const evaluateOrderKpi = async (orderId: string): Promise<KpiEvaluationResult> => {
  const response = await apiClient.get<KpiEvaluationResult>(`/kpi-profiles/evaluate-order/${orderId}`);
  return response;
};

