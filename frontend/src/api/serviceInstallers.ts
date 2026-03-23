import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  ServiceInstaller,
  ServiceInstallerContact,
  CreateServiceInstallerRequest,
  UpdateServiceInstallerRequest,
  CreateServiceInstallerContactRequest,
  UpdateServiceInstallerContactRequest,
  ServiceInstallerFilters,
  ImportResult
} from '../types/serviceInstallers';

/**
 * Service Installers API
 * Handles service installer management
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Get all service installers
 * @param filters - Optional filters (isActive, installerType, siLevel, skillIds)
 * @returns Array of service installers
 */
export const getServiceInstallers = async (filters: ServiceInstallerFilters = {}): Promise<ServiceInstaller[]> => {
  const params: Record<string, string> = {};
  
  if (filters.departmentId) params.departmentId = filters.departmentId;
  if (filters.isActive !== undefined) params.isActive = filters.isActive.toString();
  if (filters.installerType) params.installerType = filters.installerType;
  if (filters.siLevel) params.siLevel = filters.siLevel;
  if (filters.skillIds && filters.skillIds.length > 0) {
    params.skillIds = filters.skillIds.join(',');
  }
  
  const response = await apiClient.get<ServiceInstaller[] | { data: ServiceInstaller[] }>('/service-installers', {
    params
  });
  // Backend returns array directly, but check if wrapped
  return Array.isArray(response) ? response : (response as { data: ServiceInstaller[] }).data || [];
};

/**
 * Get available installers for job assignment
 * @param filters - Optional filters (departmentId, installerType, siLevel, requiredSkillIds)
 * @returns Array of available service installers
 */
export const getAvailableInstallers = async (filters: {
  departmentId?: string;
  installerType?: 'InHouse' | 'Subcontractor';
  siLevel?: 'Junior' | 'Senior';
  requiredSkillIds?: string[];
} = {}): Promise<ServiceInstaller[]> => {
  const params: Record<string, string> = {};
  
  if (filters.departmentId) params.departmentId = filters.departmentId;
  if (filters.installerType) params.installerType = filters.installerType;
  if (filters.siLevel) params.siLevel = filters.siLevel;
  if (filters.requiredSkillIds && filters.requiredSkillIds.length > 0) {
    params.requiredSkillIds = filters.requiredSkillIds.join(',');
  }
  
  const response = await apiClient.get<ServiceInstaller[] | { data: ServiceInstaller[] }>('/service-installers/available', {
    params
  });
  return Array.isArray(response) ? response : (response as { data: ServiceInstaller[] }).data || [];
};

/**
 * Get service installer by ID
 * @param siId - Service Installer ID
 * @returns Service installer details
 */
export const getServiceInstaller = async (siId: string): Promise<ServiceInstaller> => {
  const response = await apiClient.get<ServiceInstaller>(`/service-installers/${siId}`);
  return response;
};

/**
 * Create a new service installer
 * @param siData - Service installer creation data
 * @returns Created service installer
 */
export const createServiceInstaller = async (siData: CreateServiceInstallerRequest): Promise<ServiceInstaller> => {
  const response = await apiClient.post<ServiceInstaller>('/service-installers', siData);
  return response;
};

/**
 * Update service installer
 * @param siId - Service Installer ID
 * @param siData - Service installer update data
 * @returns Updated service installer
 */
export const updateServiceInstaller = async (
  siId: string,
  siData: UpdateServiceInstallerRequest
): Promise<ServiceInstaller> => {
  const response = await apiClient.put<ServiceInstaller>(`/service-installers/${siId}`, siData);
  return response;
};

/**
 * Delete service installer
 * @param siId - Service Installer ID
 * @returns Promise that resolves when service installer is deleted
 */
export const deleteServiceInstaller = async (siId: string): Promise<void> => {
  await apiClient.delete(`/service-installers/${siId}`);
};

/**
 * Get contacts for a service installer
 * @param siId - Service Installer ID
 * @returns Array of contacts
 */
export const getServiceInstallerContacts = async (siId: string): Promise<ServiceInstallerContact[]> => {
  const response = await apiClient.get<ServiceInstallerContact[] | { data: ServiceInstallerContact[] }>(
    `/service-installers/${siId}/contacts`
  );
  return Array.isArray(response) ? response : (response as { data: ServiceInstallerContact[] }).data || [];
};

/**
 * Create a contact for a service installer
 * @param siId - Service Installer ID
 * @param contactData - Contact creation data
 * @returns Created contact
 */
export const createServiceInstallerContact = async (
  siId: string,
  contactData: CreateServiceInstallerContactRequest
): Promise<ServiceInstallerContact> => {
  const response = await apiClient.post<ServiceInstallerContact>(`/service-installers/${siId}/contacts`, contactData);
  return response;
};

/**
 * Update a service installer contact
 * @param contactId - Contact ID
 * @param contactData - Contact update data
 * @returns Updated contact
 */
export const updateServiceInstallerContact = async (
  contactId: string,
  contactData: UpdateServiceInstallerContactRequest
): Promise<ServiceInstallerContact> => {
  const response = await apiClient.put<ServiceInstallerContact>(`/service-installers/contacts/${contactId}`, contactData);
  return response;
};

/**
 * Delete a service installer contact
 * @param contactId - Contact ID
 * @returns Promise that resolves when contact is deleted
 */
export const deleteServiceInstallerContact = async (contactId: string): Promise<void> => {
  await apiClient.delete(`/service-installers/contacts/${contactId}`);
};

/**
 * Service Installers - Import/Export
 */

/**
 * Export service installers to CSV file
 * @param filters - Optional filters (isActive)
 */
export const exportServiceInstallers = async (filters: ServiceInstallerFilters = {}): Promise<void> => {
  const token = getAuthToken();
  const params = new URLSearchParams(filters as any).toString();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/service-installers/export${params ? '?' + params : ''}`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'service-installers.csv';
  
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
 * Download service installers CSV template
 */
export const downloadServiceInstallersTemplate = async (): Promise<void> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/service-installers/template`;
  
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
  a.download = 'service-installers-template.csv';
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Import service installers from CSV file
 * @param file - CSV file to import
 * @returns Import result
 */
export const importServiceInstallers = async (file: File): Promise<ImportResult> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/service-installers/import`;
  
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

