import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getRateCards,
  getRateCard,
  createRateCard,
  updateRateCard,
  deleteRateCard,
  getRateCardLines,
  createRateCardLine,
  updateRateCardLine,
  deleteRateCardLine,
  type RateCardFilters,
  type CreateRateCardRequest,
  type UpdateRateCardRequest,
  type CreateRateCardLineRequest,
} from '../api/rates';
import { useToast } from '../components/ui';

/**
 * Hook to fetch rate cards list
 */
export const useRateCards = (filters: RateCardFilters = {}) => {
  return useQuery({
    queryKey: ['rateCards', filters],
    queryFn: () => getRateCards(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single rate card
 */
export const useRateCard = (id: string) => {
  return useQuery({
    queryKey: ['rateCard', id],
    queryFn: () => getRateCard(id),
    enabled: !!id,
  });
};

/**
 * Hook to fetch rate card lines
 */
export const useRateCardLines = (rateCardId: string) => {
  return useQuery({
    queryKey: ['rateCardLines', rateCardId],
    queryFn: () => getRateCardLines(rateCardId),
    enabled: !!rateCardId,
  });
};

/**
 * Hook to create a new rate card
 */
export const useCreateRateCard = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateRateCardRequest) => createRateCard(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateCards'] });
      showSuccess('Rate card created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create rate card');
    },
  });
};

/**
 * Hook to update a rate card
 */
export const useUpdateRateCard = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRateCardRequest }) =>
      updateRateCard(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['rateCards'] });
      queryClient.invalidateQueries({ queryKey: ['rateCard', variables.id] });
      showSuccess('Rate card updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update rate card');
    },
  });
};

/**
 * Hook to delete a rate card
 */
export const useDeleteRateCard = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteRateCard(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateCards'] });
      showSuccess('Rate card deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete rate card');
    },
  });
};

/**
 * Hook to create a rate card line
 */
export const useCreateRateCardLine = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateRateCardLineRequest) => createRateCardLine(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['rateCardLines', variables.rateCardId] });
      queryClient.invalidateQueries({ queryKey: ['rateCard', variables.rateCardId] });
      showSuccess('Rate card line created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create rate card line');
    },
  });
};

/**
 * Hook to update a rate card line
 */
export const useUpdateRateCardLine = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<CreateRateCardLineRequest> }) =>
      updateRateCardLine(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateCardLines'] });
      queryClient.invalidateQueries({ queryKey: ['rateCards'] });
      showSuccess('Rate card line updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update rate card line');
    },
  });
};

/**
 * Hook to delete a rate card line
 */
export const useDeleteRateCardLine = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteRateCardLine(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateCardLines'] });
      queryClient.invalidateQueries({ queryKey: ['rateCards'] });
      showSuccess('Rate card line deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete rate card line');
    },
  });
};

