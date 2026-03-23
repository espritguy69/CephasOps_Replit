/**
 * Inventory report APIs (Phase 2.2.1 contract).
 * Endpoints: GET /api/inventory/reports/usage-summary, stock-by-location-history, serial-lifecycle.
 * Export: GET /api/inventory/reports/usage-summary/export, serial-lifecycle/export (Phase 2.2.4).
 */
import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  UsageSummaryReportResultDto,
  StockByLocationHistoryResultDto,
  SerialLifecycleReportResultDto
} from '../types/inventoryReports';
import type { ApiError } from './client';

function unwrap<T>(response: unknown): T {
  if (response && typeof response === 'object' && 'data' in response && (response as { data?: T }).data !== undefined) {
    return (response as { data: T }).data as T;
  }
  return response as T;
}

export interface UsageSummaryParams {
  fromDate: string;
  toDate: string;
  groupBy?: 'Material' | 'Location' | 'Department';
  materialId?: string;
  locationId?: string;
  departmentId?: string | null;
  page?: number;
  pageSize?: number;
}

export async function getUsageSummary(params: UsageSummaryParams): Promise<UsageSummaryReportResultDto> {
  const response = await apiClient.get<UsageSummaryReportResultDto>('/inventory/reports/usage-summary', {
    params: {
      fromDate: params.fromDate,
      toDate: params.toDate,
      groupBy: params.groupBy,
      materialId: params.materialId,
      locationId: params.locationId,
      departmentId: params.departmentId ?? undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50
    }
  });
  return unwrap(response) ?? { fromDate: params.fromDate, toDate: params.toDate, items: [], totalCount: 0, page: 1, pageSize: 50 };
}

export interface StockByLocationHistoryParams {
  fromDate: string;
  toDate: string;
  snapshotType?: 'Daily' | 'Weekly' | 'Monthly';
  materialId?: string;
  locationId?: string;
  departmentId?: string | null;
  page?: number;
  pageSize?: number;
}

export async function getStockByLocationHistory(params: StockByLocationHistoryParams): Promise<StockByLocationHistoryResultDto> {
  const response = await apiClient.get<StockByLocationHistoryResultDto>('/inventory/reports/stock-by-location-history', {
    params: {
      fromDate: params.fromDate,
      toDate: params.toDate,
      snapshotType: params.snapshotType ?? 'Daily',
      materialId: params.materialId,
      locationId: params.locationId,
      departmentId: params.departmentId ?? undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50
    }
  });
  return unwrap(response) ?? { fromDate: params.fromDate, toDate: params.toDate, snapshotType: 'Daily', items: [], totalCount: 0, page: 1, pageSize: 50 };
}

export interface SerialLifecycleParams {
  serialNumber?: string;
  serialNumbers?: string;
  materialId?: string;
  departmentId?: string | null;
  page?: number;
  pageSize?: number;
}

export async function getSerialLifecycle(params: SerialLifecycleParams): Promise<SerialLifecycleReportResultDto> {
  const response = await apiClient.get<SerialLifecycleReportResultDto>('/inventory/reports/serial-lifecycle', {
    params: {
      serialNumber: params.serialNumber,
      serialNumbers: params.serialNumbers,
      materialId: params.materialId,
      departmentId: params.departmentId ?? undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50
    }
  });
  return unwrap(response) ?? { serialsQueried: [], serialLifecycles: [], totalCount: 0, page: 1, pageSize: 50 };
}

export function isForbiddenError(err: unknown): boolean {
  const apiErr = err as ApiError;
  return apiErr?.status === 403;
}

const getAuthToken = (): string => localStorage.getItem('authToken') || '';

/** Download usage-summary report as CSV. Uses same params as getUsageSummary (fromDate, toDate, groupBy, materialId, locationId, departmentId). */
export async function exportUsageSummaryReport(params: UsageSummaryParams): Promise<void> {
  const qs = new URLSearchParams();
  if (params.fromDate) qs.set('fromDate', params.fromDate);
  if (params.toDate) qs.set('toDate', params.toDate);
  if (params.groupBy) qs.set('groupBy', params.groupBy);
  if (params.materialId) qs.set('materialId', params.materialId);
  if (params.locationId) qs.set('locationId', params.locationId);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  const url = `${getApiBaseUrl()}/inventory/reports/usage-summary/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  if (!res.ok) throw new Error(res.status === 403 ? 'Access denied' : `Export failed: ${res.status}`);
  const blob = await res.blob();
  const name = res.headers.get('Content-Disposition')?.match(/filename="?(.+)"?/)?.[1] || `usage-summary-${params.fromDate}-to-${params.toDate}.csv`;
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = name;
  a.click();
  URL.revokeObjectURL(a.href);
}

/** Download serial-lifecycle report as CSV. Pass serialNumber (single) or serialNumbers (comma-separated), optional materialId, departmentId. */
export async function exportSerialLifecycleReport(params: { serialNumber?: string; serialNumbers?: string; materialId?: string; departmentId?: string | null }): Promise<void> {
  const qs = new URLSearchParams();
  if (params.serialNumber) qs.set('serialNumber', params.serialNumber);
  if (params.serialNumbers) qs.set('serialNumbers', params.serialNumbers);
  if (params.materialId) qs.set('materialId', params.materialId);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  const url = `${getApiBaseUrl()}/inventory/reports/serial-lifecycle/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  if (!res.ok) throw new Error(res.status === 403 ? 'Access denied' : res.status === 400 ? 'Invalid request (e.g. serial numbers required)' : `Export failed: ${res.status}`);
  const blob = await res.blob();
  const name = res.headers.get('Content-Disposition')?.match(/filename="?(.+)"?/)?.[1] || `serial-lifecycle-${new Date().toISOString().slice(0, 10)}.csv`;
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = name;
  a.click();
  URL.revokeObjectURL(a.href);
}
