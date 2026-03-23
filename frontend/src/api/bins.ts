import apiClient from './client';

export const getBins = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/bins', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const createBin = async (data: any) => {
  const response = await apiClient.post('/bins', data);
  return response?.data?.data || response?.data || response;
};

export const updateBin = async (id: string, data: any) => {
  const response = await apiClient.put(`/bins/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteBin = async (id: string) => {
  await apiClient.delete(`/bins/${id}`);
};