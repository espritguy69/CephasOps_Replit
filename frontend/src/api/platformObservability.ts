/**
 * Platform observability API — platform admin only.
 * Endpoints under /api/platform/analytics (SuperAdmin + AdminTenantsView).
 * Query keys prefixed with platform-observability to avoid mixing with tenant-scoped data.
 */

import apiClient from './client';

const BASE = '/platform/analytics';

export const platformObservabilityKeys = {
  all: ['platform-observability'] as const,
  summary: () => [...platformObservabilityKeys.all, 'summary'] as const,
  overview: () => [...platformObservabilityKeys.all, 'overview'] as const,
  detail: (tenantId: string) => [...platformObservabilityKeys.all, 'detail', tenantId] as const
};

export interface PlatformOperationsSummaryDto {
  activeTenantsCount: number;
  totalTenantsCount: number;
  failedJobsToday: number;
  failedNotificationsToday: number;
  failedIntegrationsToday: number;
  tenantsWithWarningsCount: number;
  generatedAtUtc: string;
}

export interface TenantOperationsOverviewItemDto {
  tenantId: string;
  tenantName: string;
  slug: string;
  isActive: boolean;
  requestCountLast24h: number;
  jobFailuresLast24h: number;
  jobsOkLast24h: number;
  notificationsSentLast24h: number;
  notificationsFailedLast24h: number;
  integrationsDeliveredLast24h: number;
  integrationsFailedLast24h: number;
  lastActivityUtc: string | null;
  healthStatus: string;
  hasWarnings: boolean;
}

export interface TenantOperationsDailyBucketDto {
  dateUtc: string;
  requestCount: number;
  jobFailures: number;
  jobsOk: number;
  notificationsSent: number;
  notificationsFailed: number;
  integrationsDelivered: number;
  integrationsFailed: number;
}

export interface TenantAnomalyDto {
  id: string;
  tenantId: string;
  kind: string;
  severity: string;
  occurredAtUtc: string;
  details: string | null;
  resolvedAtUtc: string | null;
}

export interface TenantOperationsDetailDto {
  tenantId: string;
  tenantName: string;
  isActive: boolean;
  dailyBuckets: TenantOperationsDailyBucketDto[];
  recentAnomalies: TenantAnomalyDto[];
}

export async function getPlatformOperationsSummary(): Promise<PlatformOperationsSummaryDto> {
  const data = await apiClient.get<PlatformOperationsSummaryDto>(`${BASE}/operations-summary`);
  return data as PlatformOperationsSummaryDto;
}

export async function getTenantOperationsOverview(): Promise<TenantOperationsOverviewItemDto[]> {
  const data = await apiClient.get<TenantOperationsOverviewItemDto[]>(`${BASE}/tenant-operations-overview`);
  return Array.isArray(data) ? data : [];
}

export async function getTenantOperationsDetail(tenantId: string): Promise<TenantOperationsDetailDto | null> {
  try {
    const data = await apiClient.get<TenantOperationsDetailDto>(`${BASE}/tenant-operations-detail/${tenantId}`);
    return data as TenantOperationsDetailDto;
  } catch {
    return null;
  }
}
