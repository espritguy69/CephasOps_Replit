import { useQuery } from '@tanstack/react-query';
import { getTeams } from '../api/teams';

export const useTeams = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['teams', companyId, isActive],
    queryFn: () => getTeams(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

