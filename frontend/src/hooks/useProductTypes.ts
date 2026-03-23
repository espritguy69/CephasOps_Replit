import { useQuery } from '@tanstack/react-query';
import { getProductTypes } from '../api/productTypes';

export const useProductTypes = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['productTypes', companyId, isActive],
    queryFn: () => getProductTypes(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

