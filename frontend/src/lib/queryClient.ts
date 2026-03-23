import { QueryClient } from '@tanstack/react-query';

/**
 * QueryClient configuration for React Query
 * 
 * Default options:
 * - staleTime: 1 minute - data is considered fresh for 1 minute
 * - gcTime: 5 minutes - unused data stays in cache for 5 minutes
 * - refetchOnWindowFocus: true - refetch when window regains focus
 * - retry: 1 - retry failed requests once
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60, // 1 minute
      gcTime: 1000 * 60 * 5, // 5 minutes (formerly cacheTime)
      refetchOnWindowFocus: true,
      retry: 1,
      refetchOnMount: true,
      refetchOnReconnect: true,
    },
    mutations: {
      retry: 1,
    },
  },
});

