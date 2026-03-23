import apiClient from './client';

export interface AutomationRule {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  ruleType: string; // AutoAssignment, AutoEscalation, AutoNotification, AutoStatusChange
  entityType: string; // Order, Task, Invoice
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  
  // Trigger
  triggerType: string; // StatusChange, TimeBased, ConditionBased, EventBased
  triggerConditionsJson?: string;
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  
  // Action
  actionType: string; // AssignToUser, AssignToRole, AssignToTeam, Escalate, Notify, ChangeStatus, CreateTask
  actionConfigJson?: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  
  // Conditions
  conditionsJson?: string;
  priority: number;
  isActive: boolean;
  stopOnMatch: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAutomationRuleDto {
  name: string;
  description?: string;
  ruleType: string;
  entityType: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  triggerType: string;
  triggerConditionsJson?: string;
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  actionType: string;
  actionConfigJson?: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  conditionsJson?: string;
  priority: number;
  isActive: boolean;
  stopOnMatch: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateAutomationRuleDto {
  name?: string;
  description?: string;
  triggerType?: string;
  triggerConditionsJson?: string;
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  actionType?: string;
  actionConfigJson?: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  conditionsJson?: string;
  priority?: number;
  isActive?: boolean;
  stopOnMatch?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export async function getAutomationRules(params?: {
  ruleType?: string;
  entityType?: string;
  isActive?: boolean;
}): Promise<AutomationRule[]> {
  const response = await apiClient.get<AutomationRule[]>('/automation-rules', { params });
  return response.data;
}

export async function getAutomationRule(id: string): Promise<AutomationRule> {
  const response = await apiClient.get<AutomationRule>(`/automation-rules/${id}`);
  return response.data;
}

export async function getApplicableAutomationRules(params: {
  entityType: string;
  currentStatus?: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
}): Promise<AutomationRule[]> {
  const response = await apiClient.get<AutomationRule[]>('/automation-rules/applicable', { params });
  return response.data;
}

export async function createAutomationRule(data: CreateAutomationRuleDto): Promise<AutomationRule> {
  const response = await apiClient.post<AutomationRule>('/automation-rules', data);
  return response.data;
}

export async function updateAutomationRule(id: string, data: UpdateAutomationRuleDto): Promise<AutomationRule> {
  const response = await apiClient.put<AutomationRule>(`/automation-rules/${id}`, data);
  return response.data;
}

export async function deleteAutomationRule(id: string): Promise<void> {
  await apiClient.delete(`/automation-rules/${id}`);
}

export async function toggleAutomationRuleActive(id: string): Promise<AutomationRule> {
  const response = await apiClient.post<AutomationRule>(`/automation-rules/${id}/toggle-active`);
  return response.data;
}

