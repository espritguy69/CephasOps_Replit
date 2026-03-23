/**
 * Workflow API – execute transition, allowed transitions.
 * Backend: POST /api/workflow/execute, GET /api/workflow/allowed-transitions.
 */
import { apiClient } from './client';
import type { WorkflowTransition, Location } from '../types/api';

export interface ExecuteTransitionPayload {
  remarks?: string;
  location?: Location;
}

export async function executeTransition(
  orderId: string,
  targetStatus: string,
  payload?: ExecuteTransitionPayload
): Promise<unknown> {
  const body = {
    entityType: 'Order',
    entityId: orderId,
    targetStatus,
    payload: payload?.remarks || payload?.location
      ? { remarks: payload.remarks, location: payload.location }
      : undefined,
  };
  const res = await apiClient.post<unknown>('/workflow/execute', body);
  if (res && typeof res === 'object' && 'data' in (res as object)) {
    return (res as { data: unknown }).data;
  }
  return res;
}

export async function getAllowedTransitions(
  orderId: string,
  currentStatus: string
): Promise<WorkflowTransition[]> {
  const res = await apiClient.get<WorkflowTransition[] | { data: WorkflowTransition[] }>(
    '/workflow/allowed-transitions',
    { params: { entityType: 'Order', entityId: orderId, currentStatus } }
  );
  if (Array.isArray(res)) return res;
  if (res && typeof res === 'object' && 'data' in res) return (res as { data: WorkflowTransition[] }).data;
  return [];
}
