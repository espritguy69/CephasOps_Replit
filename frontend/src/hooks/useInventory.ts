import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import {
  getMaterials,
  getMaterial,
  createMaterial,
  updateMaterial,
  deleteMaterial,
  getStockLocations,
  getStockMovements,
  recordStockMovement,
} from '../api/inventory';
import type {
  Material,
  MaterialFilters,
  CreateMaterialRequest,
  UpdateMaterialRequest,
  StockLocation,
  StockMovement,
  RecordStockMovementRequest
} from '../types/inventory';
import { useToast } from '../components/ui';

/**
 * React Query hook for fetching materials list
 * 
 * @param filters - Optional filters (category, search, isActive)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useMaterials = <TData = Material[]>(
  filters: MaterialFilters = {},
  options?: Omit<UseQueryOptions<Material[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Material[], Error, TData>({
    queryKey: ['inventory', 'materials', filters],
    queryFn: () => getMaterials(filters),
    ...options,
  });
};

/**
 * React Query hook for fetching a single material
 * 
 * @param materialId - Material ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useMaterial = <TData = Material>(
  materialId: string | undefined,
  options?: Omit<UseQueryOptions<Material, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<Material, Error, TData>({
    queryKey: ['inventory', 'materials', materialId],
    queryFn: () => getMaterial(materialId!),
    enabled: !!materialId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a material
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateMaterial = (
  options?: Omit<UseMutationOptions<Material, Error, CreateMaterialRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Material, Error, CreateMaterialRequest>({
    mutationFn: (materialData) => createMaterial(materialData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory', 'materials'] });
      showSuccess('Material created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create material');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for updating a material
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useUpdateMaterial = (
  options?: Omit<UseMutationOptions<Material, Error, { materialId: string; materialData: UpdateMaterialRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Material, Error, { materialId: string; materialData: UpdateMaterialRequest }>({
    mutationFn: ({ materialId, materialData }) => updateMaterial(materialId, materialData),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['inventory', 'materials', variables.materialId] });
      queryClient.invalidateQueries({ queryKey: ['inventory', 'materials'] });
      showSuccess('Material updated successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to update material');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for deleting a material
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useDeleteMaterial = (
  options?: Omit<UseMutationOptions<void, Error, string>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<void, Error, string>({
    mutationFn: (materialId) => deleteMaterial(materialId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory', 'materials'] });
      showSuccess('Material deleted successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to delete material');
    },
    ...options,
  });
};

/**
 * React Query hook for fetching stock locations
 * 
 * @param filters - Optional filters
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useStockLocations = <TData = StockLocation[]>(
  filters: Record<string, any> = {},
  options?: Omit<UseQueryOptions<StockLocation[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<StockLocation[], Error, TData>({
    queryKey: ['inventory', 'locations', filters],
    queryFn: () => getStockLocations(),
    ...options,
  });
};

/**
 * React Query hook for fetching stock movements
 * 
 * @param filters - Optional filters
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useStockMovements = <TData = StockMovement[]>(
  filters: Record<string, any> = {},
  options?: Omit<UseQueryOptions<StockMovement[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<StockMovement[], Error, TData>({
    queryKey: ['inventory', 'movements', filters],
    queryFn: () => getStockMovements(filters),
    ...options,
  });
};

/**
 * React Query mutation hook for creating a stock movement
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateStockMovement = (
  options?: Omit<UseMutationOptions<StockMovement, Error, RecordStockMovementRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<StockMovement, Error, RecordStockMovementRequest>({
    mutationFn: (movementData) => recordStockMovement(movementData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory', 'movements'] });
      queryClient.invalidateQueries({ queryKey: ['inventory', 'materials'] });
      showSuccess('Stock movement recorded successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to record stock movement');
    },
    ...options,
  });
};

