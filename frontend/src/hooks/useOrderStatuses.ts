import { useQuery } from '@tanstack/react-query';
import {
  getOrderStatuses,
  getStatusesByWorkflow,
  getRmaStatuses,
  getKpiStatuses,
} from '../api/orderStatuses';

/**
 * Hook to fetch all order statuses
 */
export const useOrderStatuses = () => {
  return useQuery({
    queryKey: ['orderStatuses'],
    queryFn: () => getOrderStatuses(),
    staleTime: 30 * 60 * 1000, // 30 minutes - statuses rarely change
  });
};

/**
 * Hook to fetch statuses by workflow type
 */
export const useStatusesByWorkflow = (workflowType: string) => {
  return useQuery({
    queryKey: ['orderStatuses', 'workflow', workflowType],
    queryFn: () => getStatusesByWorkflow(workflowType),
    enabled: !!workflowType,
    staleTime: 30 * 60 * 1000,
  });
};

/**
 * Hook to fetch RMA statuses
 */
export const useRmaStatuses = () => {
  return useQuery({
    queryKey: ['orderStatuses', 'rma'],
    queryFn: () => getRmaStatuses(),
    staleTime: 30 * 60 * 1000,
  });
};

/**
 * Hook to fetch KPI statuses
 */
export const useKpiStatuses = () => {
  return useQuery({
    queryKey: ['orderStatuses', 'kpi'],
    queryFn: () => getKpiStatuses(),
    staleTime: 30 * 60 * 1000,
  });
};

