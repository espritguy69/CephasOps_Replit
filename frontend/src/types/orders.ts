/**
 * Order Types - Shared type definitions for Orders module
 */

// Base Order type matching backend response
export interface Order {
  id: string;
  companyId?: string;
  departmentId?: string;
  partnerId?: string;
  partnerName?: string;
  partnerGroup?: string;
  /** Partner short code (e.g. TIME). For display. */
  partnerCode?: string;
  /** Order category code = Installation Type (e.g. FTTH, FTTO). */
  orderCategoryCode?: string;
  /** Installation method code (e.g. PRELAID, SDU_RDF). */
  installationMethodCode?: string;
  /** Installation method name for display (e.g. Prelaid, SDU / RDF Pole). */
  installationMethodName?: string;
  /** Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). Not persisted. */
  derivedPartnerCategoryLabel?: string;
  partnerOrderId?: string;
  partnerOrderType?: string;
  serviceIdType?: 'Tbbn' | 'PartnerServiceId';
  serviceId?: string;
  ticketId?: string;
  awoNumber?: string;
  uniqueId?: string;
  orderType?: string;
  orderTypeId?: string;
  orderCategoryId?: string;
  installationMethodId?: string;
  status: string;
  priority?: string;
  // Customer info
  customerName?: string;
  customerPhone?: string;
  customerPhone2?: string;
  customerEmail?: string;
  // Address info
  address?: string;
  fullAddress?: string;
  addressLine1?: string;
  addressLine2?: string;
  unitNo?: string;
  city?: string;
  state?: string;
  postcode?: string;
  // Building info
  buildingId?: string;
  buildingName?: string;
  // Relocation fields
  relocationType?: string;
  oldAddress?: string;
  oldLocationNote?: string;
  newLocationNote?: string;
  // Appointment
  appointmentDate?: string;
  appointmentTime?: string;
  appointmentWindowFrom?: string;
  appointmentWindowTo?: string;
  requestedAppointmentAt?: string;
  rescheduleCount?: number;
  // Assignment
  assignedTo?: string;
  assignedToName?: string;
  assignedSiId?: string;
  assignedTeamId?: string;
  // Splitter fields
  splitterNumber?: string;
  splitterLocation?: string;
  splitterPort?: string;
  splitterId?: string;
  // Operations
  serialsValidated?: boolean;
  photosUploaded?: boolean;
  docketUploaded?: boolean;
  invoiceEligible?: boolean;
  pnlPeriod?: string;
  invoiceId?: string;
  // Notes
  orderNotesInternal?: string;
  partnerNotes?: string;
  // Parsed materials (from parser)
  parsedMaterials?: ParsedMaterial[];
  /** Parser-origin only: count of parsed materials that could not be matched to Material master. */
  unmatchedParsedMaterialCount?: number;
  /** Parser-origin only: names of unmatched parsed materials. */
  unmatchedParsedMaterialNames?: string[];
  // Material replacements (for Assurance orders)
  materialReplacements?: OrderMaterialReplacement[];
  nonSerialisedReplacements?: OrderNonSerialisedReplacement[];
  // Network Info fields
  networkPackage?: string;
  networkBandwidth?: string;
  networkLoginId?: string;
  networkPassword?: string;
  networkWanIp?: string;
  networkLanIp?: string;
  networkGateway?: string;
  networkSubnetMask?: string;
  // ONU fields
  onuSerialNumber?: string;
  // VOIP fields
  voipServiceId?: string;
  voipPassword?: string;
  voipIpAddressOnu?: string;
  voipGatewayOnu?: string;
  voipSubnetMaskOnu?: string;
  voipIpAddressSrp?: string;
  voipRemarks?: string;
  // Audit
  createdAt?: string;
  updatedAt?: string;
}

export interface ParsedMaterial {
  id: string;
  name: string;
  quantity?: number;
  unitOfMeasure?: string;
  actionTag?: string;
  notes?: string;
}

export interface OrderFilters {
  status?: string;
  partnerId?: string;
  assignedSiId?: string;
  buildingId?: string;
  fromDate?: string;
  toDate?: string;
  departmentId?: string;
  search?: string;
}

export interface OrderStatusLog {
  id: string;
  orderId: string;
  fromStatus?: string;
  toStatus: string;
  transitionReason?: string;
  triggeredByUserId?: string;
  triggeredByUserName?: string;
  triggeredBySiId?: string;
  triggeredBySiName?: string;
  source: string;
  metadataJson?: string;
  createdAt: string;
}

export interface OrderReschedule {
  id: string;
  orderId: string;
  requestedByUserId?: string;
  requestedByUserName?: string;
  requestedBySiId?: string;
  requestedBySiName?: string;
  requestedBySource: string;
  requestedAt: string;
  originalDate: string;
  originalWindowFrom: string;
  originalWindowTo: string;
  newDate: string;
  newWindowFrom: string;
  newWindowTo: string;
  reason: string;
  approvalSource?: string;
  approvalEmailId?: string;
  status: string;
  statusChangedByUserId?: string;
  statusChangedByUserName?: string;
  statusChangedAt?: string;
  isSameDayReschedule: boolean;
  sameDayEvidenceAttachmentId?: string;
  sameDayEvidenceNotes?: string;
  createdAt: string;
}

export interface OrderBlocker {
  id: string;
  type?: string;
  description?: string;
  resolved?: boolean;
  createdAt?: string;
}

export interface OrderDocket {
  id: string;
  docketNumber?: string;
  summary?: string;
  createdAt?: string;
}

/**
 * Order material replacement - serialised material swap for Assurance orders
 * Requires TIME approval before Invoice/Docket Verified
 */
export interface OrderMaterialReplacement {
  id: string;
  orderId: string;
  oldMaterialId: string;
  oldMaterialName?: string;
  oldSerialNumber: string;
  oldSerialisedItemId?: string;
  newMaterialId: string;
  newMaterialName?: string;
  newSerialNumber: string;
  newSerialisedItemId?: string;
  approvedBy?: string;
  approvalNotes?: string;
  approvedAt?: string;
  replacementReason?: string;
  replacedBySiId?: string;
  recordedByUserId?: string;
  recordedAt: string;
  rmaRequestId?: string;
  notes?: string;
}

/**
 * Create/Update order material replacement
 */
export interface CreateOrderMaterialReplacement {
  oldMaterialId: string;
  oldSerialNumber: string;
  oldSerialisedItemId?: string;
  newMaterialId: string;
  newSerialNumber: string;
  newSerialisedItemId?: string;
  approvedBy?: string;
  approvalNotes?: string;
  replacementReason?: string;
  notes?: string;
}

/**
 * Order non-serialised replacement - for patch cords, connectors, etc.
 * No TIME approval required
 */
export interface OrderNonSerialisedReplacement {
  id: string;
  orderId: string;
  materialId: string;
  materialName?: string;
  quantityReplaced: number;
  unit?: string;
  replacementReason?: string;
  remark?: string;
  replacedBySiId?: string;
  recordedByUserId?: string;
  recordedAt: string;
}

/**
 * Create/Update order non-serialised replacement
 */
export interface CreateOrderNonSerialisedReplacement {
  materialId: string;
  quantityReplaced: number;
  unit?: string;
  replacementReason?: string;
  remark?: string;
}

// Column definition for StandardListTable
export interface TableColumn<T> {
  key: keyof T | string;
  label: string;
  align?: 'left' | 'center' | 'right';
  render?: (value: unknown, row: T) => React.ReactNode;
}

// Bulk action definition
export interface BulkAction {
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  onClick: () => void;
  variant?: 'default' | 'destructive';
}

// Upload result from parser
export interface UploadResult {
  success: boolean;
  message?: string;
  sessionId?: string;
  draftsCount?: number;
}

