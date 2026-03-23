import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getPaymentTerms,
  getPaymentTerm,
  createPaymentTerm,
  updatePaymentTerm,
  deletePaymentTerm,
  type CreatePaymentTermDto,
  type UpdatePaymentTermDto,
} from '../api/paymentTerms';
import { useToast } from '../components/ui';

export const usePaymentTerms = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['paymentTerms', companyId, isActive],
    queryFn: () => getPaymentTerms(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const usePaymentTerm = (id: string) => {
  return useQuery({
    queryKey: ['paymentTerm', id],
    queryFn: () => getPaymentTerm(id),
    enabled: !!id,
  });
};

export const useCreatePaymentTerm = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ companyId, data }: { companyId: string; data: CreatePaymentTermDto }) =>
      createPaymentTerm(companyId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['paymentTerms'] });
      showSuccess('Payment term created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create payment term');
    },
  });
};

export const useUpdatePaymentTerm = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePaymentTermDto }) =>
      updatePaymentTerm(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['paymentTerms'] });
      showSuccess('Payment term updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update payment term');
    },
  });
};

export const useDeletePaymentTerm = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deletePaymentTerm(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['paymentTerms'] });
      showSuccess('Payment term deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete payment term');
    },
  });
};

