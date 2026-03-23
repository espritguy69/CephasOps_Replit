/**
 * SLA Breach Engine API.
 * Tenant-scoped: requires company context (X-Company-Id). Query keys include department for cache invalidation on tenant switch.
 */

import apiClient from './client';

const BASE = 'insights/sla';

export const slaBreachKeys = {
  all: (departmentId?: string | null) => ['sla-breach', departmentId ?? ''] as const,
  summary: (departmentId?: string | null) => [...slaBreachKeys.all(departmentId), 'summary'] as const,
  ordersAtRisk: (departmentId?: string | null, breachState?: string | null, severity?: string | null) =>
    [...slaBreachKeys.all(departmentId), 'orders-at-risk', breachState ?? '', severity ?? ''] as const,
  platformSummary: () => ['sla-breach', 'platform-summary'] as const
};

export interface SlaBreachDistributionDto {
  onTrackCount: number;
  nearingBreachCount: number;
  breachedCount: number;
  noSlaCount: number;
}

export interface SlaBreachSummaryDto {
  distribution: SlaBreachDistributionDto;
  generatedAtUtc: string;
}

export interface SlaBreachOrderItemDto {
  orderId: string;
  orderRef?: string;
  companyId: string;
  currentStatus?: string;
  assignedSiId?: string;
  kpiDueAt?: string;
  nowUtc: string;
  minutesToDueOrOverdue?: number;
  breachState: string;
  severity: string;
  explanation: string;
  relatedSlaProfileId?: string;
  relatedSlaProfileName?: string;
  lastActivityAt?: string;
  hasBlocker: boolean;
  hasReplacement: boolean;
  hasReschedule: boolean;
}

export const SlaBreachState = {
  NoSla: 'NoSla',
  OnTrack: 'OnTrack',
  NearingBreach: 'NearingBreach',
  Breached: 'Breached'
} as const;

export async function getSlaBreachSummary(): Promise<SlaBreachSummaryDto> {
  return apiClient.get<SlaBreachSummaryDto>(`${BASE}/summary`);
}

export async function getSlaOrdersAtRisk(breachState?: string | null, severity?: string | null): Promise<SlaBreachOrderItemDto[]> {
  const params: Record<string, string> = {};
  if (breachState) params.breachState = breachState;
  if (severity) params.severity = severity;
  const config = Object.keys(params).length ? { params } : undefined;
  return apiClient.get<SlaBreachOrderItemDto[]>(`${BASE}/orders-at-risk`, config);
}

export async function getPlatformSlaSummary(): Promise<SlaBreachSummaryDto> {
  return apiClient.get<SlaBreachSummaryDto>('insights/platform-sla-summary');
}
