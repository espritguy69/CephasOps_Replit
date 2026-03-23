import apiClient from './client';

export interface GuardConditionDefinition {
  id: string;
  companyId: string;
  key: string;
  name: string;
  description?: string;
  entityType: string;
  validatorType: string;
  validatorConfigJson?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateGuardConditionDefinitionDto {
  key: string;
  name: string;
  description?: string;
  entityType: string;
  validatorType: string;
  validatorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateGuardConditionDefinitionDto {
  name?: string;
  description?: string;
  validatorType?: string;
  validatorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export async function getGuardConditionDefinitions(params?: {
  entityType?: string;
  isActive?: boolean;
}): Promise<GuardConditionDefinition[]> {
  const response = await apiClient.get<GuardConditionDefinition[]>('/workflow/guard-conditions', { params });
  return response.data;
}

export async function getGuardConditionDefinition(id: string): Promise<GuardConditionDefinition> {
  const response = await apiClient.get<GuardConditionDefinition>(`/workflow/guard-conditions/${id}`);
  return response.data;
}

export async function createGuardConditionDefinition(data: CreateGuardConditionDefinitionDto): Promise<GuardConditionDefinition> {
  const response = await apiClient.post<GuardConditionDefinition>('/workflow/guard-conditions', data);
  return response.data;
}

export async function updateGuardConditionDefinition(id: string, data: UpdateGuardConditionDefinitionDto): Promise<GuardConditionDefinition> {
  const response = await apiClient.put<GuardConditionDefinition>(`/workflow/guard-conditions/${id}`, data);
  return response.data;
}

export async function deleteGuardConditionDefinition(id: string): Promise<void> {
  await apiClient.delete(`/workflow/guard-conditions/${id}`);
}

