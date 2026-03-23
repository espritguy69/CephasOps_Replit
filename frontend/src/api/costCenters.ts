import apiClient from './client';

export const getCostCenters = async (params?: any) => {
  const response = await apiClient.get('/settings/cost-centers', { params });
  return response.data.data || [];
};

export const getCostCenter = async (id: string) => {
  const response = await apiClient.get(`/settings/cost-centers/${id}`);
  return response.data.data;
};

export const createCostCenter = async (data: any) => {
  const response = await apiClient.post('/settings/cost-centers', data);
  return response.data.data;
};

export const updateCostCenter = async (id: string, data: any) => {
  const response = await apiClient.put(`/settings/cost-centers/${id}`, data);
  return response.data.data;
};

export const deleteCostCenter = async (id: string) => {
  await apiClient.delete(`/settings/cost-centers/${id}`);
};

