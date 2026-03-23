import apiClient from './client';

export interface EscalationRule {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  entityType: string; // Order, Task, Invoice
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  
  // Trigger
  triggerType: string; // TimeBased, StatusBased, ConditionBased, EventBased
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  triggerConditionsJson?: string;
  
  // Escalation
  escalationType: string; // NotifyUser, NotifyRole, AssignToUser, AssignToRole, ChangeStatus, CreateTask
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  escalationMessage?: string;
  
  // Escalation Chain
  continueEscalation: boolean;
  nextEscalationRuleId?: string;
  nextEscalationDelayMinutes?: number;
  
  // Status
  priority: number;
  isActive: boolean;
  stopOnMatch: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEscalationRuleDto {
  name: string;
  description?: string;
  entityType: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
  triggerType: string;
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  triggerConditionsJson?: string;
  escalationType: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  escalationMessage?: string;
  continueEscalation: boolean;
  nextEscalationRuleId?: string;
  nextEscalationDelayMinutes?: number;
  priority: number;
  isActive: boolean;
  stopOnMatch: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export interface UpdateEscalationRuleDto {
  name?: string;
  description?: string;
  triggerType?: string;
  triggerStatus?: string;
  triggerDelayMinutes?: number;
  triggerConditionsJson?: string;
  escalationType?: string;
  targetUserId?: string;
  targetRole?: string;
  targetTeamId?: string;
  targetStatus?: string;
  notificationTemplateId?: string;
  escalationMessage?: string;
  continueEscalation?: boolean;
  nextEscalationRuleId?: string;
  nextEscalationDelayMinutes?: number;
  priority?: number;
  isActive?: boolean;
  stopOnMatch?: boolean;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export async function getEscalationRules(params?: {
  entityType?: string;
  triggerType?: string;
  isActive?: boolean;
}): Promise<EscalationRule[]> {
  const response = await apiClient.get<EscalationRule[]>('/escalation-rules', { params });
  return response.data;
}

export async function getEscalationRule(id: string): Promise<EscalationRule> {
  const response = await apiClient.get<EscalationRule>(`/escalation-rules/${id}`);
  return response.data;
}

export async function getApplicableEscalationRules(params: {
  entityType: string;
  currentStatus?: string;
  partnerId?: string;
  departmentId?: string;
  orderType?: string;
}): Promise<EscalationRule[]> {
  const response = await apiClient.get<EscalationRule[]>('/escalation-rules/applicable', { params });
  return response.data;
}

export async function createEscalationRule(data: CreateEscalationRuleDto): Promise<EscalationRule> {
  const response = await apiClient.post<EscalationRule>('/escalation-rules', data);
  return response.data;
}

export async function updateEscalationRule(id: string, data: UpdateEscalationRuleDto): Promise<EscalationRule> {
  const response = await apiClient.put<EscalationRule>(`/escalation-rules/${id}`, data);
  return response.data;
}

export async function deleteEscalationRule(id: string): Promise<void> {
  await apiClient.delete(`/escalation-rules/${id}`);
}

export async function toggleEscalationRuleActive(id: string): Promise<EscalationRule> {
  const response = await apiClient.post<EscalationRule>(`/escalation-rules/${id}/toggle-active`);
  return response.data;
}

