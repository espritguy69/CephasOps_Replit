import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getAssetTypes,
  getAssetType,
  createAssetType,
  updateAssetType,
  deleteAssetType,
  type AssetTypeFilters,
  type CreateAssetTypeRequest,
  type UpdateAssetTypeRequest,
} from '../api/assetTypes';
import { useToast } from '../components/ui';

/**
 * Hook to fetch asset types list
 */
export const useAssetTypes = (filters: AssetTypeFilters = {}) => {
  return useQuery({
    queryKey: ['assetTypes', filters],
    queryFn: () => getAssetTypes(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single asset type
 */
export const useAssetType = (id: string) => {
  return useQuery({
    queryKey: ['assetType', id],
    queryFn: () => getAssetType(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new asset type
 */
export const useCreateAssetType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateAssetTypeRequest) => createAssetType(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assetTypes'] });
      showSuccess('Asset type created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create asset type');
    },
  });
};

/**
 * Hook to update an asset type
 */
export const useUpdateAssetType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAssetTypeRequest }) =>
      updateAssetType(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['assetTypes'] });
      queryClient.invalidateQueries({ queryKey: ['assetType', variables.id] });
      showSuccess('Asset type updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update asset type');
    },
  });
};

/**
 * Hook to delete an asset type
 */
export const useDeleteAssetType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteAssetType(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['assetTypes'] });
      showSuccess('Asset type deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete asset type');
    },
  });
};

