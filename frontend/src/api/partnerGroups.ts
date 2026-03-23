import apiClient from './client';
import type { PartnerGroup, CreatePartnerGroupRequest, UpdatePartnerGroupRequest } from '../types/partnerGroups';

/**
 * Partner Groups API
 * Handles partner group management
 */

/**
 * Get all partner groups
 * @returns Array of partner groups
 */
export const getPartnerGroups = async (): Promise<PartnerGroup[]> => {
  const response = await apiClient.get<PartnerGroup[] | { data: PartnerGroup[] }>('/partner-groups');
  // Backend returns array directly, but check if wrapped
  return Array.isArray(response) ? response : (response as { data: PartnerGroup[] }).data || [];
};

/**
 * Get partner group by ID
 * @param partnerGroupId - Partner group ID
 * @returns Partner group details
 */
export const getPartnerGroup = async (partnerGroupId: string): Promise<PartnerGroup> => {
  const response = await apiClient.get<PartnerGroup>(`/partner-groups/${partnerGroupId}`);
  return response;
};

/**
 * Create a new partner group
 * @param partnerGroupData - Partner group creation data
 * @returns Created partner group
 */
export const createPartnerGroup = async (partnerGroupData: CreatePartnerGroupRequest): Promise<PartnerGroup> => {
  const response = await apiClient.post<PartnerGroup>('/partner-groups', partnerGroupData);
  return response;
};

/**
 * Update partner group
 * @param partnerGroupId - Partner group ID
 * @param partnerGroupData - Partner group update data
 * @returns Updated partner group
 */
export const updatePartnerGroup = async (partnerGroupId: string, partnerGroupData: UpdatePartnerGroupRequest): Promise<PartnerGroup> => {
  const response = await apiClient.put<PartnerGroup>(`/partner-groups/${partnerGroupId}`, partnerGroupData);
  return response;
};

/**
 * Delete partner group
 * @param partnerGroupId - Partner group ID
 * @returns Promise that resolves when partner group is deleted
 */
export const deletePartnerGroup = async (partnerGroupId: string): Promise<void> => {
  await apiClient.delete(`/partner-groups/${partnerGroupId}`);
};

