import { useQuery } from '@tanstack/react-query';
import { getBins } from '../api/bins';

export const useBins = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['bins', companyId, isActive],
    queryFn: () => getBins(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

