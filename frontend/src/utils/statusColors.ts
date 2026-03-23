/**
 * Status Color Utilities
 * Centralized color definitions for status badges, priority badges, and card backgrounds
 * Used across the entire CephasOps frontend for consistent styling
 * 
 * ⚠️ IMPORTANT: Updated to support PascalCase (primary) with backward compatibility for snake_case
 * Source of Truth: backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs
 * Reference: docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md
 * 
 * Per SCHEDULER_MODULE.md documentation:
 * - Grey: Pending (unassigned, in backlog panel)
 * - Blue: Assigned
 * - Yellow: OnTheWay / MetCustomer
 * - Green: OrderCompleted (waiting for docket)
 * - Red: Blocked / Overdue
 */

// ============================================================================
// Order Status Colors (aligned with SCHEDULER_MODULE.md documentation)
// ============================================================================

export const ORDER_STATUS_COLORS: Record<string, string> = {
  // PascalCase (primary - matches backend)
  Pending: 'bg-gray-100 text-gray-800 border-gray-300',
  Assigned: 'bg-blue-100 text-blue-800 border-blue-300',
  OnTheWay: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  MetCustomer: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  OrderCompleted: 'bg-green-100 text-green-800 border-green-300',
  DocketsReceived: 'bg-teal-100 text-teal-800 border-teal-300',
  DocketsVerified: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  DocketsRejected: 'bg-red-100 text-red-800 border-red-300',
  DocketsUploaded: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  ReadyForInvoice: 'bg-indigo-100 text-indigo-800 border-indigo-300',
  Invoiced: 'bg-violet-100 text-violet-800 border-violet-300',
  SubmittedToPortal: 'bg-purple-100 text-purple-800 border-purple-300',
  Completed: 'bg-green-600 text-white border-green-700',
  Blocker: 'bg-red-100 text-red-800 border-red-300',
  ReschedulePendingApproval: 'bg-amber-100 text-amber-800 border-amber-300',
  Rejected: 'bg-red-100 text-red-800 border-red-300',
  Cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
  Reinvoice: 'bg-amber-100 text-amber-800 border-amber-300',
  
  // Backward compatibility: snake_case (will be deprecated)
  pending: 'bg-gray-100 text-gray-800 border-gray-300',
  assigned: 'bg-blue-100 text-blue-800 border-blue-300',
  on_the_way: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  ontheway: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  met_customer: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  metcustomer: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  order_completed: 'bg-green-100 text-green-800 border-green-300',
  ordercompleted: 'bg-green-100 text-green-800 border-green-300',
  docket_received: 'bg-teal-100 text-teal-800 border-teal-300',
  docketsreceived: 'bg-teal-100 text-teal-800 border-teal-300',
  docket_uploaded: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  docketsuploaded: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  ready_to_invoice: 'bg-indigo-100 text-indigo-800 border-indigo-300',
  readyforinvoice: 'bg-indigo-100 text-indigo-800 border-indigo-300',
  invoiced: 'bg-violet-100 text-violet-800 border-violet-300',
  completed: 'bg-green-600 text-white border-green-700',
  blocker: 'bg-red-100 text-red-800 border-red-300',
  blocked: 'bg-red-100 text-red-800 border-red-300',
  overdue: 'bg-red-100 text-red-800 border-red-300',
  customer_issue: 'bg-red-100 text-red-800 border-red-300',
  building_issue: 'bg-red-100 text-red-800 border-red-300',
  network_issue: 'bg-red-100 text-red-800 border-red-300',
  rescheduled: 'bg-purple-100 text-purple-800 border-purple-300',
  reschedule_pending_approval: 'bg-amber-100 text-amber-800 border-amber-300',
  reschedulependingapproval: 'bg-amber-100 text-amber-800 border-amber-300',
  withdrawn: 'bg-gray-100 text-gray-600 border-gray-300',
  cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
  rejected: 'bg-red-100 text-red-800 border-red-300',
};

// Status colors for filter buttons (active state = solid, inactive = light)
// Aligned with SCHEDULER_MODULE.md documentation
// ⚠️ Updated to support PascalCase (primary) with backward compatibility
export const ORDER_STATUS_BUTTON_COLORS: Record<string, { active: string; inactive: string }> = {
  // PascalCase (primary - matches backend)
  Pending: {
    active: 'bg-gray-600 text-white border-gray-700 hover:bg-gray-700',
    inactive: 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300',
  },
  Assigned: {
    active: 'bg-blue-600 text-white border-blue-700 hover:bg-blue-700',
    inactive: 'bg-blue-100 hover:bg-blue-200 text-blue-700 border-blue-300',
  },
  OnTheWay: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  MetCustomer: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  OrderCompleted: {
    active: 'bg-green-500 text-white border-green-600 hover:bg-green-600',
    inactive: 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300',
  },
  DocketsReceived: {
    active: 'bg-teal-500 text-white border-teal-600 hover:bg-teal-600',
    inactive: 'bg-teal-100 hover:bg-teal-200 text-teal-700 border-teal-300',
  },
  DocketsVerified: {
    active: 'bg-cyan-500 text-white border-cyan-600 hover:bg-cyan-600',
    inactive: 'bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border-cyan-300',
  },
  DocketsRejected: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  DocketsUploaded: {
    active: 'bg-cyan-500 text-white border-cyan-600 hover:bg-cyan-600',
    inactive: 'bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border-cyan-300',
  },
  ReadyForInvoice: {
    active: 'bg-indigo-500 text-white border-indigo-600 hover:bg-indigo-600',
    inactive: 'bg-indigo-100 hover:bg-indigo-200 text-indigo-700 border-indigo-300',
  },
  Invoiced: {
    active: 'bg-violet-500 text-white border-violet-600 hover:bg-violet-600',
    inactive: 'bg-violet-100 hover:bg-violet-200 text-violet-700 border-violet-300',
  },
  SubmittedToPortal: {
    active: 'bg-purple-500 text-white border-purple-600 hover:bg-purple-600',
    inactive: 'bg-purple-100 hover:bg-purple-200 text-purple-700 border-purple-300',
  },
  Completed: {
    active: 'bg-green-600 text-white border-green-700 hover:bg-green-700',
    inactive: 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300',
  },
  Blocker: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  ReschedulePendingApproval: {
    active: 'bg-amber-500 text-white border-amber-600 hover:bg-amber-600',
    inactive: 'bg-amber-100 hover:bg-amber-200 text-amber-700 border-amber-300',
  },
  Cancelled: {
    active: 'bg-gray-500 text-white border-gray-600 hover:bg-gray-600',
    inactive: 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300',
  },
  Rejected: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  Reinvoice: {
    active: 'bg-amber-500 text-white border-amber-600 hover:bg-amber-600',
    inactive: 'bg-amber-100 hover:bg-amber-200 text-amber-700 border-amber-300',
  },
  
  // Backward compatibility: snake_case (will be deprecated)
  // Grey: Pending
  pending: {
    active: 'bg-gray-600 text-white border-gray-700 hover:bg-gray-700',
    inactive: 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300',
  },
  // Blue: Assigned
  assigned: {
    active: 'bg-blue-600 text-white border-blue-700 hover:bg-blue-700',
    inactive: 'bg-blue-100 hover:bg-blue-200 text-blue-700 border-blue-300',
  },
  // Yellow: OnTheWay / MetCustomer
  on_the_way: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  ontheway: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  met_customer: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  metcustomer: {
    active: 'bg-yellow-500 text-white border-yellow-600 hover:bg-yellow-600',
    inactive: 'bg-yellow-100 hover:bg-yellow-200 text-yellow-700 border-yellow-300',
  },
  // Green: OrderCompleted
  order_completed: {
    active: 'bg-green-500 text-white border-green-600 hover:bg-green-600',
    inactive: 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300',
  },
  ordercompleted: {
    active: 'bg-green-500 text-white border-green-600 hover:bg-green-600',
    inactive: 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300',
  },
  // Post-completion statuses
  docket_received: {
    active: 'bg-teal-500 text-white border-teal-600 hover:bg-teal-600',
    inactive: 'bg-teal-100 hover:bg-teal-200 text-teal-700 border-teal-300',
  },
  docketsreceived: {
    active: 'bg-teal-500 text-white border-teal-600 hover:bg-teal-600',
    inactive: 'bg-teal-100 hover:bg-teal-200 text-teal-700 border-teal-300',
  },
  docket_uploaded: {
    active: 'bg-cyan-500 text-white border-cyan-600 hover:bg-cyan-600',
    inactive: 'bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border-cyan-300',
  },
  docketsuploaded: {
    active: 'bg-cyan-500 text-white border-cyan-600 hover:bg-cyan-600',
    inactive: 'bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border-cyan-300',
  },
  ready_to_invoice: {
    active: 'bg-indigo-500 text-white border-indigo-600 hover:bg-indigo-600',
    inactive: 'bg-indigo-100 hover:bg-indigo-200 text-indigo-700 border-indigo-300',
  },
  readyforinvoice: {
    active: 'bg-indigo-500 text-white border-indigo-600 hover:bg-indigo-600',
    inactive: 'bg-indigo-100 hover:bg-indigo-200 text-indigo-700 border-indigo-300',
  },
  invoiced: {
    active: 'bg-violet-500 text-white border-violet-600 hover:bg-violet-600',
    inactive: 'bg-violet-100 hover:bg-violet-200 text-violet-700 border-violet-300',
  },
  completed: {
    active: 'bg-green-600 text-white border-green-700 hover:bg-green-700',
    inactive: 'bg-green-100 hover:bg-green-200 text-green-700 border-green-300',
  },
  // Red: Blocked / Overdue / Issues
  blocker: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  blocked: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  overdue: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  customer_issue: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  building_issue: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  network_issue: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
  // Reschedule/Approval
  rescheduled: {
    active: 'bg-purple-500 text-white border-purple-600 hover:bg-purple-600',
    inactive: 'bg-purple-100 hover:bg-purple-200 text-purple-700 border-purple-300',
  },
  reschedule_pending_approval: {
    active: 'bg-amber-500 text-white border-amber-600 hover:bg-amber-600',
    inactive: 'bg-amber-100 hover:bg-amber-200 text-amber-700 border-amber-300',
  },
  reschedulependingapproval: {
    active: 'bg-amber-500 text-white border-amber-600 hover:bg-amber-600',
    inactive: 'bg-amber-100 hover:bg-amber-200 text-amber-700 border-amber-300',
  },
  // Terminal/Cancelled
  withdrawn: {
    active: 'bg-gray-500 text-white border-gray-600 hover:bg-gray-600',
    inactive: 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300',
  },
  cancelled: {
    active: 'bg-gray-500 text-white border-gray-600 hover:bg-gray-600',
    inactive: 'bg-gray-100 hover:bg-gray-200 text-gray-700 border-gray-300',
  },
  rejected: {
    active: 'bg-red-500 text-white border-red-600 hover:bg-red-600',
    inactive: 'bg-red-100 hover:bg-red-200 text-red-700 border-red-300',
  },
};

// ============================================================================
// Priority Colors
// ============================================================================

export const PRIORITY_COLORS: Record<string, string> = {
  high: 'bg-red-100 text-red-800 border-red-300',
  medium: 'bg-orange-100 text-orange-800 border-orange-300',
  low: 'bg-green-100 text-green-800 border-green-300',
  urgent: 'bg-red-600 text-white border-red-700',
  normal: 'bg-blue-100 text-blue-800 border-blue-300',
};

// ============================================================================
// Generic Status Colors (for other entities)
// ============================================================================

export const GENERIC_STATUS_COLORS: Record<string, string> = {
  active: 'bg-green-100 text-green-800 border-green-300',
  inactive: 'bg-gray-100 text-gray-800 border-gray-300',
  draft: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  published: 'bg-blue-100 text-blue-800 border-blue-300',
  archived: 'bg-gray-100 text-gray-600 border-gray-300',
  approved: 'bg-green-100 text-green-800 border-green-300',
  rejected: 'bg-red-100 text-red-800 border-red-300',
  pending_approval: 'bg-amber-100 text-amber-800 border-amber-300',
  processing: 'bg-blue-100 text-blue-800 border-blue-300',
  failed: 'bg-red-100 text-red-800 border-red-300',
  success: 'bg-green-100 text-green-800 border-green-300',
  cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
  on_hold: 'bg-amber-100 text-amber-800 border-amber-300',
  in_progress: 'bg-blue-100 text-blue-800 border-blue-300',
  open: 'bg-blue-100 text-blue-800 border-blue-300',
  closed: 'bg-gray-100 text-gray-600 border-gray-300',
  resolved: 'bg-green-100 text-green-800 border-green-300',
};

// ============================================================================
// Invoice Status Colors
// ============================================================================

export const INVOICE_STATUS_COLORS: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-800 border-gray-300',
  sent: 'bg-blue-100 text-blue-800 border-blue-300',
  paid: 'bg-green-100 text-green-800 border-green-300',
  partial: 'bg-amber-100 text-amber-800 border-amber-300',
  overdue: 'bg-red-100 text-red-800 border-red-300',
  cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
  void: 'bg-gray-100 text-gray-600 border-gray-300',
};

// ============================================================================
// Payroll Status Colors
// ============================================================================

export const PAYROLL_STATUS_COLORS: Record<string, string> = {
  draft: 'bg-gray-100 text-gray-800 border-gray-300',
  pending: 'bg-amber-100 text-amber-800 border-amber-300',
  approved: 'bg-blue-100 text-blue-800 border-blue-300',
  processing: 'bg-indigo-100 text-indigo-800 border-indigo-300',
  paid: 'bg-green-100 text-green-800 border-green-300',
  failed: 'bg-red-100 text-red-800 border-red-300',
  cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
};

// ============================================================================
// RMA Status Colors
// ============================================================================

export const RMA_STATUS_COLORS: Record<string, string> = {
  requested: 'bg-amber-100 text-amber-800 border-amber-300',
  approved: 'bg-blue-100 text-blue-800 border-blue-300',
  rejected: 'bg-red-100 text-red-800 border-red-300',
  shipped: 'bg-indigo-100 text-indigo-800 border-indigo-300',
  received: 'bg-teal-100 text-teal-800 border-teal-300',
  inspected: 'bg-cyan-100 text-cyan-800 border-cyan-300',
  repaired: 'bg-lime-100 text-lime-800 border-lime-300',
  replaced: 'bg-emerald-100 text-emerald-800 border-emerald-300',
  closed: 'bg-green-100 text-green-800 border-green-300',
  cancelled: 'bg-gray-100 text-gray-600 border-gray-300',
};

// ============================================================================
// Card Background Colors (for order cards based on WO type)
// ============================================================================

export const CARD_BACKGROUND_COLORS = {
  pending: 'bg-gray-100',
  awo: 'bg-purple-100', // AWO orders (starts with "AWO")
  noWo: 'bg-yellow-100', // No WO number
  regular: 'bg-green-50', // Regular WO
};

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Get status badge color classes for order statuses
 * @param status - The order status (PascalCase preferred, snake_case supported for backward compatibility)
 * @returns Tailwind CSS classes for the badge
 */
export function getStatusBadgeColor(status: string): string {
  if (!status) return ORDER_STATUS_COLORS.Pending || ORDER_STATUS_COLORS.pending;
  
  // Try PascalCase first (matches backend)
  if (ORDER_STATUS_COLORS[status]) {
    return ORDER_STATUS_COLORS[status];
  }
  
  // Fallback to normalized snake_case for backward compatibility
  const normalizedStatus = status.toLowerCase().replace(/\s+/g, '_');
  return ORDER_STATUS_COLORS[normalizedStatus] || ORDER_STATUS_COLORS.Pending || ORDER_STATUS_COLORS.pending;
}

/**
 * Get status button color classes for filter buttons
 * @param status - The order status (PascalCase preferred, snake_case supported for backward compatibility)
 * @param isActive - Whether the button is active/selected
 * @returns Tailwind CSS classes for the button
 */
export function getStatusButtonColor(status: string, isActive: boolean): string {
  if (!status) {
    const colors = ORDER_STATUS_BUTTON_COLORS.Pending || ORDER_STATUS_BUTTON_COLORS.pending;
    return isActive ? colors.active : colors.inactive;
  }
  
  // Try PascalCase first (matches backend)
  if (ORDER_STATUS_BUTTON_COLORS[status]) {
    const colors = ORDER_STATUS_BUTTON_COLORS[status];
    return isActive ? colors.active : colors.inactive;
  }
  
  // Fallback to normalized snake_case for backward compatibility
  const normalizedStatus = status.toLowerCase().replace(/\s+/g, '_');
  const colors = ORDER_STATUS_BUTTON_COLORS[normalizedStatus] || ORDER_STATUS_BUTTON_COLORS.Pending || ORDER_STATUS_BUTTON_COLORS.pending;
  return isActive ? colors.active : colors.inactive;
}

/**
 * Get priority badge color classes
 * @param priority - The priority level
 * @returns Tailwind CSS classes for the badge
 */
export function getPriorityBadgeColor(priority: string): string {
  const normalizedPriority = priority?.toLowerCase() || 'medium';
  return PRIORITY_COLORS[normalizedPriority] || PRIORITY_COLORS.medium;
}

/**
 * Get generic status badge color classes
 * @param status - The status
 * @returns Tailwind CSS classes for the badge
 */
export function getGenericStatusColor(status: string): string {
  const normalizedStatus = status?.toLowerCase().replace(/\s+/g, '_') || 'active';
  return GENERIC_STATUS_COLORS[normalizedStatus] || GENERIC_STATUS_COLORS.active;
}

/**
 * Get invoice status badge color classes
 * @param status - The invoice status
 * @returns Tailwind CSS classes for the badge
 */
export function getInvoiceStatusColor(status: string): string {
  const normalizedStatus = status?.toLowerCase().replace(/\s+/g, '_') || 'draft';
  return INVOICE_STATUS_COLORS[normalizedStatus] || INVOICE_STATUS_COLORS.draft;
}

/**
 * Get payroll status badge color classes
 * @param status - The payroll status
 * @returns Tailwind CSS classes for the badge
 */
export function getPayrollStatusColor(status: string): string {
  const normalizedStatus = status?.toLowerCase().replace(/\s+/g, '_') || 'draft';
  return PAYROLL_STATUS_COLORS[normalizedStatus] || PAYROLL_STATUS_COLORS.draft;
}

/**
 * Get RMA status badge color classes
 * @param status - The RMA status
 * @returns Tailwind CSS classes for the badge
 */
export function getRmaStatusColor(status: string): string {
  const normalizedStatus = status?.toLowerCase().replace(/\s+/g, '_') || 'requested';
  return RMA_STATUS_COLORS[normalizedStatus] || RMA_STATUS_COLORS.requested;
}

/**
 * Get card background color based on WO type
 * @param orderNumber - The order/WO number
 * @param status - The order status
 * @returns Tailwind CSS class for the card background
 */
export function getCardBackgroundColor(orderNumber: string | null | undefined, status: string): string {
  // Pending status always uses gray regardless of WO type
  if (status === 'pending') {
    return CARD_BACKGROUND_COLORS.pending;
  }
  
  const isAWO = orderNumber?.startsWith('AWO');
  const hasNoWO = !orderNumber || orderNumber.trim() === '';
  
  if (isAWO) {
    return CARD_BACKGROUND_COLORS.awo;
  } else if (hasNoWO) {
    return CARD_BACKGROUND_COLORS.noWo;
  } else {
    return CARD_BACKGROUND_COLORS.regular;
  }
}

/**
 * Get boolean status color (active/inactive, yes/no, true/false)
 * @param isActive - Boolean value
 * @returns Tailwind CSS classes for the badge
 */
export function getBooleanStatusColor(isActive: boolean): string {
  return isActive 
    ? 'bg-green-100 text-green-800 border-green-300'
    : 'bg-gray-100 text-gray-600 border-gray-300';
}

/**
 * Get StatusBadge variant for priority (for use with StatusBadge component)
 */
export type StatusBadgeVariant = 'default' | 'success' | 'error' | 'warning' | 'info' | 'secondary';
export function getPriorityBadgeVariant(priority: string): StatusBadgeVariant {
  const p = priority?.toLowerCase() || '';
  if (p === 'high' || p === 'critical') return 'error';
  if (p === 'medium') return 'warning';
  if (p === 'low') return 'success';
  return 'secondary';
}

/** Building type badge color classes (shared for BuildingsPage and StatusBadge with className) */
const BUILDING_TYPE_BADGE_COLORS: Record<string, string> = {
  'Condominium': 'bg-blue-100 text-blue-800 border-blue-300',
  'Apartment': 'bg-blue-100 text-blue-800 border-blue-300',
  'Terrace House': 'bg-green-100 text-green-800 border-green-300',
  'Semi-Detached': 'bg-green-100 text-green-800 border-green-300',
  'Bungalow': 'bg-emerald-100 text-emerald-800 border-emerald-300',
  'Office Tower': 'bg-purple-100 text-purple-800 border-purple-300',
  'Office Building': 'bg-purple-100 text-purple-800 border-purple-300',
  'Shopping Mall': 'bg-pink-100 text-pink-800 border-pink-300',
  'Hotel': 'bg-amber-100 text-amber-800 border-amber-300',
  'Prelaid': 'bg-gray-100 text-gray-800 border-gray-300',
  'Non-Prelaid': 'bg-gray-100 text-gray-800 border-gray-300',
  'SDU': 'bg-gray-100 text-gray-800 border-gray-300',
  'RDF Pole': 'bg-gray-100 text-gray-800 border-gray-300',
};
export function getBuildingTypeBadgeColor(type: string): string {
  return BUILDING_TYPE_BADGE_COLORS[type] || 'bg-gray-100 text-gray-800 border-gray-300';
}

/**
 * Get status dot color for inline indicators
 * Aligned with SCHEDULER_MODULE.md documentation
 * @param status - The status (PascalCase preferred, snake_case supported for backward compatibility)
 * @returns Tailwind CSS class for the dot
 */
export function getStatusDotColor(status: string): string {
  if (!status) return 'bg-gray-500';
  
  const dotColors: Record<string, string> = {
    // PascalCase (primary - matches backend)
    Pending: 'bg-gray-500',
    Assigned: 'bg-blue-500',
    OnTheWay: 'bg-yellow-500',
    MetCustomer: 'bg-yellow-500',
    OrderCompleted: 'bg-green-500',
  DocketsReceived: 'bg-teal-500',
  DocketsVerified: 'bg-cyan-500',
  DocketsRejected: 'bg-red-500',
  DocketsUploaded: 'bg-cyan-500',
    ReadyForInvoice: 'bg-indigo-500',
    Invoiced: 'bg-violet-500',
    SubmittedToPortal: 'bg-purple-500',
    Completed: 'bg-green-600',
    Blocker: 'bg-red-500',
    ReschedulePendingApproval: 'bg-amber-500',
    Rejected: 'bg-red-500',
    Cancelled: 'bg-gray-400',
    Reinvoice: 'bg-amber-500',
    
    // Backward compatibility: snake_case
    pending: 'bg-gray-500',
    assigned: 'bg-blue-500',
    on_the_way: 'bg-yellow-500',
    ontheway: 'bg-yellow-500',
    met_customer: 'bg-yellow-500',
    metcustomer: 'bg-yellow-500',
    order_completed: 'bg-green-500',
    ordercompleted: 'bg-green-500',
    docket_received: 'bg-teal-500',
    docketsreceived: 'bg-teal-500',
    docket_uploaded: 'bg-cyan-500',
    docketsuploaded: 'bg-cyan-500',
    ready_to_invoice: 'bg-indigo-500',
    readyforinvoice: 'bg-indigo-500',
    invoiced: 'bg-violet-500',
    completed: 'bg-green-600',
    blocker: 'bg-red-500',
    blocked: 'bg-red-500',
    overdue: 'bg-red-500',
    customer_issue: 'bg-red-500',
    building_issue: 'bg-red-500',
    network_issue: 'bg-red-500',
    rescheduled: 'bg-purple-500',
    reschedule_pending_approval: 'bg-amber-500',
    reschedulependingapproval: 'bg-amber-500',
    withdrawn: 'bg-gray-400',
    cancelled: 'bg-gray-400',
    rejected: 'bg-red-500',
    active: 'bg-green-500',
    inactive: 'bg-gray-400',
  };
  
  // Try exact match first (PascalCase or snake_case)
  if (dotColors[status]) {
    return dotColors[status];
  }
  
  // Fallback to normalized snake_case
  const normalizedStatus = status.toLowerCase().replace(/\s+/g, '_');
  return dotColors[normalizedStatus] || 'bg-gray-500';
}

