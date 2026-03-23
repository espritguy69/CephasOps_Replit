import apiClient from './client';

export const getNotificationTemplates = async (params?: any) => {
  const response = await apiClient.get('/settings/notification-templates', { params });
  return response.data.data || [];
};

export const getNotificationTemplate = async (id: string) => {
  const response = await apiClient.get(`/settings/notification-templates/${id}`);
  return response.data.data;
};

export const createNotificationTemplate = async (data: any) => {
  const response = await apiClient.post('/settings/notification-templates', data);
  return response.data.data;
};

export const updateNotificationTemplate = async (id: string, data: any) => {
  const response = await apiClient.put(`/settings/notification-templates/${id}`, data);
  return response.data.data;
};

export const deleteNotificationTemplate = async (id: string) => {
  await apiClient.delete(`/settings/notification-templates/${id}`);
};

