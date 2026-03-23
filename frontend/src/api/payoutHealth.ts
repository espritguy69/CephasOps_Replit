import apiClient from './client';
import type {
  PayoutHealthDashboardDto,
  PayoutAnomalyDetectionSummaryDto,
  PayoutAnomalyListResultDto,
  PayoutAnomalyClusterDto,
  PayoutAnomalyFilterParams,
  PayoutAnomalyDto,
  PayoutAnomalyReviewDto,
  PayoutAnomalyReviewSummaryDto,
  AssignAnomalyRequestDto,
  AddAnomalyCommentRequestDto,
  RunPayoutAnomalyAlertsRequestDto,
  RunPayoutAnomalyAlertsResultDto,
  PayoutAnomalyAlertRunDto,
  AlertResponseSummaryDto
} from '../types/payoutHealth';

const BASE = '/payout-health';

/**
 * Get Payout Health Dashboard data (snapshot coverage, anomalies, top unusual, recent snapshots).
 * Read-only; no payout logic changed.
 */
export async function getPayoutHealthDashboard(): Promise<PayoutHealthDashboardDto> {
  const data = await apiClient.get<PayoutHealthDashboardDto>(`${BASE}/dashboard`);
  if (!data || typeof data !== 'object') {
    return {
      snapshotHealth: {
        completedWithSnapshot: 0,
        completedMissingSnapshot: 0,
        totalCompleted: 0,
        coveragePercent: 100
      },
      anomalySummary: {
        legacyFallbackCount: 0,
        customOverrideCount: 0,
        ordersWithWarningsCount: 0,
        zeroPayoutCount: 0,
        negativeMarginCount: 0
      },
      topUnusualPayouts: [],
      recentSnapshots: []
    };
  }
return {
      snapshotHealth: data.snapshotHealth ?? {
        completedWithSnapshot: 0,
        completedMissingSnapshot: 0,
        totalCompleted: 0,
        coveragePercent: 100,
        normalFlowCount: 0,
        repairJobCount: 0,
        unknownProvenanceCount: 0,
        backfillCount: 0,
        manualBackfillCount: 0
      },
    anomalySummary: data.anomalySummary ?? {
      legacyFallbackCount: 0,
      customOverrideCount: 0,
      ordersWithWarningsCount: 0,
      zeroPayoutCount: 0,
      negativeMarginCount: 0
    },
    topUnusualPayouts: Array.isArray(data.topUnusualPayouts) ? data.topUnusualPayouts : [],
    recentSnapshots: Array.isArray(data.recentSnapshots) ? data.recentSnapshots : [],
    latestRepairRun: data.latestRepairRun ?? null,
    recentRepairRuns: Array.isArray(data.recentRepairRuns) ? data.recentRepairRuns : []
  };
}

/** Build query params for anomaly endpoints (strip null/undefined). */
function anomalyParams(params: PayoutAnomalyFilterParams): Record<string, string | number> {
  const out: Record<string, string | number> = {};
  if (params.from != null && params.from !== '') out.from = params.from;
  if (params.to != null && params.to !== '') out.to = params.to;
  if (params.installerId != null && params.installerId !== '') out.installerId = params.installerId;
  if (params.anomalyType != null && params.anomalyType !== '') out.anomalyType = params.anomalyType;
  if (params.severity != null && params.severity !== '') out.severity = params.severity;
  if (params.payoutPath != null && params.payoutPath !== '') out.payoutPath = params.payoutPath;
  if (params.companyId != null && params.companyId !== '') out.companyId = params.companyId;
  if (params.page != null) out.page = params.page;
  if (params.pageSize != null) out.pageSize = params.pageSize;
  return out;
}

/**
 * Get anomaly detection summary (counts by type and severity) for dashboard cards.
 */
export async function getPayoutAnomalySummary(
  params: PayoutAnomalyFilterParams = {}
): Promise<PayoutAnomalyDetectionSummaryDto> {
  const data = await apiClient.get<PayoutAnomalyDetectionSummaryDto>(`${BASE}/anomaly-summary`, {
    params: anomalyParams(params)
  });
  return data ?? {
    highPayoutVsPeerCount: 0,
    excessiveCustomOverrideCount: 0,
    excessiveLegacyFallbackCount: 0,
    repeatedWarningsCount: 0,
    zeroPayoutCount: 0,
    negativeMarginClusterCount: 0,
    installerDeviationCount: 0,
    totalCount: 0,
    highSeverityCount: 0,
    mediumSeverityCount: 0,
    lowSeverityCount: 0
  };
}

/**
 * Get paged list of payout anomalies with optional filters.
 */
export async function getPayoutAnomalies(
  params: PayoutAnomalyFilterParams = {}
): Promise<PayoutAnomalyListResultDto> {
  const data = await apiClient.get<PayoutAnomalyListResultDto>(`${BASE}/anomalies`, {
    params: anomalyParams(params)
  });
  return data ?? { items: [], totalCount: 0, page: 1, pageSize: 50 };
}

/**
 * Get top anomaly clusters (e.g. installers with most custom overrides).
 */
export async function getPayoutAnomalyClusters(
  params: { from?: string; to?: string; top?: number } = {}
): Promise<PayoutAnomalyClusterDto[]> {
  const p: Record<string, string | number> = {};
  if (params.from) p.from = params.from;
  if (params.to) p.to = params.to;
  if (params.top != null) p.top = params.top;
  const data = await apiClient.get<PayoutAnomalyClusterDto[]>(`${BASE}/anomaly-clusters`, {
    params: p
  });
  return Array.isArray(data) ? data : [];
}

// --- Anomaly governance ---

export async function getPayoutAnomalyReviewSummary(): Promise<PayoutAnomalyReviewSummaryDto> {
  const data = await apiClient.get<PayoutAnomalyReviewSummaryDto>(`${BASE}/anomaly-reviews/summary`);
  return data ?? { openCount: 0, investigatingCount: 0, resolvedTodayCount: 0 };
}

export async function getPayoutAnomalyReviews(params: {
  from?: string;
  to?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<PayoutAnomalyReviewDto[]> {
  const p: Record<string, string | number> = {};
  if (params.from) p.from = params.from;
  if (params.to) p.to = params.to;
  if (params.status) p.status = params.status;
  if (params.page != null) p.page = params.page;
  if (params.pageSize != null) p.pageSize = params.pageSize;
  const data = await apiClient.get<PayoutAnomalyReviewDto[]>(`${BASE}/anomaly-reviews`, { params: p });
  return Array.isArray(data) ? data : [];
}

export async function getPayoutAnomalyReview(anomalyId: string): Promise<PayoutAnomalyReviewDto | null> {
  try {
    return await apiClient.get<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/review`);
  } catch {
    return null;
  }
}

export async function postAcknowledgeAnomaly(anomalyId: string, body?: PayoutAnomalyDto | null): Promise<PayoutAnomalyReviewDto> {
  return apiClient.post<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/acknowledge`, body ?? undefined);
}

export async function postAssignAnomaly(anomalyId: string, request: AssignAnomalyRequestDto): Promise<PayoutAnomalyReviewDto> {
  return apiClient.post<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/assign`, request);
}

export async function postResolveAnomaly(anomalyId: string, body?: PayoutAnomalyDto | null): Promise<PayoutAnomalyReviewDto> {
  return apiClient.post<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/resolve`, body ?? undefined);
}

export async function postFalsePositiveAnomaly(anomalyId: string, body?: PayoutAnomalyDto | null): Promise<PayoutAnomalyReviewDto> {
  return apiClient.post<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/false-positive`, body ?? undefined);
}

export async function postAnomalyComment(anomalyId: string, request: AddAnomalyCommentRequestDto): Promise<PayoutAnomalyReviewDto> {
  return apiClient.post<PayoutAnomalyReviewDto>(`${BASE}/anomalies/${encodeURIComponent(anomalyId)}/comment`, request);
}

/**
 * Run payout anomaly alerting: send alerts for new high-severity (and optionally repeated medium) anomalies.
 * Does not change payout or detection logic.
 */
export async function postRunAnomalyAlerts(
  request: RunPayoutAnomalyAlertsRequestDto = {}
): Promise<RunPayoutAnomalyAlertsResultDto> {
  return apiClient.post<RunPayoutAnomalyAlertsResultDto>(`${BASE}/run-anomaly-alerts`, request);
}

/**
 * Get the most recent payout anomaly alert run (scheduler or manual), if any.
 */
export async function getLatestAlertRun(): Promise<PayoutAnomalyAlertRunDto | null> {
  const data = await apiClient.get<PayoutAnomalyAlertRunDto | null>(`${BASE}/alert-runs/latest`);
  return data ?? null;
}

/**
 * Get alert response summary (alerted by status, stale count, avg time to first action).
 */
export async function getAlertResponseSummary(params: {
  from?: string;
  to?: string;
  installerId?: string;
  anomalyType?: string;
  severity?: string;
  companyId?: string;
  staleThresholdHours?: number;
} = {}): Promise<AlertResponseSummaryDto> {
  const p: Record<string, string | number> = {};
  if (params.from) p.from = params.from;
  if (params.to) p.to = params.to;
  if (params.installerId) p.installerId = params.installerId;
  if (params.anomalyType) p.anomalyType = params.anomalyType;
  if (params.severity) p.severity = params.severity;
  if (params.companyId) p.companyId = params.companyId;
  if (params.staleThresholdHours != null) p.staleThresholdHours = params.staleThresholdHours;
  const data = await apiClient.get<AlertResponseSummaryDto>(`${BASE}/alert-response-summary`, { params: p });
  return (
    data ?? {
      alertedOpen: 0,
      alertedAcknowledged: 0,
      alertedInvestigating: 0,
      alertedResolved: 0,
      alertedFalsePositive: 0,
      staleCount: 0
    }
  );
}

/**
 * Get stale alerted anomalies (alerted, still open, no action after threshold).
 */
export async function getStaleAlertedAnomalies(params: {
  from?: string;
  to?: string;
  limit?: number;
  staleThresholdHours?: number;
} = {}): Promise<PayoutAnomalyDto[]> {
  const p: Record<string, string | number> = {};
  if (params.from) p.from = params.from;
  if (params.to) p.to = params.to;
  if (params.limit != null) p.limit = params.limit;
  if (params.staleThresholdHours != null) p.staleThresholdHours = params.staleThresholdHours;
  const data = await apiClient.get<PayoutAnomalyDto[]>(`${BASE}/stale-alerted-anomalies`, { params: p });
  return Array.isArray(data) ? data : [];
}
