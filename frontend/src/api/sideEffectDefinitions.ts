import apiClient from './client';

export interface SideEffectDefinition {
  id: string;
  companyId: string;
  key: string;
  name: string;
  description?: string;
  entityType: string;
  executorType: string;
  executorConfigJson?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSideEffectDefinitionDto {
  key: string;
  name: string;
  description?: string;
  entityType: string;
  executorType: string;
  executorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateSideEffectDefinitionDto {
  name?: string;
  description?: string;
  executorType?: string;
  executorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export async function getSideEffectDefinitions(params?: {
  entityType?: string;
  isActive?: boolean;
}): Promise<SideEffectDefinition[]> {
  const response = await apiClient.get<SideEffectDefinition[]>('/workflow/side-effects', { params });
  return response.data;
}

export async function getSideEffectDefinition(id: string): Promise<SideEffectDefinition> {
  const response = await apiClient.get<SideEffectDefinition>(`/workflow/side-effects/${id}`);
  return response.data;
}

export async function createSideEffectDefinition(data: CreateSideEffectDefinitionDto): Promise<SideEffectDefinition> {
  const response = await apiClient.post<SideEffectDefinition>('/workflow/side-effects', data);
  return response.data;
}

export async function updateSideEffectDefinition(id: string, data: UpdateSideEffectDefinitionDto): Promise<SideEffectDefinition> {
  const response = await apiClient.put<SideEffectDefinition>(`/workflow/side-effects/${id}`, data);
  return response.data;
}

export async function deleteSideEffectDefinition(id: string): Promise<void> {
  await apiClient.delete(`/workflow/side-effects/${id}`);
}

