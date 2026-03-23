import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getOrderTypes,
  getOrderType,
  createOrderType,
  updateOrderType,
  deleteOrderType,
  type OrderTypeFilters,
  type CreateOrderTypeRequest,
  type UpdateOrderTypeRequest,
} from '../api/orderTypes';
import { useToast } from '../components/ui';

/**
 * Hook to fetch order types list
 */
export const useOrderTypes = (filters: OrderTypeFilters = {}) => {
  return useQuery({
    queryKey: ['orderTypes', filters],
    queryFn: () => getOrderTypes(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch a single order type
 */
export const useOrderType = (id: string) => {
  return useQuery({
    queryKey: ['orderType', id],
    queryFn: () => getOrderType(id),
    enabled: !!id,
  });
};

/**
 * Hook to create a new order type
 */
export const useCreateOrderType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (data: CreateOrderTypeRequest) => createOrderType(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orderTypes'] });
      showSuccess('Order type created successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to create order type');
    },
  });
};

/**
 * Hook to update an order type
 */
export const useUpdateOrderType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateOrderTypeRequest }) =>
      updateOrderType(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['orderTypes'] });
      queryClient.invalidateQueries({ queryKey: ['orderType', variables.id] });
      showSuccess('Order type updated successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to update order type');
    },
  });
};

/**
 * Hook to delete an order type
 */
export const useDeleteOrderType = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (id: string) => deleteOrderType(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orderTypes'] });
      showSuccess('Order type deleted successfully');
    },
    onError: (error: any) => {
      showError(error?.message || 'Failed to delete order type');
    },
  });
};

