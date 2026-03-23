/**
 * Order status constants
 * 
 * ⚠️ IMPORTANT: These must match backend OrderStatus enum exactly (PascalCase, case-sensitive)
 * Source of Truth: backend/src/CephasOps.Domain/Orders/Enums/OrderStatus.cs
 * Reference: docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md
 * 
 * Main Flow: Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted 
 *   → DocketsReceived → DocketsVerified → DocketsUploaded 
 *   → ReadyForInvoice → Invoiced → SubmittedToPortal → Completed
 */
export const ORDER_STATUS = {
  // Main flow statuses (12)
  Pending: 'Pending',
  Assigned: 'Assigned',
  OnTheWay: 'OnTheWay',
  MetCustomer: 'MetCustomer',
  OrderCompleted: 'OrderCompleted',
  DocketsReceived: 'DocketsReceived',
  DocketsVerified: 'DocketsVerified',
  DocketsRejected: 'DocketsRejected',
  DocketsUploaded: 'DocketsUploaded',
  ReadyForInvoice: 'ReadyForInvoice',
  Invoiced: 'Invoiced',
  SubmittedToPortal: 'SubmittedToPortal',
  Completed: 'Completed',
  
  // Side states (5)
  Blocker: 'Blocker',
  ReschedulePendingApproval: 'ReschedulePendingApproval',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  Reinvoice: 'Reinvoice'
} as const;

export type OrderStatus = typeof ORDER_STATUS[keyof typeof ORDER_STATUS];

/**
 * Order type constants
 * 
 * ⚠️ IMPORTANT: SDU is NOT an order type - it's an Installation Method
 * Order types: Activation, Modification (Indoor/Outdoor), Assurance, Value Added Service
 * Installation Methods: Prelaid, Non-prelaid, SDU/RDF Pole, etc.
 */
export const ORDER_TYPE = {
  Activation: 'Activation',
  ModificationIndoor: 'ModificationIndoor',
  ModificationOutdoor: 'ModificationOutdoor',
  Assurance: 'Assurance',
  ValueAddedService: 'ValueAddedService' // For upgrades: router upgrade, additional mesh, etc.
} as const;

export type OrderType = typeof ORDER_TYPE[keyof typeof ORDER_TYPE];

/**
 * Partner filter: use getPartners() API and partnerId (GUID). No hardcoded partner list.
 * Display label (e.g. TIME-FTTH) is derived from Partner.Code + OrderCategory.Code (backend).
 */

type StatusColor = 'gray' | 'blue' | 'yellow' | 'orange' | 'purple' | 'green' | 'red' | 'amber';

/**
 * Get status color for badge
 * Updated to match all valid order statuses
 */
export const getStatusColor = (status: string): StatusColor => {
  const colors: Record<string, StatusColor> = {
    // Main flow
    [ORDER_STATUS.Pending]: 'gray',
    [ORDER_STATUS.Assigned]: 'blue',
    [ORDER_STATUS.OnTheWay]: 'yellow',
    [ORDER_STATUS.MetCustomer]: 'yellow',
    [ORDER_STATUS.OrderCompleted]: 'green',
    [ORDER_STATUS.DocketsReceived]: 'teal',
    [ORDER_STATUS.DocketsVerified]: 'cyan',
    [ORDER_STATUS.DocketsRejected]: 'red',
    [ORDER_STATUS.DocketsUploaded]: 'indigo',
    [ORDER_STATUS.ReadyForInvoice]: 'purple',
    [ORDER_STATUS.Invoiced]: 'violet',
    [ORDER_STATUS.SubmittedToPortal]: 'purple',
    [ORDER_STATUS.Completed]: 'green',
    
    // Side states
    [ORDER_STATUS.Blocker]: 'red',
    [ORDER_STATUS.ReschedulePendingApproval]: 'amber',
    [ORDER_STATUS.Rejected]: 'red',
    [ORDER_STATUS.Cancelled]: 'gray',
    [ORDER_STATUS.Reinvoice]: 'amber'
  };
  return colors[status] || 'gray';
};

