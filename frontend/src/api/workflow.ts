import apiClient from './client';
import type {
  ExecuteTransitionRequest,
  WorkflowJob,
  AllowedTransition,
  TransitionParams,
  CanTransitionParams,
  WorkflowJobFilters
} from '../types/workflow';

/**
 * Workflow Engine API
 * Handles workflow execution and job management
 */

/**
 * Execute a workflow transition for an entity
 * @param executeDto - Execute transition DTO (entityId, entityType, targetStatus, payload)
 * @returns Workflow job result
 */
export const executeTransition = async (executeDto: ExecuteTransitionRequest): Promise<WorkflowJob> => {
  const response = await apiClient.post<WorkflowJob>('/workflow/execute', executeDto);
  return response;
};

/**
 * Get allowed transitions for an entity in its current status
 * @param params - Query parameters (entityType, entityId, currentStatus)
 * @returns Array of allowed transitions
 */
export const getAllowedTransitions = async (params: TransitionParams): Promise<AllowedTransition[]> => {
  const response = await apiClient.get<AllowedTransition[]>('/workflow/allowed-transitions', { params });
  return response;
};

/**
 * Check if a transition is allowed
 * @param params - Query parameters (entityType, entityId, fromStatus, toStatus)
 * @returns Whether transition is allowed
 */
export const canTransition = async (params: CanTransitionParams): Promise<boolean> => {
  const response = await apiClient.get<boolean>('/workflow/can-transition', { params });
  return response;
};

/**
 * Get workflow job by ID
 * @param jobId - Workflow job ID
 * @returns Workflow job details
 */
export const getWorkflowJob = async (jobId: string): Promise<WorkflowJob> => {
  const response = await apiClient.get<WorkflowJob>(`/workflow/jobs/${jobId}`);
  return response;
};

/**
 * Get workflow jobs for an entity
 * @param params - Query parameters (entityType, entityId, state)
 * @returns Array of workflow jobs
 */
export const getWorkflowJobs = async (params: WorkflowJobFilters = {}): Promise<WorkflowJob[]> => {
  const response = await apiClient.get<WorkflowJob[]>('/workflow/jobs', { params });
  return response;
};

