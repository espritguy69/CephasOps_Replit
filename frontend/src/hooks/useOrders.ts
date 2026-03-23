import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import {
  getOrders,
  getOrder,
  createOrder,
  updateOrder,
  getOrderStatusLogs,
  getOrderReschedules,
  getOrderBlockers,
  getOrderDockets,
} from '../api/orders';
import type { Order, OrderFilters, CreateOrderRequest, UpdateOrderRequest, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket } from '../types/orders';
import { useToast } from '../components/ui';
import { useDepartment } from '../contexts/DepartmentContext';

/**
 * React Query hook for fetching orders list
 * 
 * @param filters - Optional filters (status, partnerId, assignedSiId, buildingId, fromDate, toDate)
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch, isDepartmentLoading }
 */
export const useOrders = <TData = Order[]>(
  filters: OrderFilters = {},
  options?: Omit<UseQueryOptions<Order[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  const { departmentId, loading: departmentLoading } = useDepartment();
  const params: OrderFilters = {
    ...filters,
    ...(departmentId ? { departmentId } : {})
  };

  const query = useQuery<Order[], Error, TData>({
    queryKey: ['orders', params, departmentId ?? 'all'],
    queryFn: () => getOrders(params),
    // Don't fetch until department context is ready
    enabled: !departmentLoading && (options?.enabled !== false),
    ...options,
  });

  return {
    ...query,
    isDepartmentLoading: departmentLoading,
  };
};

/**
 * React Query hook for fetching a single order
 * 
 * @param orderId - Order ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch, isDepartmentLoading }
 */
export const useOrder = <TData = Order>(
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<Order, Error, TData>, 'queryKey' | 'queryFn'>
) => {
  const { departmentId, loading: departmentLoading } = useDepartment();

  const query = useQuery<Order, Error, TData>({
    queryKey: ['orders', orderId, departmentId ?? 'all'],
    queryFn: () => getOrder(orderId!, departmentId ? { departmentId } : {}),
    // Don't fetch until department context is ready
    enabled: !!orderId && !departmentLoading && (options?.enabled !== false),
    ...options,
  });

  return {
    ...query,
    isDepartmentLoading: departmentLoading,
  };
};

/**
 * React Query mutation hook for creating an order
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useCreateOrder = (
  options?: Omit<UseMutationOptions<Order, Error, CreateOrderRequest>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Order, Error, CreateOrderRequest>({
    mutationFn: (orderData) => createOrder(orderData),
    onSuccess: (data) => {
      // Invalidate and refetch orders list
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      showSuccess('Order created successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to create order');
    },
    ...options,
  });
};

/**
 * React Query mutation hook for updating an order
 * 
 * @returns { mutate, mutateAsync, isLoading, error, reset }
 */
export const useUpdateOrder = (
  options?: Omit<UseMutationOptions<Order, Error, { orderId: string; orderData: UpdateOrderRequest }>, 'mutationFn'>
) => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Order, Error, { orderId: string; orderData: UpdateOrderRequest }>({
    mutationFn: ({ orderId, orderData }) => updateOrder(orderId, orderData),
    onSuccess: (data, variables) => {
      // Invalidate specific order and orders list
      queryClient.invalidateQueries({ queryKey: ['orders', variables.orderId] });
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      showSuccess('Order updated successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to update order');
    },
    ...options,
  });
};

/**
 * React Query hook for fetching order status logs
 * 
 * @param orderId - Order ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useOrderStatusLogs = <TData = OrderStatusLog[]>(
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<OrderStatusLog[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<OrderStatusLog[], Error, TData>({
    queryKey: ['orders', orderId, 'status-logs'],
    queryFn: () => getOrderStatusLogs(orderId!),
    enabled: !!orderId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query hook for fetching order reschedules
 * 
 * @param orderId - Order ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useOrderReschedules = <TData = OrderReschedule[]>(
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<OrderReschedule[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<OrderReschedule[], Error, TData>({
    queryKey: ['orders', orderId, 'reschedules'],
    queryFn: () => getOrderReschedules(orderId!),
    enabled: !!orderId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query hook for fetching order blockers
 * 
 * @param orderId - Order ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useOrderBlockers = <TData = OrderBlocker[]>(
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<OrderBlocker[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<OrderBlocker[], Error, TData>({
    queryKey: ['orders', orderId, 'blockers'],
    queryFn: () => getOrderBlockers(orderId!),
    enabled: !!orderId && (options?.enabled !== false),
    ...options,
  });
};

/**
 * React Query hook for fetching order dockets
 * 
 * @param orderId - Order ID
 * @param options - Additional React Query options
 * @returns { data, isLoading, error, refetch }
 */
export const useOrderDockets = <TData = OrderDocket[]>(
  orderId: string | undefined,
  options?: Omit<UseQueryOptions<OrderDocket[], Error, TData>, 'queryKey' | 'queryFn'>
) => {
  return useQuery<OrderDocket[], Error, TData>({
    queryKey: ['orders', orderId, 'dockets'],
    queryFn: () => getOrderDockets(orderId!),
    enabled: !!orderId && (options?.enabled !== false),
    ...options,
  });
};

