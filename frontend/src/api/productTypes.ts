import apiClient from './client';

export const getProductTypes = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/product-types', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const createProductType = async (data: any) => {
  const response = await apiClient.post('/product-types', data);
  return response?.data?.data || response?.data || response;
};

export const updateProductType = async (id: string, data: any) => {
  const response = await apiClient.put(`/product-types/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteProductType = async (id: string) => {
  await apiClient.delete(`/product-types/${id}`);
};