import apiClient from './client';

export const getServicePlans = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/service-plans', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const createServicePlan = async (data: any) => {
  const response = await apiClient.post('/service-plans', data);
  return response?.data?.data || response?.data || response;
};

export const updateServicePlan = async (id: string, data: any) => {
  const response = await apiClient.put(`/service-plans/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteServicePlan = async (id: string) => {
  await apiClient.delete(`/service-plans/${id}`);
};