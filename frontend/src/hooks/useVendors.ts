import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getVendors,
  getVendor,
  createVendor,
  updateVendor,
  deleteVendor,
  type CreateVendorDto,
  type UpdateVendorDto,
} from '../api/vendors';
import { useToast } from '../components/ui';

export const useVendors = (companyId: string, isActive?: boolean) => {
  return useQuery({
    queryKey: ['vendors', companyId, isActive],
    queryFn: () => getVendors(companyId, isActive),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useVendor = (id: string) => {
  return useQuery({
    queryKey: ['vendor', id],
    queryFn: () => getVendor(id),
    enabled: !!id,
  });
};

export const useCreateVendor = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ companyId, data }: { companyId: string; data: CreateVendorDto }) =>
      createVendor(companyId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] });
      showSuccess('Vendor created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create vendor');
    },
  });
};

export const useUpdateVendor = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVendorDto }) =>
      updateVendor(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] });
      showSuccess('Vendor updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update vendor');
    },
  });
};

export const useDeleteVendor = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteVendor(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendors'] });
      showSuccess('Vendor deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete vendor');
    },
  });
};

