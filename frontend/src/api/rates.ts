import apiClient from './client';
import type {
  RateCard,
  RateCardLine,
  GponPartnerJobRate,
  GponSiJobRate,
  GponSiCustomRate,
  CreateRateCardRequest,
  UpdateRateCardRequest,
  CreateRateCardLineRequest,
  CreateGponPartnerJobRateRequest,
  CreateGponSiJobRateRequest,
  CreateGponSiCustomRateRequest,
  RateCardFilters,
  GponPartnerJobRateFilters,
  GponSiJobRateFilters,
  GponSiCustomRateFilters,
  RateResolutionRequest,
  RateResolutionResult,
  GponRateResolutionRequest,
  GponRateResolutionResult
} from '../types/rates';

/**
 * Rate Engine API
 * Handles universal rate cards and GPON-specific rate tables
 */

// ============ Rate Cards ============

/**
 * Get all rate cards
 */
export const getRateCards = async (filters: RateCardFilters = {}): Promise<RateCard[]> => {
  const response = await apiClient.get<RateCard[]>('/rates/ratecards', { params: filters });
  return response;
};

/**
 * Get a single rate card by ID
 */
export const getRateCard = async (id: string): Promise<RateCard> => {
  const response = await apiClient.get<RateCard>(`/rates/ratecards/${id}`);
  return response;
};

/**
 * Create a new rate card
 */
export const createRateCard = async (data: CreateRateCardRequest): Promise<RateCard> => {
  const response = await apiClient.post<RateCard>('/rates/ratecards', data);
  return response;
};

/**
 * Update a rate card
 */
export const updateRateCard = async (id: string, data: UpdateRateCardRequest): Promise<RateCard> => {
  const response = await apiClient.put<RateCard>(`/rates/ratecards/${id}`, data);
  return response;
};

/**
 * Delete a rate card
 */
export const deleteRateCard = async (id: string): Promise<void> => {
  await apiClient.delete(`/rates/ratecards/${id}`);
};

// ============ Rate Card Lines ============

/**
 * Get rate card lines for a specific rate card
 */
export const getRateCardLines = async (rateCardId: string): Promise<RateCardLine[]> => {
  const response = await apiClient.get<RateCardLine[]>(`/rates/ratecards/${rateCardId}/lines`);
  return response;
};

/**
 * Create a rate card line
 */
export const createRateCardLine = async (data: CreateRateCardLineRequest): Promise<RateCardLine> => {
  const response = await apiClient.post<RateCardLine>(`/rates/ratecards/${data.rateCardId}/lines`, data);
  return response;
};

/**
 * Update a rate card line
 */
export const updateRateCardLine = async (id: string, data: Partial<CreateRateCardLineRequest>): Promise<RateCardLine> => {
  const response = await apiClient.put<RateCardLine>(`/rates/ratecardlines/${id}`, data);
  return response;
};

/**
 * Delete a rate card line
 */
export const deleteRateCardLine = async (id: string): Promise<void> => {
  await apiClient.delete(`/rates/ratecardlines/${id}`);
};

// ============ GPON Partner Job Rates ============

/**
 * Get all GPON partner job rates (revenue rates)
 */
export const getGponPartnerJobRates = async (filters: GponPartnerJobRateFilters = {}): Promise<GponPartnerJobRate[]> => {
  const response = await apiClient.get<GponPartnerJobRate[]>('/rates/gpon/partner-rates', { params: filters });
  return response;
};

/**
 * Create a GPON partner job rate
 */
export const createGponPartnerJobRate = async (data: CreateGponPartnerJobRateRequest): Promise<GponPartnerJobRate> => {
  const response = await apiClient.post<GponPartnerJobRate>('/rates/gpon/partner-rates', data);
  return response;
};

/**
 * Update a GPON partner job rate
 */
export const updateGponPartnerJobRate = async (id: string, data: Partial<CreateGponPartnerJobRateRequest>): Promise<GponPartnerJobRate> => {
  const response = await apiClient.put<GponPartnerJobRate>(`/rates/gpon/partner-rates/${id}`, data);
  return response;
};

/**
 * Delete a GPON partner job rate
 */
export const deleteGponPartnerJobRate = async (id: string): Promise<void> => {
  await apiClient.delete(`/rates/gpon/partner-rates/${id}`);
};

// ============ GPON SI Job Rates ============

/**
 * Get all GPON SI job rates (payout rates by level)
 */
export const getGponSiJobRates = async (filters: GponSiJobRateFilters = {}): Promise<GponSiJobRate[]> => {
  const response = await apiClient.get<GponSiJobRate[]>('/rates/gpon/si-rates', { params: filters });
  return response;
};

/**
 * Create a GPON SI job rate
 */
export const createGponSiJobRate = async (data: CreateGponSiJobRateRequest): Promise<GponSiJobRate> => {
  const response = await apiClient.post<GponSiJobRate>('/rates/gpon/si-rates', data);
  return response;
};

/**
 * Update a GPON SI job rate
 */
export const updateGponSiJobRate = async (id: string, data: Partial<CreateGponSiJobRateRequest>): Promise<GponSiJobRate> => {
  const response = await apiClient.put<GponSiJobRate>(`/rates/gpon/si-rates/${id}`, data);
  return response;
};

/**
 * Delete a GPON SI job rate
 */
export const deleteGponSiJobRate = async (id: string): Promise<void> => {
  await apiClient.delete(`/rates/gpon/si-rates/${id}`);
};

// ============ GPON SI Custom Rates ============

/**
 * Get all GPON SI custom rates (per-SI overrides)
 */
export const getGponSiCustomRates = async (filters: GponSiCustomRateFilters = {}): Promise<GponSiCustomRate[]> => {
  const response = await apiClient.get<GponSiCustomRate[]>('/rates/gpon/si-custom-rates', { params: filters });
  return response;
};

/**
 * Create a GPON SI custom rate
 */
export const createGponSiCustomRate = async (data: CreateGponSiCustomRateRequest): Promise<GponSiCustomRate> => {
  const response = await apiClient.post<GponSiCustomRate>('/rates/gpon/si-custom-rates', data);
  return response;
};

/**
 * Update a GPON SI custom rate
 */
export const updateGponSiCustomRate = async (id: string, data: Partial<CreateGponSiCustomRateRequest>): Promise<GponSiCustomRate> => {
  const response = await apiClient.put<GponSiCustomRate>(`/rates/gpon/si-custom-rates/${id}`, data);
  return response;
};

/**
 * Delete a GPON SI custom rate
 */
export const deleteGponSiCustomRate = async (id: string): Promise<void> => {
  await apiClient.delete(`/rates/gpon/si-custom-rates/${id}`);
};

// ============ Rate Resolution ============

/**
 * Resolve rates for a given context (get revenue and payout amounts)
 */
export const resolveRates = async (request: RateResolutionRequest): Promise<RateResolutionResult> => {
  const response = await apiClient.post<RateResolutionResult>('/rates/resolve', request);
  return response;
};

/**
 * Resolve GPON rates with full result including resolution steps (for Rate Designer)
 */
export const resolveGponRates = async (
  request: GponRateResolutionRequest
): Promise<GponRateResolutionResult> => {
  const body = {
    ...request,
    referenceDate: request.referenceDate ? new Date(request.referenceDate).toISOString() : undefined
  };
  const response = await apiClient.post<GponRateResolutionResult>('/rates/resolve', body);
  return response;
};

// ============ Import/Export ============

/**
 * Export GPON partner rates to CSV
 */
export const exportGponPartnerRates = async (filters: GponPartnerJobRateFilters = {}): Promise<void> => {
  const response = await apiClient.get<Blob>('/rates/gpon/partner-rates/export', {
    params: filters,
    responseType: 'blob'
  });
  const url = window.URL.createObjectURL(response);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'gpon-partner-rates.csv';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

/**
 * Export GPON SI rates to CSV
 */
export const exportGponSiRates = async (filters: GponSiJobRateFilters = {}): Promise<void> => {
  const response = await apiClient.get<Blob>('/rates/gpon/si-rates/export', {
    params: filters,
    responseType: 'blob'
  });
  const url = window.URL.createObjectURL(response);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'gpon-si-rates.csv';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
};

