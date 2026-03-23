import apiClient from './client';

export interface SlaProfile {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  partnerId?: string;
  orderType: string;
  departmentId?: string;
  isVipOnly: boolean;
  responseSlaMinutes?: number;
  responseSlaFromStatus?: string;
  responseSlaToStatus?: string;
  resolutionSlaMinutes?: number;
  resolutionSlaFromStatus?: string;
  resolutionSlaToStatus?: string;
  escalationThresholdPercent?: number;
  escalationRole?: string;
  escalationUserId?: string;
  notifyOnEscalation: boolean;
  notifyOnBreach: boolean;
  excludeNonBusinessHours: boolean;
  excludeWeekends: boolean;
  excludePublicHolidays: boolean;
  isDefault: boolean;
  isActive: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSlaProfileDto {
  name: string;
  description?: string;
  partnerId?: string;
  orderType: string;
  departmentId?: string;
  isVipOnly: boolean;
  responseSlaMinutes?: number;
  responseSlaFromStatus?: string;
  responseSlaToStatus?: string;
  resolutionSlaMinutes?: number;
  resolutionSlaFromStatus?: string;
  resolutionSlaToStatus?: string;
  escalationThresholdPercent?: number;
  escalationRole?: string;
  escalationUserId?: string;
  notifyOnEscalation: boolean;
  notifyOnBreach: boolean;
  excludeNonBusinessHours: boolean;
  excludeWeekends: boolean;
  excludePublicHolidays: boolean;
  isDefault: boolean;
  isActive: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateSlaProfileDto {
  name?: string;
  description?: string;
  responseSlaMinutes?: number;
  responseSlaFromStatus?: string;
  responseSlaToStatus?: string;
  resolutionSlaMinutes?: number;
  resolutionSlaFromStatus?: string;
  resolutionSlaToStatus?: string;
  escalationThresholdPercent?: number;
  escalationRole?: string;
  escalationUserId?: string;
  notifyOnEscalation?: boolean;
  notifyOnBreach?: boolean;
  excludeNonBusinessHours?: boolean;
  excludeWeekends?: boolean;
  excludePublicHolidays?: boolean;
  isDefault?: boolean;
  isActive?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export async function getSlaProfiles(params?: {
  orderType?: string;
  partnerId?: string;
  departmentId?: string;
  isActive?: boolean;
}): Promise<SlaProfile[]> {
  const response = await apiClient.get<SlaProfile[]>('/sla-profiles', { params });
  return response.data;
}

export async function getSlaProfile(id: string): Promise<SlaProfile> {
  const response = await apiClient.get<SlaProfile>(`/sla-profiles/${id}`);
  return response.data;
}

export async function getEffectiveSlaProfile(params: {
  partnerId?: string;
  orderType: string;
  departmentId?: string;
  isVip?: boolean;
  effectiveDate?: string;
}): Promise<SlaProfile> {
  const response = await apiClient.get<SlaProfile>('/sla-profiles/effective', { params });
  return response.data;
}

export async function createSlaProfile(data: CreateSlaProfileDto): Promise<SlaProfile> {
  const response = await apiClient.post<SlaProfile>('/sla-profiles', data);
  return response.data;
}

export async function updateSlaProfile(id: string, data: UpdateSlaProfileDto): Promise<SlaProfile> {
  const response = await apiClient.put<SlaProfile>(`/sla-profiles/${id}`, data);
  return response.data;
}

export async function deleteSlaProfile(id: string): Promise<void> {
  await apiClient.delete(`/sla-profiles/${id}`);
}

export async function setSlaProfileAsDefault(id: string): Promise<SlaProfile> {
  const response = await apiClient.post<SlaProfile>(`/sla-profiles/${id}/set-default`);
  return response.data;
}

