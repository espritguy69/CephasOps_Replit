import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  Building,
  BuildingContact,
  BuildingRules,
  BuildingsSummary,
  CreateBuildingRequest,
  UpdateBuildingRequest,
  CreateBuildingContactRequest,
  UpdateBuildingContactRequest,
  SaveBuildingRulesRequest,
  BuildingFilters,
  ImportResult,
  PropertyType,
  ContactRole,
  PropertyTypeLabels,
  ContactRoles,
  BuildingListItem,
  BuildingMergePreview,
  MergeBuildingsRequest,
  BuildingMergeResult
} from '../types/buildings';

/**
 * Buildings API
 * Handles building management including contacts and rules
 */

// Export enums and constants (re-export from types)
export { PropertyType, PropertyTypeLabels, ContactRoles } from '../types/buildings';
// Export alias for backwards compatibility
export { PropertyType as PropertyTypes } from '../types/buildings';
export type { ContactRole };

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Get buildings summary for dashboard
 * @returns Summary data with KPIs and breakdowns
 */
export const getBuildingsSummary = async (): Promise<BuildingsSummary> => {
  const response = await apiClient.get<BuildingsSummary>('/buildings/summary');
  return response;
};

/**
 * Get all buildings with optional filters
 * @param filters - Optional filters (propertyType, installationMethodId, state, city, isActive)
 * @returns Array of buildings
 */
export const getBuildings = async (filters: BuildingFilters = {}): Promise<Building[]> => {
  const response = await apiClient.get<Building[] | { data: Building[] }>('/buildings', { params: filters });
  // Handle both direct array and wrapped response
  if (Array.isArray(response)) {
    return response;
  }
  return (response as { data: Building[] }).data || [];
};

/**
 * Get building by ID (includes contacts and rules)
 * @param buildingId - Building ID
 * @returns Building details with contacts and rules
 */
export const getBuilding = async (buildingId: string): Promise<Building> => {
  const response = await apiClient.get<Building>(`/buildings/${buildingId}`);
  return response;
};

/**
 * Create a new building
 * @param buildingData - Building creation data
 * @returns Created building
 */
export const createBuilding = async (buildingData: CreateBuildingRequest): Promise<Building> => {
  const response = await apiClient.post<Building>('/buildings', buildingData);
  return response;
};

/**
 * Update building
 * @param buildingId - Building ID
 * @param buildingData - Building update data
 * @returns Updated building
 */
export const updateBuilding = async (buildingId: string, buildingData: UpdateBuildingRequest): Promise<Building> => {
  const response = await apiClient.put<Building>(`/buildings/${buildingId}`, buildingData);
  return response;
};

/**
 * Delete building
 * @param buildingId - Building ID
 * @returns Promise that resolves when building is deleted
 */
export const deleteBuilding = async (buildingId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}`);
};

/**
 * Building Contacts
 */

/**
 * Get building contacts
 * @param buildingId - Building ID
 * @returns Array of contacts
 */
export const getBuildingContacts = async (buildingId: string): Promise<BuildingContact[]> => {
  if (!buildingId || buildingId.trim() === '') {
    throw new Error('Building ID is required');
  }
  const response = await apiClient.get<BuildingContact[] | { data: BuildingContact[] }>(`/buildings/${buildingId}/contacts`);
  // Handle both direct array and wrapped response
  if (Array.isArray(response)) {
    return response;
  }
  return (response as { data: BuildingContact[] }).data || [];
};

/**
 * Create building contact
 * @param buildingId - Building ID
 * @param contactData - Contact data
 * @returns Created contact
 */
export const createBuildingContact = async (
  buildingId: string,
  contactData: CreateBuildingContactRequest
): Promise<BuildingContact> => {
  const response = await apiClient.post<BuildingContact>(`/buildings/${buildingId}/contacts`, contactData);
  return response;
};

/**
 * Update building contact
 * @param buildingId - Building ID
 * @param contactId - Contact ID
 * @param contactData - Contact data
 * @returns Updated contact
 */
export const updateBuildingContact = async (
  buildingId: string,
  contactId: string,
  contactData: UpdateBuildingContactRequest
): Promise<BuildingContact> => {
  const response = await apiClient.put<BuildingContact>(`/buildings/${buildingId}/contacts/${contactId}`, contactData);
  return response;
};

/**
 * Delete building contact
 * @param buildingId - Building ID
 * @param contactId - Contact ID
 * @returns Promise that resolves when contact is deleted
 */
export const deleteBuildingContact = async (buildingId: string, contactId: string): Promise<void> => {
  await apiClient.delete(`/buildings/${buildingId}/contacts/${contactId}`);
};

/**
 * Building Rules
 */

/**
 * Get building rules
 * @param buildingId - Building ID
 * @returns Building rules
 */
export const getBuildingRules = async (buildingId: string): Promise<BuildingRules> => {
  const response = await apiClient.get<BuildingRules>(`/buildings/${buildingId}/rules`);
  return response;
};

/**
 * Save building rules (create or update)
 * @param buildingId - Building ID
 * @param rulesData - Rules data
 * @returns Saved rules
 */
export const saveBuildingRules = async (
  buildingId: string,
  rulesData: SaveBuildingRulesRequest
): Promise<BuildingRules> => {
  const response = await apiClient.put<BuildingRules>(`/buildings/${buildingId}/rules`, rulesData);
  return response;
};

/**
 * Buildings - Import/Export
 */

/**
 * Export buildings to CSV file
 * @param filters - Optional filters
 */
export const exportBuildings = async (filters: BuildingFilters = {}): Promise<void> => {
  const token = getAuthToken();
  const params = new URLSearchParams(filters as any).toString();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/buildings/export${params ? '?' + params : ''}`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'buildings.csv';
  
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
 * Download buildings CSV template
 */
export const downloadBuildingsTemplate = async (): Promise<void> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/buildings/template`;
  
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
  a.download = 'buildings-template.csv';
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Import buildings from CSV file
 * @param file - CSV file to import
 * @returns Import result
 */
export const importBuildings = async (file: File): Promise<ImportResult> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/buildings/import`;
  
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

/**
 * Get similar buildings that could be merge targets for a given building (admin merge tool).
 */
export const getMergeCandidates = async (buildingId: string): Promise<BuildingListItem[]> => {
  const response = await apiClient.get<BuildingListItem[] | { data: BuildingListItem[] }>(
    '/buildings/merge-candidates',
    { params: { buildingId } }
  );
  if (Array.isArray(response)) return response;
  return (response as { data: BuildingListItem[] }).data ?? [];
};

/**
 * Preview merge: how many orders and drafts would be reassigned from source to target.
 */
export const getMergePreview = async (
  sourceBuildingId: string,
  targetBuildingId: string
): Promise<BuildingMergePreview> => {
  const response = await apiClient.get<BuildingMergePreview | { data: BuildingMergePreview }>(
    '/buildings/merge-preview',
    { params: { sourceBuildingId, targetBuildingId } }
  );
  if (response && typeof (response as BuildingMergePreview).sourceBuildingId === 'string')
    return response as BuildingMergePreview;
  return (response as { data: BuildingMergePreview }).data;
};

/**
 * Merge source building into target: reassign orders and drafts to target, then soft-delete source.
 */
export const mergeBuildings = async (
  request: MergeBuildingsRequest
): Promise<BuildingMergeResult> => {
  const response = await apiClient.post<BuildingMergeResult | { data: BuildingMergeResult }>(
    '/buildings/merge',
    request
  );
  if (response && typeof (response as BuildingMergeResult).message === 'string')
    return response as BuildingMergeResult;
  return (response as { data: BuildingMergeResult }).data;
};

