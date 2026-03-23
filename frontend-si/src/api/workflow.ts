import apiClient from './client';
import type { WorkflowTransition, Location } from '../types/api';

/**
 * Workflow API
 * Handles workflow transitions and status changes
 */

export interface TransitionData {
  toStatus: string;
  remarks?: string;
  location?: Location;
  [key: string]: any;
}

/**
 * Execute a workflow transition
 * 
 * ⚠️ FIXED: Changed endpoint from /workflow/execute-transition to /workflow/execute
 * Backend endpoint: POST /api/workflow/execute (WorkflowController.cs line 31)
 * Backend DTO structure: ExecuteTransitionDto { EntityId, EntityType, TargetStatus, Payload? }
 */
export const executeTransition = async (
  orderId: string, 
  toStatus: string, 
  transitionData: Partial<TransitionData> = {}
): Promise<any> => {
  const response = await apiClient.post(`/workflow/execute`, {
    entityType: 'Order',
    entityId: orderId,
    targetStatus: toStatus,
    payload: transitionData.remarks || transitionData.location ? {
      remarks: transitionData.remarks,
      location: transitionData.location,
      ...Object.fromEntries(
        Object.entries(transitionData).filter(([key]) => 
          key !== 'toStatus' && key !== 'remarks' && key !== 'location'
        )
      )
    } : null
  });
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: any }).data;
  }
  return response;
};

/**
 * Get allowed transitions for an order
 */
export const getAllowedTransitions = async (orderId: string): Promise<WorkflowTransition[]> => {
  const response = await apiClient.get<WorkflowTransition[] | { data: WorkflowTransition[] }>(`/workflow/allowed-transitions`, {
    params: {
      entityType: 'Order',
      entityId: orderId
    }
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: WorkflowTransition[] }).data;
  }
  return [];
};

