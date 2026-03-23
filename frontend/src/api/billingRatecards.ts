import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  BillingRatecard,
  CreateBillingRatecardRequest,
  UpdateBillingRatecardRequest,
  BillingRatecardFilters,
  ImportResult
} from '../types/billingRatecards';

/**
 * Billing Ratecards API (Partner Rates / PU Rates)
 * Handles partner billing rate management
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Get all billing ratecards
 * @param filters - Optional filters (partnerId, orderTypeId, departmentId, isActive)
 * @returns Array of billing ratecards
 */
export const getBillingRatecards = async (filters: BillingRatecardFilters = {}): Promise<BillingRatecard[]> => {
  const response = await apiClient.get<BillingRatecard[] | { data: BillingRatecard[] }>('/billing/ratecards', {
    params: filters
  });
  return Array.isArray(response) ? response : (response as { data: BillingRatecard[] }).data || [];
};

/**
 * Get billing ratecard by ID
 * @param ratecardId - Ratecard ID
 * @returns Billing ratecard details
 */
export const getBillingRatecard = async (ratecardId: string): Promise<BillingRatecard> => {
  const response = await apiClient.get<BillingRatecard>(`/billing/ratecards/${ratecardId}`);
  return response;
};

/**
 * Create a new billing ratecard
 * @param ratecardData - Ratecard creation data
 * @returns Created billing ratecard
 */
export const createBillingRatecard = async (ratecardData: CreateBillingRatecardRequest): Promise<BillingRatecard> => {
  const response = await apiClient.post<BillingRatecard>('/billing/ratecards', ratecardData);
  return response;
};

/**
 * Update billing ratecard
 * @param ratecardId - Ratecard ID
 * @param ratecardData - Ratecard update data
 * @returns Updated billing ratecard
 */
export const updateBillingRatecard = async (
  ratecardId: string,
  ratecardData: UpdateBillingRatecardRequest
): Promise<BillingRatecard> => {
  const response = await apiClient.put<BillingRatecard>(`/billing/ratecards/${ratecardId}`, ratecardData);
  return response;
};

/**
 * Delete billing ratecard
 * @param ratecardId - Ratecard ID
 * @returns Promise that resolves when ratecard is deleted
 */
export const deleteBillingRatecard = async (ratecardId: string): Promise<void> => {
  await apiClient.delete(`/billing/ratecards/${ratecardId}`);
};

/**
 * Partner Rates - Import/Export
 */

/**
 * Export partner rates to CSV file
 * @param filters - Optional filters
 */
export const exportPartnerRates = async (filters: BillingRatecardFilters = {}): Promise<void> => {
  const token = getAuthToken();
  const params = new URLSearchParams(filters as any).toString();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/billing/ratecards/export${params ? '?' + params : ''}`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'partner-rates.csv';
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Download partner rates CSV template
 */
export const downloadPartnerRatesTemplate = async (): Promise<void> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/billing/ratecards/template`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Template download failed');
  }
  
  const blob = await response.blob();
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = 'partner-rates-template.csv';
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Import partner rates from CSV file
 * @param file - CSV file to import
 * @returns Import result
 */
export const importPartnerRates = async (file: File): Promise<ImportResult> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/billing/ratecards/import`;
  
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Import failed');
  }
  
  return response.json();
};

