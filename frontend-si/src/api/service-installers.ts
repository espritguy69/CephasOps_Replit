import apiClient from './client';
import type { ServiceInstaller } from '../types/api';

/**
 * Service Installers API
 * Handles SI-related API calls
 */

export interface ServiceInstallerFilters {
  isActive?: boolean;
}

/**
 * Get all service installers (admin only)
 */
export const getAllServiceInstallers = async (filters: ServiceInstallerFilters = {}): Promise<ServiceInstaller[]> => {
  const response = await apiClient.get<ServiceInstaller[] | { data: ServiceInstaller[] }>('/service-installers', {
    params: filters
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ServiceInstaller[] }).data;
  }
  return [];
};

/**
 * Get service installer by ID
 */
export const getServiceInstaller = async (siId: string): Promise<ServiceInstaller> => {
  const response = await apiClient.get<ServiceInstaller | { data: ServiceInstaller }>(`/service-installers/${siId}`);
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ServiceInstaller }).data;
  }
  return response as ServiceInstaller;
};

/**
 * Get current user's service installer profile
 */
export const getMyServiceInstallerProfile = async (): Promise<ServiceInstaller | null> => {
  // Get current user's SI profile
  // The backend should resolve this from the current user's ID
  try {
    const response = await apiClient.get<ServiceInstaller | { data: ServiceInstaller }>('/service-installers/me');
    
    if (response && typeof response === 'object' && 'data' in response) {
      return (response as { data: ServiceInstaller }).data;
    }
    return response as ServiceInstaller;
  } catch {
    // Endpoint might not exist, return null
    return null;
  }
};

