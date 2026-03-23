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

export interface CreateSideEffectDefinition {
  key: string;
  name: string;
  description?: string;
  entityType: string;
  executorType: string;
  executorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateSideEffectDefinition {
  name?: string;
  description?: string;
  executorType?: string;
  executorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

/**
 * Get all side effect definitions
 */
export async function getSideEffectDefinitions(params?: {
  entityType?: string;
  isActive?: boolean;
}): Promise<SideEffectDefinition[]> {
  const response = await apiClient.get('/workflow/side-effects', { params });
  return response.data.data || response.data;
}

/**
 * Get side effect definition by ID
 */
export async function getSideEffectDefinition(id: string): Promise<SideEffectDefinition> {
  const response = await apiClient.get(`/workflow/side-effects/${id}`);
  return response.data.data || response.data;
}

/**
 * Create a new side effect definition
 */
export async function createSideEffectDefinition(
  data: CreateSideEffectDefinition
): Promise<SideEffectDefinition> {
  const response = await apiClient.post('/workflow/side-effects', data);
  return response.data.data || response.data;
}

/**
 * Update a side effect definition
 */
export async function updateSideEffectDefinition(
  id: string,
  data: UpdateSideEffectDefinition
): Promise<SideEffectDefinition> {
  const response = await apiClient.put(`/workflow/side-effects/${id}`, data);
  return response.data.data || response.data;
}

/**
 * Delete a side effect definition
 */
export async function deleteSideEffectDefinition(id: string): Promise<void> {
  await apiClient.delete(`/workflow/side-effects/${id}`);
}

