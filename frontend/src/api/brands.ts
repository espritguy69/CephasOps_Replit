import apiClient from './client';

export const getBrands = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/brands', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const createBrand = async (data: any) => {
  const response = await apiClient.post('/brands', data);
  return response?.data?.data || response?.data || response;
};

export const updateBrand = async (id: string, data: any) => {
  const response = await apiClient.put(`/brands/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteBrand = async (id: string) => {
  await apiClient.delete(`/brands/${id}`);
};