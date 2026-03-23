import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { AxiosError } from 'axios';
import {
  getPartnerGroups,
  getPartnerGroup,
  createPartnerGroup,
  updatePartnerGroup,
  deletePartnerGroup,
  type PartnerGroup,
  type CreatePartnerGroupRequest,
  type UpdatePartnerGroupRequest,
} from '../api/partnerGroups';
import { useToast } from '../components/ui';

/**
 * Partner Groups Hooks - TanStack Query hooks for partner group management
 * 
 * Key conventions:
 * - Create query key factory for consistent cache management
 * - Show toast on mutation success/failure
 * - Invalidate relevant queries after mutations
 */

// ==================== QUERY KEYS ====================

/**
 * Query key factory for partner groups
 * 
 * - Use array structure for hierarchical invalidation
 * - Export for use in other hooks that need to invalidate
 */
export const partnerGroupsKeys = {
  all: ['partner-groups'] as const,
  lists: () => [...partnerGroupsKeys.all, 'list'] as const,
  list: () => [...partnerGroupsKeys.lists()] as const,
  details: () => [...partnerGroupsKeys.all, 'detail'] as const,
  detail: (id: string | undefined) => [...partnerGroupsKeys.details(), id] as const,
};

// ==================== QUERY HOOKS ====================

/**
 * Get all partner groups
 * @param options - TanStack Query options
 * @returns Partner groups query
 */
export const usePartnerGroups = (options = {}) => {
  return useQuery<PartnerGroup[], AxiosError>({
    queryKey: partnerGroupsKeys.list(),
    queryFn: () => getPartnerGroups(),
    ...options,
  });
};

/**
 * Get single partner group by ID
 * @param id - Partner group ID
 * @param options - TanStack Query options
 * @returns Partner group query
 */
export const usePartnerGroup = (id: string | undefined, options = {}) => {
  return useQuery<PartnerGroup, AxiosError>({
    queryKey: partnerGroupsKeys.detail(id),
    queryFn: () => {
      if (!id) throw new Error('Partner group ID is required');
      return getPartnerGroup(id);
    },
    enabled: !!id,
    ...options,
  });
};

// ==================== MUTATION HOOKS ====================

/**
 * Create a new partner group
 * @returns Create partner group mutation
 */
export const useCreatePartnerGroup = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<PartnerGroup, AxiosError, CreatePartnerGroupRequest>({
    mutationFn: (payload) => createPartnerGroup(payload),
    onSuccess: () => {
      // Invalidate all partner group lists to show new partner group
      queryClient.invalidateQueries({ queryKey: partnerGroupsKeys.lists() });
      showSuccess('Partner group created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create partner group');
    },
  });
};

/**
 * Update an existing partner group
 * @returns Update partner group mutation
 */
export const useUpdatePartnerGroup = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<PartnerGroup, AxiosError, { id: string; payload: UpdatePartnerGroupRequest }>({
    mutationFn: ({ id, payload }) => updatePartnerGroup(id, payload),
    onSuccess: (data, { id }) => {
      // Invalidate lists and the specific detail
      queryClient.invalidateQueries({ queryKey: partnerGroupsKeys.lists() });
      queryClient.invalidateQueries({ queryKey: partnerGroupsKeys.detail(id) });
      showSuccess('Partner group updated successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to update partner group');
    },
  });
};

/**
 * Delete a partner group
 * @returns Delete partner group mutation
 */
export const useDeletePartnerGroup = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<void, AxiosError, string>({
    mutationFn: (id) => deletePartnerGroup(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: partnerGroupsKeys.lists() });
      // Remove the detail from cache
      queryClient.removeQueries({ queryKey: partnerGroupsKeys.detail(id) });
      showSuccess('Partner group deleted successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to delete partner group');
    },
  });
};

