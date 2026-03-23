import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { AxiosError } from 'axios';
import {
  getAllTasks,
  getTask,
  createTask,
  updateTask,
  updateTaskStatus,
  type TasksListFilters,
  type Task,
  type CreateTaskRequest,
  type UpdateTaskRequest,
} from '../api/tasks';
import { useDepartment } from '../contexts/DepartmentContext';
import { useToast } from '../components/ui';

/**
 * PATTERN: TanStack Query Hooks for Tasks
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
 * Query key factory for tasks
 */
export const tasksKeys = {
  all: ['tasks'] as const,
  lists: () => [...tasksKeys.all, 'list'] as const,
  list: (filters: TasksListFilters, departmentId?: string | null) => 
    [...tasksKeys.lists(), filters, departmentId ?? 'all'] as const,
  details: () => [...tasksKeys.all, 'detail'] as const,
  detail: (id: string | undefined, departmentId?: string | null) => 
    [...tasksKeys.details(), id, departmentId ?? 'all'] as const,
};

// ==================== QUERY HOOKS ====================

/**
 * Get all tasks with filtering
 * 
 * - Get departmentId from context
 * - Include departmentId in query key AND params
 * - Wait for department context to be ready before fetching
 */
export const useTasks = (filters: TasksListFilters = {}, options = {}) => {
  const { departmentId, loading: departmentLoading } = useDepartment();
  
  // Merge department into filters if not explicitly provided
  const params: TasksListFilters = {
    ...filters,
    ...(departmentId && !filters.departmentId ? { departmentId } : {}),
  };

  return useQuery<Task[], AxiosError>({
    queryKey: tasksKeys.list(params, departmentId),
    queryFn: () => getAllTasks(params),
    // Don't fetch until department context is ready (if department filtering is needed)
    enabled: !departmentLoading,
    ...options,
  });
};

/**
 * Get a single task by ID
 * 
 * - Require ID to be defined
 * - Include department context
 */
export const useTask = (id: string | undefined, options = {}) => {
  const { departmentId, loading: departmentLoading } = useDepartment();

  return useQuery<Task, AxiosError>({
    queryKey: tasksKeys.detail(id, departmentId),
    queryFn: () => {
      if (!id) throw new Error('Task ID is required');
      return getTask(id);
    },
    enabled: !!id && !departmentLoading,
    ...options,
  });
};

// ==================== MUTATION HOOKS ====================

/**
 * Create task mutation hook
 * 
 * - Invalidate list queries on success
 * - Show toast notifications
 */
export const useCreateTask = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Task, AxiosError, CreateTaskRequest>({
    mutationFn: (payload) => createTask(payload),
    onSuccess: (data) => {
      // Invalidate all task lists to show new task
      queryClient.invalidateQueries({ queryKey: tasksKeys.lists() });
      showSuccess('Task created successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to create task');
    },
  });
};

/**
 * Update task mutation hook
 * 
 * - Accept ID and payload
 * - Invalidate both list and detail queries
 */
export const useUpdateTask = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Task, AxiosError, { id: string; payload: UpdateTaskRequest }>({
    mutationFn: ({ id, payload }) => updateTask(id, payload),
    onSuccess: (data, { id }) => {
      // Invalidate lists and the specific detail
      queryClient.invalidateQueries({ queryKey: tasksKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tasksKeys.detail(id) });
      showSuccess('Task updated successfully');
    },
    onError: (error) => {
      showError(error.message || 'Failed to update task');
    },
  });
};

/**
 * Update task status mutation hook
 * 
 * - For status changes specifically
 * - Invalidate relevant queries
 */
export const useUpdateTaskStatus = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation<Task, AxiosError, { id: string; status: string; notes?: string }>({
    mutationFn: ({ id, status, notes }) => updateTaskStatus(id, status, notes || null),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: tasksKeys.lists() });
      queryClient.invalidateQueries({ queryKey: tasksKeys.detail(id) });
      showSuccess(`Task status updated to ${data.status}`);
    },
    onError: (error) => {
      showError(error.message || 'Failed to update task status');
    },
  });
};

