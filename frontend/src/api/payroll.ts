import apiClient from './client';
import { getApiBaseUrl } from './config';
import type {
  PayrollPeriod,
  CreatePayrollPeriodRequest,
  PayrollRun,
  CreatePayrollRunRequest,
  JobEarningRecord,
  SiRatePlan,
  CreateSiRatePlanRequest,
  UpdateSiRatePlanRequest,
  PayrollPeriodFilters,
  PayrollRunFilters,
  JobEarningFilters,
  SiRatePlanFilters,
  ImportResult
} from '../types/payroll';

/**
 * Payroll API
 * Handles payroll periods, runs, items, SI rate plans, and payslips
 */

/**
 * Get authentication token from storage
 * @returns Auth token or empty string
 */
const getAuthToken = (): string => {
  return localStorage.getItem('authToken') || '';
};

/**
 * Get payroll periods list
 * @param filters - Optional filters (year, status)
 * @returns Array of payroll periods
 */
export const getPayrollPeriods = async (filters: PayrollPeriodFilters = {}): Promise<PayrollPeriod[]> => {
  const response = await apiClient.get<PayrollPeriod[]>('/payroll/periods', { params: filters });
  return response;
};

/**
 * Get payroll period by ID
 * @param periodId - Payroll period ID
 * @returns Payroll period details
 */
export const getPayrollPeriod = async (periodId: string): Promise<PayrollPeriod> => {
  const response = await apiClient.get<PayrollPeriod>(`/payroll/periods/${periodId}`);
  return response;
};

/**
 * Create payroll period
 * @param periodData - Payroll period creation data
 * @returns Created payroll period
 */
export const createPayrollPeriod = async (periodData: CreatePayrollPeriodRequest): Promise<PayrollPeriod> => {
  const response = await apiClient.post<PayrollPeriod>('/payroll/periods', periodData);
  return response;
};

/**
 * Get payroll runs list
 * @param filters - Optional filters (periodId, status, fromDate, toDate)
 * @returns Array of payroll runs
 */
export const getPayrollRuns = async (filters: PayrollRunFilters = {}): Promise<PayrollRun[]> => {
  const response = await apiClient.get<PayrollRun[]>('/payroll/runs', { params: filters });
  return response;
};

/**
 * Get payroll run by ID
 * @param runId - Payroll run ID
 * @returns Payroll run details
 */
export const getPayrollRun = async (runId: string): Promise<PayrollRun> => {
  const response = await apiClient.get<PayrollRun>(`/payroll/runs/${runId}`);
  return response;
};

/**
 * Create payroll run
 * @param runData - Payroll run creation data
 * @returns Created payroll run
 */
export const createPayrollRun = async (runData: CreatePayrollRunRequest): Promise<PayrollRun> => {
  const response = await apiClient.post<PayrollRun>('/payroll/runs', runData);
  return response;
};

/**
 * Finalize payroll run
 * @param runId - Payroll run ID
 * @returns Finalized payroll run
 */
export const finalizePayrollRun = async (runId: string): Promise<PayrollRun> => {
  const response = await apiClient.post<PayrollRun>(`/payroll/runs/${runId}/finalize`);
  return response;
};

/**
 * Mark payroll run as paid
 * @param runId - Payroll run ID
 * @returns Updated payroll run
 */
export const markPayrollRunPaid = async (runId: string): Promise<PayrollRun> => {
  const response = await apiClient.post<PayrollRun>(`/payroll/runs/${runId}/mark-paid`);
  return response;
};

/**
 * Get job earning records
 * @param filters - Optional filters (siId, period, orderId)
 * @returns Array of job earning records
 */
export const getJobEarningRecords = async (filters: JobEarningFilters = {}): Promise<JobEarningRecord[]> => {
  const response = await apiClient.get<JobEarningRecord[]>('/payroll/earnings', { params: filters });
  return response;
};

/**
 * Get SI rate plans list
 * @param filters - Optional filters (siId, isActive)
 * @returns Array of SI rate plans
 */
export const getSiRatePlans = async (filters: SiRatePlanFilters = {}): Promise<SiRatePlan[]> => {
  const response = await apiClient.get<SiRatePlan[]>('/payroll/si-rate-plans', { params: filters });
  return response;
};

/**
 * Get SI rate plan by ID
 * @param ratePlanId - Rate plan ID
 * @returns SI rate plan details
 */
export const getSiRatePlan = async (ratePlanId: string): Promise<SiRatePlan> => {
  const response = await apiClient.get<SiRatePlan>(`/payroll/si-rate-plans/${ratePlanId}`);
  return response;
};

/**
 * Create SI rate plan
 * @param ratePlanData - Rate plan creation data
 * @returns Created SI rate plan
 */
export const createSiRatePlan = async (ratePlanData: CreateSiRatePlanRequest): Promise<SiRatePlan> => {
  const response = await apiClient.post<SiRatePlan>('/payroll/si-rate-plans', ratePlanData);
  return response;
};

/**
 * Update SI rate plan
 * @param ratePlanId - Rate plan ID
 * @param ratePlanData - Rate plan update data
 * @returns Updated SI rate plan
 */
export const updateSiRatePlan = async (
  ratePlanId: string,
  ratePlanData: UpdateSiRatePlanRequest
): Promise<SiRatePlan> => {
  const response = await apiClient.put<SiRatePlan>(`/payroll/si-rate-plans/${ratePlanId}`, ratePlanData);
  return response;
};

/**
 * Delete SI rate plan
 * @param ratePlanId - Rate plan ID
 */
export const deleteSiRatePlan = async (ratePlanId: string): Promise<void> => {
  await apiClient.delete(`/payroll/si-rate-plans/${ratePlanId}`);
};

/**
 * Export SI rate plans to CSV file
 * @param filters - Optional filters (departmentId, isActive)
 */
export const exportSiRatePlans = async (filters: SiRatePlanFilters = {}): Promise<void> => {
  const token = getAuthToken();
  const params = new URLSearchParams(filters as any).toString();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/payroll/si-rate-plans/export${params ? '?' + params : ''}`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Export failed');
  }
  
  const blob = await response.blob();
  const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || 'si-rate-plans.csv';
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Download SI rate plans CSV template
 */
export const downloadSiRatePlansTemplate = async (): Promise<void> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/payroll/si-rate-plans/template`;
  
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (!response.ok) {
    throw new Error('Template download failed');
  }
  
  const blob = await response.blob();
  
  // Trigger download
  const downloadUrl = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = downloadUrl;
  a.download = 'si-rate-plans-template.csv';
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(downloadUrl);
  a.remove();
};

/**
 * Import SI rate plans from CSV file
 * @param file - CSV file to import
 * @returns Import result
 */
export const importSiRatePlans = async (file: File): Promise<ImportResult> => {
  const token = getAuthToken();
  const apiBaseUrl = getApiBaseUrl();
  const url = `${apiBaseUrl}/payroll/si-rate-plans/import`;
  
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    },
    body: formData
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Import failed');
  }
  
  return response.json();
};

