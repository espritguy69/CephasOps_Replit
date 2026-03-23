import { useQuery } from '@tanstack/react-query';
import { getServicePlans } from '../api/servicePlans';

export const useServicePlans = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['servicePlans', companyId, isActive],
    queryFn: () => getServicePlans(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

