import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import {
  getAssets,
  getAssetSummary,
  getAsset,
  createAsset,
  updateAsset,
  deleteAsset,
  getMaintenanceRecords,
  getUpcomingMaintenance,
  createMaintenanceRecord,
  updateMaintenanceRecord,
  deleteMaintenanceRecord,
  getDepreciationSchedule,
  createDisposal,
} from '../api/assets';
import type {
  Asset,
  AssetSummary,
  AssetFilters,
  CreateAssetRequest,
  UpdateAssetRequest,
  MaintenanceRecord,
  MaintenanceFilters,
  CreateMaintenanceRecordRequest,
  UpdateMaintenanceRecordRequest,
  DepreciationSchedule,
  CreateDisposalRequest,
  Disposal
} from '../types/assets';
import { useToast } from '../components/ui';

/**
 * React Query hook for fetching assets list
 * 
 * @param params - Optional filters (search, assetTypeId, status, departmentId, etc.)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useAssets = <TData = Asset[]>(
  params: AssetFilters = {},
  options?: Omit<UseQueryOptions<Asset[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Asset[], Error, TData>({
    queryKey: ['assets', params],
    queryFn: () => getAssets(params),
    ...options,
  });
};

/**
 * React Query hook for fetching asset summary (dashboard)
 * 
 * @param params - Optional parameters
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useAssetSummary = <TData = AssetSummary>(
  params: Record<string, any> = {},
  options?: Omit<UseQueryOptions<AssetSummary, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<AssetSummary, Error, TData>({
    queryKey: ['assets', 'summary', params],
    queryFn: () => getAssetSummary(),
    ...options,
  });
};

/**
 * React Query hook for fetching a single asset
 * 
 * @param assetId - Asset ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useAsset = <TData = Asset>(
  assetId: string | undefined,
  options?: Omit<UseQueryOptions<Asset, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Asset, Error, TData>({
    queryKey: ['assets', assetId],
    queryFn: () => getAsset(assetId!),
    enabled: !!assetId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query mutation hook for creating an asset
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateAsset = (
  options?: Omit<UseMutationOptions<Asset, Error, CreateAssetRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Asset, Error, CreateAssetRequest>({
    mutationFn: (assetData) => createAsset(assetData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'summary'] });
      showSuccess('Asset created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create asset');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for updating an asset
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useUpdateAsset = (
  options?: Omit<UseMutationOptions<Asset, Error, { assetId: string; assetData: UpdateAssetRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Asset, Error, { assetId: string; assetData: UpdateAssetRequest }>({
    mutationFn: ({ assetId, assetData }) => updateAsset(assetId, assetData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['assets', variables.assetId] });
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'summary'] });
      showSuccess('Asset updated successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to update asset');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for deleting an asset
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useDeleteAsset = (
  options?: Omit<UseMutationOptions<void, Error, string>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<void, Error, string>({
    mutationFn: (assetId) => deleteAsset(assetId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'summary'] });
      showSuccess('Asset deleted successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to delete asset');
    },
    ...options,
  });
};

/**
 * React Query hook for fetching maintenance records
 * 
 * @param params - Optional filters
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useMaintenanceRecords = <TData = MaintenanceRecord[]>(
  params: MaintenanceFilters = {},
  options?: Omit<UseQueryOptions<MaintenanceRecord[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<MaintenanceRecord[], Error, TData>({
    queryKey: ['assets', 'maintenance', params],
    queryFn: () => getMaintenanceRecords(params),
    ...options,
  });
};

/**
 * React Query hook for fetching upcoming maintenance
 * 
 * @param daysAhead - Number of days ahead to look
 * @param additionalParams - Additional parameters
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useUpcomingMaintenance = <TData = MaintenanceRecord[]>(
  daysAhead: number = 30,
  additionalParams: Record<string, any> = {},
  options?: Omit<UseQueryOptions<MaintenanceRecord[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<MaintenanceRecord[], Error, TData>({
    queryKey: ['assets', 'maintenance', 'upcoming', daysAhead, additionalParams],
    queryFn: () => getUpcomingMaintenance(daysAhead),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a maintenance record
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateMaintenanceRecord = (
  options?: Omit<UseMutationOptions<MaintenanceRecord, Error, CreateMaintenanceRecordRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<MaintenanceRecord, Error, CreateMaintenanceRecordRequest>({
    mutationFn: (data) => createMaintenanceRecord(data),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['assets', variables.assetId] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'maintenance'] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'maintenance', 'upcoming'] });
      showSuccess('Maintenance record created successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to create maintenance record');
    },
    ...options,
  });
};

/**
 * React Query hook for fetching depreciation schedule
 * 
 * @param assetId - Asset ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useDepreciationSchedule = <TData = DepreciationSchedule>(
  assetId: string | undefined,
  options?: Omit<UseQueryOptions<DepreciationSchedule, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<DepreciationSchedule, Error, TData>({
    queryKey: ['assets', assetId, 'depreciation'],
    queryFn: () => getDepreciationSchedule(assetId!),
    enabled: !!assetId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a disposal
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateDisposal = (
  options?: Omit<UseMutationOptions<Disposal, Error, CreateDisposalRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Disposal, Error, CreateDisposalRequest>({
    mutationFn: (data) => createDisposal(data),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['assets', variables.assetId] });
      queryClient.invalidateQueries({ queryKey: ['assets'] });
      queryClient.invalidateQueries({ queryKey: ['assets', 'summary'] });
      showSuccess('Disposal created successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to create disposal');
    },
    ...options,
  });
};

