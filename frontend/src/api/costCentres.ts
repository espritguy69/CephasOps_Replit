import apiClient from './client';

export const getCostCentres = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/cost-centres', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

