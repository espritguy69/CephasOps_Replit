/**
 * SLA Intelligence API — breaches, dashboard, rules.
 */
import apiClient from './client';

export interface SlaBreachDto {
  id: string;
  companyId: string | null;
  ruleId: string;
  targetType: string;
  targetId: string;
  correlationId: string | null;
  detectedAtUtc: string;
  durationSeconds: number;
  severity: string;
  status: string;
  title: string | null;
  acknowledgedAtUtc: string | null;
  resolvedAtUtc: string | null;
}

export interface SlaDashboardDto {
  openBreachesCount: number;
  criticalBreachesCount: number;
  averageResolutionTimeHours: number | null;
  mostCommonBreachedTargets: { targetType: string; targetName: string; count: number }[];
}

export interface SlaRuleDto {
  id: string;
  companyId: string | null;
  ruleType: string;
  targetType: string;
  targetName: string;
  maxDurationSeconds: number;
  warningThresholdSeconds: number | null;
  escalationThresholdSeconds: number | null;
  enabled: boolean;
  createdAtUtc: string;
}

export interface CreateSlaRuleDto {
  companyId?: string | null;
  ruleType: string;
  targetType: string;
  targetName: string;
  maxDurationSeconds: number;
  warningThresholdSeconds?: number | null;
  escalationThresholdSeconds?: number | null;
  enabled?: boolean;
}

export interface UpdateSlaRuleDto {
  ruleType?: string;
  targetType?: string;
  targetName?: string;
  maxDurationSeconds?: number;
  warningThresholdSeconds?: number | null;
  escalationThresholdSeconds?: number | null;
  enabled?: boolean;
}

export async function getSlaBreaches(params: {
  companyId?: string;
  targetType?: string;
  severity?: string;
  status?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}): Promise<{ items: SlaBreachDto[]; total: number; page: number; pageSize: number }> {
  const response = await apiClient.get<{ items: SlaBreachDto[]; total: number; page: number; pageSize: number }>(
    '/sla/breaches',
    { params: params as Record<string, string | number | undefined> }
  );
  return response as { items: SlaBreachDto[]; total: number; page: number; pageSize: number };
}

export async function getSlaDashboard(): Promise<SlaDashboardDto> {
  const response = await apiClient.get<SlaDashboardDto>('/sla/dashboard');
  return response as SlaDashboardDto;
}

export async function getSlaRules(params?: { enabled?: boolean; ruleType?: string }): Promise<{ items: SlaRuleDto[] }> {
  const response = await apiClient.get<{ items: SlaRuleDto[] }>('/sla/rules', {
    params: params as Record<string, string | boolean | undefined>
  });
  return response as { items: SlaRuleDto[] };
}

export async function createSlaRule(dto: CreateSlaRuleDto): Promise<SlaRuleDto> {
  const response = await apiClient.post<SlaRuleDto>('/sla/rules', dto);
  return response as SlaRuleDto;
}

export async function updateSlaRule(id: string, dto: UpdateSlaRuleDto): Promise<SlaRuleDto> {
  const response = await apiClient.put<SlaRuleDto>(`/sla/rules/${id}`, dto);
  return response as SlaRuleDto;
}

export async function updateSlaBreachStatus(
  id: string,
  dto: { status: string; resolvedByUserId?: string | null }
): Promise<void> {
  await apiClient.patch(`/sla/breaches/${id}`, dto);
}
