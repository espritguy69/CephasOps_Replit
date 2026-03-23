/**
 * Time Slot Types - Shared type definitions for Time Slots module
 */

export interface TimeSlot {
  id: string;
  time: string; // Format: "HH:mm" (e.g., "09:00", "14:30")
  displayTime?: string; // Format: "9:00 AM", "2:30 PM"
  sortOrder: number;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateTimeSlotRequest {
  time: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface UpdateTimeSlotRequest {
  time?: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface ReorderTimeSlotsRequest {
  timeSlotIds: string[];
}

export interface SeedDefaultTimeSlotsResponse {
  count: number;
  message?: string;
}

