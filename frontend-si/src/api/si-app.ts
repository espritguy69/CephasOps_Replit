import apiClient from './client';
import type { Location } from '../types/api';

/**
 * SI App API
 * Handles job sessions, events, photos, device scans, and location pings for Service Installer mobile app
 */

export interface JobFilters {
  status?: string;
  date?: string;
}

export interface SessionData {
  orderId: string;
  location?: Location;
  [key: string]: any;
}

export interface EventData {
  eventType: string;
  timestamp?: string;
  location?: Location;
  notes?: string;
}

export interface ScanData {
  serialNumber: string;
  deviceType?: string;
  location?: Location;
  scannedAt?: string;
}

export interface LocationData {
  latitude: number;
  longitude: number;
  timestamp?: string;
}

export interface CompletionData {
  notes?: string;
  photos?: any[];
  scans?: any[];
}

/**
 * Get assigned jobs for SI
 * Uses the standard orders endpoint with assignedSiId filter
 */
export const getAssignedJobs = async (
  companyId: string | null, 
  siId: string, 
  filters: JobFilters = {}
): Promise<any[]> => {
  // Use the standard orders endpoint with assignedSiId filter
  const response = await apiClient.get<any[] | { data: any[] }>('/orders', { 
    params: {
      ...filters,
      assignedSiId: siId || undefined,
    }
  });
  
  // Handle response envelope
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: any[] }).data;
  }
  return [];
};

/**
 * Get job details
 * Uses the standard orders endpoint
 */
export const getJobDetails = async (
  companyId: string | null, 
  siId: string, 
  orderId: string
): Promise<any> => {
  // Use the standard orders endpoint
  const response = await apiClient.get<any | { data: any }>(`/orders/${orderId}`);
  
  // Handle response envelope
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: any }).data;
  }
  return response;
};

/**
 * Start a job session
 */
export const startJobSession = async (
  companyId: string | null, 
  siId: string, 
  orderId: string, 
  sessionData: Partial<SessionData> = {}
): Promise<any> => {
  if (!companyId) {
    throw new Error('Company ID required for session creation');
  }
  const response = await apiClient.post(`/companies/${companyId}/si-app/${siId}/sessions`, {
    orderId,
    ...sessionData
  });
  return response;
};

/**
 * Get active job session
 */
export const getActiveJobSession = async (
  companyId: string | null, 
  siId: string, 
  orderId: string
): Promise<any> => {
  if (!companyId) {
    return null;
  }
  const response = await apiClient.get(`/companies/${companyId}/si-app/${siId}/sessions/active/${orderId}`);
  return response;
};

/**
 * Record job event
 */
export const recordJobEvent = async (
  companyId: string | null, 
  siId: string, 
  sessionId: string, 
  eventData: EventData
): Promise<any> => {
  if (!companyId) {
    throw new Error('Company ID required');
  }
  const response = await apiClient.post(`/companies/${companyId}/si-app/${siId}/sessions/${sessionId}/events`, eventData);
  return response;
};

/**
 * Upload job photo
 */
export const uploadJobPhoto = async (
  companyId: string | null, 
  siId: string, 
  sessionId: string, 
  photo: File, 
  metadata: { location?: Location; notes?: string; eventType?: string } = {}
): Promise<any> => {
  if (!companyId) {
    throw new Error('Company ID required');
  }
  
  const formData = new FormData();
  formData.append('photo', photo);
  if (metadata.location) {
    formData.append('latitude', metadata.location.latitude.toString());
    formData.append('longitude', metadata.location.longitude.toString());
  }
  if (metadata.notes) formData.append('notes', metadata.notes);
  if (metadata.eventType) formData.append('eventType', metadata.eventType);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';
  const token = localStorage.getItem('authToken');
  
  const response = await fetch(`${API_BASE_URL}/companies/${companyId}/si-app/${siId}/sessions/${sessionId}/photos`, {
    method: 'POST',
    headers: {
      'Authorization': token ? `Bearer ${token}` : '',
    },
    body: formData
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`API Error: ${response.status} ${response.statusText} - ${errorText}`);
  }

  return response.json();
};

/**
 * Record device scan
 */
export const recordDeviceScan = async (
  companyId: string | null, 
  siId: string, 
  sessionId: string, 
  scanData: ScanData
): Promise<any> => {
  // In single-company mode, companyId may be null
  // If companyId is null, we'll use a placeholder or get it from context
  // For now, if companyId is null, we'll throw an error with a helpful message
  // In production, you may want to get companyId from serviceInstaller context
  if (!companyId) {
    // Note: In single-company mode, you may need to get companyId from serviceInstaller
    // or use a default companyId. For now, we'll require it.
    throw new Error('Company ID required for device scan recording. Please ensure service installer profile includes companyId.');
  }
  const response = await apiClient.post(`/companies/${companyId}/si-app/${siId}/sessions/${sessionId}/scans`, scanData);
  return response;
};

/**
 * Record location ping
 */
export const recordLocationPing = async (
  companyId: string | null, 
  siId: string, 
  sessionId: string, 
  locationData: LocationData
): Promise<any> => {
  if (!companyId) {
    throw new Error('Company ID required');
  }
  const response = await apiClient.post(`/companies/${companyId}/si-app/${siId}/sessions/${sessionId}/location`, locationData);
  return response;
};

/**
 * Complete job session
 */
export const completeJobSession = async (
  companyId: string | null, 
  siId: string, 
  sessionId: string, 
  completionData: CompletionData = {}
): Promise<any> => {
  if (!companyId) {
    throw new Error('Company ID required');
  }
  const response = await apiClient.put(`/companies/${companyId}/si-app/${siId}/sessions/${sessionId}/complete`, completionData);
  return response;
};

/**
 * Get job session history
 */
export const getJobSessionHistory = async (
  companyId: string | null, 
  siId: string, 
  filters: { orderId?: string; dateRange?: string } = {}
): Promise<any[]> => {
  if (!companyId) {
    return [];
  }
  const response = await apiClient.get(`/companies/${companyId}/si-app/${siId}/sessions`, { params: filters });
  return response;
};

/**
 * Mark device as faulty
 */
export interface MarkFaultyRequest {
  serialNumber: string;
  reason: string;
  notes?: string;
}

export interface MarkFaultyResponse {
  serialisedItemId: string;
  serialNumber: string;
  materialName: string;
  stockMovementId?: string;
  rmaRequestId?: string;
  message: string;
}

export const markDeviceAsFaulty = async (
  orderId: string,
  serialNumber: string,
  data: MarkFaultyRequest
): Promise<MarkFaultyResponse> => {
  const response = await apiClient.post<MarkFaultyResponse | { data: MarkFaultyResponse }>(
    `/si-app/jobs/${orderId}/materials/${serialNumber}/mark-faulty`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MarkFaultyResponse }).data;
  }
  return response as MarkFaultyResponse;
};

/**
 * Record material replacement (Assurance orders)
 */
export interface RecordReplacementRequest {
  oldSerialNumber: string;
  newSerialNumber: string;
  replacementReason: string;
  notes?: string;
}

export interface RecordReplacementResponse {
  replacementId: string;
  oldSerialNumber: string;
  newSerialNumber: string;
  oldSerialisedItemId?: string;
  newSerialisedItemId?: string;
  rmaRequestId?: string;
  message: string;
}

export const recordMaterialReplacement = async (
  orderId: string,
  data: RecordReplacementRequest
): Promise<RecordReplacementResponse> => {
  const response = await apiClient.post<RecordReplacementResponse | { data: RecordReplacementResponse }>(
    `/si-app/jobs/${orderId}/materials/replace`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: RecordReplacementResponse }).data;
  }
  return response as RecordReplacementResponse;
};

/**
 * Return faulty material (standalone)
 */
export interface ReturnFaultyRequest {
  serialNumber?: string;
  materialId?: string;
  quantity?: number;
  orderId?: string;
  reason: string;
  notes?: string;
}

/**
 * Material returns query filters
 */
export interface MaterialReturnsQuery {
  dateFrom?: string;
  dateTo?: string;
  orderId?: string;
  materialId?: string;
  status?: 'all' | 'faulty' | 'returned' | 'rma';
  returnType?: 'all' | 'faulty' | 'replacement' | 'nonserialised';
}

/**
 * Material return DTO
 */
export interface MaterialReturn {
  id: string;
  orderId?: string;
  orderServiceId?: string;
  serialNumber?: string;
  materialId?: string;
  materialName: string;
  quantity: number;
  returnedAt: string;
  reason?: string;
  notes?: string;
  status: 'Faulty' | 'Returned' | 'RMA Created';
  rmaRequestId?: string;
  replacementReason?: string;
  returnType: 'Faulty' | 'Replacement' | 'NonSerialisedReplacement';
}

/**
 * Get material returns list for SI
 */
export const getMaterialReturns = async (
  filters?: MaterialReturnsQuery
): Promise<MaterialReturn[]> => {
  const params = new URLSearchParams();
  if (filters?.dateFrom) params.append('dateFrom', filters.dateFrom);
  if (filters?.dateTo) params.append('dateTo', filters.dateTo);
  if (filters?.orderId) params.append('orderId', filters.orderId);
  if (filters?.materialId) params.append('materialId', filters.materialId);
  if (filters?.status && filters.status !== 'all') params.append('status', filters.status);
  if (filters?.returnType && filters.returnType !== 'all') params.append('returnType', filters.returnType);

  const response = await apiClient.get<MaterialReturn[] | { data: MaterialReturn[] }>(
    `/si-app/materials/returns?${params.toString()}`
  );

  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MaterialReturn[] }).data;
  }
  return [];
};

export const returnFaultyMaterial = async (
  data: ReturnFaultyRequest
): Promise<MarkFaultyResponse> => {
  const response = await apiClient.post<MarkFaultyResponse | { data: MarkFaultyResponse }>(
    `/si-app/materials/return-faulty`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: MarkFaultyResponse }).data;
  }
  return response as MarkFaultyResponse;
};

/**
 * Record non-serialised material replacement
 */
export interface RecordNonSerialisedReplacementRequest {
  materialId: string;
  quantityReplaced: number;
  replacementReason: string;
  remark?: string;
}

export interface RecordNonSerialisedReplacementResponse {
  replacementId: string;
  message: string;
}

export const recordNonSerialisedReplacement = async (
  orderId: string,
  data: RecordNonSerialisedReplacementRequest
): Promise<RecordNonSerialisedReplacementResponse> => {
  const response = await apiClient.post<RecordNonSerialisedReplacementResponse | { data: RecordNonSerialisedReplacementResponse }>(
    `/si-app/jobs/${orderId}/materials/replace-non-serialised`,
    data
  );
  
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: RecordNonSerialisedReplacementResponse }).data;
  }
  return response as RecordNonSerialisedReplacementResponse;
};

