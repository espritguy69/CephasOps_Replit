/**
 * Workflow Status Constants - Single Source of Truth for Frontend
 * 
 * ⚠️ IMPORTANT: These must match backend OrderStatus enum exactly (PascalCase, case-sensitive)
 * Source of Truth: backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs
 * Reference: docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md
 * 
 * This file serves as the consolidated reference for all workflow statuses.
 * Use these constants throughout the frontend to ensure consistency.
 */

// ============================================================================
// ORDER WORKFLOW STATUSES (17 Total)
// ============================================================================

/**
 * Main flow statuses (12) - Primary order lifecycle
 */
export const ORDER_STATUS_MAIN_FLOW = {
  Pending: 'Pending',
  Assigned: 'Assigned',
  OnTheWay: 'OnTheWay',
  MetCustomer: 'MetCustomer',
  OrderCompleted: 'OrderCompleted',
  DocketsReceived: 'DocketsReceived',
  DocketsVerified: 'DocketsVerified',
  DocketsUploaded: 'DocketsUploaded',
  ReadyForInvoice: 'ReadyForInvoice',
  Invoiced: 'Invoiced',
  SubmittedToPortal: 'SubmittedToPortal',
  Completed: 'Completed'
} as const;

/**
 * Side states (5) - Alternative paths or terminal states
 */
export const ORDER_STATUS_SIDE_STATES = {
  Blocker: 'Blocker',
  ReschedulePendingApproval: 'ReschedulePendingApproval',
  DocketsRejected: 'DocketsRejected',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Reinvoice: 'Reinvoice'
} as const;

/**
 * All order statuses combined
 */
export const ORDER_STATUS = {
  ...ORDER_STATUS_MAIN_FLOW,
  ...ORDER_STATUS_SIDE_STATES
} as const;

export type OrderStatus = typeof ORDER_STATUS[keyof typeof ORDER_STATUS];

/**
 * Order status display names (for UI labels)
 */
export const ORDER_STATUS_DISPLAY_NAMES: Record<OrderStatus, string> = {
  Pending: 'Pending',
  Assigned: 'Assigned',
  OnTheWay: 'On The Way',
  MetCustomer: 'Met Customer',
  OrderCompleted: 'Order Completed',
  DocketsReceived: 'Dockets Received',
  DocketsVerified: 'Dockets Verified',
  DocketsRejected: 'Dockets Rejected',
  DocketsUploaded: 'Dockets Uploaded',
  ReadyForInvoice: 'Ready For Invoice',
  Invoiced: 'Invoiced',
  SubmittedToPortal: 'Submitted To Portal',
  Completed: 'Completed',
  Blocker: 'Blocker',
  ReschedulePendingApproval: 'Reschedule Pending Approval',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Reinvoice: 'Reinvoice'
};

/**
 * Order status phases (for grouping/filtering)
 */
export const ORDER_STATUS_PHASES = {
  Creation: ['Pending'],
  FieldWork: ['Assigned', 'OnTheWay', 'MetCustomer', 'OrderCompleted', 'Blocker', 'ReschedulePendingApproval'],
  Documentation: ['DocketsReceived', 'DocketsVerified', 'DocketsRejected', 'DocketsUploaded'],
  Billing: ['ReadyForInvoice', 'Invoiced', 'SubmittedToPortal', 'Reinvoice'],
  Closure: ['Completed', 'Cancelled', 'Rejected']
} as const;

/**
 * Standard order flow sequence (main path only)
 */
export const ORDER_FLOW_SEQUENCE: OrderStatus[] = [
  'Pending',
  'Assigned',
  'OnTheWay',
  'MetCustomer',
  'OrderCompleted',
  'DocketsReceived',
  'DocketsVerified',
  'DocketsRejected',
  'DocketsUploaded',
  'ReadyForInvoice',
  'Invoiced',
  'SubmittedToPortal',
  'Completed'
];

// ============================================================================
// RMA WORKFLOW STATUSES (11 Total)
// ============================================================================

export const RMA_STATUS = {
  RMARequested: 'RMARequested',
  RMAPendingReview: 'RMAPendingReview',
  RMAMraReceived: 'RMAMraReceived',
  RMAApproved: 'RMAApproved',
  RMAInTransit: 'RMAInTransit',
  RMAAtPartner: 'RMAAtPartner',
  RMARepaired: 'RMARepaired',
  RMAReplaced: 'RMAReplaced',
  RMACredited: 'RMACredited',
  RMAScrapped: 'RMAScrapped',
  RMAClosed: 'RMAClosed'
} as const;

export type RmaStatus = typeof RMA_STATUS[keyof typeof RMA_STATUS];

// ============================================================================
// KPI WORKFLOW STATUSES (14 Total)
// ============================================================================

export const KPI_STATUS = {
  // SI Performance (5)
  KpiPending: 'KpiPending',
  KpiOnTime: 'KpiOnTime',
  KpiLate: 'KpiLate',
  KpiExceededSla: 'KpiExceededSla',
  KpiExcused: 'KpiExcused',
  
  // Admin Performance (6)
  KpiDocketPending: 'KpiDocketPending',
  KpiDocketOnTime: 'KpiDocketOnTime',
  KpiDocketLate: 'KpiDocketLate',
  KpiInvoicePending: 'KpiInvoicePending',
  KpiInvoiceOnTime: 'KpiInvoiceOnTime',
  KpiInvoiceLate: 'KpiInvoiceLate',
  
  // Employer Review (3)
  KpiEmployerPending: 'KpiEmployerPending',
  KpiEmployerApproved: 'KpiEmployerApproved',
  KpiEmployerFlagged: 'KpiEmployerFlagged'
} as const;

export type KpiStatus = typeof KPI_STATUS[keyof typeof KPI_STATUS];

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/**
 * Check if a status is a valid order status
 */
export function isValidOrderStatus(status: string): status is OrderStatus {
  return status in ORDER_STATUS;
}

/**
 * Get display name for a status
 */
export function getStatusDisplayName(status: string): string {
  return ORDER_STATUS_DISPLAY_NAMES[status as OrderStatus] || status;
}

/**
 * Check if status is in main flow
 */
export function isMainFlowStatus(status: string): boolean {
  return status in ORDER_STATUS_MAIN_FLOW;
}

/**
 * Check if status is a side state
 */
export function isSideStateStatus(status: string): boolean {
  return status in ORDER_STATUS_SIDE_STATES;
}

/**
 * Get phase for a status
 */
export function getStatusPhase(status: string): string | null {
  for (const [phase, statuses] of Object.entries(ORDER_STATUS_PHASES)) {
    if (statuses.includes(status as OrderStatus)) {
      return phase;
    }
  }
  return null;
}

/**
 * Get next status in main flow sequence
 */
export function getNextStatusInFlow(currentStatus: OrderStatus): OrderStatus | null {
  const currentIndex = ORDER_FLOW_SEQUENCE.indexOf(currentStatus);
  if (currentIndex === -1 || currentIndex === ORDER_FLOW_SEQUENCE.length - 1) {
    return null;
  }
  return ORDER_FLOW_SEQUENCE[currentIndex + 1];
}

/**
 * Get previous status in main flow sequence
 */
export function getPreviousStatusInFlow(currentStatus: OrderStatus): OrderStatus | null {
  const currentIndex = ORDER_FLOW_SEQUENCE.indexOf(currentStatus);
  if (currentIndex <= 0) {
    return null;
  }
  return ORDER_FLOW_SEQUENCE[currentIndex - 1];
}

