import apiClient from './client';
import type {
  CalendarSlot,
  AvailableSlot,
  CreateSlotRequest,
  UpdateSlotRequest,
  SIAvailability,
  CreateSIAvailabilityRequest,
  UpdateSIAvailabilityRequest,
  LeaveRequest,
  CreateLeaveRequestRequest,
  UpdateLeaveRequestStatusRequest,
  SILoadDistribution,
  CalendarFilters,
  UtilizationFilters,
  SlotFilters,
  SIAvailabilityFilters,
  LeaveRequestFilters,
  SILoadFilters,
  UnassignedOrderFilters,
  ScheduleConflict
} from '../types/scheduler';

/**
 * Scheduler API
 * Handles calendar scheduling, SI availability, slots, and leave requests
 */

/**
 * Get calendar view with scheduled slots
 * @param filters - Optional filters (fromDate, toDate)
 * @returns Array of scheduled slots
 */
export const getCalendar = async (filters: CalendarFilters = {}): Promise<CalendarSlot[]> => {
  const response = await apiClient.get<CalendarSlot[]>('/scheduler/calendar', { params: filters });
  return response;
};

/**
 * Get scheduler utilization: flattened schedule slots for a date range (dedicated GET endpoint).
 * Same data as Reports Hub "scheduler-utilization" report.
 */
export const getUtilization = async (filters: UtilizationFilters): Promise<CalendarSlot[]> => {
  const response = await apiClient.get<CalendarSlot[]>('/scheduler/utilization', { params: filters });
  return response;
};

/**
 * Get available slots
 * @param filters - Optional filters (date, siId, duration)
 * @returns Array of available slots
 */
export const getAvailableSlots = async (filters: SlotFilters = {}): Promise<AvailableSlot[]> => {
  const response = await apiClient.get<AvailableSlot[]>('/scheduler/slots', { params: filters });
  return response;
};

/**
 * Create a scheduled slot
 * @param slotData - Slot creation data
 * @returns Created slot
 */
export const createSlot = async (slotData: CreateSlotRequest): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>('/scheduler/slots', slotData);
  return response;
};

/**
 * Update scheduled slot
 * @param slotId - Slot ID
 * @param slotData - Slot update data
 * @returns Updated slot
 */
export const updateSlot = async (slotId: string, slotData: UpdateSlotRequest): Promise<CalendarSlot> => {
  const response = await apiClient.put<CalendarSlot>(`/scheduler/slots/${slotId}`, slotData);
  return response;
};

/**
 * Delete scheduled slot
 * @param slotId - Slot ID
 * @returns Promise that resolves when slot is deleted
 */
export const deleteSlot = async (slotId: string): Promise<void> => {
  await apiClient.delete(`/scheduler/slots/${slotId}`);
};

/**
 * Get SI availability
 * @param filters - Optional filters (siId, startDate, endDate)
 * @returns Array of SI availability records
 */
export const getSIAvailability = async (filters: SIAvailabilityFilters = {}): Promise<SIAvailability[]> => {
  const response = await apiClient.get<SIAvailability[]>('/scheduler/availability', { params: filters });
  return response;
};

/**
 * Set SI availability
 * @param availabilityData - Availability data
 * @returns Availability record
 */
export const setSIAvailability = async (availabilityData: CreateSIAvailabilityRequest): Promise<SIAvailability> => {
  const response = await apiClient.post<SIAvailability>('/scheduler/availability', availabilityData);
  return response;
};

/**
 * Update SI availability
 * @param availabilityId - Availability ID
 * @param availabilityData - Availability update data
 * @returns Updated availability
 */
export const updateSIAvailability = async (
  availabilityId: string,
  availabilityData: UpdateSIAvailabilityRequest
): Promise<SIAvailability> => {
  const response = await apiClient.put<SIAvailability>(`/scheduler/availability/${availabilityId}`, availabilityData);
  return response;
};

/**
 * Get leave requests
 * @param filters - Optional filters (siId, status, startDate, endDate)
 * @returns Array of leave requests
 */
export const getLeaveRequests = async (filters: LeaveRequestFilters = {}): Promise<LeaveRequest[]> => {
  const response = await apiClient.get<LeaveRequest[]>('/scheduler/leave-requests', { params: filters });
  return response;
};

/**
 * Create leave request
 * @param leaveData - Leave request data
 * @returns Created leave request
 */
export const createLeaveRequest = async (leaveData: CreateLeaveRequestRequest): Promise<LeaveRequest> => {
  const response = await apiClient.post<LeaveRequest>('/scheduler/leave-requests', leaveData);
  return response;
};

/**
 * Update leave request status
 * @param leaveRequestId - Leave request ID
 * @param status - New status (Approved, Rejected)
 * @param notes - Optional notes
 * @returns Updated leave request
 */
export const updateLeaveRequestStatus = async (
  leaveRequestId: string,
  status: 'Approved' | 'Rejected',
  notes: string | null = null
): Promise<LeaveRequest> => {
  const request: UpdateLeaveRequestStatusRequest = { status };
  if (notes) request.notes = notes;
  const response = await apiClient.patch<LeaveRequest>(`/scheduler/leave-requests/${leaveRequestId}/status`, request);
  return response;
};

/**
 * Get SI load distribution
 * @param filters - Optional filters (date, departmentId)
 * @returns Array of SI load data
 */
export const getSILoadDistribution = async (filters: SILoadFilters = {}): Promise<SILoadDistribution[]> => {
  const response = await apiClient.get<SILoadDistribution[]>('/scheduler/si-load', { params: filters });
  return response;
};

/**
 * Get unassigned orders (pending orders not yet scheduled)
 * @param filters - Optional filters (partnerId, fromDate, toDate)
 * @returns Array of unassigned orders
 */
export const getUnassignedOrders = async (filters: UnassignedOrderFilters = {}): Promise<any[]> => {
  const response = await apiClient.get<any[]>('/scheduler/unassigned-orders', { params: filters });
  return response;
};

/**
 * Block an order
 * @param orderId - Order ID
 * @param blockerData - Blocker data (blockerType, description, raisedBySiId)
 * @returns Promise that resolves when order is blocked
 */
export const blockOrder = async (orderId: string, blockerData: {
  blockerType: string;
  description: string;
  raisedBySiId?: string;
}): Promise<void> => {
  await apiClient.post(`/scheduler/orders/${orderId}/block`, blockerData);
};

/**
 * Confirm schedule (changes ScheduledSlot status from Draft to Confirmed)
 * @param slotId - Slot ID
 * @returns Confirmed schedule slot
 */
export const confirmSchedule = async (slotId: string): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/confirm`);
  return response;
};

/**
 * Post schedule to SI (changes ScheduledSlot status from Confirmed to Posted and triggers order status change via workflow)
 * @param slotId - Slot ID
 * @returns Posted schedule slot
 */
export const postScheduleToSI = async (slotId: string): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/post`);
  return response;
};

/**
 * Return schedule to Draft (reverts Confirmed back to Draft)
 * @param slotId - Slot ID
 * @returns Schedule slot returned to Draft
 */
export const returnScheduleToDraft = async (slotId: string): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/return-to-draft`);
  return response;
};

/**
 * SI requests reschedule (different day) - updates ScheduledSlot and transitions order to ReschedulePendingApproval via workflow
 * @param slotId - Slot ID
 * @param rescheduleData - Reschedule request data
 * @returns Updated schedule slot
 */
export const requestReschedule = async (slotId: string, rescheduleData: {
  newDate: string; // ISO date string
  newWindowFrom: string; // TimeSpan format "HH:mm:ss"
  newWindowTo: string; // TimeSpan format "HH:mm:ss"
  reason: string;
  notes?: string;
}): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/reschedule-request`, rescheduleData);
  return response;
};

/**
 * Admin approves reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
 * @param slotId - Slot ID
 * @returns Updated schedule slot
 */
export const approveReschedule = async (slotId: string): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/reschedule-approve`);
  return response;
};

/**
 * Admin rejects reschedule - updates ScheduledSlot and transitions order back to Assigned via workflow
 * @param slotId - Slot ID
 * @param rejectionReason - Reason for rejection
 * @returns Updated schedule slot
 */
export const rejectReschedule = async (slotId: string, rejectionReason: string): Promise<CalendarSlot> => {
  const response = await apiClient.post<CalendarSlot>(`/scheduler/slots/${slotId}/reschedule-reject`, {
    rejectionReason
  });
  return response;
};

/**
 * Get scheduling conflicts for a given order or slot
 * @param filters - Optional filters (orderId, slotId, siId, date)
 * @returns Array of conflicts
 */
export const getConflicts = async (filters: {
  orderId?: string;
  slotId?: string;
  siId?: string;
  date?: string; // ISO date string
} = {}): Promise<ScheduleConflict[]> => {
  const response = await apiClient.get<ScheduleConflict[]>('/scheduler/conflicts', { params: filters });
  return response;
};

