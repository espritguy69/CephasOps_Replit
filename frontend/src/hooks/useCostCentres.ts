import { useQuery } from '@tanstack/react-query';
import { getCostCentres } from '../api/costCentres';

export const useCostCentres = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['costCentres', companyId, isActive],
    queryFn: () => getCostCentres(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

