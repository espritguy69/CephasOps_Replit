import apiClient from './client';
import type { JobEarningRecord, EarningsSummary, KpiMetrics } from '../types/api';

/**
 * Earnings and KPI API
 * Handles earnings, payroll, and KPI data for SI app
 */

export interface EarningsFilters {
  period?: string;
  orderId?: string;
  fromDate?: string;
  toDate?: string;
}

/**
 * Get job earning records for current SI
 */
export const getMyEarnings = async (filters: EarningsFilters = {}): Promise<JobEarningRecord[]> => {
  const response = await apiClient.get<JobEarningRecord[] | { data: JobEarningRecord[] }>('/payroll/earnings', {
    params: {
      ...filters,
      // siId will be determined by backend from current user
    }
  });
  
  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && 'data' in response) {
    return (response as { data: JobEarningRecord[] }).data;
  }
  return [];
};

/**
 * Get earnings summary (daily, weekly, monthly)
 */
export const getEarningsSummary = async (period: string = 'month'): Promise<EarningsSummary> => {
  // Calculate date range based on period
  const now = new Date();
  let fromDate: Date, toDate: Date;
  
  if (period === 'today') {
    fromDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    toDate = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
  } else if (period === 'week') {
    const dayOfWeek = now.getDay();
    const diff = now.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1); // Monday
    fromDate = new Date(now.getFullYear(), now.getMonth(), diff);
    toDate = new Date(now.getFullYear(), now.getMonth(), diff + 6, 23, 59, 59);
  } else if (period === 'month') {
    fromDate = new Date(now.getFullYear(), now.getMonth(), 1);
    toDate = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59);
  } else {
    // Assume YYYY-MM format
    const [year, month] = period.split('-');
    fromDate = new Date(parseInt(year), parseInt(month) - 1, 1);
    toDate = new Date(parseInt(year), parseInt(month), 0, 23, 59, 59);
  }

  const records = await getMyEarnings({
    fromDate: fromDate.toISOString(),
    toDate: toDate.toISOString(),
  });

  // Calculate summary
  const summary: EarningsSummary = {
    totalEarnings: 0,
    totalJobs: records.length,
    totalBasePay: 0,
    totalAdjustments: 0,
    averagePerJob: 0,
    period,
    fromDate: fromDate.toISOString(),
    toDate: toDate.toISOString(),
  };

  records.forEach((record) => {
    const finalPay = record.finalPay || (record as any).FinalPay || 0;
    const basePay = record.baseRate || (record as any).BaseRate || 0;
    const adjustment = record.kpiAdjustment || (record as any).KpiAdjustment || 0;
    
    summary.totalEarnings += finalPay;
    summary.totalBasePay += basePay;
    summary.totalAdjustments += adjustment;
  });

  summary.averagePerJob = summary.totalJobs > 0 
    ? summary.totalEarnings / summary.totalJobs 
    : 0;

  return summary;
};

/**
 * Get KPI metrics for current SI
 */
export const getMyKpis = async (period: string = 'month'): Promise<KpiMetrics> => {
  // Get earnings records for KPI calculation
  const records = await getMyEarnings({ period });
  
  // Calculate KPI metrics
  const kpis: KpiMetrics = {
    totalJobs: records.length,
    onTimeJobs: 0,
    lateJobs: 0,
    exceededSlaJobs: 0,
    excusedJobs: 0,
    onTimeRate: 0,
    averageEarnings: 0,
    totalEarnings: 0,
  };

  records.forEach((record) => {
    const kpiResult = record.kpiResult || (record as any).KpiResult || '';
    const finalPay = record.finalPay || (record as any).FinalPay || 0;
    
    kpis.totalEarnings += finalPay;
    
    if (kpiResult === 'OnTime' || kpiResult === 'KpiOnTime') {
      kpis.onTimeJobs++;
    } else if (kpiResult === 'Late' || kpiResult === 'KpiLate') {
      kpis.lateJobs++;
    } else if (kpiResult === 'ExceededSla' || kpiResult === 'KpiExceededSla') {
      kpis.exceededSlaJobs++;
    } else if (kpiResult === 'Excused' || kpiResult === 'KpiExcused') {
      kpis.excusedJobs++;
    }
  });

  kpis.onTimeRate = kpis.totalJobs > 0 
    ? (kpis.onTimeJobs / kpis.totalJobs) * 100 
    : 0;
  kpis.averageEarnings = kpis.totalJobs > 0 
    ? kpis.totalEarnings / kpis.totalJobs 
    : 0;

  return kpis;
};

