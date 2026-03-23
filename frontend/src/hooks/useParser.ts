import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import {
  getParseSessions,
  getParsedOrderDrafts,
  approveParsedOrderDraft,
  rejectParsedOrderDraft,
  type ParseSession,
  type ParsedOrderDraft,
  type RejectParsedOrderRequest,
} from '../api/parser';
import { useToast } from '../components/ui';

/**
 * PATTERN: TanStack Query Hooks for Parser
 * 
 * Key conventions:
 * - Create query key factory for consistent cache management
 * - Use proper query invalidation after mutations
 * - Show toast on mutation success/failure
 * - Invalidate related queries (parser and orders) after mutations
 */

// ==================== QUERY KEYS ====================

/**
 * Query key factory for parser operations
 */
export const parserKeys = {
  all: ['parser'] as const,
  sessions: () => [...parserKeys.all, 'sessions'] as const,
  sessionsList: (status?: string) => [...parserKeys.sessions(), status ?? 'all'] as const,
  session: (id: string) => [...parserKeys.sessions(), id] as const,
  drafts: () => [...parserKeys.all, 'drafts'] as const,
  draftsList: (sessionId: string) => [...parserKeys.drafts(), sessionId] as const,
  draft: (id: string) => [...parserKeys.drafts(), id] as const,
};

// ==================== QUERY HOOKS ====================

/**
 * React Query hook for fetching parse sessions
 * 
 * @param status - Optional status filter (Processing, Completed, Failed, Skipped)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useParseSessions = (
  status?: string,
  options?: Omit<UseQueryOptions<ParseSession[], Error>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<ParseSession[]>({
    queryKey: parserKeys.sessionsList(status),
    queryFn: () => getParseSessions(status),
    staleTime: 30000, // Consider data fresh for 30 seconds
    ...options,
  });
};

/**
 * React Query hook for fetching parsed order drafts for a session
 * 
 * @param sessionId - Parse session ID
 * @param options - Additional React Query options (including enabled)
 * @returns { data, isLoading, error, refetch }
 */
export const useParsedOrderDrafts = (
  sessionId: string | null,
  options?: Omit<UseQueryOptions<ParsedOrderDraft[], Error>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<ParsedOrderDraft[]>({
    queryKey: parserKeys.draftsList(sessionId || ''),
    queryFn: () => getParsedOrderDrafts(sessionId!),
    enabled: (options?.enabled !== false) && !!sessionId,
    staleTime: 30000, // Consider data fresh for 30 seconds
    ...options,
  });
};

// ==================== MUTATION HOOKS ====================

/**
 * React Query mutation hook for approving a parsed order draft
 * 
 * @param options - Additional mutation options
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useApproveParsedOrderDraft = (
  options?: Omit<UseMutationOptions<ParsedOrderDraft, Error, string>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<ParsedOrderDraft, Error, string>({
    mutationFn: (draftId: string) => approveParsedOrderDraft(draftId),
    onSuccess: (data, draftId) => {
      // Invalidate all parser-related queries to force refresh
      queryClient.invalidateQueries({ queryKey: parserKeys.all });
      // Also invalidate orders list since a new order was created
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      showSuccess('Order draft approved and order created');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to approve draft');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for rejecting a parsed order draft
 * 
 * @param options - Additional mutation options
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useRejectParsedOrderDraft = (
  options?: Omit<UseMutationOptions<ParsedOrderDraft, Error, { draftId: string; reason: string }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<ParsedOrderDraft, Error, { draftId: string; reason: string }>({
    mutationFn: ({ draftId, reason }) => 
      rejectParsedOrderDraft(draftId, { validationNotes: reason }),
    onSuccess: () => {
      // Invalidate all parser-related queries to force refresh
      queryClient.invalidateQueries({ queryKey: parserKeys.all });
      showSuccess('Draft rejected');
    },
    onError: (error) => {
      showError(error.message || 'Failed to reject draft');
    },
    ...options,
  });
};

