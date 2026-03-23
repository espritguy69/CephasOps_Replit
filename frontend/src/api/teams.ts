import apiClient from './client';

export const getTeams = async (companyId: string, isActive?: boolean) => {
  const response = await apiClient.get('/teams', { params: { companyId, isActive } });
  return Array.isArray(response) ? response : (response?.data || []);
};

export const createTeam = async (data: any) => {
  const response = await apiClient.post('/teams', data);
  return response?.data?.data || response?.data || response;
};

export const updateTeam = async (id: string, data: any) => {
  const response = await apiClient.put(`/teams/${id}`, data);
  return response?.data?.data || response?.data || response;
};

export const deleteTeam = async (id: string) => {
  await apiClient.delete(`/teams/${id}`);
};