import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getInstallationMethods,
  getInstallationMethod,
  createInstallationMethod,
  updateInstallationMethod,
  deleteInstallationMethod,
  type InstallationMethodFilters,
  type CreateInstallationMethodRequest,
  type UpdateInstallationMethodRequest,
} from '../api/installationMethods';
import { useToast } from '../components/ui';

/**
 * Hook to fetch installation methods list
 */
export const useInstallationMethods = (filters: InstallationMethodFilters = {}) => {
  return useQuery({
    queryKey: ['installationMethods', filters],
    queryFn: () => getInstallationMethods(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single installation method
 */
export const useInstallationMethod = (id: string) => {
  return useQuery({
    queryKey: ['installationMethod', id],
    queryFn: () => getInstallationMethod(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new installation method
 */
export const useCreateInstallationMethod = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateInstallationMethodRequest) => createInstallationMethod(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['installationMethods'] });
      showSuccess('Installation method created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create installation method');
    },
  });
};

/**
 * Hook to update an installation method
 */
export const useUpdateInstallationMethod = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInstallationMethodRequest }) =>
      updateInstallationMethod(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['installationMethods'] });
      queryClient.invalidateQueries({ queryKey: ['installationMethod', variables.id] });
      showSuccess('Installation method updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update installation method');
    },
  });
};

/**
 * Hook to delete an installation method
 */
export const useDeleteInstallationMethod = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteInstallationMethod(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['installationMethods'] });
      showSuccess('Installation method deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete installation method');
    },
  });
};

