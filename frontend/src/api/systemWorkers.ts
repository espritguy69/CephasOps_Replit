/**
 * System workers API - worker coordination visibility (distributed worker architecture)
 */
import apiClient from './client';

export interface WorkerInstanceDto {
  id: string;
  hostName: string;
  processId: number;
  role: string;
  startedAtUtc: string;
  lastHeartbeatUtc: string;
  isActive: boolean;
  heartbeatAgeSeconds: number | null;
  isStale: boolean;
}

export interface OwnedReplayOperationDto {
  operationId: string;
  state: string | null;
  claimedAtUtc: string | null;
}

export interface OwnedRebuildOperationDto {
  operationId: string;
  state: string | null;
  claimedAtUtc: string | null;
}

export interface WorkerInstanceDetailDto extends WorkerInstanceDto {
  ownedReplayOperations: OwnedReplayOperationDto[];
  ownedRebuildOperations: OwnedRebuildOperationDto[];
}

export async function listWorkers(): Promise<WorkerInstanceDto[]> {
  const res = await apiClient.get<WorkerInstanceDto[] | { data?: WorkerInstanceDto[] }>('/system/workers');
  if (Array.isArray(res)) return res;
  return (res as { data?: WorkerInstanceDto[] })?.data ?? [];
}

export async function getWorker(id: string): Promise<WorkerInstanceDetailDto | null> {
  const res = await apiClient.get<WorkerInstanceDetailDto | { data?: WorkerInstanceDetailDto }>(`/api/system/workers/${id}`);
  if (res && typeof res === 'object' && 'ownedReplayOperations' in res) return res as WorkerInstanceDetailDto;
  return (res as { data?: WorkerInstanceDetailDto })?.data ?? null;
}
