import apiClient from './client';

export interface ApprovalWorkflow {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  workflowType: string; // RescheduleApproval, RmaApproval, InvoiceApproval, SplitterOverrideApproval, Custom
  entityType: string; // Order, Invoice, RMA
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  minValueThreshold?: number;
  requireAllSteps: boolean;
  allowParallelApproval: boolean;
  timeoutMinutes?: number;
  autoApproveOnTimeout: boolean;
  escalationRole?: string;
  escalationUserId?: string;
  isActive: boolean;
  isDefault: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
  updatedAt: string;
  steps: ApprovalStep[];
}

export interface ApprovalStep {
  id: string;
  approvalWorkflowId: string;
  name: string;
  stepOrder: number;
  approvalType: string; // User, Role, Team, External
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  externalSource?: string;
  isRequired: boolean;
  canSkipIfPreviousApproved: boolean;
  timeoutMinutes?: number;
  autoApproveOnTimeout: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateApprovalWorkflowDto {
  name: string;
  description?: string;
  workflowType: string;
  entityType: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  minValueThreshold?: number;
  requireAllSteps: boolean;
  allowParallelApproval: boolean;
  timeoutMinutes?: number;
  autoApproveOnTimeout: boolean;
  escalationRole?: string;
  escalationUserId?: string;
  isActive: boolean;
  isDefault: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  steps: CreateApprovalStepDto[];
}

export interface CreateApprovalStepDto {
  name: string;
  stepOrder: number;
  approvalType: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  externalSource?: string;
  isRequired: boolean;
  canSkipIfPreviousApproved: boolean;
  timeoutMinutes?: number;
  autoApproveOnTimeout: boolean;
  isActive: boolean;
}

export interface UpdateApprovalWorkflowDto {
  name?: string;
  description?: string;
  minValueThreshold?: number;
  requireAllSteps?: boolean;
  allowParallelApproval?: boolean;
  timeoutMinutes?: number;
  autoApproveOnTimeout?: boolean;
  escalationRole?: string;
  escalationUserId?: string;
  isActive?: boolean;
  isDefault?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export async function getApprovalWorkflows(params?: {
  workflowType?: string;
  entityType?: string;
  isActive?: boolean;
}): Promise<ApprovalWorkflow[]> {
  const response = await apiClient.get<ApprovalWorkflow[]>('/approval-workflows', { params });
  return response.data;
}

export async function getApprovalWorkflow(id: string): Promise<ApprovalWorkflow> {
  const response = await apiClient.get<ApprovalWorkflow>(`/approval-workflows/${id}`);
  return response.data;
}

export async function getEffectiveApprovalWorkflow(params: {
  workflowType: string;
  entityType: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  value?: number;
}): Promise<ApprovalWorkflow> {
  const response = await apiClient.get<ApprovalWorkflow>('/approval-workflows/effective', { params });
  return response.data;
}

export async function createApprovalWorkflow(data: CreateApprovalWorkflowDto): Promise<ApprovalWorkflow> {
  const response = await apiClient.post<ApprovalWorkflow>('/approval-workflows', data);
  return response.data;
}

export async function updateApprovalWorkflow(id: string, data: UpdateApprovalWorkflowDto): Promise<ApprovalWorkflow> {
  const response = await apiClient.put<ApprovalWorkflow>(`/approval-workflows/${id}`, data);
  return response.data;
}

export async function deleteApprovalWorkflow(id: string): Promise<void> {
  await apiClient.delete(`/approval-workflows/${id}`);
}

export async function setApprovalWorkflowAsDefault(id: string): Promise<ApprovalWorkflow> {
  const response = await apiClient.post<ApprovalWorkflow>(`/approval-workflows/${id}/set-default`);
  return response.data;
}

