import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getMaterialCategories,
  type CreateMaterialCategoryRequest,
  type UpdateMaterialCategoryRequest,
} from '../api/inventory';
import { useToast } from '../components/ui';
import apiClient from '../api/client';

/**
 * Hook to fetch material categories list
 */
export const useMaterialCategories = (filters: { isActive?: boolean } = {}) => {
  return useQuery({
    queryKey: ['materialCategories', filters],
    queryFn: () => getMaterialCategories(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to create a new material category
 */
export const useCreateMaterialCategory = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateMaterialCategoryRequest) => 
      apiClient.post('/inventory/material-categories', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['materialCategories'] });
      showSuccess('Material category created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create material category');
    },
  });
};

/**
 * Hook to update a material category
 */
export const useUpdateMaterialCategory = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMaterialCategoryRequest }) =>
      apiClient.put(`/inventory/material-categories/${id}`, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['materialCategories'] });
      showSuccess('Material category updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update material category');
    },
  });
};

/**
 * Hook to delete a material category
 */
export const useDeleteMaterialCategory = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/inventory/material-categories/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['materialCategories'] });
      showSuccess('Material category deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete material category');
    },
  });
};

