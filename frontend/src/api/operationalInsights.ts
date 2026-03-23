/**
 * Operational dashboards API.
 * Platform health: /api/insights/platform-health (AdminTenantsView).
 * Tenant dashboards: /api/insights/* (RequireCompanyId, tenant scoped).
 */

import apiClient from './client';

const BASE = 'insights';

export const operationalInsightsKeys = {
  all: ['operational-insights'] as const,
  platformHealth: () => [...operationalInsightsKeys.all, 'platform-health'] as const,
  tenantPerformance: () => [...operationalInsightsKeys.all, 'tenant-performance'] as const,
  operationsControl: () => [...operationalInsightsKeys.all, 'operations-control'] as const,
  financialOverview: () => [...operationalInsightsKeys.all, 'financial-overview'] as const,
  riskQuality: () => [...operationalInsightsKeys.all, 'risk-quality'] as const
};

export interface TenantHealthDistributionItemDto {
  status: string;
  count: number;
}

export interface PlatformHealthDto {
  activeTenants: number;
  ordersToday: number;
  completionRate: number;
  avgCompletionTimeHours: number | null;
  failedOrders: number;
  tenantHealthDistribution: TenantHealthDistributionItemDto[];
  eventsProcessed: number;
  eventFailures: number;
  retryQueueSize: number;
  eventLagSeconds: number | null;
}

export interface TenantPerformanceDto {
  ordersThisMonth: number;
  completionRate: number;
  avgInstallTimeHours: number | null;
  activeInstallers: number;
  deviceReplacements: number;
  ordersCompletedWithinSla: number;
  ordersBreachedSla: number;
  installerResponseTimeHours: number | null;
}

export interface StuckOrderItemDto {
  orderId: string;
  status: string;
  assignedSiId: string | null;
  updatedAtUtc: string | null;
}

export interface OperationsControlDto {
  ordersAssignedToday: number;
  ordersCompletedToday: number;
  installersActive: number;
  stuckOrders: number;
  stuckOrdersList: StuckOrderItemDto[];
  exceptions: number;
  avgInstallTimeHours: number | null;
  ordersCompletedWithinSlaToday: number;
  ordersBreachedSlaToday: number;
}

export interface FinancialOverviewDto {
  revenueToday: number;
  revenueMonth: number;
  installerPayouts: number;
  profitMarginPercent: number | null;
  pendingPayouts: number;
}

export interface RiskQualityDto {
  customerComplaints: number;
  deviceFailures: number;
  rescheduledOrders: number;
  installerRatingAverage: number | null;
  repeatCustomerIssues: number;
}

export async function getPlatformHealth(): Promise<PlatformHealthDto> {
  return apiClient.get<PlatformHealthDto>(`${BASE}/platform-health`);
}

export async function getTenantPerformance(): Promise<TenantPerformanceDto> {
  return apiClient.get<TenantPerformanceDto>(`${BASE}/tenant-performance`);
}

export async function getOperationsControl(): Promise<OperationsControlDto> {
  return apiClient.get<OperationsControlDto>(`${BASE}/operations-control`);
}

export async function getFinancialOverview(): Promise<FinancialOverviewDto> {
  return apiClient.get<FinancialOverviewDto>(`${BASE}/financial-overview`);
}

export async function getRiskQuality(): Promise<RiskQualityDto> {
  return apiClient.get<RiskQualityDto>(`${BASE}/risk-quality`);
}
