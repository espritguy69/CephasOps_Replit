import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getTaxCodes,
  getTaxCode,
  createTaxCode,
  updateTaxCode,
  deleteTaxCode,
  type CreateTaxCodeDto,
  type UpdateTaxCodeDto,
} from '../api/taxCodes';
import { useToast } from '../components/ui';

export const useTaxCodes = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['taxCodes', companyId, isActive],
    queryFn: () => getTaxCodes(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useTaxCode = (id: string) => {
  return useQuery({
    queryKey: ['taxCode', id],
    queryFn: () => getTaxCode(id),
    enabled: !!id,
  });
};

export const useCreateTaxCode = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ companyId, data }: { companyId: string; data: CreateTaxCodeDto }) =>
      createTaxCode(companyId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxCodes'] });
      showSuccess('Tax code created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create tax code');
    },
  });
};

export const useUpdateTaxCode = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTaxCodeDto }) =>
      updateTaxCode(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxCodes'] });
      showSuccess('Tax code updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update tax code');
    },
  });
};

export const useDeleteTaxCode = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteTaxCode(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['taxCodes'] });
      showSuccess('Tax code deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete tax code');
    },
  });
};

