import apiClient from './client';
import type {
  TimeSlot,
  CreateTimeSlotRequest,
  UpdateTimeSlotRequest,
  ReorderTimeSlotsRequest,
  SeedDefaultTimeSlotsResponse
} from '../types/timeSlots';

/**
 * Time Slots API
 * Handles appointment time slot management (e.g., "9:00 AM", "2:30 PM")
 */

/**
 * Get all time slots
 * @returns Array of time slots
 */
export const getTimeSlots = async (): Promise<TimeSlot[]> => {
  const response = await apiClient.get<TimeSlot[] | { data: TimeSlot[] }>('/time-slots');
  // Handle both direct array and wrapped response
  if (Array.isArray(response)) {
    return response;
  }
  return (response as { data: TimeSlot[] }).data || [];
};

/**
 * Create a time slot
 * @param timeSlotData - Time slot data (time, sortOrder, isActive)
 * @returns Created time slot
 */
export const createTimeSlot = async (timeSlotData: CreateTimeSlotRequest): Promise<TimeSlot> => {
  const response = await apiClient.post<TimeSlot | { data: TimeSlot }>('/time-slots', timeSlotData);
  // Handle both direct object and wrapped response
  if ('data' in response) {
    return (response as { data: TimeSlot }).data;
  }
  return response as TimeSlot;
};

/**
 * Update a time slot
 * @param id - Time slot ID
 * @param timeSlotData - Time slot update data
 * @returns Updated time slot
 */
export const updateTimeSlot = async (id: string, timeSlotData: UpdateTimeSlotRequest): Promise<TimeSlot> => {
  const response = await apiClient.put<TimeSlot | { data: TimeSlot }>(`/time-slots/${id}`, timeSlotData);
  // Handle both direct object and wrapped response
  if ('data' in response) {
    return (response as { data: TimeSlot }).data;
  }
  return response as TimeSlot;
};

/**
 * Delete a time slot
 * @param id - Time slot ID
 * @returns Promise that resolves when time slot is deleted
 */
export const deleteTimeSlot = async (id: string): Promise<void> => {
  await apiClient.delete(`/time-slots/${id}`);
};

/**
 * Reorder time slots
 * @param timeSlotIds - Array of time slot IDs in new order
 * @returns Promise that resolves when reordering is complete
 */
export const reorderTimeSlots = async (timeSlotIds: string[]): Promise<void> => {
  const request: ReorderTimeSlotsRequest = { timeSlotIds };
  await apiClient.post('/time-slots/reorder', request);
};

/**
 * Seed default time slots
 * @returns Result with count of created slots
 */
export const seedDefaultTimeSlots = async (): Promise<SeedDefaultTimeSlotsResponse> => {
  const response = await apiClient.post<SeedDefaultTimeSlotsResponse | { data: SeedDefaultTimeSlotsResponse }>(
    '/time-slots/seed-defaults'
  );
  // Handle both direct object and wrapped response
  if ('data' in response) {
    return (response as { data: SeedDefaultTimeSlotsResponse }).data;
  }
  return response as SeedDefaultTimeSlotsResponse;
};

