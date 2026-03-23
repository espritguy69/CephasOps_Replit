import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { AxiosError } from 'axios';
import {
  getOrders,
  getOrder,
  createOrder,
  updateOrder,
  deleteOrder,
  changeOrderStatus,
  type OrdersListFilters,
  type Order,
  type CreateOrderDto,
} from '../api/orders';
import { useDepartment } from '../contexts/DepartmentContext';
import { useToast } from '../components/ui';

/**
 * PATTERN: TanStack Query Hooks
 * 
 * Key conventions:
 * - Create query key factory for consistent cache management
 * - Use useDepartment() to get department context
 * - Include departmentId in query keys for proper cache isolation
 * - Show toast on mutation success/failure
 * - Invalidate relevant queries after mutations
 */

// ==================== QUERY KEYS ====================

/**
 * PATTERN: Query key factory
 * 
 * - Use array structure for hierarchical invalidation
 * - Include all relevant parameters for cache isolation
 * - Export for use in other hooks that need to invalidate
 */
export const ordersKeys = {
  all: ['orders'] as const,
  lists: () => [...ordersKeys.all, 'list'] as const,
  list: (filters: OrdersListFilters, departmentId?: string | null) => 
    [...ordersKeys.lists(), filters, departmentId ?? 'all'] as const,
  details: () => [...ordersKeys.all, 'detail'] as const,
  detail: (id: string | undefined, departmentId?: string | null) => 
    [...ordersKeys.details(), id, departmentId ?? 'all'] as const,
};

// ==================== QUERY HOOKS ====================

/**
 * PATTERN: List query hook with department filtering
 * 
 * - Get departmentId from context
 * - Include departmentId in query key AND params
 * - Wait for department context to be ready before fetching
 */
export const useOrders = (filters: OrdersListFilters = {}, options = {}) => {
  const { departmentId, loading: departmentLoading } = useDepartment();
  
  // Merge department into filters
  const params: OrdersListFilters = {
    ...filters,
    ...(departmentId ? { departmentId } : {}),
  };

  return useQuery<Order[], AxiosError>({
    queryKey: ordersKeys.list(params, departmentId),
    queryFn: () => getOrders(params),
    // IMPORTANT: Don't fetch until department context is ready
    enabled: !departmentLoading,
    ...options,
  });
};

/**
 * PATTERN: Detail query hook
 * 
 * - Require ID to be defined
 * - Include department context
 */
export const useOrder = (id: string | undefined, options = {}) => {
  const { departmentId, loading: departmentLoading } = useDepartment();

  return useQuery<Order, AxiosError>({
    queryKey: ordersKeys.detail(id, departmentId),
    queryFn: () => {
      if (!id) throw new Error('Order ID is required');
      return getOrder(id, departmentId ? { departmentId } : {});
    },
    enabled: !!id && !departmentLoading,
    ...options,
  });
};

// ==================== MUTATION HOOKS ====================

/**
 * PATTERN: Create mutation hook
 * 
 * - Invalidate list queries on success
 * - Show toast notifications
 * - Return mutation result
 */
export const useCreateOrder = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Order, AxiosError, CreateOrderDto>({
    mutationFn: (payload) => createOrder(payload),
    onSuccess: (data) => {
      // Invalidate all order lists to show new order
      queryClient.invalidateQueries({ queryKey: ordersKeys.lists() });
      showSuccess('Order created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create order');
    },
  });
};

/**
 * PATTERN: Update mutation hook
 * 
 * - Accept ID and payload
 * - Invalidate both list and detail queries
 */
export const useUpdateOrder = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Order, AxiosError, { id: string; payload: Partial<Order> }>({
    mutationFn: ({ id, payload }) => updateOrder(id, payload),
    onSuccess: (data, { id }) => {
      // Invalidate lists and the specific detail
      queryClient.invalidateQueries({ queryKey: ordersKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ordersKeys.details() });
      showSuccess('Order updated successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to update order');
    },
  });
};

/**
 * PATTERN: Delete mutation hook
 * 
 * - Invalidate list queries
 * - Remove detail from cache
 */
export const useDeleteOrder = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<void, AxiosError, string>({
    mutationFn: (id) => deleteOrder(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ordersKeys.lists() });
      // Remove the detail from cache
      queryClient.removeQueries({ queryKey: ordersKeys.detail(id) });
      showSuccess('Order deleted successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to delete order');
    },
  });
};

/**
 * PATTERN: Action mutation hook
 * 
 * - For state changes that aren't full updates
 * - Invalidate relevant queries
 */
export const useChangeOrderStatus = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Order, AxiosError, { id: string; status: string; reason?: string }>({
    mutationFn: ({ id, status, reason }) => changeOrderStatus(id, status, reason),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: ordersKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ordersKeys.detail(id) });
      showSuccess(`Order status changed to ${data.status}`);
    },
    onError: (error) => {
      showError(error.message || 'Failed to change order status');
    },
  });
};
