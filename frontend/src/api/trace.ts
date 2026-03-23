/**
 * Trace Explorer API — unified timeline by CorrelationId, EventId, JobRunId, WorkflowJobId, or Entity.
 */
import apiClient from './client';

export interface TraceTimelineItemDto {
  timestampUtc: string;
  correlationId: string | null;
  companyId: string | null;
  itemType: string;
  status: string | null;
  source: string | null;
  entityType: string | null;
  entityId: string | null;
  title: string;
  summary: string | null;
  /** Alias for summary (detail or error message). */
  detailSummary?: string | null;
  relatedId: string | null;
  relatedIdKind: string | null;
  parentRelatedId: string | null;
  actorUserId: string | null;
  handlerName?: string | null;
}

export interface TraceTimelineDto {
  lookupKind: string;
  lookupValue: string | null;
  items: TraceTimelineItemDto[];
  totalCount?: number | null;
  page?: number | null;
  pageSize?: number | null;
}

export interface TraceMetricsDto {
  fromUtc: string;
  toUtc: string;
  failedEventsCount: number;
  deadLetterEventsCount: number;
  failedJobRunsCount: number;
  deadLetterJobRunsCount: number;
  correlationChainsWithFailuresCount: number;
}

export async function getTraceMetrics(fromUtc?: string, toUtc?: string): Promise<TraceMetricsDto> {
  const params: Record<string, string> = {};
  if (fromUtc) params.fromUtc = fromUtc;
  if (toUtc) params.toUtc = toUtc;
  const response = await apiClient.get<TraceMetricsDto>('/trace/metrics', { params });
  return response as TraceMetricsDto;
}

export async function getTraceByCorrelationId(correlationId: string): Promise<TraceTimelineDto> {
  const response = await apiClient.get<TraceTimelineDto>(`/trace/correlation/${encodeURIComponent(correlationId)}`);
  return response as TraceTimelineDto;
}

export async function getTraceByEventId(eventId: string): Promise<TraceTimelineDto | null> {
  try {
    const response = await apiClient.get<TraceTimelineDto>(`/trace/event/${eventId}`);
    return response as TraceTimelineDto;
  } catch {
    return null;
  }
}

export async function getTraceByJobRunId(jobRunId: string): Promise<TraceTimelineDto | null> {
  try {
    const response = await apiClient.get<TraceTimelineDto>(`/trace/jobrun/${jobRunId}`);
    return response as TraceTimelineDto;
  } catch {
    return null;
  }
}

export async function getTraceByWorkflowJobId(workflowJobId: string): Promise<TraceTimelineDto | null> {
  try {
    const response = await apiClient.get<TraceTimelineDto>(`/trace/workflowjob/${workflowJobId}`);
    return response as TraceTimelineDto;
  } catch {
    return null;
  }
}

export async function getTraceByEntity(entityType: string, entityId: string): Promise<TraceTimelineDto> {
  const response = await apiClient.get<TraceTimelineDto>('/trace/entity', {
    params: { entityType, entityId }
  });
  return response as TraceTimelineDto;
}
