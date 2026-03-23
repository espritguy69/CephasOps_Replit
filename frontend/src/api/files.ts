import apiClient from './client';
import { getApiBaseUrl } from './config';
import type { FileMetadata, UploadFileMetadata, FileFilters } from '../types/files';

/**
 * Files API
 * Handles file upload, download, and metadata management
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Upload a file
 * @param file - File to upload
 * @param metadata - Optional metadata (module, entityId, entityType)
 * @returns File metadata
 */
export const uploadFile = async (file: File, metadata: UploadFileMetadata = {}): Promise<FileMetadata> => {
  const formData = new FormData();
  formData.append('file', file);
  if (metadata.module) formData.append('module', metadata.module);
  if (metadata.entityId) formData.append('entityId', metadata.entityId);
  if (metadata.entityType) formData.append('entityType', metadata.entityType);

  const apiBaseUrl = getApiBaseUrl();
  const queryParams = new URLSearchParams();
  if (metadata.module) queryParams.append('module', metadata.module);
  if (metadata.entityId) queryParams.append('entityId', metadata.entityId);
  if (metadata.entityType) queryParams.append('entityType', metadata.entityType);

  const response = await fetch(`${apiBaseUrl}/files/upload?${queryParams.toString()}`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${getAuthToken()}`
    },
    body: formData
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }

  return response.json();
};

/**
 * Download a file
 * @param fileId - File ID
 * @returns File blob
 */
export const downloadFile = async (fileId: string): Promise<Blob> => {
  const apiBaseUrl = getApiBaseUrl();
  const response = await fetch(`${apiBaseUrl}/files/${fileId}/download`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${getAuthToken()}`
    }
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.status} ${response.statusText}`);
  }

  return response.blob();
};

/**
 * Get file metadata
 * @param fileId - File ID
 * @returns File metadata
 */
export const getFileMetadata = async (fileId: string): Promise<FileMetadata> => {
  const response = await apiClient.get<FileMetadata>(`/files/${fileId}/metadata`);
  return response;
};

/**
 * Get file by ID (download)
 * @param fileId - File ID
 * @returns File data
 */
export const getFile = async (fileId: string): Promise<FileMetadata> => {
  const response = await apiClient.get<FileMetadata>(`/files/${fileId}`);
  return response;
};

/**
 * Delete a file
 * @param fileId - File ID
 * @returns Promise that resolves when file is deleted
 */
export const deleteFile = async (fileId: string): Promise<void> => {
  await apiClient.delete(`/files/${fileId}`);
};

/**
 * Get files list
 * @param filters - Optional filters (module, entityId, entityType)
 * @returns Array of file metadata
 */
export const getFiles = async (filters: FileFilters = {}): Promise<FileMetadata[]> => {
  const response = await apiClient.get<FileMetadata[]>(`/files`, { params: filters });
  return response;
};

