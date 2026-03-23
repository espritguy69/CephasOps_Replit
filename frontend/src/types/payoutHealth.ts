/**
 * Types for Payout Health Dashboard (read-only reporting).
 */

export interface PayoutSnapshotHealthDto {
  completedWithSnapshot: number;
  completedMissingSnapshot: number;
  totalCompleted: number;
  coveragePercent: number;
  normalFlowCount: number;
  repairJobCount: number;
  unknownProvenanceCount: number;
  backfillCount: number;
  manualBackfillCount: number;
}

export interface RepairRunSummaryDto {
  id: string;
  startedAt: string;
  completedAt: string | null;
  totalProcessed: number;
  createdCount: number;
  skippedCount: number;
  errorCount: number;
  triggerSource: string;
  notes: string | null;
}

export interface PayoutAnomalySummaryDto {
  legacyFallbackCount: number;
  customOverrideCount: number;
  ordersWithWarningsCount: number;
  zeroPayoutCount: number;
  negativeMarginCount: number;
}

export interface TopUnusualPayoutRowDto {
  orderId: string;
  finalPayout: number;
  currency: string;
  payoutPath: string | null;
  rateGroupId: string | null;
  groupAveragePayout: number;
  multipleOfAverage: number;
  calculatedAt: string;
}

export interface RecentSnapshotRowDto {
  orderId: string;
  finalPayout: number;
  currency: string;
  payoutPath: string | null;
  calculatedAt: string;
}

export interface PayoutHealthDashboardDto {
  snapshotHealth: PayoutSnapshotHealthDto;
  anomalySummary: PayoutAnomalySummaryDto;
  topUnusualPayouts: TopUnusualPayoutRowDto[];
  recentSnapshots: RecentSnapshotRowDto[];
  latestRepairRun: RepairRunSummaryDto | null;
  recentRepairRuns: RepairRunSummaryDto[];
}

// --- Anomaly detection (read-only monitoring) ---

export interface PayoutAnomalyDto {
  id: string;
  anomalyType: string;
  severity: string;
  orderId?: string | null;
  installerId?: string | null;
  installerName?: string | null;
  payoutSnapshotId?: string | null;
  payoutAmount?: number | null;
  baselineAmount?: number | null;
  deviationPercent?: number | null;
  payoutPath?: string | null;
  rateGroupId?: string | null;
  serviceProfileId?: string | null;
  orderCategoryId?: string | null;
  installationMethodId?: string | null;
  orderTypeId?: string | null;
  companyId?: string | null;
  detectedAt: string;
  reason: string;
  warningCount?: number | null;
  customOverrideCount?: number | null;
  legacyFallbackCount?: number | null;
  negativeMarginCount?: number | null;
  reviewStatus?: string | null;
  assignedToUserId?: string | null;
  assignedToUserName?: string | null;
  /** Whether this anomaly has been sent to an alert channel (e.g. email). */
  alerted?: boolean;
  /** UTC time of the most recent successful alert for this anomaly. */
  lastAlertedAt?: string | null;
  /** UTC time of last review action (acknowledge, assign, resolve, comment) if any. */
  lastActionAt?: string | null;
}

export interface PayoutAnomalyDetectionSummaryDto {
  highPayoutVsPeerCount: number;
  excessiveCustomOverrideCount: number;
  excessiveLegacyFallbackCount: number;
  repeatedWarningsCount: number;
  zeroPayoutCount: number;
  negativeMarginClusterCount: number;
  installerDeviationCount: number;
  totalCount: number;
  highSeverityCount: number;
  mediumSeverityCount: number;
  lowSeverityCount: number;
}

export interface PayoutAnomalyClusterDto {
  clusterKind: string;
  label: string;
  entityId?: string | null;
  contextKey?: string | null;
  anomalyCount: number;
  extraCount?: number | null;
}

export interface PayoutAnomalyFilterParams {
  from?: string | null;
  to?: string | null;
  installerId?: string | null;
  anomalyType?: string | null;
  severity?: string | null;
  payoutPath?: string | null;
  companyId?: string | null;
  page?: number;
  pageSize?: number;
}

export interface PayoutAnomalyListResultDto {
  items: PayoutAnomalyDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// --- Anomaly governance (review) ---

export interface PayoutAnomalyReviewDto {
  id: string;
  anomalyFingerprintId: string;
  anomalyType: string;
  orderId?: string | null;
  installerId?: string | null;
  payoutSnapshotId?: string | null;
  severity: string;
  detectedAt: string;
  status: string;
  assignedToUserId?: string | null;
  assignedToUserName?: string | null;
  notesJson?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PayoutAnomalyReviewSummaryDto {
  openCount: number;
  investigatingCount: number;
  resolvedTodayCount: number;
}

export interface AssignAnomalyRequestDto {
  assignedToUserId?: string | null;
}

export interface AddAnomalyCommentRequestDto {
  text: string;
}

// --- Anomaly alerting ---

export interface RunPayoutAnomalyAlertsRequestDto {
  recipientEmails?: string[];
  includeMediumRepeated?: boolean;
}

export interface RunPayoutAnomalyAlertsResultDto {
  anomaliesConsidered: number;
  anomaliesAlerted: number;
  alertsSent: number;
  alertsFailed: number;
  skippedCount: number;
  channelsUsed: string[];
  errors: string[];
}

export interface PayoutAnomalyAlertRunDto {
  id: string;
  startedAt: string;
  completedAt: string | null;
  evaluatedCount: number;
  sentCount: number;
  skippedCount: number;
  errorCount: number;
  triggerSource: string;
}

/** Alert response summary: counts of alerted anomalies by review status. */
export interface AlertResponseSummaryDto {
  alertedOpen: number;
  alertedAcknowledged: number;
  alertedInvestigating: number;
  alertedResolved: number;
  alertedFalsePositive: number;
  staleCount: number;
  averageTimeToFirstActionMinutes?: number | null;
}
