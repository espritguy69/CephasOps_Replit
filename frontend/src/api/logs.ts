/**
 * Audit / security activity API
 * GET /api/logs/audit, GET /api/logs/security-activity, GET /api/logs/security-alerts
 */
import apiClient from './client';

export interface SecurityActivityEntry {
  id: string;
  timestamp: string;
  userId?: string | null;
  userEmail?: string | null;
  action: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  metadataJson?: string | null;
}

export interface SecurityActivityResult {
  items: SecurityActivityEntry[];
  totalCount: number;
}

export async function getSecurityActivity(params: {
  userId?: string | null;
  action?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  page?: number;
  pageSize?: number;
}): Promise<SecurityActivityResult> {
  const q: Record<string, string | number> = {};
  if (params.userId != null && params.userId !== '') q.userId = params.userId;
  if (params.action != null && params.action !== '') q.action = params.action;
  if (params.dateFrom != null && params.dateFrom !== '') q.dateFrom = params.dateFrom;
  if (params.dateTo != null && params.dateTo !== '') q.dateTo = params.dateTo;
  if (params.page != null) q.page = params.page;
  if (params.pageSize != null) q.pageSize = params.pageSize;
  const data = await apiClient.get<{ items: SecurityActivityEntry[]; totalCount: number }>(
    '/logs/security-activity',
    { params: q }
  );
  return {
    items: data.items ?? [],
    totalCount: data.totalCount ?? 0
  };
}

export interface SecurityAlert {
  detectedAtUtc: string;
  userId?: string | null;
  userEmail?: string | null;
  alertType: string;
  description: string;
  ipSummary?: string | null;
  eventCount: number;
  windowMinutes: number;
}

export async function getSecurityAlerts(params: {
  dateFrom?: string | null;
  dateTo?: string | null;
  userId?: string | null;
  alertType?: string | null;
}): Promise<SecurityAlert[]> {
  const q: Record<string, string> = {};
  if (params.dateFrom != null && params.dateFrom !== '') q.dateFrom = params.dateFrom;
  if (params.dateTo != null && params.dateTo !== '') q.dateTo = params.dateTo;
  if (params.userId != null && params.userId !== '') q.userId = params.userId;
  if (params.alertType != null && params.alertType !== '') q.alertType = params.alertType;
  const response = await apiClient.get<{ data?: SecurityAlert[] } | SecurityAlert[]>('/logs/security-alerts', { params: q });
  const data = Array.isArray(response) ? response : (response as { data?: SecurityAlert[] }).data;
  return Array.isArray(data) ? data : [];
}
