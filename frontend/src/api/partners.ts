import apiClient from './client';
import type { Partner, CreatePartnerRequest, UpdatePartnerRequest, PartnerFilters } from '../types/partners';

/**
 * Partners API
 * Handles partner management
 */

/**
 * Get all partners
 * @param filters - Optional filters (isActive)
 * @returns Array of partners
 */
export const getPartners = async (filters: PartnerFilters = {}): Promise<Partner[]> => {
  const response = await apiClient.get<Partner[] | { data: Partner[] }>('/partners', { params: filters });
  // Backend returns array directly, but check if wrapped
  return Array.isArray(response) ? response : (response as { data: Partner[] }).data || [];
};

/**
 * Get partner by ID
 * @param partnerId - Partner ID
 * @returns Partner details
 */
export const getPartner = async (partnerId: string): Promise<Partner> => {
  const response = await apiClient.get<Partner>(`/partners/${partnerId}`);
  return response;
};

/**
 * Create a new partner
 * @param partnerData - Partner creation data
 * @returns Created partner
 */
export const createPartner = async (partnerData: CreatePartnerRequest): Promise<Partner> => {
  const response = await apiClient.post<Partner>('/partners', partnerData);
  return response;
};

/**
 * Update partner
 * @param partnerId - Partner ID
 * @param partnerData - Partner update data
 * @returns Updated partner
 */
export const updatePartner = async (partnerId: string, partnerData: UpdatePartnerRequest): Promise<Partner> => {
  const response = await apiClient.put<Partner>(`/partners/${partnerId}`, partnerData);
  return response;
};

/**
 * Delete partner
 * @param partnerId - Partner ID
 * @returns Promise that resolves when partner is deleted
 */
export const deletePartner = async (partnerId: string): Promise<void> => {
  await apiClient.delete(`/partners/${partnerId}`);
};

