import apiClient from './client';

export const getReportDefinitions = async (params?: any) => {
  const response = await apiClient.get('/settings/report-definitions', { params });
  return response.data.data || [];
};

export const getReportDefinition = async (id: string) => {
  const response = await apiClient.get(`/settings/report-definitions/${id}`);
  return response.data.data;
};

export const createReportDefinition = async (data: any) => {
  const response = await apiClient.post('/settings/report-definitions', data);
  return response.data.data;
};

export const updateReportDefinition = async (id: string, data: any) => {
  const response = await apiClient.put(`/settings/report-definitions/${id}`, data);
  return response.data.data;
};

export const deleteReportDefinition = async (id: string) => {
  await apiClient.delete(`/settings/report-definitions/${id}`);
};

