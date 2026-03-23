import apiClient from './client';
import type {
  WorkflowDefinition,
  WorkflowTransition,
  CreateWorkflowDefinitionRequest,
  UpdateWorkflowDefinitionRequest,
  CreateTransitionRequest,
  UpdateTransitionRequest,
  WorkflowDefinitionFilters,
  EffectiveWorkflowParams
} from '../types/workflowDefinitions';

/**
 * Workflow Definitions API
 * Handles workflow definition and transition management
 */

/**
 * Get all workflow definitions
 * @param filters - Optional filters (entityType, isActive)
 * @returns Array of workflow definitions
 */
export const getWorkflowDefinitions = async (filters: WorkflowDefinitionFilters = {}): Promise<WorkflowDefinition[]> => {
  const response = await apiClient.get<WorkflowDefinition[]>('/workflow-definitions', { params: filters });
  return response;
};

/**
 * Get workflow definition by ID
 * @param definitionId - Workflow definition ID
 * @returns Workflow definition details
 */
export const getWorkflowDefinition = async (definitionId: string): Promise<WorkflowDefinition> => {
  const response = await apiClient.get<WorkflowDefinition>(`/workflow-definitions/${definitionId}`);
  return response;
};

/**
 * Get effective workflow definition for an entity type
 * @param params - Query parameters (entityType, partnerId, departmentId, orderTypeCode)
 * @returns Effective workflow definition
 */
export const getEffectiveWorkflowDefinition = async (params: EffectiveWorkflowParams): Promise<WorkflowDefinition> => {
  const response = await apiClient.get<WorkflowDefinition>('/workflow-definitions/effective', { params });
  return response;
};

/**
 * Create a new workflow definition
 * @param definitionData - Workflow definition creation data
 * @returns Created workflow definition
 */
export const createWorkflowDefinition = async (
  definitionData: CreateWorkflowDefinitionRequest
): Promise<WorkflowDefinition> => {
  const response = await apiClient.post<WorkflowDefinition>('/workflow-definitions', definitionData);
  return response;
};

/**
 * Update an existing workflow definition
 * @param definitionId - Workflow definition ID
 * @param definitionData - Workflow definition update data
 * @returns Updated workflow definition
 */
export const updateWorkflowDefinition = async (
  definitionId: string,
  definitionData: UpdateWorkflowDefinitionRequest
): Promise<WorkflowDefinition> => {
  const response = await apiClient.put<WorkflowDefinition>(`/workflow-definitions/${definitionId}`, definitionData);
  return response;
};

/**
 * Delete a workflow definition
 * @param definitionId - Workflow definition ID
 * @returns Promise that resolves when workflow definition is deleted
 */
export const deleteWorkflowDefinition = async (definitionId: string): Promise<void> => {
  await apiClient.delete(`/workflow-definitions/${definitionId}`);
};

/**
 * Get transitions for a workflow definition
 * @param definitionId - Workflow definition ID
 * @returns Array of workflow transitions
 */
export const getTransitions = async (definitionId: string): Promise<WorkflowTransition[]> => {
  const response = await apiClient.get<WorkflowTransition[]>(`/workflow-definitions/${definitionId}/transitions`);
  return response;
};

/**
 * Add a transition to a workflow definition
 * @param definitionId - Workflow definition ID
 * @param transitionData - Transition creation data
 * @returns Created transition
 */
export const addTransition = async (
  definitionId: string,
  transitionData: CreateTransitionRequest
): Promise<WorkflowTransition> => {
  const response = await apiClient.post<WorkflowTransition>(
    `/workflow-definitions/${definitionId}/transitions`,
    transitionData
  );
  return response;
};

/**
 * Update a workflow transition
 * @param transitionId - Transition ID
 * @param transitionData - Transition update data
 * @returns Updated transition
 */
export const updateTransition = async (
  transitionId: string,
  transitionData: UpdateTransitionRequest
): Promise<WorkflowTransition> => {
  const response = await apiClient.put<WorkflowTransition>(
    `/workflow-definitions/transitions/${transitionId}`,
    transitionData
  );
  return response;
};

/**
 * Delete a workflow transition
 * @param transitionId - Transition ID
 * @returns Promise that resolves when transition is deleted
 */
export const deleteTransition = async (transitionId: string): Promise<void> => {
  await apiClient.delete(`/workflow-definitions/transitions/${transitionId}`);
};

