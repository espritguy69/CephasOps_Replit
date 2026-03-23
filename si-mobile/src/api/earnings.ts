/**
 * Earnings API – payroll earnings for current SI.
 * Backend: GET /api/payroll/earnings.
 */
import { apiClient } from './client';
import type { JobEarningRecord } from '../types/api';

export interface EarningsFilters {
  fromDate?: string;
  toDate?: string;
  period?: string;
}

export async function getMyEarnings(filters: EarningsFilters = {}): Promise<JobEarningRecord[]> {
  const res = await apiClient.get<JobEarningRecord[] | { data: JobEarningRecord[] }>(
    '/payroll/earnings',
    { params: filters as Record<string, string | number | undefined> }
  );
  if (Array.isArray(res)) return res;
  if (res && typeof res === 'object' && 'data' in res)
    return (res as { data: JobEarningRecord[] }).data;
  return [];
}
