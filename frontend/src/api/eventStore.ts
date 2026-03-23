/**
 * Event Store API — list events, failed/dead-letter, dashboard, retry, replay, related links.
 * Company-scoped for non–global admins.
 */
import apiClient from './client';

export interface EventStoreListItemDto {
  eventId: string;
  eventType: string;
  occurredAtUtc: string;
  createdAtUtc: string;
  processedAtUtc: string | null;
  retryCount: number;
  status: string;
  correlationId: string | null;
  companyId: string | null;
  entityType: string | null;
  entityId: string | null;
  lastError: string | null;
  lastHandler: string | null;
}

export interface EventStoreDetailDto extends EventStoreListItemDto {
  payload: string;
  triggeredByUserId: string | null;
  source: string | null;
  lastErrorAtUtc: string | null;
  parentEventId: string | null;
}

export interface EventStoreDashboardDto {
  eventsToday: number;
  processedCount: number;
  failedCount: number;
  deadLetterCount: number;
  processedPercent: number;
  failedPercent: number;
  totalRetryCount: number;
  topFailingEventTypes: { eventType: string; count: number }[];
  topFailingCompanies: { companyId: string | null; failedCount: number; deadLetterCount: number }[];
}

export interface EventStoreRelatedJobRunDto {
  id: string;
  jobName: string;
  jobType: string;
  status: string;
  correlationId: string | null;
  eventId: string | null;
  startedAtUtc: string;
  completedAtUtc: string | null;
  errorMessage: string | null;
}

export interface EventStoreRelatedWorkflowJobDto {
  id: string;
  correlationId: string | null;
  entityType: string;
  entityId: string;
  currentStatus: string | null;
  targetStatus: string | null;
  state: string;
  createdAt: string;
}

export interface EventStoreRelatedLinksDto {
  jobRuns: EventStoreRelatedJobRunDto[];
  workflowJobs: EventStoreRelatedWorkflowJobDto[];
}

export interface EventReplayResult {
  success: boolean;
  message?: string | null;
  errorMessage?: string | null;
  blockedReason?: string | null;
}

export interface ListEventsParams {
  companyId?: string;
  eventType?: string;
  status?: string;
  correlationId?: string;
  entityType?: string;
  entityId?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}

export interface ListEventsResponse {
  items: EventStoreListItemDto[];
  total: number;
  page: number;
  pageSize: number;
}

// --- Event Bus Observability (handler processing) ---

export interface EventProcessingLogItemDto {
  id: string;
  eventId: string;
  handlerName: string;
  state: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  attemptCount: number;
  error: string | null;
  replayOperationId: string | null;
  correlationId: string | null;
}

export interface EventDetailWithProcessingDto {
  event: EventStoreDetailDto;
  processingLogs: EventProcessingLogItemDto[];
}

export interface ListProcessingLogParams {
  page?: number;
  pageSize?: number;
  failedOnly?: boolean;
  eventId?: string;
  replayOperationId?: string;
  correlationId?: string;
}

export interface ListProcessingLogResponse {
  items: EventProcessingLogItemDto[];
  total: number;
  page: number;
  pageSize: number;
}

export async function listEvents(params: ListEventsParams = {}): Promise<ListEventsResponse> {
  const response = await apiClient.get<ListEventsResponse>('/event-store/events', { params });
  return response as ListEventsResponse;
}

export async function listFailedEvents(page = 1, pageSize = 50): Promise<ListEventsResponse> {
  const response = await apiClient.get<ListEventsResponse>('/event-store/events/failed', {
    params: { page, pageSize }
  });
  return response as ListEventsResponse;
}

export async function listDeadLetterEvents(page = 1, pageSize = 50): Promise<ListEventsResponse> {
  const response = await apiClient.get<ListEventsResponse>('/event-store/events/dead-letter', {
    params: { page, pageSize }
  });
  return response as ListEventsResponse;
}

export async function getEvent(eventId: string): Promise<EventStoreDetailDto> {
  const response = await apiClient.get<EventStoreDetailDto>(`/event-store/events/${eventId}`);
  return response as EventStoreDetailDto;
}

export async function getEventRelatedLinks(eventId: string): Promise<EventStoreRelatedLinksDto> {
  const response = await apiClient.get<EventStoreRelatedLinksDto>(`/event-store/events/${eventId}/related-links`);
  return response as EventStoreRelatedLinksDto;
}

export async function getEventStoreDashboard(fromUtc?: string, toUtc?: string): Promise<EventStoreDashboardDto> {
  const params: Record<string, string> = {};
  if (fromUtc) params.fromUtc = fromUtc;
  if (toUtc) params.toUtc = toUtc;
  const response = await apiClient.get<EventStoreDashboardDto>('/event-store/dashboard', {
    params: Object.keys(params).length ? params : undefined
  });
  return response as EventStoreDashboardDto;
}

export async function retryEvent(eventId: string): Promise<EventReplayResult> {
  const response = await apiClient.post<EventReplayResult>(`/event-store/events/${eventId}/retry`);
  return response as EventReplayResult;
}

export async function replayEvent(eventId: string): Promise<EventReplayResult> {
  const response = await apiClient.post<EventReplayResult>(`/event-store/events/${eventId}/replay`);
  return response as EventReplayResult;
}

export async function getReplayPolicy(eventType: string): Promise<{ eventType: string; allowed: boolean; blocked: boolean }> {
  const response = await apiClient.get<{ eventType: string; allowed: boolean; blocked: boolean }>(
    `/event-store/replay-policy/${encodeURIComponent(eventType)}`
  );
  return response as { eventType: string; allowed: boolean; blocked: boolean };
}

// Observability: recent handler processing (bounded, paginated)
export async function listProcessingLog(params: ListProcessingLogParams = {}): Promise<ListProcessingLogResponse> {
  const response = await apiClient.get<ListProcessingLogResponse>('/event-store/observability/processing', {
    params: params as Record<string, string | number | boolean | undefined>
  });
  return response as ListProcessingLogResponse;
}

// Observability: processing rows for a single event
export async function getEventProcessing(eventId: string): Promise<EventProcessingLogItemDto[]> {
  const response = await apiClient.get<EventProcessingLogItemDto[]>(
    `/event-store/events/${eventId}/observability/processing`
  );
  return Array.isArray(response) ? response : [];
}

// Observability: event detail with related handler processing
export async function getEventDetailWithProcessing(eventId: string): Promise<EventDetailWithProcessingDto> {
  const response = await apiClient.get<EventDetailWithProcessingDto>(
    `/event-store/observability/events/${eventId}`
  );
  return response as EventDetailWithProcessingDto;
}

// --- Operational Replay (batch/filtered) ---

export interface ReplayRequestDto {
  companyId?: string | null;
  eventType?: string | null;
  status?: string | null;
  fromOccurredAtUtc?: string | null;
  toOccurredAtUtc?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  correlationId?: string | null;
  maxEvents?: number | null;
  dryRun?: boolean;
  replayReason?: string | null;
  replayTarget?: string | null;
  replayMode?: string | null;
}

export interface ReplayPreviewResultDto {
  totalMatched: number;
  evaluatedCount: number;
  eligibleCount: number;
  blockedCount: number;
  blockedReasons: string[];
  sampleEvents: EventStoreListItemDto[];
  companiesAffected: (string | null)[];
  eventTypesAffected: string[];
  /** Phase 2 */
  orderingStrategyId?: string | null;
  orderingStrategyDescription?: string | null;
  orderingGuaranteeLevel?: string | null;
  orderingDegradedReason?: string | null;
  estimatedAffectedEntityTypes?: string[];
  limitations?: string[];
  replayTargetId?: string | null;
  /** Projection-capable diff preview (bounded) */
  affectedProjectionCategories?: string[];
  estimatedChangedEntityCount?: number | null;
  projectionPreviewQuality?: string | null;
  projectionPreviewUnavailableReason?: string | null;
  /** Event Ledger expansion: ledger families that may be written when replay runs */
  ledgerFamiliesAffected?: string[];
  ledgerWritesExpected?: boolean;
  ledgerDerivedProjectionsImpacted?: string[];
  ledgerPreviewUnavailableReason?: string | null;
}

export interface ReplayTargetDescriptorDto {
  id: string;
  displayName: string;
  description?: string | null;
  supported: boolean;
  orderingStrategyId?: string | null;
  orderingStrategyDescription?: string | null;
  supportsPreview: boolean;
  supportsApply: boolean;
  supportsCheckpoint: boolean;
  isReplaySafe: boolean;
  supportedFilterNames: string[];
  limitations: string[];
  orderingGuaranteeLevel?: string | null;
  orderingDegradedReason?: string | null;
}

export interface ReplayOperationProgressDto {
  operationId: string;
  state: string | null;
  resumeRequired: boolean;
  totalEligible: number | null;
  totalExecuted: number | null;
  totalSucceeded: number | null;
  totalFailed: number | null;
  processedCountAtLastCheckpoint: number | null;
  lastCheckpointAtUtc: string | null;
  lastProcessedEventId: string | null;
  progressPercent: number | null;
  orderingStrategyId?: string | null;
  orderingGuaranteeLevel?: string | null;
  orderingDegradedReason?: string | null;
}

export interface OperationalReplayExecutionResultDto {
  replayOperationId: string;
  dryRun: boolean;
  totalMatched: number;
  totalEligible: number;
  totalExecuted: number;
  totalSucceeded: number;
  totalFailed: number;
  replayCorrelationId?: string | null;
  state?: string | null;
  errorMessage?: string | null;
}

export interface ReplayOperationListItemDto {
  id: string;
  requestedByUserId: string | null;
  requestedAtUtc: string;
  dryRun: boolean;
  replayReason: string | null;
  companyId: string | null;
  eventType: string | null;
  replayTarget: string | null;
  replayMode: string | null;
  startedAtUtc: string | null;
  totalMatched: number | null;
  totalEligible: number | null;
  totalExecuted: number | null;
  totalSucceeded: number | null;
  totalFailed: number | null;
  skippedCount: number | null;
  durationMs: number | null;
  errorSummary: string | null;
  state: string | null;
  completedAtUtc: string | null;
  /** Phase 2 */
  resumeRequired?: boolean;
  orderingStrategyId?: string | null;
  retriedFromOperationId?: string | null;
  rerunReason?: string | null;
  orderingGuaranteeLevel?: string | null;
  orderingDegradedReason?: string | null;
}

export interface ReplayOperationEventItemDto {
  eventId: string;
  eventType: string | null;
  entityType: string | null;
  entityId: string | null;
  succeeded: boolean;
  errorMessage: string | null;
  skippedReason: string | null;
  processedAtUtc: string;
  durationMs: number | null;
}

export interface ReplayOperationDetailDto extends ReplayOperationListItemDto {
  status: string | null;
  fromOccurredAtUtc: string | null;
  toOccurredAtUtc: string | null;
  entityType: string | null;
  entityId: string | null;
  correlationId: string | null;
  maxEvents: number | null;
  replayCorrelationId: string | null;
  notes: string | null;
  eventResults?: ReplayOperationEventItemDto[] | null;
  /** Phase 2 */
  lastCheckpointAtUtc?: string | null;
  lastProcessedEventId?: string | null;
  processedCountAtLastCheckpoint?: number | null;
}

export async function previewReplay(request: ReplayRequestDto): Promise<ReplayPreviewResultDto> {
  const response = await apiClient.post<ReplayPreviewResultDto>('/event-store/replay/preview', request);
  return response as ReplayPreviewResultDto;
}

export async function executeReplay(
  request: ReplayRequestDto,
  async = false
): Promise<OperationalReplayExecutionResultDto | { replayOperationId: string; message: string }> {
  const url = async ? '/event-store/replay/execute?async=true' : '/event-store/replay/execute';
  const response = await apiClient.post<OperationalReplayExecutionResultDto | { replayOperationId: string; message: string }>(url, request);
  return response as OperationalReplayExecutionResultDto | { replayOperationId: string; message: string };
}

export async function listReplayOperations(page = 1, pageSize = 20): Promise<{
  items: ReplayOperationListItemDto[];
  total: number;
  page: number;
  pageSize: number;
}> {
  const response = await apiClient.get<{ items: ReplayOperationListItemDto[]; total: number; page: number; pageSize: number }>(
    '/event-store/replay/operations',
    { params: { page, pageSize } }
  );
  return response as { items: ReplayOperationListItemDto[]; total: number; page: number; pageSize: number };
}

export async function getReplayOperation(id: string): Promise<ReplayOperationDetailDto> {
  const response = await apiClient.get<ReplayOperationDetailDto>(`/event-store/replay/operations/${id}`);
  return response as ReplayOperationDetailDto;
}

/** Phase 2: List replay targets from registry. */
export async function listReplayTargets(): Promise<ReplayTargetDescriptorDto[]> {
  const response = await apiClient.get<ReplayTargetDescriptorDto[] | { data?: ReplayTargetDescriptorDto[] }>('/event-store/replay/targets');
  if (Array.isArray(response)) return response;
  const data = (response as { data?: ReplayTargetDescriptorDto[] })?.data;
  return Array.isArray(data) ? data : [];
}

/** Phase 2: Progress for active/resumable run. Returns null when operation not found. */
export async function getReplayOperationProgress(id: string): Promise<ReplayOperationProgressDto | null> {
  try {
    const response = await apiClient.get<ReplayOperationProgressDto>(`/event-store/replay/operations/${id}/progress`);
    return response as ReplayOperationProgressDto;
  } catch {
    return null;
  }
}

/** Phase 2: Resume operation (PartiallyCompleted or Pending). Returns result when sync; 202 when async. */
export async function resumeReplayOperation(
  id: string,
  asyncMode = false
): Promise<OperationalReplayExecutionResultDto | { replayOperationId: string; message: string }> {
  const url = asyncMode ? `/event-store/replay/operations/${id}/resume?async=true` : `/event-store/replay/operations/${id}/resume`;
  const response = await apiClient.post<OperationalReplayExecutionResultDto | { replayOperationId: string; message: string }>(url);
  return response as OperationalReplayExecutionResultDto | { replayOperationId: string; message: string };
}

/** Phase 2: Rerun only failed events as a new operation. */
export async function rerunFailedReplayOperation(
  id: string,
  body?: { rerunReason?: string | null }
): Promise<OperationalReplayExecutionResultDto> {
  const response = await apiClient.post<OperationalReplayExecutionResultDto>(
    `/event-store/replay/operations/${id}/rerun-failed`,
    body ?? {}
  );
  return response as OperationalReplayExecutionResultDto;
}

/** Request cancel. Pending/PartiallyCompleted: cancelled immediately. Running: stops at next checkpoint. */
export async function cancelReplayOperation(id: string): Promise<OperationalReplayExecutionResultDto> {
  const response = await apiClient.post<OperationalReplayExecutionResultDto>(
    `/event-store/replay/operations/${id}/cancel`
  );
  return response as OperationalReplayExecutionResultDto;
}

// --- Event Ledger (operational ledger foundation) ---

export interface LedgerFamilyDescriptorDto {
  id: string;
  displayName: string;
  description?: string | null;
  orderingStrategyId?: string | null;
  orderingGuaranteeLevel?: string | null;
}

export interface LedgerEntryDto {
  id: string;
  sourceEventId?: string | null;
  replayOperationId?: string | null;
  ledgerFamily: string;
  category?: string | null;
  companyId?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  eventType: string;
  occurredAtUtc: string;
  recordedAtUtc: string;
  payloadSnapshot?: string | null;
  correlationId?: string | null;
  triggeredByUserId?: string | null;
  orderingStrategyId?: string | null;
}

export interface ListLedgerEntriesParams {
  companyId?: string;
  entityType?: string;
  entityId?: string;
  ledgerFamily?: string;
  fromOccurredUtc?: string;
  toOccurredUtc?: string;
  page?: number;
  pageSize?: number;
}

export async function listLedgerFamilies(): Promise<LedgerFamilyDescriptorDto[]> {
  const response = await apiClient.get<LedgerFamilyDescriptorDto[] | { data?: LedgerFamilyDescriptorDto[] }>('/event-store/ledger/families');
  return Array.isArray(response) ? response : (response as { data?: LedgerFamilyDescriptorDto[] }).data ?? [];
}

export async function listLedgerEntries(params: ListLedgerEntriesParams = {}): Promise<{
  items: LedgerEntryDto[];
  total: number;
  page: number;
  pageSize: number;
}> {
  const response = await apiClient.get<{ items: LedgerEntryDto[]; total: number; page: number; pageSize: number }>(
    '/event-store/ledger/entries',
    { params }
  );
  return response as { items: LedgerEntryDto[]; total: number; page: number; pageSize: number };
}

export async function getLedgerEntry(id: string): Promise<LedgerEntryDto> {
  const response = await apiClient.get<LedgerEntryDto>(`/event-store/ledger/entries/${id}`);
  return response as LedgerEntryDto;
}

export interface WorkflowTransitionTimelineItemDto {
  ledgerEntryId: string;
  sourceEventId: string;
  occurredAtUtc: string;
  recordedAtUtc: string;
  companyId?: string | null;
  entityType: string;
  entityId: string;
  fromStatus?: string | null;
  toStatus?: string | null;
  workflowJobId?: string | null;
  payloadSnapshot?: string | null;
}

export async function getWorkflowTransitionTimelineFromLedger(params: {
  entityType: string;
  entityId: string;
  companyId?: string;
  fromOccurredUtc?: string;
  toOccurredUtc?: string;
  limit?: number;
}): Promise<WorkflowTransitionTimelineItemDto[]> {
  const response = await apiClient.get<WorkflowTransitionTimelineItemDto[]>(
    '/event-store/ledger/timeline/workflow-transition',
    { params }
  );
  return response as WorkflowTransitionTimelineItemDto[];
}

export interface OrderTimelineItemDto {
  ledgerEntryId: string;
  sourceEventId?: string | null;
  occurredAtUtc: string;
  recordedAtUtc: string;
  ledgerFamily: string;
  eventType: string;
  category?: string | null;
  priorStatus?: string | null;
  newStatus?: string | null;
  triggeredByUserId?: string | null;
  orderingStrategyId?: string | null;
  payloadSnapshot?: string | null;
}

export async function getOrderTimelineFromLedger(params: {
  orderId: string;
  companyId?: string;
  fromOccurredUtc?: string;
  toOccurredUtc?: string;
  limit?: number;
}): Promise<OrderTimelineItemDto[]> {
  const response = await apiClient.get<OrderTimelineItemDto[] | { data?: OrderTimelineItemDto[] }>(
    '/event-store/ledger/timeline/order',
    { params }
  );
  if (Array.isArray(response)) return response;
  return (response as { data?: OrderTimelineItemDto[] }).data ?? [];
}

export interface UnifiedOrderHistoryItemDto {
  ledgerEntryId: string;
  occurredAtUtc: string;
  recordedAtUtc: string;
  ledgerFamily: string;
  eventType: string;
  category?: string | null;
  priorStatus?: string | null;
  newStatus?: string | null;
  sourceEventId?: string | null;
  orderingStrategyId?: string | null;
  triggeredByUserId?: string | null;
}

export async function getUnifiedOrderHistoryFromLedger(params: {
  orderId: string;
  companyId?: string;
  fromOccurredUtc?: string;
  toOccurredUtc?: string;
  limit?: number;
}): Promise<UnifiedOrderHistoryItemDto[]> {
  const response = await apiClient.get<UnifiedOrderHistoryItemDto[] | { data?: UnifiedOrderHistoryItemDto[] }>(
    '/event-store/ledger/timeline/unified-order',
    { params }
  );
  if (Array.isArray(response)) return response;
  return (response as { data?: UnifiedOrderHistoryItemDto[] }).data ?? [];
}
