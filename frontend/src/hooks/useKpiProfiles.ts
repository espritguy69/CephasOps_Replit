import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getKpiProfiles,
  getKpiProfile,
  createKpiProfile,
  updateKpiProfile,
  deleteKpiProfile,
  setDefaultKpiProfile,
  type KpiProfileFilters,
  type CreateKpiProfileRequest,
  type UpdateKpiProfileRequest,
} from '../api/kpiProfiles';
import { useToast } from '../components/ui';

/**
 * Hook to fetch KPI profiles list
 */
export const useKpiProfiles = (filters: KpiProfileFilters = {}) => {
  return useQuery({
    queryKey: ['kpiProfiles', filters],
    queryFn: () => getKpiProfiles(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single KPI profile
 */
export const useKpiProfile = (id: string) => {
  return useQuery({
    queryKey: ['kpiProfile', id],
    queryFn: () => getKpiProfile(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new KPI profile
 */
export const useCreateKpiProfile = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateKpiProfileRequest) => createKpiProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create KPI profile');
    },
  });
};

/**
 * Hook to update a KPI profile
 */
export const useUpdateKpiProfile = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateKpiProfileRequest }) =>
      updateKpiProfile(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      queryClient.invalidateQueries({ queryKey: ['kpiProfile', variables.id] });
      showSuccess('KPI profile updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update KPI profile');
    },
  });
};

/**
 * Hook to delete a KPI profile
 */
export const useDeleteKpiProfile = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteKpiProfile(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete KPI profile');
    },
  });
};

/**
 * Hook to set KPI profile as default
 */
export const useSetDefaultKpiProfile = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => setDefaultKpiProfile(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kpiProfiles'] });
      showSuccess('KPI profile set as default');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to set default KPI profile');
    },
  });
};

