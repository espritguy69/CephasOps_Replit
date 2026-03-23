import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getMaterials,
  getMaterial,
  createMaterial,
  updateMaterial,
  deleteMaterial,
  type MaterialFilters,
  type CreateMaterialRequest,
  type UpdateMaterialRequest,
} from '../api/inventory';
import { useToast } from '../components/ui';

/**
 * Hook to fetch materials list
 */
export const useMaterials = (filters: MaterialFilters = {}) => {
  return useQuery({
    queryKey: ['materials', filters],
    queryFn: () => getMaterials(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single material
 */
export const useMaterial = (id: string) => {
  return useQuery({
    queryKey: ['material', id],
    queryFn: () => getMaterial(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new material
 */
export const useCreateMaterial = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateMaterialRequest) => createMaterial(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['materials'] });
      showSuccess('Material created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create material');
    },
  });
};

/**
 * Hook to update a material
 */
export const useUpdateMaterial = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMaterialRequest }) =>
      updateMaterial(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['materials'] });
      queryClient.invalidateQueries({ queryKey: ['material', variables.id] });
      showSuccess('Material updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update material');
    },
  });
};

/**
 * Hook to delete a material
 */
export const useDeleteMaterial = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteMaterial(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['materials'] });
      showSuccess('Material deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete material');
    },
  });
};

