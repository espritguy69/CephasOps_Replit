/**
 * Workflow Definitions Types - Shared type definitions for Workflow Definitions module
 */

export interface WorkflowTransition {
  id: string;
  companyId?: string;
  workflowDefinitionId: string;
  fromStatus: string;
  toStatus: string;
  label?: string;
  allowedRoles?: string[];
  guardConditions?: Record<string, any>;
  sideEffectsConfig?: Record<string, any>;
  displayOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  createdByUserId?: string;
  updatedByUserId?: string;
}

export interface WorkflowDefinition {
  id: string;
  companyId?: string;
  name: string;
  entityType: string;
  description?: string;
  isActive: boolean;
  isDefault: boolean;
  partnerId?: string;
  partnerName?: string;
  departmentId?: string;
  departmentName?: string;
  orderTypeCode?: string;
  transitions: WorkflowTransition[];
  createdAt?: string;
  updatedAt?: string;
  createdByUserId?: string;
  updatedByUserId?: string;
}

export interface CreateWorkflowDefinitionRequest {
  name: string;
  entityType: string;
  description?: string;
  isActive?: boolean;
  isDefault?: boolean;
  partnerId?: string;
  departmentId?: string;
  orderTypeCode?: string;
}

export interface UpdateWorkflowDefinitionRequest {
  name?: string;
  description?: string;
  entityType?: string;
  isActive?: boolean;
  isDefault?: boolean;
  partnerId?: string;
  departmentId?: string;
  orderTypeCode?: string;
}

export interface CreateTransitionRequest {
  fromStatus?: string;
  toStatus: string;
  label?: string;
  allowedRoles?: string[];
  guardConditions?: Record<string, any>;
  sideEffectsConfig?: Record<string, any>;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateTransitionRequest {
  fromStatus?: string;
  toStatus?: string;
  label?: string;
  allowedRoles?: string[];
  guardConditions?: Record<string, any>;
  sideEffectsConfig?: Record<string, any>;
  displayOrder?: number;
  isActive?: boolean;
}

export interface WorkflowDefinitionFilters {
  entityType?: string;
  isActive?: boolean;
}

export interface EffectiveWorkflowParams {
  entityType: string;
  partnerId?: string;
  departmentId?: string;
  orderTypeCode?: string;
}

