/**
 * Automated Operational Intelligence API.
 * Tenant-scoped: requires company context (X-Company-Id). Use query keys that include
 * department/company so tenant switch invalidates cached intelligence data.
 */

import apiClient from './client';

const BASE = 'insights/operational-intelligence';

/** Query key factory: include department/company for cache invalidation on tenant switch */
export const operationalIntelligenceKeys = {
  all: (departmentId?: string | null) => ['operational-intelligence', departmentId ?? ''] as const,
  summary: (departmentId?: string | null) => [...operationalIntelligenceKeys.all(departmentId), 'summary'] as const,
  ordersAtRisk: (departmentId?: string | null, severity?: string | null) =>
    [...operationalIntelligenceKeys.all(departmentId), 'orders-at-risk', severity ?? ''] as const,
  installersAtRisk: (departmentId?: string | null, severity?: string | null) =>
    [...operationalIntelligenceKeys.all(departmentId), 'installers-at-risk', severity ?? ''] as const,
  buildingsAtRisk: (departmentId?: string | null, severity?: string | null) =>
    [...operationalIntelligenceKeys.all(departmentId), 'buildings-at-risk', severity ?? ''] as const,
  tenantRiskSignals: (departmentId?: string | null) =>
    [...operationalIntelligenceKeys.all(departmentId), 'tenant-risk-signals'] as const,
  platformSummary: () => ['operational-intelligence', 'platform-summary'] as const
};

export interface IntelligenceExplanationDto {
  ruleCode: string;
  summary: string;
  detail?: string;
  sourceCount?: number;
  severity: string;
}

export interface OrderRiskSignalDto {
  orderId: string;
  orderRef?: string;
  companyId: string;
  status?: string;
  assignedSiId?: string;
  updatedAtUtc?: string;
  severity: string;
  detectedAtUtc: string;
  reasons: IntelligenceExplanationDto[];
}

export interface InstallerRiskSignalDto {
  installerId: string;
  installerDisplayName?: string;
  companyId: string;
  severity: string;
  detectedAtUtc: string;
  reasons: IntelligenceExplanationDto[];
}

export interface BuildingRiskSignalDto {
  buildingId: string;
  buildingDisplayName?: string;
  companyId: string;
  severity: string;
  detectedAtUtc: string;
  reasons: IntelligenceExplanationDto[];
}

export interface TenantRiskSignalDto {
  companyId: string;
  tenantId?: string;
  severity: string;
  detectedAtUtc: string;
  reasons: IntelligenceExplanationDto[];
}

export interface OperationalIntelligenceSummaryDto {
  ordersAtRiskCount: number;
  installersAtRiskCount: number;
  buildingsAtRiskCount: number;
  criticalCount: number;
  warningCount: number;
  infoCount: number;
  generatedAtUtc: string;
}

export async function getOperationalIntelligenceSummary(): Promise<OperationalIntelligenceSummaryDto> {
  return apiClient.get<OperationalIntelligenceSummaryDto>(`${BASE}/summary`);
}

export async function getOrdersAtRisk(severity?: string | null): Promise<OrderRiskSignalDto[]> {
  const config = severity ? { params: { severity } } : undefined;
  return apiClient.get<OrderRiskSignalDto[]>(`${BASE}/orders-at-risk`, config);
}

export async function getInstallersAtRisk(severity?: string | null): Promise<InstallerRiskSignalDto[]> {
  const config = severity ? { params: { severity } } : undefined;
  return apiClient.get<InstallerRiskSignalDto[]>(`${BASE}/installers-at-risk`, config);
}

export async function getBuildingsAtRisk(severity?: string | null): Promise<BuildingRiskSignalDto[]> {
  const config = severity ? { params: { severity } } : undefined;
  return apiClient.get<BuildingRiskSignalDto[]>(`${BASE}/buildings-at-risk`, config);
}

export async function getTenantRiskSignals(): Promise<TenantRiskSignalDto[]> {
  return apiClient.get<TenantRiskSignalDto[]>(`${BASE}/tenant-risk-signals`);
}

/** Platform admin only */
export async function getPlatformOperationalIntelligenceSummary(): Promise<OperationalIntelligenceSummaryDto> {
  return apiClient.get<OperationalIntelligenceSummaryDto>('insights/platform-operational-intelligence');
}
