/**
 * SI Operational Insights API — GET /api/admin/operations/si-insights
 * Read-only operational intelligence for Service Installer field operations.
 */
import apiClient from './client';

export interface SiInstallerAverageDto {
  siId: string | null;
  siDisplayName: string | null;
  averageHours: number;
  orderCount: number;
}

export interface SiStuckOrderDto {
  orderId: string;
  status: string;
  daysInCurrentStatus: number;
}

export interface SiCompletionPerformanceDto {
  averageAssignedToCompleteHours: number | null;
  ordersCompletedInWindow: number;
  byInstaller: SiInstallerAverageDto[];
  ordersStuckLongerThanDays: SiStuckOrderDto[];
  stuckThresholdDays: number;
}

export interface SiReasonCountDto {
  reason: string | null;
  count: number;
}

export interface SiOrderChurnDto {
  orderId: string;
  transitionCount: number;
  rescheduleCount: number;
  blockerCount: number;
}

export interface SiRescheduleBlockerPatternsDto {
  topRescheduleReasons: SiReasonCountDto[];
  topBlockerReasons: SiReasonCountDto[];
  ordersWithHighChurn: SiOrderChurnDto[];
  churnThresholdTransitions: number;
}

export interface SiInstallerCountDto {
  siId: string | null;
  siDisplayName: string | null;
  count: number;
}

export interface SiMaterialReplacementPatternsDto {
  topReplacementReasons: SiReasonCountDto[];
  byInstaller: SiInstallerCountDto[];
  ordersWithMultipleReplacements: number;
}

export interface SiAssuranceReworkDto {
  assuranceOrdersCompletedInWindow: number;
  assuranceOrdersWithReplacement: number;
  topAssuranceIssues: SiReasonCountDto[];
}

export interface SiBuildingCountDto {
  buildingId: string;
  buildingName: string | null;
  rescheduleCount: number;
  blockerCount: number;
}

export interface SiOperationalHotspotsDto {
  buildingsWithMostDisruptions: SiBuildingCountDto[];
  coverageNote: string | null;
}

/** Building Reliability Score item: band and contributing factors. For prioritization only. */
export interface SiBuildingReliabilityItemDto {
  buildingId: string;
  buildingName: string | null;
  band: 'LowRisk' | 'ModerateRisk' | 'HighRisk';
  rescheduleCount: number;
  blockerCount: number;
  highChurnOrderCount: number;
  stuckOrderCount: number;
  assuranceWithReplacementCount: number;
  ordersWithReplacementsCount: number;
  reasonSummary: string | null;
}

export interface SiBuildingReliabilityDto {
  buildings: SiBuildingReliabilityItemDto[];
  interpretationNote: string | null;
}

/** Single order failure pattern: name, count, samples, explanation, strength. */
export interface SiOrderFailurePatternItemDto {
  patternId: string;
  patternName: string;
  count: number;
  sampleOrderIds: string[];
  sampleBuildingIds: string[];
  sampleInstallerIds: string[];
  sampleInstallerDisplayNames: (string | null)[];
  explanation: string | null;
  strength: 'StrongSignal' | 'ReviewNeeded' | 'PartialCoverage';
  limitations: string | null;
}

export interface SiOrderFailurePatternsDto {
  patterns: SiOrderFailurePatternItemDto[];
  interpretationNote: string | null;
}

/** Building-level cluster where multiple operational signals align. */
export interface SiPatternClusterItemDto {
  buildingId: string;
  buildingName: string | null;
  signalsPresent: string[];
  sampleOrderIds: string[];
  interpretation: string | null;
  classification: 'OperationalCluster' | 'PossibleInfrastructureIssue';
  limitations: string | null;
}

export interface SiPatternClustersDto {
  clusters: SiPatternClusterItemDto[];
  interpretationNote: string | null;
}

export interface SiOperationalInsightsDto {
  generatedAtUtc: string;
  companyId: string | null;
  windowDays: number;
  dataQualityNote: string | null;
  completionPerformance: SiCompletionPerformanceDto;
  rescheduleBlockerPatterns: SiRescheduleBlockerPatternsDto;
  materialReplacementPatterns: SiMaterialReplacementPatternsDto;
  assuranceRework: SiAssuranceReworkDto;
  operationalHotspots: SiOperationalHotspotsDto;
  buildingReliability: SiBuildingReliabilityDto;
  orderFailurePatterns: SiOrderFailurePatternsDto;
  patternClusters: SiPatternClustersDto;
  dataGaps: string[];
}

export interface GetSiInsightsParams {
  companyId?: string | null;
  windowDays?: number;
}

/**
 * Fetch SI operational insights for the current tenant (or specified company for SuperAdmin).
 */
export async function getSiInsights(params?: GetSiInsightsParams): Promise<SiOperationalInsightsDto> {
  const query: Record<string, string | number> = {};
  if (params?.windowDays != null) query.windowDays = params.windowDays;
  if (params?.companyId) query.companyId = params.companyId;
  const response = await apiClient.get<SiOperationalInsightsDto>('/admin/operations/si-insights', {
    params: query
  });
  return response as SiOperationalInsightsDto;
}
