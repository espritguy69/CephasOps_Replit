import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getChecklistItemsByStatus,
  createChecklistItem,
  updateChecklistItem,
  deleteChecklistItem,
  getChecklistWithAnswers,
  submitChecklistAnswers,
  validateChecklist,
  reorderChecklistItems,
  bulkUpdateChecklistItems,
  copyChecklistFromStatus,
  type CreateOrderStatusChecklistItemDto,
  type UpdateOrderStatusChecklistItemDto,
  type SubmitOrderStatusChecklistAnswersDto,
} from '../api/orderStatusChecklists';
import { useToast } from '../components/ui';

/**
 * Hook to fetch checklist items for a status
 */
export const useChecklistItemsByStatus = (statusCode: string) => {
  return useQuery({
    queryKey: ['checklistItems', statusCode],
    queryFn: () => getChecklistItemsByStatus(statusCode),
    enabled: !!statusCode,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to create a checklist item
 */
export const useCreateChecklistItem = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateOrderStatusChecklistItemDto) =>
      createChecklistItem(statusCode, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      showSuccess('Checklist item created successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to create checklist item');
    },
  });
};

/**
 * Hook to update a checklist item
 */
export const useUpdateChecklistItem = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: UpdateOrderStatusChecklistItemDto }) =>
      updateChecklistItem(statusCode, itemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      queryClient.invalidateQueries({ queryKey: ['checklistWithAnswers'] });
      showSuccess('Checklist item updated successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to update checklist item');
    },
  });
};

/**
 * Hook to delete a checklist item
 */
export const useDeleteChecklistItem = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (itemId: string) => deleteChecklistItem(statusCode, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      queryClient.invalidateQueries({ queryKey: ['checklistWithAnswers'] });
      showSuccess('Checklist item deleted successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to delete checklist item');
    },
  });
};

/**
 * Hook to fetch checklist with answers for an order
 */
export const useChecklistWithAnswers = (orderId: string, statusCode: string) => {
  return useQuery({
    queryKey: ['checklistWithAnswers', orderId, statusCode],
    queryFn: () => getChecklistWithAnswers(orderId, statusCode),
    enabled: !!orderId && !!statusCode,
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

/**
 * Hook to submit checklist answers
 */
export const useSubmitChecklistAnswers = (orderId: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: SubmitOrderStatusChecklistAnswersDto) =>
      submitChecklistAnswers(orderId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistWithAnswers', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orders', orderId] });
      showSuccess('Checklist answers submitted successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to submit checklist answers');
    },
  });
};

/**
 * Hook to validate checklist completion
 */
export const useValidateChecklist = (orderId: string, statusCode: string) => {
  return useQuery({
    queryKey: ['checklistValidation', orderId, statusCode],
    queryFn: () => validateChecklist(orderId, statusCode),
    enabled: !!orderId && !!statusCode,
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Hook to reorder checklist items
 */
export const useReorderChecklistItems = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (itemOrderMap: Record<string, number>) =>
      reorderChecklistItems(statusCode, itemOrderMap),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      showSuccess('Checklist items reordered successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to reorder checklist items');
    },
  });
};

/**
 * Hook to bulk update checklist items
 */
export const useBulkUpdateChecklistItems = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({
      itemIds,
      updateDto,
    }: {
      itemIds: string[];
      updateDto: UpdateOrderStatusChecklistItemDto;
    }) => bulkUpdateChecklistItems(statusCode, itemIds, updateDto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      showSuccess('Checklist items updated successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to update checklist items');
    },
  });
};

/**
 * Hook to copy checklist from another status
 */
export const useCopyChecklistFromStatus = (statusCode: string) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (sourceStatusCode: string) =>
      copyChecklistFromStatus(statusCode, sourceStatusCode),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklistItems', statusCode] });
      showSuccess('Checklist copied successfully');
    },
    onError: (error: Error) => {
      showError(error.message || 'Failed to copy checklist');
    },
  });
};

