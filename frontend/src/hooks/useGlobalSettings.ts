import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getGlobalSettings,
  getGlobalSetting,
  createGlobalSetting,
  updateGlobalSetting,
  deleteGlobalSetting,
  type GlobalSettingFilters,
  type CreateGlobalSettingRequest,
  type UpdateGlobalSettingRequest,
} from '../api/globalSettings';
import { useToast } from '../components/ui';

/**
 * Hook to fetch global settings list
 */
export const useGlobalSettings = (filters: GlobalSettingFilters = {}) => {
  return useQuery({
    queryKey: ['globalSettings', filters],
    queryFn: () => getGlobalSettings(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single global setting
 */
export const useGlobalSetting = (key: string) => {
  return useQuery({
    queryKey: ['globalSetting', key],
    queryFn: () => getGlobalSetting(key),
    enabled: !!key,
  });
};

/**
 * Hook to create a new global setting
 */
export const useCreateGlobalSetting = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateGlobalSettingRequest) => createGlobalSetting(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['globalSettings'] });
      showSuccess('Global setting created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create global setting');
    },
  });
};

/**
 * Hook to update a global setting
 */
export const useUpdateGlobalSetting = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ key, data }: { key: string; data: UpdateGlobalSettingRequest }) =>
      updateGlobalSetting(key, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['globalSettings'] });
      queryClient.invalidateQueries({ queryKey: ['globalSetting', variables.key] });
      showSuccess('Global setting updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update global setting');
    },
  });
};

/**
 * Hook to delete a global setting
 */
export const useDeleteGlobalSetting = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (key: string) => deleteGlobalSetting(key),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['globalSettings'] });
      showSuccess('Global setting deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete global setting');
    },
  });
};

