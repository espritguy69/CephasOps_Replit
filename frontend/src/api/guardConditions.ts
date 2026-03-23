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

export interface CreateGuardConditionDefinition {
  key: string;
  name: string;
  description?: string;
  entityType: string;
  validatorType: string;
  validatorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

export interface UpdateGuardConditionDefinition {
  name?: string;
  description?: string;
  validatorType?: string;
  validatorConfigJson?: string;
  isActive?: boolean;
  displayOrder?: number;
}

/**
 * Get all guard condition definitions
 */
export async function getGuardConditionDefinitions(params?: {
  entityType?: string;
  isActive?: boolean;
}): Promise<GuardConditionDefinition[]> {
  const response = await apiClient.get('/workflow/guard-conditions', { params });
  return response.data.data || response.data;
}

/**
 * Get guard condition definition by ID
 */
export async function getGuardConditionDefinition(id: string): Promise<GuardConditionDefinition> {
  const response = await apiClient.get(`/workflow/guard-conditions/${id}`);
  return response.data.data || response.data;
}

/**
 * Create a new guard condition definition
 */
export async function createGuardConditionDefinition(
  data: CreateGuardConditionDefinition
): Promise<GuardConditionDefinition> {
  const response = await apiClient.post('/workflow/guard-conditions', data);
  return response.data.data || response.data;
}

/**
 * Update a guard condition definition
 */
export async function updateGuardConditionDefinition(
  id: string,
  data: UpdateGuardConditionDefinition
): Promise<GuardConditionDefinition> {
  const response = await apiClient.put(`/workflow/guard-conditions/${id}`, data);
  return response.data.data || response.data;
}

/**
 * Delete a guard condition definition
 */
export async function deleteGuardConditionDefinition(id: string): Promise<void> {
  await apiClient.delete(`/workflow/guard-conditions/${id}`);
}

