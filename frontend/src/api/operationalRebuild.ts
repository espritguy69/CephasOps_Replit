/**
 * Operational State Rebuilder API — list targets, preview, execute rebuild, get result.
 * Admin-only; company-scoped for non–global admins.
 */
import apiClient from './client';

export interface RebuildTargetDescriptorDto {
  id: string;
  displayName: string;
  description: string;
  sourceOfTruth: string;
  rebuildStrategy: string;
  scopeRuleNames: string[];
  orderingGuarantee: string;
  isFullRebuild: boolean;
  supportsPreview: boolean;
  limitations: string[];
  supportsResume?: boolean;
}

export interface RebuildRequestDto {
  rebuildTargetId: string;
  companyId?: string | null;
  fromOccurredAtUtc?: string | null;
  toOccurredAtUtc?: string | null;
  dryRun?: boolean;
}

export interface RebuildPreviewResultDto {
  rebuildTargetId: string;
  displayName: string;
  sourceRecordCount: number;
  currentTargetRowCount?: number | null;
  rebuildStrategy: string;
  scopeDescription?: string | null;
  dryRun: boolean;
}

export interface RebuildExecutionResultDto {
  rebuildOperationId: string;
  rebuildTargetId: string;
  state: string;
  dryRun: boolean;
  rowsDeleted: number;
  rowsInserted: number;
  rowsUpdated: number;
  sourceRecordCount?: number | null;
  durationMs?: number | null;
  errorMessage?: string | null;
}

export interface RebuildOperationSummaryDto {
  id: string;
  rebuildTargetId: string;
  requestedByUserId?: string | null;
  requestedAtUtc: string;
  scopeCompanyId?: string | null;
  fromOccurredAtUtc?: string | null;
  toOccurredAtUtc?: string | null;
  dryRun: boolean;
  state: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  durationMs?: number | null;
  rowsDeleted: number;
  rowsInserted: number;
  rowsUpdated: number;
  sourceRecordCount?: number | null;
  errorMessage?: string | null;
  notes?: string | null;
  resumeRequired?: boolean;
  lastCheckpointAtUtc?: string | null;
  processedCountAtLastCheckpoint?: number;
  checkpointCount?: number;
  retriedFromOperationId?: string | null;
  rerunReason?: string | null;
  backgroundJobId?: string | null;
}

export interface RebuildProgressDto {
  operationId: string;
  state: string;
  resumeRequired: boolean;
  lastCheckpointAtUtc?: string | null;
  processedCountAtLastCheckpoint: number;
  checkpointCount: number;
  rowsDeleted: number;
  rowsInserted: number;
  rowsUpdated: number;
  sourceRecordCount?: number | null;
}

function unwrap<T>(response: T | { data?: T }): T {
  if (response != null && typeof response === 'object' && 'data' in response)
    return (response as { data: T }).data;
  return response as T;
}

export async function listRebuildTargets(): Promise<RebuildTargetDescriptorDto[]> {
  const response = await apiClient.get<RebuildTargetDescriptorDto[] | { data: RebuildTargetDescriptorDto[] }>('/event-store/rebuild/targets');
  const data = unwrap(response);
  return Array.isArray(data) ? data : [];
}

export async function previewRebuild(request: RebuildRequestDto): Promise<RebuildPreviewResultDto> {
  const body = { ...request, dryRun: true };
  const response = await apiClient.post<RebuildPreviewResultDto | { data: RebuildPreviewResultDto }>('/event-store/rebuild/preview', body);
  return unwrap(response) as RebuildPreviewResultDto;
}

export async function executeRebuild(request: RebuildRequestDto, asyncMode = false): Promise<RebuildExecutionResultDto | { rebuildOperationId: string; message: string }> {
  const url = asyncMode ? '/event-store/rebuild/execute?async=true' : '/event-store/rebuild/execute';
  const response = await apiClient.post<RebuildExecutionResultDto | { rebuildOperationId: string; message: string } | { data: RebuildExecutionResultDto | { rebuildOperationId: string; message: string } }>(url, request);
  const d = unwrap(response);
  return d as RebuildExecutionResultDto | { rebuildOperationId: string; message: string };
}

export async function getRebuildProgress(id: string): Promise<RebuildProgressDto> {
  const response = await apiClient.get<RebuildProgressDto | { data: RebuildProgressDto }>(`/event-store/rebuild/operations/${id}/progress`);
  return unwrap(response) as RebuildProgressDto;
}

export async function resumeRebuildOperation(id: string, asyncMode = false, rerunReason?: string): Promise<RebuildExecutionResultDto | { rebuildOperationId: string; message: string }> {
  const q = new URLSearchParams();
  if (asyncMode) q.set('async', 'true');
  if (rerunReason) q.set('rerunReason', rerunReason);
  const suffix = q.toString() ? `?${q.toString()}` : '';
  const response = await apiClient.post<RebuildExecutionResultDto | { rebuildOperationId: string; message: string } | { data: RebuildExecutionResultDto | { rebuildOperationId: string; message: string } }>(`/event-store/rebuild/operations/${id}/resume${suffix}`);
  const d = unwrap(response);
  return d as RebuildExecutionResultDto | { rebuildOperationId: string; message: string };
}

export async function getRebuildOperation(id: string): Promise<RebuildOperationSummaryDto> {
  const response = await apiClient.get<RebuildOperationSummaryDto | { data: RebuildOperationSummaryDto }>(`/event-store/rebuild/operations/${id}`);
  return unwrap(response) as RebuildOperationSummaryDto;
}

export async function listRebuildOperations(
  page: number = 1,
  pageSize: number = 20,
  state?: string | null,
  rebuildTargetId?: string | null
): Promise<{ items: RebuildOperationSummaryDto[]; total: number; page: number; pageSize: number }> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (state) params.set('state', state);
  if (rebuildTargetId) params.set('rebuildTargetId', rebuildTargetId);
  const response = await apiClient.get<{ items: RebuildOperationSummaryDto[]; total: number; page: number; pageSize: number } | { data: { items: RebuildOperationSummaryDto[]; total: number; page: number; pageSize: number } }>(`/event-store/rebuild/operations?${params}`);
  const d = unwrap(response);
  if (d && typeof d === 'object' && 'items' in d) return d as { items: RebuildOperationSummaryDto[]; total: number; page: number; pageSize: number };
  return { items: [], total: 0, page: 1, pageSize };
}
