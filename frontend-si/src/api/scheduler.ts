import apiClient from './client';

/**
 * Scheduler API for SI App
 * Handles reschedule requests and schedule-related operations
 */

export interface RequestRescheduleRequest {
  newDate: string; // ISO date string
  newWindowFrom: string; // TimeSpan format "HH:mm:ss"
  newWindowTo: string; // TimeSpan format "HH:mm:ss"
  reason: string;
  notes?: string;
}

export interface ScheduleSlot {
  id: string;
  orderId: string;
  serviceInstallerId: string;
  date: string;
  windowFrom: string;
  windowTo: string;
  status: string;
  rescheduleRequestedDate?: string;
  rescheduleRequestedTime?: string;
  rescheduleReason?: string;
  rescheduleNotes?: string;
  rescheduleRequestedBySiId?: string;
  rescheduleRequestedAt?: string;
}

/**
 * Get schedule slot for an order
 */
export const getScheduleSlotForOrder = async (orderId: string): Promise<ScheduleSlot | null> => {
  try {
    const response = await apiClient.get<ScheduleSlot[] | { data: ScheduleSlot[] }>('/scheduler/slots', {
      params: { orderId: orderId }
    });
    
    let slots: ScheduleSlot[] = [];
    if (Array.isArray(response)) {
      slots = response;
    } else if (response && typeof response === 'object' && 'data' in response) {
      slots = (response as { data: ScheduleSlot[] }).data;
    }
    
    // Return the first slot for this order (should only be one)
    return slots.length > 0 ? slots[0] : null;
  } catch (error) {
    console.error('Error getting schedule slot:', error);
    return null;
  }
};

/**
 * SI requests reschedule (different day) - updates ScheduledSlot and transitions order to ReschedulePendingApproval via workflow
 */
export const requestReschedule = async (
  slotId: string,
  rescheduleData: RequestRescheduleRequest
): Promise<ScheduleSlot> => {
  const response = await apiClient.post<ScheduleSlot | { data: ScheduleSlot }>(
    `/scheduler/slots/${slotId}/reschedule-request`,
    rescheduleData
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: ScheduleSlot }).data;
  }
  return response as ScheduleSlot;
};

/**
 * Get scheduling conflicts for a slot
 */
export const getConflicts = async (slotId: string): Promise<any[]> => {
  const response = await apiClient.get<any[] | { data: any[] }>('/scheduler/conflicts', {
    params: { slotId }
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: any[] }).data;
  }
  return [];
};

