import apiClient from './client';
import { getApiBaseUrl } from './config';

/**
 * Parser API
 * Handles file parsing, parse sessions, and order draft management
 */

export interface ParseSession {
  id: string;
  status: string;
  sourceType: string;
  sourceDescription?: string;
  parsedOrdersCount: number;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
  snapshotFileId?: string;
}

/** Parsed material line (e.g. from TIME Excel) for parser review / missing material display */
export interface ParsedDraftMaterial {
  id: string;
  name: string;
  actionTag?: string;
  quantity?: number;
  unitOfMeasure?: string;
  notes?: string;
}

/** Building suggested by fuzzy match for parser edit */
export interface BuildingMatchCandidate {
  building: { id: string; name: string; code?: string; city?: string; state?: string };
  similarityScore: number;
}

export interface ParsedOrderDraft {
  id: string;
  parseSessionId: string;
  serviceId?: string;
  ticketId?: string;
  awoNumber?: string; // AWO Number for Assurance orders
  partnerId?: string;
  partnerCode?: string;
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  additionalContactNumber?: string; // Additional contact number for Assurance orders
  issue?: string; // Issue description for Assurance orders
  addressText?: string;
  oldAddress?: string;
  appointmentDate?: string;
  appointmentWindow?: string;
  orderTypeCode?: string;
  orderTypeHint?: string;
  /** Order category ID (FTTH, FTTO, etc.) for parser review/edit */
  orderCategoryId?: string;
  packageName?: string;
  bandwidth?: string;
  onuSerialNumber?: string;
  onuPassword?: string;
  username?: string;
  password?: string;
  internetWanIp?: string;
  internetLanIp?: string;
  internetGateway?: string;
  internetSubnetMask?: string;
  voipServiceId?: string;
  remarks?: string;
  /** Extra/unmapped parser info - read-only, displayed as "Additional Information" on Create Order page. */
  additionalInformation?: string;
  buildingId?: string;
  buildingName?: string;
  buildingStatus?: string;
  /** Enriched: suggested buildings when no match yet (parser edit only) */
  suggestedBuildings?: BuildingMatchCandidate[];
  /** Parsed materials (e.g. missing material) for parser review */
  materials?: ParsedDraftMaterial[];
  /** Number of parsed materials that could not be matched to Material master (backend truth). */
  unmatchedMaterialCount?: number;
  /** Names of parsed materials that could not be matched (for warning display). */
  unmatchedMaterialNames?: string[];
  confidenceScore: number;
  validationStatus: string;
  validationNotes?: string;
  createdOrderId?: string;
  sourceFileName?: string;
  createdAt: string;
  /** When set, an order with the same ServiceId already exists. Approving will update that order. */
  existingOrderId?: string;
}

export interface UpdateParsedOrderDraftRequest {
  serviceId?: string;
  ticketId?: string;
  awoNumber?: string; // AWO Number for Assurance orders
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  addressText?: string;
  oldAddress?: string;
  appointmentDate?: string;
  appointmentWindow?: string;
  orderTypeCode?: string;
  orderCategoryId?: string;
  packageName?: string;
  bandwidth?: string;
  onuSerialNumber?: string;
  onuPassword?: string;
  username?: string;
  password?: string;
  internetWanIp?: string;
  internetLanIp?: string;
  internetGateway?: string;
  internetSubnetMask?: string;
  voipServiceId?: string;
  remarks?: string;
  buildingId?: string;
}

export interface ApproveParsedOrderRequest {
  validationNotes?: string;
}

export interface RejectParsedOrderRequest {
  validationNotes: string;
}

export interface BulkApproveError {
  draftId: string;
  message: string;
}

export interface BulkApproveParsedOrdersResult {
  succeededCount: number;
  failedCount: number;
  succeededDraftIds: string[];
  errors: BulkApproveError[];
}

/**
 * Bulk approve multiple parsed order drafts in one call
 */
export const approveParsedOrderDraftsBulk = async (
  draftIds: string[]
): Promise<BulkApproveParsedOrdersResult> => {
  const response = await apiClient.post<BulkApproveParsedOrdersResult>(
    '/parser/drafts/bulk-approve',
    { draftIds }
  );
  return response;
};

/**
 * Get all parse sessions
 */
export const getParseSessions = async (status?: string): Promise<ParseSession[]> => {
  const response = await apiClient.get<ParseSession[]>('/parser/sessions', { 
    params: status ? { status } : {} 
  });
  return response;
};

/**
 * Get parse session by ID
 */
export const getParseSession = async (id: string): Promise<ParseSession> => {
  const response = await apiClient.get<ParseSession>(`/parser/sessions/${id}`);
  return response;
};

/**
 * Get parsed order drafts for a session
 */
export const getParsedOrderDrafts = async (sessionId: string): Promise<ParsedOrderDraft[]> => {
  const response = await apiClient.get<ParsedOrderDraft[]>(`/parser/sessions/${sessionId}/drafts`);
  return response;
};

/**
 * Get parsed order draft by ID
 */
export const getParsedOrderDraft = async (id: string): Promise<ParsedOrderDraft> => {
  const response = await apiClient.get<ParsedOrderDraft>(`/parser/drafts/${id}`);
  return response;
};

/**
 * Update a parsed order draft (amend data before approval)
 */
export const updateParsedOrderDraft = async (
  id: string, 
  data: UpdateParsedOrderDraftRequest
): Promise<ParsedOrderDraft> => {
  const response = await apiClient.put<ParsedOrderDraft>(`/parser/drafts/${id}`, data);
  return response;
};

/**
 * Approve a parsed order draft
 */
export const approveParsedOrderDraft = async (
  id: string, 
  data?: ApproveParsedOrderRequest
): Promise<ParsedOrderDraft> => {
  const response = await apiClient.post<ParsedOrderDraft>(`/parser/drafts/${id}/approve`, data || {});
  return response;
};

/**
 * Reject a parsed order draft
 */
export const rejectParsedOrderDraft = async (
  id: string, 
  data: RejectParsedOrderRequest
): Promise<ParsedOrderDraft> => {
  const response = await apiClient.post<ParsedOrderDraft>(`/parser/drafts/${id}/reject`, data);
  return response;
};

/**
 * Check if an order already exists for the given Service ID (duplicate warning before approve).
 */
export interface OrderExistsByServiceIdResult {
  exists: boolean;
  orderId?: string;
  serviceId?: string;
  ticketId?: string;
}

export const checkOrderExistsByServiceId = async (
  serviceId: string
): Promise<OrderExistsByServiceIdResult> => {
  const response = await apiClient.get<OrderExistsByServiceIdResult>(
    '/parser/drafts/check-duplicate',
    { params: { serviceId } }
  );
  return response;
};

/** Create a parsed material alias (manual resolve). Future drafts will auto-resolve this name. */
export interface CreateParsedMaterialAliasRequest {
  aliasText: string;
  materialId: string;
}

export interface ParsedMaterialAlias {
  id: string;
  companyId?: string;
  aliasText: string;
  normalizedAliasText: string;
  materialId: string;
  materialItemCode?: string;
  materialDescription?: string;
  createdByUserId?: string;
  source?: string;
  isActive: boolean;
  createdAt: string;
}

export const createParsedMaterialAlias = async (
  body: CreateParsedMaterialAliasRequest
): Promise<ParsedMaterialAlias> => {
  const response = await apiClient.post<ParsedMaterialAlias>('/parser/material-aliases', body);
  return response;
};

export const listParsedMaterialAliases = async (): Promise<ParsedMaterialAlias[]> => {
  const response = await apiClient.get<ParsedMaterialAlias[]>('/parser/material-aliases');
  return response ?? [];
};

/**
 * Upload files for parsing
 */
export const uploadFilesForParsing = async (files: File[]): Promise<ParseSession> => {
  const formData = new FormData();
  files.forEach(file => {
    formData.append('files', file);
  });

  // Get auth token and department ID (key must match DepartmentContext STORAGE_KEYS)
  const token = localStorage.getItem('authToken');
  const departmentId = localStorage.getItem('cephasops.activeDepartmentId');
  
  if (!token) {
    throw new Error('Authentication required. Please login again.');
  }

  const apiBaseUrl = getApiBaseUrl();
  const headers: Record<string, string> = {
    'Authorization': `Bearer ${token}`
  };
  
  // Add department header if available
  if (departmentId) {
    headers['X-Department-Id'] = departmentId;
  }
  
  // Note: Don't set Content-Type header for FormData - browser will set it with boundary

  const response = await fetch(`${apiBaseUrl}/parser/upload`, {
    method: 'POST',
    headers,
    body: formData
  });

  if (!response.ok) {
    let errorMessage = `Upload failed: ${response.status} ${response.statusText}`;
    try {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        const errorData = await response.json();
        // Handle ApiResponse envelope
        if (errorData && typeof errorData === 'object' && 'success' in errorData) {
          errorMessage = errorData.message || errorData.errors?.join(', ') || errorMessage;
        } else {
          errorMessage = errorData.message || errorData.error || errorMessage;
        }
      }
    } catch {
      // If parsing fails, use default message
    }
    
    // Clear auth on 401
    if (response.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      throw new Error('Session expired. Please login again.');
    }
    
    throw new Error(errorMessage);
  }

  const result = await response.json();
  // Unwrap ApiResponse envelope if present
  if (result && typeof result === 'object' && 'success' in result && result.data) {
    return result.data;
  }
  return result;
};

/**
 * Filter parameters for parsed order drafts query
 */
export interface ParsedOrderDraftFilters {
  validationStatus?: string; // Pending, Valid, NeedsReview, Rejected
  sourceType?: string; // Email, FileUpload
  status?: string; // Session status filter
  serviceId?: string; // Search by service ID
  customerName?: string; // Search by customer name
  partnerId?: string; // Filter by partner ID
  buildingStatus?: string; // Existing, New
  confidenceMin?: number; // Min confidence 0-1 (e.g. 0.8 for 80%)
  buildingMatched?: boolean; // true = has building, false = no building
  fromDate?: string; // ISO date string
  toDate?: string; // ISO date string
  page?: number; // Page number (1-based)
  pageSize?: number; // Page size
  sortBy?: string; // Sort field (serviceId, customerName, createdAt, validationStatus, confidenceScore)
  sortOrder?: 'asc' | 'desc'; // Sort order
}

/**
 * Paginated result for parsed order drafts
 */
export interface PagedParsedOrderDrafts {
  items: ParsedOrderDraft[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ParserStatistics {
  totalSessionsToday: number;
  successfulSessionsToday: number;
  failedSessionsToday: number;
  totalDrafts: number;
  pendingDrafts: number;
  validDrafts: number;
  needsReviewDrafts: number;
  rejectedDrafts: number;
  approvedDrafts: number;
  averageConfidenceScore: number;
  totalSessionsAllTime: number;
  totalDraftsAllTime: number;
}

/** Parser analytics for dashboard (period-based). */
export interface ParserAnalytics {
  parseSuccessRate: number;
  autoMatchRate: number;
  totalSessions: number;
  completedSessions: number;
  failedSessions: number;
  totalDrafts: number;
  buildingMatchedDrafts: number;
  confidenceDistribution: { label: string; count: number }[];
  commonErrors: { message: string; count: number }[];
  ordersCreatedPerDay: { date: string; count: number }[];
  fromDate: string;
  toDate: string;
}

/**
 * Get parser analytics for a date range (parse success rate, auto-match rate, confidence distribution, common errors, orders per day).
 */
export const getParserAnalytics = async (
  fromDate?: string,
  toDate?: string
): Promise<ParserAnalytics> => {
  const params: Record<string, string> = {};
  if (fromDate) params.fromDate = fromDate;
  if (toDate) params.toDate = toDate;
  const response = await apiClient.get<ParserAnalytics>('/parser/analytics', { params });
  return response;
};

/**
 * Get parsed order drafts with comprehensive filtering and pagination
 */
export const getParsedOrderDraftsWithFilters = async (
  filters: ParsedOrderDraftFilters = {}
): Promise<PagedParsedOrderDrafts> => {
  const params: Record<string, string> = {};
  
  if (filters.validationStatus) params.validationStatus = filters.validationStatus;
  if (filters.sourceType) params.sourceType = filters.sourceType;
  if (filters.status) params.status = filters.status;
  if (filters.serviceId) params.serviceId = filters.serviceId;
  if (filters.customerName) params.customerName = filters.customerName;
  if (filters.partnerId) params.partnerId = filters.partnerId;
  if (filters.buildingStatus) params.buildingStatus = filters.buildingStatus;
  if (filters.confidenceMin != null) params.confidenceMin = filters.confidenceMin.toString();
  if (filters.buildingMatched != null) params.buildingMatched = filters.buildingMatched.toString();
  if (filters.fromDate) params.fromDate = filters.fromDate;
  if (filters.toDate) params.toDate = filters.toDate;
  if (filters.page) params.page = filters.page.toString();
  if (filters.pageSize) params.pageSize = filters.pageSize.toString();
  if (filters.sortBy) params.sortBy = filters.sortBy;
  if (filters.sortOrder) params.sortOrder = filters.sortOrder;
  
  const response = await apiClient.get<PagedParsedOrderDrafts>('/parser/drafts', { params });
  return response;
};

/**
 * Get parser statistics
 */
export const getParserStatistics = async (): Promise<ParserStatistics> => {
  const response = await apiClient.get<ParserStatistics>('/parser/statistics');
  return response;
};

/**
 * Retry a failed parse session
 */
export const retryParseSession = async (sessionId: string): Promise<ParseSession> => {
  const response = await apiClient.post<ParseSession>(`/parser/sessions/${sessionId}/retry`);
  return response;
};

/**
 * Export parser logs (sessions and drafts) to CSV or JSON
 */
export const exportParserLogs = async (
  format: 'csv' | 'json' = 'csv',
  fromDate?: string,
  toDate?: string,
  status?: string,
  validationStatus?: string
): Promise<void> => {
  const params: Record<string, string> = { format };
  if (fromDate) params.fromDate = fromDate;
  if (toDate) params.toDate = toDate;
  if (status) params.status = status;
  if (validationStatus) params.validationStatus = validationStatus;

  const token = localStorage.getItem('authToken');
  const departmentId = localStorage.getItem('cephasops.activeDepartmentId');
  const apiBaseUrl = getApiBaseUrl();

  if (!token) {
    throw new Error('Authentication required. Please login again.');
  }

  const queryString = new URLSearchParams(params).toString();
  const url = `${apiBaseUrl}/parser/logs/export?${queryString}`;

  const headers: Record<string, string> = {
    'Authorization': `Bearer ${token}`
  };

  if (departmentId) {
    headers['X-Department-Id'] = departmentId;
  }

  const response = await fetch(url, { headers });

  if (!response.ok) {
    if (response.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      throw new Error('Session expired. Please login again.');
    }
    throw new Error(`Export failed: ${response.status} ${response.statusText}`);
  }

  // Get filename from Content-Disposition header or use default
  const contentDisposition = response.headers.get('content-disposition');
  let filename = `parser-logs-${new Date().toISOString().split('T')[0]}.${format}`;
  if (contentDisposition) {
    const filenameMatch = contentDisposition.match(/filename="?(.+?)"?$/);
    if (filenameMatch) {
      filename = filenameMatch[1];
    }
  }

  // Download file
  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = downloadUrl;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(downloadUrl);
};

