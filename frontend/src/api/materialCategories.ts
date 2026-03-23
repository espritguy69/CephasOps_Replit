import apiClient from './client';

export const getMaterialCategories = async (params?: any) => {
  const response = await apiClient.get('/settings/material-categories', { params });
  return response.data.data || [];
};

export const getMaterialCategory = async (id: string) => {
  const response = await apiClient.get(`/settings/material-categories/${id}`);
  return response.data.data;
};

export const createMaterialCategory = async (data: any) => {
  const response = await apiClient.post('/settings/material-categories', data);
  return response.data.data;
};

export const updateMaterialCategory = async (id: string, data: any) => {
  const response = await apiClient.put(`/settings/material-categories/${id}`, data);
  return response.data.data;
};

export const deleteMaterialCategory = async (id: string) => {
  await apiClient.delete(`/settings/material-categories/${id}`);
};

