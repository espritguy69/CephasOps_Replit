import apiClient from './client';
import { getApiBaseUrl } from './config';
import type { ReportDefinitionHubDto, RunReportRequestDto, RunReportResultDto } from '../types/reports';

const BASE = '/reports';

const getAuthToken = (): string => localStorage.getItem('authToken') || '';

export async function getReportDefinitions(): Promise<ReportDefinitionHubDto[]> {
  const data = await apiClient.get<ReportDefinitionHubDto[]>(`${BASE}/definitions`);
  return Array.isArray(data) ? data : [];
}

export async function getReportDefinition(reportKey: string): Promise<ReportDefinitionHubDto | null> {
  const data = await apiClient.get<ReportDefinitionHubDto>(`${BASE}/definitions/${encodeURIComponent(reportKey)}`);
  return data && typeof data === 'object' ? data : null;
}

export async function runReport(reportKey: string, request: RunReportRequestDto): Promise<RunReportResultDto> {
  const payload = await apiClient.post<RunReportResultDto>(`${BASE}/${encodeURIComponent(reportKey)}/run`, request);
  if (!payload || typeof payload !== 'object') {
    return { items: [], totalCount: 0 };
  }
  return {
    items: Array.isArray(payload.items) ? payload.items : [],
    totalCount: payload.totalCount ?? 0,
    page: payload.page,
    pageSize: payload.pageSize
  };
}

export function isForbiddenError(err: unknown): boolean {
  return err instanceof Error && 'status' in err && (err as { status?: number }).status === 403;
}

export type ExportFormat = 'csv' | 'xlsx' | 'pdf';

const downloadFileFromResponse = async (res: Response, defaultName: string): Promise<void> => {
  if (!res.ok) throw new Error(res.status === 403 ? 'Access denied' : `Export failed: ${res.status}`);
  const blob = await res.blob();
  const name = res.headers.get('Content-Disposition')?.match(/filename="?(.+)"?/)?.[1] || defaultName;
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = name;
  a.click();
  URL.revokeObjectURL(a.href);
};

/** Params for materials-list export (same as report run). */
export interface ExportMaterialsReportParams {
  format?: ExportFormat;
  departmentId?: string;
  category?: string;
  isActive?: boolean;
}

/** Export materials list (CSV, Excel, or PDF). Uses api/reports/materials-list/export. */
export async function exportMaterialsReport(params: ExportMaterialsReportParams): Promise<void> {
  const format = params.format ?? 'csv';
  const qs = new URLSearchParams();
  qs.set('format', format);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  if (params.category) qs.set('category', params.category);
  if (params.isActive !== undefined) qs.set('isActive', String(params.isActive));
  const dateStr = new Date().toISOString().slice(0, 10);
  const ext = format === 'xlsx' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
  const url = `${getApiBaseUrl()}/reports/materials-list/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  await downloadFileFromResponse(res, `materials-${dateStr}.${ext}`);
}

/** Params for orders-list export (same as report run, up to 10k rows). */
export interface ExportOrdersListParams {
  format?: ExportFormat;
  departmentId?: string;
  keyword?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  assignedSiId?: string;
}

/** Export orders-list report (CSV, Excel, or PDF, up to 10k rows). */
export async function exportOrdersListReport(params: ExportOrdersListParams): Promise<void> {
  const format = params.format ?? 'csv';
  const qs = new URLSearchParams();
  qs.set('format', format);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  if (params.keyword) qs.set('keyword', params.keyword);
  if (params.status) qs.set('status', params.status);
  if (params.fromDate) qs.set('fromDate', params.fromDate);
  if (params.toDate) qs.set('toDate', params.toDate);
  if (params.assignedSiId) qs.set('assignedSiId', params.assignedSiId);
  const dateStr = new Date().toISOString().slice(0, 10);
  const ext = format === 'xlsx' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
  const url = `${getApiBaseUrl()}/reports/orders-list/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  await downloadFileFromResponse(res, `orders-list-${dateStr}.${ext}`);
}

/** Params for stock-summary export (same as report run). */
export interface ExportStockSummaryParams {
  format?: ExportFormat;
  departmentId?: string;
  locationId?: string;
  materialId?: string;
}

/** Export stock-summary report (CSV, Excel, or PDF). */
export async function exportStockSummaryReport(params: ExportStockSummaryParams): Promise<void> {
  const format = params.format ?? 'csv';
  const qs = new URLSearchParams();
  qs.set('format', format);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  if (params.locationId) qs.set('locationId', params.locationId);
  if (params.materialId) qs.set('materialId', params.materialId);
  const dateStr = new Date().toISOString().slice(0, 10);
  const ext = format === 'xlsx' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
  const url = `${getApiBaseUrl()}/reports/stock-summary/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  await downloadFileFromResponse(res, `stock-summary-${dateStr}.${ext}`);
}

/** Params for ledger export (same as report run). */
export interface ExportLedgerParams {
  format?: ExportFormat;
  departmentId?: string;
  materialId?: string;
  locationId?: string;
  orderId?: string;
  entryType?: string;
  fromDate?: string;
  toDate?: string;
}

/** Params for scheduler-utilization export (same as report run). */
export interface ExportSchedulerUtilizationParams {
  format?: ExportFormat;
  departmentId?: string;
  fromDate?: string;
  toDate?: string;
  siId?: string;
}

/** Export ledger report (CSV, Excel, or PDF, up to 10k rows). */
export async function exportLedgerReport(params: ExportLedgerParams): Promise<void> {
  const format = params.format ?? 'csv';
  const qs = new URLSearchParams();
  qs.set('format', format);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  if (params.materialId) qs.set('materialId', params.materialId);
  if (params.locationId) qs.set('locationId', params.locationId);
  if (params.orderId) qs.set('orderId', params.orderId);
  if (params.entryType) qs.set('entryType', params.entryType);
  if (params.fromDate) qs.set('fromDate', params.fromDate);
  if (params.toDate) qs.set('toDate', params.toDate);
  const dateStr = new Date().toISOString().slice(0, 10);
  const ext = format === 'xlsx' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
  const url = `${getApiBaseUrl()}/reports/ledger/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  await downloadFileFromResponse(res, `ledger-${dateStr}.${ext}`);
}

/** Export scheduler-utilization report (CSV, Excel, or PDF). */
export async function exportSchedulerUtilizationReport(params: ExportSchedulerUtilizationParams): Promise<void> {
  const format = params.format ?? 'csv';
  const qs = new URLSearchParams();
  qs.set('format', format);
  if (params.departmentId) qs.set('departmentId', params.departmentId);
  if (params.fromDate) qs.set('fromDate', params.fromDate);
  if (params.toDate) qs.set('toDate', params.toDate);
  if (params.siId) qs.set('siId', params.siId);
  const dateStr = new Date().toISOString().slice(0, 10);
  const ext = format === 'xlsx' ? 'xlsx' : format === 'pdf' ? 'pdf' : 'csv';
  const url = `${getApiBaseUrl()}/reports/scheduler-utilization/export?${qs.toString()}`;
  const res = await fetch(url, { headers: { Authorization: `Bearer ${getAuthToken()}` } });
  await downloadFileFromResponse(res, `scheduler-utilization-${dateStr}.${ext}`);
}
