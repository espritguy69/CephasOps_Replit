import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getStockLocations } from '../api/inventory';
import { useToast } from '../components/ui';

/**
 * Hook to fetch warehouses (stock locations) list
 */
export const useWarehouses = () => {
  return useQuery({
    queryKey: ['warehouses'],
    queryFn: () => getStockLocations(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

