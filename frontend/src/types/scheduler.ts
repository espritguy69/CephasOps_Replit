/**
 * Scheduler Types - Shared type definitions for Scheduler module
 */

/**
 * Order statuses with their display labels
 * 
 * ⚠️ IMPORTANT: Updated to use PascalCase matching backend OrderStatus enum
 * Source of Truth: backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs
 * Reference: docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md
 */
export const ORDER_STATUSES = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Assigned', label: 'Assigned' },
  { value: 'OnTheWay', label: 'On The Way' },
  { value: 'MetCustomer', label: 'Met Customer' },
  { value: 'OrderCompleted', label: 'Order Completed' },
  { value: 'DocketsReceived', label: 'Dockets Received' },
  { value: 'DocketsVerified', label: 'Dockets Verified' },
  { value: 'DocketsRejected', label: 'Dockets Rejected' },
  { value: 'DocketsUploaded', label: 'Dockets Uploaded' },
  { value: 'ReadyForInvoice', label: 'Ready For Invoice' },
  { value: 'Invoiced', label: 'Invoiced' },
  { value: 'SubmittedToPortal', label: 'Submitted To Portal' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Blocker', label: 'Blocker' },
  { value: 'ReschedulePendingApproval', label: 'Reschedule Pending Approval' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: 'Rejected', label: 'Invoice Rejected' },
  { value: 'Reinvoice', label: 'Reinvoice' }
] as const;

export type OrderStatusValue = typeof ORDER_STATUSES[number]['value'];

// Reschedule reason types
export const RESCHEDULE_REASONS = [
  { value: 'customer_issue', label: 'Customer Issue' },
  { value: 'building_issue', label: 'Building Issue' },
  { value: 'network_issue', label: 'Network Issue' }
] as const;

export type RescheduleReason = typeof RESCHEDULE_REASONS[number]['value'];

export interface CalendarSlot {
  id: string;
  orderId: string;
  serviceInstallerId: string;
  date: string;
  windowFrom: string; // TimeSpan format "HH:mm:ss"
  windowTo: string; // TimeSpan format "HH:mm:ss"
  plannedTravelMin?: number;
  sequenceIndex: number;
  status: string; // Draft, Confirmed, Posted, RescheduleRequested, RescheduleApproved, RescheduleRejected, etc.
  createdByUserId: string;
  createdAt: string;
  // Confirmation and posting tracking
  confirmedByUserId?: string;
  confirmedAt?: string;
  postedByUserId?: string;
  postedAt?: string;
  // Reschedule request fields
  rescheduleRequestedDate?: string;
  rescheduleRequestedTime?: string;
  rescheduleReason?: string;
  rescheduleNotes?: string;
  rescheduleRequestedBySiId?: string;
  rescheduleRequestedAt?: string;
  // Enriched order details
  serviceId?: string;
  ticketId?: string;
  externalRef?: string;
  customerName?: string;
  buildingName?: string;
  partnerName?: string;
  partnerId?: string;
  /** Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). */
  derivedPartnerCategoryLabel?: string;
  orderStatus?: string;
  orderNumber?: string;
  // Address info
  address?: string;
  fullAddress?: string;
  customerPhone?: string;
  customerEmail?: string;
  serviceType?: string;
  // Enriched SI details
  serviceInstallerName?: string;
  serviceInstallerIsSubcontractor?: boolean;
  serviceInstallerSiLevel?: string;
  // Legacy fields for backward compatibility
  siId?: string;
  siName?: string;
  startTime?: string;
  endTime?: string;
  duration?: number;
  notes?: string;
}

// Extended order interface for scheduler
export interface SchedulerOrder {
  id: string;
  orderNumber?: string;
  serviceId?: string;
  ticketId?: string;
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  buildingName?: string;
  address?: string;
  fullAddress?: string;
  appointmentDate?: string;
  appointmentTime?: string;
  status: string;
  serviceType?: string;
  partnerName?: string;
  partnerId?: string;
  /** Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). */
  derivedPartnerCategoryLabel?: string;
  assignedSiId?: string;
  assignedToName?: string;
}

export interface AvailableSlot {
  id: string;
  siId: string;
  siName?: string;
  date: string;
  startTime: string;
  endTime: string;
  duration: number;
  isAvailable: boolean;
}

export interface CreateSlotRequest {
  orderId?: string;
  serviceInstallerId: string;
  date: string;
  windowFrom: string;
  windowTo: string;
  // Legacy fields
  siId?: string;
  startTime?: string;
  endTime?: string;
  duration?: number;
  notes?: string;
}

export interface UpdateSlotRequest {
  orderId?: string;
  siId?: string;
  startTime?: string;
  endTime?: string;
  duration?: number;
  notes?: string;
  /** Backend-aligned: use for move/resize (ISO date, HH:mm:ss) */
  serviceInstallerId?: string;
  date?: string;
  windowFrom?: string;
  windowTo?: string;
}

export interface SIAvailability {
  id: string;
  siId: string;
  siName?: string;
  date: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  notes?: string;
}

export interface CreateSIAvailabilityRequest {
  siId: string;
  date: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  notes?: string;
}

export interface UpdateSIAvailabilityRequest {
  date?: string;
  startTime?: string;
  endTime?: string;
  isAvailable?: boolean;
  notes?: string;
}

export interface LeaveRequest {
  id: string;
  siId: string;
  siName?: string;
  startDate: string;
  endDate: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  notes?: string;
  requestedAt?: string;
  reviewedAt?: string;
  reviewedBy?: string;
}

export interface CreateLeaveRequestRequest {
  siId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface UpdateLeaveRequestStatusRequest {
  status: 'Approved' | 'Rejected';
  notes?: string;
}

export interface SILoadDistribution {
  siId: string;
  siName?: string;
  totalSlots: number;
  totalHours: number;
  averageLoad: number;
}

export interface CalendarFilters {
  fromDate?: string;
  toDate?: string;
  departmentId?: string;
}

/** Filters for GET /scheduler/utilization (flattened slots by date range). */
export interface UtilizationFilters {
  fromDate: string;
  toDate: string;
  departmentId?: string;
  siId?: string;
}

export interface SlotFilters {
  date?: string;
  siId?: string;
  duration?: number;
}

export interface SIAvailabilityFilters {
  siId?: string;
  startDate?: string;
  endDate?: string;
}

export interface LeaveRequestFilters {
  siId?: string;
  status?: 'Pending' | 'Approved' | 'Rejected';
  startDate?: string;
  endDate?: string;
}

export interface SILoadFilters {
  date?: string;
  departmentId?: string;
}

export interface UnassignedOrderFilters {
  partnerId?: string;
  fromDate?: string;
  toDate?: string;
}

// Dialog state interfaces
export interface TimeChangeDialogState {
  open: boolean;
  orderId: string | null;
  currentTime: string;
}

export interface CompletionConfirmDialogState {
  open: boolean;
  orderId: string | null;
  orderNumber: string;
  customerName: string;
}

export interface RescheduleDialogState {
  open: boolean;
  orderId: string | null;
  orderNumber: string;
  customerName: string;
  currentDate: string;
  currentTime: string;
}

// Installer for drag-and-drop
export interface DraggableInstallerData {
  installerId: string;
  installerName: string;
}

// Schedule conflict interface
export interface ScheduleConflict {
  slotId: string;
  orderId: string;
  serviceInstallerId: string;
  date: string;
  windowFrom: string; // TimeSpan format "HH:mm:ss"
  windowTo: string; // TimeSpan format "HH:mm:ss"
  status: string;
  orderServiceId?: string;
  orderCustomerName?: string;
  orderBuildingName?: string;
  conflictType: string; // TimeOverlap, etc.
  conflictDescription: string;
}
