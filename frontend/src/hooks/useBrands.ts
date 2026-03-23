import { useQuery } from '@tanstack/react-query';
import { getBrands } from '../api/brands';

export const useBrands = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['brands', companyId, isActive],
    queryFn: () => getBrands(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

