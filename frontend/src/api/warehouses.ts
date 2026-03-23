import apiClient from './client';

export const getWarehouses = async (params?: any) => {
  const response = await apiClient.get('/warehouses', { params });
  return Array.isArray(response) ? response : (response?.data?.data || response?.data || []);
};

export const getWarehouse = async (id: string) => {
  const response = await apiClient.get(`/warehouses/${id}`);
  return response?.data?.data || response?.data || response;
};

export const createWarehouse = async (data: any) => {
  const response = await apiClient.post('/warehouses', data);
  return response?.data?.data || response?.data || response;
};

export const updateWarehouse = async (id: string, data: any) => {
  const response = await apiClient.put(`/warehouses/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteWarehouse = async (id: string) => {
  await apiClient.delete(`/warehouses/${id}`);
};

