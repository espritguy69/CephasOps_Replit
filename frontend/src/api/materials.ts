import apiClient from './client';
import type { Material } from '../types/materials';

/**
 * Materials API Module
 * Handles all material-related API calls
 */

export interface GetMaterialsParams {
  isActive?: boolean;
  categoryId?: string;
  isSerialised?: boolean;
  search?: string;
}

export const getMaterials = async (params?: GetMaterialsParams): Promise<Material[]> => {
  const response = await apiClient.get('/materials', { params });
  return response.data.data || [];
};

export const getMaterial = async (materialId: string): Promise<Material> => {
  const response = await apiClient.get(`/materials/${materialId}`);
  return response.data.data;
};

export const createMaterial = async (data: Partial<Material>): Promise<Material> => {
  const response = await apiClient.post('/materials', data);
  return response.data.data;
};

export const updateMaterial = async (materialId: string, data: Partial<Material>): Promise<Material> => {
  const response = await apiClient.put(`/materials/${materialId}`, data);
  return response.data.data;
};

export const deleteMaterial = async (materialId: string): Promise<void> => {
  await apiClient.delete(`/materials/${materialId}`);
};

