/**
 * Workflow Types - Shared type definitions for Workflow module
 */

export interface ExecuteTransitionRequest {
  entityId: string;
  entityType: string;
  targetStatus: string;
  payload?: Record<string, any>;
}

export interface WorkflowJob {
  id: string;
  entityId: string;
  entityType: string;
  fromStatus: string;
  toStatus: string;
  state: 'Pending' | 'Running' | 'Completed' | 'Failed';
  result?: any;
  error?: string;
  startedAt?: string;
  completedAt?: string;
  createdAt: string;
}

export interface AllowedTransition {
  fromStatus: string;
  toStatus: string;
  label?: string;
  requiresApproval?: boolean;
  conditions?: string[];
}

export interface TransitionParams {
  entityType: string;
  entityId: string;
  currentStatus?: string;
}

export interface CanTransitionParams {
  entityType: string;
  entityId: string;
  fromStatus: string;
  toStatus: string;
}

export interface WorkflowJobFilters {
  entityType?: string;
  entityId?: string;
  state?: string;
}

